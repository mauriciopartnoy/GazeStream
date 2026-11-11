using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using GazeStream.AppData;
using GazeStream.Controls;
using GazeStream.Eyetracker;
using GazeStream.Utilities;
using GazeStream.Utilities.Events;
using SkyleAvaloniaExample.Interop;

namespace GazeStream.Windows
{
    /// <summary>
    /// Calibración del Skyle. El dispositivo decide la posición de cada punto (callback OnCalibrationPoint);
    /// la ventana lo dibuja, espera a que el usuario lo mire y pide la recolección.
    /// Los callbacks llegan en el hilo de I/O de skylelib: todo cambio de estado se hace en el hilo de UI.
    /// </summary>
    public partial class SkyleCalibrationWindow : Window
    {
        public static SkyleCalibrationWindow I { get; private set; } //Singleton Instance
        public static int CalibrationPointProgress { get; private set; }
        public static float CalibrationPointProgress01 { get; private set; }

        //Tiempo que se muestra cada punto antes de empezar a recolectar, para que el usuario llegue a mirarlo.
        static readonly TimeSpan POINT_SETTLE_TIME = TimeSpan.FromSeconds(1.5);
        //Límite por punto: si el Skyle no termina, se aborta para no dejar la app esperando.
        static readonly TimeSpan TIMEOUT_PER_POINT = TimeSpan.FromSeconds(15);

        readonly List<CalibrationPointGraphic> points = new();
        CalibrationPointGraphic? currentPoint;
        bool isCalibrating;
        bool closeOnFinished;
        //Identifica la calibración en curso; los callbacks y tareas de una calibración anterior se descartan.
        int session;
        SkyleClient? client;

        public SkyleCalibrationWindow()
        {
            InitializeComponent();
            I = this;
            Loaded += OnLoaded;
            Closed += OnClosed;
            PreviewKeyDown += CancelCalibrationOnKeyPress;
            ForceTopmost();
            DataContext = Settings.I;
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
            if (!isCalibrating) ResetPage();
            LoadPointsOption();
        }

        void OnClosed(object sender, EventArgs e)
        {
            CancelCalibration();
            if (I == this) I = null;
        }

        void LoadPointsOption()
        {
            switch (Settings.I.LastPointsOption.Value)
            {
                case 0: PointsSetting_3.IsChecked = true; break;
                case 1: PointsSetting_5.IsChecked = true; break;
                default: PointsSetting_9.IsChecked = true; break;
            }
        }

        int GetPointsSelection()
        {
            if (PointsSetting_3.IsChecked == true) return 0;
            if (PointsSetting_5.IsChecked == true) return 1;
            return 2;
        }

        // ---- Inicio / fin ----

        public void StartCalibration_Click(object sender, RoutedEventArgs e)
        {
            int pointsArray = GetPointsSelection();
            Settings.I.LastPointsOption.Value = pointsArray;
            StartCalibration(pointsArray, closeOnFinished: false);
        }

        /// <param name="pointsArray">0: 3 puntos, 1: 5 puntos, 2: 9 puntos.</param>
        public void StartCalibration(int pointsArray, bool closeOnFinished)
        {
            if (isCalibrating) { Debug.WriteLine("Skyle: ya hay una calibración en curso."); return; }

            GazeDeviceSkyle? skyle = GazeManager.I?.skyle;
            SkyleClient? c = skyle?.Client;
            if (skyle == null || c == null || !skyle.IsConnected)
            {
                GlobalEvents.OnCalibrationFailed.Invoke();
                _ = ShowMessage("El Skyle no está conectado.");
                return;
            }

            if (c.IsLocalLink)
            {
                //Skyle X es dueño del dispositivo: se le pide que muestre su propia calibración.
                int hostResult = c.StartHostCalibration();
                Debug.WriteLine("Skyle: calibración delegada a Skyle X: " + (SkyleResult)hostResult);
                if (hostResult != (int)SkyleResult.Ok) GlobalEvents.OnCalibrationFailed.Invoke();
                Close();
                return;
            }

            byte[] pointIndices = GazeDeviceSkyle.GetCalibrationPoints(pointsArray);
            this.closeOnFinished = closeOnFinished;
            client = c;
            int mySession = ++session;
            Subscribe(c);
            isCalibrating = true;
            DisableButtons();
            FadeText.HideMessage();
            GlobalEvents.OnCalibrationStart.Invoke();

            int result = c.StartCalibration(pointIndices);
            if (result != (int)SkyleResult.Ok)
            {
                FailCalibration("No se pudo iniciar la calibración del Skyle.");
                return;
            }

            _ = AbortIfTimedOut(mySession, TimeSpan.FromTicks(TIMEOUT_PER_POINT.Ticks * pointIndices.Length));
        }

