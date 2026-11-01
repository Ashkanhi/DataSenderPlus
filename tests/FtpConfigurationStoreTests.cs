using DataSenderPlusProject.Helpers;
using DataSenderPlusProject.Models;
using System;
using System.Data;
using System.Data.SqlClient;

internal static class FtpConfigurationStoreTests
{
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }

    private static void Reject(FtpConfigurationModel settings)
    {
        try { FtpConfigurationStore.Validate(settings); }
        catch (ArgumentException) { return; }
        throw new Exception("Invalid settings accepted");
    }

    private static void Main()
    {
        var table = new DataTable();
        table.Columns.Add("ConfigID", typeof(int));
        foreach (string name in new[] { "ServerAddress", "UserName", "Password", "FileName" }) table.Columns.Add(name);
        table.Columns.Add("IntervalValue", typeof(int)); table.Columns.Add("SendFlag", typeof(int));
        table.Columns.Add("FolderPath"); table.Columns.Add("OutputTypeID", typeof(int));
        table.Rows.Add(2, "ftp://example.test/folder", "user'نام", "synthetic;secret'", "SenderFile_2", 10, 1, @"C:\Exports", 1);
        table.Rows.Add(5, "", "", "", "SenderFile_5", 30, 2, DBNull.Value, 4);
        table.Rows.Add(4, DBNull.Value, DBNull.Value, DBNull.Value, "Incomplete", DBNull.Value, DBNull.Value, DBNull.Value, DBNull.Value);
        table.Rows.Add(DBNull.Value, "", "", "", "NoKey", 10, 2, "", 1);
        FtpConfigurationModel record;
        using (var reader = table.CreateDataReader())
        {
            var rows = FtpConfigurationStore.ReadRows(reader);
            Check(rows.Count == 3 && rows[1].ConfigID == 5 && rows[1].FolderPath == "", "Read IDs, nullable folder, and skip missing key");
            var incomplete = rows[2];
            Check(incomplete.ConfigID == 4 && incomplete.IntervalValue == 0 && incomplete.OutputTypeID == 0
                && incomplete.SendFlag == 0 && incomplete.Password == "" && incomplete.ServerAddress == "",
                "NULL numeric and text fields load without InvalidCastException");
            Reject(incomplete);
            incomplete.IntervalValue = 15; incomplete.OutputTypeID = 1; incomplete.SendFlag = 2;
            using (var repaired = FtpConfigurationStore.CreateUpdateCommand(null, incomplete))
                Check((int)repaired.Parameters["@IntervalValue"].Value == 15
                    && (int)repaired.Parameters["@SendFlag"].Value == 2, "Local-only mode is persisted as integer 2");
            record = rows[0];
        }
        FtpConfigurationStore.Validate(record);
        using (var command = FtpConfigurationStore.CreateUpdateCommand(null, record))
        {
            Check(command.Parameters.Count == 9, "All fields parameterized");
            Check((int)command.Parameters["@ConfigID"].Value == 2, "Update selected ID only");
            Check((string)command.Parameters["@Password"].Value == record.Password, "Password preserved exactly");
            Check(!command.CommandText.Contains(record.Password), "No credentials embedded in SQL");
            Check(command.Parameters["@SendFlag"].SqlDbType == SqlDbType.Int
                && (int)command.Parameters["@SendFlag"].Value == 1, "FTP flag persists as integer 1");
        }
        record.FileName = "../bad"; Reject(record); record.FileName = "safe";
        record.IntervalValue = 0; Reject(record); record.IntervalValue = 1;
        record.OutputTypeID = 99; Reject(record); record.OutputTypeID = 1;
        record.ServerAddress = "https://example.test/"; Reject(record);
        record.ServerAddress = "ftp://user:password@example.test/"; Reject(record);
        record.SendFlag = 0; Reject(record);
        record.SendFlag = 3; Reject(record);
        record.SendFlag = 2; FtpConfigurationStore.Validate(record);
        record.FolderPath = "relative-folder"; Reject(record);
        record.FolderPath = ""; FtpConfigurationStore.Validate(record);
        Console.WriteLine("PASS: configuration mapping, all update parameters, selected-record isolation, credentials, validation, and local-only export.");
    }
}
