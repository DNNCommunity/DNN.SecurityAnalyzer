using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.AccessControl;

namespace DNN.Modules.SecurityAnalyzer.Components.Checks
{
    public class DiskAcccessPermissions : IAuditCheck
    {
        public CheckResult Execute()
        {
            var result = new CheckResult(SeverityEnum.Unverified, "CheckDiskAccess");
            var accessErrors = CheckAccessToDrives();
            if (accessErrors.Count == 0)
            {
                result.Severity = SeverityEnum.Pass;
            }
            else
            {
                result.Severity = SeverityEnum.Failure;
                result.Notes = accessErrors;
            }
            return result;
        }

        #region private methods

        private static IList<string> CheckAccessToDrives()
        {
            var errors = new List<string>();
            try
            {
                var drives = DriveInfo.GetDrives();
                foreach (var drive in drives.Where(d => d.IsReady))
                {
                    var driveType = drive.DriveType;
                    if (driveType == DriveType.Fixed || driveType == DriveType.Network)
                    {
                        var permissions = CheckPermissionOnDir(drive.RootDirectory);
                        if (permissions.AnyYes)
                        {
                            errors.Add($"{drive.Name} - Read:{permissions.Read}, Write:{permissions.Write}, Create:{permissions.Create}, Delete:{permissions.Delete}");
                        }
                    }
                }
            }
            catch (IOException)
            {
                // e.g., a disk error or a drive was not ready
            }
            catch (UnauthorizedAccessException)
            {
                // The caller does not have the required permission.
            }
            return errors;
        }

        private static Permissions CheckPermissionOnDir(DirectoryInfo dir)
        {
            var permissions = new Permissions(No);
            var disSecurity = Directory.GetAccessControl(dir.FullName);
            var accessRules = disSecurity?.GetAccessRules(true, true, typeof(System.Security.Principal.SecurityIdentifier));
            if (accessRules != null)
            {
                foreach (FileSystemAccessRule rule in accessRules)
                {
                    if (rule.AccessControlType == AccessControlType.Allow)
                    {
                        if ((rule.FileSystemRights & (FileSystemRights.Write | FileSystemRights.WriteData)) != 0)
                            permissions.Write = Yes;

                        if ((rule.FileSystemRights & (FileSystemRights.Read | FileSystemRights.ReadData)) != 0)
                            permissions.Read = Yes;

                        if ((rule.FileSystemRights & FileSystemRights.Delete) != 0)
                            permissions.Delete = Yes;
                    }
                }
            }

            return permissions;
        }
        #endregion

        #region helpers

        private const char Yes = 'Y';
        private const char No = 'N';

        private struct Permissions
        {
            public char Create;
            public char Write;
            public char Read;
            public char Delete;

            public Permissions(char initial)
            {
                Create = Write = Read = Delete = initial;
            }

            public bool AnyYes => Create == Yes || Write == Yes || Read == Yes || Delete == Yes;
        }

        #endregion
    }
}