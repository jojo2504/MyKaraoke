using Microsoft.Data.Sqlite;
using MyKaraoke.Service.Database;
using MyKaraoke.Service.Logging;

namespace MyKaraoke.Core.PlaybackManager {
    public class Song {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Artist { get; set; }
        public string VocalFileHash { get; set; }
        public string MusicFileHash { get; set; }

        public byte[] GetVocalData() {
            return DatabaseHelper.RetrieveDataFromHash(VocalFileHash);
        }

        public byte[] GetMusicData() {
            return DatabaseHelper.RetrieveDataFromHash(MusicFileHash);
        }
    }
}