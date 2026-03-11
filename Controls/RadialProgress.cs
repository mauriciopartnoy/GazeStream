using System;
using System.Windows;
using C = System.Windows.Controls;
using B = System.Windows.Media;

namespace GazeStream.Controls
{
    public class RadialProgress : C.Control
    {
        static RadialProgress()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(RadialProgress),
                new FrameworkPropertyMetadata(typeof(RadialProgress)));
        }

        public B.Brush Fill
        {
            get => (B.Brush)GetValue(FillProperty);
            set => SetValue(FillProperty, value);
        }

        public static readonly DependencyProperty FillProperty =
            DependencyProperty.Register(
                nameof(Fill),
                typeof(B.Brush),
                typeof(RadialProgress),
                new PropertyMetadata(B.Brushes.DodgerBlue));

        public double Progress01
        {
            get => (double)GetValue(Progress01Property);
            set => SetValue(Progress01Property, value);
        }

        public static readonly DependencyProperty Progress01Property =
            DependencyProperty.Register(
                nameof(Progress01),
                typeof(double),
                typeof(RadialProgress),
                new PropertyMetadata(0.0, OnProgressChanged));

        public double SweepAngle
        {
            get => (double)GetValue(SweepAngleProperty);
            private set => SetValue(SweepAnglePropertyKey, value);
        }

        private static readonly DependencyPropertyKey SweepAnglePropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(SweepAngle),
                typeof(double),
                typeof(RadialProgress),
                new PropertyMetadata(0.0));

        public static readonly DependencyProperty SweepAngleProperty =
            SweepAnglePropertyKey.DependencyProperty;

        private static void OnProgressChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var ctrl = (RadialProgress)d;
            double progress = Math.Clamp((double)e.NewValue, 0, 1);
            ctrl.SweepAngle = progress * 360;
        }

        public double StartAngle
        {
            get => (double)GetValue(StartAngleProperty);
            set => SetValue(StartAngleProperty, value);
        }

        public static readonly DependencyProperty StartAngleProperty =
            DependencyProperty.Register(
                nameof(StartAngle),
                typeof(double),
                typeof(RadialProgress),
                new PropertyMetadata(-90.0)); // default = top

        public bool IsClockwise
        {
            get => (bool)GetValue(IsClockwiseProperty);
            set => SetValue(IsClockwiseProperty, value);
        }

        public static readonly DependencyProperty IsClockwiseProperty =
            DependencyProperty.Register(
                nameof(IsClockwise),
                typeof(bool),
                typeof(RadialProgress),
                new PropertyMetadata(true));
    }

}
