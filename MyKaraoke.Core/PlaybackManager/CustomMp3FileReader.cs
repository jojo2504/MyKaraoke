using Microsoft.VisualBasic.Logging;
using MyKaraoke.Service.Logging;
using NAudio.Wave;

namespace MyKaraoke.Core.PlaybackManager {
    public class CustomMp3FileReader : Mp3FileReader {
        public double BytesPerMillisecond { get; set; }

        public TimeSpan CustomCurrentTime {
            get {
                Logger.Important($"{Position}");
                Logger.Important($"{BytesPerMillisecond}");
                //Logger.Important($"Current time before conversion : {Position / BytesPerMillisecond}");
                return TimeSpan.FromMilliseconds(Position / BytesPerMillisecond);
            }
            set {
                Position = (long)(value.TotalMilliseconds * BytesPerMillisecond);
            }
        }

        // Constructor to set BytesPerMillisecond
        public CustomMp3FileReader(Stream inputStream, double bytesPerMillisecond) : base(inputStream) {
            BytesPerMillisecond = bytesPerMillisecond;
        }
    }
}