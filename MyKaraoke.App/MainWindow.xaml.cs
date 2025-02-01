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
using MyKaraoke.Service.EnvironmentSetup;
using Microsoft.Data.Sqlite;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Windows.Input;
using System.Windows.Media.Animation; // For IValueConverter

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
        private bool _displayCurrentLyricTextTop = true;
        private bool _displayedFirstLyricText = false;

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
            DatabaseSongsListView.ItemsSource = Library.Songs;
            SongListView.ItemsSource = _playlist.Songs;
            Logger.Success("Initialized ListViews");
        }

        private void UpdateLibrary() {
            Library.FetchAllSongs();
            CollectionViewSource.GetDefaultView(DatabaseSongsListView.ItemsSource)?.Refresh();
            Logger.Success("Updated Library");
        }

        private void AddSongToLibrary(Song song) {
            Library.AddSongToLibrary(song);
            DatabaseSongsListView.ItemsSource = Library.Songs;
        }


        private void InitializePlayback() {
            _playlist = new Playlist();
            _playback = new Playback(_playlist);
            _playback.CurrentSongChanged += OnCurrentSongChanged;

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

        private void OnCurrentSongChanged(object sender, EventArgs e) {
            // Perform any actions when the current song changes
            Logger.Log($"Current song changed: {_playback.CurrentSong?.Title}");
            CurrentLyricTextBlock.Text = "";
            NextLyricTextBlock.Text = "";
            // If you want to sync lyrics when the song changes, call SetupLyricSync
            LoadLyrics(_playback.CurrentSong);
            SetupLyricSync();
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

        private async Task AddSongToDatabase() {
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

                ProcessHashData(_vocalHash, vocalData);
                ProcessHashData(_musicHash, musicData);
                string LRCHash = await ProcessLyricsAsync();

                // Insert the song metadata (name, vocal hash, music hash) into the database
                DatabaseHelper.UploadSong(SongNameTextBox.Text, ArtistNameTextBox.Text, _vocalHash, _musicHash, LRCHash);

                SongListView.Items.Refresh();

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

        private void ProcessHashData(string hash, byte[] data) {
            DatabaseHelper.InsertFileHashToDatabase(hash);
            FileHasher.SaveFileToDisk(hash, data);
            Logger.Log("Inserted vocalHash and vocalData to database and directory");
        }

        async Task<string> ProcessLyricsAsync() {
            try {
                Logger.Log("Trying to search for lyrics");

                string scriptPath = Path.Combine(SolutionRoot, "scripts/search_lyrics.py");
                string[] scriptArguments = new string[] { SongNameTextBox.Text, ArtistNameTextBox.Text };

                string lyrics = await Task.Run(() => PythonScriptRunner.ExecutePythonScriptWithCPython(scriptPath, scriptArguments));
                byte[] lyricsData = Encoding.UTF8.GetBytes(lyrics);
                string lyricsHash = FileHasher.ComputeSHA256FromBytes(lyricsData);

                await Task.Run(() => DatabaseHelper.InsertFileHashToDatabase(lyricsHash));
                await Task.Run(() => FileHasher.SaveFileToDisk(lyricsHash, lyricsData));

                Logger.Success("Successfully inserted lyrics hash and saved lyrics data.");

                return lyricsHash;
            }
            catch (Exception ex) {
                Logger.Error($"Error processing lyrics: {ex.Message}");
                return null;
            }
        }

        private void UpdateLyricDisplay(double currentTime) {
            var displayedLyricLine = _playback.LyricSync.GetCurrentLyric(currentTime);
            var nextLyricLine = _playback.LyricSync.GetCurrentLyric(currentTime);

            if (displayedLyricLine != null) {
                if (displayedLyricLine.Text != _lastLyricLineText) {
                    _lastLyricLineText = displayedLyricLine.Text;
                    var timeBeforeFadingOut = displayedLyricLine.Duration.Seconds + nextLyricLine.Duration.Seconds/2;
                    if (displayedLyricLine.Text != "") {
                        if (!_displayCurrentLyricTextTop) {
                            CurrentLyricTextBlock.Text = displayedLyricLine.Text;
                            StartLyricsAnimation(CurrentLyricTextBlock, timeBeforeFadingOut);
                        }
                        else {
                            NextLyricTextBlock.Text = displayedLyricLine.Text;
                            StartLyricsAnimation(NextLyricTextBlock, timeBeforeFadingOut);
                        }
                        _displayCurrentLyricTextTop = !_displayCurrentLyricTextTop;
                    }
                    Logger.Log($"{currentTime} currentLyric => {displayedLyricLine.Text}");
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
                return;
            }

            _playback.Play();
        }

        public void LoadLyrics(Song song) {
            byte[] LRCData = song.GetLRCData();
            string lyrics = Encoding.UTF8.GetString(LRCData);
            _playback.LyricSync.ParseLyrics(lyrics);
        }

        private void DeleteSongButton_Click(object sender, RoutedEventArgs e) {
            if (SongListView.SelectedItem is Song selectedSong) {
                _playlist.RemoveSong(selectedSong);
                SongListView.Items.Refresh();
                // Optional: Add database delete method here
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
                    Text = CurrentLyricTextBlock.Text,
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
                    Source = CurrentLyricTextBlock,
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
            var menuItem = sender as MenuItem;
            // Get the DataContext of the MenuItem (the song that was right-clicked)
            var selectedSong = menuItem?.DataContext as Song;
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

        private void ModifySong_Click(object sender, RoutedEventArgs e) {
            Logger.Log("ModifySong_Click");
        }

        private void PlayNow_Click(object sender, RoutedEventArgs e) {
            var menuItem = sender as MenuItem;
            var selectedSong = menuItem.DataContext as Song;
            _playback.CurrentSong = selectedSong;
            _playback.Play();
        }

        private void ListBoxItem_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e) {
            if (sender is ListBoxItem item) {
                item.IsSelected = true; // Select the item
            }
        }

        private void ListBoxItem_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
            if (sender is ListBoxItem item) {
                item.IsSelected = true; // Select the item
            }
        }

        private void StartLyricsAnimation(TextBlock textBlock, long FadeOutBeginTime) {
            // Clear the local value first
            textBlock.ClearValue(UIElement.OpacityProperty);

            var storyboard = new Storyboard();

            var fadeIn = new DoubleAnimation {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromSeconds(0.6),
                AutoReverse = false,
                FillBehavior = FillBehavior.HoldEnd
            };

            var fadeOut = new DoubleAnimation {
                From = 1,
                To = 0,
                Duration = TimeSpan.FromSeconds(0.6),
                BeginTime = TimeSpan.FromSeconds(FadeOutBeginTime),
                AutoReverse = false,
                FillBehavior = FillBehavior.HoldEnd
            };

            // Add animations to the Storyboard
            Storyboard.SetTarget(fadeIn, textBlock);
            Storyboard.SetTargetProperty(fadeIn, new PropertyPath(UIElement.OpacityProperty));
            storyboard.Children.Add(fadeIn);

            Storyboard.SetTarget(fadeOut, textBlock);
            Storyboard.SetTargetProperty(fadeOut, new PropertyPath(UIElement.OpacityProperty));
            storyboard.Children.Add(fadeOut);

            // Start the Storyboard
            storyboard.Begin();
        }
    }
}