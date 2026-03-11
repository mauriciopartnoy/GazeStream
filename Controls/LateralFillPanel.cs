using System;
using System.Windows;
using C = System.Windows.Controls;
using B = System.Windows.Media;
using System.Windows.Media;

namespace GazeStream.Controls
{
    public enum FillDirection
    {
        LeftToRight,
        RightToLeft,
        TopToBottom,
        BottomToTop
    }

    public class LateralFillPanel : C.Control
    {
        static LateralFillPanel()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(LateralFillPanel),
                new FrameworkPropertyMetadata(typeof(LateralFillPanel)));
        }

        public CornerRadius CornerRadius
        {
            get => (CornerRadius)GetValue(CornerRadiusProperty);
            set => SetValue(CornerRadiusProperty, value);
        }

        public static readonly DependencyProperty CornerRadiusProperty =
            DependencyProperty.Register(
                nameof(CornerRadius),
                typeof(CornerRadius),
                typeof(LateralFillPanel),
                new FrameworkPropertyMetadata(
                    new CornerRadius(0),
                    FrameworkPropertyMetadataOptions.AffectsRender));
        public double Progress01
        {
            get => (double)GetValue(Progress01Property);
            set => SetValue(Progress01Property, value);
        }

        public static readonly DependencyProperty Progress01Property =
            DependencyProperty.Register(
                nameof(Progress01),
                typeof(double),
                typeof(LateralFillPanel),
                new PropertyMetadata(0d, OnVisualPropertyChanged));

        public FillDirection FillDirection
        {
            get => (FillDirection)GetValue(FillDirectionProperty);
            set => SetValue(FillDirectionProperty, value);
        }

        public static readonly DependencyProperty FillDirectionProperty =
            DependencyProperty.Register(
                nameof(FillDirection),
                typeof(FillDirection),
                typeof(LateralFillPanel),
                new PropertyMetadata(FillDirection.LeftToRight, OnVisualPropertyChanged));

        public B.Brush FillBrush
        {
            get => (B.Brush)GetValue(FillBrushProperty);
            set => SetValue(FillBrushProperty, value);
        }

        public static readonly DependencyProperty FillBrushProperty =
            DependencyProperty.Register(
                nameof(FillBrush),
                typeof(B.Brush),
                typeof(LateralFillPanel),
                new PropertyMetadata(B.Brushes.DodgerBlue));

        private static void OnVisualPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((LateralFillPanel)d).UpdateVisual();
        }

        private ScaleTransform _scale;
        private C.Border _fill;

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            _fill = GetTemplateChild("PART_Fill") as C.Border;
            _scale = GetTemplateChild("PART_Scale") as ScaleTransform;

            UpdateVisual();
        }

        private void UpdateVisual()
        {
            if (_scale == null || _fill == null)
                return;

            switch (FillDirection)
            {
                case FillDirection.LeftToRight:
                    _fill.RenderTransformOrigin = new System.Windows.Point(0, 0.5);
                    _scale.ScaleX = Progress01;
                    _scale.ScaleY = 1;
                    break;

                case FillDirection.RightToLeft:
                    _fill.RenderTransformOrigin = new System.Windows.Point(1, 0.5);
                    _scale.ScaleX = Progress01;
                    _scale.ScaleY = 1;
                    break;

                case FillDirection.TopToBottom:
                    _fill.RenderTransformOrigin = new System.Windows.Point(0.5, 0);
                    _scale.ScaleX = 1;
                    _scale.ScaleY = Progress01;
                    break;

                case FillDirection.BottomToTop:
                    _fill.RenderTransformOrigin = new System.Windows.Point(0.5, 1);
                    _scale.ScaleX = 1;
                    _scale.ScaleY = Progress01;
                    break;
            }
        }
    }
}
