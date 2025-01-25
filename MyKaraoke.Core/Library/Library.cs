using MyKaraoke.Core.PlaybackManager;
using MyKaraoke.Service.Database;
using Microsoft.Data.Sqlite;

namespace MyKaraoke.Core.Library{
    public class Library{
        public static List<Song> GetAllSongs() {
            List<Song> songs = new List<Song>();
            var command = new SqliteCommand();
            command.CommandText = "SELECT SongId, Title, VocalHash, MusicHash FROM Songs";
                
            using (var reader = SQLiteManager.DatabaseExecuteReader(command)) {
                while (reader.Read()) {
                    songs.Add(new Song {
                        Id = reader.GetInt32(0),
                        Title = reader.GetString(1),
                        VocalHash = reader.GetString(2),
                        MusicHash = reader.GetString(3)
                    });
                }
            }
            return songs;
        }
    }
}