using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Reflection;

namespace DataSenderPlusProject.Models
{
    public static class Logger
    {
        

        private static string GetLogFolder()
        {
            string appPath = Path.GetDirectoryName(
                Assembly.GetExecutingAssembly().Location);

            return Path.Combine(appPath, "Logs");
        }

        private static void WriteLog(string level, string message)
        {
            try
            {
                string logFolder = GetLogFolder();

                if (!Directory.Exists(logFolder))
                {
                    Directory.CreateDirectory(logFolder);
                }

                string logFile = Path.Combine(
                    logFolder,
                    $"{DateTime.Now:yyyy-MM-dd}.log");

                string logText =
                    $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{level}] {message}";

                File.AppendAllText(logFile, logText + Environment.NewLine);
            }
            catch
            {
                // Logger نباید باعث توقف برنامه شود.
            }
        }

        public static void Info(string message)
        {
            WriteLog("INFO", message);
        }

        public static void Warning(string message)
        {
            WriteLog("WARNING", message);
        }

        public static void Error(string message)
        {
            WriteLog("ERROR", message);
        }
    }
}
