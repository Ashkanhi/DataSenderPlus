using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace DataSenderPlusProject.Models
{
    public sealed class LogEntry
    {
        public string Timestamp { get; set; }
        public string Level { get; set; }
        public string Message { get; set; }
        public string Summary { get { return Message.Split(new[] { '\r', '\n' }, 2)[0]; } }
    }

    public static class LogReader
    {
        private static readonly Regex Header = new Regex(
            @"^(\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}) \[(INFO|WARNING|ERROR)\] (.*)$");

        public static IReadOnlyList<LogEntry> ReadRecent(string folder, int count = 10, bool errorsOnly = false)
        {
            if (count < 1) throw new ArgumentOutOfRangeException(nameof(count));
            var result = new List<LogEntry>();
            if (!Directory.Exists(folder)) return result;

            // Daily filenames sort chronologically; read older days only if needed.
            var files = Directory.EnumerateFiles(folder, "*.log")
                .Where(path => IsDailyLog(path))
                .OrderByDescending(path => Path.GetFileName(path), StringComparer.Ordinal);
            foreach (string path in files)
            {
                var recent = new Queue<LogEntry>();
                LogEntry current = null;
                var message = new StringBuilder();
                using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read,
                    FileShare.ReadWrite | FileShare.Delete))
                using (var reader = new StreamReader(stream, Encoding.UTF8))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        Match match = Header.Match(line);
                        if (match.Success)
                        {
                            if (current != null) current.Message = message.ToString();
                            current = new LogEntry { Timestamp = match.Groups[1].Value,
                                Level = match.Groups[2].Value };
                            message.Clear();
                            message.Append(match.Groups[3].Value);
                            if (!errorsOnly || current.Level == "ERROR")
                            {
                                recent.Enqueue(current);
                                if (recent.Count > count - result.Count) recent.Dequeue();
                            }
                        }
                        else if (current != null)
                        {
                            // Keep exception stack traces attached to their log entry.
                            message.AppendLine().Append(line);
                        }
                    }
                    if (current != null) current.Message = message.ToString();
                }
                result.AddRange(recent.Reverse());
                if (result.Count == count) break;
            }
            return result;
        }

        private static bool IsDailyLog(string path)
        {
            DateTime date;
            return DateTime.TryParseExact(Path.GetFileNameWithoutExtension(path),
                "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out date);
        }
    }
}
