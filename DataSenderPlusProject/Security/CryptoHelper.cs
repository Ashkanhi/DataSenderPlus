using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace DataSenderPlusProject
{
    public static class CryptoHelper
    {
        // ==========================================
        // کلید رمزنگاری (AES Key)
        // ==========================================
        // این رشته باید دقیقا 32 کاراکتر باشد.
        // 32 بایت = AES-256
        // این کلید باید هم در LicenseGenerator
        // و هم در DataSender دقیقا یکسان باشد.
        private static readonly byte[] Key =
            Encoding.UTF8.GetBytes("12345678901234567890123456789012");

        // ==========================================
        // IV
        // ==========================================
        // این رشته باید دقیقا 16 کاراکتر باشد.
        // IV باعث می‌شود خروجی رمزنگاری قابل پیش‌بینی نباشد.
        // این هم باید در هر دو پروژه یکسان باشد.
        private static readonly byte[] IV =
            Encoding.UTF8.GetBytes("1234567890123456");

        // ==========================================
        // Encrypt
        // متن ساده را به متن رمز شده تبدیل می‌کند.
        // ==========================================
        public static string Encrypt(string plainText)
        {
            // ساخت موتور AES
            using (Aes aes = Aes.Create())
            {
                // تنظیم کلید
                aes.Key = Key;

                // تنظیم IV
                aes.IV = IV;

                // ساخت عملیات Encrypt
                ICryptoTransform encryptor =
                    aes.CreateEncryptor(aes.Key, aes.IV);

                // حافظه موقت برای نگهداری خروجی
                using (MemoryStream ms = new MemoryStream())
                {
                    // نوشتن اطلاعات رمز شده داخل حافظه
                    using (CryptoStream cs =
                        new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                    {
                        // تبدیل رشته به Stream
                        using (StreamWriter sw = new StreamWriter(cs))
                        {
                            // نوشتن متن داخل AES
                            sw.Write(plainText);
                        }
                    }

                    // خروجی AES بایت است.
                    // چون داخل فایل نمی‌توان بایت خام ذخیره کرد،
                    // آن را به Base64 تبدیل می‌کنیم.
                    return Convert.ToBase64String(ms.ToArray());
                }
            }
        }

        // ==========================================
        // Decrypt
        // متن رمز شده را به متن اصلی برمی‌گرداند.
        // ==========================================
        public static string Decrypt(string cipherText)
        {
            // ساخت موتور AES
            using (Aes aes = Aes.Create())
            {
                // همان کلیدی که موقع Encrypt استفاده کردیم
                aes.Key = Key;

                // همان IV
                aes.IV = IV;

                // ساخت عملیات Decrypt
                ICryptoTransform decryptor =
                    aes.CreateDecryptor(aes.Key, aes.IV);

                // تبدیل Base64 به Byte[]
                using (MemoryStream ms =
                    new MemoryStream(Convert.FromBase64String(cipherText)))
                {
                    // خواندن اطلاعات رمز شده
                    using (CryptoStream cs =
                        new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                    {
                        // تبدیل Stream به رشته
                        using (StreamReader sr = new StreamReader(cs))
                        {
                            // برگرداندن متن اصلی
                            return sr.ReadToEnd();
                        }
                    }
                }
            }
        }
    }
}
