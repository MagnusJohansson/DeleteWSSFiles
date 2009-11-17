using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Utility;
using Microsoft.SharePoint;
using System.IO;

namespace DeleteFiles
{
  /// <summary>
  ///  Copyright © 2009 Magnus Johansson
  ///  http://www.InsomniacGeek.com
  /// </summary>
  public class Program
  {
    static StreamWriter writer = null;

    public static void Main(string[] args)
    {
      try
      {
        CommandArgs commandArgs = CommandLine.Parse(args);
        Dictionary<string, string> dict = commandArgs.ArgPairs;

        string url = string.Empty;
        bool recursive = false;
        bool preview = false;
        string mask = string.Empty;
        string outfile = string.Empty;
        bool all = false;

        if (dict.ContainsKey("url"))
        {
          url = dict["url"];
          if (url.Length < 1)
          {
            Usage();
            return;
          }
        }
        else
        {
          Usage();
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

        if (dict.ContainsKey("mask"))
        {
          mask = dict["mask"];
        }

        if (dict.ContainsKey("outfile"))
        {
          outfile = dict["outfile"];
          writer = new StreamWriter(outfile, true);
        }

        if (!preview)
        {
          Console.Write("Are you really sure you want to delete these files? (Y/N)");
          string result = Console.ReadLine();
          if (!result.ToLower().Contains('y'))
          {
            return;
          }
        }

        Console.WriteLine("Searching...");

        DeleteFiles(url, recursive, mask, preview, all);

        if (writer != null)
        {
          writer.Close();
          writer.Dispose();
        }

        Console.WriteLine("Done.");
      }
      catch (Exception ex)
      {
        Console.WriteLine("Error: " + ex.Message);
        if (ex.InnerException != null)
        {
          Console.WriteLine("ErrorInner exception: " + ex.InnerException.Message);
        }
      }
    }

    private static void DeleteFiles(string url, bool recursive, string fileMask, bool preview, bool deleteAll)
    {
      using (SPSite site = new SPSite(url))
      {
        IterateWeb(site.RootWeb, recursive, fileMask, preview, deleteAll);
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
    private static void IterateWeb(SPWeb web, bool recursive, string fileMask, bool preview, bool deleteAll)
    {
      foreach (SPWeb subWeb in web.Webs)
      {
        for (int listIndex = 0; listIndex < subWeb.Lists.Count; listIndex++)
        {
          SPList list = subWeb.Lists[listIndex];
          SPDocumentLibrary docLib = list as SPDocumentLibrary;
          if (docLib == null)
          {
            continue;
          }

          for (int i = list.Items.Count - 1; i >= 0; i--)
          {
            SPListItem item = list.Items[i];

            bool deleteIt = deleteAll;

            if (item.File != null)
            {
              if (fileMask.Length > 0)
              {
                if (!item.File.Name.ToLower().Contains(fileMask.ToLower()))
                {
                  continue;
                }
              }

              string info = string.Format("{0}/{1}/{2}", item.ParentList.ParentWeb.Url, item.ParentList.Title, item.File.Name);
              if (!deleteAll)
              {
                Console.Write("Do you want to delete {0}? ([Y]es/[N]o/Yes [A]ll)", info);
                string response = Console.ReadLine();
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
              else
              {
                Console.WriteLine(info);
              }


              if (!preview && deleteIt)
              {
                // DELETE IT
                list.Items.DeleteItemById(item.ID);

                Console.WriteLine("DELETED");

                if (writer != null)
                {
                  writer.WriteLine("{0}\t{1}", DateTime.Now.ToString(), info);
                  writer.Flush();
                }

              }
            }
          }
        }
        if (recursive)
        {
          IterateWeb(subWeb, recursive, fileMask, preview, deleteAll);
        }
        subWeb.Dispose();
      }
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
      Console.WriteLine("  [-mask] <value>");
      Console.WriteLine("  [-outfile <filename>]");
      Console.WriteLine();
      Console.WriteLine();
      Console.WriteLine("  -url <url>\t\tThe URL to request.");
      Console.WriteLine("  -recursive\tWill iterate all sub sites.");
      Console.WriteLine("  -recursive\tWill do a preview instead of delete.");
      Console.WriteLine("  -mask <value>\tFile mask value.");
      Console.WriteLine("  -outfile <filename>\tWill write the result to a logfile.");
    }

  }
}
