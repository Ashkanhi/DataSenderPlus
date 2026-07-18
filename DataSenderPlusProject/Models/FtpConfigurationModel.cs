using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataSenderPlusProject.Models
{
    public class FtpConfigurationModel
    {
        public string ServerAddress { get; set; }

        public string UserName { get; set; }

        public string Password { get; set; }

        public string FileName { get; set; }

        public int IntervalValue { get; set; }

        public int SendFlag { get; set; }

        public string FolderPath { get; set; }
    }
}
