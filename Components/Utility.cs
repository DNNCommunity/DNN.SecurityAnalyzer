using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DotNetNuke.Common.Utilities;
using DotNetNuke.Data;

namespace DNN.Modules.SecurityAnalyzer.Components
{
    public class Utility
    {
        /// <summary>
        ///     delete unnedded installwizard files
        /// </summary>
        public static void CleanUpInstallerFiles()
        {
            FileSystemUtils.DeleteFile(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Install\\InstallWizard.aspx"));
            FileSystemUtils.DeleteFile(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Install\\InstallWizard.aspx.cs"));
        }

        private static string GetFileText(string name)
        {
            var fileContents = String.Empty;
            try
            {
                // If the file has been deleted since we took  
                // the snapshot, ignore it and return the empty string. 
                if (File.Exists(name))
                {
                    fileContents = File.ReadAllText(name);
                }
            }
            catch (Exception)
            {
                
                //might be a locking issue
            }
          
            return fileContents;
        }

        /// <summary>
        ///     search all files in the website for matching text
        /// </summary>
        /// <param name="searchText">the matching text</param>
        /// <returns>ienumerable of file names</returns>
        public static IEnumerable<string> SearchFiles(string searchText)
        {
            try
            {
                var dir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
                IEnumerable<FileInfo> fileList = dir.GetFiles("*.*", SearchOption.AllDirectories);
                var queryMatchingFiles =
                    from file in fileList
                    let fileText = GetFileText(file.FullName)
                    where fileText.Contains(searchText)
                    select file.Name;
                return queryMatchingFiles;
            }
            catch
            {
                //suppress any unexpected error
            }
            return null;
        }

        /// <summary>
        ///     search all website files for files with a particular extension
        /// </summary>
        /// <param name="extensions">a regular expression for extension</param>
        /// <returns></returns>
        public static string[] FindFiles(string extensions)
        {
            var files = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory, extensions);
            return files;
        }

        public static string SearchDatabase(string searchText)
        {
            var results = "";
            var dataProvider = DataProvider.Instance();
            var rowCount = 0;
            try
            {
                var dr = dataProvider.ExecuteReader("SearchAllTables", searchText);
                while (dr.Read())
                {
                    rowCount = rowCount + 1;
                    results = results + dr["ColumnName"] + ":" + dr["ColumnValue"] + "<br/>";
                }
            }
            catch
            {
            }
            results = "Database instances Found:" + rowCount + "<br/>" + results;
            return results;
        }
    }
}