using MyKaraoke.Core.PlaybackManager;
using MyKaraoke.Service.Database;
using Microsoft.Data.Sqlite;

namespace MyKaraoke.Core.Library {
    public class Library {
        public static HashSet<Song> Songs = [];
        public static HashSet<Song> GetAllSongs() {
            HashSet<Song> songs = new HashSet<Song>();
            var command = new SqliteCommand("SELECT * FROM Songs");

            using (var reader = SQLiteManager.DatabaseExecuteReader(command,
                       successMessage: "Query executed successfully.",
                       warningMessage: "No rows returned.")) {
                while (reader.Read()) {
                    songs.Add(new Song {
                        Id = reader.GetInt32(0),
                        Title = reader.GetString(1),
                        Artist = reader.GetString(2),
                        VocalHash = reader.GetString(3),
                        MusicHash = reader.GetString(4),
                        LRCHash = reader.GetString(5)
                    });
                }
            }
            HashSet<Song> SongsCache = new HashSet<Song>(songs);
            return songs;
        }

        public static void AddSongToLibrary(Song song){
            Songs.Add(song);
        }
    }
}