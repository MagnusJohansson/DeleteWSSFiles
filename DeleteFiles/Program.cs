using System;
using System.Collections.Generic;
using System.Linq;
using Utility;
using Microsoft.SharePoint;
using log4net;
using System.Reflection;

namespace DeleteFiles
{
  /// <summary>
  ///  Copyright © 2009 Magnus Johansson
  ///  http://www.InsomniacGeek.com
  /// </summary>
  public class Program
  {
    //private static StreamWriter _writer;
    private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

    public static void Main(string[] args)
    {
      try
      {
        log4net.Config.XmlConfigurator.Configure();

        CommandArgs commandArgs = CommandLine.Parse(args);
        Dictionary<string, string> dict = commandArgs.ArgPairs;

        string url;
        bool recursive = false;
        bool preview = false;
        string contains = string.Empty;
        const bool all = false;
        bool quiet = false;

        if (dict.ContainsKey("url"))
        {
          url = dict["url"];
          if (url.Length < 1)
          {
            Usage();
            Log.Info("Missing -url parameter");
            return;
          }
        }
        else
        {
          Usage();
          Log.Info("Missing -url parameter");
          return;
        }

        if (dict.ContainsKey("recursive"))
        {
          recursive = true;
        }
        if (dict.ContainsKey("preview"))
        {
          preview = true;
        }

        if (dict.ContainsKey("contains"))
        {
          contains = dict["contains"];
        }
        else
        {
          Usage();
          Log.Info("Missing -contains parameter");
          return;
        }

        if (dict.ContainsKey("outfile"))
        {
          var outfile = dict["outfile"];
        }

        if (dict.ContainsKey("quiet"))
        {
          quiet = true;
        }

        if (!preview && !quiet)
        {
          Console.Write("Are you really sure you want to delete files? (Y/N)");
          var result = Console.ReadLine();
          if (result != null)
            if (!result.ToLower().Contains('y'))
            {
              return;
            }
        }
        Log.Info(string.Format("Started with paramers: {0}", string.Join(" ", args)));

        Log.Info("Searching...");
        DeleteFiles(url, recursive, contains, preview, all, quiet);

        Log.Info("Done.");
      }
      catch (Exception ex)
      {
        Log.Error("Error: ", ex);
      }
    }

