using DataSenderPlusProject.Helpers;
using DataSenderPlusProject.Models;
using DataSenderPlusProject.Security;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace DataSenderPlusProject
{
    public partial class MainWindow : Window
    {

        DispatcherTimer Show_timer;
        TimeSpan TimeForShow;
        DispatcherTimer DataSender_Type_1 = new DispatcherTimer();

        DispatcherTimer Show_timer_Type_2;
        TimeSpan TimeForShow_Type_2;
        DispatcherTimer DataSender_Type_2 = new DispatcherTimer();

        DispatcherTimer Show_timer_Type_3;
        TimeSpan TimeForShow_Type_3;
        DispatcherTimer DataSender_Type_3 = new DispatcherTimer();

        DispatcherTimer Show_timer_Type_4;
        TimeSpan TimeForShow_Type_4;
        DispatcherTimer DataSender_Type_4 = new DispatcherTimer();

        DispatcherTimer Show_timer_Type_5;
        TimeSpan TimeForShow_Type_5;
        DispatcherTimer DataSender_Type_5 = new DispatcherTimer();

        DataSenderPlus dp = new DataSenderPlus();
        C_JasonFile J = new C_JasonFile();
        private LogViewerWindow logViewer;

        private void ShowSettings_Click(object sender, RoutedEventArgs e)
        {
            if (new SettingsWindow { Owner = this }.ShowDialog() == true) StartConfiguredService();
        }

        private void RefreshAccess_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                RefreshSenderAccess();
            }
            catch (Exception error)
            {
                Logger.Error($"Reading SenderType.AccessFlag failed.{Environment.NewLine}{error}");
                MessageBox.Show(this, "خواندن دسترسی سربرگ‌ها ناموفق بود. اتصال دیتابیس و ستون AccessFlag جدول SenderType را بررسی کنید.",
                    "دسترسی سربرگ‌ها", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void ShowLogs_Click(object sender, RoutedEventArgs e)
        {
            if (logViewer == null)
            {
                logViewer = new LogViewerWindow { Owner = this };
                logViewer.Closed += (s, args) => logViewer = null;
                logViewer.Show();
            }
            else
            {
                logViewer.WindowState = WindowState.Normal;
                logViewer.Activate();
            }
        }

        public MainWindow()
        {
            LicenseManager manager = new LicenseManager();

            LicenseModel license = manager.LoadLicense();

            // فایل لایسنس وجود ندارد
            if (license == null)
            {
                HardwareInfo hardware = new HardwareInfo();

                ActivationWindow window =
                    new ActivationWindow(hardware.GetHardwareFingerprint());

                window.ShowDialog();

                Environment.Exit(0);
                return;
            }

            // لایسنس غیرفعال است
            if (!license.IsActive)
            {
                MessageBox.Show("License is Disabled.");

                Environment.Exit(0);
                return;
            }

            // تاریخ لایسنس گذشته است
            if (license.ExpireDate.Date < DateTime.Today)
            {
                MessageBox.Show("License Expired.");
                //MessageBox.Show("Step 2");


                Environment.Exit(0);
                //MessageBox.Show("Step 3");

                return;
            }

            //// تعداد روز باقی مانده
            //int remainDays = (license.ExpireDate.Date - DateTime.Today).Days;

            //MessageBox.Show(
            //    $"License Expire : {license.ExpireDate:yyyy/MM/dd}\n" +
            //    $"Remaining Days : {remainDays}");



            /* LicenseManager manager = new LicenseManager();

           LicenseModel license = manager.LoadLicense();

           MessageBox.Show(license.ExpireDate.ToString());


           LicenseManager manager = new LicenseManager();

           LicenseModel model = new LicenseModel();

           HardwareInfo hardware = new HardwareInfo();

           model.HardwareFingerprint = hardware.GetHardwareFingerprint();

           model.ExpireDate = DateTime.Now.AddDays(30);

           model.LastCheckDate = DateTime.Now;

           model.IsActive = true;

           manager.SaveLicense(model);


           LicenseManager manager = new LicenseManager();

           MessageBox.Show(manager.LicenseExists().ToString());

           HardwareInfo hardware = new HardwareInfo();

           string fp = hardware.GetHardwareFingerprint();

           MessageBox.Show(fp  ) ; 
           */


            //    this.Closing += MainWindow_Closing;

            InitializeComponent();
            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += TimerTick_DateNow;
            timer.Start();

            PersianDate_Lbl.Content = dp.PersianDate();

            Application.Current.MainWindow = this;
            Loaded += InitializeDatabaseSettings;
        }

        private void InitializeDatabaseSettings(object sender, RoutedEventArgs e)
        {
            Loaded -= InitializeDatabaseSettings;
            bool needsSettings = false;
            try
            {
                string warning = DatabaseSettings.MigrateLegacyConfig();
                if (warning != null)
                {
                    Logger.Warning(warning);
                    MessageBox.Show(this, warning, "تنظیمات", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                var settings = DatabaseSettings.Load();
                needsSettings = string.IsNullOrWhiteSpace(settings.Server)
                    || string.IsNullOrWhiteSpace(settings.Database) || string.IsNullOrWhiteSpace(settings.UserName);
            }
            catch (Exception error)
            {
                ReportSettingsError(error);
                needsSettings = true;
            }

            // Cancelling settings leaves the main window open and automatic sending stopped.
            if (needsSettings && new SettingsWindow { Owner = this }.ShowDialog() != true) return;
            StartConfiguredService();
        }

        private void ReportSettingsError(Exception error)
        {
            // Avoid logging exception text that could contain configuration values.
            Logger.Error($"Database settings initialization failed. Type={error.GetType().FullName}; HResult={error.HResult}");
            MessageBox.Show(this, DatabaseSettings.DescribeError(error) +
                "\nبرنامه باز می‌ماند؛ از فرم تنظیمات مشخصات اتصال را اصلاح کنید.",
                "تنظیمات", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        private void StartConfiguredService()
        {
            try
            {
                RefreshSenderAccess();
                if (((TabItem)SenderTabs.Items[0]).IsEnabled && !DataSender_Type_1.IsEnabled && IsServiceMode())
                    SendData_Btn_Click(null, null);
            }
            catch (Exception error)
            {
                Logger.Error($"Automatic sender startup failed. Type={error.GetType().FullName}; HResult={error.HResult}");
                MessageBox.Show(this, "خواندن دسترسی سربرگ‌ها یا راه‌اندازی ارسال خودکار ناموفق بود. اتصال دیتابیس، AccessFlag و تنظیمات زمان‌بندی را بررسی کنید. برنامه باز می‌ماند.",
                    "خطای راه‌اندازی", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void RefreshSenderAccess()
        {
            // Disable until the complete access list has been read successfully.
            SenderAccess.Apply(SenderTabs, new bool[5]);
            using (SqlConnection connection = GetConnection())
            using (var command = new SqlCommand(
                "SELECT TypeID, AccessFlag FROM SenderType WHERE TypeID BETWEEN 1 AND 5", connection))
            {
                command.CommandTimeout = 15;
                using (var reader = command.ExecuteReader())
                    SenderAccess.Apply(SenderTabs, SenderAccess.ReadFlags(reader));
            }
            var sendTimers = new[] { DataSender_Type_1, DataSender_Type_2, DataSender_Type_3, DataSender_Type_4, DataSender_Type_5 };
            var displayTimers = new[] { Show_timer, Show_timer_Type_2, Show_timer_Type_3, Show_timer_Type_4, Show_timer_Type_5 };
            for (int index = 0; index < 5; index++)
            {
                if (!((TabItem)SenderTabs.Items[index]).IsEnabled)
                {
                    sendTimers[index].Stop();
                    displayTimers[index]?.Stop();
                }
            }
        }
        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;

            this.Hide();
        }
        void TimerTick_DateNow(object sender, EventArgs e)
        {
            DateTime_Lbl.Content = DateTime.Now.ToLongTimeString();
        }



        private SqlConnection GetConnection()
        {
            string cs = J.GetConnectionString();

            SqlConnection cn = new SqlConnection(cs);

            cn.Open();

            return cn;
        }
        private FtpConfigurationModel GetFtpConfiguration(int configId, SqlConnection cn)
        {
            string query = @"
    SELECT
        ServerAddress,
        UserName,
        Password,
        FileName,
        IntervalValue,
        SendFlag,
        FolderPath,
        OutPutTypeID

    FROM FtpConfiguration
    WHERE ConfigID = @ConfigID";

            SqlCommand cmd = new SqlCommand(query, cn);

            cmd.Parameters.AddWithValue("@ConfigID", configId);

            SqlDataReader reader = cmd.ExecuteReader();

            if (reader.Read())
            {
                FtpConfigurationModel config = new FtpConfigurationModel();

                config.ServerAddress = reader["ServerAddress"].ToString();
                config.UserName = reader["UserName"].ToString();
                config.Password = reader["Password"].ToString();
                config.FileName = reader["FileName"].ToString();
                config.IntervalValue = Convert.ToInt32(reader["IntervalValue"]);
                config.SendFlag = Convert.ToInt32(reader["SendFlag"]);
                config.FolderPath = reader["FolderPath"].ToString();
                config.OutputTypeID = Convert.ToInt32(reader["OutputTypeID"]);

                reader.Close();

                return config;
            }

            reader.Close();

            return null;
        }

        private bool IsServiceMode()
        {
            try
            {
                using (SqlConnection cn = GetConnection())
                {
                    string query = @"
                SELECT TOP 1 ServiceMode
                FROM SystemConfiguration";

                    SqlCommand cmd = new SqlCommand(query, cn);

                    object result = cmd.ExecuteScalar();

                    if (result == null)
                        return false;

                    return Convert.ToBoolean(result);
                }
            }
            catch
            {
                return false;
            }
        }

        public void F_UploadFtp(int configId)
        {
            try
            {
                //throw new Exception("Test SMS");
                Logger.Info($"F_UploadFtp Started.  ConfigID={configId}");

                SqlConnection cn = GetConnection();

                Logger.Info("Database connection opened.");

                // دریافت تنظیمات FTP
                FtpConfigurationModel config = GetFtpConfiguration(configId, cn);

                if (config == null)
                {
                    throw new Exception("FTP Configuration not found.");
                }

                Logger.Info($"FTP Configuration Loaded. File : {config.FileName}");

                // دریافت نام Stored Procedure
                string Q_ProcedureName =
                    "SELECT ProcedureName FROM SenderType WHERE TypeID = @TypeID AND AccessFlag = 1";

                SqlCommand GetProcedureName_cmd = new SqlCommand(Q_ProcedureName, cn);

                GetProcedureName_cmd.Parameters.AddWithValue("@TypeID", configId);

                var vProcedureName = GetProcedureName_cmd.ExecuteScalar();

                if (vProcedureName == null)
                {
                    cn.Close();
                    Logger.Warning($"Export skipped: SenderType is missing or AccessFlag is disabled. TypeID={configId}");
                    RefreshSenderAccess();
                    return;
                }

                // اجرای Stored Procedure
                DataTable dtable = new DataTable();

                SqlCommand cmd = new SqlCommand();
                cmd.Connection = cn;
                cmd.CommandText = vProcedureName.ToString();
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandTimeout = DatabaseSettings.GetCommandTimeoutSeconds();

                SqlDataAdapter adp = new SqlDataAdapter(cmd);
                adp.Fill(dtable);

                Logger.Info($"Stored Procedure executed successfully. Rows Count : {dtable.Rows.Count}");
                // اتصال به دیتابیس دیگر لازم نیست
                cn.Close();

                Logger.Info("Database connection closed.");

                // تعیین مسیر ذخیره فایل
                string basePath;

                if (string.IsNullOrWhiteSpace(config.FolderPath))
                {
                    basePath = System.IO.Path.GetDirectoryName(
                        Assembly.GetExecutingAssembly().Location);
                }
                else
                {
                    basePath = config.FolderPath;
                }

                // ساخت فایل با فرمت انتخاب شده
                ExportHelper.Export(
                    dtable,                 // اطلاعات خروجی
                    config.OutputTypeID,    // نوع خروجی (JSON,CSV,...)
                    config.FileName,        // نام فایل
                    basePath);              // مسیر ذخیره

                Logger.Info("Export File Created Successfully.");

                // ارسال به FTP
                if (config.SendFlag == 1)
                {
                    Logger.Info("FTP upload started.");

                    string extension = ExportHelper.GetFileExtension(config.OutputTypeID);
                    string localFilePath = System.IO.Path.Combine(basePath, "ExportFile", config.FileName + extension);
                    J.F_SendFile2Ftp(config.ServerAddress, config.UserName, config.Password, localFilePath);
                    Logger.Info($"FTP upload completed successfully. ConfigID={configId}");
                }

                // Record success only after export and any requested upload have finished.
                using (SqlConnection logConnection = GetConnection())
                using (SqlCommand logCommand = new SqlCommand(
                    @"INSERT INTO LogRegisteration (ConfigID, UploadTime, StatusID, Comment)
                      VALUES (@ConfigID, GETDATE(), 1, @Comment)", logConnection))
                {
                    logCommand.Parameters.AddWithValue("@ConfigID", configId);
                    logCommand.Parameters.AddWithValue("@Comment", "The Update Was Completed Successfully.");
                    logCommand.ExecuteNonQuery();
                }
                Logger.Info("F_UploadFtp_Ts1 Finished.");

                Log_Type_1_lbl.Content =
                    "Ok" + " _ " +
                    dp.PersianDate() + " _ " +
                    DateTime.Now.ToShortTimeString();
            }
            catch (Exception Error)
            {
                Logger.Error($"F_UploadFtp failed. ConfigID={configId}{Environment.NewLine}{Error}");
                SmsHelper.SendError(Error.Message);

                Log_Type_1_lbl.Content =
                    "Err" + " _ " +
                    dp.PersianDate() + " _ " +
                    DateTime.Now.ToShortTimeString();

                try
                {
                    using (SqlConnection logConnection = GetConnection())
                    using (SqlCommand logCommand = new SqlCommand(
                        @"INSERT INTO LogRegisteration (ConfigID, LogDate, StatusID, Comment)
                          VALUES (@ConfigID, GETDATE(), 1, @Comment)", logConnection))
                    {
                        logCommand.Parameters.AddWithValue("@ConfigID", configId);
                        logCommand.Parameters.AddWithValue("@Comment", Error.ToString());
                        logCommand.ExecuteNonQuery();
                    }
                }
                catch (Exception logError)
                {
                    Logger.Error($"Failed to write LogRegisteration. ConfigID={configId}{Environment.NewLine}{logError}");
                }
            }
        }
    

        void TimerTick_UploadFtp(object sender, EventArgs e) { F_UploadFtp(1); }
        void TimerTick_UploadFtp_Type_2(object sender, EventArgs e) { F_UploadFtp(2); }
        void TimerTick_UploadFtp_Type_3(object sender, EventArgs e) { F_UploadFtp(3); }
        void TimerTick_UploadFtp_Type_4(object sender, EventArgs e) { F_UploadFtp(4); }
        void TimerTick_UploadFtp_Type_5(object sender, EventArgs e) { F_UploadFtp(5); }

        private void SendData_Btn_Click(object sender, RoutedEventArgs e)
        {
            string cs = J.GetConnectionString();
            SqlConnection cn = new SqlConnection(cs);
            cn.Open();
            SqlCommand GetInterval_Type_1_cmd = new SqlCommand(" SELECT IntervalValue  FROM FtpConfiguration WHERE ConfigID  = 1 ", cn);
            var vInterval_Type_1 = GetInterval_Type_1_cmd.ExecuteScalar();
            cn.Close();

            TimeForShow = TimeSpan.FromMinutes(Convert.ToDouble(vInterval_Type_1));
            Show_timer = new DispatcherTimer(new TimeSpan(0, 0, 1), DispatcherPriority.Normal, delegate
            {
                Timer_Type_1_lbl.Content = TimeForShow.ToString("c");
                if (TimeForShow == TimeSpan.Zero)
                    TimeForShow = TimeForShow.Add(TimeSpan.FromSeconds(Convert.ToDouble(vInterval_Type_1) * 60));
                else
                    TimeForShow = TimeForShow.Add(TimeSpan.FromSeconds(-1));
            }, Application.Current.Dispatcher);

            Show_timer.Start();
            DataSender_Type_1.Interval = TimeSpan.FromMinutes(Convert.ToDouble(vInterval_Type_1));
            DataSender_Type_1.Tick += TimerTick_UploadFtp;
            DataSender_Type_1.Start();
        }

        private void Create_Btn_Click(object sender, RoutedEventArgs e) { F_UploadFtp(1); }

        private void Panel_Type_Button_MouseRightButtonDown(object sender, MouseButtonEventArgs e) { }

        private void T2_SendData_Btn_Click(object sender, RoutedEventArgs e)
        {
            string cs = J.GetConnectionString();
            SqlConnection cn = new SqlConnection(cs);
            cn.Open();
            SqlCommand GetInterval_Type_2_cmd = new SqlCommand(" SELECT IntervalValue  FROM FtpConfiguration WHERE ConfigID  = 2 ", cn);
            var vInterval_Type_2 = GetInterval_Type_2_cmd.ExecuteScalar();
            cn.Close();
            TimeForShow_Type_2 = TimeSpan.FromMinutes(Convert.ToDouble(vInterval_Type_2));
            Show_timer_Type_2 = new DispatcherTimer(new TimeSpan(0, 0, 1), DispatcherPriority.Normal, delegate
            {
                T2_Timer_Type_1_lbl.Content = TimeForShow_Type_2.ToString("c");
                if (TimeForShow_Type_2 == TimeSpan.Zero)
                    TimeForShow_Type_2 = TimeForShow_Type_2.Add(TimeSpan.FromSeconds(Convert.ToDouble(vInterval_Type_2) * 60));
                else
                    TimeForShow_Type_2 = TimeForShow_Type_2.Add(TimeSpan.FromSeconds(-1));
            }, Application.Current.Dispatcher);
            Show_timer_Type_2.Start();
            DataSender_Type_2.Interval = TimeSpan.FromMinutes(Convert.ToDouble(vInterval_Type_2));
            DataSender_Type_2.Tick += TimerTick_UploadFtp_Type_2;
            DataSender_Type_2.Start();
        }

        private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e) { }

        private void T2_Create_Btn_Click(object sender, RoutedEventArgs e) { F_UploadFtp(2); }

        private void Log_Type_1_lbl_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            Process.Start("explorer.exe", @System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\ExportFile\\");
        }

        private void T2_Log_Type_1_lbl_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            Process.Start("explorer.exe", @System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\ExportFile\\");
        }

        private void T3_Create_Btn_Click(object sender, RoutedEventArgs e) { F_UploadFtp(3); }

        private void T3_SendData_Btn_Click(object sender, RoutedEventArgs e)
        {
            string cs = J.GetConnectionString();
            SqlConnection cn = new SqlConnection(cs);
            cn.Open();
            SqlCommand GetInterval_Type_3_cmd = new SqlCommand(" SELECT IntervalValue  FROM FtpConfiguration WHERE ConfigID  = 3 ", cn);
            var vInterval_Type_3 = GetInterval_Type_3_cmd.ExecuteScalar();
            cn.Close();
            TimeForShow_Type_3 = TimeSpan.FromMinutes(Convert.ToDouble(vInterval_Type_3));
            Show_timer_Type_3 = new DispatcherTimer(new TimeSpan(0, 0, 1), DispatcherPriority.Normal, delegate
            {
                T3_Timer_Type_1_lbl.Content = TimeForShow_Type_3.ToString("c");
                if (TimeForShow_Type_3 == TimeSpan.Zero)
                    TimeForShow_Type_3 = TimeForShow_Type_3.Add(TimeSpan.FromSeconds(Convert.ToDouble(vInterval_Type_3) * 60));
                else
                    TimeForShow_Type_3 = TimeForShow_Type_3.Add(TimeSpan.FromSeconds(-1));
            }, Application.Current.Dispatcher);
            Show_timer_Type_3.Start();
            DataSender_Type_3.Interval = TimeSpan.FromMinutes(Convert.ToDouble(vInterval_Type_3));
            DataSender_Type_3.Tick += TimerTick_UploadFtp_Type_3;
            DataSender_Type_3.Start();
        }

        private void T4_Create_Btn_Click(object sender, RoutedEventArgs e) { F_UploadFtp(4); }

        private void T4_SendData_Btn_Click(object sender, RoutedEventArgs e)
        {
            string cs = J.GetConnectionString();
            SqlConnection cn = new SqlConnection(cs);
            cn.Open();
            SqlCommand GetInterval_Type_4_cmd = new SqlCommand(" SELECT IntervalValue  FROM FtpConfiguration WHERE ConfigID  = 4 ", cn);
            var vInterval_Type_4 = GetInterval_Type_4_cmd.ExecuteScalar();
            cn.Close();
            TimeForShow_Type_4 = TimeSpan.FromMinutes(Convert.ToDouble(vInterval_Type_4));
            Show_timer_Type_4 = new DispatcherTimer(new TimeSpan(0, 0, 1), DispatcherPriority.Normal, delegate
            {
                T4_Timer_Type_1_lbl.Content = TimeForShow_Type_4.ToString("c");
                if (TimeForShow_Type_4 == TimeSpan.Zero)
                    TimeForShow_Type_4 = TimeForShow_Type_4.Add(TimeSpan.FromSeconds(Convert.ToDouble(vInterval_Type_4) * 60));
                else
                    TimeForShow_Type_4 = TimeForShow_Type_4.Add(TimeSpan.FromSeconds(-1));
            }, Application.Current.Dispatcher);
            Show_timer_Type_4.Start();
            DataSender_Type_4.Interval = TimeSpan.FromMinutes(Convert.ToDouble(vInterval_Type_4));
            DataSender_Type_4.Tick += TimerTick_UploadFtp_Type_4;
            DataSender_Type_4.Start();
        }

        private void T5_Create_Btn_Click(object sender, RoutedEventArgs e) { F_UploadFtp(5); }

        

        private void Title_Type_6_lbl_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            
        }
    }
}
