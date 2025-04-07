using Microsoft.Data.Sqlite;
using Microsoft.VisualBasic.Logging;
using MyKaraoke.Core.Database;
using MyKaraoke.Service.Logging;
using System.Collections.ObjectModel;
using MyKaraoke.Core.Models;

namespace MyKaraoke.Core.PlaybackManager {
    public class Playlist {
        public ObservableCollection<Song> Songs { get; private set; } = new ObservableCollection<Song>();
        private Random random = new Random();

        public Song Next(bool shuffling = false) {
            Song nextSong;
            if (Songs.Count == 0) return null;
            if (shuffling) {
                var song_index = random.Next(Songs.Count);
                nextSong = Songs[song_index];
                Songs.RemoveAt(song_index);
            }
            else {
                nextSong = Songs[0];
                Songs.RemoveAt(0); // Remove the song from the playlist
            }
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