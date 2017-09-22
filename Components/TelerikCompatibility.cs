using System;
using System.IO;
using System.Reflection;
using System.Runtime.Remoting.Channels;
using System.Web;
using System.Web.UI;
using DotNetNuke.Application;
using DotNetNuke.Common;
using Assembly = System.Reflection.Assembly;

namespace DNN.Modules.SecurityAnalyzer.Components
{
    internal class TelerikCompatibility
    {
        public TelerikCompatibility()
        {
        }

        public void PatchIt()
        {
            try
            {
                if (DotNetNukeContext.Current.Application.Version < new Version(7, 1, 1))
                {
                    var editorFile = Path.Combine(Globals.ApplicationMapPath, "bin\\DotNetNuke.RadEditorProvider.dll");
                    var telerikFile = Path.Combine(Globals.ApplicationMapPath, "bin\\Telerik.Web.UI.dll");
                    if (File.Exists(editorFile) && File.Exists(telerikFile))
                    {
                        var type1 = Assembly.LoadFile(editorFile)
                            .GetType("DotNetNuke.Providers.RadEditorProvider.FileSystemValidation")
                            .GetMethod("OnCreateFile", BindingFlags.Public | BindingFlags.Instance)
                            .GetParameters()[1].ParameterType;
                        var type2 = Assembly.LoadFile(telerikFile)
                            .GetType("Telerik.Web.UI.UploadedFile")
                            .GetProperty("ContentLength", BindingFlags.Public | BindingFlags.Instance)?.PropertyType;

                        if (type1 != type2)
                        {
                            UpdateConfigFiles(DotNetNukeContext.Current.Application.Version < new Version(6, 2, 0));
                        }
                    }
                }
            }
            catch (Exception ex)
            {

            }
        }

        private void UpdateConfigFiles(bool pre62)
        {
            try
            {
                var folderPath = Path.Combine(Globals.ApplicationMapPath, "DesktopModules\\Admin\\RadEditorProvider\\ConfigFile");
                var files = Directory.GetFiles(folderPath, "*.xml");
                foreach (string file in files)
                {
                    ProcessConfigFile(file, pre62);
                }
            }
            catch (Exception ex)
            {
            }
        }

        private void ProcessConfigFile(string file, bool pre62)
        {
            try
            {
                //Backup
                var backUpFileName = file + ".bak";
                if (!File.Exists(backUpFileName))
                {
                    File.Copy(file, backUpFileName);
                }

                var patchedType = pre62
                    ? "Dnn.PatchedFileBrowserProviderPre62.PatchedFileBrowserProvider, Dnn.PatchedFileBrowserProviderPre62"
                    : "Dnn.PatchedFileBrowserProvider62.PatchedFileBrowserProvider, Dnn.PatchedFileBrowserProvider62";
                var content = File.ReadAllText(file);
                content =
                    content.Replace(
                        "DotNetNuke.Providers.RadEditorProvider.TelerikFileBrowserProvider, DotNetNuke.RadEditorProvider",
                        patchedType);

                File.WriteAllText(file, content);
            }
            catch (Exception)
            {
            }
        }
    }
}