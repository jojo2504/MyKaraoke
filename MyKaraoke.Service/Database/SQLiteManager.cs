using Microsoft.Data.Sqlite;
using System;
using System.IO;
using System.Data;
using MyKaraoke.Service.Logging;
using static MyKaraoke.Service.EnvironmentSetup.Constants;

namespace MyKaraoke.Service.Database{
    public static class SQLiteManager {
        private readonly static string databaseFolderPath = Path.Combine(BaseAppDataPath, "database");
        private readonly static string connectionString = @$"Data Source={DatabasePath}";
        static SQLiteManager() {
            Logger.Log("Initializing SQLiteManager");
            try {
                if (EnsureDatabaseFolderExists()) {
                    DoesDatabaseExists();
                }
            }
            catch (Exception ex) {
                Logger.Error(ex.Message);
            }
        }

        private static bool EnsureDatabaseFolderExists() {
            try {
                if (!Directory.Exists(databaseFolderPath)) {
                    Directory.CreateDirectory(databaseFolderPath);
                    Logger.Success("Database directory created at: " + databaseFolderPath);
                }
                else {
                    Logger.Warning("Database directory already exists at: " + databaseFolderPath);
                    return true;
                }
            }
            catch (Exception ex) {
                Logger.Error(ex.Message);
            }
            return false;
        }

        private static bool DoesDatabaseExists() {
            if (File.Exists(DatabasePath)) {
                Logger.Warning("Database already exists at: " + DatabasePath);
                return true;
            }
            else {
                Logger.Warning("Database doesn't exist at: " + DatabasePath);
                return false;
            }
        }

        private static void ExecuteDatabaseScript(string scriptFilePath, string successMessage) {
            try {
                Logger.Log($"Reading script from: {Path.GetFullPath(scriptFilePath)}");
                string script = File.ReadAllText(scriptFilePath);

                Logger.Log($"Connection to: {connectionString}");
                using var connection = new SqliteConnection(connectionString);
                connection.Open();

                using var command = new SqliteCommand(script, connection);
                command.ExecuteNonQuery();

                Logger.Success(successMessage);
            }
            catch (Exception ex) {
                Logger.Error(ex);
            }
        }

        public static void CreateDatabase() {
            Logger.Log("Creating database...");
            var scriptPath = Path.Combine(SolutionRoot, "scripts/create_database.sql");

            ExecuteDatabaseScript(scriptPath, "Created database");
            if (!File.Exists(scriptPath)) {
                Logger.Error($"Create database script not found at: {scriptPath}");
            }
        }

        public static void ResetDatabase() {
            Logger.Log("Resetting database...");
            var scriptPath = Path.Combine(SolutionRoot, "scripts/reset_database.sql");
            Logger.Log($"Reset database script path => {scriptPath}");
            
            ExecuteDatabaseScript(scriptPath, "Resetted database");
            if (!File.Exists(scriptPath)) {
                Logger.Error($"Reset database script not found at: {scriptPath}");
            }

            CreateDatabase();
            Logger.Success("Recreated the database");
        }

        public static void DatabaseExecuteCommand(SqliteCommand command, string successMessage="", string errorMessage="") {
            Logger.Log($"Connecting to: {connectionString}");
            Logger.Log($"Command: {command.CommandText}");

            try {
                using var connection = new SqliteConnection(connectionString);
                connection.Open();
                command.Connection = connection;
                command.ExecuteNonQuery();

                Logger.Success(successMessage);
            }
            catch(Exception ex) {
                Logger.Error(ex);
            }
        }

        public static object? DatabaseExecuteScalar(SqliteCommand command, string successMessage="", string warningMessage="", string errorMessage = "") {
            Logger.Log($"Connecting to: {connectionString}");
            Logger.Log($"Command: {command.CommandText}");
        
            try {
                using var connection = new SqliteConnection(connectionString);
                connection.Open();
                command.Connection = connection;
                object result = command.ExecuteScalar();

                if (result != null) {
                    Logger.Success(successMessage);
                }
                else {
                    Logger.Warning(warningMessage);
                }
                return result;
            }
            catch (Exception ex) {
                Logger.Error(ex);
                return null;
            }
        }

        public static SqliteDataReader? DatabaseExecuteReader(SqliteCommand command, string successMessage="", string warningMessage="", string errorMessage="") {
            Logger.Log($"Connecting to: {connectionString}");
            Logger.Log($"Command: {command.CommandText}");
            try {
                var connection = new SqliteConnection(connectionString);
                connection.Open();
                command.Connection = connection;
                
                var result = command.ExecuteReader(CommandBehavior.CloseConnection);

                if (result != null) {
                    Logger.Success(successMessage);
                }
                else {
                    Logger.Warning(warningMessage);
                }
                return result;
            }
            catch(Exception ex) {
                Logger.Error(ex);
                return null;
            }
        }
    }
}