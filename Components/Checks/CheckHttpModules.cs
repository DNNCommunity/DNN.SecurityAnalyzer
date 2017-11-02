using DNN.Modules.SecurityAnalyzer.HttpModules;

namespace DNN.Modules.SecurityAnalyzer.Components.Checks
{
    public class CheckHttpModules : IAuditCheck
    {
        public string Id => "CheckHttpModules";

        public bool LazyLoad => false;

        public CheckResult Execute()
        {
            var result = new CheckResult(SeverityEnum.Unverified, Id)
            {
                Severity = !SecurityAnalyzerModule.SAHttpModuleExists
                    ? SeverityEnum.Failure
                    : SeverityEnum.Pass
            };
            return result;
        }
    }
}