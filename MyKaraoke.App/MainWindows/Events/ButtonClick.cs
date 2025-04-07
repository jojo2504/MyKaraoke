using System.Windows;
using System.Windows.Controls;
using MyKaraoke.Core.PlaybackManager;
using MyKaraoke.Service.Logging;
using Microsoft.Win32;
using System.IO;
using MyKaraoke.Core.Database;
using MyKaraoke.Service.PythonServer;
using System.Text;
using static MyKaraoke.Service.EnvironmentSetup.Constants;

namespace MyKaraokeApp.MainWindows.Events {
    public class ButtonClick {
        public static void Play(Playback playback) {
            if (playback.CurrentSong == null) {
                if (playback.Playlist.Songs.Count == 0) {
                    Logger.Warning("There is no songs in the playlist.");
                    return;
                }
                else {
                    playback.CurrentSong = playback.Playlist.Next();
                }
            }
            else {
                Logger.Warning("A song is already playing");
                return;
            }
            playback.Play();
        }

        public static void ResumePause(Playback playback) {
            if (playback.IsPaused) {
                playback.Resume();
                playback.IsPaused = false;
            }
            else {
                playback.Pause();
                playback.IsPaused = true;
            }
        }

        public static void Skip(Playback playback) {
            playback.Skip();
        }

        public static void ToggleShuffling(Playback playback) {
            playback.IsShuffling = !playback.IsShuffling;
        }

        public static void MaximizeLyrics(Window lyricsWindow, TextBlock CurrentLyricTextBlock) {
            if (lyricsWindow == null || !lyricsWindow.IsVisible) {
                lyricsWindow = new Window {
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
                lyricsWindow.Content = scrollViewer;

                // Bind the text to keep it synchronized
                var binding = new System.Windows.Data.Binding("Text") {
                    Source = CurrentLyricTextBlock,
                    Mode = System.Windows.Data.BindingMode.TwoWay,
                    UpdateSourceTrigger = System.Windows.Data.UpdateSourceTrigger.PropertyChanged
                };
                textBlock.SetBinding(TextBlock.TextProperty, binding);

                lyricsWindow.Show();
            }
            else {
                lyricsWindow.Activate();
            }
        }

        public static (string, string) UploadFile(bool isVocal, TextBlock VocalFileNameText, TextBlock MusicFileNameText) {
            OpenFileDialog openFileDialog = new OpenFileDialog {
                Filter = "MP3 files (*.mp3)|*.mp3|All files (*.*)|*.*"
            };
            if (openFileDialog.ShowDialog() == true) {
                var selectedPath = openFileDialog.FileName;
                var fileName = Path.GetFileName(selectedPath);
                var fileHash = FileHasher.ComputeSHA256(selectedPath);

                // Store the selected file path and hash locally
                if (isVocal) {
                    VocalFileNameText.Text = fileName;
                }
                else {
                    MusicFileNameText.Text = fileName;
                }
                return (selectedPath, fileHash);
            }
            return (null, null);
        }

        public static async Task AddSongToDatabase(
            TextBox SongNameTextBox,
            TextBox ArtistNameTextBox,
            TextBlock VocalFileNameText,
            TextBlock MusicFileNameText,
            ListBox SongListView,
            string selectedVocalPath,
            string selectedMusicPath,
            string vocalHash,
            string musicHash,
            Action UpdateLibrary)
        {
            // Ensure all required fields are filled in
            if (string.IsNullOrEmpty(SongNameTextBox.Text) ||
                string.IsNullOrEmpty(ArtistNameTextBox.Text) ||
                string.IsNullOrEmpty(selectedVocalPath) ||
                string.IsNullOrEmpty(selectedMusicPath)) {
                MessageBox.Show("Please provide a song name and upload both vocal and music files.");
                return;
            }

            try {
                // Read file data
                byte[] vocalData = File.ReadAllBytes(selectedVocalPath);
                byte[] musicData = File.ReadAllBytes(selectedMusicPath);

                ProcessHashData(vocalHash, vocalData);
                ProcessHashData(musicHash, musicData);
                string LRCHash = await ProcessLyricsAsync(SongNameTextBox, ArtistNameTextBox);

                // Insert the song metadata (name, vocal hash, music hash) into the database
                DatabaseHelper.UploadSong(SongNameTextBox.Text, ArtistNameTextBox.Text, vocalHash, musicHash, LRCHash);

                SongListView.Items.Refresh();

                // Reset fields after upload
                SongNameTextBox.Text = "";
                ArtistNameTextBox.Text = "";
                VocalFileNameText.Text = "No vocal file selected";
                MusicFileNameText.Text = "No music file selected";
                selectedVocalPath = "";
                selectedMusicPath = "";
                UpdateLibrary.Invoke();
            }
            catch (Exception ex) {
                Logger.Error(ex);
            }
        }

        public static async Task<string> ProcessLyricsAsync(TextBox SongNameTextBox, TextBox ArtistNameTextBox) {
            try {
                Logger.Log("Trying to search for lyrics");

                string scriptPath = Path.Combine(SolutionRoot, "MyKaraoke.Service/PythonServer/scripts/search_lyrics.py");
                string[] scriptArguments = [SongNameTextBox.Text, ArtistNameTextBox.Text];

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

        public static void ProcessHashData(string hash, byte[] data) {
            DatabaseHelper.InsertFileHashToDatabase(hash);
            FileHasher.SaveFileToDisk(hash, data);
            Logger.Log("Inserted vocalHash and vocalData to database and directory");
        }
    }
}