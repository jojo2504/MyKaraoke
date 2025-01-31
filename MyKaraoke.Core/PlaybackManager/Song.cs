using Microsoft.Data.Sqlite;
using MyKaraoke.Service.Database;
using MyKaraoke.Service.Logging;

namespace MyKaraoke.Core.PlaybackManager {
    public class Song {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Artist { get; set; }
        public string VocalHash { get; set; }
        public string MusicHash { get; set; }
        public string LRCHash { get; set; }

        public byte[] GetVocalData() {
            return DatabaseHelper.RetrieveDataFromHash(VocalHash);
        }

        public byte[] GetMusicData() {
            return DatabaseHelper.RetrieveDataFromHash(MusicHash);
        }
        public byte[] GetLRCData() {
            return DatabaseHelper.RetrieveDataFromHash(LRCHash);
        }
    }
}