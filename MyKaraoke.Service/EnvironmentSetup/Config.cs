using System;
using System.IO;
using System.Text.Json;

namespace MyKaraoke.Service.EnvironmentSetup {
    internal static class ConfigLoader {
        // Path properties
        public static string BaseAppDataPath { get; }
        public static string LogsPath { get; }
        public static string DatabasePath { get; }
        public static string SongsPath { get; }

        public static DirectoryInfo TryGetSolutionDirectoryInfo(string currentPath = null) {
            var directory = new DirectoryInfo(
                currentPath ?? Directory.GetCurrentDirectory());
            while (directory != null && !directory.GetFiles("*.sln").Any()) {
                directory = directory.Parent;
            }
            return directory;
        }

        static ConfigLoader() {
            try {
                DirectoryInfo solutionRoot = TryGetSolutionDirectoryInfo();
                // Construct the path to appsettings.json in the 'config' directory
                string appSettingsPath = Path.Combine(solutionRoot.FullName, "config", "appsettings.json");

                string jsonText = File.ReadAllText(appSettingsPath);

                var config = JsonDocument.Parse(jsonText).RootElement;

                // Load paths configuration
                var pathsConfig = config.GetProperty("Paths");

                // Get base directory configuration
                var baseDirectoryConfig = pathsConfig.GetProperty("BaseDirectory");
                if (baseDirectoryConfig.GetProperty("Type").GetString() == "AppData") {
                    string baseDirectoryPath = baseDirectoryConfig.GetProperty("Path").GetString()!;
                    BaseAppDataPath = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                        baseDirectoryPath
                    );
                }

                // Get logs configuration
                var logsConfig = pathsConfig.GetProperty("Logs");
                if (logsConfig.GetProperty("Type").GetString() == "AppData") {
                    string logsPath = logsConfig.GetProperty("Path").GetString()!;
                    LogsPath = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                        logsPath
                    );
                }

                // Get database configuration
                var databaseConfig = pathsConfig.GetProperty("Database");
                if (databaseConfig.GetProperty("Type").GetString() == "AppData") {
                    string databasePath = databaseConfig.GetProperty("Path").GetString()!;
                    DatabasePath = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                        databasePath
                    );

                    var songsConfig = databaseConfig.GetProperty("Songs");
                    string songsDirectoryPath = songsConfig.GetProperty("Path").GetString()!;

                    SongsPath = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                        songsDirectoryPath
                    );
                }

                // Create directories if they don't exist
                Directory.CreateDirectory(BaseAppDataPath);
                Directory.CreateDirectory(LogsPath);
                Directory.CreateDirectory(SongsPath);
            }
            catch (Exception ex) {
                throw new InvalidOperationException("Failed to initialize configuration", ex);
            }
        }
    }
}
