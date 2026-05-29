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
        Task updateTask;

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
            UpdateVersionText();
        }

        void UpdateVersionText()
        {
            string updateMessage;
            if (string.IsNullOrEmpty(App.NewestVersion))
            {
                updateMessage = "GazeStream ya está actualizado.";
            }
            else
            {
                //Check diff...?
                updateMessage = "Instalar versión: " + App.NewestVersion;
            }
            UpdateText.Text = updateMessage;
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

        void Update_App_Click(object sender, RoutedEventArgs e)
        {
            if (!updateTask.IsCompleted) return;
            updateTask = Task.Run(App.UpdateApp);
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

        public void SelectTab(string tabName)
        {
            switch (tabName)
            {
                case "Interaction":
                    MainTabControl.SelectedItem = Interaction;
                    break;
                case "Cursor":
                    MainTabControl.SelectedItem = Cursor;
                    break;
                case "Keyboard":
                    MainTabControl.SelectedItem = Keyboard;
                    break;
                case "Barrido":
                    MainTabControl.SelectedItem = Barrido;
                    break;
                case "Sound":
                    MainTabControl.SelectedItem = Sound;
                    break;
                case "Help":
                    MainTabControl.SelectedItem = Help;
                    break;
            }
        }
    }
}
