using System.Windows.Controls;
using MyKaraoke.Core.Library;
using MyKaraoke.Core.Models;
using MyKaraoke.Core.PlaybackManager;
using MyKaraoke.Service.Logging;

namespace MyKaraokeApp.MainWindows.Events {
    public static class MenuItemClick {
        public static void PlayNow(object sender, Playback playback) {
            var menuItem = sender as MenuItem;
            var selectedSong = menuItem.DataContext as Song;
            playback.CurrentSong = selectedSong;
            playback.Play();
        }

        public static void AddToPlaylist(object sender, Playlist playlist) {
            var menuItem = sender as MenuItem;
            if (menuItem?.DataContext is not Song selectedSong) {
                Logger.Log("No song selected");
                return;
            }
            playlist.AddSong(selectedSong);
            Logger.Log($"Added '{selectedSong.Title}' to the playlist via context menu.");
        }

        public static void RemoveSong(object sender, Playlist playlist) {
            var menuItem = sender as MenuItem;
            var selectedSong = menuItem?.DataContext as Song;
            if (selectedSong == null) {
                Logger.Log("No song selected");
                return;
            }
            playlist.RemoveSong(selectedSong);
        }

        public static void DeleteSong(object sender) {
            var menuItem = sender as MenuItem;
            var selectedSong = menuItem?.DataContext as Song;
            Library.RemoveSongFromLibrary(selectedSong);
        }

        public static void ModifySong(object sender) {
            Logger.Log("ModifySong_Click");
            throw new NotImplementedException();
        }
    }
}