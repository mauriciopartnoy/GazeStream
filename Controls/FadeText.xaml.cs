using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using M = System.Windows.Media;
using System.Windows.Media.Animation;

namespace GazeStream.Controls
{
    public partial class FadeText : System.Windows.Controls.UserControl
    {
        public FadeText()
        {
            InitializeComponent();
        }
        public M.Brush TextColor
        {
            get => (M.Brush)GetValue(TextColorProperty);
            set => SetValue(TextColorProperty, value);
        }

        public static readonly DependencyProperty TextColorProperty =
            DependencyProperty.Register(
                nameof(TextColor),
                typeof(Brush),
                typeof(FadeText),
                new PropertyMetadata(new M.SolidColorBrush(M.Colors.White)));

        public void HideMessage()
        {
            MessageText.Text = string.Empty;
        }

        public async Task ShowMessage(
            string message,
            double fadeTime = 0.5,
            double durationSeconds = 2.0,
            M.Color? overrideColor = null)
        {
            MessageText.Text = message;

            if (overrideColor.HasValue)
            {
                MessageText.Foreground =
                    new M.SolidColorBrush(overrideColor.Value);
            }
            else
            {
                MessageText.ClearValue(TextBlock.ForegroundProperty);
            }

            await FadeTo(1.0, fadeTime);
            await Task.Delay(TimeSpan.FromSeconds(durationSeconds));
            await FadeTo(0.0, fadeTime);
        }

        private Task FadeTo(double targetOpacity, double durationSeconds)
        {
            var tcs = new TaskCompletionSource();

            var animation = new DoubleAnimation
            {
                To = targetOpacity,
                Duration = TimeSpan.FromSeconds(durationSeconds),
                EasingFunction = new QuadraticEase()
            };

            var storyboard = new Storyboard();
            storyboard.Children.Add(animation);

            Storyboard.SetTarget(animation, RootGrid);
            Storyboard.SetTargetProperty(animation, new PropertyPath("Opacity"));

            storyboard.Completed += (_, __) => tcs.SetResult();
            storyboard.Begin();

            return tcs.Task;
        }
    }
}
