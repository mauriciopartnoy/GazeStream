using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Numerics;
using System.Diagnostics;
using GazeStream.Eyetracker;
using GazeStream.Utilities;
using GazeStream.AppData;
using GazeStream.Controls;
using GazeStream.Utilities.Events;
using System.Runtime.InteropServices;

namespace GazeStream.Windows
{
    public partial class SkyleCalibrationWindow : Window
    {
        public static SkyleCalibrationWindow I { get; private set; } //Singleton Instance

        CancellationTokenSource cts;
        bool isCalibrating;

        public SkyleCalibrationWindow()
        {
            InitializeComponent();
            I = this;
            Loaded += OnLoaded;
            Closed += OnClosed;
            PreviewKeyDown += CancelCalibrationOnKeyPress;
            ForceTopmost();
            DataContext = Settings.I;
            //GlobalEvents.OnStartCalibrationCommand.Add(StartCalibrationAndCloseOnFinished);
        }

        private void ForceTopmost()
        {
            if (!this.IsVisible)
            {
                this.Show();
            }

            this.WindowState = WindowState.Maximized;
            this.Activate();
            this.Topmost = true;  // temporarily
            this.Topmost = false; // reset
            this.Focus();
        }

        void OnLoaded(object sender, RoutedEventArgs e)
        {
            //ResetPage();
            //LoadToggleOptions();
        }

        void OnClosed(object sender, EventArgs e)
        {
            //IMPORTANTE: Hacer genérico si hay multiples eyetrackers que pueden recibir el comando.
            //GlobalEvents.OnStartCalibrationCommand.Remove(StartCalibrationAndCloseOnFinished); 
            CancelCalibration();
        }

        void CancelCalibrationOnKeyPress(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                CancelCalibration();
                e.Handled = true;
            }
        }

        public void Back_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
            Debug.WriteLine("Back pressed. Closing window.");
        }

        public void StartCalibration_Click(object sender, RoutedEventArgs e)
        {
            if (isCalibrating) return;
            //StartCalibrationTaskUsingPanelSettings();
        }

        void CancelCalibration_Click(object e, RoutedEventArgs args)
        {
            CancelCalibration();
        }

        void StartCalibration()
        { 
        
        }

        void CancelCalibration()
        { 
        
        }

    }
}
