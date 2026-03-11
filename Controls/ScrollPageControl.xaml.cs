using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using InputSimulatorEx;
using System.ComponentModel;
namespace GazeStream.Controls
{
    /// <summary>
    /// Interaction logic for ScrollPageControl.xaml
    /// </summary>
    public partial class ScrollPageControl : System.Windows.Controls.UserControl
    {
        public ScrollPageControl()
        {
            InitializeComponent();
        }

        CancellationTokenSource cts;

        const int steps = 5;
        const float time = .3f;

        public ScrollViewer TargetScrollViewer
        {
            get => (ScrollViewer)GetValue(TargetScrollViewerProperty);
            set => SetValue(TargetScrollViewerProperty, value);
        }

        public static readonly DependencyProperty TargetScrollViewerProperty =
            DependencyProperty.Register(
                nameof(TargetScrollViewer),
                typeof(ScrollViewer),
                typeof(ScrollPageControl),
                new PropertyMetadata(null));

        public void ScrollUp(object sender, EventArgs args)
        {
            if (TargetScrollViewer == null) return;
            _= ScrollTaskUp();
        }

        public void ScrollDown(object sender, EventArgs args)
        {
            if (TargetScrollViewer == null) return;
            _= ScrollTaskDown();
        }
        async Task ScrollTaskUp()
        {
            cts = new CancellationTokenSource();
            int timeStep = ((int)(time * 1000)) / steps;
            for (int i = 0; i < steps; i++)
            {
                TargetScrollViewer.LineUp();
                await Task.Delay(timeStep, cts.Token);
            }
        }

        async Task ScrollTaskDown()
        {
            if (cts != null) cts.Cancel();
            cts = new CancellationTokenSource();
            int timeStep = ((int)(time * 1000)) / steps;
            for (int i = 0; i < steps; i++)
            {
                TargetScrollViewer.LineDown();
                await Task.Delay(timeStep, cts.Token);
            }
        }
    }
}
