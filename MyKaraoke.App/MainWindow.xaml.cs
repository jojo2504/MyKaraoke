using System.Windows;
using Microsoft.Win32;
using MyKaraoke.Core.PlaybackManager;
using MyKaraoke.Service.Database;
using System.IO;
using MyKaraoke.Service.Logging;
using System.Windows.Controls;
using static MyKaraoke.Service.EnvironmentSetup.Constants;
using MyKaraoke.Core.Library;
using MyKaraoke.Core.Lyrics;
using System.Windows.Threading;
using MyKaraoke.Service.Lyrics;
using System.Windows.Media;
using System.Globalization; // For CultureInfo
using System.Windows.Data;
using Microsoft.VisualBasic.Logging; // For IValueConverter

namespace MyKaraokeApp {
    public partial class MainWindow : Window {
        private Window _lyricsWindow;
        private Playlist _playlist;
        private Playback _playback;
        private string _selectedVocalPath = "";
        private string _selectedMusicPath = "";
        private string _vocalHash = "";
        private string _musicHash = "";
        private string _lastLyricLineText = "";
        private LyricSync _lyricSync = new LyricSync();

        // Default constructor required by WPF (parameterless)
        public MainWindow() : this([]) {
        }
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
            DatabaseSongsListView.ItemsSource = Library.GetAllSongs();
            SongListView.ItemsSource = _playlist.Songs;
            Logger.Success("Initialized ListViews");
        }

        private void UpdateLibrary() {
            DatabaseSongsListView.ItemsSource = Library.GetAllSongs();
            foreach (Song song in Library.GetAllSongs()) {
                Logger.Important($"SONG INFO {song.Title} {song.Artist}");
            }
            Logger.Success("Updated Library");
        }

        private void InitializePlayback() {
            _playlist = new Playlist();
            _playback = new Playback(_playlist);
            SongListView.ItemsSource = _playlist.Songs;
        }

        private void WireUpEventHandlers() {
            VocalUploadButton.Click += (s, e) => UploadFile(true);
            MusicUploadButton.Click += (s, e) => UploadFile(false);
            ConfirmUploadButton.Click += (s, e) => AddSongToDatabase();
            PlayButton.Click += PlayButton_Click;
            SkipButton.Click += SkipButton_Click;

            GeneralVolumeSlider.ValueChanged += (s, e) =>
                _playback?.SetGeneralVolume((float)GeneralVolumeSlider.Value / 100);
            VocalVolumeSlider.ValueChanged += (s, e) =>
                _playback?.SetVocalVolume((float)VocalVolumeSlider.Value / 100);
            MusicVolumeSlider.ValueChanged += (s, e) =>
                _playback?.SetMusicVolume((float)MusicVolumeSlider.Value / 100);
        }

        private void UploadFile(bool isVocal) {
            OpenFileDialog openFileDialog = new OpenFileDialog {
                Filter = "MP3 files (*.mp3)|*.mp3|All files (*.*)|*.*"
            };

            if (openFileDialog.ShowDialog() == true) {
                string selectedPath = openFileDialog.FileName;
                string fileName = Path.GetFileName(selectedPath);
                string fileHash = FileHasher.ComputeSHA256(selectedPath);

                // Store the selected file path and hash locally
                if (isVocal) {
                    _selectedVocalPath = selectedPath;
                    _vocalHash = fileHash;
                    VocalFileNameText.Text = fileName;
                }
                else {
                    _selectedMusicPath = selectedPath;
                    _musicHash = fileHash;
                    MusicFileNameText.Text = fileName;
                }
            }
        }

        private void AddSongToDatabase() {
            // Ensure all required fields are filled in
            if (string.IsNullOrEmpty(SongNameTextBox.Text) ||
                string.IsNullOrEmpty(ArtistNameTextBox.Text) ||
                string.IsNullOrEmpty(_selectedVocalPath) ||
                string.IsNullOrEmpty(_selectedMusicPath)) {
                MessageBox.Show("Please provide a song name and upload both vocal and music files.");
                return;
            }

            try {
                // Read file data
                byte[] vocalData = File.ReadAllBytes(_selectedVocalPath);
                byte[] musicData = File.ReadAllBytes(_selectedMusicPath);

                // Insert vocal and music files into the database
                DatabaseHelper.InsertFileHashToDatabase(_vocalHash);
                FileHasher.SaveFileToDisk(_vocalHash, vocalData);
                Logger.Success("Inserted vocalHash and vocalData to database and directory");

                FileHasher.SaveFileToDisk(_musicHash, musicData);
                DatabaseHelper.InsertFileHashToDatabase(_musicHash);
                Logger.Success("Inserted musicHash and musicData to database and directory");

                // Insert the song metadata (name, vocal hash, music hash) into the database
                DatabaseHelper.UploadSong(SongNameTextBox.Text, ArtistNameTextBox.Text, _vocalHash, _musicHash);
                Logger.Success("Uploaded song in database");

                // Refresh Song List
                Logger.Log("Refreshing song list view");
                SongListView.Items.Refresh();
                Logger.Success("");

                // Reset fields after upload
                SongNameTextBox.Text = "";
                ArtistNameTextBox.Text = "";
                VocalFileNameText.Text = "No vocal file selected";
                MusicFileNameText.Text = "No music file selected";
                _selectedVocalPath = "";
                _selectedMusicPath = "";
                UpdateLibrary();
            }
            catch (Exception ex) {
                Logger.Error(ex);
            }
        }

