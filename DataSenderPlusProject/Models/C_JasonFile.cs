using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace DataSenderPlusProject
{
    internal class C_JasonFile
    {
        public string ServerPath, UserName, Password;
        public string ErrorMessage;

        public void F_ReadConfig()
        {
            StreamReader Sr = new StreamReader(@System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\Setting.txt");
            ServerPath = Sr.ReadLine();
            UserName = Sr.ReadLine();
            Password = Sr.ReadLine();

            Sr.Close();
        }

        public string GetConnectionString()
        {

            StreamReader Sr = new StreamReader(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\Config.txt");

            string Server = Sr.ReadLine();
            string DataBaseName = Sr.ReadLine();
            string UserName = Sr.ReadLine();
            string Password = Sr.ReadLine();

            Sr.Close();

            string cs = "Data Source=" + Server + "; Initial Catalog=" + DataBaseName + "; User Id = "
             + UserName + "; Password = " + Password + "; timeout=15;";

            return cs;

        }





        public string F_CreateJasonFile(DataTable GetProcedureData)
        {

            string JasonExport = string.Empty;

            JasonExport = JsonConvert.SerializeObject(GetProcedureData);

            return JasonExport;

        }


        public void F_SendFile2Ftp(string GetFtpUrl, string GetUserName, string GetPassword, string GetFileName)
        {
            try
            {
                WebClient ConnectFtp = new WebClient();
                ConnectFtp.Credentials = new NetworkCredential(GetUserName, GetPassword);
                ConnectFtp.UploadFile(GetFtpUrl + GetFileName + ".json", @System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\ExportFile\\" + GetFileName + ".json");
            }
            catch (Exception Error)
            {


                string cs = GetConnectionString();

                DataSenderPlus dp = new DataSenderPlus();
                SqlConnection cn = new SqlConnection(cs);
                MainWindow mw = new MainWindow();

                cn.Open();
                string CorrectError = Error.ToString().Replace("'", "''");

                string Q_LogRecord = " 	 INSERT INTO [dbo].[LogRegisteration]( ConfigID , LogDate ,StatusID ,Comment ) " +
                                         " VALUES(@ConfigID, GetDate() , 1, " + "N' " + CorrectError.ToString() + "' ) ";

                SqlCommand LogRecord_cmd = new SqlCommand(Q_LogRecord, cn);

                LogRecord_cmd.Parameters.AddWithValue("@ConfigID", 1);

                LogRecord_cmd.ExecuteNonQuery();

                cn.Close();

                ErrorMessage = "Err" + " _ " + dp.PersianDate() + " _ " + DateTime.Now.ToShortTimeString();

            }
        }
    }
}
