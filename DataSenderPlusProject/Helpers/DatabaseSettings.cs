using DataSenderPlusProject.Models;
using System.Configuration;
using System;
using System.Data.SqlClient;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace DataSenderPlusProject.Helpers
{
    internal sealed class DatabaseConnectionSettings
    {
        public string Server { get; set; }
        public string Database { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public int CommandTimeoutSeconds { get; set; } = 300;

        public string BuildConnectionString()
        {
            return new SqlConnectionStringBuilder
            {
                DataSource = Server, InitialCatalog = Database, UserID = UserName,
                Password = Password, ConnectTimeout = 15, PersistSecurityInfo = false
            }.ConnectionString;
        }
    }

    internal static class DatabaseSettings
    {
        private const int DefaultCommandTimeoutSeconds = 300;
        private const string PasswordPrefix = "dpapi-user-v1:";

        public static bool CheckAccessPassword(string password)
        {
            using (var hash = SHA256.Create())
            {
                return Convert.ToBase64String(hash.ComputeHash(Encoding.UTF8.GetBytes(password)))
                    == "1fkbaXaD5TVcQd+TQpzsxkzqDLSITAWDm4eHI95T10E=";
            }
        }

        public static DatabaseConnectionSettings Load(bool decryptPassword = true)
        {
            ConfigurationManager.RefreshSection("appSettings");
            var values = ConfigurationManager.AppSettings;
            string encrypted = values["DatabasePasswordEncrypted"];
            string password = "";
            if (decryptPassword && !string.IsNullOrEmpty(encrypted))
            {
                try
                {
                    if (!encrypted.StartsWith(PasswordPrefix, StringComparison.Ordinal))
                        throw new CryptographicException();
                    password = Encoding.UTF8.GetString(ProtectedData.Unprotect(
                        Convert.FromBase64String(encrypted.Substring(PasswordPrefix.Length)),
                        null, DataProtectionScope.CurrentUser));
                }
                catch (Exception error) when (error is CryptographicException || error is FormatException)
                {
                    throw new InvalidOperationException("رمز ذخیره‌شده قابل بازیابی نیست؛ رمز دیتابیس را در تنظیمات دوباره وارد کنید.");
                }
            }
            return new DatabaseConnectionSettings
            {
                Server = values["DatabaseServer"] ?? "",
                Database = values["DatabaseName"] ?? "",
                UserName = values["DatabaseUserName"] ?? "",
                Password = password,
                CommandTimeoutSeconds = GetCommandTimeoutSeconds()
            };
        }

        public static void Save(DatabaseConnectionSettings settings)
        {
            if (string.IsNullOrWhiteSpace(settings.Server) || string.IsNullOrWhiteSpace(settings.Database)
                || string.IsNullOrWhiteSpace(settings.UserName) || settings.CommandTimeoutSeconds < 0)
                throw new ArgumentException("سرور، دیتابیس، نام کاربری و مهلت معتبر را وارد کنید.");

            string encrypted = PasswordPrefix + Convert.ToBase64String(ProtectedData.Protect(
                Encoding.UTF8.GetBytes(settings.Password ?? ""), null, DataProtectionScope.CurrentUser));
            var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            SetValue(config, "DatabaseServer", settings.Server);
            SetValue(config, "DatabaseName", settings.Database);
            SetValue(config, "DatabaseUserName", settings.UserName);
            SetValue(config, "DatabasePasswordEncrypted", encrypted);
            SetValue(config, "SqlCommandTimeoutSeconds", settings.CommandTimeoutSeconds.ToString(System.Globalization.CultureInfo.InvariantCulture));
            config.Save(ConfigurationSaveMode.Modified);
            ConfigurationManager.RefreshSection("appSettings");
        }

        private static void SetValue(Configuration config, string key, string value)
        {
            if (config.AppSettings.Settings[key] == null) config.AppSettings.Settings.Add(key, value);
            else config.AppSettings.Settings[key].Value = value;
        }

        public static string MigrateLegacyConfig()
        {
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config.txt");
            if (!File.Exists(path)) return null;
            ConfigurationManager.RefreshSection("appSettings");
            // Never overwrite settings already saved through the form.
            var values = ConfigurationManager.AppSettings;
            if (!string.IsNullOrWhiteSpace(values["DatabaseServer"])
                && !string.IsNullOrWhiteSpace(values["DatabaseName"])
                && !string.IsNullOrWhiteSpace(values["DatabaseUserName"])
                && !string.IsNullOrEmpty(values["DatabasePasswordEncrypted"]))
            {
                // Existing settings take precedence; a leftover file must not block startup.
                Load();
                return "تنظیمات جدید استفاده می‌شود. فایل قدیمی Config.txt هنوز وجود دارد و ممکن است رمز متنی داشته باشد؛ پس از بررسی آن را حذف کنید.";
            }
            string[] lines = File.ReadAllLines(path);
            if (lines.Length < 4 || Array.FindIndex(lines, 4, lines.Length - 4, line => !string.IsNullOrWhiteSpace(line)) >= 0)
                throw new InvalidOperationException("ساختار Config.txt معتبر نیست؛ انتقال خودکار انجام نشد.");
            var settings = new DatabaseConnectionSettings
            {
                Server = lines[0], Database = lines[1], UserName = lines[2], Password = lines[3],
                CommandTimeoutSeconds = GetCommandTimeoutSeconds()
            };
            Save(settings);
            var saved = Load();
            if (saved.BuildConnectionString() != settings.BuildConnectionString())
                throw new InvalidOperationException("بررسی انتقال تنظیمات ناموفق بود؛ فایل قدیمی حفظ شد.");
            try
            {
                File.Delete(path);
            }
            catch (Exception error) when (error is IOException || error is UnauthorizedAccessException)
            {
                return "تنظیمات منتقل شد، اما حذف Config.txt ممکن نشد. برای حذف رمز متنی، فایل قدیمی را پس از بررسی دستی حذف کنید.";
            }
            return null;
        }

        public static string DescribeError(Exception error)
        {
            if (error is ConfigurationErrorsException)
                return "ساختار فایل config کنار برنامه معتبر نیست یا فایل قابل خواندن نیست.";
            if (error is CryptographicException || error is InvalidOperationException)
                return error is InvalidOperationException ? error.Message
                    : "رمزگذاری ویندوز در دسترس نیست؛ برنامه را با حساب ویندوز دارای پروفایل فعال اجرا کنید.";
            if (error is UnauthorizedAccessException || error is IOException)
                return "دسترسی خواندن یا نوشتن فایل تنظیمات امکان‌پذیر نیست. مجوز پوشهٔ برنامه را بررسی کنید.";
            if (error is ArgumentException)
                return "مشخصات دیتابیس در فایل قدیمی ناقص یا نامعتبر است.";
            return "خواندن تنظیمات ناموفق بود. نوع خطا: " + error.GetType().Name;
        }

        public static int GetCommandTimeoutSeconds()
        {
            try
            {
                // Reload before each execution so edits apply without restarting the app.
                ConfigurationManager.RefreshSection("appSettings");
                string value = ConfigurationManager.AppSettings["SqlCommandTimeoutSeconds"];
                if (value == null) return DefaultCommandTimeoutSeconds;

                int seconds;
                if (int.TryParse(value, out seconds) && seconds >= 0)
                    return seconds;

                Logger.Warning("Invalid SqlCommandTimeoutSeconds; using 300 seconds. Expected a non-negative integer.");
            }
            catch (ConfigurationErrorsException error)
            {
                Logger.Warning("Cannot read SqlCommandTimeoutSeconds; using 300 seconds. " + error.Message);
            }

            return DefaultCommandTimeoutSeconds;
        }
    }
}
