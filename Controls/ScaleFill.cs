using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace GazeStream.Controls
{
    public class ScaleFill : ContentControl
    {
        static ScaleFill()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(ScaleFill),
                new FrameworkPropertyMetadata(typeof(ScaleFill)));
        }

        private ScaleTransform _scaleTransform;

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            _scaleTransform = GetTemplateChild("PART_ScaleTransform") as ScaleTransform;
            UpdateScale();
        }

        #region Progress01

        public static readonly DependencyProperty Progress01Property =
            DependencyProperty.Register(
                nameof(Progress01),
                typeof(double),
                typeof(ScaleFill),
                new PropertyMetadata(0.0, OnProgressChanged));

        public double Progress01
        {
            get => (double)GetValue(Progress01Property);
            set => SetValue(Progress01Property, value);
        }

        #endregion

        #region IsReversed

        public static readonly DependencyProperty IsReversedProperty =
            DependencyProperty.Register(
                nameof(IsReversed),
                typeof(bool),
                typeof(ScaleFill),
                new PropertyMetadata(false, OnProgressChanged));

        public bool IsReversed
        {
            get => (bool)GetValue(IsReversedProperty);
            set => SetValue(IsReversedProperty, value);
        }

        #endregion

        private static void OnProgressChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((ScaleFill)d).UpdateScale();
        }

        private void UpdateScale()
        {
            if (_scaleTransform == null)
                return;

            double p = Math.Max(0, Math.Min(1, Progress01));
            double value = IsReversed ? 1.0 - p : p;

            //AL estar invertido el overlay solo va a ser visible si hubo un mínimo de activación.
            if (IsReversed && p == 0)
            {
                value = 0;
            }

            _scaleTransform.ScaleX = value;
            _scaleTransform.ScaleY = value;
        }
    }
}