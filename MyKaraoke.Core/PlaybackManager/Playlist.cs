using Microsoft.Data.Sqlite;
using Microsoft.VisualBasic.Logging;
using MyKaraoke.Service.Database;
using MyKaraoke.Service.Logging;
using System.Collections.ObjectModel;

namespace MyKaraoke.Core.PlaybackManager {
    public class Playlist {
        public ObservableCollection<Song> Songs { get; private set; }

        public Playlist() {
            Songs = new ObservableCollection<Song>();
        }

        public Song Next() {
            if (Songs.Count == 0) return null;
            var nextSong = Songs[0];
            Songs.RemoveAt(0); // Remove the song from the playlist
            return nextSong;
        }

        public void AddSong(Song song) {
            // Add the song to the ObservableCollection
            Songs.Add(song);
            Logger.Success($"Added {song.Title} to the playlist.");
        }

        public void RemoveSong(Song song) {
            Songs.Remove(song);
        }
    }
}