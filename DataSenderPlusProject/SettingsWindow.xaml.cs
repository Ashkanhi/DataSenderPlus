using DataSenderPlusProject.Helpers;
using System;
using System.Windows;
using System.Security.Cryptography;

namespace DataSenderPlusProject
{
    public partial class SettingsWindow : Window
    {
        private bool unlocked;

        public SettingsWindow()
        {
            InitializeComponent();
            Loaded += (sender, args) => AccessPassword.Focus();
            Closed += (sender, args) =>
            {
                AccessPassword.Clear();
                DatabasePassword.Clear();
                VisiblePassword.Clear();
                unlocked = false;
            };
        }

        private void Unlock_Click(object sender, RoutedEventArgs e)
        {
            if (!DatabaseSettings.CheckAccessPassword(AccessPassword.Password))
            {
                AccessPassword.Clear();
                StatusText.Text = "رمز ورود صحیح نیست.";
                AccessPassword.Focus();
                return;
            }
            AccessPassword.Clear();
            unlocked = true;
            UnlockButton.IsDefault = false;
            SaveButton.IsDefault = true;
            AccessPanel.Visibility = Visibility.Collapsed;
            EditorPanel.Visibility = Visibility.Visible;
            StatusText.Text = "";
            TimeoutInput.Text = "300";
            try
            {
                // Preserve server/database/user even when the old password cannot be decrypted.
                var settings = DatabaseSettings.Load(false);
                ServerInput.Text = settings.Server;
                DatabaseInput.Text = settings.Database;
                UserInput.Text = settings.UserName;
                TimeoutInput.Text = settings.CommandTimeoutSeconds.ToString();
                DatabasePassword.Password = DatabaseSettings.Load().Password;
            }
            catch (Exception error)
            {
                StatusText.Text = DatabaseSettings.DescribeError(error);
            }
            ServerInput.Focus();
        }

        private void ShowPassword_Changed(object sender, RoutedEventArgs e)
        {
            if (!unlocked) return;
            if (ShowPassword.IsChecked == true)
            {
                VisiblePassword.Text = DatabasePassword.Password;
                DatabasePassword.Visibility = Visibility.Collapsed;
                VisiblePassword.Visibility = Visibility.Visible;
            }
            else
            {
                DatabasePassword.Password = VisiblePassword.Text;
                VisiblePassword.Clear();
                VisiblePassword.Visibility = Visibility.Collapsed;
                DatabasePassword.Visibility = Visibility.Visible;
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (!unlocked) return;
            int seconds;
            if (!int.TryParse(TimeoutInput.Text, out seconds) || seconds < 0)
            {
                StatusText.Text = "مهلت اجرا باید عدد صحیح صفر یا بیشتر باشد.";
                return;
            }
            try
            {
                DatabaseSettings.Save(new DatabaseConnectionSettings
                {
                    Server = ServerInput.Text.Trim(), Database = DatabaseInput.Text.Trim(),
                    UserName = UserInput.Text.Trim(),
                    Password = ShowPassword.IsChecked == true ? VisiblePassword.Text : DatabasePassword.Password,
                    CommandTimeoutSeconds = seconds
                });
                DataSenderPlusProject.Models.Logger.Info("Database settings saved successfully; changes apply to the next connection.");
                DialogResult = true;
            }
            catch (ArgumentException)
            {
                StatusText.Text = "سرور، نام دیتابیس و نام کاربری را کامل وارد کنید.";
            }
            catch (CryptographicException)
            {
                StatusText.Text = "رمزگذاری انجام نشد؛ برنامه را با حساب ویندوز دارای پروفایل فعال اجرا کنید. تنظیمات ذخیره نشد.";
            }
            catch (Exception)
            {
                StatusText.Text = "ذخیره انجام نشد؛ دسترسی نوشتن به فایل config کنار برنامه و ساختار آن را بررسی کنید.";
            }
        }
    }
}
