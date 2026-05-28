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
using System.Numerics;
using System.Diagnostics;
using GazeStream.Eyetracker;
using GazeStream.Utilities;
using GazeStream.Utilities.Save;
using GazeStream.AppData;
using GazeStream.Controls;
using GazeStream.Utilities.Events;
using System.Runtime.InteropServices;

namespace GazeStream.Windows
{
    public partial class CalibrationWindow : Window
    {
        public static CalibrationWindow I { get; private set; } //Singleton Instance

        public static ASeeTracker.processCallback processCB = new ASeeTracker.processCallback(process_callback);
        public static ASeeTracker.finishCallback finishCB = new ASeeTracker.finishCallback(finish_callback);
        public static ASeeTracker.gazeCallback gazeCB = new ASeeTracker.gazeCallback(OnGazeCallback);

        Task? calibrationTask;
        CancellationTokenSource cts;
        bool isCalibrating;
        List<CalibrationPointGraphic> points = new List<CalibrationPointGraphic>();
        static CalibrationPointGraphic? currentPoint;
        static bool pointComplete;
        _7i_coefficient_t coefficient;
        static _7i_eye_data_ex_t eyesData;
        static Vector3 gazeOrigin;
        static bool calibrationPointChanged;
        static DateTime lastCalibrationPointChangeTime;
        static TimeSpan calibrationPointUnchangedTime;
        public static int CalibrationPointProgress { get; private set; }
        public static float CalibrationPointProgress01 { get; private set; }


        Vector2[] points3 = {
                new Vector2(0.05f, 0.95f),
                new Vector2(0.95f, 0.95f),
                new Vector2(0.50f, 0.05f)};

        //Vector2[] points5 = {
        //        new Vector2(0.50f, 0.50f),
        //        new Vector2(0.05f, 0.95f),
        //        new Vector2(0.95f, 0.95f),
        //        new Vector2(0.05f, 0.05f),
        //        new Vector2(0.95f, 0.05f)};

        Vector2[] points5 = {
                new Vector2(0.50f, 0.50f),
                new Vector2(0.01f, 0.99f),
                new Vector2(0.99f, 0.99f),
                new Vector2(0.01f, 0.01f),
                new Vector2(0.99f, 0.01f)};

        Vector2[] points9 = {
                new Vector2(0.50f, 0.50f),
                new Vector2(0.95f, 0.50f),
                new Vector2(0.05f, 0.95f),
                new Vector2(0.50f, 0.95f),
                new Vector2(0.95f, 0.95f),
                new Vector2(0.05f, 0.05f),
                new Vector2(0.50f, 0.05f),
                new Vector2(0.95f, 0.05f),
                new Vector2(0.05f, 0.50f)};

        public CalibrationWindow()
        {
            InitializeComponent();
            I = this;
            Loaded += OnLoaded;
            Closed += OnClosed;
            PreviewKeyDown += CancelCalibrationOnKeyPress;
            GlobalEvents.OnStartCalibrationCommand.Add(StartCalibrationAndCloseOnFinished);
            ForceTopmost();
        }

        void InitializePointArraysUsingScreenSpace()
        {
            Vector2 screen = new Vector2((float)this.ActualWidth, (float)this.ActualHeight); //Canvas is this size.
            float pixelMargin = 20;
            float left = pixelMargin / screen.X;
            float right = 1f - left;
            float top = pixelMargin / screen.Y;
            float down = 1f - top;

            //TODO 
            //float marginPercent = 0.03f;
            //float left = marginPercent;
            //float right = 1f - marginPercent;
            //float top = marginPercent;
            //float down = 1f - marginPercent;

            //Positions
            Vector2 middleCenter = new Vector2(.5f, .5f);
            Vector2 middleLeft = new Vector2(left, .5f);
            Vector2 middleRight = new Vector2(right, .5f);
            Vector2 lowerCenter = new Vector2(.5f, down);
            Vector2 lowerLeft = new Vector2(left, down);
            Vector2 lowerRight = new Vector2(right, down);
            Vector2 upperCenter = new Vector2(.5f, top);
            Vector2 upperLeft = new Vector2(left, top);
            Vector2 upperRight = new Vector2(right, top);

            points3 = new Vector2[3];
            points3[0] = lowerLeft;
            points3[1] = lowerRight;
            points3[2] = upperCenter;

            points5 = new Vector2[5];
            points5[0] = middleCenter;
            points5[1] = upperLeft;
            points5[2] = upperRight;
            points5[3] = lowerLeft;
            points5[4] = lowerRight;

            points9 = new Vector2[9];
            points9[0] = middleCenter;
            points9[1] = middleRight;
            points9[2] = upperLeft;
            points9[3] = upperCenter;
            points9[4] = upperRight;
            points9[5] = lowerLeft;
            points9[6] = lowerCenter;
            points9[7] = lowerRight;
            points9[8] = middleLeft;
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
            Debug.WriteLine("Landed on Calibration Page. Loading RadioButton settings.");
            InitializePointArraysUsingScreenSpace();
            ResetPage();
            LoadToggleOptions();
            Settings.I.FilterProfile.OnValueChanged += SetFilterSelectionRadioButton;
        }

