using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Web;
using System.Web.UI;
using DotNetNuke.Common;
using DotNetNuke.Common.Utilities;
using DotNetNuke.Entities.Portals;
using DotNetNuke.Providers.RadEditorProvider;
using DotNetNuke.Services.FileSystem;
using Microsoft.VisualBasic.CompilerServices;
using Telerik.Web.UI.Widgets;
using Assembly = System.Reflection.Assembly;

namespace Dnn.PatchedFileBrowserProviderPre62
{
	public class PatchedFileBrowserProvider : TelerikFileBrowserProvider
	{
        private FileSystemValidation _DNNValidator;
        private FileSystemValidation DNNValidator
        {
            get
            {
                return _DNNValidator ?? (_DNNValidator = new FileSystemValidation());
            }
        }

        private FileController _DNNFileCtrl = null;
        private FileController DNNFileCtrl
        {
            get
            {
                if (_DNNFileCtrl == null)
                {
                    _DNNFileCtrl = new FileController();
                }
                return _DNNFileCtrl;
            }
        }

        private PortalSettings PortalSettings
        {
            get
            {
                return PortalSettings.Current;
            }
        }

        private FileSystemContentProvider _TelerikContent;
        private FileSystemContentProvider TelerikContent
        {
            get
            {
                if (this._TelerikContent == null)
                {
                    this._TelerikContent = new FileSystemContentProvider(this.Context, this.SearchPatterns, new string[]
                    {
                FileSystemValidation.HomeDirectory
                    }, new string[]
                    {
                FileSystemValidation.HomeDirectory
                    }, new string[]
                    {
                FileSystemValidation.HomeDirectory
                    }, Conversions.ToString(FileSystemValidation.ToVirtualPath(this.SelectedUrl)), Conversions.ToString(FileSystemValidation.ToVirtualPath(this.SelectedItemTag)));
                }
                return this._TelerikContent;
            }
        }


        public PatchedFileBrowserProvider(HttpContext context, string[] searchPatterns, string[] viewPaths, string[] uploadPaths, string[] deletePaths, string selectedUrl, string selectedItemTag) : base(context, searchPatterns, viewPaths, uploadPaths, deletePaths, selectedUrl, selectedItemTag)
		{
        }

        public override string StoreFile(Telerik.Web.UI.UploadedFile file, string path, string name, params string[] arguments)
        {
            try
            {
                string virtualPath = (string)typeof(FileSystemValidation)
                .GetMethod("ToVirtualPath", BindingFlags.Static | BindingFlags.Public)
                .Invoke(null, new[] { path }).ToString();

                string combinedVirtualPath = (string)typeof(FileSystemValidation)
                    .GetMethod("CombineVirtualPath", BindingFlags.Static | BindingFlags.Public)
                    .Invoke(null, new[] { virtualPath, name }).ToString();

                string returnValue = DNNValidator.OnCreateFile(combinedVirtualPath, (int)file.ContentLength);
                if (!(string.IsNullOrEmpty(returnValue)))
                {
                    return returnValue;
                }

                returnValue = TelerikContent.StoreFile(file, virtualPath, name, arguments);

                DotNetNuke.Services.FileSystem.FileInfo dnnFileInfo = new DotNetNuke.Services.FileSystem.FileInfo();
                FillFileInfo(file, ref dnnFileInfo);

                //Add or update file
                FolderInfo dnnFolder = DNNValidator.GetUserFolder(virtualPath);
                DotNetNuke.Services.FileSystem.FileInfo dnnFile = DNNFileCtrl.GetFile(name, PortalSettings.PortalId, dnnFolder.FolderID);
                if (dnnFile != null)
                {
                    DNNFileCtrl.UpdateFile(dnnFile.FileId, dnnFileInfo.FileName, dnnFileInfo.Extension, file.ContentLength, dnnFileInfo.Width, dnnFileInfo.Height, dnnFileInfo.ContentType, dnnFolder.FolderPath, dnnFolder.FolderID);
                }
                else
                {
                    DNNFileCtrl.AddFile(PortalSettings.PortalId, dnnFileInfo.FileName, dnnFileInfo.Extension, file.ContentLength, dnnFileInfo.Width, dnnFileInfo.Height, dnnFileInfo.ContentType, dnnFolder.FolderPath, dnnFolder.FolderID, true);
                }

                return returnValue;

            }
            catch (Exception ex)
            {
                string unknownText = GetUnknownText();
                return unknownText;
            }
        }

        private string GetUnknownText()
        {
            string str;
            try
            {
                str = DNNValidator.GetString("SystemError.Text");
            }
            catch (Exception ex)
            {
                str = "An unknown error occurred.";
            }
            return str;
        }

        private void ShowMessage(string message)
        {
            var pageObject = HttpContext.Current.Handler as Page;

            ScriptManager.RegisterClientScriptBlock(pageObject, pageObject.GetType(), "showAlertFromServer", @"
                    function showradAlertFromServer(message)
                    {
                        function f()
                        {// MS AJAX Framework is liaded
                            Sys.Application.remove_load(f);
                            // RadFileExplorer already contains a RadWindowManager inside, so radalert can be called without problem
                            radalert(message);
                        }

                        Sys.Application.add_load(f);
                    }", true);

            var script = string.Format("showradAlertFromServer('{0}');", message);
            ScriptManager.RegisterStartupScript(pageObject, pageObject.GetType(), "KEY", script, true);
        }

        private void FillFileInfo(Telerik.Web.UI.UploadedFile file, ref DotNetNuke.Services.FileSystem.FileInfo fileInfo)
        {
            //The core API expects the path to be stripped off the filename
            fileInfo.FileName = ((file.FileName.Contains("\\")) ? System.IO.Path.GetFileName(file.FileName) : file.FileName);
            fileInfo.Extension = file.GetExtension();
            if (fileInfo.Extension.StartsWith("."))
            {
                fileInfo.Extension = fileInfo.Extension.Remove(0, 1);
            }

            fileInfo.ContentType = FileSystemUtils.GetContentType(fileInfo.Extension);

            FillImageInfo(file.InputStream, ref fileInfo);
        }

        private void FillImageInfo(Stream fileStream, ref DotNetNuke.Services.FileSystem.FileInfo fileInfo)
        {
            var imageExtensions = new FileExtensionWhitelist(Globals.glbImageFileTypes);
            if (imageExtensions.IsAllowedExtension(fileInfo.Extension))
            {
                System.Drawing.Image img = null;
                try
                {
                    img = System.Drawing.Image.FromStream(fileStream);
                    if (fileStream.Length > int.MaxValue)
                    {
                        fileInfo.Size = int.MaxValue;
                    }
                    else
                    {
                        fileInfo.Size = int.Parse(fileStream.Length.ToString());
                    }
                    fileInfo.Width = img.Width;
                    fileInfo.Height = img.Height;
                }
                catch
                {
                    // error loading image file
                    fileInfo.ContentType = "application/octet-stream";
                }
                finally
                {
                    if (img != null)
                    {
                        img.Dispose();
                    }
                }
            }
        }
    }
}