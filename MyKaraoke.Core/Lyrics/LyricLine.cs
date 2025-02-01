using Microsoft.VisualBasic.Logging;
using MyKaraoke.Service.Logging;
using System.Text.RegularExpressions;

namespace MyKaraoke.Core.Lyrics {
    public class LyricLine {
        public string Text { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan Duration { get; set; }
        public bool IsHighlighted { get; set; }

        public bool IsActive(TimeSpan currentPosition) {
            return currentPosition >= StartTime &&
                   currentPosition <= (StartTime + Duration);
        }
    }
}