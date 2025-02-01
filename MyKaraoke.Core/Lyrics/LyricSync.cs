using Microsoft.VisualBasic.Logging;
using MyKaraoke.Service.Logging;
using System.Text.RegularExpressions;

namespace MyKaraoke.Core.Lyrics {
    public class LyricSync {
        public List<LyricLine> Lines { get; private set; } = new List<LyricLine>();

        public void ParseLyrics(string lyricsContent) {
            Lines.Clear();
            Logger.Log($"Input length: {lyricsContent.Length}");

            // Regex that captures each <timestamp> and word
            var regex = new Regex(@"\[(\d{2}:\d{2}\.\d{2})\](.*)");

            var wordMatches = regex.Matches(lyricsContent);
            if (wordMatches.Count == 0) {
                regex = new Regex(@"\[(\d{2}:\d{2}\.\d{3})\](.*)");
                wordMatches = regex.Matches(lyricsContent);
            }
            Logger.Log($"Total matches found: {wordMatches.Count}");

            for (int i = 0; i < wordMatches.Count; i++) {
                var match = wordMatches[i];
                if (i == 0) {
                    Logger.Log($"{match.Groups[0].Value}".Trim());
                    Logger.Log($"{match.Groups[1].Value}");
                    Logger.Log($"{match.Groups[2].Value}".Trim());
                }

                if (TimeSpan.TryParse("0:" + match.Groups[1].Value, out TimeSpan startTime)) {
                    string text = match.Groups[2].Value.Trim();
                    TimeSpan endTime;

                    if (i < wordMatches.Count - 1) {
                        if (!TimeSpan.TryParse("0:" + wordMatches[i + 1].Groups[1].Value, out endTime)) {
                            endTime = startTime + TimeSpan.FromMilliseconds(500);
                        }
                    }
                    else {
                        endTime = startTime + TimeSpan.FromMilliseconds(500);
                    }

                    Lines.Add(new LyricLine {
                        Text = text,
                        StartTime = startTime,
                        Duration = endTime - startTime,
                        IsHighlighted = false
                    });
                }
            }

            Lines.Sort((a, b) => a.StartTime.CompareTo(b.StartTime));
            Logger.Log($"Lines parsed: {Lines.Count}");

            if (Lines.Count > 0) {
                Logger.Important("Printing all lines in lines");
                foreach (var line in Lines) {
                    Logger.Log($"{line.StartTime}: {line.Text}");
                }
            }
        }

        public LyricLine GetCurrentLyric(double currentTime) {
            // Find the lyric line where the current position falls within its start time and duration
            var currentLyric = Lines.FirstOrDefault(l =>
                currentTime >= l.StartTime.TotalMilliseconds &&
                currentTime < (l.StartTime.TotalMilliseconds + l.Duration.TotalMilliseconds));
            if (currentLyric != null) {
                //Logger.Warning($"currentLyric.Text {currentLyric.Text} at {currentTime}");
                // Highlight the current lyric
                currentLyric.IsHighlighted = true;
                return currentLyric;
            }

            // If no lyric is found, return null
            return null;
        }

        public LyricLine GetNextLyric(double currentTime) {
            var nextLyric = Lines.FirstOrDefault(l =>
                l.StartTime.TotalMilliseconds > currentTime);
            if (nextLyric != null) {
                nextLyric.IsHighlighted = true;
                return nextLyric;
            }
            return null;
        }
    }
}