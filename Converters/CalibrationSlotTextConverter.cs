using System.Globalization;
using System.Windows.Data;

namespace GazeStream.Converters
{
    public class CalibrationSlotTextConverter : IValueConverter
    {
        public object Convert(
            object value,
            Type targetType,
            object parameter,
            CultureInfo culture)
        {
            if (value is byte[] data)
            {
                return (data == null || data.Length == 0) ? "Vacío" : "Cargar";
            }

            return "Vacío";
        }

        public object ConvertBack(
            object value,
            Type targetType,
            object parameter,
            CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
