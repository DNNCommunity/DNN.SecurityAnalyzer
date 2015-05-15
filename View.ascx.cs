using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using DNN.Modules.SecurityAnalyzer.Components;
using DotNetNuke.Common;
using DotNetNuke.Common.Utilities;
using DotNetNuke.Entities.Users;
using DotNetNuke.Services.Exceptions;
using DotNetNuke.Services.Localization;

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
            }
            catch (Exception exc) //Module failed to load
            {
                Exceptions.ProcessModuleLoadException(this, exc);
            }

            if (!Page.IsPostBack)
            {
                Utility.CleanUpInstallerFiles();
                GetAuditResults();
                GetSuperUsers();
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
            pnlDatabaseresults.Visible = true;
            pnlFileresults.Visible = true;
            var foundinfiles = Utility.SearchFiles(txtSearchTerm.Text);
            IEnumerable<string> files = foundinfiles as IList<string> ?? foundinfiles.ToList();
            if (files.Any() == false)
            {
                lblfileresults.Text = Localization.GetString("NoFileResults");
            }
            else
            {
                var results = files.Aggregate("", (current, filename) => current + filename + "<br/>");
                lblfileresults.Text = results;
            }

            lbldatabaseresults.Text = Utility.SearchDatabase(txtSearchTerm.Text);
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
    }
}