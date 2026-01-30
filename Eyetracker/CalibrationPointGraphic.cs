using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Threading;
using GazeStream.Utilities;
using GazeStream.Resources;

namespace GazeStream.Eyetracker
{
    internal class CalibrationPointGraphic
    {
        public FrameworkElement Element { get; private set; }
        public float Progress01 { get; private set; }

        private readonly ScaleTransform spawnScale;
        private readonly ScaleTransform progressScale;
        private readonly RotateTransform rotateTransform;

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
            spawnScale = new ScaleTransform(0, 0);       // entry / exit
            progressScale = new ScaleTransform(1, 1);    // calibration progress
            rotateTransform = new RotateTransform(0);    // constant rotation

            var transformGroup = new TransformGroup();
            transformGroup.Children.Add(spawnScale);     // [0]
            transformGroup.Children.Add(progressScale);  // [1]
            transformGroup.Children.Add(rotateTransform);// [2]

            ellipse.RenderTransform = transformGroup;
            Element = ellipse;

            var spawnStoryboard = CreateSpawnStoryboard();
            var rotationStoryboard = CreateRotationStoryboard();

            Element.Loaded += (_, _) =>
            {
                rotationStoryboard.Begin(Element, true);
                spawnStoryboard.Begin(Element, true);
            };

            Element.Unloaded += (_, _) =>
            {
                spawnStoryboard.Stop(Element);
                rotationStoryboard.Stop(Element);
            };
        }

        Storyboard CreateRotationStoryboard()
        {
            var storyboard = new Storyboard();

            var animation = new DoubleAnimation
            {
                From = 0,
                To = 360,
                Duration = TimeSpan.FromSeconds(1),
                RepeatBehavior = RepeatBehavior.Forever
            };

            Storyboard.SetTarget(animation, Element);
            Storyboard.SetTargetProperty(
                animation,
                new PropertyPath(
                    "(UIElement.RenderTransform).(TransformGroup.Children)[2].(RotateTransform.Angle)"
                ));

            storyboard.Children.Add(animation);
            return storyboard;
        }


        // =====================
        // Animations
        // =====================

        Storyboard CreateSpawnStoryboard()
        {
            var storyboard = new Storyboard();

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

            Storyboard.SetTargetProperty(
                scaleX,
                new PropertyPath("(UIElement.RenderTransform).(TransformGroup.Children)[0].(ScaleTransform.ScaleX)")
            );

            Storyboard.SetTargetProperty(
                scaleY,
                new PropertyPath("(UIElement.RenderTransform).(TransformGroup.Children)[0].(ScaleTransform.ScaleY)")
            );

            storyboard.Children.Add(scaleX);
            storyboard.Children.Add(scaleY);

            return storyboard;
        }


        public void UpdateCalibrationPoint(float progress01)
        {
            App.Instance.Dispatcher.Invoke(() =>
            {
                Progress01 = Math.Clamp(progress01, 0f, 1f);

                double scale = Helper.Lerp(1.0f, 0.2f, Progress01);

                progressScale.ScaleX = scale;
                progressScale.ScaleY = scale;
            });
        }


        public void Destroy(Canvas canvas)
        {
            SoundManager.PlayUISound(SoundResources.PopWav);
            canvas.Children.Remove(Element);
        }
    }
}
