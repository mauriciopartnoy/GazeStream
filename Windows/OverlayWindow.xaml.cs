using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Windows.Threading;
using GazeStream.Eyetracker;
using GazeStream.AppData;
using System.Diagnostics;
using GazeStream.Utilities;
using GazeStream.Utilities.Events;

namespace GazeStream.Windows
{
    public partial class OverlayWindow : Window
    {
        bool isCalibrating;
        public OverlayWindow()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            // Fullscreen overlay
            Left = 0;
            Top = 0;
            Width = SystemParameters.PrimaryScreenWidth;
            Height = SystemParameters.PrimaryScreenHeight;
            Settings.I.BubbleToggle.OnValueChanged += OnBubbleToggled;
            OnBubbleToggled(Settings.I.BubbleToggle.Value);
            GlobalEvents.OnCalibrationStart.Add(OnCalibrationStart);
            GlobalEvents.OnCalibrationFinished.Add(OnCalibrationFinished);
        }

        void OnCalibrationStart()
        {
            isCalibrating = true;
            HideBubble();
        }

        void OnCalibrationFinished()
        {
            isCalibrating = false;
            OnBubbleToggled(Settings.I.BubbleToggle.Value);
        }

        private void UpdateBubblePosition()
        {
            var point = Helper.ViewportToUIElementPoint(RootCanvas, GazeManager.I.SmoothViewportPoint);
            Canvas.SetLeft(Cursor, point.X - Cursor.Width / 2);
            Canvas.SetTop(Cursor, point.Y - Cursor.Height / 2);
        }

        void OnBubbleToggled(bool enabled)
        {
            if (enabled)
            {
                ShowBubble();
            }
            else
            {
                HideBubble();
            }
        }

        void ShowBubble()
        {
            Cursor.Opacity = Settings.I.BubbleOpacity.Value;
            GazeManager.OnGazeUpdateUI -= UpdateBubblePosition;
            GazeManager.OnGazeUpdateUI += UpdateBubblePosition;
        }

        void HideBubble()
        {
            Cursor.Opacity = 0;
            GazeManager.OnGazeUpdateUI -= UpdateBubblePosition;
        }
    }
}
