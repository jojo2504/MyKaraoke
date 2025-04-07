using System.Globalization;
using Microsoft.Data.Sqlite;
using MyKaraoke.Service.Logging;
using MyKaraoke.Core.Models;
using static MyKaraoke.Service.EnvironmentSetup.Constants;

namespace MyKaraoke.Core.Database {
    public static class DatabaseHelper {
        private static readonly string StorageRoot = FilesPath; // appdata/roaming/MyKaraoke/Songs

        public static byte[] RetrieveDataFromHash(string fileHash) {
            var path = FileHasher.GetFilePathFromHash(fileHash);
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
            command.CommandText = "INSERT OR REPLACE INTO Files (FileHash) VALUES (@Hash)";
            command.Parameters.AddWithValue("@Hash", hash);
            try {
                SQLiteManager.DatabaseExecuteCommand(command);
            }
            catch (Exception ex) {
                Logger.Error(ex);
            }
        }

        public static bool FileHashExists(string hash) {
            var command = new SqliteCommand();
            command.CommandText = "SELECT COUNT(*) FROM Files WHERE FileHash = @Hash";
            command.Parameters.AddWithValue("@Hash", hash);

            object? result = SQLiteManager.DatabaseExecuteScalar(command);
            if (result != null && Convert.ToInt32(result) > 0) {
                // The file hash exists in the database
                return true;
            }
            return false;
        }

        public static void UploadSong(string title, string artist, string vocalHash, string musicHash, string LRCHash) {
            var command = new SqliteCommand();
            command.CommandText = "INSERT INTO Songs (Title, Artist, VocalHash, MusicHash, LRCHash) VALUES (@Title, @Artist, @VocalHash, @MusicHash, @LRCHash)";
            command.Parameters.AddWithValue("@Title", CultureInfo.CurrentCulture.TextInfo.ToTitleCase(title.ToLower()));
            command.Parameters.AddWithValue("@Artist", artist);
            command.Parameters.AddWithValue("@VocalHash", vocalHash);
            command.Parameters.AddWithValue("@MusicHash", musicHash);
            command.Parameters.AddWithValue("@LRCHash", LRCHash);
            try {
                SQLiteManager.DatabaseExecuteCommand(command, successMessage: $"Song '{title}' uploaded successfully.");
            }
            catch (Exception ex) {
                Logger.Error($"Error uploading song: {ex.Message}");
            }
        }

        public static void DeleteSongFromDatabase(Song song){
            if (song == null) return;
            var command = new SqliteCommand();
            command.CommandText = $"DELETE FROM Songs WHERE songID = {song.Id}";
            try {
                SQLiteManager.DatabaseExecuteCommand(command, successMessage: $"Song '{song.Title}' deleted successfully.");
            }
            catch (Exception ex) {
                Logger.Error($"Error uploading song: {ex.Message}");
            }
        }

        public static void PrintDatabase() {
            var command = new SqliteCommand {
                CommandText = "SELECT * FROM Songs"
            };
            try {
                // Reusing DatabaseExecuteReader to get the data
                using (var reader = SQLiteManager.DatabaseExecuteReader(command, successMessage: "Query executed successfully.", warningMessage: "No rows returned.", errorMessage: "Error reading Songs table.")) {
                    Logger.Log("=== Songs Table ===");
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