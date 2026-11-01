using DataSenderPlusProject.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;

namespace DataSenderPlusProject.Helpers
{
    internal static class FtpConfigurationStore
    {
        public static List<FtpConfigurationModel> Load(string connectionString)
        {
            using (var connection = new SqlConnection(connectionString))
            using (var command = new SqlCommand(
                @"SELECT ConfigID, ServerAddress, UserName, Password, FileName,
                         IntervalValue, SendFlag, FolderPath, OutputTypeID
                  FROM FtpConfiguration ORDER BY ConfigID", connection))
            {
                command.CommandTimeout = 15;
                connection.Open();
                using (var reader = command.ExecuteReader()) return ReadRows(reader);
            }
        }

        internal static List<FtpConfigurationModel> ReadRows(IDataReader reader)
        {
            var rows = new List<FtpConfigurationModel>();
            while (reader.Read())
            {
                var settings = ReadRow(reader);
                if (settings != null) rows.Add(settings);
            }
            return rows;
        }

        internal static FtpConfigurationModel ReadRow(IDataRecord reader)
        {
            // A missing key cannot be safely edited. Keep other records available.
            if (reader["ConfigID"] == DBNull.Value || Convert.ToInt32(reader["ConfigID"]) < 1)
            {
                Logger.Warning("Ignored a FtpConfiguration row without a valid ConfigID.");
                return null;
            }
            return new FtpConfigurationModel
            {
                ConfigID = Convert.ToInt32(reader["ConfigID"]),
                ServerAddress = Convert.ToString(reader["ServerAddress"]),
                UserName = Convert.ToString(reader["UserName"]),
                Password = Convert.ToString(reader["Password"]),
                FileName = Convert.ToString(reader["FileName"]),
                // Unset fields are editable; reading never changes the database.
                IntervalValue = ReadIntOrUnset(reader["IntervalValue"]),
                SendFlag = ReadIntOrUnset(reader["SendFlag"]),
                FolderPath = Convert.ToString(reader["FolderPath"]),
                OutputTypeID = ReadIntOrUnset(reader["OutputTypeID"])
            };
        }

        private static int ReadIntOrUnset(object value)
        {
            return value == DBNull.Value || value == null ? 0 : Convert.ToInt32(value);
        }

        public static void Validate(FtpConfigurationModel settings)
        {
            if (settings.ConfigID < 1) throw new ArgumentException("فایل ارسالی را انتخاب کنید.");
            if (string.IsNullOrWhiteSpace(settings.FileName)
                || settings.FileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0
                || settings.FileName == "." || settings.FileName == ".."
                || settings.FileName.EndsWith(".") || settings.FileName.EndsWith(" "))
                throw new ArgumentException("نام فایل باید معتبر و بدون مسیر باشد.");
            if (settings.IntervalValue < 1) throw new ArgumentException("بازهٔ ارسال باید حداقل یک دقیقه باشد.");
            if (settings.OutputTypeID < 1 || settings.OutputTypeID > 4)
                throw new ArgumentException("فرمت خروجی را انتخاب کنید.");
            if (settings.SendFlag != 1 && settings.SendFlag != 2)
                throw new ArgumentException("روش ارسال را انتخاب کنید: ۱ برای آپلود FTP یا ۲ برای ذخیرهٔ لوکال.");
            if (!string.IsNullOrWhiteSpace(settings.FolderPath) && !Path.IsPathRooted(settings.FolderPath))
                throw new ArgumentException("مسیر پوشهٔ خروجی باید کامل باشد؛ برای مسیر پیش‌فرض آن را خالی بگذارید.");
            if (settings.SendFlag == 1)
            {
                Uri address;
                if (!Uri.TryCreate(settings.ServerAddress, UriKind.Absolute, out address)
                    || address.Scheme != Uri.UriSchemeFtp || string.IsNullOrEmpty(address.Host)
                    || !string.IsNullOrEmpty(address.UserInfo) || !string.IsNullOrEmpty(address.Query)
                    || !string.IsNullOrEmpty(address.Fragment))
                    throw new ArgumentException("آدرس FTP را مانند ftp://server/folder وارد کنید؛ نام کاربری و رمز را در کادرهای جدا بنویسید.");
            }
        }

        internal static SqlCommand CreateUpdateCommand(SqlConnection connection, FtpConfigurationModel settings)
        {
            Validate(settings);
            var command = new SqlCommand(
                @"UPDATE FtpConfiguration SET ServerAddress=@ServerAddress, UserName=@UserName,
                  Password=@Password, FileName=@FileName, IntervalValue=@IntervalValue,
                  SendFlag=@SendFlag, FolderPath=@FolderPath, OutputTypeID=@OutputTypeID
                  WHERE ConfigID=@ConfigID", connection);
            command.CommandTimeout = 15;
            command.Parameters.Add("@ConfigID", SqlDbType.Int).Value = settings.ConfigID;
            command.Parameters.Add("@ServerAddress", SqlDbType.NVarChar, -1).Value = settings.ServerAddress ?? "";
            command.Parameters.Add("@UserName", SqlDbType.NVarChar, -1).Value = settings.UserName ?? "";
            command.Parameters.Add("@Password", SqlDbType.NVarChar, -1).Value = settings.Password ?? "";
            command.Parameters.Add("@FileName", SqlDbType.NVarChar, -1).Value = settings.FileName;
            command.Parameters.Add("@IntervalValue", SqlDbType.Int).Value = settings.IntervalValue;
            command.Parameters.Add("@SendFlag", SqlDbType.Int).Value = settings.SendFlag;
            command.Parameters.Add("@FolderPath", SqlDbType.NVarChar, -1).Value = settings.FolderPath ?? "";
            command.Parameters.Add("@OutputTypeID", SqlDbType.Int).Value = settings.OutputTypeID;
            return command;
        }

        public static void Save(FtpConfigurationModel settings, string connectionString)
        {
            using (var connection = new SqlConnection(connectionString))
            using (var command = CreateUpdateCommand(connection, settings))
            {
                connection.Open();
                if (command.ExecuteNonQuery() == 0)
                    throw new InvalidOperationException("رکورد انتخاب‌شده دیگر در FtpConfiguration موجود نیست؛ فهرست را دوباره بخوانید.");
            }
        }
    }
}
