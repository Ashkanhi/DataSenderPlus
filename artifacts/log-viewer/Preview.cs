using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DataSenderPlusProject;
using DataSenderPlusProject.Models;
class Preview {
 [STAThread] static void Main() {
  var window = new LogViewerWindow();
  var grid = (DataGrid)window.FindName("LogGrid");
  var entries = new LogEntry[10];
  for(int i=0;i<10;i++) entries[i] = new LogEntry { Timestamp="2026-10-12 16:05:24", Level=i==0?"ERROR":"INFO", Message=i==0?"System.Exception: Sample error\n   at DataSenderPlusProject.MainWindow.F_UploadFtp(Int32 configId)":"Stored Procedure executed successfully. Rows Count : 333034" };
  grid.ItemsSource=entries; grid.SelectedIndex=0;
  ((TextBlock)window.FindName("StatusText")).Text="۱۰ لاگ آخر — جدیدترین در بالا";
  window.WindowStartupLocation=WindowStartupLocation.Manual; window.Left=-10000; window.ShowActivated=false; window.Show(); window.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.ApplicationIdle, new Action(() => {})); grid.ItemsSource=entries; grid.SelectedIndex=0; ((TextBlock)window.FindName("StatusText")).Text="۱۰ لاگ آخر — جدیدترین در بالا"; var content=(FrameworkElement)window.Content; ((Grid)content).Background=window.Background;
  content.Measure(new Size(868,500)); content.Arrange(new Rect(0,0,868,500)); content.UpdateLayout();
  var bitmap=new RenderTargetBitmap(900,532,96,96,PixelFormats.Pbgra32); bitmap.Render(window);
  var png=new PngBitmapEncoder(); png.Frames.Add(BitmapFrame.Create(bitmap));
  using(var file=File.Create(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"log-viewer-preview.png"))) png.Save(file);
  window.Close(); Console.WriteLine("PASS: WPF window initialized and rendered with 10 entries.");
 }
}