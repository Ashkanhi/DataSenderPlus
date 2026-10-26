using DataSenderPlusProject.Models;
using System;
using System.IO;
using System.Linq;
using System.Text;

// Standalone regression checks; compile together with Models/LogReader.cs.
internal static class LogReaderTests
{
    private static void Check(bool condition, string name)
    {
        if (!condition) throw new Exception(name);
    }

    private static void Main()
    {
        string folder = Path.Combine(Path.GetTempPath(), "DataSenderLogTests-" + Guid.NewGuid());
        Check(LogReader.ReadRecent(folder).Count == 0, "Missing directory");
        Directory.CreateDirectory(folder);
        try
        {
            Check(LogReader.ReadRecent(folder).Count == 0, "Empty directory");
            File.WriteAllText(Path.Combine(folder, "other.log"), "unrelated file");
            var older = new StringBuilder("orphan continuation\n");
            for (int i = 0; i < 12; i++)
                older.AppendLine("2026-10-11 10:00:00 [INFO] Older " + i);
            File.WriteAllText(Path.Combine(folder, "2026-10-11.log"), older.ToString());
            string latest = Path.Combine(folder, "2026-10-12.log");
            File.WriteAllText(latest, "2026-10-12 10:00:00 [ERROR] خطای آزمایشی\n   at First()\n   at Second()\n" +
                "2026-10-12 10:00:00 [WARNING] Last warning\n");
            var entries = LogReader.ReadRecent(folder);
            Check(entries.Count == 10, "Ten records across dates");
            Check(entries[0].Message == "Last warning", "Newest first, same timestamp");
            Check(entries[1].Level == "ERROR" && entries[1].Message.Contains("at Second()"), "Stack trace preserved");
            Check(entries[1].Summary == "خطای آزمایشی", "Unicode summary");
            Check(entries[2].Message == "Older 11" && entries[9].Message == "Older 4", "Older day tail");
            Check(LogReader.ReadRecent(folder, 1).Single().Message == "Last warning", "Custom count");
            Check(LogReader.ReadRecent(folder, 20).Count == 14, "Fewer than requested");
            using (var writer = new FileStream(latest, FileMode.Append, FileAccess.Write, FileShare.Read))
                Check(LogReader.ReadRecent(folder).Count == 10, "Read while file open for append");
            File.AppendAllText(latest, "2026-10-12 10:01:00 [INFO] Refreshed\n");
            Check(LogReader.ReadRecent(folder)[0].Message == "Refreshed", "Refresh sees appended record");
            File.WriteAllText(Path.Combine(folder, "2026-10-13.log"), "");
            Check(LogReader.ReadRecent(folder)[0].Message == "Refreshed", "Empty latest file");
            File.AppendAllText(Path.Combine(folder, "2026-10-11.log"), "2026-10-11 11:00:00 [ERROR] Older error\n   at Older()\n");
            for (int i = 0; i < 15; i++) File.AppendAllText(latest, "2026-10-12 11:00:00 [INFO] Normal message\n");
            var errors = LogReader.ReadRecent(folder, 10, true);
            Check(errors.Count == 2 && errors.All(entry => entry.Level == "ERROR"), "Filter across days beyond ten normal records");
            Check(errors[0].Summary == "خطای آزمایشی" && errors[0].Message.Contains("at Second()")
                && errors[1].Message.Contains("at Older()"), "Filtered order and stack traces");
            Check(LogReader.ReadRecent(folder, 1, true).Count == 1, "Filtered count limit");
            Console.WriteLine("PASS: ordering, count, multiple days, multiline errors, Unicode, refresh, shared access, empty/missing logs.");
        }
        finally
        {
            // Only delete the uniquely created test directory.
            Directory.Delete(folder, true);
        }
    }
}
