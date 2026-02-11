
using System.Windows;
using System.Windows.Media;
using M = System.Windows.Media;
using GazeStream.ViewModels;

namespace GazeStream.Controls
{
    /// <summary>
    /// Interaction logic for GazeBubble.xaml
    /// </summary>
    public partial class GazeCursor : System.Windows.Controls.UserControl
    {
        public GazeCursor()
        {
            InitializeComponent();    
            DataContext = ViewModelsLocator.CursorSettingsViewModel;
        }

        public static readonly DependencyProperty ColorProperty =
            DependencyProperty.Register(
                nameof(Color),
                typeof(M.Brush),
                typeof(GazeCursor),
                new PropertyMetadata(M.Brushes.Red));

        public M.Brush Color
        {
            get => (M.Brush)GetValue(ColorProperty);
            set => SetValue(ColorProperty, value);
        }

        public static readonly DependencyProperty SizeProperty =
            DependencyProperty.Register(
                nameof(Size),
                typeof(double),
                typeof(GazeCursor),
                new PropertyMetadata(40.0));

        public double Size
        {
            get => (double)GetValue(SizeProperty);
            set => SetValue(SizeProperty, value);
        }

        public static readonly DependencyProperty CursorTypeProperty =
    DependencyProperty.Register(
        nameof(CursorType),
        typeof(CursorVisualType),
        typeof(GazeCursor),
        new PropertyMetadata(CursorVisualType.Bubble));

        public CursorVisualType CursorType
        {
            get => (CursorVisualType)GetValue(CursorTypeProperty);
            set => SetValue(CursorTypeProperty, value);
        }
    }
}
public enum CursorVisualType
{
    Bubble,
    CustomImage
}
