using DataSenderPlusProject;
using DataSenderPlusProject.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Xml.Linq;

internal static class SettingsLayoutTests
{
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }

    private static void Layout(Window window)
    {
        window.Dispatcher.Invoke(DispatcherPriority.ApplicationIdle, new Action(() => { }));
        window.UpdateLayout();
    }

    private static void Capture(Window window, string name)
    {
        Layout(window);
        var bitmap = new RenderTargetBitmap((int)window.ActualWidth, (int)window.ActualHeight, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(window);
        var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using (var file = File.Create(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, name))) encoder.Save(file);
    }

    private static IEnumerable<DependencyObject> Descendants(DependencyObject root)
    {
        for (int index = 0; index < VisualTreeHelper.GetChildrenCount(root); index++)
        {
            var child = VisualTreeHelper.GetChild(root, index);
            yield return child;
            foreach (var nested in Descendants(child)) yield return nested;
        }
    }

    private static void CheckButtons(Window window)
    {
        foreach (var button in Descendants(window).OfType<Button>().Where(button => button.IsVisible))
        {
            var bounds = button.TransformToAncestor(window).TransformBounds(new Rect(button.RenderSize));
            Check(bounds.Left >= -1 && bounds.Top >= -1 && bounds.Right <= window.ActualWidth + 1
                && bounds.Bottom <= window.ActualHeight + 1, "Button outside window: " + button.Content);
        }
    }

    [STAThread]
    private static void Main(string[] args)
    {
        var settings = new SettingsWindow { WindowStartupLocation = WindowStartupLocation.Manual, Left = -10000, ShowActivated = false };
        settings.Show();
        Layout(settings); CheckButtons(settings); Capture(settings, "settings-access.png");
        ((PasswordBox)settings.FindName("AccessPassword")).Password = "RWP@1400";
        ((Button)settings.FindName("UnlockButton")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        ((TextBox)settings.FindName("ServerInput")).Text = "";
        ((Button)settings.FindName("TestDatabaseButton")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Check(((TextBlock)settings.FindName("StatusText")).Text.Contains("سرور")
            && ((Button)settings.FindName("TestDatabaseButton")).IsEnabled, "Invalid SQL test explains error and restores button");
        ((TextBox)settings.FindName("ServerInput")).Text = @"localhost\SQLEXPRESS";
        ((TextBox)settings.FindName("DatabaseInput")).Text = "SampleDatabase";
        ((TextBox)settings.FindName("UserInput")).Text = "sender_user";
        Layout(settings); CheckButtons(settings); Capture(settings, "database-settings.png");
        // Supply synthetic records without making a database request.
        typeof(SettingsWindow).GetField("senderLoaded", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(settings, true);
        var tabs = (TabControl)settings.FindName("SettingsTabs"); tabs.SelectedIndex = 1;
        var selector = (ComboBox)settings.FindName("SenderSelection");
        selector.ItemsSource = new[]
        {
            new FtpConfigurationModel { ConfigID=2, FileName="SenderFile_2", ServerAddress="ftp://example.test/incoming", UserName="ftp_user", Password="synthetic-only", IntervalValue=15, OutputTypeID=2, SendFlag=1, FolderPath=@"E:\DataExports" },
            new FtpConfigurationModel { ConfigID=5, FileName="SenderFile_5", ServerAddress="", UserName="", Password="", IntervalValue=30, OutputTypeID=4, SendFlag=2, FolderPath="" }
        };
        selector.SelectedIndex = 0;
        Layout(settings);
        Check(Descendants(selector).OfType<TextBlock>().Any(text => text.Text == "فایل ارسالی 2"),
            "Custom ComboBox template respects DisplayMemberPath");
        Check(((TextBox)settings.FindName("ExportFileName")).Text == "SenderFile_2", "Selected record populates fields");
        Check(Convert.ToString(((ComboBox)settings.FindName("OutputTypeInput")).SelectedValue) == "2", "Output format selected");
        var password = (PasswordBox)settings.FindName("FtpPassword");
        var shown = (TextBox)settings.FindName("FtpVisiblePassword");
        var toggle = (CheckBox)settings.FindName("ShowFtpPassword");
        Check(password.Password == "synthetic-only" && shown.Visibility == Visibility.Collapsed, "FTP password masked");
        ((TextBox)settings.FindName("FtpAddress")).Text = "invalid-address";
        ((Button)settings.FindName("TestFtpButton")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Check(((TextBlock)settings.FindName("StatusText")).Text.Contains("FTP")
            && ((Button)settings.FindName("TestFtpButton")).IsEnabled, "Invalid FTP test explains error and restores button");
        ((TextBox)settings.FindName("FtpAddress")).Text = "ftp://example.test/incoming";
        toggle.IsChecked = true; Check(shown.Text == "synthetic-only", "Reveal FTP password");
        shown.Text = "edited"; toggle.IsChecked = false; Check(password.Password == "edited" && shown.Text == "", "Hide edited FTP password");
        Layout(settings); CheckButtons(settings); Capture(settings, "sender-settings.png");
        selector.SelectedIndex = 1;
        Check(((TextBox)settings.FindName("ExportFileName")).Text == "SenderFile_5" && password.Password == "", "Switch record clears old password");
        var mode = (ComboBox)settings.FindName("SendModeInput");
        Check(Convert.ToString(mode.SelectedValue) == "2" && !((Button)settings.FindName("TestFtpButton")).IsEnabled,
            "SendFlag=2 selects local mode and disables FTP test");
        ((Button)settings.FindName("TestFtpButton")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Check(((TextBlock)settings.FindName("StatusText")).Text.Contains("لوکال"), "Local mode cannot upload even if test handler is invoked");
        mode.SelectedValue = "1";
        Check(((Button)settings.FindName("TestFtpButton")).IsEnabled, "FTP test available only in mode 1");
        selector.ItemsSource = new[] { new FtpConfigurationModel { ConfigID = 4, FileName = "Incomplete", IntervalValue = 0, OutputTypeID = 0, SendFlag = 0 } };
        selector.SelectedIndex = 0;
        Check(((TextBox)settings.FindName("IntervalInput")).Text == "", "Unset interval stays empty for repair");
        Check(((ComboBox)settings.FindName("OutputTypeInput")).SelectedIndex == -1, "Unset format does not silently choose a format");
        Check(((TextBlock)settings.FindName("StatusText")).Text.Contains("ناقص"), "Incomplete record explains required repair");
        settings.Height = settings.MinHeight; Layout(settings); CheckButtons(settings); Capture(settings, "sender-settings-small.png");
        settings.Close(); Check(password.Password == "", "Clear password on close");

        // Render the real main-window XAML without running licensing, database or timers.
        var xml = XDocument.Load(args[0]);
        var events = new[] { "Click", "SelectionChanged", "MouseDoubleClick", "MouseRightButtonDown" };
        foreach (var element in xml.Descendants())
            foreach (var attribute in element.Attributes().ToArray())
                if (attribute.Name.LocalName == "Class" || events.Contains(attribute.Name.LocalName)) attribute.Remove();
        var main = (Window)XamlReader.Parse(xml.ToString());
        main.WindowStartupLocation = WindowStartupLocation.Manual; main.Left = -10000; main.ShowActivated = false;
        main.Show(); Layout(main); CheckButtons(main); Capture(main, "main-window.png");
        main.Width = main.MinWidth; main.Height = main.MinHeight;
        Layout(main); CheckButtons(main); Capture(main, "main-window-small.png"); main.Close();
        Console.WriteLine("PASS: two tabs, per-record controls, password visibility, selection, and all buttons within default/minimum window sizes.");
    }
}
