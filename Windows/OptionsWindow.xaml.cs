using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using GazeStream.Eyetracker;
using GazeStream.AppData;
namespace GazeStream.Windows
{
    /// <summary>
    /// Interaction logic for OptionsWindow.xaml
    /// </summary>
    public partial class OptionsWindow : Window
    {
        public OptionsWindow()
        {
            InitializeComponent();
            DataContext = Settings.I;
            Loaded += OnLoaded;
            Closed += OnClosed;
        }

        void OnLoaded(object sender, RoutedEventArgs e)
        {
            GazeManager.OnGazeDeviceChanged -= UpdateDeviceInfo;
            GazeManager.OnGazeDeviceChanged += UpdateDeviceInfo;
            UpdateDeviceInfo(GazeManager.I.GazeDevice);
        }

        void OnClosed(object sender, EventArgs e)
        {
            GazeManager.OnGazeDeviceChanged -= UpdateDeviceInfo;
        }

        private void OnTabChanged(object sender, SelectionChangedEventArgs e)
        {
            //GazeManager.I.Clear();
            //if (tabControl.SelectedContent is FrameworkElement fe)
            //    GazeRegistration.RegisterAllInteractables(fe, GazeManager.I);
        }

        void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        void UpdateDeviceInfo(IGazeDevice device)
        {
            App.Current.Dispatcher.BeginInvoke(new Action(()=>
            {
                if (device == null)
                {
                    GazeDevice.Text = "Dispositivo: Sin Conexión";
                    return;
                }
                GazeDevice.Text = device.DeviceName;
            }));
        }
    }
}
