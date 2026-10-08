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
using SkyleAvaloniaExample.Interop;

namespace GazeStream.Windows
{
    public partial class SkyleCalibrationWindow : Window
    {
        public static SkyleCalibrationWindow I { get; private set; } //Singleton Instance
        public static int CalibrationPointProgress { get; private set; }
        public static float CalibrationPointProgress01 { get; private set; }

        static bool calibrationPointChanged;
        CancellationTokenSource cts;
        List<CalibrationPointGraphic> points = new List<CalibrationPointGraphic>();
        CalibrationPointGraphic? currentPoint;
        bool isCalibrating;

        SkyleClient Client { get; set; }

        public SkyleCalibrationWindow()
        {
            InitializeComponent();
            I = this;
            Loaded += OnLoaded;
            Closed += OnClosed;
            PreviewKeyDown += CancelCalibrationOnKeyPress;
            ForceTopmost();
            DataContext = Settings.I;
            Client = GazeManager.I.skyle.Client;
            //GlobalEvents.OnStartCalibrationCommand.Add(StartCalibrationAndCloseOnFinished);
        }

        void SubscribeCallbacks()
        {
            if (GazeManager.I.skyle == null) return;

            Client.CalibrationPointReceived += OnCalibrationPoint;
            Client.CalibrationProgressReceived += OnCalibrationPointProgress;
            Client.CalibrationFinishedReceived += OnCalibrationFinished;
        }

        void UnsubscribeCallbacks()
        {
            if (GazeManager.I.skyle == null) return;

            Client.CalibrationPointReceived -= OnCalibrationPoint;
            Client.CalibrationProgressReceived -= OnCalibrationPointProgress;
            Client.CalibrationFinishedReceived -= OnCalibrationFinished;
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
            SubscribeCallbacks();
            ResetPage();
            //LoadToggleOptions();
        }

        void OnClosed(object sender, EventArgs e)
        {
            //IMPORTANTE: Hacer genérico si hay multiples eyetrackers que pueden recibir el comando.
            //GlobalEvents.OnStartCalibrationCommand.Remove(StartCalibrationAndCloseOnFinished); 
            CancelCalibration();
            UnsubscribeCallbacks();
        }


        void OnCalibrationPointProgress(SkyleCollectingCalibrationPoints points)
        {
            Debug.WriteLine("Progress: " + points.Progress);
            int percent = points.Progress;
            calibrationPointChanged = percent != CalibrationPointProgress;
            CalibrationPointProgress = percent;
            CalibrationPointProgress01 = (float)percent / 100f;
            currentPoint?.UpdateCalibrationPoint(CalibrationPointProgress01);
        }
        void OnCalibrationPoint(SkyleNextCalibrationPoint point)
        {
            Vector2 pointViewport = Helper.ScreenPointToViewport(new Vector2(point.Point.X, point.Point.Y));
            SpawnCalibrationPoint(pointViewport);
            Debug.WriteLine("New calibration point called");
        }

        void OnCalibrationFinished(SkyleFinishedCalibration finished)
        {
            Debug.WriteLine("Skyle Calibration finished");
            EnableButtons();
        }

        public void SpawnCalibrationPoint(Vector2 viewportPos)
        {
            DestroyCurrentCalibrationPoint();
            CalibrationPointGraphic newPoint = new CalibrationPointGraphic(20, System.Windows.Media.Color.FromArgb(255, 0, 255, 0));
            currentPoint = newPoint;
            MainCanvas.Children.Add(currentPoint.Element);
            MainCanvas.SetElementToViewportPositionActualSize(currentPoint.Element, viewportPos);
            points.Add(currentPoint);
        }
        private void DestroyCurrentCalibrationPoint()
        {
            if (currentPoint == null) return;
            currentPoint.Destroy(MainCanvas);
            currentPoint = null;
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

        void DisableButtons()
        {
            SettingsPanel.IsHitTestVisible = false;
            SettingsPanel.Visibility = Visibility.Collapsed;
            CloseButton.IsHitTestVisible = false;
            CloseButton.Visibility = Visibility.Collapsed;
            BordeImantado.Visibility = Visibility.Collapsed;
            BordePuntero.Visibility = Visibility.Collapsed;
        }

        void EnableButtons()
        {
            SettingsPanel.IsHitTestVisible = true;
            SettingsPanel.Visibility = Visibility.Visible;
            CloseButton.IsHitTestVisible = true;
            CloseButton.Visibility = Visibility.Visible;
            BordeImantado.Visibility = Visibility.Visible;
            BordePuntero.Visibility = Visibility.Visible;
        }

        private void ResetPage()
        {
            Debug.WriteLine("Resetting Page");
            DestroyCurrentCalibrationPoint();
            EyeDisplay.Visibility = Visibility.Collapsed;
            EnableButtons();
            FadeText.HideMessage();
        }
    }
}
