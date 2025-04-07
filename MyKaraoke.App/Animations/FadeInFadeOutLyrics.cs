using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace MyKaraokeApp.Animations {
    public static class FadeInFadeOutLyrics {
        public static void StartLyricsAnimation(TextBlock textBlock, TimeSpan FadeOutBeginTime) {
            textBlock.ClearValue(UIElement.OpacityProperty);

            var storyboard = new Storyboard();

            var fadeIn = new DoubleAnimation {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromSeconds(0.3),
                AutoReverse = false,
                FillBehavior = FillBehavior.HoldEnd
            };

            var fadeOut = new DoubleAnimation {
                From = 1,
                To = 0,
                Duration = TimeSpan.FromSeconds(0.6),
                BeginTime = TimeSpan.FromSeconds(FadeOutBeginTime.TotalSeconds),
                AutoReverse = false,
                FillBehavior = FillBehavior.HoldEnd
            };

            Storyboard.SetTarget(fadeIn, textBlock);
            Storyboard.SetTargetProperty(fadeIn, new PropertyPath(UIElement.OpacityProperty));
            storyboard.Children.Add(fadeIn);

            Storyboard.SetTarget(fadeOut, textBlock);
            Storyboard.SetTargetProperty(fadeOut, new PropertyPath(UIElement.OpacityProperty));
            storyboard.Children.Add(fadeOut);

            storyboard.Begin();
        }
    }
}