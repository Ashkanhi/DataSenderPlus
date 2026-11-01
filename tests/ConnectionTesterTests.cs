using DataSenderPlusProject.Helpers;
using System;
using System.Data.SqlClient;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

internal static class ConnectionTesterTests
{
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }

    private sealed class TestFtpServer : IDisposable
    {
        private readonly TcpListener control = new TcpListener(IPAddress.Loopback, 0);
        private TcpListener data;
        private readonly Task worker;
        private readonly bool denyLogin, denyWrite;
        public string FileName, UserName;
        public byte[] Payload;
        public string Address { get; }

        public TestFtpServer(bool denyLogin = false, bool denyWrite = false)
        {
            this.denyLogin = denyLogin; this.denyWrite = denyWrite;
            control.Start();
            Address = "ftp://127.0.0.1:" + ((IPEndPoint)control.LocalEndpoint).Port + "/incoming";
            worker = Task.Run(() => Serve());
        }

        private void Serve()
        {
            using (var client = control.AcceptTcpClient())
            using (var stream = client.GetStream())
            using (var reader = new StreamReader(stream, Encoding.UTF8, false, 1024, true))
            using (var writer = new StreamWriter(stream, new UTF8Encoding(false), 1024, true) { NewLine = "\r\n", AutoFlush = true })
            {
                client.ReceiveTimeout = 10000; client.SendTimeout = 10000;
                writer.WriteLine("220 Local test FTP");
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    int split = line.IndexOf(' ');
                    string verb = split < 0 ? line : line.Substring(0, split);
                    string argument = split < 0 ? "" : line.Substring(split + 1);
                    switch (verb.ToUpperInvariant())
                    {
                        case "USER": UserName = argument; writer.WriteLine("331 Password required"); break;
                        case "PASS":
                            writer.WriteLine(denyLogin ? "530 Login rejected" : "230 Logged in");
                            if (denyLogin) return;
                            break;
                        case "PWD": writer.WriteLine("257 \"/\" current directory"); break;
                        case "CWD": writer.WriteLine("250 Directory changed"); break;
                        case "TYPE": case "OPTS": writer.WriteLine("200 OK"); break;
                        case "PASV": case "EPSV":
                            data = new TcpListener(IPAddress.Loopback, 0); data.Start();
                            int port = ((IPEndPoint)data.LocalEndpoint).Port;
                            writer.WriteLine(verb == "PASV" ? "227 Entering Passive Mode (127,0,0,1," + (port / 256) + "," + (port % 256) + ")" : "229 Entering Extended Passive Mode (|||" + port + "|)");
                            break;
                        case "STOR":
                            if (denyWrite) { writer.WriteLine("550 Permission denied"); return; }
                            FileName = argument;
                            writer.WriteLine("150 Opening binary data connection");
                            using (var incoming = data.AcceptTcpClient())
                            using (var input = incoming.GetStream())
                            using (var memory = new MemoryStream())
                            {
                                incoming.ReceiveTimeout = 10000;
                                input.CopyTo(memory); Payload = memory.ToArray();
                            }
                            writer.WriteLine("226 Transfer complete");
                            return;
                        default: writer.WriteLine("200 OK"); break;
                    }
                }
            }
        }

        public void Complete()
        {
            Check(worker.Wait(10000), "FTP server did not finish");
        }

        public void Dispose() { data?.Stop(); control.Stop(); }
    }

    private static void Main()
    {
        var formValues = new DatabaseConnectionSettings
        {
            Server = "unsaved-server", Database = "unsaved-db", UserName = "unsaved-user", Password = "synthetic;secret"
        };
        var builder = new SqlConnectionStringBuilder(ConnectionTester.CreateDatabaseTestConnectionString(formValues));
        Check(builder.DataSource == formValues.Server && builder.InitialCatalog == formValues.Database
            && builder.UserID == formValues.UserName && builder.Password == formValues.Password, "SQL test uses current unsaved form values");
        Check(builder.ConnectTimeout == 8 && !builder.Pooling, "Bounded SQL test uses a fresh connection");
        try { ConnectionTester.TestDatabaseAsync(new DatabaseConnectionSettings()).GetAwaiter().GetResult(); throw new Exception("Missing settings accepted"); }
        catch (ArgumentException) { }
        var first = ConnectionTester.CreateFtpTestUri("ftp://example.test/incoming/", "SenderFile_1.txt");
        var second = ConnectionTester.CreateFtpTestUri("ftp://example.test/incoming");
        Check(first.AbsolutePath.StartsWith("/incoming/SmartDataSync_Test_") && first.AbsolutePath.EndsWith(".txt")
            && first != second && !first.AbsolutePath.EndsWith("/SenderFile_1.txt"), "Unique test file never uses the primary filename");
        try { ConnectionTester.CreateFtpTestUri("https://example.test/"); throw new Exception("Non-FTP accepted"); }
        catch (ArgumentException) { }
        using (var server = new TestFtpServer())
        {
            string name = ConnectionTester.UploadFtpTestAsync(server.Address, "unsaved-ftp-user", "synthetic-password", "SenderFile_1.txt").GetAwaiter().GetResult();
            server.Complete();
            Check(server.UserName == "unsaved-ftp-user", "FTP uses current credentials");
            Check(server.FileName.EndsWith(name), "Returned filename matches uploaded file");
            Check(name.StartsWith("SmartDataSync_Test_") && name != "SenderFile_1.txt", "Real upload uses a separate test filename");
            Check(Encoding.UTF8.GetString(server.Payload) == "تست" && server.Payload.Length < 32, "Actual transfer contains only tiny UTF-8 test text");
        }
        using (var server = new TestFtpServer(denyLogin: true))
        {
            try { ConnectionTester.UploadFtpTestAsync(server.Address, "user", "wrong").GetAwaiter().GetResult(); throw new Exception("Rejected login reported as success"); }
            catch (WebException error) { Check(ConnectionTester.DescribeFtpFailure(error).Contains("530"), "Login failure explained"); }
            server.Complete();
        }
        using (var server = new TestFtpServer(denyWrite: true))
        {
            try { ConnectionTester.UploadFtpTestAsync(server.Address, "user", "password").GetAwaiter().GetResult(); throw new Exception("Rejected upload reported as success"); }
            catch (WebException error) { Check(ConnectionTester.DescribeFtpFailure(error).Contains("550"), "Write-permission failure explained"); }
            server.Complete();
        }
        Console.WriteLine("PASS: unsaved SQL values, validation, unique FTP filename, real local upload/payload, and FTP authentication/write failures.");
    }
}
