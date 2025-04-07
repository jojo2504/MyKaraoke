using System.Windows.Controls;
using MyKaraoke.Core.Lyrics;
using MyKaraoke.Core.PlaybackManager;
using MyKaraoke.Service.Logging;

namespace MyKaraokeApp.MainWindows.Events {
    public static class OnChanges {
        public static void OnCurrentSongChanges(Playback playback, Action SetupLyricSync, TextBlock CurrentLyricTextBlock, TextBlock NextLyricTextBlock) {
            Logger.Log($"Current song changed: {playback.CurrentSong?.Title}");
            CurrentLyricTextBlock.Text = "";
            NextLyricTextBlock.Text = "";

            LyricSync.LoadLyrics(playback);
            SetupLyricSync?.Invoke();
        }
    }
}
