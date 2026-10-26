using DataSenderPlusProject.Helpers;
using System;
using System.Configuration;
using System.Data.SqlClient;
using System.IO;

internal static class DatabaseSettingsTests
{
    private static void Check(bool result, string name)
    {
        if (!result) throw new Exception(name);
    }

    private static void Main()
    {
        // Run from an isolated test output directory, never the application directory.
        string configPath = AppDomain.CurrentDomain.SetupInformation.ConfigurationFile;
        string legacyPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config.txt");
        Check(!File.Exists(legacyPath), "Do not overwrite existing legacy configuration");
        File.WriteAllText(configPath, "<configuration><appSettings><add key='Unrelated' value='preserved'/></appSettings></configuration>");
        Check(DatabaseSettings.CheckAccessPassword("RWP@1400"), "Correct access password");
        Check(!DatabaseSettings.CheckAccessPassword("wrong"), "Incorrect access password");
        var settings = new DatabaseConnectionSettings
        {
            Server = "test-server", Database = "test-db", UserName = "test-user",
            Password = "Test;Secret=\"عبارت\"", CommandTimeoutSeconds = 600
        };
        DatabaseSettings.Save(settings);
        Check(!File.ReadAllText(configPath).Contains(settings.Password), "No plaintext secret");
        Check(File.ReadAllText(configPath).Contains("dpapi-user-v1:"), "Password is protected");
        Check(ConfigurationManager.AppSettings["Unrelated"] == "preserved", "Preserve unrelated settings");
        var loaded = DatabaseSettings.Load();
        Check(loaded.Password == settings.Password && loaded.Server == settings.Server && loaded.Database == settings.Database
            && loaded.UserName == settings.UserName && loaded.CommandTimeoutSeconds == 600, "Round trip");
        var connection = new SqlConnectionStringBuilder(loaded.BuildConnectionString());
        Check(connection.Password == settings.Password && connection.ConnectTimeout == 15, "Special characters and connection timeout");
        settings.CommandTimeoutSeconds = 0;
        DatabaseSettings.Save(settings);
        Check(DatabaseSettings.GetCommandTimeoutSeconds() == 0, "Live timeout refresh");
        File.WriteAllText(legacyPath, "legacy-server\nlegacy-db\nlegacy-user\nlegacy-password\n");
        Check(DatabaseSettings.MigrateLegacyConfig() != null, "Leftover legacy file produces a warning, not startup failure");
        Check(File.Exists(legacyPath) && DatabaseSettings.Load().Server == "test-server", "Preserve conflicting legacy settings");
        File.WriteAllText(configPath, "<configuration><appSettings/></configuration>");
        DatabaseSettings.MigrateLegacyConfig();
        Check(!File.Exists(legacyPath), "Remove plaintext after migration");
        loaded = DatabaseSettings.Load();
        Check(loaded.Server == "legacy-server" && loaded.Database == "legacy-db" && loaded.UserName == "legacy-user"
            && loaded.Password == "legacy-password", "Migration preserves all four values");
        Check(!File.ReadAllText(configPath).Contains("legacy-password"), "Migrated secret protected");
        Console.WriteLine("PASS: password gate, DPAPI round trip, safe connection string, settings preservation, live timeout, and legacy migration.");
    }
}
