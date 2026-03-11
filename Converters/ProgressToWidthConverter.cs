using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;


namespace GazeStream.Converters
{
    public class ProgressToWidthConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double progress && parameter is FrameworkElement element)
            {
                return progress * element.ActualWidth;
            }

            return 0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => System.Windows.Data.Binding.DoNothing;
    }

}
