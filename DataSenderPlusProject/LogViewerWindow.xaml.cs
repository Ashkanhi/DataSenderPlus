using DataSenderPlusProject.Models;
using System;
using System.Threading.Tasks;
using System.Windows;

namespace DataSenderPlusProject
{
    public partial class LogViewerWindow : Window
    {
        public LogViewerWindow()
        {
            InitializeComponent();
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            await RefreshLogs();
        }

        private async void Refresh_Click(object sender, RoutedEventArgs e)
        {
            await RefreshLogs();
        }

        private async Task RefreshLogs()
        {
            RefreshButton.IsEnabled = false;
            ErrorsOnly.IsEnabled = false;
            bool errorsOnly = ErrorsOnly.IsChecked == true;
            StatusText.Text = "در حال خواندن لاگ‌ها…";
            try
            {
                var entries = await Task.Run(() => Logger.ReadRecent(10, errorsOnly));
                LogGrid.ItemsSource = entries;
                if (entries.Count > 0) LogGrid.SelectedIndex = 0;
                StatusText.Text = entries.Count == 0
                    ? (errorsOnly ? "خطایی در فایل‌های لاگ پیدا نشد." : "هنوز لاگی ثبت نشده است.")
                    : $"{entries.Count} {(errorsOnly ? "خطای" : "لاگ")} آخر — جدیدترین در بالا";
            }
            catch (Exception error)
            {
                LogGrid.ItemsSource = null;
                StatusText.Text = "خواندن لاگ‌ها ناموفق بود: " + error.Message;
            }
            finally
            {
                RefreshButton.IsEnabled = true;
                ErrorsOnly.IsEnabled = true;
            }
        }
    }
}
