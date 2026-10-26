using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using DataSenderPlusProject.Helpers;
using DataSenderPlusProject.Models;
using System.Diagnostics;

namespace DataSenderPlusProject
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private readonly SingleInstanceGuard instanceGuard =
            new SingleInstanceGuard("DataSenderPlus_Unique_App_ID_123456");

        protected override void OnStartup(StartupEventArgs e)
        {
            if (!instanceGuard.TryAcquire())
            {
                // Only this newly launched process exits; the running sender is untouched.
                MessageBox.Show("یک نسخهٔ دیگر از برنامه در حال اجراست. فقط این اجرای جدید بسته می‌شود.",
                    "هشدار", MessageBoxButton.OK, MessageBoxImage.Warning);
                Shutdown();
                return;
            }

            base.OnStartup(e);
            Logger.Info($"Application started. PID={Process.GetCurrentProcess().Id}; Path={System.Reflection.Assembly.GetExecutingAssembly().Location}");
            MainWindow = new MainWindow();
            MainWindow.Show();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            instanceGuard.Dispose();
            base.OnExit(e);
        }
    }
}
