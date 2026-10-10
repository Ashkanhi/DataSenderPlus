using System;
using System.Data;
using System.IO;
using Newtonsoft.Json;
using DataSenderPlusProject.Helpers;
class JsonExportCheck
{
    static void Main()
    {
        string root = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "smoke-output");
        var table = new DataTable();
        table.Columns.Add("Id", typeof(int));
        table.Columns.Add("Text", typeof(string));
        table.Columns.Add("Amount", typeof(decimal));
        table.Columns.Add("Date", typeof(DateTime));
        table.Columns.Add("Flag", typeof(bool));
        table.Columns.Add("Bytes", typeof(byte[]));
        table.Rows.Add(1, "\u0633\u0644\u0627\u0645 \"quote\"\nline", 12.34m, new DateTime(2026, 10, 10), true, new byte[] { 0, 127, 255 });
        table.Rows.Add(2, DBNull.Value, DBNull.Value, DBNull.Value, DBNull.Value, DBNull.Value);
        ExportHelper.Export(table, 1, "types", root);
        string path = Path.Combine(root, "ExportFile", "types.json");
        if (File.ReadAllText(path) != JsonConvert.SerializeObject(table, Formatting.Indented))
            throw new Exception("Output differs from original serializer");
        byte[] bytes = File.ReadAllBytes(path);
        if (bytes.Length >= 3 && bytes[0] == 239 && bytes[1] == 187 && bytes[2] == 191)
            throw new Exception("Unexpected BOM");
        table.Clear();
        ExportHelper.Export(table, 1, "types", root);
        if (File.ReadAllText(path) != "[]") throw new Exception("Empty/overwrite failed");
        table = new DataTable();
        table.Columns.Add("Id", typeof(int));
        table.Columns.Add("Text", typeof(string));
        string payload = new string('x', 256);
        for (int i = 0; i < 333034; i++) table.Rows.Add(i, payload);
        ExportHelper.Export(table, 1, "large", root);
        int rows = 0;
        using (var input = File.OpenText(Path.Combine(root, "ExportFile", "large.json")))
        using (var reader = new JsonTextReader(input))
            while (reader.Read()) if (reader.TokenType == JsonToken.StartObject) rows++;
        if (rows != 333034) throw new Exception("Row count mismatch: " + rows);
        Console.WriteLine("PASS: exact JSON compatibility, UTF-8 without BOM, nulls, empty/overwrite, and 333034 streamed rows.");
        Console.WriteLine("Large output bytes: " + new FileInfo(Path.Combine(root, "ExportFile", "large.json")).Length);
    }
}