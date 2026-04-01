using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace GazeStream.Controls
{
    public class IconToggleButton : ToggleButton
    {
        static IconToggleButton()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(IconToggleButton),
                new FrameworkPropertyMetadata(typeof(IconToggleButton)));
        }

        public double IconSize
        {
            get => (double)GetValue(IconSizeProperty);
            set => SetValue(IconSizeProperty, value);
        }

        public static readonly DependencyProperty IconSizeProperty =
            DependencyProperty.Register(
                nameof(IconSize),
                typeof(double),
                typeof(IconToggleButton),
                new PropertyMetadata(32.0));

        public ImageSource IconOn
        {
            get => (ImageSource)GetValue(IconOnProperty);
            set => SetValue(IconOnProperty, value);
        }

        public static readonly DependencyProperty IconOnProperty =
            DependencyProperty.Register(
                nameof(IconOn),
                typeof(ImageSource),
                typeof(IconToggleButton));

        public ImageSource IconOff
        {
            get => (ImageSource)GetValue(IconOffProperty);
            set => SetValue(IconOffProperty, value);
        }

        public static readonly DependencyProperty IconOffProperty =
            DependencyProperty.Register(
                nameof(IconOff),
                typeof(ImageSource),
                typeof(IconToggleButton));
    }
}
