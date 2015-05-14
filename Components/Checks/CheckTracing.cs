using System;
using System.Web;
using System.Web.UI;

namespace DNN.Modules.SecurityAnalyzer.Components.Checks
{
    public class CheckTracing : IAuditCheck
    {
        public CheckResult Execute()
        {
            var result = new CheckResult(SeverityEnum.Unverified, "CheckTracing");
            try
            {
                var page = HttpContext.Current.Handler as Page;

                if (page != null)
                {
                    if (page.TraceEnabled)
                    {
                        result.Severity = SeverityEnum.Failure;
                    }
                    else
                    {
                        result.Severity = SeverityEnum.Pass;
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
            return result;
        }
    }
}