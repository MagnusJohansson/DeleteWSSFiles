# DeleteWSSFiles
This small command line utility is used for deleting files and documents from a SharePoint or MOSS 2007 site collection.

## Usage
`DeleteFiles.exe -url <website>
[-recursive]
[-preview]
[-contains] <value>
[-outfile <filename>]
[-quiet]`


`-url` <url> The URL to process.  
`-recursive` Will iterate all sub sites.  
`-preview` Will do a preview instead of delete.  
`-contains <value>` File name contains value. (Remark: It's not a file mask, it's a string comparison with the 'containing' method)  
`-outfile <filename>` Will write the result to a logfile.  
`-quiet` Will silently answer Yes to all delete questions.  

## Example  
Example, to delete all PDF files from a site:

`DeleteFiles.exe -url http://intranet -recursive -contains .pdf`  
Will delete all PDF documents from the whols site collection.

### Warning
Use this utility with care. It will permanently delete your documents from the site collection.

### Disclaimer
By using this utility you are fully responsible for what it does to your site collection.