        async Task AbortIfTimedOut(int mySession, TimeSpan timeout)
        {
            await Task.Delay(timeout);
            if (isCalibrating && session == mySession)
            {
                Debug.WriteLine("Skyle: tiempo de calibración agotado.");
                client?.AbortCalibration();
                FailCalibration("La calibración tardó demasiado. Intentá de nuevo.");
            }
        }

        void Subscribe(SkyleClient c)
        {
            c.CalibrationPointReceived += OnCalibrationPoint;
            c.CalibrationProgressReceived += OnCalibrationPointProgress;
            c.CalibrationFinishedReceived += OnCalibrationFinished;
            c.CalibrationAbortedReceived += OnCalibrationAborted;
        }

        void Unsubscribe()
        {
            if (client == null) return;
            client.CalibrationPointReceived -= OnCalibrationPoint;
            client.CalibrationProgressReceived -= OnCalibrationPointProgress;
            client.CalibrationFinishedReceived -= OnCalibrationFinished;
            client.CalibrationAbortedReceived -= OnCalibrationAborted;
        }

        void CompleteCalibration()
        {
            if (!isCalibrating) return;
            isCalibrating = false;
            session++;
            Unsubscribe();
            DestroyCurrentCalibrationPoint();
            GlobalEvents.OnCalibrationSuccess.Invoke();
            GlobalEvents.OnCalibrationFinished.Invoke();

            if (closeOnFinished) { Close(); return; }
            EnableButtons();
            _ = ShowMessage("¡La calibración fue exitosa!");
        }

        void FailCalibration(string message)
        {
            if (!isCalibrating) return;
            isCalibrating = false;
            session++;
            Unsubscribe();
            DestroyCurrentCalibrationPoint();
            GlobalEvents.OnCalibrationFailed.Invoke();
            //Finished reanuda el bucle de GazeManager, que se detuvo con OnCalibrationStart.
            GlobalEvents.OnCalibrationFinished.Invoke();
            EnableButtons();
            _ = ShowMessage(message);
        }

        void CancelCalibration()
        {
            if (!isCalibrating)
            {
                ResetPage();
                return;
            }
            isCalibrating = false;
            session++;
            client?.AbortCalibration();
            Unsubscribe();
            //Cancel reanuda el bucle de GazeManager.
            GlobalEvents.OnCalibrationCancel.Invoke();
            ResetPage();
        }

        // ---- Callbacks del Skyle (hilo de I/O de skylelib) ----

        void OnCalibrationPoint(SkyleNextCalibrationPoint point)
        {
            Debug.WriteLine($"Skyle calibration point {point.Index} at X:{point.Point.X} Y:{point.Point.Y}");
            App.Instance.UIDispatcher.BeginInvoke(() =>
            {
                if (!isCalibrating) return;
                int mySession = session;
                CalibrationPointProgress = 0;
                CalibrationPointProgress01 = 0;
                SpawnCalibrationPoint(Helper.ScreenPointToViewport(new Vector2(point.Point.X, point.Point.Y)));
                _ = CollectAfterDelay(mySession);
            });
        }

        async Task CollectAfterDelay(int mySession)
        {
            //Antes se dormía el hilo de I/O de skylelib, bloqueando todos los mensajes del dispositivo.
            await Task.Delay(POINT_SETTLE_TIME);
            if (isCalibrating && session == mySession)
            {
                client?.CollectCalibrationPoint();
            }
        }

        void OnCalibrationPointProgress(SkyleCollectingCalibrationPoints progress)
        {
            App.Instance.UIDispatcher.BeginInvoke(() =>
            {
                if (!isCalibrating) return;
                CalibrationPointProgress = progress.Progress;
                CalibrationPointProgress01 = progress.Progress / 255f; //Progress va de 0 a 255.
                currentPoint?.UpdateCalibrationPoint(CalibrationPointProgress01);
            });
        }

        void OnCalibrationFinished(SkyleFinishedCalibration finished)
        {
            Debug.WriteLine($"Skyle calibration finished. left:{finished.left_count} right:{finished.right_count}");
            App.Instance.UIDispatcher.BeginInvoke(CompleteCalibration);
        }

        void OnCalibrationAborted()
        {
            Debug.WriteLine("Skyle calibration aborted by device.");
            App.Instance.UIDispatcher.BeginInvoke(() => FailCalibration("El Skyle canceló la calibración."));
        }

        // ---- UI ----

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

        async Task ShowMessage(string message)
        {
            try { await FadeText.ShowMessage(message, .5, 2.5); }
            catch (TaskCanceledException) { }
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
        }

        void CancelCalibration_Click(object e, RoutedEventArgs args)
        {
            CancelCalibration();
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
            DestroyCurrentCalibrationPoint();
            EyeDisplay.Visibility = Visibility.Collapsed;
            EnableButtons();
            FadeText.HideMessage();
        }
    }
}
