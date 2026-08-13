using System.Windows;
using System.Windows.Threading;
using GazeStream.Eyetracker;
using System.Diagnostics;
using System.Windows.Interop;
using System.Runtime.InteropServices;


namespace GazeStream.Windows
{
   
    public partial class OverlayInteraction : Window
    {
        public OverlayInteraction()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            OnGazeDeviceChanged(GazeManager.I.GazeDevice);
            GazeManager.OnGazeDeviceChanged += OnGazeDeviceChanged;
        }
        private void Intelligaze_Click(object sender, RoutedEventArgs e)
        {
            GazeManager.I.SwitchIntelligazeGUI();
        }

        void OnGazeDeviceChanged(IGazeDevice gazeDevice)
        {
            Debug.WriteLine("Gaze device changed.");
            bool isIntelligaze;
            if (gazeDevice == null)
            {
                isIntelligaze = false;
            }
            else
            {
                isIntelligaze = gazeDevice.DeviceName == GazeManager.I.intelligaze.DeviceName;
                Debug.WriteLine("Gaze device changed." + gazeDevice.DeviceName + isIntelligaze);

            }
            Dispatcher.Invoke(() =>
            {
                //TEST ONLY

                Intelligaze_Button.Visibility = isIntelligaze ? Visibility.Visible : Visibility.Collapsed;
                Intelligaze_Button.IsHitTestVisible = isIntelligaze ? true : false;
            });
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            var hwnd = new WindowInteropHelper(this).Handle;

            int exStyle = GetWindowLong(hwnd, GWL_EXSTYLE);

            SetWindowLong(
                hwnd,
                GWL_EXSTYLE,
                exStyle | WS_EX_NOACTIVATE
            );
        }

        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_NOACTIVATE = 0x08000000;

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(
            IntPtr hWnd,
            int nIndex,
            int dwNewLong
        );

    }
}
