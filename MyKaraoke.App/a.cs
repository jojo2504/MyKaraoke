using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace MyKaraokeApp {
    public class StringToVisibilityConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            // If the string is null or empty, show the placeholder (Visible)
            // Otherwise hide the placeholder (Collapsed)
            if (string.IsNullOrEmpty(value as string))
                return Visibility.Visible;
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            // ConvertBack is not needed for this scenario
            throw new NotImplementedException();
        }
    }
}