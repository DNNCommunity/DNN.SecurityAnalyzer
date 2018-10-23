namespace DNN.Modules.SecurityAnalyzer.Components.Checks
{
    using DotNetNuke.Application;

    public class CheckDnnVersion : IAuditCheck
    {
        public string Id => "CheckDnnVersion";

        public bool LazyLoad => false;

        public CheckResult Execute()
        {
            if (DotNetNukeContext.Current.Application.Version.Major < 9)
            {
                return new CheckResult(SeverityEnum.Failure, Id);
            }

            return new CheckResult(SeverityEnum.Warning, Id);
        }
    }
}