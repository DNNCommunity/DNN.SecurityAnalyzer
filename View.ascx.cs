using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using DNN.Modules.SecurityAnalyzer.Components;
using DotNetNuke.Common;
using DotNetNuke.Common.Utilities;
using DotNetNuke.Data;
using DotNetNuke.Entities.Users;
using DotNetNuke.Services.Exceptions;
using DotNetNuke.Services.Localization;
using DotNetNuke.UI.Skins;
using DotNetNuke.UI.Skins.Controls;

namespace DNN.Modules.SecurityAnalyzer
{
    public partial class View : SecurityAnalyzerModuleBase
    {
        protected ArrayList Users { get; set; }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!UserInfo.IsSuperUser)
            {
                Response.Redirect(Globals.NavigateURL("Access Denied"), true);
            }

            try
            {
                cmdSearch.Click += cmdSearch_Click;
                cmdModifiedFiles.Click += cmdModifiedFiles_Click;
            }
            catch (Exception exc) //Module failed to load
            {
                Exceptions.ProcessModuleLoadException(this, exc);
            }

            if (!Page.IsPostBack)
            {
                Utility.CleanUpInstallerFiles();

                if (Utility.UpdateTelerikSkinsSettings())
                {
                    Response.Redirect(Request.RawUrl, true);
                }

                GetAuditResults();
                GetSuperUsers();
                GetModifiedSettings();


                if (DotNetNuke.Application.DotNetNukeContext.Current.Application.Version < new Version(6, 0, 0))
                {
                    panelResources.Visible = true;
                }
            }
        }

        private void GetAuditResults()
        {
            var audit = new AuditChecks();
            Localization.LocalizeDataGrid(ref dgResults, LocalResourceFile);
            dgResults.DataSource = audit.DoChecks();
            dgResults.DataBind();
        }

        private void cmdSearch_Click(object sender, EventArgs e)
        {
            var scriptTimeout = Server.ScriptTimeout;
            try
            {
                Server.ScriptTimeout = int.MaxValue;

                pnlDatabaseresults.Visible = true;
                pnlFileresults.Visible = true;
                var foundinfiles = Utility.SearchFiles(txtSearchTerm.Text);
                IEnumerable<string> files = foundinfiles as IList<string> ?? foundinfiles.ToList();
                if (files.Any() == false)
                {
                    lblfileresults.Text = Localization.GetString("NoFileResults", LocalResourceFile);
                }
                else
                {
                    var results = files.Aggregate("", (current, filename) => current + filename + "<br/>");
                    lblfileresults.Text = results;
                }

                lbldatabaseresults.Text = Utility.SearchDatabase(txtSearchTerm.Text);
            }
            finally
            {
                Server.ScriptTimeout = scriptTimeout;
            }
        }

        private void SetMinimalModifiedFilesGrid()
        {
            dgModifiedFiles.DataSource = new List<FileInfo>
                {
                    new FileInfo(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "web.config")),
                    new FileInfo(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "default.aspx"))
                };
            dgModifiedFiles.DataBind();
        }

        private void cmdModifiedFiles_Click(object sender, EventArgs e)
        {
            var scriptTimeout = Server.ScriptTimeout;
            try
            {
                Server.ScriptTimeout = 600; // 10 minutes

                Localization.LocalizeDataGrid(ref dgModifiedExecutableFiles, LocalResourceFile);
                dgModifiedExecutableFiles.DataSource = Utility.GetLastModifiedExecutableFiles();
                dgModifiedExecutableFiles.DataBind();

                Localization.LocalizeDataGrid(ref dgModifiedFiles, LocalResourceFile);
                dgModifiedFiles.DataSource = Utility.GetLastModifiedFiles();
                dgModifiedFiles.DataBind();
            }
            catch (Exception)
            {
                SetMinimalModifiedFilesGrid();
            }
            finally
            {
                Server.ScriptTimeout = scriptTimeout;
            }
        }

        public string GetSeverityImageUrl(int severity)
        {
            switch (severity)
            {
                case (int) SeverityEnum.Pass:
                    return ResolveUrl("~/images/green-ok.gif");
                case (int) SeverityEnum.Warning:
                    return ResolveUrl("~/images/yellow-warning.gif");
                case (int) SeverityEnum.Failure:
                    return ResolveUrl("~/images/red-error.gif");
            }
            return ResolveUrl("~/images/icon_help_32px.gif");
        }

        public string DisplayResult(int severity, string successText, string failureTest)
        {
            switch (severity)
            {
                case (int) SeverityEnum.Pass:
                    return successText;
                default:
                    return failureTest;
            }
        }

        public string DisplayFriendlyName(string reason)
        {
            return reason;
        }

        public string DisplayNotes(IList<String> notes)
        {
            try
            {
                if (notes != null)
                {
                    if (notes.Count == 0)
                    {
                        return "N/A";
                    }
                    return notes.Aggregate(string.Empty, (current, note) => current + note + "<br/>");
                }
            }
            catch (Exception)
            {
                throw;
            }

            return "N/A";
        }

        public string DisplayEmail(string email)
        {
            var displayEmail = Null.NullString;
            try
            {
                if (email != null)
                {
                    displayEmail = HtmlUtils.FormatEmail(email, false);
                }
            }
            catch (Exception exc) //Module failed to load
            {
                Exceptions.ProcessModuleLoadException(this, exc);
            }
            return displayEmail;
        }

        public string GetFilePath(string filePath)
        {
            var path = Regex.Replace(filePath, Regex.Escape(Globals.ApplicationMapPath), string.Empty, RegexOptions.IgnoreCase);

            return path.TrimStart('\\');
        }

        public string DisplayDate(DateTime userDate)
        {
            var date = Null.NullString;
            try
            {
                date = !Null.IsNull(userDate) ? userDate.ToString(CultureInfo.InvariantCulture) : "";
            }
            catch (Exception exc) //Module failed to load
            {
                Exceptions.ProcessModuleLoadException(this, exc);
            }
            return date;
        }

        private void GetSuperUsers()
        {
            var totalRecords = 0;

            Users = UserController.GetUsers(-1, dgUsers.CurrentPageIndex, dgUsers.PageSize,
                ref totalRecords, true, true);

            Localization.LocalizeDataGrid(ref dgUsers, LocalResourceFile);
            dgUsers.DataSource = Users;
            dgUsers.DataBind();
        }

        private void GetModifiedSettings()
        {
            try
            {
                var reader = DataProvider.Instance().ExecuteReader("SecurityAnalyzer_GetModifiedSettings");
                if (reader != null)
                {
                    var tables = new List<DataTable>();
                    do
                    {
                        var table = new DataTable { Locale = CultureInfo.CurrentCulture };
                        table.Load(reader);
                        tables.Add(table);
                    }
                    while (!reader.IsClosed); // table.Load automatically moves to the next result and closes the reader once there are no more

                    dgPortalSettings.DataSource = tables[0];
                    dgPortalSettings.DataBind();

                    dgHostSettings.DataSource = tables[1];
                    dgHostSettings.DataBind();

                    dgTabSettings.DataSource = tables[2];
                    dgTabSettings.DataBind();

                    dgModuleSettings.DataSource = tables[3];
                    dgModuleSettings.DataBind();
                }
            }
            catch (Exception ex)
            {
                Skin.AddModuleMessage(this, ex.Message, ModuleMessage.ModuleMessageType.RedError);
                throw;
            }
        }

    }
}