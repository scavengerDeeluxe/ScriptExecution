using System.Windows;

namespace ScriptRunner
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class LogShow : Window
    {
        public string ScriptText { get; set; }

        public LogShow(string logText)
        {
            InitializeComponent();
            ScriptText = logText;
            Loaded += LogShow_Loaded;
        }
        public void UpdateLog(string newLog)
        {
            ScriptText = newLog;
            ViewerPopout.Document.Blocks.Clear();
            ViewerPopout.AppendText(ScriptText ?? string.Empty);
        }
        private void LogShow_Loaded(object sender, RoutedEventArgs e)
        {
            if (ViewerPopout != null)
            {
                ViewerPopout.Document.Blocks.Clear();

            }

            ViewerPopout.AppendText(e.ToString() ?? string.Empty);
        }
    }
}
