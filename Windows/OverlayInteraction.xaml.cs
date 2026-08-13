using System.Windows;
using System.Windows.Threading;
using GazeStream.Eyetracker;
using System.Diagnostics;

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

    }
}
