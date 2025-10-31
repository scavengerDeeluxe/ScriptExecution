using System;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Security;

namespace ScriptRunner
{
    public class RemoteScriptservice
    {
        private readonly string _username;
        private readonly SecureString _password;
        private readonly string _domain;
        private readonly ConnectionOptions _options;
        
        public RemoteScriptservice(string username, SecureString password, string domain, ConnectionOptions options)
        {
            _username = username;
            _password = password;
            _domain = domain;
            _options = options;
        }

        public bool CopyScriptToRemote(RemoteJob job)
        {
            try
            {
                string scriptToCopy = job.remotePath;
                System.Diagnostics.Debug.WriteLine($"Attempting to copy script to: {scriptToCopy}");
                System.Diagnostics.Debug.WriteLine($"Script content length: {job.Script?.Length ?? 0}");

                job.Log += $"📤 Beginning script copy operation...\n";
                job.Log += $"📁 Target path: {scriptToCopy}\n";
                job.Log += $"📊 Script size: {job.Script?.Length ?? 0} characters\n";
                string remoteTempPath = job.TargetDir;

                ImpersonationHelper.RunAs(
                    username: _username,
                    domain: _domain,
                    securePwd: _password,
                    action: () =>
                    {
                        job.Log += $"🔑 Running under impersonated credentials: {_domain}\\{_username}\n";
                        
                        // Check if target directory exists and is accessible

                        string targetDir = System.IO.Path.GetDirectoryName(scriptToCopy);
                        if (!System.IO.Directory.Exists(targetDir))
                        {
                            job.Log += $"❌ Target directory does not exist: {targetDir}\n";
                            throw new System.IO.DirectoryNotFoundException($"Target directory not found: {targetDir}");
                        }
                        job.Log += $"✅ Target directory exists and is accessible: {targetDir}\n";
                        
                        // Write the script file
                        job.Log += $"💾 Writing script content to remote file...\n";
                        System.IO.File.WriteAllText(scriptToCopy, job.Script);
                        System.Diagnostics.Debug.WriteLine($"Script successfully written to: {scriptToCopy}");
                        job.Log += $"✅ Script file written successfully\n";
                        
                        // Verify the file was actually created and has content
                        if (!System.IO.File.Exists(scriptToCopy))
                        {
                            job.Log += $"❌ Verification failed: Script file was not created at target location\n";
                            throw new System.IO.IOException($"Script file was not created at: {scriptToCopy}");
                        }
                        
                        var fileInfo = new System.IO.FileInfo(scriptToCopy);
                        if (fileInfo.Length == 0)
                        {
                            job.Log += $"❌ Verification failed: Script file is empty (0 bytes)\n";
                            throw new System.IO.IOException($"Script file is empty at: {scriptToCopy}");
                        }
                        
                        job.Log += $"✅ Verification passed: File size is {fileInfo.Length} bytes\n";
                        job.Log += $"📅 File created at: {fileInfo.CreationTime}\n";
                    }
                );
                
                job.Log += $"🎉 Script copy operation completed successfully!\n";
                return true;
            }
            catch (UnauthorizedAccessException ex)
            {
                System.Diagnostics.Debug.WriteLine($"Access denied copying script: {ex.Message}");
                job.Status = $"Access denied copying script";
                job.Log += $"🔒 Access denied: {ex.Message}\n";
                job.Log += $"💡 Check if user {_domain}\\{_username} has write access to the target\n";
                return false;
            }
            catch (System.IO.DirectoryNotFoundException ex)
            {
                System.Diagnostics.Debug.WriteLine($"Directory not found: {ex.Message}");
                job.Status = $"Target directory not found";
                job.Log += $"📁 Directory not found: {ex.Message}\n";
                job.Log += $"💡 Check if the target computer is accessible and the path exists\n";
                return false;
            }
            catch (System.Net.NetworkInformation.PingException ex)
            {
                System.Diagnostics.Debug.WriteLine($"Network error: {ex.Message}");
                job.Status = $"Network error";
                job.Log += $"🌐 Network error: {ex.Message}\n";
                job.Log += $"💡 Check network connectivity to target computer\n";
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error copying script: {ex.Message}");
                job.Status = $"Error copying script: {ex.Message}";
                job.Log += $"❌ Unexpected error during copy: {ex.Message}\n";
                job.Log += $"📋 Exception type: {ex.GetType().Name}\n";
                if (ex.InnerException != null)
                {
                    job.Log += $"🔍 Inner exception: {ex.InnerException.Message}\n";
                }
                return false;
            }
        }

