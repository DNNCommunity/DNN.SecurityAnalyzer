using System;
using System.Collections.Generic;
using System.Linq;

namespace DNN.Modules.SecurityAnalyzer.Components.Checks
{
    public class CheckUnexpectedExtensions : IAuditCheck
    {
        public string Id => "CheckUnexpectedExtensions";

        public CheckResult Execute()
        {
            var result = new CheckResult(SeverityEnum.Unverified, Id);
            try
            {
                IList<string> invalidFolders = new List<string>();
                var investigatefiles = Utility.FindUnexpectedExtensions(ref invalidFolders);
                if (investigatefiles.Count() > 0)
                {
                    result.Severity = SeverityEnum.Failure;
                    foreach (var filename in investigatefiles)
                    {
                        result.Notes.Add("file:" + filename);
                    }
                }
                else
                {
                    result.Severity = SeverityEnum.Pass;
                }

                if (invalidFolders.Count > 0)
                {
                    var folders = string.Join("", invalidFolders.Select(f => $"<p>{f}</p>").ToArray());
                    result.Notes.Add($"<p>Below folders are not able to access by permission restriction:</p>{folders}");
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