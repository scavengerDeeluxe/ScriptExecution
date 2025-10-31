using LibGit2Sharp;
using Newtonsoft.Json; // Add at the top if not present
using NuGet.Protocol.Core.Types;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Management;
using System.Management.Automation;
using System.Net;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Xml;
using System.Xml.Linq;
using MessageBox = System.Windows.MessageBox;
using Path = System.IO.Path;
using Repository = LibGit2Sharp.Repository;

namespace ScriptRunner
{
    public partial class MainWindow : Window
    {
        string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

        private JobManager jobManager;
        private RemoteScriptservice remoteService;
        private ScriptRatingService ratingService;
        private FileSystemItem currentSelectedScript;
        // Mapping of remedy types to their corresponding criteria lists
        private Dictionary<string, List<ComboBoxPairs>> _critDict;
        public MainWindow()
        {
            InitializeComponent();

            // Initialize rating service
            ratingService = new ScriptRatingService();

            string path = appDataPath + "\\ScriptRunner\\Scripts";
            string userscripts = Script.Properties.Settings.Default.UserScripts;
            // Initialize the criteria combo box
            cmb_crit.ItemsSource = new List<ComboBoxPairs>();

            // Force editor colors after initialization
            this.Loaded += MainWindow_LoadedForColors;

            if (Directory.Exists(path + "\\userScripts"))
            {

            }
            else
            {

                if (!Directory.Exists(userscripts))
                {
                    try
                    {
                        Directory.CreateDirectory(userscripts);
                    }
                    catch
                    {
                        System.Windows.MessageBox.Show("Failed to create user scripts directory. Please check the path in settings.");

                    }
                    if (Script.Properties.Settings.Default.UserScripts.Trim() != null)
                    {
                        SymbolicLinkCreator.CreateLink(path + "\\userScripts", userscripts, true);
                    }
                    try
                    {
                        Directory.CreateDirectory(path + "\\userScripts");
                    }
                    catch (Exception ex)
                    {
                        System.Windows.MessageBox.Show($"Failed to create user scripts directory: {ex.Message}");
                    }



                }

            }

            _allItems = LoadPs1DirectoryTree(path);
            //  treeViewScripts.Items.Clear();
            //    treeViewScripts.ItemsSource = _allItems;
            LoadMonokaiSodaTheme();
            jobManager = new JobManager();
            lsv_jobs.ItemsSource = jobManager.Jobs;
            Script.Properties.Settings.Default.DarkTheme = true;
            // Load values
            string name = Script.Properties.Settings.Default.Username;
            bool dark = Script.Properties.Settings.Default.DarkTheme;
            SetTheme(dark);
            txtuser.Text = Script.Properties.Settings.Default.Username;
            txt_domain.Text = Script.Properties.Settings.Default.Domain;
            Script.Properties.Settings.Default.Save();
            // Populate Remedy type combo box
            var typeList = new List<ComboBoxPairs>
            {
                new ComboBoxPairs("Asset", "AST%3AComputerSystem"),
                new ComboBoxPairs("Incident", "HPD%3AHelp+Desk"),
                new ComboBoxPairs("Work Order", "WOI%3AWorkOrder"),
                new ComboBoxPairs("Changes", "CHG%3AInfrastructure+Change"),
                new ComboBoxPairs("Tasks", "TMS%3ATask"),
                new ComboBoxPairs("PhoneAsset", "AST%3AComputerSystem") // separate criteria for phone assets
            };
            cmb_type.DisplayMemberPath = "critName";
            cmb_type.SelectedValuePath = "critValue";
            cmb_type.ItemsSource = typeList;
            cmb_type.SelectedIndex = 0;
            // Prepare criteria lists for each type
            // Asset
            var assetList = new List<ComboBoxPairs>
            {
                new ComboBoxPairs("Tag_Number", "260100004"),
                new ComboBoxPairs("Owner_name", "301002900"),
                new ComboBoxPairs("CI Name*", "200000020"),
                new ComboBoxPairs("Company", "1000000001"),
                new ComboBoxPairs("Serial_Number", "200000001"),
                new ComboBoxPairs("Last Logon", "2000005003"),
                new ComboBoxPairs("Top Console User", "2000005001"),
                new ComboBoxPairs("Accounting_Code", "260100001"),
                new ComboBoxPairs("Building", "260000001"),
                new ComboBoxPairs("Category", "200000003"),
                new ComboBoxPairs("Part Number", "200000013"),
                new ComboBoxPairs("Cost_Center", "260100007"),
                new ComboBoxPairs("CreatedByLoginName", "270000670"),
                new ComboBoxPairs("Domain", "260140117"),
                new ComboBoxPairs("Floor", "260000004"),
                new ComboBoxPairs("Instance_Id", "179"),
                new ComboBoxPairs("Inventory_Name", "304386891"),
                new ComboBoxPairs("Invoice_Number", "260100009"),
                new ComboBoxPairs("Manufacturer", "240001003"),
                new ComboBoxPairs("Manufacturer_Name", "240001003"),
                new ComboBoxPairs("Manufacturer_OS", "270000570"),
                new ComboBoxPairs("Model_Number", "240001002"),
                new ComboBoxPairs("Owner_contact", "301002800"),
                new ComboBoxPairs("ReconciliationIdentity", "400129200"),
                new ComboBoxPairs("Requisition_ID", "263000017"),
                new ComboBoxPairs("Room", "260000005"),
                new ComboBoxPairs("SupplierName", "240001008"),
                new ComboBoxPairs("System_Role", "260000002"),
                new ComboBoxPairs("SystemType", "301016700"),
                new ComboBoxPairs("Type", "200000004"),
            };
            // Incident
            var incidentList = new List<ComboBoxPairs>
            {
                new ComboBoxPairs("Incident Number", "1000000161"),
                new ComboBoxPairs("Request Assignee", "1000003230"),
                new ComboBoxPairs("Summary", "1000000000"),
                new ComboBoxPairs("Customer Company", "1000003299"),
                new ComboBoxPairs("Submitter", "2"),
                new ComboBoxPairs("Request Assignee", "1000000422"),
                new ComboBoxPairs("Summary", "1000000217"),
                new ComboBoxPairs("Customer Company", "1000000218"),
            };
            // Work Order
            var workOrderList = new List<ComboBoxPairs>
            {
                new ComboBoxPairs("Work Order ID", "1000000182"),
                new ComboBoxPairs("Request Assignee", "1000003230"),
                new ComboBoxPairs("Summary", "1000000000"),
                new ComboBoxPairs("Customer Company", "1000003299"),
                new ComboBoxPairs("Submitter", "2"),
            };
            // Changes
            var changeList = new List<ComboBoxPairs>
            {
                new ComboBoxPairs("ChangeID", "1000000182"),
                new ComboBoxPairs("Request_Assignee", "1000003230"),
                new ComboBoxPairs("Customer_Company", "1000003299"),
            };
            // Tasks
            var taskList = new List<ComboBoxPairs>
            {
                new ComboBoxPairs("TaskID", "1"),
            };
            // Phone Asset
            var phoneAssetList = new List<ComboBoxPairs>
            {
                new ComboBoxPairs("IMEI", "2000005011"),
                new ComboBoxPairs("ICCID", "2000005017"),
                new ComboBoxPairs("Phone Number", "2000005019"),
            };
            // Map criteria lists by type name
            _critDict = new Dictionary<string, List<ComboBoxPairs>>
            {
                { "Asset", assetList },
                { "Incident", incidentList },
                { "Work Order", workOrderList },
                { "Changes", changeList },
                { "Tasks", taskList },
                { "PhoneAsset", phoneAssetList },
            };
        }
        public class SymbolicLinkCreator
        {
            [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
            static extern bool CreateSymbolicLink(string lpSymlinkFileName, string lpTargetFileName, int dwFlags);

            // 0x0 = file, 0x1 = directory
            private const int SYMBOLIC_LINK_FLAG_FILE = 0x0;
            private const int SYMBOLIC_LINK_FLAG_DIRECTORY = 0x1;

            public static bool CreateLink(string linkPath, string targetPath, bool isDirectory)
            {
                int flags = isDirectory ? SYMBOLIC_LINK_FLAG_DIRECTORY : SYMBOLIC_LINK_FLAG_FILE;
                bool result = CreateSymbolicLink(linkPath, targetPath, flags);
                if (!result)
                {
                    int error = Marshal.GetLastWin32Error();
                    Console.WriteLine($"Failed to create symlink. Error {error}: {new System.ComponentModel.Win32Exception(error).Message}");
                }
                return result;
            }
        }
        public ObservableCollection<FileSystemItem> LoadPs1DirectoryTree(string rootPath)
        {
            var items = new ObservableCollection<FileSystemItem>();

            try
            {
                // Add any ps1 files directly under the root path first
                foreach (var ps1File in Directory.GetFiles(rootPath, "*.ps1"))
                {
                    items.Add(new FileSystemItem
                    {
                        Name = Path.GetFileName(ps1File),
                        FullPath = ps1File,
                        IsDirectory = false
                    });
                }

                // Then process directories
                foreach (var dir in Directory.GetDirectories(rootPath))
                {
                    var dirItem = new FileSystemItem
                    {
                        Name = System.IO.Path.GetFileName(dir),
                        FullPath = dir,
                        IsDirectory = true
                    };

                    // Add .ps1 children from this directory
                    foreach (var ps1File in Directory.GetFiles(dir, "*.ps1"))
                    {
                        dirItem.Children.Add(new FileSystemItem
                        {
                            Name = System.IO.Path.GetFileName(ps1File),
                            FullPath = ps1File,
                            IsDirectory = false
                        });
                    }

                    // Recurse into subdirectories
                    var childDirs = LoadPs1DirectoryTree(dir);
                    foreach (var child in childDirs)
                        dirItem.Children.Add(child);

                    // Only add directories that have children (PS1 files or subdirectories with PS1 files)
                    if (dirItem.Children.Count > 0)
                    {
                        items.Add(dirItem);
                    }
                }

                // Load ratings for all items
                if (ratingService != null)
                {
                    ratingService.LoadRatingsForItems(items);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error loading directory: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Error loading directory {rootPath}: {ex.Message}");
            }

            return items;
        }
        private void treeViewScripts_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (treeViewScripts.SelectedItem is FileSystemItem item && !item.IsDirectory)
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine($"Attempting to load script: {item.FullPath}");

                    if (!System.IO.File.Exists(item.FullPath))
                    {
                        System.Windows.MessageBox.Show($"Script file not found: {item.FullPath}");
                        return;
                    }

                    string scriptText = System.IO.File.ReadAllText(item.FullPath);
                    System.Diagnostics.Debug.WriteLine($"Script loaded, length: {scriptText?.Length ?? 0}");

                    if (string.IsNullOrEmpty(scriptText))
                    {
                        System.Windows.MessageBox.Show($"Script file is empty: {item.FullPath}");
                        return;
                    }

                    // Load script into TextBox
                    LoadScriptAsPlainText(scriptText);

                    // Force refresh of the editor
                    editorscript.Focus();

                    System.Diagnostics.Debug.WriteLine($"Script loaded into editor successfully");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error loading script: {ex.Message}");
                    System.Windows.MessageBox.Show($"Failed to load script: {ex.Message}");
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"No valid script selected or item is directory");
                if (treeViewScripts.SelectedItem == null)
                    System.Diagnostics.Debug.WriteLine("SelectedItem is null");
                else if (treeViewScripts.SelectedItem is FileSystemItem dirItem && dirItem.IsDirectory)
                    System.Diagnostics.Debug.WriteLine("Selected item is a directory");
            }
        }
        private static string EnsureUncPrefix(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return path;
            if (!path.StartsWith(@"\\"))
                return @"\\" + path.TrimStart('\\');
            return path;
        }
        private async void Button_Click_1(object sender, RoutedEventArgs e)
        {
            // Fix for CS1501: No overload for method 'ClearValue' takes 0 arguments
            // The `ClearValue` method requires a DependencyProperty as an argument. 
            // Assuming `editorLogs` is a WPF control, the correct usage is to pass the appropriate DependencyProperty.
            editorLogs.ClearValue(System.Windows.Controls.TextBox.TextProperty);
            string pcTarget = txt_Target.Text.ToLower().Trim();

            // Get text from RichTextBox
            var scriptText = editorscript.Text.Trim();

            if (string.IsNullOrWhiteSpace(scriptText))
            {
                System.Windows.MessageBox.Show("No script to run.");
                return;
            }

            // Now build the remote/script & log paths off of that:

            SaveHistory(scriptText);
            string thisDate = DateTime.Now.ToString("yyyyMMddHHmmss");
            string remoteFileName = "temppowershellrunner" + thisDate + ".ps1";
            string remoteLog = "pslogs" + thisDate + ".log";

            var job = new RemoteJob
            {
                Name = remoteFileName,
                Script = scriptText,
                ScriptName = ((FileSystemItem)treeViewScripts.SelectedItem)?.Name ?? "Untitled Script",
                Status = "Initializing",
                Cancellation = new CancellationTokenSource(),
                DateTime = thisDate,
                TargetPC = pcTarget,
                TargetShare = EnsureUncPrefix($@"{pcTarget}\c$"),
                TargetDir = EnsureUncPrefix($@"{pcTarget}\c$\Windows\Temp"),
                LogPath = EnsureUncPrefix($@"{pcTarget}\c$\Windows\Temp\{remoteLog}"),
                remotePath = EnsureUncPrefix($@"{pcTarget}\c$\Windows\Temp\{remoteFileName}"),
                Log = $"🔄 Starting script execution for target: {pcTarget}\n"
            };

            //  Jobs.Add(job);
            jobManager.AddJob(job);

            await Task.Run(() =>
            {
                try
                {
                    // Update status and log
                    Dispatcher.Invoke(() =>
                    {
                        job.Status = "Preparing credentials";
                        job.Log += "🔑 Preparing credentials for remote execution...\n";
                    });

                    string plainPwd = SecureStringToString(_savedSecurePassword);

                    ConnectionOptions _options = new ConnectionOptions()
                    {
                        Username = _savedUsername,
                        Password = plainPwd,
                        Authority = $"ntlmdomain:{_savedDomain}",
                        Impersonation = ImpersonationLevel.Impersonate,
                        Authentication = AuthenticationLevel.PacketPrivacy
                    };

                    Dispatcher.Invoke(() =>
                    {
                        job.Status = "Creating remote service";
                        job.Log += "🔧 Initializing remote service...\n";
                    });

                    //      RemoteScriptservice ser = new RemoteScriptservice(_savedUsername, _savedSecurePassword, _savedDomain, _options);
                    try
                    {
                        remoteService = new RemoteScriptservice(
                            username: _savedUsername, // e.g. "myuser"
                            password: _savedSecurePassword,     // plain text password
                            domain: _savedDomain,
                            options: _options
                        );

                        Dispatcher.Invoke(() =>
                        {
                            job.Status = "Copying script to target";
                        });

                        if (remoteService == null || !remoteService.CopyScriptToRemote(job))
                        {
                            Dispatcher.Invoke(() =>
                            {
                                // show the real share you tried
                                Debug.WriteLine($"→ UNC: {job.TargetShare}");
                                // show the Windows network API return code
                                job.Status = "Error copying script";
                                job.Log += $"❌ Failed to copy script to {job.TargetPC}\n";
                                job.Log += $"📁 Attempted path: {job.TargetShare}\n";
                            });
                            return;
                        }

                        Dispatcher.Invoke(() =>
                        {
                            job.Status = "Script copied - starting execution";
                        });

                        remoteService.RunRemoteScript(job, _options);

                        // Wait a moment to allow process to start
                        Thread.Sleep(1000);

                        // Check if process actually started
                        if (job.ProcessId <= 0)
                        {
                            Dispatcher.Invoke(() =>
                            {
                                job.Status = "Failed to start remote process";
                                job.Log += $"❌ Remote process failed to start or no ProcessId returned\n";
                            });
                            return;
                        }

                        Dispatcher.Invoke(() =>
                        {
                            job.Status = "Running";
                            job.Log += $"⏳ Script is now running remotely (PID: {job.ProcessId})\n";
                            job.Log += $"📝 Monitoring log file: {job.LogPath}\n";
                        });

                        // Create a better log monitor using polling instead of FileSystemWatcher
                        var logMonitor = new RemoteLogMonitor(
                            job.LogPath,
                            _savedUsername,
                            _savedDomain,
                            _savedSecurePassword,
                            job.Cancellation.Token,
                            (logContent) =>
                            {
                                Dispatcher.Invoke(() =>
                                {
                                    job.Log = $"🔄 Script execution log:\n{logContent}";

                                    // Update the display if this is the currently selected job
                                    if (lsv_jobs.SelectedItem == job)
                                    {
                                        DataTable table;
                                        if (IsStructuredData(logContent, out table))
                                        {
                                            if (DG_Logs.Items.Count > 0)
                                            {
                                                DG_Logs.Items.Clear();
                                            }
                                            if (DG_Logs.ItemsSource != null)
                                            {
                                                DG_Logs.ItemsSource = null;
                                            }
                                            DG_Logs.ItemsSource = table.DefaultView;
                                            DG_Logs.IsEnabled = true;
                                            editorLogs.IsEnabled = false;
                                            editorLogs.Document.Blocks.Clear();
                                        }
                                        else
                                        {
                                            DG_Logs.ItemsSource = null;
                                            DG_Logs.IsEnabled = false;
                                            editorLogs.IsEnabled = true;
                                            editorLogs.Document.Blocks.Clear();
                                            editorLogs.AppendText(logContent);
                                        }
                                    }
                                });
                            }
                        );

                        // Monitor the process and wait for completion
                        bool processRunning = true;
                        var processCheckInterval = TimeSpan.FromSeconds(2);
                        var nextProcessCheck = DateTime.Now.Add(processCheckInterval);

                        while (!job.Cancellation.Token.IsCancellationRequested && processRunning)
                        {
                            Thread.Sleep(500);

                            // Update log display if this is the currently selected job
                            Dispatcher.Invoke(() =>
                            {
                                if (lsv_jobs.SelectedItem == job)
                                {
                                    editorLogs.Document.Blocks.Clear();
                                    editorLogs.AppendText(job.Log);
                                }
                            });

                            // Check process status every few seconds
                            if (DateTime.Now >= nextProcessCheck)
                            {
                                processRunning = remoteService.IsRemoteProcessRunning(job);
                                nextProcessCheck = DateTime.Now.Add(processCheckInterval);

                                if (!processRunning)
                                {
                                    Dispatcher.Invoke(() =>
                                    {
                                        job.Status = "Process completed";
                                        job.Log += $"✅ Remote process has completed execution\n";

                                        // Final update if this is the selected job
                                        if (lsv_jobs.SelectedItem == job)
                                        {
                                            editorLogs.Document.Blocks.Clear();
                                            editorLogs.AppendText(job.Log);
                                        }
                                    });
                                    break;
                                }
                            }
                        }

                        logMonitor.Dispose();

                        if (job.Cancellation.Token.IsCancellationRequested)
                        {
                            Dispatcher.Invoke(() =>
                            {
                                job.Status = "Cancelled by user";
                                job.Log += $"🛑 Script execution was cancelled by user\n";
                            });
                        }
                        else if (!processRunning)
                        {
                            Dispatcher.Invoke(() =>
                            {
                                job.Status = "Completed";
                                job.Log += $"🎉 Script execution completed successfully\n";
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        Dispatcher.Invoke(() =>
                        {
                            job.Status = "Error: " + ex.Message;
                            job.Log += $"❌ Error during script execution: {ex.Message}\n";
                            job.Log += $"📋 Full error details: {ex}\n";
                        });
                    }
                }
                catch (Exception outerEx)
                {
                    Dispatcher.Invoke(() =>
                    {
                        job.Status = "Critical Error: " + outerEx.Message;
                        job.Log += $"💥 Critical error in script runner: {outerEx.Message}\n";
                    });
                }

            });

        }



        bool IsStructuredData(string text, out DataTable dataTable)
        {
            {
                dataTable = new DataTable();

                if (string.IsNullOrWhiteSpace(text))
                    return false;

                var lines = text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                if (lines.Length < 2)
                    return false; // Must have at least header + data

                string[] delimiterCandidates = { "\t", ",", "|", ";" };
                string detectedDelimiter = null;

                foreach (var delimiter in delimiterCandidates)
                {
                    var firstLineParts = lines[0].Split(new[] { delimiter }, StringSplitOptions.None);
                    if (firstLineParts.Length > 1)
                    {
                        detectedDelimiter = delimiter;
                        break;
                    }
                }

                // Fallback: try splitting on multiple spaces
                if (detectedDelimiter == null)
                {
                    detectedDelimiter = "  ";
                }

                var columnCount = lines[0].Split(new[] { detectedDelimiter }, StringSplitOptions.RemoveEmptyEntries).Length;

                foreach (var line in lines)
                {
                    var columns = line.Split(new[] { detectedDelimiter }, StringSplitOptions.RemoveEmptyEntries);
                    if (columns.Length != columnCount)
                        return false; // Inconsistent row lengths → not structured
                }

                // Create columns
                for (int i = 0; i < columnCount; i++)
                {
                    dataTable.Columns.Add($"Column{i + 1}");
                }

                // Populate rows
                foreach (var line in lines)
                {
                    var cells = line.Split(new[] { detectedDelimiter }, StringSplitOptions.None);
                    if (cells.Length > dataTable.Columns.Count)
                        cells = cells.Take(dataTable.Columns.Count).ToArray(); // Adjust to match column count
                    dataTable.Rows.Add(cells);
                }

                return true;
            }
        }
        private LogShow _logShowWindow;
        private void OpenLogsPopou(object sender, RoutedEventArgs e)
        {

            var theseLogs = new System.Windows.Documents.TextRange(editorLogs.Document.ContentStart, editorLogs.Document.ContentEnd).Text;

            if (_logShowWindow == null || !_logShowWindow.IsVisible)
            {
                _logShowWindow = new LogShow(theseLogs);
                _logShowWindow.Owner = this;
                _logShowWindow.Show();
            }
            else
            {
                _logShowWindow.UpdateLog(theseLogs);
                _logShowWindow.Activate();
            }

        }
        private readonly ConnectionOptions _options;
        private void lb_jobs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lsv_jobs.SelectedItem is RemoteJob job)
            {
                dg_logs.IsEnabled = false;
                // Fallback to plain text display
                editorLogs.IsEnabled = true;
                editorLogs.Document.Blocks.Clear();
                editorLogs.AppendText(job.Log);




            }
        }
        private void CancelJob_Click(object sender, RoutedEventArgs e)
        {
            if (lsv_jobs.SelectedItem is RemoteJob job)
            {
                job.Cancellation?.Cancel();
            }
        }
        private void SaveHistory(string script)
        {
            try
            {
                var dir = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ScriptRunner");
                Directory.CreateDirectory(dir);
                var file = System.IO.Path.Combine(dir, "history.log");
                File.AppendAllText(file, $"{DateTime.Now:u} - {script}{Environment.NewLine}");
            }
            catch (Exception ex)
            {
                // e.g. Debug.WriteLine or append to editorLogs
                Dispatcher.Invoke(() => editorLogs.AppendText($"Watcher failed: {ex}\n"));
            }
        }
        private void Savescript_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.SaveFileDialog();
            dlg.Filter = "PowerShell script|*.ps1|All Files|*.*";
            if (dlg.ShowDialog() == true)
            {
                // Get text from RichTextBox
                var scriptText = editorscript.Text.Trim();
                File.WriteAllText(dlg.FileName, scriptText);
            }
        }
        private async void TestConnection_Click(object sender, RoutedEventArgs e)
        {
            string target = txt_Target.Text.Trim();
            if (string.IsNullOrWhiteSpace(target))
            {
                System.Windows.MessageBox.Show("Enter a target computer name first.");
                return;
            }

            await Task.Run(() =>
            {
                try
                {
                    using (var ping = new System.Net.NetworkInformation.Ping())
                    {
                        var reply = ping.Send(target, 2000);
                        Dispatcher.Invoke(() =>
                        {
                            System.Windows.MessageBox.Show(reply.Status == System.Net.NetworkInformation.IPStatus.Success ?
                                $"Ping to {target} successful" : $"Ping to {target} failed: {reply.Status}");
                        });
                    }
                }
                catch (Exception ex)
                {
                    Dispatcher.Invoke(() => System.Windows.MessageBox.Show($"Ping failed: {ex.Message}"));
                }
            });
        }
        private void SetTheme(bool darkMode)
        {
            (Application.Current as App)?.SetTheme(darkMode);

            // Force editor colors after theme change
            if (editorscript != null)
            {
                Dispatcher.BeginInvoke(new Action(() => ForceEditorColors()), System.Windows.Threading.DispatcherPriority.Background);
            }
        }
        private void slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            SetTheme(e.NewValue >= 1);
            Script.Properties.Settings.Default.DarkTheme = e.NewValue >= 1;
            Script.Properties.Settings.Default.Save();
        }
        public bool IsRemoteProcessRunning(RemoteJob job)
        {
            if (remoteService == null) return false;
            return remoteService.IsRemoteProcessRunning(job);
        }
        private void ListBoxItem_Selected(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txt_Target.Text))
            {
                System.Windows.MessageBox.Show("Target computer name is required.");
                return;
            }

            try
            {
                string args = $"/computer={txt_Target.Text}";
                Process.Start("compmgmt.msc", args);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Failed to launch Computer Management for {txt_Target.Text}: {ex.Message}");
            }
        }
        protected override void OnClosed(EventArgs e)
        {
            jobManager.Dispose();
            base.OnClosed(e);
        }
        private void ListBoxItem_Selected_1(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txt_Target.Text))
            {
                System.Windows.MessageBox.Show("Target computer name is required.");
                return;
            }

            try
            {
                string args = $"/computer={txt_Target.Text}";
                Process.Start("eventvwr.msc", args);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Failed to launch Computer Management for {txt_Target.Text}: {ex.Message}");
            }
        }
        private void bt_Connect_Click(object sender, RoutedEventArgs e)
        { }
        private void Button_Click_2(object sender, RoutedEventArgs e)
        {

        }
        private void ClearLogs()
        {
            if (dg_logs.ItemsSource != null)
            {
                dg_logs.ItemsSource = null;
            }
            if (dg_logs.Items.Count > 0)
            {
                dg_logs.Items.Clear();
            }
            if (DG_Logs.ItemsSource != null)
            {
                DG_Logs.ItemsSource = null;
            }
            if (DG_Logs.Items.Count > 0)
            {
                DG_Logs.Items.Clear();
            }
        }
        private void lsv_jobs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lsv_jobs.SelectedItem is RemoteJob selectedJob)
            {
                ClearLogs();

                string log = selectedJob.Log ?? "No log output yet.";

                var table = ParseFormattedTable(log);
                LoadTableTextToWpfGrid(dg_logs, log);

                var cleaned = log.Trim();

                editorLogs.Document.Blocks.Clear();
                editorLogs.AppendText(cleaned);
                editorLogs.Visibility = Visibility.Visible;
                if (_logShowWindow != null && _logShowWindow.IsVisible)
                {
                    var newLog = cleaned; // get the new log text
                    _logShowWindow.UpdateLog(newLog);
                }
            }

        }
        private void LoadTableTextToWpfGrid(System.Windows.Controls.DataGrid grid, string rawText)
        {
            var table = ParseFormattedTable(rawText);

            // Convert to a list of dictionaries for dynamic binding
            var items = new List<Dictionary<string, string>>();
            foreach (DataRow row in table.Rows)
            {
                var dict = new Dictionary<string, string>();
                foreach (DataColumn col in table.Columns)
                {
                    dict[col.ColumnName] = row[col].ToString();
                }
                items.Add(dict);
            }

            grid.ItemsSource = items;
        }
        private DataTable ParseFormattedTable(string rawText)
        {
            var lines = rawText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length < 2)
                return new DataTable();

            // Assume header is first line, divider (---) is second, then data rows
            var headers = Regex.Split(lines[0].Trim(), "\\s{2,}");
            var table = new DataTable();
            foreach (var header in headers)
                table.Columns.Add(header);

            for (int i = 2; i < lines.Length; i++)
            {
                var rowParts = Regex.Split(lines[i].Trim(), "\\s{2,}");
                var row = table.NewRow();

                for (int j = 0; j < headers.Length && j < rowParts.Length; j++)
                    row[j] = rowParts[j];

                table.Rows.Add(row);
            }

            return table;
        }
        private TextBoxTraceListener textBoxListener;

        private List<WmiQuery> savedQueries = new List<WmiQuery>();
        private void LoadQueriesFromJson(string filePath)
        {
            try
            {
                var json = File.ReadAllText(filePath);
                savedQueries = JsonConvert.DeserializeObject<List<WmiQuery>>(json);
                lb_savedQueries.ItemsSource = savedQueries;
                lb_savedQueries.DisplayMemberPath = "Name";
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Failed to load queries: {ex.Message}");
            }
        }
        private IEnumerable<string> GetTargets()
        {
            var entries = txtWmiTargets.Text
                .Split(new[] { '\n', '\r', ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var entry in entries)
            {
                if (entry.Contains("/"))
                {
                    foreach (var ip in ExpandCidr(entry))
                        yield return ip;
                }
                else
                {
                    yield return entry;
                }
            }
        }
        private void InitializeAdminTools()
        {
            var tools = new ObservableCollection<AdminTools>
    {
        new AdminTools { Name = "🖥 CompMgmt", executeTarget = "compmgmt.msc", tool = "Computer Management" },
        new AdminTools { Name = "🗒 EventVwr", executeTarget = "eventvwr.msc", tool = "Event Viewer" },
        new AdminTools { Name = "📅 TaskSched", executeTarget = "taskschd.msc", tool = "Task Scheduler" },
        new AdminTools { Name = "⚙ Services", executeTarget = "services.msc", tool = "Services" }
    };

            lsv_adminTools.ItemsSource = tools;
        }
        private void Grid_Loaded(object sender, RoutedEventArgs e)
        {
            var fontsize = new List<string> { "8", "9", "10", "11", "12", "14", "16", "18", "20", "22", "24", "26", "28", "30", "32" };
        }
        private void editorscript_LostFocus(object sender, RoutedEventArgs e)
        {
            editorscript.Width = 392;
            editorscript.Height = 443;


        }
        private readonly string QueriesDir = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ScriptRunner", "WmiQueries");


        private void LoadWmiNamespaces()
        {
            var namespaces = new List<string>
    {
        "root\\cimv2",
        "root\\cimv2\\mdm",
        "root\\wmi",
        "root\\default",
        "root\\dcim\\sysman\\biosattributes",
        "root\\dell",
        "root\\Microsoft",
        "root\\ccm",
        "root\\SMS"
    };
            cb_WmiNamespace.ItemsSource = namespaces;
            if (namespaces.Count > 0)
                cb_WmiNamespace.SelectedIndex = 0;

        }
        private void cb_WmiNamespace_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cb_WmiNamespace.SelectedItem == null) return;

            string ns = cb_WmiNamespace.SelectedItem.ToString();
            string target = GetTargetz().FirstOrDefault() ?? "localhost";
            try
            {
                var scope = new ManagementScope($"\\\\{target}\\{ns}");
                // Fixing the CS1001 and CS1002 errors by completing the statement with a semicolon and assigning a value to Timeout.
                scope.Options.Timeout = new TimeSpan(0, 0, 8); // Example: setting a 30-second timeout
                scope.Connect();
                var searcher = new ManagementObjectSearcher(scope, new ObjectQuery("select * from meta_class"));
                var classes = new List<string>();
                foreach (ManagementClass c in searcher.Get())
                {
                    string name = c["__CLASS"].ToString();
                    if (!name.StartsWith("__"))
                        classes.Add(name);
                }
                classes.Sort();
                cb_WmiClass.ItemsSource = classes;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Failed to load WMI classes: {ex.Message}");
            }
        }
        private async void cb_WmiClass_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string ns = cb_WmiNamespace.SelectedItem.ToString();
            string cls = cb_WmiClass.SelectedItem.ToString();

            try
            {
                var props = await Task.Run(() =>
                {
                    var scope = new ManagementScope($@"\\localhost\{ns}");
                    scope.Options.Timeout = new TimeSpan(0, 0, 8); // Example: setting a 30-second timeout

                    scope.Connect();

                    var query = new ObjectQuery($"SELECT * FROM meta_class WHERE __CLASS = '{cls}'");
                    var searcher = new ManagementObjectSearcher(scope, query);
                    var result = searcher.Get().Cast<ManagementClass>().FirstOrDefault();
                    if (result == null) return new List<string>();
                    return result.Properties.Cast<PropertyData>().Select(p => p.Name).ToList();
                });

                lb_WmiProperties.ItemsSource = props;
            }
            catch
            {
                // Optionally handle or log errors here
            }
        }
        private void btnBuildWmiQuery_Click(object sender, RoutedEventArgs e)
        {
            if (cb_WmiClass.SelectedItem == null) return;
            var selectedProps = lb_WmiProperties.SelectedItems.Cast<string>().ToList();
            string propPart = selectedProps.Count > 0 ? string.Join(",", selectedProps) : "*";
            txt_wmiQuery.Text = $"SELECT {propPart} FROM {cb_WmiClass.SelectedItem}";
        }
        private IEnumerable<string> GetTargetz()
        {
            txtWmiTargets.AppendText(txt_Target.Text);
            return txtWmiTargets.Text.Split(new[] { '\n', '\r', ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries);
        }
        private async Task<bool> IsPortOpenAsync(string host, int port, int timeout = 1000)
        {
            return await WmiService.IsPortOpenAsync(host, port, timeout);
        }
        private async Task<ICollection> RunWmiQueryWithTimeout(string target, string ns, string query, int timeoutMs)
        {
            return await WmiService.RunWmiQueryWithTimeout(target, ns, query, timeoutMs, _wmiOptions);
        }
        private async void btnRunWmiQuery_Click(object sender, RoutedEventArgs e)
        {
            string query = txt_wmiQuery.Text.Trim();
            if (string.IsNullOrWhiteSpace(query) || cb_WmiNamespace.SelectedItem == null) return;
            string ns = cb_WmiNamespace.SelectedItem.ToString();

            var rawTargets = GetTargets().ToList();

            pb_Wmi.Minimum = 0;
            pb_Wmi.Maximum = rawTargets.Count;
            pb_Wmi.Value = 0;

            var results = new ObservableCollection<Dictionary<string, object>>();
            if (dgWmiResults.ItemsSource != null) { dgWmiResults.ItemsSource = null; }
            if (dgWmiResults.Items.Count > 0) { dgWmiResults.Items.Clear(); }

            dgWmiResults.ItemsSource = results;
            foreach (var col in dgWmiResults.Columns.ToList())
                dgWmiResults.Columns.Remove(col);

            int completed = 0;
            var columnsCreated = false;
            var columnsLock = new object();
            var semaphore = new SemaphoreSlim(5); // Limit concurrency

            wmi_logs.ClearValue(System.Windows.Controls.TextBox.TextProperty);

            await Task.WhenAll(rawTargets.Select(async target =>
            {
                await semaphore.WaitAsync();
                try
                {
                    Dispatcher.Invoke(() =>
                    {
                        wmi_logs.AppendText($"Checking port 135 on {target}...\n");
                        wmi_logs.ScrollToEnd();
                    });

                    if (!await IsPortOpenAsync(target, 135, 250))
                    {
                        Dispatcher.Invoke(() =>
                        {
                            wmi_logs.AppendText($"Port 135 closed or unreachable on {target}\n");
                            wmi_logs.ScrollToEnd();
                        });
                        return;
                    }

                    Dispatcher.Invoke(() =>
                    {
                        wmi_logs.AppendText($"Port 135 open on {target}, querying WMI...\n");
                        wmi_logs.ScrollToEnd();
                    });

                    ICollection collection = null;
                    try
                    {
                        collection = await RunWmiQueryWithTimeout(target, ns, query, 35000); // 35 second timeout
                    }
                    catch (TimeoutException)
                    {
                        Dispatcher.Invoke(() =>
                        {
                            wmi_logs.AppendText($"WMI query timed out on {target}\n");
                            wmi_logs.ScrollToEnd();
                        });
                        return;
                    }

                    foreach (ManagementObject obj in collection)
                    {
                        var dict = new Dictionary<string, object>();
                        foreach (PropertyData p in obj.Properties)
                            dict[p.Name] = obj[p.Name] ?? DBNull.Value;
                        dict["Target"] = target;

                        lock (columnsLock)
                        {
                            if (!columnsCreated)
                            {
                                Dispatcher.Invoke(() =>
                                {
                                    dgWmiResults.Columns.Clear();
                                    foreach (var key in dict.Keys)
                                        dgWmiResults.Columns.Add(new DataGridTextColumn
                                        {
                                            Header = key,
                                            Binding = new System.Windows.Data.Binding($"[{key}]")
                                        });
                                });
                                columnsCreated = true;
                            }
                        }

                        Dispatcher.Invoke(() => results.Add(dict));
                    }
                }
                catch (ManagementException mex)
                {
                    Dispatcher.Invoke(() =>
                    {
                        wmi_logs.AppendText($"WMI error on {target}: {mex.Message}\n");
                        wmi_logs.ScrollToEnd();
                    });
                }
                finally
                {
                    Dispatcher.Invoke(() =>
                    {
                        completed++;
                        pb_Wmi.Value = completed;
                    });
                    semaphore.Release();
                }
            }));

        }
        private IEnumerable<string> ExpandCidr(string cidr)
        {
            return WmiService.ExpandCidr(cidr);
        }
        private void btnLoadQuery_Click(object sender, RoutedEventArgs e)
        {
            if (lb_savedQueries.SelectedItem is WmiQuery selected)
            {

                cb_WmiNamespace.SelectedValue = selected.Namespace;
                cb_WmiClass.SelectedValue = selected.Class;
                txt_wmiQuery.Text = selected.Data;
            }

        }
        private TabItem _scriptTabItem;
        private void TabItem_RequestBringIntoView(object sender, RequestBringIntoViewEventArgs e)
        {
            if (sender is TabItem tabItem && tabItem.Content is Grid grid)
            {
                // Check if the script has already been loaded
                if (_scriptTabItem == tabItem)
                    return; // Script already loaded

                // Remember the current tab
                _scriptTabItem = tabItem;

                // Deselect all other tabs
                foreach (var item in tabControl.Items)
                {
                    if (item is TabItem ti && ti != tabItem)
                    {
                        ti.IsSelected = false;
                    }
                }

                // Load the script into the editor
                if (tabItem.Tag is string scriptPath && File.Exists(scriptPath))
                {
                    var scriptText = File.ReadAllText(scriptPath);
                    editorscript.Text = null;
                    editorscript.AppendText(scriptText);
                }
            }

            TargetGrid.Visibility = Visibility.Hidden;

        }
        private void TabItem_RequestBringIntoView_1(object sender, RequestBringIntoViewEventArgs e)
        {
            TargetGrid.Visibility = Visibility.Visible;

        }
        private void TabItem_RequestBringIntoView_2(object sender, RequestBringIntoViewEventArgs e)
        {
            TargetGrid.Visibility = Visibility.Visible;

        }
        private void lb_savedQueries_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lb_savedQueries.SelectedItem is SavedQuery selected)
            {
                txt_wmiQuery.Text = selected.Query;
            }
        }
        private ObservableCollection<FileSystemItem> _allItems; // Store the full, unfiltered tree
        private void search_TextChanged(object sender, TextChangedEventArgs e)
        {
            string filterText = search.Text?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(filterText))
            {
                // Show all files (no folders) if search is empty
                treeViewScripts.ItemsSource = FilterFilesOnly(_allItems);
            }
            else
            {
                treeViewScripts.ItemsSource = FilterTree(_allItems, filterText);
            }
        }
        private ObservableCollection<FileSystemItem> FilterFilesOnly(IEnumerable<FileSystemItem> items)
        {
            var result = new ObservableCollection<FileSystemItem>();
            foreach (var item in items)
            {
                if (item.IsDirectory && item.Children != null)
                {
                    foreach (var child in FilterFilesOnly(item.Children))
                        result.Add(child);
                }
                else if (!item.IsDirectory)
                {
                    result.Add(item);
                }
            }
            return result;
        }
        private ObservableCollection<FileSystemItem> FilterTree(IEnumerable<FileSystemItem> items, string filter)
        {
            var result = new ObservableCollection<FileSystemItem>();
            if (items == null)
                return result;

            foreach (var item in items)
            {
                if (item.IsDirectory && item.Children != null)
                {
                    var filteredChildren = FilterTree(item.Children, filter);
                    if (filteredChildren.Count > 0)
                    {
                        // Clone the folder, but only with matching children
                        var folder = new FileSystemItem
                        {
                            Name = item.Name,
                            FullPath = item.FullPath,
                            IsDirectory = true,
                            Children = filteredChildren
                        };
                        result.Add(folder);
                    }
                }
                else if (!item.IsDirectory && item.Name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    result.Add(item);
                }
            }
            return result;
        }
        private static string FormatUserName(NetworkCredential credentials)
        {
            return String.Format("{0}\\{1}", credentials.Domain, credentials.UserName);
        }
        ConnectionOptions _wmiOptions = new ConnectionOptions();
        private string _savedUsername;
        private SecureString _savedSecurePassword;
        private string _savedDomain;


        public void Button_Click(object sender, RoutedEventArgs e)
        {
            string username = txtuser.Text.Trim();
            SecureString securePassword = GetSecurePassword(pwpass.Password);

            _savedUsername = username;
            _savedSecurePassword = securePassword;
            _savedDomain = txt_domain.Text.Trim();

            if (_savedUsername != null)
            {
                Script.Properties.Settings.Default.Username = _savedUsername;
                Script.Properties.Settings.Default.Save();
            }
            if (_savedDomain != null)
            {
                Script.Properties.Settings.Default.Domain = _savedDomain;
                Script.Properties.Settings.Default.Save();
            }

            _wmiOptions = new ConnectionOptions
            {
                Username = username,
                Password = SecureStringToString(securePassword), // still needed for WMI
                EnablePrivileges = true
            };
            btn_saveCreds.IsEnabled = false;
            btn_resetcreds.IsEnabled = true;

            // store credentials for reuse
            string plainSavedPwd = SecureStringToString(_savedSecurePassword);
            remoteService = new RemoteScriptservice(_savedUsername, _savedSecurePassword, _savedDomain, _options);

            pwpass.Clear();
            tabScripts.IsEnabled = true;
            tabTasks.IsEnabled = true;
            tabWmi.IsEnabled = true;
            credConfirm.Visibility = Visibility.Visible;
            labelCredReset.Visibility = Visibility.Hidden;
        }
        private System.Security.SecureString GetSecurePassword(string password)
        {
            var securePassword = new System.Security.SecureString();
            foreach (char c in password)
            {
                securePassword.AppendChar(c);
            }
            securePassword.MakeReadOnly();
            return securePassword;
        }
        private static string SecureStringToString(SecureString ss)
        {
            var ptr = Marshal.SecureStringToGlobalAllocUnicode(ss);
            try
            {
                return Marshal.PtrToStringUni(ptr) ?? string.Empty;
            }
            finally
            {
                Marshal.ZeroFreeGlobalAllocUnicode(ptr);
            }
        }
        private void editorLogs_TextChanged(object sender, TextChangedEventArgs e)
        {
            DG_Logs.Visibility = Visibility.Collapsed;
            editorLogs.Visibility = Visibility.Visible;

            // Get the current text from the RichTextBox
            string currentText = new TextRange(editorLogs.Document.ContentStart, editorLogs.Document.ContentEnd).Text;

            // Simple duplicate prevention - if the text is exactly the same as last time, skip processing
            if (currentText.Equals(_lastEditorLogsText, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            // Update the tracking variable
            _lastEditorLogsText = currentText;
        }

        private string _lastEditorLogsText = string.Empty;

        private void DG_Logs_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            DG_Logs.Visibility = Visibility.Visible;
            editorLogs.Visibility = Visibility.Collapsed;

        }
        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            string u = _savedUsername;
            SecureString pw = _savedSecurePassword;
            string target = txt_Target.Text;
            string domain = _savedDomain;
            runPosh2(u, pw, target, domain);
        }
        public void runPosh2(string user, SecureString pass, string target, string domain)
        {
            string _secUser = user;
            string _dom = domain;
            SecureString _pw = pass;
            string _tar = target;
            PSCredential ps = new PSCredential(_secUser, _pw);

            try
            {
                Process Explorer = new Process();
                Explorer.StartInfo.FileName = "powershell.exe";
                string pslaunch = "Enter-PSSession {0} -SessionOption(New-PSSessionOption -NoMachineProfile:$true)";
                string sPS = string.Format(pslaunch, _tar);
                Explorer.StartInfo.Arguments = @"-NoExit -executionpolicy bypass  -Command " + sPS;

                if (!string.IsNullOrEmpty(_secUser))
                {
                    System.Management.Automation.PSCredential credential = new System.Management.Automation.PSCredential(_secUser, _pw);
                    string serializedCred = System.Management.Automation.PSSerializer.Serialize(credential);
                    string filename = System.IO.Path.GetTempFileName();
                    File.WriteAllText(filename, serializedCred);
                    string creds = "(Import-Clixml " + filename + ")";
                    //creds += "; rm " + filename;

                    Explorer.StartInfo.Arguments += " -Credential " + creds;
                }

                Explorer.StartInfo.WindowStyle = ProcessWindowStyle.Normal;

                Explorer.Start();
            }
            catch
            {
                Debug.WriteLine("Failed");
            }

        }
        public static System.Security.SecureString ToSecure(string current)
        {
            var secure = new System.Security.SecureString();
            foreach (var c in current.ToCharArray()) secure.AppendChar(c);
            return secure;
        }
        private void txtuser_TextChanged(object sender, TextChangedEventArgs e)
        {

        }
        private void txt_Target_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (txt_Target.Text.Length > 0)
            {
                btn_execute.IsEnabled = true;
            }
            else
            {
                btn_execute.IsEnabled = false;
            }
        }
        private void btn_resetcreds_Click(object sender, RoutedEventArgs e)
        {
            btn_saveCreds.IsEnabled = true;
            labelCredReset.Visibility = Visibility.Visible;
            credConfirm.Visibility = Visibility.Hidden;
            btn_resetcreds.IsEnabled = false;
        }
        private void lsv_adminTools_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (lsv_adminTools.SelectedItem is AdminTools tool && !string.IsNullOrWhiteSpace(tool.executeTarget))
            {
                string target = txt_Target.Text.Trim();

                if (string.IsNullOrWhiteSpace(target))
                {
                    System.Windows.MessageBox.Show("Please enter a target computer name.");
                    return;
                }
                try
                {
                    string args = tool.executeTarget + " /computer:" + target;

                    ProcessStartInfo psi = new ProcessStartInfo
                    {
                        FileName = "mmc.exe", // or any .exe you want to run
                        Arguments = args,
                        Verb = "runas", // this triggers elevation
                        UseShellExecute = true // must be true for "runas" to work
                    };

                    Process.Start(psi);
                }
                catch (System.ComponentModel.Win32Exception ex)
                {
                    MessageBox.Show("User declined elevation or there was an error.");
                    MessageBox.Show($"Details: {ex.Message}");
                }

            }
        }
        private void Button_Click_4(object sender, RoutedEventArgs e)
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog();
            System.Windows.Forms.DialogResult result = dialog.ShowDialog();

            Script.Properties.Settings.Default.UserScripts = dialog.SelectedPath;
            Script.Properties.Settings.Default.Save();
        }
        private void RemedyTab_RequestBringIntoView(object sender, RequestBringIntoViewEventArgs e)
        {

            TargetGrid.Visibility = Visibility.Visible;


        }
        private void btn_query_Click(object sender, RoutedEventArgs e)
        {
            string Target = string.Empty;
            string cmbType = cmb_type.SelectedValue?.ToString() ?? string.Empty;
            string Criteria = cmb_crit.SelectedValue?.ToString().Trim() ?? string.Empty;
            string Operator = cmb_operator.Text;
            string baseUrl = "https://mn-itservices.us.onbmc.com/arsys/forms/onbmc-s/SHR%3ALandingConsole/Default+Administrator+View/?mode=search&F304255500=" + cmbType + "&F1000000076=FormOpenNoAppList&F303647600=SearchTicketWithQual&F304255610='";

            Target = baseUrl + Criteria + "'" + Operator + "%22";

            string[] entries = (string[])RtbSelection(tb_targets.Text);
            int batchSize = (int)slide_batch.Value;
            StringBuilder finalString = new StringBuilder(Target);
            if (Criteria == "Owner_name")
            {
                Target = Target + "%25";
            }
            for (int i = 0; i < entries.Length; i++)
            {
                string entry = entries[i].Trim();
                if (entry.Length > 2)
                {
                    if (i > 0 && i % batchSize == 0)
                    {
                        OpenBrowser(finalString.ToString());
                        finalString.Clear();
                        finalString.Append(Target);
                    }
                    else
                    {
                        finalString.Append(entry + (i < entries.Length - 1 ? "%22OR'" + Criteria + "'" + Operator + "%22" : "%22"));
                    }
                }
            }

            if (finalString.Length > Target.Length)
            {
                OpenBrowser(finalString.ToString().Replace("*", "%25"));

            }
        }
        private void OpenBrowser(string url)
        {
            try
            {
                System.Diagnostics.Process.Start(new ProcessStartInfo
                {

                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch (System.ComponentModel.Win32Exception noBrowser)
            {
                if (noBrowser.ErrorCode == -2147467259)
                    System.Windows.MessageBox.Show(noBrowser.Message);
            }
            catch (System.Exception other)
            {
                System.Windows.MessageBox.Show(other.Message);
            }
        }
        private Array RtbSelection(String e)
        {
            string splitlines = e;
            List<string> strings = new List<string>(
    splitlines.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries));
            return strings.ToArray();
        }
        private void cmb_type_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Use dictionary-driven criteria mapping if available
            if (_critDict != null && cmb_type.SelectedItem is ComboBoxPairs sel)
            {
                if (_critDict.TryGetValue(sel.critName, out var list))
                {
                    cmb_crit.DisplayMemberPath = "critName";
                    cmb_crit.SelectedValuePath = "critValue";
                    cmb_crit.ItemsSource = list;
                    cmb_crit.SelectedIndex = 0;
                }
                else
                {
                    cmb_crit.ItemsSource = null;
                }
                return;
            }
            int targetItem = cmb_type.SelectedIndex;
            if (targetItem.Equals(0))
            {
                List<ComboBoxPairs> critListAsset = new List<ComboBoxPairs>();
                critListAsset.Add(new ComboBoxPairs("Tag_Number", "260100004"));
                critListAsset.Add(new ComboBoxPairs("Owner_name", "301002900"));
                critListAsset.Add(new ComboBoxPairs("CI Name*", "200000020"));
                critListAsset.Add(new ComboBoxPairs("Company", "1000000001"));
                critListAsset.Add(new ComboBoxPairs("Serial_Number", "200000001"));
                critListAsset.Add(new ComboBoxPairs("Last Logon", "2000005003"));
                critListAsset.Add(new ComboBoxPairs("Top Console User", "2000005001"));
                critListAsset.Add(new ComboBoxPairs("Accounting_Code", "260100001"));
                critListAsset.Add(new ComboBoxPairs("Building", "260000001"));
                critListAsset.Add(new ComboBoxPairs("Category", "200000003"));
                critListAsset.Add(new ComboBoxPairs("Part Number", "200000013"));
                critListAsset.Add(new ComboBoxPairs("Cost_Center", "260100007"));
                critListAsset.Add(new ComboBoxPairs("CreatedByLoginName", "270000670"));
                critListAsset.Add(new ComboBoxPairs("Domain", "260140117"));
                critListAsset.Add(new ComboBoxPairs("Floor", "260000004"));
                critListAsset.Add(new ComboBoxPairs("Instance_Id", "179"));
                critListAsset.Add(new ComboBoxPairs("Inventory_Name", "304386891"));
                critListAsset.Add(new ComboBoxPairs("Invoice_Number", "260100009"));
                critListAsset.Add(new ComboBoxPairs("Manufacturer", "240001003"));
                critListAsset.Add(new ComboBoxPairs("Manufacturer_Name", "240001003"));
                critListAsset.Add(new ComboBoxPairs("Manufacturer_OS", "270000570"));
                critListAsset.Add(new ComboBoxPairs("Model_Number", "240001002"));
                critListAsset.Add(new ComboBoxPairs("Owner_contact", "301002800"));

                critListAsset.Add(new ComboBoxPairs("ReconciliationIdentity", "400129200"));
                critListAsset.Add(new ComboBoxPairs("Requisition_ID", "263000017"));
                critListAsset.Add(new ComboBoxPairs("Room", "260000005"));
                critListAsset.Add(new ComboBoxPairs("SupplierName", "240001008"));
                critListAsset.Add(new ComboBoxPairs("System_Role", "260000002"));
                critListAsset.Add(new ComboBoxPairs("SystemType", "301016700"));
                critListAsset.Add(new ComboBoxPairs("Type", "200000004"));
                cmb_crit.DisplayMemberPath = "critName";
                cmb_crit.SelectedValuePath = "critValue";
                cmb_crit.ItemsSource = critListAsset;
                cmb_crit.SelectedIndex = 0;

            }

            if (targetItem.Equals(2))
            {

                List<ComboBoxPairs> critListWorkOrder = new List<ComboBoxPairs>();
                critListWorkOrder.Add(new ComboBoxPairs("Work Order ID", "1000000182"));
                critListWorkOrder.Add(new ComboBoxPairs("Request Assignee", "1000003230"));
                critListWorkOrder.Add(new ComboBoxPairs("Summary", "1000000000"));
                critListWorkOrder.Add(new ComboBoxPairs("Customer Company", "1000003299"));
                critListWorkOrder.Add(new ComboBoxPairs("Submitter", "2"));
                cmb_crit.DisplayMemberPath = "critName";
                cmb_crit.SelectedValuePath = "critValue";
                cmb_crit.ItemsSource = critListWorkOrder;
                cmb_crit.SelectedIndex = 0;
            }
            if (targetItem.Equals(1))
            {

                List<ComboBoxPairs> critListIncident = new List<ComboBoxPairs>();
                critListIncident.Add(new ComboBoxPairs("Incident Number", "1000000161"));
                critListIncident.Add(new ComboBoxPairs("Request Assignee", "1000003230"));
                critListIncident.Add(new ComboBoxPairs("Summary", "1000000000"));
                critListIncident.Add(new ComboBoxPairs("Customer Company", "1000003299"));
                critListIncident.Add(new ComboBoxPairs("Submitter", "2"));
                critListIncident.Add(new ComboBoxPairs("Request Assignee", "1000000422"));
                critListIncident.Add(new ComboBoxPairs("Summary", "1000000217"));
                critListIncident.Add(new ComboBoxPairs("Customer Company", "1000000218"));
                cmb_crit.DisplayMemberPath = "critName";
                cmb_crit.SelectedValuePath = "critValue";

                cmb_crit.ItemsSource = critListIncident;
                cmb_crit.SelectedIndex = 0;
            }
            if (targetItem.Equals(4))
            {

                List<ComboBoxPairs> critListTask = new List<ComboBoxPairs>();
                critListTask.Add(new ComboBoxPairs("TaskID", "1"));
                cmb_crit.DisplayMemberPath = "critName";
                cmb_crit.SelectedValuePath = "critValue";
                cmb_crit.ItemsSource = critListTask;
                cmb_crit.SelectedIndex = 0;
            }
            if (targetItem.Equals(3))
            {

                List<ComboBoxPairs> critListChange = new List<ComboBoxPairs>();
                critListChange.Add(new ComboBoxPairs("ChangeID", "1000000182"));
                critListChange.Add(new ComboBoxPairs("Request_Assignee", "1000003230"));
                critListChange.Add(new ComboBoxPairs("Customer_Company", "1000003299"));
                cmb_crit.DisplayMemberPath = "critName";
                cmb_crit.SelectedValuePath = "critValue";
                cmb_crit.ItemsSource = critListChange;
                cmb_crit.SelectedIndex = 0;
            }
            if (targetItem.Equals(5))
            {

                List<ComboBoxPairs> critListChange = new List<ComboBoxPairs>();
                critListChange.Add(new ComboBoxPairs("IMEI", "2000005011"));
                critListChange.Add(new ComboBoxPairs("ICCID", "2000005017"));
                critListChange.Add(new ComboBoxPairs("Phone Number", "2000005019"));
                cmb_crit.DisplayMemberPath = "critName";
                cmb_crit.SelectedValuePath = "critValue";
                cmb_crit.ItemsSource = critListChange;
                cmb_crit.SelectedIndex = 0;
            }
        }
        private void btn_clipboard_Click(object sender, RoutedEventArgs e)
        {
            string Operator = cmb_operator.Text;

            string Criteria = cmb_crit.SelectedValue.ToString().Trim();
            int Counter = 1;
            Array items = RtbSelection(tb_targets.Text);
            int TotalCount = items.Length;

            string newText = null;
            foreach (string entry in items)
            {
                string entryString = entry.Trim();
                if (Counter == TotalCount)
                {
                    newText += "'" + Criteria + "' " + Operator + " \"" + entryString + "\"";
                    System.Windows.Clipboard.SetText(newText);
                }
                else
                {
                    newText += "'" + Criteria + "' " + Operator + " \"" + entryString + "\" OR ";
                }
                Counter++;
            }
        }
        private void slide_batch_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            var change = slide_batch.Value;
            if (batchSizeLabel != null)
            {
                batchSizeLabel.Content = Math.Round(change);
            }
            else
            {
                // Handle the null case, e.g., log an error or initialize the label
            }
        }

        private void script_Click(object sender, RoutedEventArgs e)
        {

        }

        private void tabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void treeViewScripts_GotFocus(object sender, RoutedEventArgs e)
        {

        }

        private void treeViewScripts_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue is FileSystemItem selectedItem && !selectedItem.IsDirectory)
            {
                currentSelectedScript = selectedItem;
                ShowRatingForScript(selectedItem);
                var ratingPanel = FindName("RatingPanel") as StackPanel;
                if (ratingPanel != null)
                {
                    ratingPanel.Visibility = Visibility.Visible;
                }
            }
            else
            {
                currentSelectedScript = null;
                var ratingPanel = FindName("RatingPanel") as StackPanel;
                if (ratingPanel != null)
                {
                    ratingPanel.Visibility = Visibility.Collapsed;
                }
            }
        }

        private void Star_Click(object sender, RoutedEventArgs e)
        {
            if (currentSelectedScript == null) return;

            if (sender is Button starButton && starButton.Tag is string tagValue)
            {
                if (int.TryParse(tagValue, out int rating))
                {
                    // Update the rating for the current script
                    ratingService.SetRating(currentSelectedScript.FullPath, rating);
                    currentSelectedScript.Rating = rating;

                    // Update the visual display
                    UpdateStarDisplay(rating);

                    // Show feedback window
                    PromptForFeedback(currentSelectedScript.FullPath, rating);
                }
            }
        }

        private void PromptForFeedback(string scriptPath, int rating)
        {
            string webhookUrl = "https://prod-15.usgovtexas.logic.azure.us:443/workflows/0876cb925e2c46c89ffb068ee6846589/triggers/manual/paths/invoke?api-version=2016-06-01&sp=%2Ftriggers%2Fmanual%2Frun&sv=1.0&sig=U-AJTAoJZzDmLo87U6SWOJHuzHL4A24Wpq0BuaUxt0M";
            var feedbackWindow = new RatingFeedbackWindow(webhookUrl, scriptPath, rating)
            {
                Owner = this
            };

            feedbackWindow.ShowDialog();
        }

        private void ShowRatingForScript(FileSystemItem script)
        {
            if (script == null) return;

            int rating = ratingService.GetRating(script.FullPath);
            script.Rating = rating;
            UpdateStarDisplay(rating);
        }

        private void UpdateStarDisplay(int rating)
        {
            var starButtons = new[]
            {
                FindName("star1") as Button,
                FindName("star2") as Button,
                FindName("star3") as Button,
                FindName("star4") as Button,
                FindName("star5") as Button
            };

            for (int i = 0; i < starButtons.Length; i++)
            {
                var starButton = starButtons[i];
                if (starButton != null)
                {
                    if (i < rating)
                    {
                        starButton.Content = "★"; // Filled star
                        starButton.Foreground = new SolidColorBrush(Color.FromRgb(255, 215, 0)); // Gold color
                    }
                    else
                    {
                        starButton.Content = "☆"; // Empty star
                        starButton.Foreground = new SolidColorBrush(Colors.Gray);
                    }
                }
            }
        }


        private void OpenscriptPopout_Click_1(object sender, MouseButtonEventArgs e)
        {
            // Get text from RichTextBox
            var currentscript = editorscript.Text;

            var popup = new ScriptEditorWindow();
            popup.Owner = this;
            popup.Show(); // Non-modal: parent window remains interactive


            // If you want to update the main editor when the popout closes, you can handle the Closed event:
            popup.Closed += (s, args) =>
            {
                if (!string.IsNullOrEmpty(popup.ScriptText))
                {
                    editorscript.Text = null;
                    editorscript.AppendText(popup.ScriptText);
                }

            };
        }

        private void lbl_scriptName_MouseEnter(object sender, MouseEventArgs e)
        {
            lbl_scriptName.TextEffects = new TextEffectCollection
            {
                new TextEffect
                {
                    PositionStart = 0,
                    PositionCount = lbl_scriptName.Text.Length,
                    Foreground = new SolidColorBrush(Colors.AliceBlue),
                    Transform = new ScaleTransform(1.1, 1.1)
                }
            };
        }

        private void MainWindow_LoadedForColors(object sender, RoutedEventArgs e)
        {
            // Force editor colors after everything is loaded
            ForceEditorColors();

            // Set initial text for the RichTextBox
            editorscript.Text = null;

            editorscript.AppendText("Editor ready - find scripts in the pane to the left, you will load the script to this pane after double clicking it.");
        }

        private void ForceEditorColors()
        {
            try
            {
                // Get the foreground brush from app resources
                var foregroundBrush = Application.Current.Resources["ForegroundBrush"] as SolidColorBrush;
                if (foregroundBrush != null)
                {
                    editorscript.Foreground = foregroundBrush;
                    System.Diagnostics.Debug.WriteLine($"Set editor foreground to: {foregroundBrush.Color}");
                }
                else
                {
                    // Fallback to white text
                    editorscript.Foreground = new SolidColorBrush(Colors.White);
                }

                System.Diagnostics.Debug.WriteLine("Forced editor colors");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to force editor colors: {ex.Message}");
            }
        }

        private void LoadScriptAsPlainText(string scriptText)
        {
            try
            {
                // With RichTextBox, clear and set text properly
                editorscript.Text = null;
                editorscript.AppendText(scriptText);


                System.Diagnostics.Debug.WriteLine("Script loaded as plain text in RichTextBox");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading script as plain text: {ex.Message}");
                editorscript.Text = null;
                editorscript.AppendText(scriptText);
            }
        }

        private void LoadCustomSyntaxHighlighting()
        {
            // With standard TextBox, just force colors - no syntax highlighting needed
            ForceEditorColors();
        }

        private void LoadMonokaiSodaTheme()
        {
            // Use the custom highlighting method
            LoadCustomSyntaxHighlighting();
        }

        private async Task SyncScriptsFromGitHub()
        {
            string scriptRunnerDir = Path.Combine(appDataPath, "ScriptRunner");
            string scriptsDir = Path.Combine(scriptRunnerDir, "Scripts");
            string logFile = Path.Combine(scriptRunnerDir, "history.log");
            string repoUrl = "https://github.com/ScriptRunnr/scripts.git";

            // Ensure all required directories exist
            if (!Directory.Exists(scriptRunnerDir))
                Directory.CreateDirectory(scriptRunnerDir);
            if (!Directory.Exists(scriptsDir))
                Directory.CreateDirectory(scriptsDir);

            void Log(string msg)
            {
                try
                {
                    var logDir = Path.GetDirectoryName(logFile);
                    if (!Directory.Exists(logDir))
                        Directory.CreateDirectory(logDir);
                    File.AppendAllText(logFile, $"[{DateTime.Now:u}] {msg}\n");
                }
                catch { }
                Dispatcher.Invoke(() =>
                {
                    try
                    {
                        editorLogs.AppendText($"{msg}\n");
                        editorLogs.ScrollToEnd();
                    }
                    catch { }
                });
            }

            await Task.Run(() =>
            {
                try
                {
                    if (!Repository.IsValid(scriptsDir) || !Directory.EnumerateFiles(scriptsDir).Any())
                    {
                        // If not a git repo or empty, clone fresh
                        Log($"Cloning repository from {repoUrl} to {scriptsDir}");
                        // Clean up any partial/old files
                        foreach (var file in Directory.GetFiles(scriptsDir)) File.Delete(file);
                        foreach (var dir in Directory.GetDirectories(scriptsDir)) Directory.Delete(dir, true);
                        Repository.Clone(repoUrl, scriptsDir);
                        Log("Clone complete.");
                    }
                    else
                    {
                        using (var repo = new Repository(scriptsDir))
                        {
                            Log("Fetching updates from GitHub...");
                            var remote = repo.Network.Remotes["origin"] ?? repo.Network.Remotes.Add("origin", repoUrl);
                            var refSpecs = remote.FetchRefSpecs.Select(x => x.Specification);
                            Commands.Fetch(repo, remote.Name, refSpecs, null, null);

                            // Checkout or create a local branch tracking origin/main
                            var branch = repo.Branches["main"] ?? repo.Branches["master"];
                            if (branch == null)
                            {
                                Log("No main/master branch found in remote repo.");
                                return;
                            }
                            if (!branch.IsCurrentRepositoryHead)
                                Commands.Checkout(repo, branch);

                            // Merge remote changes
                            var remoteBranch = repo.Branches[$"origin/{branch.FriendlyName}"];
                            if (remoteBranch != null)
                            {
                                var result = repo.Merge(remoteBranch, new LibGit2Sharp.Signature("ScriptRunner", "noreply@localhost", DateTimeOffset.Now));
                                Log($"Merge result: {result.Status}");
                            }
                            Log("Repository sync complete.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log($"Git sync error: {ex.Message}");
                }
            });
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Initialize admin tools
            InitializeAdminTools();
            LoadWmiNamespaces();

            // Ensure WmiQueries.json is present in AppData before loading
            await EnsureWmiQueriesFile();

            // Load WMI queries from AppData
            string wmiQueriesPath = Path.Combine(appDataPath, "ScriptRunner","Scripts", "WmiQueries.json");
            if (File.Exists(wmiQueriesPath))
            {
                LoadQueriesFromJson(wmiQueriesPath);
            }

            // Sync scripts from GitHub
            await SyncScriptsFromGitHub();

            // Load scripts into tree view
            string scriptsPath = Path.Combine(appDataPath, "ScriptRunner", "Scripts");
            _allItems = LoadPs1DirectoryTree(scriptsPath);
            treeViewScripts.Items.Clear();
            treeViewScripts.ItemsSource = _allItems;

            // Set up the trace listener for Immediate Window output
            textBoxListener = new TextBoxTraceListener(immediateWindowOutput);
            Trace.Listeners.Add(textBoxListener);
        }

        /// <summary>
        /// Ensures WmiQueries.json exists in AppData, copying from the app directory or GitHub if needed
        /// </summary>
        private async Task EnsureWmiQueriesFile()
        {
            string appDataScriptRunnerDir = Path.Combine(appDataPath, "ScriptRunner", "Scripts");
            string appDataWmiQueries = Path.Combine(appDataScriptRunnerDir, "WmiQueries.json");
            string localWmiQueries = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "WmiQueries.json");

            // Ensure the ScriptRunner directory exists
            if (!Directory.Exists(appDataScriptRunnerDir))
                Directory.CreateDirectory(appDataScriptRunnerDir);

            bool needsCopy = !File.Exists(appDataWmiQueries) || new FileInfo(appDataWmiQueries).Length == 0;

            if (needsCopy)
            {
                try
                {
                    // Try to copy from local app directory first
                    if (File.Exists(localWmiQueries) && new FileInfo(localWmiQueries).Length > 0)
                    {
                        File.Copy(localWmiQueries, appDataWmiQueries, true);
                        Debug.WriteLine("WmiQueries.json copied from application directory to AppData");
                    }
                    else
                    {
                        // If not found locally, try to download from GitHub
                        await DownloadWmiQueriesFromGitHub(appDataWmiQueries);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Failed to copy WmiQueries.json: {ex.Message}");
                    MessageBox.Show($"Warning: Could not load WMI queries configuration.\n{ex.Message}",
                    "Configuration Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        /// <summary>
        /// Downloads WmiQueries.json from the GitHub repository
        /// </summary>
        private async Task DownloadWmiQueriesFromGitHub(string destinationPath)
        {
            string wmiQueriesUrl = "https://github.com/ScriptRunnr/scripts/blob/1f1de9ccfaa78dac6dac3fb7980350717ededc31/wmiQueries.json";

            try
            {
                using (var client = new WebClient())
                {
                    await client.DownloadFileTaskAsync(wmiQueriesUrl, destinationPath);
                    Debug.WriteLine("WmiQueries.json downloaded from GitHub");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to download WmiQueries.json from GitHub: {ex.Message}");
                // Create a default/empty queries file
                File.WriteAllText(destinationPath, "[]");
                Debug.WriteLine("Created empty WmiQueries.json as fallback");
            }
        }

        private string GetStatusMessage(string status, string targetPC)
        {
            // Map status to user-friendly messages with emojis
            switch (status)
            {
                case "Initializing":
                    return $"🔄 Initializing on {targetPC}...";

                case "Preparing credentials":
                    return $"🔑 Preparing credentials for {targetPC}...";

                case "Creating remote service":
                    return $"🔧 Creating remote service on {targetPC}...";

                case "Copying script to target":
                    return $"📤 Copying script to {targetPC}...";

                case "Script copied - starting execution":
                    return $"🚀 Starting execution on {targetPC}...";

                case "Running":
                    return $"⏳ Executing on {targetPC}...";

                case "Process completed":
                case "Completed":
                    return $"✅ Execution Complete on {targetPC}!";

                case "Cancelled by user":
                    return $"🛑 Execution Cancelled on {targetPC}";

                case "Failed to start remote process":
                    return $"❌ Failed to start on {targetPC}";

                case "Error copying script":
                    return $"❌ Failed to copy script to {targetPC}";

                default:
                    // For errors and other statuses
                    if (status.StartsWith("Error"))
                    {
                        return $"❌ Error on {targetPC}";
                    }
                    else if (status.StartsWith("Failed"))
                    {
                        return $"❌ Failed on {targetPC}";
                    }
                    return null; // Don't show message for unknown statuses
            }
        }


        public class TextBoxTraceListener : TraceListener
        {
            private TextBox textBox;
            private Dispatcher dispatcher;
            private string _lastWrittenText = string.Empty;
            private DateTime _lastWriteTime = DateTime.MinValue;

            public TextBoxTraceListener(TextBox textBox)
            {
                this.textBox = textBox;
                this.dispatcher = textBox.Dispatcher;
        }

            public override void Write(string message)
      {
                if (string.IsNullOrEmpty(message)) return;

                // Check for duplicate within a small time window (100ms)
                var now = DateTime.Now;
                if (message.Equals(_lastWrittenText, StringComparison.OrdinalIgnoreCase) &&
                    now.Subtract(_lastWriteTime).TotalMilliseconds < 100)
                {
                    // Skip this duplicate write
                    return;
        }

                // Update tracking variables
                _lastWrittenText = message;
                _lastWriteTime = now;

                dispatcher.BeginInvoke(new Action(() =>
                {
                    textBox.AppendText(message);
                    textBox.ScrollToEnd();
                }));
    }

            public override void WriteLine(string message)
            {
                Write(message + Environment.NewLine);
            }
        }


        // Represents a pair of display name and value for combo boxes
        public class ComboBoxPairs
        {
            public string critName { get; set; }
            public override string ToString()
            {
                return critName;
            }
            public string critValue { get; set; }
            public ComboBoxPairs(string CritName, string CritValue)
            {
                critName = CritName;
                critValue = CritValue;
            }
        }
    }
}