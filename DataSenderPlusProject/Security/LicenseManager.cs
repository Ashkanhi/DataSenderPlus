using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json;

namespace DataSenderPlusProject.Security
{
    public class LicenseManager
    {
        private readonly string _licensePath;

        public LicenseManager()
        {
            string appPath = Path.GetDirectoryName(
                System.Reflection.Assembly.GetExecutingAssembly().Location);

            _licensePath = Path.Combine(appPath, "config.cache");
        }

        public bool LicenseExists()
        {
            return File.Exists(_licensePath);
        }

  
            public void SaveLicense(LicenseModel license)
        {
            string json =
                Newtonsoft.Json.JsonConvert.SerializeObject(license);

            File.WriteAllText(_licensePath, json);
        }
        
        public LicenseModel LoadLicense()
        {
            if (!File.Exists(_licensePath))
            {
                return null;
            }

            string json = File.ReadAllText(_licensePath);

            LicenseModel license =
                JsonConvert.DeserializeObject<LicenseModel>(json);

            return license;
        }
    }
}