        void OnClosed(object sender, EventArgs e)
        {
            Debug.WriteLine("Calibration Window closed. Loading RadioButton settings.");
            GlobalEvents.OnStartCalibrationCommand.Remove(StartCalibrationAndCloseOnFinished);
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

        void LoadToggleOptions()
        {
            SetEyesSelectionRadioButton(Settings.I.LastEyesOption.Value);
            SetPointsSettingRadioButton(Settings.I.LastPointsOption.Value);
            SetFilterSelectionRadioButton(Settings.I.FilterProfile.Value);
        }

        public static void process_callback(int index, int percent, IntPtr context)
        {
            calibrationPointChanged = percent != CalibrationPointProgress;
            CalibrationPointProgress = percent;
            CalibrationPointProgress01 = (float)percent / 100f;
            currentPoint?.UpdateCalibrationPoint(CalibrationPointProgress01);
            //Debug.WriteLine($"process: {index},{percent}");
        }

        public static void OnGazeCallback(ref _7i_eye_data_ex_t eyes, IntPtr context)
        {
            Debug.WriteLine($"Gaze Origin: X {eyes.recom_gaze.gaze_origin.x} Z {eyes.recom_gaze.gaze_origin.z} " +
                $"GazeOriginLeft: X {eyes.left_gaze.gaze_origin.x} Z {eyes.left_gaze.gaze_origin.z} " +
                $"PupilDistLeft: {eyes.left_pupil.pupil_distance} " +
                $"LeftDiameter: {eyes.left_pupil.pupil_diameter} " +
                $"LeftDmm: {eyes.left_pupil.pupil_diameter_mm}");
            if (eyes.left_pupil.pupil_center.x <= 0 || eyes.right_pupil.pupil_center.x <= 0) return;
            eyesData = eyes;
            //Debug.WriteLine($"Pupil Mm: {eyes.right_pupil.pupil_diameter_mm} PupilPos: {eyes.right_pupil.pupil_center} PupilDist: {eyes.right_pupil.pupil_distance}");
        }

        public static void finish_callback(int index, int error, IntPtr context)
        {
            CalibrationPointProgress01 = 0;
            CalibrationPointProgress = 0;
            Debug.WriteLine($"finish: {index}, {error}");
            pointComplete = true;
        }


        public void DefaultCalibration_Click(object sender, RoutedEventArgs e)
        {
            if (GazeManager.I.IsJoacoDevice)
            {
                GazeManager.I.joacoA11.SetDefaultCalibration();
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
            StartCalibrationTaskUsingPanelSettings();
        }

        void StartCalibrationAndCloseOnFinished(int pointsArrayIndex, int eyes)
        {
            if (isCalibrating) { Debug.WriteLine("Is Calibrating. Cannot start new calibration."); return; }
            cts = new CancellationTokenSource();
            calibrationTask = StartCalibrationTask(pointsArrayIndex, eyes, true, cts.Token);
        }
        void StartCalibrationTaskUsingPanelSettings()
        {
            int pointsArrayIndex = GetPointsSelection();
            int eyes = GetEyesSelection();
            cts = new CancellationTokenSource();
            calibrationTask = StartCalibrationTask(pointsArrayIndex, eyes, false, cts.Token);
        }

        async Task StartCalibrationTask(int pointsArrayIndex, int eyes, bool closeOnFinished, CancellationToken token)
        {
            Debug.WriteLine("Starting calibration process.");
            if (isCalibrating) return;
            isCalibrating = true;
            GlobalEvents.OnCalibrationStart.Invoke();

            try
            {
                DisableButtons();
                if (!TryConnectEyetrackerDevice())
                {
                    CancelCalibration();
                    return;
                }

                await FadeText.ShowMessage("Estos son tus ojos.", .5, 2, token);
                await ShowEyeDisplay(token);
                await FadeText.ShowMessage("Mira los puntos para hacerlos desaparecer.", .5, 2, token);          
                await CalibrationTask(pointsArrayIndex, eyes, token);
            }
            catch
            {

            }
            finally
            {
                ResetPage();
                isCalibrating = false;
                GazeManager.I?.GazeDevice?.Initialize();
                GlobalEvents.OnCalibrationFinished.Invoke();
                Debug.WriteLine("Calibration Finished!");
                if (closeOnFinished)
                {
                    Close();
                }
            }
        }

        async Task ShowEyeDisplay(CancellationToken token)
        {
            EyeDisplay.Visibility = Visibility.Visible;

            var tcs = new TaskCompletionSource();

            void OnKeyDown(object s, System.Windows.Input.KeyEventArgs e) => tcs.TrySetResult();
            void OnMouseDown(object s, MouseButtonEventArgs e) => tcs.TrySetResult();
            void OnTouchDown(object s, TouchEventArgs e) => tcs.TrySetResult();

            this.KeyDown += OnKeyDown;
            this.MouseDown += OnMouseDown;
            this.TouchDown += OnTouchDown;
            int timeoutMs = 5000; //ms
            int timer = 0;
            try
            {
                while (!tcs.Task.IsCompleted)
                {
                    EyeDisplay.UpdateEyeDisplay(eyesData);
                    await Task.Delay(16, token);
                    timer += 16;
                    if (timer > timeoutMs)
                    {
                        tcs.TrySetResult();
                    }
                }
            }
            finally
            {
                this.KeyDown -= OnKeyDown;
                this.MouseDown -= OnMouseDown;
                this.TouchDown -= OnTouchDown;
                EyeDisplay.Visibility = Visibility.Collapsed;
            }
        }

        public bool TryConnectEyetrackerDevice()
        {
            Disconnect();
            int startResult = ASeeTracker._7i_start(AppPaths.EyetrackerConfigPath);
            ASeeTracker._7i_set_gaze_callback(Marshal.GetFunctionPointerForDelegate(gazeCB), IntPtr.Zero);


            if (startResult == 0)
            {
                GlobalEvents.OnEyetrackerConnected.Invoke();
                ErrorText.Text = "";
                Debug.WriteLine("Joaco Device connected.");
                UpdateCoefficient();
                int startTrackingResult = ASeeTracker._7i_start_tracking(ref coefficient);
                Debug.WriteLine("Start tracking result: " + startTrackingResult);
                return true;
            }
            else
            {
                GlobalEvents.OnEyetrackerConnectionFailed.Invoke();
                ErrorText.Text = "¡Algo salió mal! No fue posible iniciar el dispositivo Joaco.";
                Debug.WriteLine("Couldn't connect to Joaco Device. Are you using the correct SDK?");
                return false;
            }
        }

        void UpdateCoefficient()
        {
            coefficient = new _7i_coefficient_t();
            coefficient.buf = Settings.I.LastCalibrationBuff.Value;
            Debug.WriteLine("Las calibration buff: " + coefficient.buf.Length);

            if (coefficient.buf == null || coefficient.buf.Length == 0)
            {

                coefficient.buf = GetDefaultCalibrationBuff();
                Debug.WriteLine("coe2 = " + coefficient.buf);
            }
        }

        public byte[] GetDefaultCalibrationBuff()
        {
            //CALIBRACIÓN PARA EL SDK ANTERIOR
            //byte[] defaultCalibration = new byte[] { 141, 19, 0, 0, 193, 134, 1, 0, 0, 0, 0, 192, 226, 172, 35, 64, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 50, 48, 50, 52, 48, 57, 48, 52, 95, 49, 48, 50, 52, 95, 67, 79, 77, 80, 85, 84, 69, 67, 72, 65, 78, 71, 69, 0, 0, 0, 0, 0, 85, 85, 85, 60, 247, 18, 122, 63, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 154, 153, 249, 64, 102, 102, 118, 64, 72, 225, 170, 63, 0, 0, 0, 0, 208, 151, 246, 188, 88, 153, 129, 191, 92, 73, 175, 19, 134, 192, 219, 191, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 128, 235, 122, 182, 93, 82, 149, 63, 89, 151, 138, 178, 46, 97, 246, 191, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 178, 79, 181, 119, 71, 8, 219, 63, 210, 216, 56, 49, 140, 129, 246, 63, 51, 51, 51, 51, 51, 51, 31, 64, 205, 204, 204, 204, 204, 204, 16, 64, 102, 102, 102, 102, 102, 102, 245, 63, 205, 204, 204, 204, 204, 204, 20, 64, 0, 0, 0, 0, 0, 0, 240, 63, 141, 19, 0, 0, 193, 134, 1, 0, 0, 0, 0, 96, 24, 111, 33, 64, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 50, 48, 50, 52, 48, 57, 48, 52, 95, 49, 48, 50, 52, 95, 67, 79, 77, 80, 85, 84, 69, 67, 72, 65, 78, 71, 69, 0, 0, 0, 0, 0, 85, 85, 85, 60, 247, 18, 122, 63, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 154, 153, 249, 64, 102, 102, 118, 64, 72, 225, 170, 63, 0, 0, 0, 0, 64, 63, 224, 125, 43, 109, 123, 191, 33, 27, 133, 108, 104, 59, 221, 191, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 54, 166, 81, 227, 240, 68, 189, 191, 246, 178, 138, 45, 169, 170, 244, 191, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 237, 249, 60, 24, 182, 114, 222, 63, 97, 121, 255, 70, 254, 108, 224, 63, 51, 51, 51, 51, 51, 51, 31, 64, 205, 204, 204, 204, 204, 204, 16, 64, 102, 102, 102, 102, 102, 102, 245, 63, 205, 204, 204, 204, 204, 204, 20, 64, 0, 0, 0, 0, 0, 0, 240, 63, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 64, 3, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

            //CALIBRACIÓN PARA LA ÚLTIMA VERSIÓN DEL SDK
            byte[] defaultCalibration = new byte[] { 232, 3, 0, 0, 232, 3, 0, 0, 0, 0, 0, 192, 116, 254, 35, 64, 0, 0, 0, 192, 116, 254, 35, 64, 0, 0, 0, 0, 0, 0, 240, 63, 0, 0, 0, 0, 0, 0, 240, 63, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 50, 48, 50, 53, 49, 50, 48, 49, 95, 50, 48, 52, 56, 95, 80, 79, 76, 89, 83, 67, 82, 69, 69, 78, 50, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 63, 0, 0, 0, 63, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 10, 89, 153, 167, 236, 215, 179, 63, 121, 221, 221, 34, 55, 30, 96, 191, 190, 96, 71, 181, 178, 43, 31, 64, 184, 207, 181, 235, 139, 243, 27, 64, 133, 235, 81, 184, 30, 133, 245, 63, 0, 0, 0, 0, 0, 0, 0, 0, 101, 125, 56, 130, 105, 117, 105, 64, 1, 18, 37, 139, 149, 111, 71, 64, 126, 123, 102, 35, 0, 186, 128, 192, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 154, 188, 27, 187, 62, 99, 239, 222, 214, 191, 70, 123, 128, 144, 40, 18, 186, 63, 196, 21, 108, 65, 96, 85, 187, 63, 24, 156, 213, 137, 103, 178, 99, 63, 0, 0, 0, 0, 0, 0, 178, 188, 234, 228, 10, 186, 10, 150, 228, 191, 177, 79, 88, 34, 116, 71, 150, 63, 250, 228, 129, 144, 132, 148, 192, 63, 99, 144, 120, 218, 62, 250, 167, 191, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 247, 230, 60, 193, 60, 89, 223, 63, 63, 77, 143, 200, 60, 125, 240, 63, 51, 51, 51, 51, 51, 51, 31, 64, 205, 204, 204, 204, 204, 204, 16, 64, 102, 102, 102, 102, 102, 102, 245, 63, 205, 204, 204, 204, 204, 204, 20, 64, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 232, 3, 0, 0, 232, 3, 0, 0, 0, 0, 0, 192, 116, 254, 35, 64, 0, 0, 0, 192, 116, 254, 35, 64, 0, 0, 0, 0, 0, 0, 240, 63, 0, 0, 0, 0, 0, 0, 240, 63, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 50, 48, 50, 53, 49, 50, 48, 49, 95, 50, 48, 52, 56, 95, 80, 79, 76, 89, 83, 67, 82, 69, 69, 78, 50, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 63, 0, 0, 0, 63, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 113, 26, 35, 234, 8, 230, 4, 192, 154, 74, 5, 134, 222, 98, 241, 191, 118, 50, 243, 81, 77, 51, 31, 64, 11, 81, 223, 251, 141, 119, 15, 64, 66, 21, 161, 179, 66, 51, 245, 63, 0, 0, 0, 0, 0, 0, 0, 0, 139, 98, 8, 17, 123, 128, 112, 64, 245, 133, 254, 6, 35, 30, 72, 64, 233, 239, 77, 178, 148, 183, 128, 192, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 128, 177, 60, 195, 60, 27, 139, 1, 210, 216, 191, 11, 193, 49, 195, 156, 134, 167, 63, 96, 95, 32, 175, 199, 47, 129, 63, 235, 33, 114, 64, 151, 158, 170, 63, 0, 0, 0, 0, 0, 0, 130, 188, 76, 206, 250, 111, 59, 35, 234, 191, 112, 23, 134, 219, 177, 2, 165, 63, 144, 55, 217, 165, 239, 232, 115, 191, 243, 140, 241, 185, 232, 80, 184, 191, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 221, 242, 10, 34, 160, 9, 215, 191, 213, 131, 141, 2, 236, 23, 238, 63, 51, 51, 51, 51, 51, 51, 31, 64, 205, 204, 204, 204, 204, 204, 16, 64, 102, 102, 102, 102, 102, 102, 245, 63, 205, 204, 204, 204, 204, 204, 20, 64, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

            Debug.WriteLine(defaultCalibration.Length);
            return defaultCalibration;
        }


        async Task CalibrationTask(int pointsArrayIndex, int eyes, CancellationToken token)
        {
            Debug.WriteLine("Starting calibration Task.");
            Vector2[] points = GetCalibrationPointsArray(pointsArrayIndex);
            _7i_coefficient_t coefficient = new _7i_coefficient_t();
            int pointIndex = 1;
            int pointCount = points.Length;


            //Calibration Mode - 0: ambos ojos, 1: ojo izq, 2: ojo der.
            int setModeResult = ASeeTracker._7i_set_calibration_mode(eyes);
            int result = ASeeTracker._7i_start_calibration(pointCount);

            for (int i = 0; isCalibrating && i < pointCount; i++)
            {
                pointComplete = false;

                //El gráfico de calibración se muestra antes para que el usuario pueda tener la mirada en la posición correcta al iniciar cada punto.              
                SpawnCalibrationPoint(new Vector2(points[i].X, points[i].Y));
                await Task.Delay(TimeSpan.FromSeconds(2), token);

                lastCalibrationPointChangeTime = DateTime.Now;
                _7i_point2d_t point = new _7i_point2d_t();
                point.x = points[i].X;
                point.y = points[i].Y;
                result = ASeeTracker._7i_start_calibration_point(pointIndex, ref point,
                Marshal.GetFunctionPointerForDelegate(processCB), IntPtr.Zero,
                Marshal.GetFunctionPointerForDelegate(finishCB), IntPtr.Zero);
                Debug.WriteLine($"_7i_start_calibration_point: {pointIndex}, {result}");


                while (!pointComplete)
                {
                    //Open Restart Dialog if a point is not calibrated after a period
                    if (calibrationPointChanged)
                    {
                        lastCalibrationPointChangeTime = DateTime.Now;
                    }
                    calibrationPointUnchangedTime = DateTime.Now - lastCalibrationPointChangeTime;
                    App.Instance.Dispatcher.BeginInvoke(() => ShowRestartCalibrationWindow(calibrationPointUnchangedTime.Seconds > 5));
                    await Task.Delay(16, token);
                }

                ++pointIndex;
            }

            //En caso de interrupción
            if (pointIndex != pointCount + 1)
            {
                result = ASeeTracker._7i_cancel_calibration();
                Console.WriteLine("_7i_cancel_calibration: {0:D}", result);
                CancelCalibration();
            }

            //En caso de exito
            int calibration_success = ASeeTracker._7i_compute_calibration(ref coefficient);
            Debug.WriteLine($"_7i_compute_calibration: {calibration_success}");
            result = ASeeTracker._7i_complete_calibration();
            Debug.WriteLine($"_7i_complete_calibration: {result}");

            //Puntaje para la calibración de cada ojo.
            float left_score = 0;
            float right_score = 0;
            ASeeTracker._7i_get_calibration_score(ref left_score, ref right_score);
            Console.WriteLine("_7i_complete_calibration: {0:G}, {1:G}", left_score, right_score);

            //Limpieza de gráficos
            DestroyCurrentCalibrationPoint();

            //OnSuccess
            if (0 == calibration_success)
            {
                Debug.WriteLine("Calibration success!");
                Disconnect();
                await FadeText.ShowMessage("¡La calibración fue exitosa!", .5, 2, token);
                Settings.I.LastEyesOption.Value = eyes;
                Settings.I.LastPointsOption.Value = pointsArrayIndex;
                Settings.I.LastCalibrationBuff.Value = coefficient.buf;
                Settings.I.SaveSettings();
                SaveCalibrationAsPreset(points.Length, eyes, left_score, right_score, coefficient.buf);
                GlobalEvents.OnCalibrationSuccess.Invoke();
            }
        }

        void ShowRestartCalibrationWindow(bool show)
        {
            if (show)
            {
                RestartPanel.Visibility = Visibility.Visible;
                RestartPanel.IsHitTestVisible = true;
            }
            else
            {
                RestartPanel.Visibility = Visibility.Collapsed;
                RestartPanel.IsHitTestVisible = false;
            }
        }

        void CloseRestartCalibrationWindow(object sender, RoutedEventArgs args)
        {
            lastCalibrationPointChangeTime = DateTime.Now;  //Resets timer.
            RestartPanel.Visibility = Visibility.Collapsed;
            RestartPanel.IsHitTestVisible = false;
        }

        void SaveCalibrationAsPreset(int points, int eyesOption, float scoreLeft, float scoreRight, byte[] buff)
        {
            Vector2 screenSize = Helper.GetScreenSize(this);
            string name = $"Preset_{screenSize.X}_{screenSize.Y}";
            EyesData eyes = new EyesData(eyesData);
            CalibrationPreset preset = new CalibrationPreset(name, scoreLeft, scoreRight, screenSize, gazeOrigin, eyes,points, eyesOption, buff);
            CalibrationPresets.I.AddPreset(preset);            
        }


        bool startedCancelProcess;
        public void CancelCalibration()
        {
            if (!isCalibrating) return;
            if (startedCancelProcess) return;
            startedCancelProcess = true;
            cts.Cancel();
            ResetPage();
            Disconnect();
            GlobalEvents.OnCalibrationCancel.Invoke();
            GlobalEvents.OnCalibrationFinished.Invoke();
            isCalibrating = false;
            startedCancelProcess = false;
        }

        void CancelCalibration_Click(object e, RoutedEventArgs args)
        {
            CancelCalibration();
        }

        private void ResetPage()
        {
            Debug.WriteLine("Resetting Page");
            //isCalibrating = false;
            pointComplete = false;
            CloseRestartCalibrationWindow(null, null);
            DestroyCurrentCalibrationPoint();
            EyeDisplay.Visibility = Visibility.Collapsed;
            EnableButtons();
            FadeText.HideMessage();
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

        private void DestroyAllCalibrationPoints()
        {
            foreach (CalibrationPointGraphic p in points)
            {
                p.Destroy(MainCanvas);
            }
            points.Clear();
        }

        public void Disconnect()
        {
            try
            {
                Debug.WriteLine("Cancel calibration 1");
                ASeeTracker._7i_cancel_calibration();
                Debug.WriteLine("Cancel calibration 2");
                ASeeTracker._7i_stop_tracking();
                Debug.WriteLine("Cancel calibration 3");
                ASeeTracker._7i_stop();
            }
            catch
            {

            }
            finally
            {
                Debug.WriteLine("Cancel calibration 4");
                ASeeTracker._7i_device_disconnect();
                Debug.WriteLine("Cancel calibration 5");
                GlobalEvents.OnEyetrackerDisconnected.Invoke();
            }
        }

        void DisableButtons()
        {
            SettingsPanel.IsHitTestVisible = false;
            SettingsPanel.Visibility = Visibility.Collapsed;
            CloseButton.IsHitTestVisible = false;
            CloseButton.Visibility = Visibility.Collapsed;
        }

        void EnableButtons()
        {
            SettingsPanel.IsHitTestVisible = true;
            SettingsPanel.Visibility = Visibility.Visible;
            CloseButton.IsHitTestVisible = true;
            CloseButton.Visibility = Visibility.Visible;
        }


        #region CONTROL_SETTINGS
        public int GetEyesSelection()
        {
            if (EyeSetting_Left.IsChecked == true)
            {
                return 1;
            }
            else if (EyeSetting_Right.IsChecked == true)
            {
                return 2;
            }
            else return 0; //Both Eyes
        }

        public void SetEyesSelectionRadioButton(int eyesOption)
        {
            if (eyesOption == 1)
            {
                EyeSetting_Left.IsChecked = true;
            }
            else if (eyesOption == 2)
            {
                EyeSetting_Right.IsChecked = true;
            }
            else
            {
                EyeSetting_Both.IsChecked = true;
            }
        }

        public int GetFilterSelection()
        {
            if (FilterSetting_Bajo.IsChecked == true)
            {
                return 0;
            }
            else if (FilterSetting_Medio.IsChecked == true)
            {
                return 1;
            }
            else return 2;  //Filter Alto
        }

        public void SetFilterSelectionRadioButton(FilterProfile profile)
        {
            int filterOption = (int)profile;
            if (filterOption == 0)
            {
                FilterSetting_Bajo.IsChecked = true;
            }
            else if (filterOption == 1)
            {
                FilterSetting_Medio.IsChecked = true;
            }
            else
            {
                FilterSetting_Alto.IsChecked = true;
            }
        }

        private void FilterSetting_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.RadioButton rb)
            {
                switch (rb.Name)
                {
                    case nameof(FilterSetting_Bajo):
                        Settings.I.FilterProfile.Value = (FilterProfile)0;
                        break;

                    case nameof(FilterSetting_Medio):
                        Settings.I.FilterProfile.Value = (FilterProfile)1;
                        break;

                    case nameof(FilterSetting_Alto):
                        Settings.I.FilterProfile.Value = (FilterProfile)2;
                        break;
                }
            }
        }

        public int GetPointsSelection()
        {
            if (PointsSetting_3.IsChecked == true)
            {
                return 0;
            }
            else if (PointsSetting_5.IsChecked == true)
            {
                return 1;
            }
            else return 2; //9 points
        }

        public Vector2[] GetPointsUsingMenuSelection()
        {
            int pointsSetting = GetPointsSelection();
            return GetCalibrationPointsArray(pointsSetting);
        }

        public Vector2[] GetCalibrationPointsArray(int index)
        {
            Vector2[] points = default;
            switch (index)
            {
                case 0: points = points3; break;
                case 1: points = points5; break;
                case 2: points = points9; break;
            }
            return points;
        }

        public void SetPointsSettingRadioButton(int pointsOption)
        {
            if (pointsOption == 0)
            {
                PointsSetting_3.IsChecked = true;
            }
            else if (pointsOption == 1)
            {
                PointsSetting_5.IsChecked = true;
            }
            else
            {
                PointsSetting_9.IsChecked = true;
            }
        }
        #endregion
    }
}
