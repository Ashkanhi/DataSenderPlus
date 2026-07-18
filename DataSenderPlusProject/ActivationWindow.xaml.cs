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
using System.Windows.Shapes;

namespace DataSenderPlusProject
{
    /// <summary>
    /// Interaction logic for ActivationWindow.xaml
    /// </summary>
    public partial class ActivationWindow : Window
    {
        public ActivationWindow(string hardwareId)
        {
            InitializeComponent();
            txtHardware.Text = hardwareId;
        }
        private void btnCopy_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(txtHardware.Text);

            MessageBox.Show("Hardware ID Copied.");
        }

        private void btnExit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}
