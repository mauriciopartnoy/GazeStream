using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace GazeStream.Converters
{
    public class EnumToVisibilityConverter : IValueConverter
    {
        public bool Invert { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
                return Visibility.Collapsed;

            string enumValue = value.ToString();
            string targetValue = parameter.ToString();

            bool isMatch = enumValue.Equals(targetValue, StringComparison.Ordinal);

            if (Invert)
                isMatch = !isMatch;

            return isMatch ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
