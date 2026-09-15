using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace GazeStream.Converters
{
    public class SumToMarginConverter : IMultiValueConverter
    {
        public object Convert(
            object[] values,
            Type targetType,
            object parameter,
            CultureInfo culture)
        {
            double value1 = System.Convert.ToDouble(values[0]);
            double value2 = System.Convert.ToDouble(values[1]);

            double sum = value1 + value2;

            return new Thickness(sum);
        }

        public object[] ConvertBack(
            object value,
            Type[] targetTypes,
            object parameter,
            CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
