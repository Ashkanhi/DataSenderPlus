using System;
using System.Diagnostics;
using System.IO;
using System.ServiceProcess;
using System.Threading;

namespace RainProcessWatcher
{
    public partial class ProcessWatcherService : ServiceBase
    {
        
        private Timer _timer;
        private string _configFile;        
        private string _processName;        
        private string _exePath;        
        private int _checkInterval = 120;

        public ProcessWatcherService()
        {
            InitializeComponent();
        }

        /// <summary>
        /// نوشتن لاگ
        /// </summary>
        private void WriteLog(string message)
        {
            try
            {
                string logPath = Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "WatcherLog_" + DateTime.Now.ToString("yyyyMMdd") + ".txt");

                File.AppendAllText(
                    logPath,
                    DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") +
                    "    " +
                    message +
                    Environment.NewLine);
            }
            catch
            {
            }
        }

        protected override void OnStart(string[] args)
        {
            WriteLog("========== Service Started ==========");

            // مسیر فایل تنظیمات
            _configFile = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "watcher.config");

            // خواندن فایل تنظیمات
            LoadConfig();

            WriteLog("Timer Started");

            // ایجاد تایمر
            _timer = new Timer(
                CheckProcess,
                null,
                0,
                _checkInterval * 1000);
        }

        protected override void OnStop()
        {
            WriteLog("Service Stopped");

            if (_timer != null)
            {
                _timer.Dispose();
            }
        }

        /// <summary>
        /// خواندن فایل تنظیمات
        /// </summary>
        private void LoadConfig()
        {
            WriteLog("Loading Config...");

            if (!File.Exists(_configFile))
            {
                WriteLog("Config File Not Found");
                return;
            }

            string[] lines = File.ReadAllLines(_configFile);

            foreach (string line in lines)
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                if (line.StartsWith("#"))
                    continue;

                string[] item = line.Split('=');

                if (item.Length != 2)
                    continue;

                switch (item[0].Trim())
                {
                    case "ProcessName":
                        _processName = item[1].Trim();
                        WriteLog("ProcessName : " + _processName);
                        break;

                    case "ExePath":
                        _exePath = item[1].Trim();
                        WriteLog("ExePath : " + _exePath);
                        break;

                    case "CheckInterval":
                        _checkInterval = Convert.ToInt32(item[1].Trim());
                        WriteLog("CheckInterval : " + _checkInterval);
                        break;
                }
            }
        }

        /// <summary>
        /// بررسی اجرا بودن Process
        /// </summary>
        private void CheckProcess(object state)
        {
            try
            {
                WriteLog("--------------------------------------");
                WriteLog("Checking Process...");

                if (string.IsNullOrWhiteSpace(_processName))
                {
                    WriteLog("ProcessName Is Empty");
                    return;
                }

                if (string.IsNullOrWhiteSpace(_exePath))
                {
                    WriteLog("ExePath Is Empty");
                    return;
                }

                Process[] processes =
                    Process.GetProcessesByName(_processName);

                WriteLog("Running Process Count : " + processes.Length);

                if (processes.Length > 0)
                {
                    WriteLog("Process Already Running");
                    return;
                }

                WriteLog("Process Not Running");

                if (!File.Exists(_exePath))
                {
                    WriteLog("Exe File Not Found");
                    return;
                }

                WriteLog("Starting Process...");

                Process.Start(_exePath);

                WriteLog("Process Started Successfully");
            }
            catch (Exception ex)
            {
                WriteLog("ERROR : " + ex.Message);
                WriteLog(ex.ToString());
            }
        }
    }
}