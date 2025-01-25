using System.Windows;
using Microsoft.Win32;
using MyKaraoke.Core.PlaybackManager;
using MyKaraoke.Service.Database;
using System.IO;
using MyKaraoke.Service.Logging;
using System.Windows.Controls;
using static MyKaraoke.Service.EnvironmentSetup.Constants;
using MyKaraoke.Core.Library;

namespace MyKaraokeApp {
    public partial class MainWindow : Window {
        private Playlist _playlist;
        private Playback _playback;
        private string _selectedVocalPath = "";
        private string _selectedMusicPath = "";
        private string _vocalHash = "";
        private string _musicHash = "";

        // Default constructor required by WPF (parameterless)
        public MainWindow() : this([]) {
        }
        public MainWindow(string[] args) {
            Logger.Log($"BaseAppDataPath => {BaseAppDataPath}");
            Logger.Log($"LogsPath => {LogsPath}");
            Logger.Log($"DatabasePath => {DatabasePath}");
            Logger.Log($"SongsPath => {SongsPath}");
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

        private void InitializeListViews(){
            // Load songs from database
            DatabaseSongsListView.ItemsSource = Library.GetAllSongs();
            SongListView.ItemsSource = _playlist.Songs;
            Logger.Success("Initialized ListViews");
        }

        private void UpdateLibrary(){
            DatabaseSongsListView.ItemsSource = Library.GetAllSongs();
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
            DeleteSongButton.Click += DeleteSongButton_Click;

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
                DatabaseHelper.InsertFileToDatabase(_vocalHash, vocalData);
                FileHasher.SaveFileToDisk(_vocalHash, vocalData);
                Logger.Success("Inserted vocalHash and vocalData to database and directory");

                FileHasher.SaveFileToDisk(_musicHash, vocalData);
                DatabaseHelper.InsertFileToDatabase(_musicHash, musicData);
                Logger.Success("Inserted musicHash and musicData to database and directory");

                // Insert the song metadata (name, vocal hash, music hash) into the database
                DatabaseHelper.UploadSong(SongNameTextBox.Text, _vocalHash, _musicHash);
                Logger.Success("Uploaded song in database");

                // Refresh Song List
                Logger.Log("Refreshing song list view");
                SongListView.Items.Refresh();
                Logger.Success("");

                // Reset fields after upload
                SongNameTextBox.Text = "";
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

        private void PlayButton_Click(object sender, RoutedEventArgs e) {
            _playback.Play();
        }

        private void SkipButton_Click(object sender, RoutedEventArgs e) {
            _playback.Skip();
        }
    }
}