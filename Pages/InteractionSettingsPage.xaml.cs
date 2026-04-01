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
using System.Windows.Navigation;
using System.Windows.Shapes;
using GazeStream.ViewModels;
using GazeStream.AppData;
using System.Diagnostics;
using GazeStream.Eyetracker;

namespace GazeStream.Pages
{ 
    public partial class InteractionSettingsPage : System.Windows.Controls.UserControl
    {
        public InteractionSettingsPage()
        {
            InitializeComponent();
            DataContext = Settings.I;
        }

        private void GazeButton_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.MessageBox.Show("Activated by gaze!");
        }

        private void RestartApp_Click(object sender, RoutedEventArgs e)
        {
            App.RestartApp();
        }

        private void Stop_Click(object sender, RoutedEventArgs e)
        {
            GazeManager.I.StopGazeDeviceUpdateLoop();
        }

        private void Start_Click(object sender, RoutedEventArgs e)
        {
            GazeManager.I.StartGazeDeviceUpdateLoop();
        }
    }
}
