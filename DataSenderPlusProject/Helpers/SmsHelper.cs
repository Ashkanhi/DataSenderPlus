using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kavenegar ;

namespace DataSenderPlusProject.Helpers
{
    public class SmsHelper
    {
        // کلید کاوه نگار
        private const string ApiKey =
            "716A3369634B6B65735646765A632F303835476447513D3D";

        // شماره ارسال کننده
        private const string Sender =
            "1000880660";

        public static void SendError(string errorMessage)
        {
            try
            {
                C_JasonFile json = new C_JasonFile();

                using (SqlConnection cn = new SqlConnection(json.GetConnectionString()))
                {
                    cn.Open();

                    SqlCommand cmd = new SqlCommand(
                        @"SELECT TOP 1 AdminMobileNumber
                  FROM SystemConfiguration", cn);

                    string mobile = Convert.ToString(cmd.ExecuteScalar());

                    if (string.IsNullOrWhiteSpace(mobile))
                        return;

                    string message =
                        "DataSender Error" +
                        "\n" +
                        errorMessage;

                    Send(mobile, message);
                }
            }
            catch
            {
                // اگر ارسال پیامک هم خطا داد
                // چیزی انجام نده
            }
        }

        public static bool Send(string mobile, string message)
        {
            try
            {
                KavenegarApi api = new KavenegarApi(ApiKey);

                api.Send(
                    Sender,
                    mobile,
                    message);

                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
