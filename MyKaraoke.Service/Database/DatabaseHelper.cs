using Microsoft.Data.Sqlite;
using MyKaraoke.Service.Logging;
using static MyKaraoke.Service.EnvironmentSetup.Constants;

namespace MyKaraoke.Service.Database {
    public static class DatabaseHelper {
        private static readonly string StorageRoot = FilesPath; // appdata/roaming/MyKaraoke/Songs

        public static byte[] RetrieveDataFromHash(string fileHash) {
            var path = FileHasher.GetFilePathFromHash(fileHash);
            Logger.Important($"PATH => {path}");
            Logger.Important($"PATH => {path}");
            Logger.Important($"PATH => {path}");
            try {
                return File.ReadAllBytes(path);
            }
            catch (Exception ex) {
                Logger.Error($"Error reading file {path}: {ex.Message}");
                return null;
            }
        }

        public static void InsertFileHashToDatabase(string hash) {
            var command = new SqliteCommand();
            command.CommandText = "INSERT OR IGNORE INTO Files (FileHash) VALUES (@Hash)";
            command.Parameters.AddWithValue("@Hash", hash);
            try {
                SQLiteManager.DatabaseExecuteCommand(command);
            }
            catch (Exception ex) {
                Logger.Error(ex);
            }
        }

        public static void UploadSong(string title, string artist, string vocalHash, string musicHash) {
            var command = new SqliteCommand();
            command.CommandText = "INSERT INTO Songs (Title, Artist, VocalHash, MusicHash) VALUES (@Title, @Artist, @VocalHash, @MusicHash)";
            command.Parameters.AddWithValue("@Title", title);
            command.Parameters.AddWithValue("@Artist", artist);
            command.Parameters.AddWithValue("@VocalHash", vocalHash);
            command.Parameters.AddWithValue("@MusicHash", musicHash);
            try {
                SQLiteManager.DatabaseExecuteCommand(command);
            }
            catch (Exception ex) {
                Logger.Error($"Error uploading song: {ex.Message}");
            }
            Logger.Log($"Song '{title}' uploaded successfully.");
        }

        public static void PrintDatabase() {
            Logger.Log("=== Songs Table ===");

            var command = new SqliteCommand {
                CommandText = "SELECT * FROM Songs"
            };
            try {
                // Reusing DatabaseExecuteReader to get the data
                using (var reader = SQLiteManager.DatabaseExecuteReader(command, successMessage: "Query executed successfully.", warningMessage: "No rows returned.", errorMessage: "Error reading Songs table.")) {
                    if (reader != null) {
                        while (reader.Read()) {
                            Logger.Log($"SongId: {reader["SongId"]}, Title: {reader["Title"]}, Artist: {reader["Artist"]}, VocalHash: {reader["VocalHash"]}, MusicHash: {reader["MusicHash"]}");
                        }
                    }
                }
            }


            catch (Exception exception){
                Logger.Error(exception);
            }
        }
    }
}