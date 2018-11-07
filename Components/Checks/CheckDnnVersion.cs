namespace DNN.Modules.SecurityAnalyzer.Components.Checks
{
    using DotNetNuke.Application;
    using DotNetNuke.Services.Localization;

    public class CheckDnnVersion : IAuditCheck
    {
        private const string ResourceFileRoot = "~/DesktopModules/DNNCorp/SecurityAnalyzer/App_LocalResources/View.ascx";

        public string Id => "CheckDnnVersion";

        public bool LazyLoad => false;

        public CheckResult Execute()
        {
            const int KnownCompromisedVersion = 8;
            var severity = DotNetNukeContext.Current.Application.Version.Major <= KnownCompromisedVersion
                ? SeverityEnum.Failure
                : SeverityEnum.Warning;

            return new CheckResult(severity, Id)
                   {
                       Notes =
                       {
                           Localization.GetString("CheckDnnVersion" +  severity + "Note", ResourceFileRoot),
                       }
                   };
        }
    }
}
