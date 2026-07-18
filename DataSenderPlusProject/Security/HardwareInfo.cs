using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Management;
using System.Security.Cryptography;


namespace DataSenderPlusProject.Security
{
    public class HardwareInfo
    {
        public string GetDiskSerial()
        {
            ManagementObjectSearcher searcher =
                new ManagementObjectSearcher("SELECT SerialNumber FROM Win32_PhysicalMedia");

            foreach (ManagementObject obj in searcher.Get())
            {
                string serial = obj["SerialNumber"]?.ToString();

                if (!string.IsNullOrWhiteSpace(serial))
                {
                    return serial.Trim();
                }
            }

            return "";
        }

        public string GetCpuId()
        {
            ManagementObjectSearcher searcher =
                new ManagementObjectSearcher("SELECT ProcessorId FROM Win32_Processor");

            foreach (ManagementObject obj in searcher.Get())
            {
                return obj["ProcessorId"].ToString();
            }

            return "";
        }

        public string GetMotherboardSerial()
        {
            ManagementObjectSearcher searcher =
                new ManagementObjectSearcher("SELECT SerialNumber FROM Win32_BaseBoard");

            foreach (ManagementObject obj in searcher.Get())
            {
                return obj["SerialNumber"]?.ToString() ?? "";
            }

            return "";
        }

        public string GetHardwareFingerprint()
        {
            string rawData =
                GetCpuId() +
                GetMotherboardSerial() +
                GetDiskSerial();

            using (SHA256 sha = SHA256.Create())
            {
                byte[] bytes = Encoding.UTF8.GetBytes(rawData);
                byte[] hash = sha.ComputeHash(bytes);

                StringBuilder sb = new StringBuilder();

                foreach (byte b in hash)
                {
                    sb.Append(b.ToString("X2"));
                }

                return sb.ToString();
            }
        }
    }
}
