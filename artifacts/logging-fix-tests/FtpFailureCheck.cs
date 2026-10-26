using System;
using System.IO;
using System.Reflection;
using DataSenderPlusProject.Models;
class FtpFailureCheck {
 static void Main() {
  var assembly=typeof(Logger).Assembly;
  var type=assembly.GetType("DataSenderPlusProject.C_JasonFile", true);
  var instance=Activator.CreateInstance(type,true);
  bool failed=false;
  try {
   type.GetMethod("F_SendFile2Ftp").Invoke(instance,new object[]{"ftp://127.0.0.1/", "test", "test", Path.Combine(AppDomain.CurrentDomain.BaseDirectory,Guid.NewGuid()+".json")});
  } catch(TargetInvocationException error) {
   failed=true;
   if(!(error.InnerException is System.Net.WebException)) throw;
   Logger.Error("Synthetic FTP failure. ConfigID=4"+Environment.NewLine+error.InnerException);
  }
  if(!failed) throw new Exception("FTP failure was swallowed");
  var entries=Logger.ReadRecent(10,true);
  if(entries.Count==0 || !entries[0].Message.Contains("Synthetic FTP failure. ConfigID=4") || !entries[0].Message.Contains("WebException")) throw new Exception("Error not readable by viewer");
  Console.WriteLine("PASS: missing export propagates FTP exception and full error is readable by log viewer.");
 }
}