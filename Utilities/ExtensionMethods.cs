using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace GazeStream.Utilities
{
    public static class ExtensionMethods
    {
        /// <summary>
        /// Uses ActualWidth / ActualHeight (element already arranged)
        /// </summary>
        public static void SetElementToViewportPositionActualSize(
            this Canvas canvas,
            UIElement element,
            Vector2 position)
        {
            if (canvas.ActualWidth == 0 || canvas.ActualHeight == 0)
                return;

            double pixelX = position.X * canvas.ActualWidth;
            double pixelY = position.Y * canvas.ActualHeight;

            if (element is FrameworkElement fe)
            {
                Canvas.SetLeft(fe, pixelX - fe.Width / 2);
                Canvas.SetTop(fe, pixelY - fe.Height / 2);
            }
        }

        /// <summary>
        /// Uses DesiredSize (element measured but not arranged)
        /// </summary>
        public static void SetElementToViewportPosition(
            this Canvas canvas,
            UIElement element,
            Vector2 position)
        {
            if (canvas.ActualWidth == 0 || canvas.ActualHeight == 0)
                return;

            // Ensure DesiredSize is valid
            element.Measure(new System.Windows.Size(double.PositiveInfinity, double.PositiveInfinity));

            double pixelX = position.X * canvas.ActualWidth;
            double pixelY = position.Y * canvas.ActualHeight;

            Canvas.SetLeft(element, pixelX - element.DesiredSize.Width / 2);
            Canvas.SetTop(element, pixelY - element.DesiredSize.Height / 2);
        }

    }
}
