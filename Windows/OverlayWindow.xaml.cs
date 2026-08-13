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

        const int GWL_EXSTYLE = -20;
        const int WS_EX_TRANSPARENT = 0x20;
        const int WS_EX_LAYERED = 0x80000;

        [DllImport("user32.dll")]
        static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            // Fullscreen overlay
            //Left = 0;
            //Top = 0;
            //Width = SystemParameters.PrimaryScreenWidth;
            //Height = SystemParameters.PrimaryScreenHeight;
            Settings.I.BubbleToggle.OnValueChanged += OnBubbleToggled;
            OnBubbleToggled(Settings.I.BubbleToggle.Value);
            GlobalEvents.OnCalibrationStart.Add(OnCalibrationStart);
            GlobalEvents.OnCalibrationFinished.Add(OnCalibrationFinished);
            OnGazeDeviceChanged(GazeManager.I.GazeDevice);
            GazeManager.OnGazeDeviceChanged += OnGazeDeviceChanged;
        }

        //ALTERNATIVO

        //protected override void OnSourceInitialized(EventArgs e)
        //{
        //    base.OnSourceInitialized(e);

        //    var hwnd = new WindowInteropHelper(this).Handle;

        //    int exStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
        //    SetWindowLong(hwnd, GWL_EXSTYLE, exStyle | WS_EX_LAYERED | WS_EX_TRANSPARENT);
        //}

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            var hwnd = new WindowInteropHelper(this).Handle;
            int exStyle = WindowsHelper.GetWindowStyle(hwnd);
            WindowsHelper.EnableClickThrough(hwnd, exStyle);
            SetWindowLong(hwnd, GWL_EXSTYLE, exStyle | WS_EX_LAYERED | WS_EX_TRANSPARENT);
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
            Canvas.SetLeft(Cursor, point.X - Cursor.ActualWidth / 2);
            Canvas.SetTop(Cursor, point.Y - Cursor.ActualHeight / 2);
        }

        private void Intelligaze_Click(object sender, RoutedEventArgs e)
        {
            GazeManager.I.SwitchIntelligazeGUI();
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

        void OnGazeDeviceChanged(IGazeDevice gazeDevice)
        {
            //Debug.WriteLine("Gaze device changed.");
            //bool isIntelligaze;
            //if (gazeDevice == null)
            //{
            //    isIntelligaze = false;
            //}
            //else
            //{           
            //    isIntelligaze = gazeDevice.DeviceName == GazeManager.I.intelligaze.DeviceName;
            //    Debug.WriteLine("Gaze device changed." + gazeDevice.DeviceName + isIntelligaze);

            //}
            //Dispatcher.Invoke(()=>
            //{
            //    Intelligaze_Button.Visibility = isIntelligaze ? Visibility.Visible : Visibility.Collapsed;
            //    Intelligaze_Button.IsHitTestVisible = isIntelligaze ? true : false;
            //});
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
