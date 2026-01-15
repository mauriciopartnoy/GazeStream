using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Threading;
using GazeStream.Utilities;

namespace GazeStream.Eyetracker
{
    internal class CalibrationPointGraphic
    {
        public FrameworkElement Element { get; private set; }
        public float Progress01 { get; private set; }

        private readonly RotateTransform rotateTransform;
        private readonly ScaleTransform scaleTransform;

        public CalibrationPointGraphic(double size, System.Windows.Media.Color color)
        {
            // --- Visual ---
            var ellipse = new Ellipse
            {
                Width = size,
                Height = size * 0.7,
                Fill = new SolidColorBrush(color),
                RenderTransformOrigin = new System.Windows.Point(0.5, 0.5)
            };

            // --- Transforms ---
            scaleTransform = new ScaleTransform(0, 0);
            rotateTransform = new RotateTransform(0);

            var transformGroup = new TransformGroup();
            transformGroup.Children.Add(scaleTransform);
            transformGroup.Children.Add(rotateTransform);

            ellipse.RenderTransform = transformGroup;
            Element = ellipse;

            var spawnStoryboard = CreateSpawnStoryboard();
            var rotationStoryboard = CreateRotationStoryboard();


            spawnStoryboard.Completed += (_, _) => rotationStoryboard.Begin(Element, true);
            Element.Loaded += (_, _) => spawnStoryboard.Begin(Element, true);
            Element.Unloaded += (_, _) => spawnStoryboard.Stop(Element);
            Element.Unloaded += (_, _) => rotationStoryboard.Stop(Element);

        }

        Storyboard CreateRotationStoryboard()
        {
            Storyboard rotationStoryboard = new Storyboard();

            var rotationAnimation = new DoubleAnimation
            {
                From = 0,
                To = 360,
                Duration = TimeSpan.FromSeconds(2),
                RepeatBehavior = RepeatBehavior.Forever
            };


            Storyboard.SetTarget(rotationAnimation, Element);
            Storyboard.SetTargetProperty(
                rotationAnimation,
                new PropertyPath(
                    "(UIElement.RenderTransform).(TransformGroup.Children)[1].(RotateTransform.Angle)"
                ));

            rotationStoryboard.Children.Add(rotationAnimation);
            return rotationStoryboard;
        }

        // =====================
        // Animations
        // =====================

        Storyboard CreateSpawnStoryboard()
        {
            Storyboard spawnStoryboard = new Storyboard();

            var scaleX = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(300))
            {
                EasingFunction = new BackEase { Amplitude = 0.3 }
            };

            var scaleY = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(300))
            {
                EasingFunction = new BackEase { Amplitude = 0.3 }
            };

            Storyboard.SetTarget(scaleX, Element);
            Storyboard.SetTarget(scaleY, Element);
            Storyboard.SetTargetProperty(scaleX, new PropertyPath("(UIElement.RenderTransform).(TransformGroup.Children)[0].(ScaleTransform.ScaleX)"));
            Storyboard.SetTargetProperty(scaleY, new PropertyPath("(UIElement.RenderTransform).(TransformGroup.Children)[0].(ScaleTransform.ScaleY)"));
            spawnStoryboard.Children.Add(scaleX);
            spawnStoryboard.Children.Add(scaleY);

            return spawnStoryboard;
        }

        public void UpdateCalibrationPoint(float progress01)
        {
            App.Instance.Dispatcher.Invoke(() =>
            {
                Progress01 = Math.Clamp(progress01, 0f, 1f);
                double scale = Helper.Lerp(1.0f, 0.2f, Progress01);
                scaleTransform.ScaleX = scale;
                scaleTransform.ScaleY = scale;
            });
        }

        public void Destroy(Canvas canvas)
        {
            canvas.Children.Remove(Element);

            // Optional sound hook
            // SoundManager.Play("Pop.wav");
        }
    }
}
