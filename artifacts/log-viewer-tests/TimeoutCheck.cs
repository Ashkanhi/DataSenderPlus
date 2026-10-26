using System;
using System.IO;
using DataSenderPlusProject.Helpers;
namespace DataSenderPlusProject.Models { public static class Logger { public static void Warning(string message) {} } }
class TimeoutCheck {
 static void Verify(string setting, int expected) {
  File.WriteAllText(AppDomain.CurrentDomain.SetupInformation.ConfigurationFile, "<configuration><appSettings>"+setting+"</appSettings></configuration>");
  int actual=DatabaseSettings.GetCommandTimeoutSeconds();
  if(actual!=expected) throw new Exception("Expected "+expected+", got "+actual);
 }
 static void Main() {
  Verify("",300);
  Verify("<add key='SqlCommandTimeoutSeconds' value='600'/>",600);
  Verify("<add key='SqlCommandTimeoutSeconds' value='120'/>",120);
  Verify("<add key='SqlCommandTimeoutSeconds' value='0'/>",0);
  Verify("<add key='SqlCommandTimeoutSeconds' value='-1'/>",300);
  Verify("<add key='SqlCommandTimeoutSeconds' value='invalid'/>",300);
  Console.WriteLine("PASS: configuration loading, live refresh, zero, and missing/invalid defaults.");
 }
}