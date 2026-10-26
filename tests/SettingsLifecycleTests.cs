using DataSenderPlusProject;
using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

internal static class SettingsLifecycleTests
{
    private static void Check(bool condition, string name)
    {
        if (!condition) throw new Exception(name);
    }

    [STAThread]
    private static void Main()
    {
        string configPath = AppDomain.CurrentDomain.SetupInformation.ConfigurationFile;
        File.WriteAllText(configPath, "<configuration><appSettings>"
            + "<add key='DatabaseServer' value='synthetic-server'/>"
            + "<add key='DatabaseName' value='synthetic-db'/>"
            + "<add key='DatabaseUserName' value='synthetic-user'/>"
            + "<add key='DatabasePasswordEncrypted' value='dpapi-user-v1:invalid'/>"
            + "</appSettings></configuration>");
        var app = new Application { ShutdownMode = ShutdownMode.OnMainWindowClose };
        var main = new Window { Width = 300, Height = 200, Left = -10000, ShowActivated = false };
        app.MainWindow = main;
        bool exited = false;
        app.Exit += (sender, args) => exited = true;
        main.Loaded += (sender, args) => main.Dispatcher.BeginInvoke(new Action(() =>
        {
            var dialog = new SettingsWindow { Owner = main, WindowStartupLocation = WindowStartupLocation.Manual, Left = -10000, ShowActivated = false };
            dialog.Loaded += (s, e) => dialog.Dispatcher.BeginInvoke(new Action(() =>
            {
                ((PasswordBox)dialog.FindName("AccessPassword")).Password = "RWP@1400";
                ((Button)dialog.FindName("UnlockButton")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                Check(((TextBox)dialog.FindName("ServerInput")).Text == "synthetic-server", "Server retained after decryption failure");
                Check(((TextBox)dialog.FindName("DatabaseInput")).Text == "synthetic-db", "Database retained");
                Check(((TextBox)dialog.FindName("UserInput")).Text == "synthetic-user", "User retained");
                Check(((TextBlock)dialog.FindName("StatusText")).Text.Length > 0, "Decryption failure explained");
                ((PasswordBox)dialog.FindName("DatabasePassword")).Password = "synthetic-new-password";
                ((Button)dialog.FindName("SaveButton")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                Check(dialog.DialogResult == true, "Save succeeded");
            }));
            Check(dialog.ShowDialog() == true, "Dialog saved and closed");
            Check(!exited && main.IsVisible && app.MainWindow == main, "Main window remains open after save");
            Check(!File.ReadAllText(configPath).Contains("synthetic-new-password"), "Saved password protected");
            var cancelled = new SettingsWindow { Owner = main, WindowStartupLocation = WindowStartupLocation.Manual, Left = -10000, ShowActivated = false };
            cancelled.Loaded += (s, e) => cancelled.Dispatcher.BeginInvoke(new Action(() => cancelled.Close()));
            cancelled.ShowDialog();
            Check(!exited && main.IsVisible, "Main window remains open after cancel");
            Console.WriteLine("PASS: invalid-password recovery, real settings save, protected persistence, and main window survives save/cancel.");
            main.Close();
        }));
        app.Run(main);
    }
}
