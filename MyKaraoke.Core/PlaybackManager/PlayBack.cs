using System.Text;
using Microsoft.Data.Sqlite;
using Microsoft.VisualBasic.Logging;
using MyKaraoke.Service.Database;
using MyKaraoke.Service.Logging;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using MyKaraoke.Core.Lyrics;

namespace MyKaraoke.Core.PlaybackManager {
    public class Playback {
        public Playlist Playlist;
        private Song _currentSong;
        public Song CurrentSong {
            get => _currentSong;
            set {
                if (_currentSong != value) {
                    _currentSong = value;
                    OnCurrentSongChanged();  // Raise the event when the song changes
                }
            }
        }
        public bool IsPaused = false;
        public LyricSync LyricSync = new LyricSync();

        // Audio readers and output devices
        public CustomMp3FileReader VocalMp3Reader;
        private CustomMp3FileReader _musicMp3Reader;
        private readonly WaveOutEvent _vocalOutput;
        private readonly WaveOutEvent _musicOutput;

        // Volume properties
        private float _generalVolume = 0.5f;
        private float _vocalVolume = 0.5f;
        private float _musicVolume = 0.5f;
        private VolumeSampleProvider _vocalVolumeProvider;
        private VolumeSampleProvider _musicVolumeProvider;

        public Playback(Playlist playlist) {
            Playlist = playlist;
            _vocalOutput = new WaveOutEvent();
            _musicOutput = new WaveOutEvent();

            foreach (Song song in Playlist.Songs) {
                Logger.Log(song.Title);
            }
        }

        public event EventHandler CurrentSongChanged;
        protected virtual void OnCurrentSongChanged() {
            CurrentSongChanged?.Invoke(this, EventArgs.Empty);
        }

        public void Play() {
            try {
                // Retrieve audio data from database
                byte[] vocalData = CurrentSong.GetVocalData();
                byte[] musicData = CurrentSong.GetMusicData();

                // Stop previous playback
                _vocalOutput?.Stop();
                _musicOutput?.Stop();

                // Dispose previous readers if needed
                VocalMp3Reader?.Dispose();
                VocalMp3Reader?.Dispose();

                var vocalMemoryStream = new MemoryStream(vocalData);
                Logger.Log($"vocalMemoryStream.Length => {vocalMemoryStream.Length}");

                VocalMp3Reader = new CustomMp3FileReader(vocalMemoryStream, CalculateBytesPerMillisecond(vocalData));
                Logger.Log($"VocalMp3Reader.TotalTime => {VocalMp3Reader.TotalTime}");
                Logger.Log($"VocalMp3Reader.TotalTimeInMilliseconds => {VocalMp3Reader.TotalTime.TotalMilliseconds}");

                var vocalPcmStream = new MediaFoundationResampler(VocalMp3Reader, WaveFormat.CreateIeeeFloatWaveFormat(44100, 2));
                vocalPcmStream.ResamplerQuality = 60;
                _vocalVolumeProvider = new VolumeSampleProvider(vocalPcmStream.ToSampleProvider());

                _vocalOutput.Init(_vocalVolumeProvider);
                _vocalOutput.Play();

                var musicMemoryStream = new MemoryStream(musicData);
                _musicMp3Reader = new CustomMp3FileReader(musicMemoryStream, CalculateBytesPerMillisecond(musicData));

                var musicPcmStream = new MediaFoundationResampler(_musicMp3Reader, WaveFormat.CreateIeeeFloatWaveFormat(44100, 2));
                musicPcmStream.ResamplerQuality = 60;
                _musicVolumeProvider = new VolumeSampleProvider(musicPcmStream.ToSampleProvider());

                _musicOutput.Init(_musicVolumeProvider);
                _musicOutput.Play();

                UpdateVolume();

                // Attach event handlers for playback stopped
                if (VocalMp3Reader.TotalTime < _musicMp3Reader.TotalTime) {
                    _vocalOutput.PlaybackStopped += OnPlaybackStopped;
                }
                else {
                    _musicOutput.PlaybackStopped += OnPlaybackStopped;
                }

                Logger.Log($"Now playing: {CurrentSong.Title}");
            }
            catch (Exception ex) {
                Logger.Error($"Playback error: {ex.Message}");
            }
        }

        public void PlayNext() {
            CurrentSong = Playlist.Next();
            Logger.Log($"Will play this : {CurrentSong.Title}");
            if (CurrentSong != null) {
                Play();
            }
        }

        public void Skip() {
            if (Playlist.Songs.Count == 0) {
                Logger.Warning("Playlist is empty.");
                return;
            }
            ;
            Logger.Log("Skipping current song");
            Stop();
            PlayNext();
        }

        // Method to calculate bytes per millisecond
        private double CalculateBytesPerMillisecond(byte[] audioData) {
            using (var memoryStream = new MemoryStream(audioData))
            using (var tempReader = new Mp3FileReader(memoryStream)) {
                return (tempReader.WaveFormat.SampleRate * tempReader.WaveFormat.Channels * tempReader.WaveFormat.BitsPerSample / 8.0) / 1000.0;
            }
        }

        private void OnPlaybackStopped(object sender, StoppedEventArgs e) {
            // Check if both outputs have stopped
            try {
                if (VocalMp3Reader != null && _musicMp3Reader != null &&
                    VocalMp3Reader.Position >= VocalMp3Reader.Length &&
                    _musicMp3Reader.Position >= _musicMp3Reader.Length) {
                    
                    // Clean up resources for the current song
                    VocalMp3Reader?.Dispose();
                    _musicMp3Reader?.Dispose();
                    VocalMp3Reader = null;
                    _musicMp3Reader = null;
                    
                    _currentSong = Playlist.Next(); // For some reasons, 'CurrentSong = Playlist.Next()' doesn't work XD
                    CurrentSong = _currentSong;

                    if (CurrentSong != null) {
                        Play();
                    }
                    else {
                        Logger.Log("Playlist is empty.");
                    }
                }
            }
            catch (Exception ex) {
                Logger.Fatal($"OnPlaybackStopped CRASH : {ex}");
            }
        }

        public void Stop() {
            try {
                // Stop playback on both devices
                _vocalOutput?.Stop();
                _musicOutput?.Stop();

                // Dispose of resources
                _vocalOutput?.Dispose();
                _musicOutput?.Dispose();
            }
            catch (Exception ex) {
                Logger.Error(ex.Message);
            }
        }

        public void Resume() {
            Logger.Log("Resuming the song");
            _vocalOutput?.Play();
            _musicOutput?.Play();
        }

        public void Pause() {
            Logger.Log("Pausing the song");
            _vocalOutput?.Pause();
            _musicOutput?.Pause();
        }

        public void SetGeneralVolume(float volume) {
            _generalVolume = Math.Clamp(volume, 0.0f, 1.0f);
            UpdateVolume();
        }

        public void SetVocalVolume(float volume) {
            _vocalVolume = Math.Clamp(volume, 0.0f, 1.0f);
            UpdateVolume();
        }

        public void SetMusicVolume(float volume) {
            _musicVolume = Math.Clamp(volume, 0.0f, 1.0f);
            UpdateVolume();
        }

        private void UpdateVolume() {
            if (_vocalVolumeProvider != null) {
                // Update the volume for the vocal stream dynamically
                _vocalVolumeProvider.Volume = _generalVolume * _vocalVolume;
            }

            if (_musicVolumeProvider != null) {
                // Update the volume for the music stream dynamically
                _musicVolumeProvider.Volume = _generalVolume * _musicVolume;
            }
        }
    }
}
