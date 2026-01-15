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

namespace GazeStream.Windows
{
    public partial class OverlayWindow : Window
    {
        const float BUBBLE_OPACITY = .8f;
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
        }

        private void UpdateBubblePosition()
        {
            Debug.WriteLine("Update bubble position called");
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
            Cursor.Opacity = BUBBLE_OPACITY;
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
