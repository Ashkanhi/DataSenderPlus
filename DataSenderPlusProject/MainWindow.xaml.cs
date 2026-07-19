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
using System.Threading; // اضافه شده برای مدیریت Mutex
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
        // تعریف Mutex برای جلوگیری از اجرای چندباره برنامه
        private static Mutex _mutex = null;

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

            // کنترل اجرای تنها یک نسخه از برنامه
            bool createdNew;
            _mutex = new Mutex(true, "DataSenderPlus_Unique_App_ID_123456", out createdNew);

            if (!createdNew)
            {
                MessageBox.Show("برنامه در حال حاضر در حال اجرا است.", "هشدار", MessageBoxButton.OK, MessageBoxImage.Warning);
                Application.Current.Shutdown();
                return;
            }

            InitializeComponent();
            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += TimerTick_DateNow;
            timer.Start();

            PersianDate_Lbl.Content = dp.PersianDate();


            
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
        FolderPath
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

                reader.Close();

                return config;
            }

            reader.Close();

            return null;
        }

        public void F_UploadFtp(int configId)
        {
            try
            {
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
                    "SELECT ProcedureName FROM SenderType WHERE TypeID = @TypeID ";

                SqlCommand GetProcedureName_cmd = new SqlCommand(Q_ProcedureName, cn);

                GetProcedureName_cmd.Parameters.AddWithValue("@TypeID", configId);

                var vProcedureName = GetProcedureName_cmd.ExecuteScalar();

                if (vProcedureName == null)
                {
                    throw new Exception("Procedure Name not found.");
                }

                // ثبت لاگ در دیتابیس
                string Q_LogRecord =
                    @"INSERT INTO LogRegisteration
              (ConfigID, UploadTime, StatusID, Comment)
              VALUES (@ConfigID, GETDATE(), 1,
              'The Update Was Completed Successfully.')";

                SqlCommand LogRecord_cmd = new SqlCommand(Q_LogRecord, cn);
                LogRecord_cmd.Parameters.AddWithValue("@ConfigID", configId);
                LogRecord_cmd.ExecuteNonQuery();

                // اجرای Stored Procedure
                DataTable dtable = new DataTable();

                SqlCommand cmd = new SqlCommand();
                cmd.Connection = cn;
                cmd.CommandText = vProcedureName.ToString();
                cmd.CommandType = CommandType.StoredProcedure;

                SqlDataAdapter adp = new SqlDataAdapter(cmd);
                adp.Fill(dtable);

                Logger.Info($"Stored Procedure executed successfully. Rows Count : {dtable.Rows.Count}");

                // تبدیل به Json
                string JsonOutput = J.F_CreateJasonFile(dtable).ToString();

                cn.Close();

                Logger.Info("Database connection closed.");

                // تعیین مسیر فایل
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

                // ذخیره فایل Json
                File.WriteAllText(
                   System.IO.Path.Combine(basePath, "ExportFile", config.FileName + ".json"),
                    JsonOutput);

                Logger.Info($"Json file created successfully. File Name : {config.FileName}.json");

                // ارسال به FTP
                if (config.SendFlag == 1)
                {
                    Logger.Info("FTP upload started.");

                    J.F_SendFile2Ftp(
                        config.ServerAddress,
                        config.UserName,
                        config.Password,
                        config.FileName);

                    if (J.ErrorMessage == null)
                    {
                        Logger.Info("FTP upload completed successfully.");

                        Log_Type_1_lbl.Content =
                            "Ok" + " _ " +
                            dp.PersianDate() + " _ " +
                            DateTime.Now.ToShortTimeString();
                    }
                    else
                    {
                        Logger.Warning($"FTP upload failed. {J.ErrorMessage}");

                        Log_Type_1_lbl.Content = J.ErrorMessage;
                    }
                }

                Logger.Info("F_UploadFtp_Ts1 Finished.");

                Log_Type_1_lbl.Content =
                    "Ok" + " _ " +
                    dp.PersianDate() + " _ " +
                    DateTime.Now.ToShortTimeString();
            }
            catch (Exception Error)
            {
                Logger.Error(Error.ToString());

                Log_Type_1_lbl.Content =
                    "Err" + " _ " +
                    dp.PersianDate() + " _ " +
                    DateTime.Now.ToShortTimeString();

                try
                {
                    SqlConnection cn = GetConnection();

                    string CorrectError = Error.ToString().Replace("'", "''");

                    string Q_LogRecord =
                        @"INSERT INTO LogRegisteration
                  (ConfigID, LogDate, StatusID, Comment)
                  VALUES (@ConfigID, GETDATE(), 1, N'" + CorrectError + "')";

                    SqlCommand LogRecord_cmd = new SqlCommand(Q_LogRecord, cn);

                    LogRecord_cmd.Parameters.AddWithValue("@ConfigID", 1);

                    LogRecord_cmd.ExecuteNonQuery();

                    cn.Close();
                }
                catch
                {
                    // اگر دیتابیس در دسترس نبود،
                    // حداقل لاگ فایل ثبت شده است.
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
                    TimeForShow_Type_4 = TimeForShow_Type_3.Add(TimeSpan.FromSeconds(Convert.ToDouble(vInterval_Type_4) * 60));
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