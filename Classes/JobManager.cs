using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace ScriptRunner
{
    public class JobManager : IDisposable
    {
        public ObservableCollection<RemoteJob> Jobs { get; } = new ObservableCollection<RemoteJob>();
        private CancellationTokenSource _cts = new CancellationTokenSource();

        public JobManager()
        {
            StartPolling();
        }

        public void AddJob(RemoteJob job)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                Jobs.Add(job);
                // Select the newly added job in the ListView
                var mainWindowz = (MainWindow)Application.Current.MainWindow;
                mainWindowz.lsv_jobs.SelectedItem = job;
            });
        }

        private void StartPolling()
        {
            Task.Run(async () =>
            {
                while (!_cts.Token.IsCancellationRequested)
                {
                    foreach (var job in Jobs.ToList())
                    {
                        if (job.Status == "Running" && job.ProcessId > 0)
                        {
                            bool isRunning = false;
                            try
                            {
                                isRunning = ((MainWindow)Application.Current.MainWindow)
                                    .IsRemoteProcessRunning(job);
                            }
                            catch
                            {
                                /* handle/log as needed */
                            }

                            if (!isRunning)
                            {
                                Application.Current.Dispatcher.Invoke(() =>
                                {
                                    job.Status = "Completed";
                                });
                            }
                        }
                    }

                    await Task.Delay(2000, _cts.Token);
                }
            }, _cts.Token);
        }

        public void Dispose()
        {
            _cts.Cancel();
        }
    }
}
