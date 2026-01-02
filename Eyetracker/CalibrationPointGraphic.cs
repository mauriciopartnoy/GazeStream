using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace GazeStream.Eyetracker
{
    internal class CalibrationPointGraphic
    {
        public FrameworkElement Element { get; private set; }
        public float Progress01 { get; private set; }

        private readonly DispatcherTimer rotationTimer;
        private readonly RotateTransform rotateTransform;
        private readonly ScaleTransform scaleTransform;

        private float rotationSpeed;

        private const float MinRotationSpeed = 5f;
        private const float MaxRotationSpeed = 10f;

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
            rotateTransform = new RotateTransform(0);
            scaleTransform = new ScaleTransform(0, 0);

            var transformGroup = new TransformGroup();
            transformGroup.Children.Add(scaleTransform);
            transformGroup.Children.Add(rotateTransform);

            ellipse.RenderTransform = transformGroup;
            Element = ellipse;

            // --- Rotation timer ---
            rotationTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(16)
            };
            rotationTimer.Tick += RotateTick;
            rotationTimer.Start();

            StartSpawnAnimation();
        }

        // =====================
        // Animations
        // =====================

        private void StartSpawnAnimation()
        {
            var anim = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromSeconds(0.3),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };

            scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, anim);
            scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, anim);
        }

        private void RotateTick(object? sender, EventArgs e)
        {
            rotateTransform.Angle += rotationSpeed;
            if (rotateTransform.Angle >= 360)
                rotateTransform.Angle -= 360;
        }

        // =====================
        // Public API
        // =====================

        public void UpdateCalibrationPoint(float progress01)
        {
            Progress01 = Math.Clamp(progress01, 0f, 1f);

            rotationSpeed = Lerp(MinRotationSpeed, MaxRotationSpeed, Progress01);

            double scale = Lerp(1.0, 0.2, Progress01);
            scaleTransform.ScaleX = scale;
            scaleTransform.ScaleY = scale;
        }

        public void Destroy(Canvas canvas)
        {
            rotationTimer.Stop();
            canvas.Children.Remove(Element);

            // Optional sound hook
            // SoundManager.Play("Pop.wav");
        }

        // =====================
        // Helpers
        // =====================

        private static float Lerp(float a, float b, float t)
            => a + (b - a) * t;

        private static double Lerp(double a, double b, float t)
            => a + (b - a) * t;
    }
}
