using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

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
                        var permissions = CheckCreateWrireRead(drive.RootDirectory);
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

        private static Permissions CheckCreateWrireRead(DirectoryInfo dir)
        {
            var permissions = new Permissions(No);
            var text = Guid.NewGuid().ToString();
            var buffer = Encoding.ASCII.GetBytes(text);
            var fname = Path.Combine( dir.FullName, Path.GetRandomFileName());

            try
            {
                using (var stream = File.OpenWrite(fname))
                {
                    if (stream.CanTimeout)
                    {
                        stream.WriteTimeout = stream.ReadTimeout = 1000;
                    }

                    permissions.Create = Yes;
                    stream.Write(buffer, 0, buffer.Length);
                    stream.Flush();
                    stream.Close();
                    permissions.Write = Yes;
                }

                Array.Clear(buffer, 0, buffer.Length);
                using (var stream2 = File.OpenRead(fname))
                {
                    stream2.Read(buffer, 0, buffer.Length);
                    var text2 = Encoding.ASCII.GetString(buffer);
                    if (text2 == text)
                        permissions.Read = Yes;
                }
            }
            catch (Exception)
            {
                // ignore
            }
            finally
            {
                if (File.Exists(fname))
                {
                    try
                    {
                        File.Delete(fname);
                        permissions.Delete = Yes;
                    }
                    catch (Exception)
                    {
                        //ignore
                    }
                }
            }
            
            return permissions;
        }

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
    }
}