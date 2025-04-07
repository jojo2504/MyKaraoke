using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using MyKaraoke.Core.Database;
using MyKaraoke.Core.Library;
using MyKaraoke.Core.PlaybackManager;
using MyKaraoke.Service.EnvironmentSetup;
using MyKaraoke.Service.Logging;
using MyKaraokeApp.Animations;
using MyKaraokeApp.MainWindows.Events;
using static MyKaraoke.Service.EnvironmentSetup.Constants;

namespace MyKaraokeApp.MainWindows {
    public partial class MainWindow : Window {
        private Window _lyricsWindow;
        private Playlist _playlist;
        private Playback _playback;
        private string _lastLyricLineText = "";
        private bool _displayCurrentLyricTextTop = true;
        private string vocalFilePath;
        private string vocalHash;
        private string musicFilePath;
        private string musicHash;
        private int _selectedIndex = 0;
        private const double ItemHeight = 60; // Approximate height of each item (including margins)
        private const double SelectedScale = 1.5; // Scale factor for the selected item (larger on the left)

        // Default constructor required by WPF (parameterless)
        public MainWindow() : this([]) { }

        public MainWindow(string[] args) {
            Logger.Log($"BaseAppDataPath => {BaseAppDataPath}");
            Logger.Log($"LogsPath => {LogsPath}");
            Logger.Log($"DatabasePath => {DatabasePath}");
            Logger.Log($"FilesPath => {FilesPath}");
            Logger.Log($"SolutionRoot => {SolutionRoot}");
            Logger.Log($"Received {args.Length} argument(s).");
            foreach (var arg in args) {
                Logger.Log($"Argument: {arg}");
            }
            if (args.Length > 0 && args[0] == "--reset") {
                Helper.ResetFileDirectory();
                SQLiteManager.ResetDatabase();
            }
            try {
                InitializeComponent();
                InitializePlayback();
                InitializeListViews();
                WireUpEventHandlers();
                DatabaseHelper.PrintDatabase();
            }
            catch (Exception exception) {
                Logger.Fatal(exception);
            }
        }

        private void InitializeListViews() {
            SongListView.ItemsSource = _playlist.Songs;
            Logger.Success("Initialized ListViews");
        }

        public void UpdateLibrary() {
            Library.FetchAllSongs();
            //CollectionViewSource.GetDefaultView(DatabaseSongsListView.ItemsSource)?.Refresh();
            Logger.Success("Updated Library");
        }

        private void InitializePlayback() {
            _playlist = new Playlist();
            _playback = new Playback(_playlist);
            DataContext = _playback;
            Logger.Log($"DataContext is: {DataContext}");  // Pour vérifier
            SongListView.ItemsSource = _playlist.Songs;

            SongLibraryList.ItemsSource = Library.GetAllSongs();
        }

        private void WireUpEventHandlers() {
            //Upload Events
            VocalUploadButton.Click += (sender, e) => (vocalFilePath, vocalHash) = ButtonClick.UploadFile(true, VocalFileNameText, MusicFileNameText);
            MusicUploadButton.Click += (sender, e) => (musicFilePath, musicHash) = ButtonClick.UploadFile(false, VocalFileNameText, MusicFileNameText);
            ConfirmUploadButton.Click += (sender, e) => ButtonClick.AddSongToDatabase(
                SongNameTextBox,
                ArtistNameTextBox,
                VocalFileNameText,
                MusicFileNameText,
                SongListView,
                vocalFilePath,
                musicFilePath,
                vocalHash,
                musicHash,
                UpdateLibrary
            );

            //Playback Events
            MaximizeLyricsButton.Click += (sender, e) => ButtonClick.MaximizeLyrics(_lyricsWindow, CurrentLyricTextBlock);
            PlayButton.Click += (sender, e) => ButtonClick.Play(_playback);
            SkipButton.Click += (sender, e) => ButtonClick.Skip(_playback);
            ResumePauseButton.Click += (sender, e) => ButtonClick.ResumePause(_playback);
            ToggleShufflingButton.Click += (sender, e) => ButtonClick.ToggleShuffling(_playback);

            _playback.CurrentSongChanged += (sender, e) => OnChanges.OnCurrentSongChanges(_playback, SetupLyricSync, CurrentLyricTextBlock, NextLyricTextBlock);

            //Library Events
            //PlayNowLibraryMenuItem.Click += (sender, e) => MenuItemClick.PlayNow(sender, _playback);
            //AddToPlaylistLibraryMenuItem.Click += (sender, e) => MenuItemClick.AddToPlaylist(sender, _playlist);
            //ModifySongLibraryMenuItem.Click += (sender, e) => MenuItemClick.ModifySong(sender);
            //DeleteSongLibraryMenuItem.Click += (sender, e) => MenuItemClick.DeleteSong(sender);

            //Playlist Events
            PlayNowPlaylistMenuItem.Click += (sender, e) => MenuItemClick.PlayNow(sender, _playback);
            RemoveSongPlaylistMenuItem.Click += (sender, e) => MenuItemClick.RemoveSong(sender, _playlist);

            //Slider Events
            GeneralVolumeSlider.ValueChanged += (sender, e) => _playback?.SetGeneralVolume((float)GeneralVolumeSlider.Value / 100);
            VocalVolumeSlider.ValueChanged += (sender, e) => _playback?.SetVocalVolume((float)VocalVolumeSlider.Value / 100);
            MusicVolumeSlider.ValueChanged += (sender, e) => _playback?.SetMusicVolume((float)MusicVolumeSlider.Value / 100);
        }

