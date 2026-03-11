using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace GazeStream.Converters
{
    public class AngleToArcConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            double sweep = (double)values[0];
            double width = (double)values[1];
            double height = (double)values[2];
            double startAngle = (double)values[3];
            bool clockwise = (bool)values[4];

            if (sweep <= 0)
                return Geometry.Empty;

            if (sweep >= 360)
            {
                double r = Math.Min(width, height) / 2;
                return new EllipseGeometry(
                    new System.Windows.Point(width / 2, height / 2),
                    r,
                    r);
            }

            double radius = Math.Min(width, height) / 2;
            System.Windows.Point center = new System.Windows.Point(width / 2, height / 2);

            double startRad = startAngle * Math.PI / 180;
            double endRad = (startAngle + (clockwise ? sweep : -sweep)) * Math.PI / 180;

            System.Windows.Point startPoint = new System.Windows.Point(
                center.X + radius * Math.Cos(startRad),
                center.Y + radius * Math.Sin(startRad));

            System.Windows.Point endPoint = new System.Windows.Point(
                center.X + radius * Math.Cos(endRad),
                center.Y + radius * Math.Sin(endRad));

            bool isLargeArc = sweep > 180;

            var figure = new PathFigure { StartPoint = center };
            figure.Segments.Add(new LineSegment(startPoint, true));
            figure.Segments.Add(new ArcSegment(
                endPoint,
                new System.Windows.Size(radius, radius),
                0,
                isLargeArc,
                clockwise ? SweepDirection.Clockwise : SweepDirection.Counterclockwise,
                true));
            figure.Segments.Add(new LineSegment(center, true));

            var geometry = new PathGeometry();
            geometry.Figures.Add(figure);

            return geometry;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

}
