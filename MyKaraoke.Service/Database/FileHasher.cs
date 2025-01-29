using Microsoft.Data.Sqlite;
using MyKaraoke.Service.Database;
using MyKaraoke.Service.Logging;
using System.Security.Cryptography;
using static MyKaraoke.Service.EnvironmentSetup.Constants;

namespace MyKaraoke.Service.Database {
    public static class FileHasher {
        public static string GetFilePathFromHash(string hash) {
            string subDir1 = hash.Substring(0, 2); // First 2 characters
            string subDir2 = hash.Substring(2, 2); // Next 2 characters
            string fileDir = Path.Combine(FilesPath, subDir1, subDir2);
            Directory.CreateDirectory(fileDir);  // Ensure directories exist

            Logger.Log(Path.Combine(fileDir, hash));
            return Path.Combine(fileDir, hash);  // Final file path
        }

        // Save the file to the hashed directory
        public static void SaveFileToDisk(string hash, byte[] fileData) {
            Logger.Log("Saving File To Disk:");

            string filePath = GetFilePathFromHash(hash);
            File.WriteAllBytes(filePath, fileData);  // Save the file
            Logger.Success($"File saved to {filePath}");
        }

        public static string ComputeSHA256(string filePath) {
            try {
                Logger.Log($"Hashing {filePath}");
                using (var sha256 = SHA256.Create()) {
                    using (var stream = File.OpenRead(filePath)) {
                        byte[] hash = sha256.ComputeHash(stream);
                        var hashString = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                        
                        Logger.Success($"Hash Version => {hashString}");
                        return hashString;
                    }
                }
            }
            catch (Exception ex) {
                Logger.Error(ex);
                return null;
            }
        }
    }
}

