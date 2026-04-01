
using System.Windows;
using System.Windows.Media;
using M = System.Windows.Media;
using GazeStream.ViewModels;
using GazeStream.Utilities;
using GazeStream.AppData;
using System.ComponentModel;

namespace GazeStream.Controls
{

    public partial class GazeCursor : System.Windows.Controls.UserControl
    {
        public GazeCursor()
        {
            InitializeComponent();
            Loaded += GazeButton_Loaded;
            Unloaded += GazeButton_Unloaded;
            DataContext = Settings.I;
        }

        private void GazeButton_Loaded(object sender, RoutedEventArgs e)
        {
            if (DesignerProperties.GetIsInDesignMode(this)) return;

            ApplySettings();
            Settings.I.BubbleColor.OnChanged += ApplySettings;

        }

        private void GazeButton_Unloaded(object sender, RoutedEventArgs e)
        {
            if (DesignerProperties.GetIsInDesignMode(this)) return;
            Settings.I.BubbleColor.OnChanged -= ApplySettings;
        }

        private void ApplySettings()
        {
            Dispatcher.Invoke(() =>
            {
                Color = Helper.GetBrushFromBasicColorEnum(Settings.I.BubbleColor.Value);
            });

        }

        public static readonly DependencyProperty ColorProperty =
            DependencyProperty.Register(
                nameof(Color),
                typeof(M.Brush),
                typeof(GazeCursor),
                new PropertyMetadata(M.Brushes.Blue));

        public M.Brush Color
        {
            get => (M.Brush)GetValue(ColorProperty);
            set => SetValue(ColorProperty, value);
        }
    }
}

