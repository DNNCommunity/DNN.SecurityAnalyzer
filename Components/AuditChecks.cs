using System.Collections.Generic;
using DNN.Modules.SecurityAnalyzer.Components.Checks;

namespace DNN.Modules.SecurityAnalyzer.Components
{
    public class AuditChecks
    {
        private readonly List<IAuditCheck> _auditChecks = new List<IAuditCheck>();

        public List<CheckResult> DoChecks()
        {
            _auditChecks.Add(new CheckDebug());
            _auditChecks.Add(new CheckTracing());
            _auditChecks.Add(new CheckViewstatemac());
            _auditChecks.Add(new CheckBiography());
            _auditChecks.Add(new CheckSiteRegistration());
            _auditChecks.Add(new CheckRarelyUsedSuperuser());
            _auditChecks.Add(new CheckSuperuserOldPassword());
            _auditChecks.Add(new CheckUnexpectedExtensions());
            _auditChecks.Add(new CheckDefaultPage());
            _auditChecks.Add(new CheckModuleHeaderAndFooter());


            var results = new List<CheckResult>();
            foreach (var check in _auditChecks)
            {
                var result = check.Execute();
                results.Add(result);
            }
            return results;
        }
    }
}