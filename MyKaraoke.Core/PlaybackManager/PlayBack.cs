using Microsoft.Data.Sqlite;
using Microsoft.VisualBasic.Logging;
using MyKaraoke.Service.Database;
using MyKaraoke.Service.Logging;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace MyKaraoke.Core.PlaybackManager {
    public class Playback {
        public Playlist Playlist;
        private Song _currentSong;

        // Audio readers and output devices
        private Mp3FileReader _vocalMp3Reader;
        private Mp3FileReader _musicMp3Reader;
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

            foreach (Song song in Playlist.Songs){
                Logger.Log(song.Title);
            }
        }
        
        public void Play() {
            try {
                _currentSong ??= Playlist.Next();
                if (_currentSong == null) return;
                
                // Retrieve audio data from database
                byte[] vocalData = _currentSong.GetVocalData();
                byte[] musicData = _currentSong.GetMusicData();

                // Stop previous playback
                _vocalOutput?.Stop();
                _musicOutput?.Stop();

                // Dispose previous readers if needed
                _vocalMp3Reader?.Dispose();
                _vocalMp3Reader?.Dispose();

                var vocalMemoryStream = new MemoryStream(vocalData);
                _vocalMp3Reader = new Mp3FileReader(vocalMemoryStream);

                var vocalPcmStream = new MediaFoundationResampler(_vocalMp3Reader, WaveFormat.CreateIeeeFloatWaveFormat(44100, 2));
                vocalPcmStream.ResamplerQuality = 60;
                _vocalVolumeProvider = new VolumeSampleProvider(vocalPcmStream.ToSampleProvider());

                _vocalOutput.Init(_vocalVolumeProvider);
                _vocalOutput.Play();

                var musicMemoryStream = new MemoryStream(musicData);
                _musicMp3Reader = new Mp3FileReader(musicMemoryStream);

                var musicPcmStream = new MediaFoundationResampler(_musicMp3Reader, WaveFormat.CreateIeeeFloatWaveFormat(44100, 2));
                musicPcmStream.ResamplerQuality = 60;
                _musicVolumeProvider = new VolumeSampleProvider(musicPcmStream.ToSampleProvider());

                _musicOutput.Init(_musicVolumeProvider);
                _musicOutput.Play();

                UpdateVolume();

                // Attach event handlers for playback stopped
                _vocalOutput.PlaybackStopped += OnPlaybackStopped;
                _musicOutput.PlaybackStopped += OnPlaybackStopped;


                Logger.Log($"Now playing: {_currentSong.Title}");
            } catch (Exception ex) {
                Logger.Error($"Playback error: {ex.Message}");
            }
        }

        public void PlayNext() {
            _currentSong = Playlist.Next();
            Logger.Log($"Will play this : {_currentSong.Title}");
            if (_currentSong != null) {
                Play();
            }
        }

        public void Skip(){
            if (Playlist.Songs.Count == 0){
                Logger.Log("Playlist is empty.");
                return;
            };
            Logger.Log("Skipping current song");
            Stop();
            PlayNext();
        }

        private void OnPlaybackStopped(object sender, StoppedEventArgs e) {
            // Check if both outputs have stopped
            try {
                if (_vocalMp3Reader != null && _musicMp3Reader != null &&
                    _vocalMp3Reader.Position >= _vocalMp3Reader.Length &&
                    _musicMp3Reader.Position >= _musicMp3Reader.Length) {
                            
                    // Clean up resources for the current song
                    _vocalMp3Reader?.Dispose();
                    _musicMp3Reader?.Dispose();
                    _vocalMp3Reader = null;
                    _musicMp3Reader = null;

                    // Move to the next song
                    _currentSong = Playlist.Next();
                    if (_currentSong != null) {
                        Play();
                    } else {
                        // No more songs in the playlist
                        Logger.Log("Playlist is empty.");
                    }
                }

                // Log any errors during playback
                if (e.Exception != null) {
                    Logger.Error($"Playback stopped due to an error: {e.Exception.Message}");
                }
            }
            catch(Exception ex) {
                Logger.Fatal(ex);
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
