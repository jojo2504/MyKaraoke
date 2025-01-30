using MyKaraoke.Core.PlaybackManager;
using MyKaraoke.Service.Database;
using Microsoft.Data.Sqlite;

namespace MyKaraoke.Core.Library {
    public class Library {
        public static List<Song> GetAllSongs() {
            List<Song> songs = new List<Song>();
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
                        MusicHash = reader.GetString(4)
                    });
                }
            }
            return songs;
        }
    }
}