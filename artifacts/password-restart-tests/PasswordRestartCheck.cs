using System;
using System.Diagnostics;
using System.IO;
using DataSenderPlusProject.Helpers;
class PasswordRestartCheck {
 static int Main(string[] args) {
  if(args.Length>0) {
   DatabaseConnectionSettings saved; string error;
   if(!DatabaseSettings.TryLoad(out saved,out error) || saved.Password!="synthetic-restart-only" || saved.Server!="synthetic-server") return 2;
   return 0;
  }
  string path=AppDomain.CurrentDomain.SetupInformation.ConfigurationFile;
  File.WriteAllText(path,"<configuration><appSettings/></configuration>");
  DatabaseSettings.Save(new DatabaseConnectionSettings {Server="synthetic-server",Database="synthetic-db",UserName="synthetic-user",Password="synthetic-restart-only"});
  if(File.ReadAllText(path).Contains("synthetic-restart-only")) throw new Exception("Plaintext password persisted");
  using(var process=Process.Start(new ProcessStartInfo {FileName=typeof(PasswordRestartCheck).Assembly.Location,Arguments="--read",UseShellExecute=false,CreateNoWindow=true})) {
   if(!process.WaitForExit(10000) || process.ExitCode!=0) throw new Exception("New-process decryption failed");
  }
  Console.WriteLine("PASS: password encrypted on save and correctly read by a fresh process under the same Windows account.");
  return 0;
 }
}