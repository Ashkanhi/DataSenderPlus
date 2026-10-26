using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using DataSenderPlusProject;
class SettingsPreview {
 [STAThread] static void Main() {
  var window=new SettingsWindow();
  window.WindowStartupLocation=WindowStartupLocation.Manual; window.Left=-10000; window.ShowActivated=false; window.Show();
  var access=(PasswordBox)window.FindName("AccessPassword"); var unlock=(Button)window.FindName("UnlockButton");
  var editor=(StackPanel)window.FindName("EditorPanel");
  access.Password="incorrect"; unlock.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
  if(editor.Visibility!=Visibility.Collapsed) throw new Exception("Gate bypass");
  access.Password="RWP@1400"; unlock.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
  if(editor.Visibility!=Visibility.Visible || access.Password!="") throw new Exception("Unlock failed");
  ((TextBox)window.FindName("ServerInput")).Text="localhost\\SQLEXPRESS";
  ((TextBox)window.FindName("DatabaseInput")).Text="SampleDatabase";
  ((TextBox)window.FindName("UserInput")).Text="sender_user";
  var password=(PasswordBox)window.FindName("DatabasePassword"); var visible=(TextBox)window.FindName("VisiblePassword");
  var toggle=(CheckBox)window.FindName("ShowPassword"); password.Password="sample-only";
  toggle.IsChecked=true; if(visible.Text!="sample-only") throw new Exception("Reveal failed");
  visible.Text="edited-sample"; toggle.IsChecked=false;
  if(password.Password!="edited-sample" || visible.Text!="") throw new Exception("Hide/edit failed");
  window.Dispatcher.Invoke(DispatcherPriority.ApplicationIdle,new Action(()=>{})); window.UpdateLayout();
  var image=new RenderTargetBitmap((int)window.ActualWidth,(int)window.ActualHeight,96,96,PixelFormats.Pbgra32); image.Render(window);
  var encoder=new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(image));
  using(var file=File.Create(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"settings-preview.png"))) encoder.Save(file);
  window.Close(); if(password.Password!="" || visible.Text!="") throw new Exception("Close cleanup failed");
  Console.WriteLine("PASS: wrong/correct access password, masked/revealed editing, close cleanup, and rendered form.");
 }
}