        private void UpdateLyricDisplay(double currentTime) {
            var currentLyricLine = _lyricSync.GetCurrentLyric(currentTime);
            if (currentLyricLine != null) {
                if (currentLyricLine.Text != _lastLyricLineText) {
                    _lastLyricLineText = currentLyricLine.Text;
                    Logger.Log($"{currentTime} currentLyric => {currentLyricLine.Text}");
                }
                // Update text with styling
                LyricsTextBlock.Text = currentLyricLine.Text;

                // Optional: Add highlighting or animation
                LyricsTextBlock.Foreground = currentLyricLine.IsHighlighted
                    ? Brushes.Red
                    : Brushes.Black;
            }
            else {
                if (_lastLyricLineText != null) {
                    Logger.Warning($"{currentTime} currentLyric => NULL");
                }
                _lastLyricLineText = null;
                LyricsTextBlock.Text = "Waiting for lyrics...";
            }
        }

        private void SetupLyricSync() {
            // Create a timer for real-time lyric updates
            DispatcherTimer lyricTimer = new DispatcherTimer {
                Interval = TimeSpan.FromMilliseconds(100)  // Timer fires every 100 milliseconds
            };


            lyricTimer.Tick += (s, e) => {
                if (_playback.VocalMp3Reader != null) {
                    // Pass the current position to the lyric display method
                    UpdateLyricDisplay(_playback.VocalMp3Reader.CurrentTime.TotalMilliseconds);
                }
                else {
                    Logger.Fatal("_playback.VocalMp3Reader is null");
                }
            };

            lyricTimer.Start();
        }

        private void LoadLyricsForSong(Song song) {
            string trackName = song.Title;
            string artistName = song.Artist;

            string scriptPath = Path.Combine(SolutionRoot, "scripts/search_lyrics.py");
            string[] scriptArguments = new string[] { trackName, artistName };
            string processedLyrics = PythonScriptRunner.ExecutePythonScriptWithCPython(scriptPath, scriptArguments);

            // Parse and use the processed lyrics (e.g., update the display)
            _lyricSync.ParseLyrics(processedLyrics);
        }

        // Modify Play method to include lyrics
        private void PlayButton_Click(object sender, RoutedEventArgs e) {
            if (_playback.CurrentSong == null) {
                if (_playback.Playlist.Songs.Count == 0) {
                    Logger.Warning("There is no songs in the playlist.");
                    return;
                }
                else {
                    _playback.CurrentSong = _playback.Playlist.Next();
                }
            }
            else {
                Logger.Warning("A song is already playing");
                Logger.Log("Stopping instead (temporary)");
                _playback.Stop();
                return;
            }
            LoadLyricsForSong(_playback.CurrentSong);
            _playback.Play();
            SetupLyricSync();
        }

        private void DeleteSongButton_Click(object sender, RoutedEventArgs e) {
            if (SongListView.SelectedItem is Song selectedSong) {
                _playlist.RemoveSong(selectedSong);
                SongListView.Items.Refresh();
                // Optional: Add database delete method here
            }
        }

        private void AddToPlaylistButton_Click(object sender, RoutedEventArgs e) {
            // Get the button that was clicked
            Button button = sender as Button;

            // Get the data context (Song) of the button's parent
            if (button?.DataContext is Song selectedSong) {
                // Add the song to the playlist
                _playlist.AddSong(selectedSong);
            }
        }

        private void SkipButton_Click(object sender, RoutedEventArgs e) {
            _playback.Skip();
        }

        private void MaximizeLyricsButton_Click(object sender, RoutedEventArgs e) {
            if (_lyricsWindow == null || !_lyricsWindow.IsVisible) {
                _lyricsWindow = new Window {
                    Title = "Lyrics View",
                    Width = 800,
                    Height = 600,
                    WindowStartupLocation = WindowStartupLocation.CenterScreen,
                    Background = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFrom("#1A1A2E")
                };

                var scrollViewer = new ScrollViewer();
                var textBlock = new TextBlock {
                    Text = LyricsTextBlock.Text,
                    FontSize = 32,
                    TextAlignment = TextAlignment.Center,
                    TextWrapping = TextWrapping.Wrap,
                    Foreground = System.Windows.Media.Brushes.White,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(20)
                };

                scrollViewer.Content = textBlock;
                _lyricsWindow.Content = scrollViewer;

                // Bind the text to keep it synchronized
                var binding = new System.Windows.Data.Binding("Text") {
                    Source = LyricsTextBlock,
                    Mode = System.Windows.Data.BindingMode.TwoWay,
                    UpdateSourceTrigger = System.Windows.Data.UpdateSourceTrigger.PropertyChanged
                };
                textBlock.SetBinding(TextBlock.TextProperty, binding);

                _lyricsWindow.Show();
            }
            else {
                _lyricsWindow.Activate();
            }
        }

        private void AddToPlaylist_Click(object sender, RoutedEventArgs e) {
            Logger.Log("AddToPlaylist_Click");
            var menuItem = sender as MenuItem;
            if (menuItem == null) {
                Logger.Log("Sender is not a MenuItem");
                return;
            }
            // Get the DataContext of the MenuItem (the song that was right-clicked)
            var selectedSong = menuItem.DataContext as Song;
            if (selectedSong == null) {
                Logger.Log("No song selected");
                return;
            }
            // Add the selected song to the playlist
            _playlist.Songs.Add(selectedSong);
            Logger.Log($"Added '{selectedSong.Title}' to the playlist via context menu.");
        }

        private void RemoveSong_Click(object sender, RoutedEventArgs e) {
            Logger.Log("RemoveSong_Click");
        }

        private void DeleteSongButton(object sender, RoutedEventArgs e) {
            Logger.Log("DeleteSongButton");
        }

        private void PauseResume_Click(object sender, RoutedEventArgs e) {
            if (_playback.IsPaused) {
                _playback.Resume();
                _playback.IsPaused = false;
            }
            else {
                _playback.Pause();
                _playback.IsPaused = true;
            }
        }
    }
}