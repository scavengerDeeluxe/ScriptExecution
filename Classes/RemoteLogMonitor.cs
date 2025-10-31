using System;
using System.IO;
using System.Security;
using System.Threading;
using System.Threading.Tasks;

namespace ScriptRunner
{
    /// <summary>
    /// A class that monitors remote log files using polling instead of FileSystemWatcher
    /// </summary>
    public class RemoteLogMonitor : IDisposable
    {
        private readonly string _logPath;
        private readonly string _username;
        private readonly string _domain;
        private readonly SecureString _password;
        private readonly CancellationToken _cancellationToken;
        private readonly Action<string> _onLogChanged;
        private readonly Timer _timer;
        private DateTime _lastWriteTime = DateTime.MinValue;
        private long _lastFileSize = -1;

        public RemoteLogMonitor(string logPath, string username, string domain, SecureString password, 
                               CancellationToken cancellationToken, Action<string> onLogChanged)
        {
            _logPath = logPath;
            _username = username;
            _domain = domain;
            _password = password;
            _cancellationToken = cancellationToken;
            _onLogChanged = onLogChanged;
            
            // Create a timer that checks the file every second
            _timer = new Timer(CheckForChanges, null, TimeSpan.Zero, TimeSpan.FromSeconds(1));
        }

        private void CheckForChanges(object state)
        {
            if (_cancellationToken.IsCancellationRequested)
            {
                _timer.Change(Timeout.Infinite, Timeout.Infinite);
                return;
            }

            try
            {
                ImpersonationHelper.RunAs(_username, _domain, _password, () =>
                {
                    if (!File.Exists(_logPath))
                        return;

                    var fileInfo = new FileInfo(_logPath);
                    bool hasChanged = false;
                    
                    // Check if file has been modified since last check
                    if (fileInfo.LastWriteTime > _lastWriteTime || fileInfo.Length != _lastFileSize)
                    {
                        _lastWriteTime = fileInfo.LastWriteTime;
                        _lastFileSize = fileInfo.Length;
                        hasChanged = true;
                    }

                    if (hasChanged)
                    {
                        try
                        {
                            // Read the file content
                            string content = File.ReadAllText(_logPath);
                            // Invoke the callback
                            _onLogChanged(content);
                        }
                        catch (IOException)
                        {
                            // File might be locked, will try again on next timer tick
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                // Log error but don't crash the timer
                _onLogChanged($"?? Error monitoring log file: {ex.Message}");
            }
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}