using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataSenderPlusProject
{
    internal class DataSenderPlus
    {

        public string PersianDate()
        {
            PersianCalendar pc = new PersianCalendar();
            StringBuilder sb = new StringBuilder();
            DateTime thisDate = DateTime.Now;

            sb.Append(pc.GetYear(thisDate).ToString());
            sb.Append("/");
            sb.Append(pc.GetMonth(thisDate).ToString());
            sb.Append("/");
            sb.Append(pc.GetDayOfMonth(thisDate).ToString());


            return sb.ToString();

        }
    }
}
