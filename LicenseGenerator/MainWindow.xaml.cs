using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml;
using LicenseGenerator.Models ;
using System.IO ;
using Newtonsoft.Json;

namespace LicenseGenerator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void btnGenerate_Click(object sender, RoutedEventArgs e)
        {
            // اگر تاریخ انتخاب نشده باشد
            if (dpExpireDate.SelectedDate == null)
            {
                MessageBox.Show("Please select Expire Date.");
                return;
            }

            // ساخت مدل لایسنس
            LicenseModel license = new LicenseModel();

            // کد سخت افزاری
            license.HardwareFingerprint = txtHardware.Text;

            // تاریخ انقضای لایسنس
            license.ExpireDate = dpExpireDate.SelectedDate.Value;

            // تاریخ آخرین بررسی
            license.LastCheckDate = DateTime.Now;

            // وضعیت فعال بودن لایسنس
            license.IsActive = chkIsActive.IsChecked == true;

            // تبدیل مدل به Json
            string json = JsonConvert.SerializeObject(
                license,
                Newtonsoft.Json.Formatting.Indented);

            // رمزنگاری Json
            string encryptText = CryptoHelper.Encrypt(json);

            // مسیر فایل
            string filePath = System.IO.Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "config.cache");

            // ذخیره فایل رمز شده
            File.WriteAllText(filePath, encryptText);

            MessageBox.Show("License Created Successfully.");
        }
    }
}
