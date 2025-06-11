using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
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
        public MainWindow()
        {
            InitializeComponent();

            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += TimerTick_DateNow;
            timer.Start();

            PersianDate_Lbl.Content = dp.PersianDate();


            CheckServiceMode();


        }
        void TimerTick_DateNow(object sender, EventArgs e)
        {

            DateTime_Lbl.Content = DateTime.Now.ToLongTimeString();


        }
        public void F_UploadFtp_Ts1()
        {


            try
            {

                string cs = J.GetConnectionString();

                SqlConnection cn = new SqlConnection(cs);

                cn.Open();

                string Q_ProcedureName = " SELECT  ProcedureName    FROM SenderType WHERE TypeID  = 1 ";
                string Q_ServerAddress = " SELECT  ServerAddress    FROM FtpConfiguration WHERE ConfigID = 1 ";
                string Q_UserName = " SELECT  UserName         FROM FtpConfiguration WHERE ConfigID = 1  ";
                string Q_Password = " SELECT  Password         FROM FtpConfiguration WHERE ConfigID = 1  ";
                string Q_FileName = " SELECT  FileName         FROM FtpConfiguration WHERE ConfigID = 1  ";
                string Q_TimerInterval = " SELECT  IntervalValue    FROM FtpConfiguration WHERE ConfigID = 1  ";
                string Q_SendFlag = " SELECT  SendFlag    FROM FtpConfiguration WHERE ConfigID = 1  ";

                SqlCommand GetProcedureName_cmd = new SqlCommand(Q_ProcedureName, cn);
                SqlCommand GetServerAddress_cmd = new SqlCommand(Q_ServerAddress, cn);
                SqlCommand GetUserName_cmd = new SqlCommand(Q_UserName, cn);
                SqlCommand GetPassword_cmd = new SqlCommand(Q_Password, cn);
                SqlCommand GetFileName_cmd = new SqlCommand(Q_FileName, cn);
                SqlCommand TimerInterval_cmd = new SqlCommand(Q_TimerInterval, cn);
                SqlCommand GetSendFlag_cmd   = new SqlCommand(Q_SendFlag, cn);


                var vProcedureName = GetProcedureName_cmd.ExecuteScalar();
                var vServerAddress = GetServerAddress_cmd.ExecuteScalar();
                var vUserName = GetUserName_cmd.ExecuteScalar();
                var vPassword = GetPassword_cmd.ExecuteScalar();
                var vFileName = GetFileName_cmd.ExecuteScalar();
                var vTimerInterval = TimerInterval_cmd.ExecuteScalar();
                var vSendFlag = GetSendFlag_cmd.ExecuteScalar();


                string Q_LogRecord = " 	 INSERT INTO [dbo].[LogRegisteration]( ConfigID , UploadTime ,StatusID ,Comment ) " +
                                        " VALUES(@ConfigID, GetDate() , 1, 'The Update Was Completed Successfully.' ) ";

                SqlCommand LogRecord_cmd = new SqlCommand(Q_LogRecord, cn);

                LogRecord_cmd.Parameters.AddWithValue("@ConfigID", 1);

                LogRecord_cmd.ExecuteNonQuery();



                DataTable dtable = new DataTable();
                SqlCommand cmd = new SqlCommand();

                cmd.Connection = cn;
                cmd.CommandText = vProcedureName.ToString();
                cmd.CommandType = CommandType.StoredProcedure;
                SqlDataAdapter adp = new SqlDataAdapter(cmd);
                adp.Fill(dtable);

                var JsonOutput = J.F_CreateJasonFile(dtable).ToString();

                cn.Close();

                File.WriteAllText(@System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location)
                   + "\\ExportFile\\"
                    + vFileName + ".json", JsonOutput);

                //J.F_ReadConfig(); 
                if ( Convert.ToInt32( vSendFlag) == 1  )
                {
                    
                        J.F_SendFile2Ftp(vServerAddress.ToString(), vUserName.ToString(), vPassword.ToString(), vFileName.ToString());


                        if (J.ErrorMessage == null)
                        {
                            Log_Type_1_lbl.Content = "Ok" + " _ " + dp.PersianDate() + " _ " + DateTime.Now.ToShortTimeString();
                        }
                        else if (J.ErrorMessage != null)
                        {
                            Log_Type_1_lbl.Content = J.ErrorMessage;

                        }
                    }

                Log_Type_1_lbl.Content = "Ok" + " _ " + dp.PersianDate() + " _ " + DateTime.Now.ToShortTimeString();


                // MessageBox.Show("عملیات با موفقیت انجام شد"); 


            }
            catch (Exception Error)
            {

                // MessageBox.Show(Error.ToString());

                Log_Type_1_lbl.Content = "Err" + " _ " + dp.PersianDate() + " _ " + DateTime.Now.ToShortTimeString();

                string cs = J.GetConnectionString();

                SqlConnection cn = new SqlConnection(cs);

                cn.Open();
                string CorrectError = Error.ToString().Replace("'", "''");
                string Q_LogRecord = " 	 INSERT INTO [dbo].[LogRegisteration]( ConfigID , LogDate ,StatusID ,Comment ) " +
                                        " VALUES(@ConfigID, GetDate() , 1, " + "N' " + CorrectError + "' ) ";

                SqlCommand LogRecord_cmd = new SqlCommand(Q_LogRecord, cn);

                LogRecord_cmd.Parameters.AddWithValue("@ConfigID", 1);

                LogRecord_cmd.ExecuteNonQuery();

                cn.Close();

            }

        }

        public void F_UploadFtp_Ts2()
        {


            try
            {

                string cs = J.GetConnectionString();

                SqlConnection cn = new SqlConnection(cs);

                cn.Open();

                string Q_ProcedureName = " SELECT  ProcedureName    FROM SenderType WHERE TypeID  = 2 ";
                string Q_ServerAddress = " SELECT  ServerAddress    FROM FtpConfiguration WHERE ConfigID = 2 ";
                string Q_UserName = " SELECT  UserName         FROM FtpConfiguration WHERE ConfigID = 2  ";
                string Q_Password = " SELECT  Password         FROM FtpConfiguration WHERE ConfigID = 2  ";
                string Q_FileName = " SELECT  FileName         FROM FtpConfiguration WHERE ConfigID = 2  ";
                string Q_TimerInterval = " SELECT  IntervalValue    FROM FtpConfiguration WHERE ConfigID = 2  ";

                SqlCommand GetProcedureName_cmd = new SqlCommand(Q_ProcedureName, cn);
                SqlCommand GetServerAddress_cmd = new SqlCommand(Q_ServerAddress, cn);
                SqlCommand GetUserName_cmd = new SqlCommand(Q_UserName, cn);
                SqlCommand GetPassword_cmd = new SqlCommand(Q_Password, cn);
                SqlCommand GetFileName_cmd = new SqlCommand(Q_FileName, cn);
                SqlCommand TimerInterval_cmd = new SqlCommand(Q_TimerInterval, cn);


                var Type_2_vProcedureName = GetProcedureName_cmd.ExecuteScalar();
                var Type_2_vServerAddress = GetServerAddress_cmd.ExecuteScalar();
                var Type_2_vUserName = GetUserName_cmd.ExecuteScalar();
                var Type_2_vPassword = GetPassword_cmd.ExecuteScalar();
                var Type_2_vFileName = GetFileName_cmd.ExecuteScalar();
                var Type_2_vTimerInterval = TimerInterval_cmd.ExecuteScalar();

                string Q_LogRecord = " 	 INSERT INTO [dbo].[LogRegisteration]( ConfigID , UploadTime ,StatusID ,Comment ) " +
                                        " VALUES(@ConfigID, GetDate() , 2, 'The Update Was Completed Successfully.' ) ";

                SqlCommand LogRecord_cmd = new SqlCommand(Q_LogRecord, cn);

                LogRecord_cmd.Parameters.AddWithValue("@ConfigID", 2);

                LogRecord_cmd.ExecuteNonQuery();



                DataTable dtable = new DataTable();
                SqlCommand cmd = new SqlCommand();

                cmd.Connection = cn;
                cmd.CommandText = Type_2_vProcedureName.ToString();
                cmd.CommandType = CommandType.StoredProcedure;
                SqlDataAdapter adp = new SqlDataAdapter(cmd);
                adp.Fill(dtable);

                var JsonOutput = J.F_CreateJasonFile(dtable).ToString();

                cn.Close();

                File.WriteAllText(@System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location)
                    + "\\ExportFile\\" + Type_2_vFileName + ".json", JsonOutput);

                //J.F_ReadConfig(); 
                J.F_SendFile2Ftp(Type_2_vServerAddress.ToString(), Type_2_vUserName.ToString()
                               , Type_2_vPassword.ToString(), Type_2_vFileName.ToString());


                if (J.ErrorMessage == null)
                {
                    T2_Log_Type_1_lbl.Content = "Ok" + " _ " + dp.PersianDate() + " _ " + DateTime.Now.ToShortTimeString();
                }
                else if (J.ErrorMessage != null)
                {
                    T2_Log_Type_1_lbl.Content = J.ErrorMessage;

                }

               // MessageBox.Show("عملیات با موفقیت انجام شد");
            }
            catch (Exception Error)
            {

                // MessageBox.Show(Error.ToString());

                T2_Log_Type_1_lbl.Content = "Err" + " _ " + dp.PersianDate() + " _ " + DateTime.Now.ToShortTimeString();

                string cs = J.GetConnectionString();

                SqlConnection cn = new SqlConnection(cs);

                cn.Open();
                string CorrectError = Error.ToString().Replace("'", "''");
                string Q_LogRecord = " 	 INSERT INTO [dbo].[LogRegisteration]( ConfigID , LogDate ,StatusID ,Comment ) " +
                                        " VALUES(@ConfigID, GetDate() , 2, " + "N' " + CorrectError + "' ) ";

                SqlCommand LogRecord_cmd = new SqlCommand(Q_LogRecord, cn);

                LogRecord_cmd.Parameters.AddWithValue("@ConfigID", 2);

                LogRecord_cmd.ExecuteNonQuery();

                cn.Close();

            }
        }

        public void F_UploadFtp_Ts3()
        {


            try
            {

                string cs = J.GetConnectionString();

                SqlConnection cn = new SqlConnection(cs);

                cn.Open();

                string Q_ProcedureName = " SELECT  ProcedureName    FROM SenderType WHERE TypeID  = 3 ";
                string Q_ServerAddress = " SELECT  ServerAddress    FROM FtpConfiguration WHERE ConfigID = 3 ";
                string Q_UserName = " SELECT  UserName         FROM FtpConfiguration WHERE ConfigID = 3  ";
                string Q_Password = " SELECT  Password         FROM FtpConfiguration WHERE ConfigID = 3  ";
                string Q_FileName = " SELECT  FileName         FROM FtpConfiguration WHERE ConfigID = 3  ";
                string Q_TimerInterval = " SELECT  IntervalValue    FROM FtpConfiguration WHERE ConfigID = 3  ";

                SqlCommand GetProcedureName_cmd = new SqlCommand(Q_ProcedureName, cn);
                SqlCommand GetServerAddress_cmd = new SqlCommand(Q_ServerAddress, cn);
                SqlCommand GetUserName_cmd = new SqlCommand(Q_UserName, cn);
                SqlCommand GetPassword_cmd = new SqlCommand(Q_Password, cn);
                SqlCommand GetFileName_cmd = new SqlCommand(Q_FileName, cn);
                SqlCommand TimerInterval_cmd = new SqlCommand(Q_TimerInterval, cn);


                var Type_3_vProcedureName = GetProcedureName_cmd.ExecuteScalar();
                var Type_3_vServerAddress = GetServerAddress_cmd.ExecuteScalar();
                var Type_3_vUserName = GetUserName_cmd.ExecuteScalar();
                var Type_3_vPassword = GetPassword_cmd.ExecuteScalar();
                var Type_3_vFileName = GetFileName_cmd.ExecuteScalar();
                var Type_3_vTimerInterval = TimerInterval_cmd.ExecuteScalar();

                string Q_LogRecord = " 	 INSERT INTO [dbo].[LogRegisteration]( ConfigID , UploadTime ,StatusID ,Comment ) " +
                                        " VALUES(@ConfigID, GetDate() , 3, 'The Update Was Completed Successfully.' ) ";

                SqlCommand LogRecord_cmd = new SqlCommand(Q_LogRecord, cn);

                LogRecord_cmd.Parameters.AddWithValue("@ConfigID", 3);

                LogRecord_cmd.ExecuteNonQuery();



                DataTable dtable = new DataTable();
                SqlCommand cmd = new SqlCommand();

                cmd.Connection = cn;
                cmd.CommandText = Type_3_vProcedureName.ToString();
                cmd.CommandType = CommandType.StoredProcedure;
                SqlDataAdapter adp = new SqlDataAdapter(cmd);
                adp.Fill(dtable);

                var JsonOutput = J.F_CreateJasonFile(dtable).ToString();

                cn.Close();

                File.WriteAllText(@System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location)
                    + "\\ExportFile\\" + Type_3_vFileName + ".json", JsonOutput);

                //J.F_ReadConfig(); 
                J.F_SendFile2Ftp(Type_3_vServerAddress.ToString(), Type_3_vUserName.ToString()
                               , Type_3_vPassword.ToString(), Type_3_vFileName.ToString());


                if (J.ErrorMessage == null)
                {
                    T3_Log_Type_1_lbl.Content = "Ok" + " _ " + dp.PersianDate() + " _ " + DateTime.Now.ToShortTimeString();
                }
                else if (J.ErrorMessage != null)
                {
                    T3_Log_Type_1_lbl.Content = J.ErrorMessage;

                }

                // MessageBox.Show("عملیات با موفقیت انجام شد");
            }
            catch (Exception Error)
            {

                // MessageBox.Show(Error.ToString());

                T3_Log_Type_1_lbl.Content = "Err" + " _ " + dp.PersianDate() + " _ " + DateTime.Now.ToShortTimeString();

                string cs = J.GetConnectionString();

                SqlConnection cn = new SqlConnection(cs);

                cn.Open();
                string CorrectError = Error.ToString().Replace("'", "''");
                string Q_LogRecord = " 	 INSERT INTO [dbo].[LogRegisteration]( ConfigID , LogDate ,StatusID ,Comment ) " +
                                        " VALUES(@ConfigID, GetDate() , 3, " + "N' " + CorrectError + "' ) ";

                SqlCommand LogRecord_cmd = new SqlCommand(Q_LogRecord, cn);

                LogRecord_cmd.Parameters.AddWithValue("@ConfigID", 3);

                LogRecord_cmd.ExecuteNonQuery();

                cn.Close();

            }
        }

        public void F_UploadFtp_Ts4()
        {


            try
            {

                string cs = J.GetConnectionString();

                SqlConnection cn = new SqlConnection(cs);

                cn.Open();

                string Q_ProcedureName = " SELECT  ProcedureName    FROM SenderType WHERE TypeID  = 4 ";
                string Q_ServerAddress = " SELECT  ServerAddress    FROM FtpConfiguration WHERE ConfigID = 4 ";
                string Q_UserName =      " SELECT  UserName         FROM FtpConfiguration WHERE ConfigID = 4  ";
                string Q_Password =      " SELECT  Password         FROM FtpConfiguration WHERE ConfigID = 4  ";
                string Q_FileName =      " SELECT  FileName         FROM FtpConfiguration WHERE ConfigID = 4  ";
                string Q_TimerInterval = " SELECT  IntervalValue    FROM FtpConfiguration WHERE ConfigID = 4  ";

                SqlCommand GetProcedureName_cmd = new SqlCommand(Q_ProcedureName, cn);
                SqlCommand GetServerAddress_cmd = new SqlCommand(Q_ServerAddress, cn);
                SqlCommand GetUserName_cmd = new SqlCommand(Q_UserName, cn);
                SqlCommand GetPassword_cmd = new SqlCommand(Q_Password, cn);
                SqlCommand GetFileName_cmd = new SqlCommand(Q_FileName, cn);
                SqlCommand TimerInterval_cmd = new SqlCommand(Q_TimerInterval, cn);


                var Type_4_vProcedureName = GetProcedureName_cmd.ExecuteScalar();
                var Type_4_vServerAddress = GetServerAddress_cmd.ExecuteScalar();
                var Type_4_vUserName = GetUserName_cmd.ExecuteScalar();
                var Type_4_vPassword = GetPassword_cmd.ExecuteScalar();
                var Type_4_vFileName = GetFileName_cmd.ExecuteScalar();
                var Type_4_vTimerInterval = TimerInterval_cmd.ExecuteScalar();

                string Q_LogRecord = " 	 INSERT INTO [dbo].[LogRegisteration]( ConfigID , UploadTime ,StatusID ,Comment ) " +
                                        " VALUES(@ConfigID, GetDate() , 1 , 'The Update Was Completed Successfully.' ) ";

                SqlCommand LogRecord_cmd = new SqlCommand(Q_LogRecord, cn);

                LogRecord_cmd.Parameters.AddWithValue("@ConfigID", 4);

                LogRecord_cmd.ExecuteNonQuery();



                DataTable dtable = new DataTable();
                SqlCommand cmd = new SqlCommand();

                cmd.Connection = cn;
                cmd.CommandText = Type_4_vProcedureName.ToString();
                cmd.CommandType = CommandType.StoredProcedure;
                SqlDataAdapter adp = new SqlDataAdapter(cmd);
                adp.Fill(dtable);

                var JsonOutput = J.F_CreateJasonFile(dtable).ToString();

                cn.Close();

                File.WriteAllText(@System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location)
                    + "\\ExportFile\\" + Type_4_vFileName + ".json", JsonOutput);

                //J.F_ReadConfig(); 
                J.F_SendFile2Ftp(Type_4_vServerAddress.ToString(), Type_4_vUserName.ToString()
                               , Type_4_vPassword.ToString(), Type_4_vFileName.ToString());


                if (J.ErrorMessage == null)
                {
                    T4_Log_Type_1_lbl.Content = "Ok" + " _ " + dp.PersianDate() + " _ " + DateTime.Now.ToShortTimeString();
                }
                else if (J.ErrorMessage != null)
                {
                    T4_Log_Type_1_lbl.Content = J.ErrorMessage;

                }

                // MessageBox.Show("عملیات با موفقیت انجام شد");
            }
            catch (Exception Error)
            {

                // MessageBox.Show(Error.ToString());

                T4_Log_Type_1_lbl.Content = "Err" + " _ " + dp.PersianDate() + " _ " + DateTime.Now.ToShortTimeString();

                string cs = J.GetConnectionString();

                SqlConnection cn = new SqlConnection(cs);

                cn.Open();
                string CorrectError = Error.ToString().Replace("'", "''");
                string Q_LogRecord = " 	 INSERT INTO [dbo].[LogRegisteration]( ConfigID , LogDate ,StatusID ,Comment ) " +
                                        " VALUES(@ConfigID, GetDate() , 4 , " + "N' " + CorrectError + "' ) ";

                SqlCommand LogRecord_cmd = new SqlCommand(Q_LogRecord, cn);

                LogRecord_cmd.Parameters.AddWithValue("@ConfigID", 4);

                LogRecord_cmd.ExecuteNonQuery();

                cn.Close();

            }
        }
        public void F_UploadFtp_Ts5()
        {


            try
            {

                string cs = J.GetConnectionString();

                SqlConnection cn = new SqlConnection(cs);

                cn.Open();

                string Q_ProcedureName = " SELECT  ProcedureName    FROM SenderType WHERE TypeID  = 5 ";
                string Q_ServerAddress = " SELECT  ServerAddress    FROM FtpConfiguration WHERE ConfigID = 5 ";
                string Q_UserName = " SELECT  UserName         FROM FtpConfiguration WHERE ConfigID = 5  ";
                string Q_Password = " SELECT  Password         FROM FtpConfiguration WHERE ConfigID = 5  ";
                string Q_FileName = " SELECT  FileName         FROM FtpConfiguration WHERE ConfigID = 5  ";
                string Q_TimerInterval = " SELECT  IntervalValue    FROM FtpConfiguration WHERE ConfigID = 5  ";

                SqlCommand GetProcedureName_cmd = new SqlCommand(Q_ProcedureName, cn);
                SqlCommand GetServerAddress_cmd = new SqlCommand(Q_ServerAddress, cn);
                SqlCommand GetUserName_cmd = new SqlCommand(Q_UserName, cn);
                SqlCommand GetPassword_cmd = new SqlCommand(Q_Password, cn);
                SqlCommand GetFileName_cmd = new SqlCommand(Q_FileName, cn);
                SqlCommand TimerInterval_cmd = new SqlCommand(Q_TimerInterval, cn);


                var Type_5_vProcedureName = GetProcedureName_cmd.ExecuteScalar();
                var Type_5_vServerAddress = GetServerAddress_cmd.ExecuteScalar();
                var Type_5_vUserName = GetUserName_cmd.ExecuteScalar();
                var Type_5_vPassword = GetPassword_cmd.ExecuteScalar();
                var Type_5_vFileName = GetFileName_cmd.ExecuteScalar();
                var Type_5_vTimerInterval = TimerInterval_cmd.ExecuteScalar();

                string Q_LogRecord = " 	 INSERT INTO [dbo].[LogRegisteration]( ConfigID , UploadTime ,StatusID ,Comment ) " +
                                        " VALUES(@ConfigID, GetDate() , 1 , 'The Update Was Completed Successfully.' ) ";

                SqlCommand LogRecord_cmd = new SqlCommand(Q_LogRecord, cn);

                LogRecord_cmd.Parameters.AddWithValue("@ConfigID", 5);

                LogRecord_cmd.ExecuteNonQuery();



                DataTable dtable = new DataTable();
                SqlCommand cmd = new SqlCommand();

                cmd.Connection = cn;
                cmd.CommandText = Type_5_vProcedureName.ToString();
                cmd.CommandType = CommandType.StoredProcedure;
                SqlDataAdapter adp = new SqlDataAdapter(cmd);
                adp.Fill(dtable);

                var JsonOutput = J.F_CreateJasonFile(dtable).ToString();

                cn.Close();

                File.WriteAllText(@System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location)
                    + "\\ExportFile\\" + Type_5_vFileName + ".json", JsonOutput);

                //J.F_ReadConfig()
                //
                J.F_SendFile2Ftp(Type_5_vServerAddress.ToString(), Type_5_vUserName.ToString()
               , Type_5_vPassword.ToString(), Type_5_vFileName.ToString());


                if (J.ErrorMessage == null)
                {
                    T4_Log_Type_1_lbl.Content = "Ok" + " _ " + dp.PersianDate() + " _ " + DateTime.Now.ToShortTimeString();
                }
                else if (J.ErrorMessage != null)
                {
                    T4_Log_Type_1_lbl.Content = J.ErrorMessage;

                }

                // MessageBox.Show("عملیات با موفقیت انجام شد");
            }
            catch (Exception Error)
            {

                // MessageBox.Show(Error.ToString());

                T4_Log_Type_1_lbl.Content = "Err" + " _ " + dp.PersianDate() + " _ " + DateTime.Now.ToShortTimeString();

                string cs = J.GetConnectionString();

                SqlConnection cn = new SqlConnection(cs);

                cn.Open();
                string CorrectError = Error.ToString().Replace("'", "''");
                string Q_LogRecord = " 	 INSERT INTO [dbo].[LogRegisteration]( ConfigID , LogDate ,StatusID ,Comment ) " +
                                        " VALUES(@ConfigID, GetDate() , 5 , " + "N' " + CorrectError + "' ) ";

                SqlCommand LogRecord_cmd = new SqlCommand(Q_LogRecord, cn);

                LogRecord_cmd.Parameters.AddWithValue("@ConfigID", 5);

                LogRecord_cmd.ExecuteNonQuery();

                cn.Close();

            }
        }


        void TimerTick_UploadFtp(object sender, EventArgs e)
        {

            F_UploadFtp_Ts1();

        }

        void TimerTick_UploadFtp_Type_2(object sender, EventArgs e)
        {

            F_UploadFtp_Ts2();

        }

        void TimerTick_UploadFtp_Type_3(object sender, EventArgs e)
        {

            F_UploadFtp_Ts3();

        }
        void TimerTick_UploadFtp_Type_4(object sender, EventArgs e)
        {

            F_UploadFtp_Ts4();

        }
        void TimerTick_UploadFtp_Type_5(object sender, EventArgs e)
        {

            F_UploadFtp_Ts5();

        }



        private void SendData_Btn_Click(object sender, RoutedEventArgs e)
        {
            string cs = J.GetConnectionString();

            SqlConnection cn = new SqlConnection(cs);

            cn.Open();

            SqlCommand GetInterval_Type_1_cmd = new SqlCommand(" SELECT IntervalValue  FROM FtpConfiguration WHERE ConfigID  = 1 ", cn);

            var vInterval_Type_1 = GetInterval_Type_1_cmd.ExecuteScalar();

            cn.Close();

            /*-----------------------ShowTimeForUser------------------------------------*/

            TimeForShow = TimeSpan.FromMinutes(Convert.ToDouble(vInterval_Type_1));

            Show_timer = new DispatcherTimer(new TimeSpan(0, 0, 1), DispatcherPriority.Normal, delegate
            {
                Timer_Type_1_lbl.Content = TimeForShow.ToString("c");
                if (TimeForShow == TimeSpan.Zero)
                {
                    TimeForShow = TimeForShow.Add(TimeSpan.FromSeconds(Convert.ToDouble(vInterval_Type_1) * 60));
                }
                else
                {
                    //Show_timer.Stop();
                    TimeForShow = TimeForShow.Add(TimeSpan.FromSeconds(-1));
                }
            }, Application.Current.Dispatcher);

            Show_timer.Start();


            /*----------------------TimerForSaveAndUploadFile-------------------------------------*/

            DataSender_Type_1.Interval = TimeSpan.FromMinutes(Convert.ToDouble(vInterval_Type_1));
            DataSender_Type_1.Tick += TimerTick_UploadFtp;
            DataSender_Type_1.Start();
        }

        private void Create_Btn_Click(object sender, RoutedEventArgs e)
        {
            F_UploadFtp_Ts1();
        }

        private void Panel_Type_Button_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            
           // string appPath = AppDomain.CurrentDomain.BaseDirectory;
          //  Process.Start("explorer.exe", appPath);

         
        }

        private void T2_SendData_Btn_Click(object sender, RoutedEventArgs e)
        {

            string cs = J.GetConnectionString();

            SqlConnection cn = new SqlConnection(cs);

            cn.Open();

            SqlCommand GetInterval_Type_2_cmd = new SqlCommand(" SELECT IntervalValue  FROM FtpConfiguration WHERE ConfigID  = 2 ", cn);

            var vInterval_Type_2 = GetInterval_Type_2_cmd.ExecuteScalar();

            cn.Close();

            /*-----------------------ShowTimeForUser------------------------------------*/

            TimeForShow_Type_2 = TimeSpan.FromMinutes(Convert.ToDouble(vInterval_Type_2));

            Show_timer_Type_2 = new DispatcherTimer(new TimeSpan(0, 0, 1), DispatcherPriority.Normal, delegate
            {
                T2_Timer_Type_1_lbl.Content = TimeForShow_Type_2.ToString("c");
                if (TimeForShow_Type_2 == TimeSpan.Zero)
                {
                    TimeForShow_Type_2 = TimeForShow_Type_2.Add(TimeSpan.FromSeconds(Convert.ToDouble(vInterval_Type_2) * 60));
                }
                else
                {
                    //Show_timer.Stop();
                    TimeForShow_Type_2 = TimeForShow_Type_2.Add(TimeSpan.FromSeconds(-1));
                }
            }, Application.Current.Dispatcher);

            Show_timer_Type_2.Start();


            /*----------------------TimerForSaveAndUploadFile-------------------------------------*/

            DataSender_Type_2.Interval = TimeSpan.FromMinutes(Convert.ToDouble(vInterval_Type_2));
            DataSender_Type_2.Tick += TimerTick_UploadFtp_Type_2;
            DataSender_Type_2.Start();


        }

        private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void T2_Create_Btn_Click(object sender, RoutedEventArgs e)
        {
            F_UploadFtp_Ts2(); 
        }

        private void Log_Type_1_lbl_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            string appPath = AppDomain.CurrentDomain.BaseDirectory;
            Process.Start("explorer.exe", @System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location)
                   + "\\ExportFile\\");
        }

        private void T2_Log_Type_1_lbl_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            string appPath = AppDomain.CurrentDomain.BaseDirectory;
            Process.Start("explorer.exe", @System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location)
                   + "\\ExportFile\\");
        }

        private void T3_Create_Btn_Click(object sender, RoutedEventArgs e)
        {
            F_UploadFtp_Ts3();
        }

        private void T3_SendData_Btn_Click(object sender, RoutedEventArgs e)
        {
            string cs = J.GetConnectionString();

            SqlConnection cn = new SqlConnection(cs);

            cn.Open();

            SqlCommand GetInterval_Type_3_cmd = new SqlCommand(" SELECT IntervalValue  FROM FtpConfiguration WHERE ConfigID  = 3 ", cn);

            var vInterval_Type_3 = GetInterval_Type_3_cmd.ExecuteScalar();

            cn.Close();

            /*-----------------------ShowTimeForUser------------------------------------*/

            TimeForShow_Type_3 = TimeSpan.FromMinutes(Convert.ToDouble(vInterval_Type_3));

            Show_timer_Type_3 = new DispatcherTimer(new TimeSpan(0, 0, 1), DispatcherPriority.Normal, delegate
            {
                T3_Timer_Type_1_lbl.Content = TimeForShow_Type_3.ToString("c");
                if (TimeForShow_Type_3 == TimeSpan.Zero)
                {
                    TimeForShow_Type_3 = TimeForShow_Type_3.Add(TimeSpan.FromSeconds(Convert.ToDouble(vInterval_Type_3) * 60));
                }
                else
                {
                    //Show_timer.Stop();
                    TimeForShow_Type_3 = TimeForShow_Type_3.Add(TimeSpan.FromSeconds(-1));
                }
            }, Application.Current.Dispatcher);

            Show_timer_Type_3.Start();


            /*----------------------TimerForSaveAndUploadFile-------------------------------------*/

            DataSender_Type_3.Interval = TimeSpan.FromMinutes(Convert.ToDouble(vInterval_Type_3)) ;
            DataSender_Type_3.Tick += TimerTick_UploadFtp_Type_3;
            DataSender_Type_3.Start();
        }

        private void T4_Create_Btn_Click(object sender, RoutedEventArgs e)
        {
            F_UploadFtp_Ts4();
        }

        private void T4_SendData_Btn_Click(object sender, RoutedEventArgs e)
        {
            string cs = J.GetConnectionString();

            SqlConnection cn = new SqlConnection(cs);

            cn.Open();

            SqlCommand GetInterval_Type_4_cmd = new SqlCommand(" SELECT IntervalValue  FROM FtpConfiguration WHERE ConfigID  = 4 ", cn);

            var vInterval_Type_4 = GetInterval_Type_4_cmd.ExecuteScalar();

            cn.Close();

            /*-----------------------ShowTimeForUser------------------------------------*/

            TimeForShow_Type_4 = TimeSpan.FromMinutes(Convert.ToDouble(vInterval_Type_4));

            Show_timer_Type_4 = new DispatcherTimer(new TimeSpan(0, 0, 1), DispatcherPriority.Normal, delegate
            {
                T4_Timer_Type_1_lbl.Content = TimeForShow_Type_4.ToString("c");
                if (TimeForShow_Type_4 == TimeSpan.Zero)
                {
                    TimeForShow_Type_4 = TimeForShow_Type_3.Add(TimeSpan.FromSeconds(Convert.ToDouble(vInterval_Type_4) * 60));
                }
                else
                {
                    //Show_timer.Stop();
                    TimeForShow_Type_4 = TimeForShow_Type_4.Add(TimeSpan.FromSeconds(-1));
                }
            }, Application.Current.Dispatcher);

            Show_timer_Type_4.Start();


            /*----------------------TimerForSaveAndUploadFile-------------------------------------*/

            DataSender_Type_4.Interval = TimeSpan.FromMinutes(Convert.ToDouble(vInterval_Type_4));
            DataSender_Type_4.Tick += TimerTick_UploadFtp_Type_4;
            DataSender_Type_4.Start();
        }

        private void T5_Create_Btn_Click(object sender, RoutedEventArgs e)
        {
            F_UploadFtp_Ts4();
        }

        public void CheckServiceMode ()
        {
            string cs = J.GetConnectionString();

            SqlConnection cn = new SqlConnection(cs);

            cn.Open();


            SqlCommand GetInterval_Type_1_cmd = new SqlCommand(" SELECT IntervalValue  FROM FtpConfiguration WHERE ConfigID  = 1 ", cn);
            SqlCommand GetInterval_Type_2_cmd = new SqlCommand(" SELECT IntervalValue  FROM FtpConfiguration WHERE ConfigID  = 2 ", cn);
            SqlCommand GetInterval_Type_3_cmd = new SqlCommand(" SELECT IntervalValue  FROM FtpConfiguration WHERE ConfigID  = 3 ", cn);
            SqlCommand GetInterval_Type_4_cmd = new SqlCommand(" SELECT IntervalValue  FROM FtpConfiguration WHERE ConfigID  = 4 ", cn);
            SqlCommand GetInterval_Type_5_cmd = new SqlCommand(" SELECT IntervalValue  FROM FtpConfiguration WHERE ConfigID  = 5 ", cn);

            var vInterval_Type_1 = GetInterval_Type_1_cmd.ExecuteScalar();
            var vInterval_Type_2 = GetInterval_Type_2_cmd.ExecuteScalar();
            var vInterval_Type_3 = GetInterval_Type_3_cmd.ExecuteScalar();
            var vInterval_Type_4 = GetInterval_Type_4_cmd.ExecuteScalar();
            var vInterval_Type_5 = GetInterval_Type_5_cmd.ExecuteScalar();

            SqlCommand ServiceMode_Cmd = new SqlCommand(" SELECT ServiceMode   FROM SystemConfiguration ", cn);

            var vServiceMode = ServiceMode_Cmd.ExecuteScalar();
            bool bvServiceMode = Convert.ToBoolean(vServiceMode);

            SqlCommand Type_1_ServiceMode_Cmd = new SqlCommand(" SELECT AccessFlag  FROM [dbo].[SenderType] WHERE TypeID  = 1 ", cn);
            SqlCommand Type_2_ServiceMode_Cmd = new SqlCommand(" SELECT AccessFlag  FROM [dbo].[SenderType] WHERE TypeID  = 2 ", cn);
            SqlCommand Type_3_ServiceMode_Cmd = new SqlCommand(" SELECT AccessFlag  FROM [dbo].[SenderType] WHERE TypeID  = 3 ", cn);
            SqlCommand Type_4_ServiceMode_Cmd = new SqlCommand(" SELECT AccessFlag  FROM [dbo].[SenderType] WHERE TypeID  = 4 ", cn);
            SqlCommand Type_5_ServiceMode_Cmd = new SqlCommand(" SELECT AccessFlag  FROM [dbo].[SenderType] WHERE TypeID  = 5 ", cn);



            var v_Type_1_ServiceMode = Type_1_ServiceMode_Cmd.ExecuteScalar();
            var v_Type_2_ServiceMode = Type_2_ServiceMode_Cmd.ExecuteScalar();
            var v_Type_3_ServiceMode = Type_3_ServiceMode_Cmd.ExecuteScalar();
            var v_Type_4_ServiceMode = Type_4_ServiceMode_Cmd.ExecuteScalar();
            var v_Type_5_ServiceMode = Type_5_ServiceMode_Cmd.ExecuteScalar();


            bool bv_Type_1_ServiceMode = Convert.ToBoolean(v_Type_1_ServiceMode);
            bool bv_Type_2_ServiceMode = Convert.ToBoolean(v_Type_2_ServiceMode);
            bool bv_Type_3_ServiceMode = Convert.ToBoolean(v_Type_3_ServiceMode);
            bool bv_Type_4_ServiceMode = Convert.ToBoolean(v_Type_4_ServiceMode);
            bool bv_Type_5_ServiceMode = Convert.ToBoolean(v_Type_5_ServiceMode);

            if (bvServiceMode)
            {
                if (bv_Type_1_ServiceMode)
                {


                    /*-----------------------ShowTimeForUser------------------------------------*/

                    TimeForShow = TimeSpan.FromMinutes(Convert.ToDouble(vInterval_Type_1));

                    Show_timer = new DispatcherTimer(new TimeSpan(0, 0, 1), DispatcherPriority.Normal, delegate
                    {
                        Timer_Type_1_lbl.Content = TimeForShow.ToString("c");
                        if (TimeForShow == TimeSpan.Zero)
                        {
                            TimeForShow = TimeForShow.Add(TimeSpan.FromSeconds(Convert.ToDouble(vInterval_Type_1) * 60));
                        }
                        else
                        {
                            //Show_timer.Stop();
                            TimeForShow = TimeForShow.Add(TimeSpan.FromSeconds(-1));
                        }
                    }, Application.Current.Dispatcher);

                    Show_timer.Start();
                    DataSender_Type_1.Interval = TimeSpan.FromMinutes(Convert.ToDouble(vInterval_Type_1));
                    DataSender_Type_1.Tick += TimerTick_UploadFtp;
                    DataSender_Type_1.Start();
                }

                if (bv_Type_2_ServiceMode)
                {
                    DataSender_Type_2.Interval = TimeSpan.FromMinutes(Convert.ToDouble(vInterval_Type_2));
                    DataSender_Type_2.Tick += TimerTick_UploadFtp;
                    DataSender_Type_2.Start();
                }
                if (bv_Type_3_ServiceMode)
                {
                    DataSender_Type_3.Interval = TimeSpan.FromMinutes(Convert.ToDouble(vInterval_Type_3));
                    DataSender_Type_3.Tick += TimerTick_UploadFtp;
                    DataSender_Type_3.Start();
                }
                if (bv_Type_4_ServiceMode)
                {
                    DataSender_Type_4.Interval = TimeSpan.FromMinutes(Convert.ToDouble(vInterval_Type_4));
                    DataSender_Type_4.Tick += TimerTick_UploadFtp;
                    DataSender_Type_4.Start();
                }
                if (bv_Type_5_ServiceMode)
                {
                    DataSender_Type_5.Interval = TimeSpan.FromMinutes(Convert.ToDouble(vInterval_Type_5));
                    DataSender_Type_5.Tick += TimerTick_UploadFtp;
                    DataSender_Type_5.Start();
                }


            }


        }

        private void Title_Type_6_lbl_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            CheckServiceMode(); 
        }
    }
}

