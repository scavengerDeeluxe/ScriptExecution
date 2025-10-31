using ScriptRunner;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Script
{
    /// <summary>
    /// Interaction logic for splash.xaml
    /// </summary>

    public partial class splash : Window
    {
        private static void Main()
        {
            var ResetSplashCreated = new ManualResetEvent(false);
            var SplashThread = new Thread(ShowSplash);
            SplashThread.SetApartmentState(ApartmentState.STA);
            SplashThread.IsBackground = true;
            SplashThread.Name = "Splash Screen";
            SplashThread.Start();
            ResetSplashCreated.WaitOne();
            App SplashWindow = new App();
            var app = new App();
            App.InitializeComponent();
            app.Run();
        }

private static void ShowSplash()
        {
            SplashWindow = new SplashWindow();
            SplashWindow.Show();
            ResetSplashCreated.Set();
            System.Windows.Threading.Dispatcher.Run();
        }
        public interface IApplicationLoading
        {
            void AddMessage(string message);
            void PercentComplete(int current);
            void LoadComplete();
        }
        public void LoadComplete()
        {
            Dispatcher.InvokeShutdown();
        }


    }
}
