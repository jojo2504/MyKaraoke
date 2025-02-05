using MyKaraoke.Service.Database;
using System.Globalization;

namespace MyKaraoke.Service.Models {
    public class Song {
        public int Id { get; set; }
        private string _title;
        public string Title {
            get => _title;
            set {
                // Convert the input to title case
                _title = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(value.ToLower());
            }
        }
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