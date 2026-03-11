using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using System.Diagnostics;
using System.Windows.Media;
using B = System.Windows.Controls.Primitives;
using System.Windows.Controls.Primitives;
using System.Reflection;
using System.Windows.Controls;


namespace GazeStream.Eyetracker
{
    public class GazeButton : GazeControl
    {
     
        public CornerRadius CornerRadius
        {
            get => (CornerRadius)GetValue(CornerRadiusProperty);
            set => SetValue(CornerRadiusProperty, value);
        }

        public static readonly DependencyProperty CornerRadiusProperty =
            DependencyProperty.Register(
                nameof(CornerRadius),
                typeof(CornerRadius),
                typeof(GazeButton),
                new FrameworkPropertyMetadata(
                    new CornerRadius(0),
                    FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly RoutedEvent ClickEvent =
           EventManager.RegisterRoutedEvent(
               nameof(Click),
               RoutingStrategy.Bubble,
               typeof(RoutedEventHandler),
               typeof(GazeButton));

        public event RoutedEventHandler Click
        {
            add => AddHandler(ClickEvent, value);
            remove => RemoveHandler(ClickEvent, value);
        }

        ContentPresenter _contentPresenter;

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            _contentPresenter = GetTemplateChild("PART_Content") as ContentPresenter;
        }
        protected override void OnFocus()
        {
            Debug.WriteLine("OnFocus called!!!!!!!");
            System.Windows.Point center = new System.Windows.Point(ActualWidth / 2, ActualHeight / 2);
            var result = VisualTreeHelper.HitTest(_contentPresenter, center);

            if (result?.VisualHit is DependencyObject hit)
            {
                var button = FindButtonBaseUpwards(hit);

                ActivateButtonBase(button);
                //if (button is ToggleButton toggle)
                //{
                //    toggle.IsChecked = !toggle.IsChecked;
                //}
                //else
                //{
                //    button.RaiseEvent(new RoutedEventArgs(B.ButtonBase.ClickEvent, button));
                //}
            }

            Debug.WriteLine(result?.VisualHit?.GetType().FullName);
        }

        public static void ActivateButtonBase(B.ButtonBase button)
        {
            if (button == null) return;
            if (!button.IsEnabled) return;

            var method = typeof(B.ButtonBase)
                .GetMethod("OnClick",
                    BindingFlags.Instance | BindingFlags.NonPublic);

            method?.Invoke(button, null);
        }

        private B.ButtonBase FindButtonBaseUpwards(DependencyObject start)
        {
            while (start != null)
            {
                if (start is B.ButtonBase button)
                    return button;

                start = VisualTreeHelper.GetParent(start);
            }

            return null;
        }

        protected override void OnGazeEnter()
        {
            base.OnGazeEnter();
            Debug.WriteLine("OnGazeEntered!!!");
        }

        protected override void OnGazeUpdate(double deltaTime)
        {
            base.OnGazeUpdate(deltaTime);

        }

        protected override void OnGazeExit()
        {
            base.OnGazeExit();
            Debug.WriteLine("OnGazeExit!!!");

        }
    }
}
