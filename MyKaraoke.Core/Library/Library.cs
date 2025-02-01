using MyKaraoke.Core.PlaybackManager;
using MyKaraoke.Service.Database;
using Microsoft.Data.Sqlite;
using System.Collections.ObjectModel;

namespace MyKaraoke.Core.Library {
    public class Library {
        public static ObservableCollection<Song> Songs = [];

        static Library() {
            FetchAllSongs();
        }

        public static void FetchAllSongs() {
            Songs.Clear();
            var command = new SqliteCommand("SELECT * FROM Songs");

            using (var reader = SQLiteManager.DatabaseExecuteReader(command,
                       successMessage: "Query executed successfully.",
                       warningMessage: "No rows returned.")) {
                while (reader.Read()) {
                    Songs.Add(new Song {
                        Id = reader.GetInt32(0),
                        Title = reader.GetString(1),
                        Artist = reader.GetString(2),
                        VocalHash = reader.GetString(3),
                        MusicHash = reader.GetString(4),
                        LRCHash = reader.GetString(5)
                    });
                }
            }
        }

        public static ObservableCollection<Song> GetAllSongs() {
            return Songs;
        }

        public static void AddSongToLibrary(Song song){
            Songs.Add(song);
        }

        public static void RemoveSongFromLibrary(Song song){
            Songs.Remove(song);
        }
    }
}