        private void UpdateLyricDisplay(double currentTime) {
            var currentLyricLine = _playback.LyricSync.GetCurrentLyric(currentTime);
            var nextLyricLine = _playback.LyricSync.GetNextLyric(currentTime);

            if (currentLyricLine != null) {
                if (currentLyricLine.Text != _lastLyricLineText) {
                    _lastLyricLineText = currentLyricLine.Text;
                    var timeBeforeFadingOut = TimeSpan.FromSeconds(currentLyricLine.Duration.Seconds);
                    if (currentLyricLine.Text != "") {
                        if (!_displayCurrentLyricTextTop) {
                            CurrentLyricTextBlock.Text = currentLyricLine.Text;
                            FadeInFadeOutLyrics.StartLyricsAnimation(CurrentLyricTextBlock, timeBeforeFadingOut);
                        }
                        else {
                            NextLyricTextBlock.Text = currentLyricLine.Text;
                            FadeInFadeOutLyrics.StartLyricsAnimation(NextLyricTextBlock, timeBeforeFadingOut);
                        }
                        _displayCurrentLyricTextTop = !_displayCurrentLyricTextTop;
                    }
                    Logger.Log($"startTime: {currentLyricLine.StartTime}, endTime: {currentLyricLine.EndTime} => {currentLyricLine.Text}");
                }
            }
            else {
                if (_lastLyricLineText != null) {
                    Logger.Warning($"{currentTime} currentLyric => NULL");
                }
                _lastLyricLineText = null;
            }
        }

        private void SetupLyricSync() {
            // Create a timer for real-time lyric updates
            DispatcherTimer lyricTimer = new DispatcherTimer {
                Interval = TimeSpan.FromMilliseconds(100)  // Timer fires every 100 milliseconds
            };
            lyricTimer.Tick += (s, e) => {
                if (_playback.VocalMp3Reader != null) {
                    UpdateLyricDisplay(_playback.VocalMp3Reader.CurrentTime.TotalMilliseconds);
                }
                else {
                    lyricTimer.Stop();
                }
            };
            lyricTimer.Start();
        }

        // Helper method to find a visual child of a specific type
        private T FindVisualChild<T>(DependencyObject parent) where T : DependencyObject {
            try {
                int childrenCount = VisualTreeHelper.GetChildrenCount(parent);
                for (int i = 0; i < childrenCount; i++) {
                    var child = VisualTreeHelper.GetChild(parent, i);
                    if (child is T typedChild) {
                        return typedChild;
                    }
                    var childOfChild = FindVisualChild<T>(child);
                    if (childOfChild != null) {
                        return childOfChild;
                    }
                }
                return null;
            }
            catch (Exception ex) {
                Logger.Error($"Error in FindVisualChild: {ex.Message}");
                return null;
            }
        }
    }
}