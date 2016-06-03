using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Xml;
using DotNetNuke.Application;
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
            var files = new List<string>
            {
                "DotNetNuke.install.config",
                "DotNetNuke.install.config.resources",
                "InstallWizard.aspx",
                "InstallWizard.aspx.cs",
                "InstallWizard.aspx.designer.cs",
                "UpgradeWizard.aspx",
                "UpgradeWizard.aspx.cs",
                "UpgradeWizard.aspx.designer.cs",
                "Install.aspx",
                "Install.aspx.cs",
                "Install.aspx.designer.cs",
            };

            foreach (var file in files)
            {
                try
                {
                    FileSystemUtils.DeleteFile(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Install\\" + file));
                }
                catch (Exception ex)
                {
                    //do nothing.
                }
            }
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
        ///     search all website files for files with a potential dangerous extension
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<string> FindUnexpectedExtensions()
        {
            var files = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory, "*.*", SearchOption.AllDirectories)
            .Where(s => s.EndsWith(".asp") || s.EndsWith(".php"));
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

        public static XmlDocument LoadFileSumData()
        {
            using (
                var stream =
                    Assembly.GetExecutingAssembly()
                        .GetManifestResourceStream("DNN.Modules.SecurityAnalyzer.Resources.sums.resouces"))
            {
                if (stream != null)
                {
                    var xmlDocument = new XmlDocument();
                    xmlDocument.Load(stream);

                    return xmlDocument;
                }
                else
                {
                    return null;
                }
            }
        }

        public static string GetFileCheckSum(string fileName)
        {
            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(fileName))
                {
                    return BitConverter.ToString(md5.ComputeHash(stream)).Replace("-", "").ToLower();
                }
            }
        }

        public static string GetApplicationVersion()
        {
            return DotNetNukeContext.Current.Application.Version.ToString(3);
        }

        public static string GetApplicationType()
        {
            switch (DotNetNukeContext.Current.Application.Name)
            {
                case "DNNCORP.CE":
                    return "Platform";
                case "DNNCORP.XE":
                case "DNNCORP.PE":
                    return "Content";
                case "DNNCORP.SOCIAL":
                    return "Social";
                default:
                    return "Platform";
            }
        }
    }
}