using System;
using System.Data.SqlClient;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace DataSenderPlusProject.Helpers
{
    internal static class ConnectionTester
    {
        internal static string CreateDatabaseTestConnectionString(DatabaseConnectionSettings settings)
        {
            if (string.IsNullOrWhiteSpace(settings.Server) || string.IsNullOrWhiteSpace(settings.Database)
                || string.IsNullOrWhiteSpace(settings.UserName))
                throw new ArgumentException("آدرس سرور، نام دیتابیس و نام کاربری را وارد کنید.");
            var builder = new SqlConnectionStringBuilder(settings.BuildConnectionString())
            {
                ConnectTimeout = 8, Pooling = false, ConnectRetryCount = 0
            };
            return builder.ConnectionString;
        }

        public static async Task TestDatabaseAsync(DatabaseConnectionSettings settings)
        {
            using (var connection = new SqlConnection(CreateDatabaseTestConnectionString(settings)))
            using (var command = new SqlCommand("SELECT 1", connection))
            {
                command.CommandTimeout = 8;
                await connection.OpenAsync().ConfigureAwait(false);
                object result = await command.ExecuteScalarAsync().ConfigureAwait(false);
                if (Convert.ToInt32(result) != 1)
                    throw new InvalidOperationException("پاسخ تست دیتابیس معتبر نبود.");
            }
        }

        internal static Uri CreateFtpTestUri(string folder)
        {
            Uri address;
            if (!Uri.TryCreate(folder, UriKind.Absolute, out address) || address.Scheme != Uri.UriSchemeFtp
                || string.IsNullOrEmpty(address.Host) || !string.IsNullOrEmpty(address.UserInfo)
                || !string.IsNullOrEmpty(address.Query) || !string.IsNullOrEmpty(address.Fragment))
                throw new ArgumentException("آدرس FTP را مانند ftp://server/folder وارد کنید؛ کاربر و رمز را در کادرهای جدا بنویسید.");
            string fileName = "SmartDataSync_Test_" + DateTime.UtcNow.ToString("yyyyMMdd_HHmmss")
                + "_" + Guid.NewGuid().ToString("N") + ".txt";
            return new Uri(address.AbsoluteUri.TrimEnd('/') + "/" + fileName);
        }

        public static Task<string> UploadFtpTestAsync(string folder, string userName, string password)
        {
            Uri target = CreateFtpTestUri(folder);
            return Task.Run(() =>
            {
                byte[] content = new UTF8Encoding(false).GetBytes("تست");
                var request = (FtpWebRequest)WebRequest.Create(target);
                request.Method = WebRequestMethods.Ftp.UploadFile;
                request.Credentials = new NetworkCredential(userName, password);
                request.Timeout = 15000;
                request.ReadWriteTimeout = 15000;
                request.UseBinary = true;
                request.UsePassive = true;
                request.KeepAlive = false;
                request.ContentLength = content.Length;
                using (var output = request.GetRequestStream()) output.Write(content, 0, content.Length);
                using (var response = (FtpWebResponse)request.GetResponse())
                {
                    if (response.StatusCode != FtpStatusCode.ClosingData && response.StatusCode != FtpStatusCode.FileActionOK)
                        throw new InvalidOperationException("سرور، تکمیل انتقال فایل تست را تأیید نکرد.");
                }
                return System.IO.Path.GetFileName(target.LocalPath);
            });
        }

        public static string DescribeDatabaseFailure(SqlException error)
        {
            switch (error.Number)
            {
                case 18456: return "ورود به دیتابیس ناموفق بود؛ نام کاربری یا رمز را بررسی کنید.";
                case 4060: return "دیتابیس در دسترس این کاربر نیست؛ نام دیتابیس و مجوز دسترسی را بررسی کنید.";
                case -2: return "مهلت تست تمام شد؛ آدرس سرور و دسترسی شبکه را بررسی کنید.";
                default: return $"ارتباط دیتابیس برقرار نشد (SQL {error.Number})؛ سرور، شبکه و مشخصات ورود را بررسی کنید.";
            }
        }

        public static string DescribeFtpFailure(WebException error)
        {
            using (var response = error.Response as FtpWebResponse)
            {
                if (response != null)
                {
                    if (response.StatusCode == FtpStatusCode.NotLoggedIn)
                        return "ورود به FTP ناموفق بود؛ نام کاربری و رمز را بررسی کنید (FTP 530).";
                    if (response.StatusCode == FtpStatusCode.ActionNotTakenFileUnavailable)
                        return "مسیر FTP موجود نیست یا کاربر مجوز نوشتن فایل ندارد (FTP 550).";
                    return $"ارسال فایل تست ناموفق بود (FTP {(int)response.StatusCode}). مسیر، مجوز نوشتن و اتصال داده را بررسی کنید.";
                }
            }
            if (error.Status == WebExceptionStatus.Timeout)
                return "مهلت تست FTP تمام شد؛ سرور، شبکه و تنظیمات فایروال را بررسی کنید.";
            if (error.Status == WebExceptionStatus.NameResolutionFailure)
                return "نام سرور FTP پیدا نشد؛ آدرس سرور را بررسی کنید.";
            return "اتصال FTP برقرار نشد؛ آدرس، پورت و دسترسی شبکه را بررسی کنید.";
        }
    }
}