        public void RunRemoteScript(RemoteJob job, ConnectionOptions wmiOptions)
        {
            try
            {
                string scriptPath = job.remotePath;
                string logPath = job.LogPath;
                string command = $"cmd /c powershell.exe -ExecutionPolicy Bypass -NoProfile -File \"{scriptPath}\" > \"{logPath}\" 2>&1";
                
                System.Diagnostics.Debug.WriteLine($"Attempting to run remote script: {command}");
                job.Log += $"🔄 Starting remote script execution...\n";
                job.Log += $"📜 Command: {command}\n";
                job.Log += $"📂 Script path: {scriptPath}\n";
                job.Log += $"📝 Log path: {logPath}\n";
                
                ImpersonationHelper.RunAs(
                    username: _username,
                    domain: _domain,
                    securePwd: _password,
                    action: () =>
                    {
                        var scope = new ManagementScope($"\\\\{job.TargetPC}\\root\\cimv2", wmiOptions);
                        
                        System.Diagnostics.Debug.WriteLine($"Connecting to WMI scope: \\\\{job.TargetPC}\\root\\cimv2");
                        job.Log += $"🔗 Connecting to WMI scope: \\\\{job.TargetPC}\\root\\cimv2\n";
                        
                        scope.Connect();
                        job.Log += $"✅ WMI connection established successfully\n";

                        ManagementClass processClass = new ManagementClass(scope, new ManagementPath("Win32_Process"), null);
                        ManagementBaseObject inParams = processClass.GetMethodParameters("Create");
                        inParams["CommandLine"] = command;
                        
                        job.Log += $"⚙️ Invoking WMI process creation...\n";
                        ManagementBaseObject outParams = processClass.InvokeMethod("Create", inParams, null);
                        
                        // Critical: Check the WMI return value
                        int returnValue = Convert.ToInt32(outParams["ReturnValue"]);
                        job.Log += $"📊 WMI method returned code: {returnValue}\n";
                        
                        if (returnValue != 0)
                        {
                            string errorMessage = GetWmiErrorMessage(returnValue);
                            job.Status = $"WMI process creation failed: {errorMessage} (Code: {returnValue})";
                            job.Log += $"❌ WMI process creation failed: {errorMessage} (Code: {returnValue})\n";
                            System.Diagnostics.Debug.WriteLine($"WMI process creation failed: {errorMessage} (Code: {returnValue})");
                            return;
                        }
                        
                        // Only set ProcessId if process creation succeeded
                        if (outParams["ProcessId"] != null)
                        {
                            job.ProcessId = Convert.ToInt32(outParams["ProcessId"]);
                            System.Diagnostics.Debug.WriteLine($"Remote process started with PID: {job.ProcessId}");
                            job.Log += $"✅ Remote process started successfully with PID: {job.ProcessId}\n";
                            job.Status = "Running";
                        }
                        else
                        {
                            job.Status = "Process creation unclear - no ProcessId returned";
                            job.Log += $"⚠️ Process creation reported success but no ProcessId returned\n";
                            job.Log += $"⚠️ This may indicate the process started but exited immediately\n";
                        }
                    }
                );
            }
            catch (Exception ex)
            {
                job.Status = $"Error running remote script: {ex.Message}";
                job.Log += $"❌ Exception during remote script execution: {ex.Message}\n";
                job.Log += $"📋 Exception type: {ex.GetType().Name}\n";
                if (ex.InnerException != null)
                {
                    job.Log += $"🔍 Inner exception: {ex.InnerException.Message}\n";
                }
                System.Diagnostics.Debug.WriteLine($"Error running remote script: {ex.Message}");
            }
        }

        private string GetWmiErrorMessage(int returnValue)
        {
            switch (returnValue)
            {
                case 0: return "Successful completion";
                case 2: return "Access denied - Check user permissions";
                case 3: return "Insufficient privilege - User needs admin rights";
                case 8: return "Unknown failure - Check target machine availability";
                case 9: return "Path not found - Check script path";
                case 21: return "Invalid parameter - Check command syntax";
                case 1326: return "Logon failure - Check credentials";
                default: return $"Unknown error code {returnValue}";
            }
        }

        public bool IsRemoteProcessRunning(RemoteJob job)
        {
            try
            {
                if (job.ProcessId <= 0)
                {
                    System.Diagnostics.Debug.WriteLine("No valid ProcessId to check");
                    return false;
                }
                
                var scope = new ManagementScope($"\\\\{job.TargetPC}\\root\\cimv2", _options);
                scope.Connect();
                var query = new ObjectQuery($"SELECT * FROM Win32_Process WHERE ProcessId = {job.ProcessId}");
                using (var searcher = new ManagementObjectSearcher(scope, query))
                {
                    using (var results = searcher.Get())
                    {
                        bool isRunning = results.Count > 0;
                        System.Diagnostics.Debug.WriteLine($"Process {job.ProcessId} running status: {isRunning}");
                        return isRunning;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error checking process status: {ex.Message}");
                // If we can't check the process status, assume it's no longer running
                return false;
            }
        }

        /// <summary>
        /// Get detailed information about a remote process
        /// </summary>
        public string GetRemoteProcessInfo(RemoteJob job)
        {
            try
            {
                if (job.ProcessId <= 0)
                {
                    return "No valid ProcessId to query";
                }
                
                var scope = new ManagementScope($"\\\\{job.TargetPC}\\root\\cimv2", _options);
                scope.Connect();
                var query = new ObjectQuery($"SELECT * FROM Win32_Process WHERE ProcessId = {job.ProcessId}");
                using (var searcher = new ManagementObjectSearcher(scope, query))
                {
                    using (var results = searcher.Get())
                    {
                        if (results.Count == 0)
                        {
                            return $"Process {job.ProcessId} not found (may have exited)";
                        }
                        
                        foreach (ManagementObject process in results)
                        {
                            string name = process["Name"]?.ToString() ?? "Unknown";
                            string commandLine = process["CommandLine"]?.ToString() ?? "Unknown";
                            string creationDate = process["CreationDate"]?.ToString() ?? "Unknown";
                            
                            return $"Process {job.ProcessId}: {name}\nCommand: {commandLine}\nStarted: {creationDate}";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return $"Error getting process info: {ex.Message}";
            }
            
            return "No process information available";
        }

        [DllImport("mpr.dll", CharSet = CharSet.Unicode)]
        private static extern int WNetAddConnection2(ref NETRESOURCE netResource, string password, string username, int flags);

        [DllImport("mpr.dll", CharSet = CharSet.Unicode)]
        private static extern int WNetCancelConnection2(string name, int flags, bool force);

        [StructLayout(LayoutKind.Sequential)]
        public struct NETRESOURCE
        {
            public int dwScope;
            public int dwType;
            public int dwDisplayType;
            public int dwUsage;
            public string lpLocalName;
            public string lpRemoteName;
            public string lpComment;
            public string lpProvider;
        }
    }
}