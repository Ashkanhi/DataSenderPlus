using DataSenderPlusProject.Helpers;
using System;
using System.Windows;
using System.Security.Cryptography;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Controls;
using DataSenderPlusProject.Models;
using System.Data.SqlClient;
using System.Net;
using System.Windows.Media;

namespace DataSenderPlusProject
{
    public partial class SettingsWindow : Window
    {
        private bool unlocked;
        private bool senderBusy;
        private bool senderLoaded;
        public Dictionary<int, int> SavedSenderIntervals { get; } = new Dictionary<int, int>();

        public SettingsWindow()
        {
            InitializeComponent();
            MaxHeight = SystemParameters.WorkArea.Height;
            Loaded += (sender, args) => AccessPassword.Focus();
            Closing += (sender, args) =>
            {
                if (!senderBusy) return;
                args.Cancel = true;
                StatusText.Text = "عملیات در حال انجام است؛ پس از پایان آن فرم را ببندید.";
            };
            Closed += (sender, args) =>
            {
                AccessPassword.Clear();
                DatabasePassword.Clear();
                VisiblePassword.Clear();
                FtpPassword.Clear();
                FtpVisiblePassword.Clear();
                SenderSelection.ItemsSource = null;
                unlocked = false;
            };
        }

        private async void SettingsTabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!unlocked || e.Source != SettingsTabs) return;
            SaveButton.IsDefault = SettingsTabs.SelectedIndex == 0;
            SaveSenderButton.IsDefault = SettingsTabs.SelectedIndex == 1;
            if (SettingsTabs.SelectedIndex == 1 && !senderLoaded && !senderBusy)
                await LoadSenders();
        }

        private async void ReloadSender_Click(object sender, RoutedEventArgs e)
        {
            if (unlocked && !senderBusy) await LoadSenders();
        }

        private void SetSenderBusy(bool busy)
        {
            senderBusy = busy;
            ReloadSenderButton.IsEnabled = !busy;
            SenderSelection.IsEnabled = !busy;
            SenderFields.IsEnabled = !busy && SenderSelection.SelectedItem != null;
            SaveSenderButton.IsEnabled = !busy && SenderSelection.SelectedItem != null;
            // Keep the database configuration unchanged while a request is in progress.
            SaveButton.IsEnabled = !busy;
            TestDatabaseButton.IsEnabled = !busy;
            UpdateFtpTestAvailability();
            SettingsTabs.IsEnabled = !busy;
            if (busy) StatusText.Foreground = new SolidColorBrush(Color.FromRgb(98, 120, 141));
        }

        private void SendMode_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (unlocked) UpdateFtpTestAvailability();
        }

        private void UpdateFtpTestAvailability()
        {
            TestFtpButton.IsEnabled = !senderBusy && SenderSelection.SelectedItem != null
                && Convert.ToString((SendModeInput.SelectedItem as ComboBoxItem)?.Tag) == "1";
        }

        private void ShowTestResult(string message, bool success)
        {
            StatusText.Text = message;
            StatusText.Foreground = new SolidColorBrush(success ? Color.FromRgb(8, 127, 108) : Color.FromRgb(180, 35, 24));
        }

        private async void TestDatabase_Click(object sender, RoutedEventArgs e)
        {
            if (!unlocked || senderBusy) return;
            // Read controls on the UI thread, including any unsaved password edits.
            var settings = new DatabaseConnectionSettings
            {
                Server = ServerInput.Text.Trim(), Database = DatabaseInput.Text.Trim(), UserName = UserInput.Text.Trim(),
                Password = ShowPassword.IsChecked == true ? VisiblePassword.Text : DatabasePassword.Password
            };
            SetSenderBusy(true);
            TestDatabaseButton.Content = "در حال تست…";
            StatusText.Text = "در حال بررسی اتصال و پاسخ دیتابیس…";
            try
            {
                await ConnectionTester.TestDatabaseAsync(settings);
                ShowTestResult("تست موفق بود؛ اتصال به دیتابیس برقرار شد و پاسخ دریافت شد. تنظیمات ذخیره نشدند.", true);
                Logger.Info("Database connection test succeeded.");
            }
            catch (ArgumentException error) { ShowTestResult(error.Message, false); }
            catch (SqlException error)
            {
                ShowTestResult(ConnectionTester.DescribeDatabaseFailure(error), false);
                Logger.Warning($"Database connection test failed. SQL={error.Number}");
            }
            catch (Exception error)
            {
                ShowTestResult("تست دیتابیس ناموفق بود؛ مشخصات اتصال و شبکه را بررسی کنید.", false);
                Logger.Warning($"Database connection test failed. Type={error.GetType().FullName}");
            }
            finally
            {
                TestDatabaseButton.Content = "تست ارتباط دیتابیس";
                SetSenderBusy(false);
            }
        }

        private async void TestFtp_Click(object sender, RoutedEventArgs e)
        {
            if (!unlocked || senderBusy || SenderSelection.SelectedItem == null) return;
            if (Convert.ToString((SendModeInput.SelectedItem as ComboBoxItem)?.Tag) != "1")
            {
                ShowTestResult("در حالت فقط ذخیرهٔ لوکال، فایل به FTP ارسال نمی‌شود. برای تست FTP روش ارسال ۱ را انتخاب کنید.", false);
                return;
            }
            string address = FtpAddress.Text.Trim(), user = FtpUser.Text.Trim();
            string password = ShowFtpPassword.IsChecked == true ? FtpVisiblePassword.Text : FtpPassword.Password;
            SetSenderBusy(true);
            TestFtpButton.Content = "در حال ارسال…";
            StatusText.Text = "در حال ارسال فایل متنی کوچک به FTP…";
            try
            {
                string fileName = await ConnectionTester.UploadFtpTestAsync(address, user, password);
                ShowTestResult("تست موفق بود؛ فایل با متن «تست» روی سرور قرار گرفت: " + fileName, true);
                Logger.Info($"FTP upload test succeeded. File={fileName}");
            }
            catch (ArgumentException error) { ShowTestResult(error.Message, false); }
            catch (WebException error)
            {
                ShowTestResult(ConnectionTester.DescribeFtpFailure(error), false);
                Logger.Warning($"FTP upload test failed. Status={error.Status}");
            }
            catch (Exception error)
            {
                ShowTestResult("تست ارسال ناموفق بود؛ آدرس FTP، شبکه و مجوز نوشتن را بررسی کنید.", false);
                Logger.Warning($"FTP upload test failed. Type={error.GetType().FullName}");
            }
            finally
            {
                TestFtpButton.Content = "تست ارسال FTP";
                SetSenderBusy(false);
            }
        }

        private async Task LoadSenders()
        {
            string connectionString;
            if (!TryGetSenderConnection(out connectionString)) return;
            SetSenderBusy(true);
            StatusText.Text = "در حال خواندن تنظیمات ارسال…";
            try
            {
                var records = await Task.Run(() => FtpConfigurationStore.Load(connectionString));
                SenderSelection.ItemsSource = records;
                senderLoaded = true;
                StatusText.Text = records.Count == 0 ? "رکوردی در FtpConfiguration موجود نیست." : "فایل موردنظر را انتخاب کنید و پس از ویرایش، ذخیره کنید.";
                if (records.Count > 0) SenderSelection.SelectedIndex = 0;
            }
            catch (Exception error)
            {
                SenderSelection.ItemsSource = null;
                StatusText.Text = "خواندن تنظیمات ارسال ناموفق بود؛ اتصال دیتابیس و جدول FtpConfiguration را بررسی کنید.";
                Logger.Error($"Loading sender settings failed. Type={error.GetType().FullName}; HResult={error.HResult}");
            }
            finally { SetSenderBusy(false); }
        }

        private bool TryGetSenderConnection(out string connectionString)
        {
            connectionString = null;
            DatabaseConnectionSettings settings;
            string error;
            if (!DatabaseSettings.TryLoad(out settings, out error))
            {
                senderLoaded = false;
                SettingsTabs.SelectedIndex = 0;
                StatusText.Text = error;
                DatabasePassword.Focus();
                Logger.Warning("Sender settings require database credential recovery; no database request was started.");
                return false;
            }
            if (string.IsNullOrWhiteSpace(settings.Server) || string.IsNullOrWhiteSpace(settings.Database)
                || string.IsNullOrWhiteSpace(settings.UserName))
            {
                SettingsTabs.SelectedIndex = 0;
                StatusText.Text = "ابتدا مشخصات اتصال دیتابیس را کامل وارد و ذخیره کنید.";
                ServerInput.Focus();
                return false;
            }
            connectionString = settings.BuildConnectionString();
            return true;
        }

        private void SenderSelection_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (!unlocked) return;
            var settings = SenderSelection.SelectedItem as FtpConfigurationModel;
            FtpPassword.Clear();
            FtpVisiblePassword.Clear();
            ShowFtpPassword.IsChecked = false;
            if (settings == null)
            {
                SenderFields.IsEnabled = false;
                SaveSenderButton.IsEnabled = false;
                TestFtpButton.IsEnabled = false;
                return;
            }
            ExportFileName.Text = settings.FileName;
            ExportFolder.Text = settings.FolderPath;
            OutputTypeInput.SelectedValue = settings.OutputTypeID.ToString();
            IntervalInput.Text = settings.IntervalValue > 0 ? settings.IntervalValue.ToString() : "";
            SendModeInput.SelectedValue = settings.SendFlag.ToString();
            FtpAddress.Text = settings.ServerAddress;
            FtpUser.Text = settings.UserName;
            FtpPassword.Password = settings.Password;
            StatusText.Text = settings.IntervalValue < 1 || settings.OutputTypeID < 1 || settings.OutputTypeID > 4
                || string.IsNullOrWhiteSpace(settings.FileName) || (settings.SendFlag != 1 && settings.SendFlag != 2)
                ? "تنظیمات این رکورد ناقص است؛ نام فایل، بازهٔ ارسال، فرمت خروجی و روش ارسال را تکمیل و ذخیره کنید."
                : "تنظیمات فایل انتخاب‌شده را ویرایش و ذخیره کنید.";
            SenderFields.IsEnabled = !senderBusy;
            SaveSenderButton.IsEnabled = !senderBusy;
            UpdateFtpTestAvailability();
        }

        private void ShowFtpPassword_Changed(object sender, RoutedEventArgs e)
        {
            if (!unlocked) return;
            if (ShowFtpPassword.IsChecked == true)
            {
                FtpVisiblePassword.Text = FtpPassword.Password;
                FtpPassword.Visibility = Visibility.Collapsed;
                FtpVisiblePassword.Visibility = Visibility.Visible;
            }
            else
            {
                FtpPassword.Password = FtpVisiblePassword.Text;
                FtpVisiblePassword.Clear();
                FtpVisiblePassword.Visibility = Visibility.Collapsed;
                FtpPassword.Visibility = Visibility.Visible;
            }
        }

        private void BrowseFolder_Click(object sender, RoutedEventArgs e)
        {
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                dialog.Description = "پوشهٔ پایهٔ ذخیرهٔ فایل‌ها را انتخاب کنید؛ زیرپوشهٔ ExportFile در آن ساخته می‌شود.";
                dialog.SelectedPath = ExportFolder.Text;
                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK) ExportFolder.Text = dialog.SelectedPath;
            }
        }

        private async void SaveSender_Click(object sender, RoutedEventArgs e)
        {
            if (!unlocked || senderBusy) return;
            var selected = SenderSelection.SelectedItem as FtpConfigurationModel;
            if (selected == null) return;
            string connectionString;
            if (!TryGetSenderConnection(out connectionString)) return;
            int interval, outputType, sendFlag;
            if (!int.TryParse(IntervalInput.Text, out interval) || !int.TryParse(Convert.ToString(OutputTypeInput.SelectedValue), out outputType)
                || !int.TryParse(Convert.ToString((SendModeInput.SelectedItem as ComboBoxItem)?.Tag), out sendFlag))
            {
                StatusText.Text = "بازهٔ ارسال را به دقیقه وارد کنید و فرمت خروجی و روش ارسال را انتخاب کنید.";
                return;
            }
            var settings = new FtpConfigurationModel
            {
                ConfigID = selected.ConfigID, ServerAddress = FtpAddress.Text.Trim(), UserName = FtpUser.Text.Trim(),
                Password = ShowFtpPassword.IsChecked == true ? FtpVisiblePassword.Text : FtpPassword.Password,
                FileName = ExportFileName.Text.Trim(), FolderPath = ExportFolder.Text.Trim(),
                IntervalValue = interval, SendFlag = sendFlag, OutputTypeID = outputType
            };
            try
            {
                FtpConfigurationStore.Validate(settings);
                SetSenderBusy(true);
                StatusText.Text = "در حال ذخیرهٔ تنظیمات ارسال…";
                await Task.Run(() => FtpConfigurationStore.Save(settings, connectionString));
                // Update only the selected in-memory record after database persistence succeeds.
                selected.ServerAddress = settings.ServerAddress; selected.UserName = settings.UserName;
                selected.Password = settings.Password; selected.FileName = settings.FileName;
                selected.FolderPath = settings.FolderPath; selected.IntervalValue = settings.IntervalValue;
                selected.SendFlag = settings.SendFlag; selected.OutputTypeID = settings.OutputTypeID;
                SavedSenderIntervals[settings.ConfigID] = settings.IntervalValue;
                StatusText.Text = "تنظیمات این فایل ذخیره شد. تغییرات پس از بستن تنظیمات، از اجرای بعدی استفاده می‌شوند.";
                Logger.Info($"Sender settings saved successfully. ConfigID={settings.ConfigID}");
            }
            catch (ArgumentException error) { StatusText.Text = error.Message; }
            catch (SqlException error)
            {
                StatusText.Text = $"ذخیره انجام نشد (SQL {error.Number}). اتصال، مجوز UPDATE و طول مقادیر ستون‌ها را بررسی کنید.";
                Logger.Error($"Saving sender settings failed. ConfigID={selected.ConfigID}; SQL={error.Number}");
            }
            catch (Exception error)
            {
                StatusText.Text = "ذخیرهٔ تنظیمات ارسال ناموفق بود؛ رکورد و اتصال دیتابیس را بررسی کنید.";
                Logger.Error($"Saving sender settings failed. ConfigID={selected.ConfigID}; Type={error.GetType().FullName}");
            }
            finally { SetSenderBusy(false); }
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
                DatabaseConnectionSettings decrypted;
                string passwordError;
                if (DatabaseSettings.TryLoad(out decrypted, out passwordError))
                    DatabasePassword.Password = decrypted.Password;
                else
                    StatusText.Text = passwordError;
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
