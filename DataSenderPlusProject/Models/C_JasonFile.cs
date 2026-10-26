using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace DataSenderPlusProject
{
    internal class C_JasonFile
    {
        public string ServerPath, UserName, Password;

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
            return DataSenderPlusProject.Helpers.DatabaseSettings.Load().BuildConnectionString();
        }

        public void F_SendFile2Ftp(string ftpUrl, string userName, string password, string localFilePath)
        {
            // Let the caller record the same failure in the file log and database.
            using (var client = new WebClient())
            {
                client.Credentials = new NetworkCredential(userName, password);
                string destination = ftpUrl.TrimEnd('/') + "/" + Uri.EscapeDataString(Path.GetFileName(localFilePath));
                client.UploadFile(destination, localFilePath);
            }
        }
    }
}