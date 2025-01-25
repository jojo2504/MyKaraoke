using Microsoft.Data.Sqlite;
using MyKaraoke.Service.Logging;
using static MyKaraoke.Service.EnvironmentSetup.Constants;

namespace MyKaraoke.Service.Database {
    public static class DatabaseHelper {
        private static readonly string StorageRoot = SongsPath; // appdata/roaming/MyKaraoke/Songs

        public static byte[] RetrieveFileFromHash(string fileHash) {
            byte[] fileData = null;
            var command = new SqliteCommand();

            command.CommandText = "SELECT FileData FROM Files WHERE FileHash = @Hash";
            command.Parameters.AddWithValue("@Hash", fileHash);

            try {
                using (var reader = SQLiteManager.DatabaseExecuteReader(command)) {
                    if (reader != null && reader.Read()) {
                        fileData = reader["FileData"] as byte[];
                    }
                }
            }
            catch (Exception ex) {
                Logger.Error($"Error retrieving file: {ex.Message}");
            }

            return fileData;
        }

        public static void InsertFileToDatabase(string hash, byte[] fileData) {
            var command = new SqliteCommand();
            command.CommandText = "INSERT OR IGNORE INTO Files (FileHash, FileData) VALUES (@Hash, @Data)";
            command.Parameters.AddWithValue("@Hash", hash);
            command.Parameters.AddWithValue("@Data", fileData);
            try {
                SQLiteManager.DatabaseExecuteCommand(command);
            }
            catch (Exception ex) {
                Logger.Error(ex);
            }
        }

        public static void UploadSong(string title, string vocalHash, string musicHash) {
            var command = new SqliteCommand();
            command.CommandText = "INSERT INTO Songs (Title, VocalHash, MusicHash) VALUES (@Title, @VocalHash, @MusicHash)";
            command.Parameters.AddWithValue("@Title", title);
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
                            Logger.Log($"SongId: {reader["SongId"]}, Title: {reader["Title"]}, VocalHash: {reader["VocalHash"]}, MusicHash: {reader["MusicHash"]}");
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