    private static void DeleteFiles(string url,
                                    bool recursive,
                                    string fileMask,
                                    bool preview,
                                    bool deleteAll,
                                    bool quiet)
    {
      using (var site = new SPSite(url))
      {
        IterateWeb(site.RootWeb, recursive, fileMask, preview, deleteAll, quiet);
      }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="web"></param>
    /// <param name="recursive"></param>
    /// <param name="fileMask"></param>
    /// <param name="preview"></param>
    /// <param name="deleteAll"></param>
    private static void IterateWeb(SPWeb web,
                                    bool recursive,
                                    string fileMask,
                                    bool preview,
                                    bool deleteAll,
                                    bool quiet)
    {
      DeleteFromWeb(web, deleteAll, fileMask, preview, quiet);
      if (!recursive) return;

      foreach (SPWeb subWeb in web.Webs)
      {
        Log.Debug(string.Format("Iterating sub web {0}", subWeb.Url));
        IterateWeb(subWeb, true, fileMask, preview, deleteAll, quiet);
        subWeb.Dispose();
      }
    }

    private static SPListItemCollection GetItems(SPList list, string fileMask)
    {
      var queryString = string.Format(@"<Where>
      <Contains>
         <FieldRef Name='FileLeafRef' />
         <Value Type='File'>{0}</Value>
      </Contains>
   </Where>", fileMask);
      var spQuery = new SPQuery
                      {
                        Query = queryString,
                        ViewFields = "<FieldRef Name='ID'></FieldRef>",
                        ViewAttributes = "Scope=\"Recursive\""
                      };
      return list.GetItems(spQuery);
    }

    private static bool DeleteFromWeb(SPWeb subWeb,
                                      bool deleteAll,
                                      string fileMask,
                                      bool preview,
                                      bool quiet)
    {
      var uri = new Uri(subWeb.Url);

      Log.Debug("Iterating lists...");
      for (var listIndex = 0; listIndex < subWeb.Lists.Count; listIndex++)
      {
        var list = subWeb.Lists[listIndex];
        Log.Debug(string.Format("Searching list {0}/{1}...", list.ParentWebUrl, list.Title));

        if (list.BaseTemplate != SPListTemplateType.DocumentLibrary)
        {
          Log.Debug("Not a Document Library, skipping.");
          continue;
        }

        SPListItemCollection items = GetItems(list, fileMask); //list.Items;
        if (items == null)
        {
          Log.Debug("No matching items found.");
          continue;
        }

        for (var i = 0; i < items.Count; i++)
        {
          SPListItem item = items[i]; //list.Items[i];

          Log.Debug(string.Format("Checking list item {0}/{1}...", item.File.ParentFolder.ServerRelativeUrl, item.Name));

          bool deleteIt = deleteAll;

          //if (item.File == null) continue;
          //if (fileMask.Length > 0)
          //{
          //  if (!FindFilesPatternToRegex.Convert(fileMask).IsMatch(item.File.Name))
          //  {
          //    Log.Debug(string.Format("{0}/{1} doesn't match the file mask. Skipping this item...", item.File.ParentFolder.ServerRelativeUrl, item.File.Name));
          //    continue;
          //  }
          //}

          var info = string.Format("{0}://{1}{2}/{3}", uri.Scheme, uri.Host, item.File.ParentFolder.ServerRelativeUrl, item.File.Name);

          if (quiet)
          {
            deleteAll = true;
            deleteIt = true;
          }

          if (!deleteAll)
          {
            Log.Debug("Asking user for input");
            Console.Write("Do you want to delete {0}? ([Y]es/[N]o/Yes [A]ll)", info);
            string response = Console.ReadLine();
            Log.Debug(string.Format("User respone: {0}", response));
            if (response != null)
            {
              if (response.ToLower().Contains("a"))
              {
                deleteIt = true;
                deleteAll = true;
              }
              if (response.ToLower().Contains("y"))
              {
                deleteIt = true;
              }
            }
          }

          if (deleteIt)
          {
            Log.Info(string.Format("{0} Deleting {1}", preview ? "[PREVIEW]" : "", info));
          }


          if (preview || !deleteIt) continue;

          // DELETE IT
          Log.Debug(string.Format("Trying to delete item {0}", item.Name));
          if (item.File.CheckOutStatus == SPFile.SPCheckOutStatus.None)
          {
            list.Items.DeleteItemById(item.ID);
            Log.Debug(string.Format("Item {0} deleted", item.Name));
          }
          else
          {
            Log.Info(string.Format("File {0} is checked out, skipped.", item.Name));
          }

          //Log.Info(info);
        }
      }
      return deleteAll;
    }

    /// <summary>
    /// Prints the usage of the arguments
    /// </summary>
    private static void Usage()
    {
      Console.WriteLine("Usage:");
      Console.WriteLine("DeleteFiles.exe -url <website> ");
      Console.WriteLine("  [-recursive]");
      Console.WriteLine("  [-preview]");
      Console.WriteLine("  [-contains] <value>");
      Console.WriteLine("  [-outfile <filename>]");
      Console.WriteLine("  [-quiet]");
      Console.WriteLine();
      Console.WriteLine();
      Console.WriteLine("  -url <url>\t\tThe URL to process.");
      Console.WriteLine("  -recursive\t\tWill iterate all sub sites.");
      Console.WriteLine("  -preview\t\tWill do a preview instead of delete.");
      Console.WriteLine("  -contains <value>\tFile name contains value. (Remark: It's not a file mask, it's a string comparison with the 'containing' method)");
      Console.WriteLine("  -outfile <filename>\tWill write the result to a logfile.");
      Console.WriteLine("  -quiet \t\tWill silently answer Yes to all delete questions.");
      Console.WriteLine("");
      Console.WriteLine("Example, to delete all PDF files from a site:");
      Console.WriteLine("DeleteFiles.exe -url http://intranet -recursive -contains .pdf");
    }

  }
}
