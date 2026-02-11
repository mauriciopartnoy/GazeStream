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
        bool isCalibrating;
        List<CalibrationPointGraphic> points = new List<CalibrationPointGraphic>();
        static CalibrationPointGraphic? currentPoint;
        static bool pointComplete;
        static _7i_eye_data_ex_t eyesData;
        public static int CalibrationPointProgress { get; private set; }
        public static float CalibrationPointProgress01 { get; private set; }


        Vector2[] points3 = {
                new Vector2(0.05f, 0.95f),
                new Vector2(0.95f, 0.95f),
                new Vector2(0.50f, 0.05f)};

        Vector2[] points5 = {
                new Vector2(0.50f, 0.50f),
                new Vector2(0.05f, 0.95f),
                new Vector2(0.95f, 0.95f),
                new Vector2(0.05f, 0.05f),
                new Vector2(0.95f, 0.05f)};

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
            GlobalEvents.OnStartCalibrationCommand.Add(StartCalibration);
        }
        void OnLoaded(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("Landed on Calibration Page. Loading RadioButton settings.");
            ResetPage();
            LoadToggleOptions();
            Settings.I.FilterProfile.OnValueChanged += SetFilterSelectionRadioButton;
        }

        void OnClosed(object sender, EventArgs e)
        {
            Debug.WriteLine("Calibration Window closed. Loading RadioButton settings.");
            CancelCalibration();
        }

        void LoadToggleOptions()
        {
            SetEyesSelectionRadioButton(Settings.I.LastEyesOption.Value);
            SetPointsSettingRadioButton(Settings.I.LastPointsOption.Value);
            SetFilterSelectionRadioButton(Settings.I.FilterProfile.Value);
        }

        public static void process_callback(int index, int percent, IntPtr context)
        {
            CalibrationPointProgress = percent;
            CalibrationPointProgress01 = (float)percent / 100f;
            currentPoint?.UpdateCalibrationPoint(CalibrationPointProgress01);
            //Debug.WriteLine($"process: {index},{percent}");
        }

        public static void OnGazeCallback(ref _7i_eye_data_ex_t eyes, IntPtr context)
        {
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

        public void Back_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
            Debug.WriteLine("Back pressed. Closing window.");
        }

        public void StartCalibration_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("Starting calibration");
            if (isCalibrating) return;
            calibrationTask = StartCalibrationTaskUsingPanelSettings();
            Debug.WriteLine("Starting calibration");
        }

        void StartCalibration(int pointsArrayIndex, int eyes)
        {
            _= StartCalibrationTask(pointsArrayIndex, eyes);
        }
        async Task StartCalibrationTaskUsingPanelSettings()
        {
            int pointsArrayIndex = GetPointsSelection();
            int eyes = GetEyesSelection();
            await StartCalibrationTask(pointsArrayIndex, eyes);
        }

        async Task StartCalibrationTask(int pointsArrayIndex, int eyes)
        {
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

                await FadeText.ShowMessage("Estos son tus ojos.", .5, 2);
                await ShowEyeDisplay();
                await FadeText.ShowMessage("Mira los puntos para hacerlos desaparecer.", .5, 2);          
                await CalibrationTask(pointsArrayIndex, eyes);
            }
            catch
            {

            }
            finally
            {
                ResetPage();
                isCalibrating = false;
                GlobalEvents.OnCalibrationFinished.Invoke();
                Debug.WriteLine("Calibration Finished!");
            }
        }

        async Task ShowEyeDisplay()
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
                    await Task.Delay(16);
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


        async Task CalibrationTask(int pointsArrayIndex, int eyes = 0)
        {
            Debug.WriteLine("Starting calibration.");
            Vector2[] points = GetCalibrationPointsArray();
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
                await Task.Delay(TimeSpan.FromSeconds(2));

                _7i_point2d_t point = new _7i_point2d_t();
                point.x = points[i].X;
                point.y = points[i].Y;
                result = ASeeTracker._7i_start_calibration_point(pointIndex, ref point,
                Marshal.GetFunctionPointerForDelegate(processCB), IntPtr.Zero,
                Marshal.GetFunctionPointerForDelegate(finishCB), IntPtr.Zero);
                Debug.WriteLine($"_7i_start_calibration_point: {pointIndex}, {result}");


                while (!pointComplete)
                {
                    await Task.Delay(16);
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
            float right_scroe = 0;
            ASeeTracker._7i_get_calibration_score(ref left_score, ref right_scroe);
            Console.WriteLine("_7i_complete_calibration: {0:G}, {1:G}", left_score, right_scroe);

            //Limpieza de gráficos
            DestroyCurrentCalibrationPoint();

            //OnSuccess
            if (0 == calibration_success)
            {
                Debug.WriteLine("Calibration success!");
                Disconnect();
                await FadeText.ShowMessage("¡La calibración fue exitosa!");
                Settings.I.LastEyesOption.Value = eyes;
                Settings.I.LastPointsOption.Value = pointsArrayIndex;
                Settings.I.LastCalibrationBuff.Value = coefficient.buf;
                Settings.I.SaveSettings();
                SaveCalibrationAsPreset(points.Length, eyes, coefficient.buf);
                GlobalEvents.OnCalibrationSuccess.Invoke();
                GazeManager.I?.GazeDevice?.Initialize();
            }
        }

        void SaveCalibrationAsPreset(int points, int eyesBias, byte[] buff)
        {
            //if (!Settings.I.SaveCalibrationAsPresetToggle.Value) return;
            Vector2 screenSize = Helper.GetScreenSize(this);
            string name = $"Preset_{screenSize.X}*{screenSize.Y}";
            EyesData eyes = new EyesData(eyesData);
            CalibrationPreset preset = new CalibrationPreset(name, screenSize, eyes,points, eyesBias, buff);
            CalibrationPresets.I.AddPreset(preset);
            FileOps.OpenDirectory(AppPaths.EyetrackerPath);
        }

        public void CancelCalibration()
        {
            if (!isCalibrating) return;
            ResetPage();
            Disconnect();
            GlobalEvents.OnCalibrationCancel.Invoke();
            GlobalEvents.OnCalibrationFinished.Invoke();
        }

        private void ResetPage()
        {
            Debug.WriteLine("Resetting Page");
            isCalibrating = false;
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
            Debug.WriteLine("Trying to destroy current calibration point." + (currentPoint != null));
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
            ASeeTracker._7i_cancel_calibration();
            ASeeTracker._7i_stop_tracking();
            ASeeTracker._7i_stop();
            ASeeTracker._7i_device_disconnect();
            GlobalEvents.OnEyetrackerDisconnected.Invoke();
        }

        void DisableButtons()
        {
            SettingsPanel.IsHitTestVisible = false;
            SettingsPanel.Visibility = Visibility.Collapsed;        
        }

        void EnableButtons()
        {
            SettingsPanel.IsHitTestVisible = true;
            SettingsPanel.Visibility = Visibility.Visible;     
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

        public Vector2[] GetCalibrationPointsArray()
        {
            int pointsSetting = GetPointsSelection();
            Vector2[] points = default;
            switch (pointsSetting)
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
