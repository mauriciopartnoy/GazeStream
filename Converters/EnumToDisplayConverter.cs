using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;
using System.Reflection;
using System.ComponentModel;
using GazeStream.AppData;
using System.Windows.Media;

namespace GazeStream.Converters
{
    class EnumToDisplayConverter : System.Windows.Data.IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return null;

            var type = value.GetType();
            var member = type.GetMember(value.ToString()).FirstOrDefault();

            if (member == null)
                return null;

            var descriptionAttr = member.GetCustomAttribute<DescriptionAttribute>();
            var iconAttr = member.GetCustomAttribute<IconAttribute>();
            var brushAttr = member.GetCustomAttribute<ColorBrushAttribute>();

            System.Windows.Media.Brush brush = null;
            System.Windows.Media.Brush backgroundBrush = null;

            if (brushAttr != null)
            {
                brush = App.Current.TryFindResource(brushAttr.BrushKey) as System.Windows.Media.Brush;

                if (brush is SolidColorBrush scb)
                {
                    var darker = System.Windows.Media.Color.Multiply(scb.Color, 0.6f);
                    backgroundBrush = new SolidColorBrush(darker);
                }
            }

            return new EnumDisplayData
            {
                Text = descriptionAttr?.Description ?? value.ToString(),
                Icon = iconAttr?.Icon,
                Brush = brush,
                BackgroundBrush = backgroundBrush
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    //class EnumToDisplayConverter : System.Windows.Data.IValueConverter
    //{
    //    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    //    {
    //        if (value == null)
    //            return null;

    //        var type = value.GetType();
    //        var member = type.GetMember(value.ToString()).FirstOrDefault();

    //        if (member == null)
    //            return null;

    //        var descriptionAttr = member.GetCustomAttribute<DescriptionAttribute>();
    //        var iconAttr = member.GetCustomAttribute<IconAttribute>(); // custom attribute

    //        return new EnumDisplayData
    //        {
    //            Text = descriptionAttr?.Description ?? value.ToString(),
    //            Icon = iconAttr?.Icon

    //        };
    //    }

    //    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    //    {
    //        throw new NotImplementedException();
    //    }
    //}

    public class EnumDisplayData
    {
        public string Text { get; set; }
        public ImageSource Icon { get; set; }

        public System.Windows.Media.Brush Brush { get; set; }
        public System.Windows.Media.Brush BackgroundBrush { get; set; }

        public bool HasText => !string.IsNullOrEmpty(Text);
        public bool HasIcon => Icon != null;
        public bool HasBrush => Brush != null;
    }
}

