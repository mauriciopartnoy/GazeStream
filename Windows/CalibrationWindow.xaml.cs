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

namespace GazeStream.Windows
{
    /// <summary>
    /// Interaction logic for CalibrationWindow.xaml
    /// </summary>
    public partial class CalibrationWindow : Window
    {
        public CalibrationWindow()
        {
            InitializeComponent();
        }

        Task? calibrationTask;
        bool isCalibrating;
        static bool pointComplete;
        List<CalibrationPointGraphic> points = new List<CalibrationPointGraphic>();
        static CalibrationPointGraphic? currentPoint;
        public static _7i_eye_data_ex_t eyesData;


        public static int CalibrationPointProgress { get; private set; }
        public static float CalibrationPointProgress01 { get; private set; }

        //User values

        //System Settings



        public static ASeeTracker.processCallback processCB = new ASeeTracker.processCallback(process_callback);
        public static ASeeTracker.finishCallback finishCB = new ASeeTracker.finishCallback(finish_callback);
        public static ASeeTracker.gazeCallback gazeCB = new ASeeTracker.gazeCallback(OnGazeCallback);
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

        public CalibrationPage()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            Debug.WriteLine("Landed on Calibration Page. Loading RadioButton settings.");
            base.OnNavigatedTo(e);
            ResetPage();
            LoadToggleOptions();
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            Debug.WriteLine("Exiting Calibration Page. Saving page settings.");
            base.OnNavigatedFrom(e);
        }

        void LoadToggleOptions()
        {
            SetEyesSelectionRadioButton(SaveManager.GetSystemSetting(SaveKeys.LAST_EYES_SETTING_KEY, 0));
            SetPointsSettingRadioButton(SaveManager.GetSystemSetting(SaveKeys.LAST_POINTS_SETTING_KEY, 2));
            SetFilterSelectionRadioButton(SaveManager.GetSystemSetting(SaveKeys.LAST_FILTER_SETTING_KEY, 2));
        }

        void SaveToggleOptions()
        {
            SaveManager.SetSystemSetting<int>(SaveKeys.LAST_EYES_SETTING_KEY, GetEyesSelection());
            SaveManager.SetSystemSetting<int>(SaveKeys.LAST_POINTS_SETTING_KEY, GetPointsSelection());
            SaveManager.SetSystemSetting<int>(SaveKeys.LAST_FILTER_SETTING_KEY, GetFilterSelection());
            SaveManager.SaveSystemSettings();
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
            //Stop calibration
            SaveToggleOptions();
            PageManager.Back();
            Debug.WriteLine("Back pressed");
        }

        public void StartCalibration_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("Starting calibration");
            if (isCalibrating) return;
            calibrationTask = StartCalibrationTask();
            Debug.WriteLine("Starting calibration");
        }

        async Task StartCalibrationTask()
        {
            if (isCalibrating) return;
            isCalibrating = true;

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

                //Calibration
                Vector2[] points = GetCalibrationPointsArray();
                int eyes = GetEyesSelection();
                await CalibrationTask(points, eyes);
            }
            catch
            {

            }
            finally
            {
                //Cleanup
                ResetPage();
                isCalibrating = false;
                Debug.WriteLine("Calibration Finished!");
            }
        }

        async Task ShowEyeDisplay()
        {
            EyeDisplay.Visibility = Visibility.Visible;
            bool buttonPressed = false;
            if (App.Instance.CurrentWindow != null)
            {
                App.Instance.CurrentWindow.Content.KeyDown += OnKeyDown;
                App.Instance.CurrentWindow.Content.Tapped += OnTap;
            }
            while (!buttonPressed)
            {
                App.Instance.Dispatcher.TryEnqueue(() =>
                {
                    EyeDisplay.UpdateEyeDisplay(eyesData);
                });

                await Task.Delay(16);
            }
            EyeDisplay.Visibility = Visibility.Collapsed;
            if (App.Instance.CurrentWindow != null)
            {
                App.Instance.CurrentWindow.Content.KeyDown -= OnKeyDown;
                App.Instance.CurrentWindow.Content.Tapped -= OnTap;
            }

            void OnKeyDown(object sender, KeyRoutedEventArgs args)
            {
                buttonPressed = true;
            }

            void OnTap(object sender, TappedRoutedEventArgs args)
            {
                buttonPressed = true;
            }
        }

        public bool TryConnectEyetrackerDevice()
        {
            //GazeManager.I.DisconnectDevice();
            //Disconnect();

            int startResult = ASeeTracker._7i_start(AppPaths.EyetrackerConfigPath);
            ASeeTracker._7i_set_gaze_callback(Marshal.GetFunctionPointerForDelegate(gazeCB), IntPtr.Zero);

            if (startResult == 0)
            {
                ErrorText.Text = "";
                Debug.WriteLine("Joaco Device connected.");
                return true;
            }
            else
            {
                ErrorText.Text = "¡Algo salió mal! No fue posible iniciar el dispositivo Joaco.";
                Debug.WriteLine("Couldn't connect to Joaco Device. Are you using the correct SDK?");
                return false;
            }
        }

        async Task CalibrationTask(Vector2[] points, int eyes = 0)
        {
            isCalibrating = true;
            Debug.WriteLine("Starting proper calibration.");
            //Acá se va a guardar el resultado de la calibración. Podemos guardar el coefficient.buf como un byte[] para usar mas tarde.
            _7i_coefficient_t coefficient = new _7i_coefficient_t();
            int pointIndex = 1;
            int pointCount = points.Length;


            //Seteamos el modo antes de iniciar. 0: ambos ojos, 1: ojo izq, 2: ojo der.
            int setModeResult = ASeeTracker._7i_set_calibration_mode(eyes);
            Debug.WriteLine("Set Mode result: " + setModeResult);


            //Inicio de calibración
            int result = ASeeTracker._7i_start_calibration(pointCount);

            for (int i = 0; isCalibrating && i < pointCount; i++)
            {
                pointComplete = false;

                /*
                 Primero mostramos el gráfico. Su posición en el eje Y está invertido ya que el sistema de coordenadas
                 del Viewport de Unity tiene el origen (0,0) ABAJO-IZQ vs Invensun que lo tiene ARRIBA-IZQ.
                 El usuario tiene 2 segundos para mirar hacia el punto antes de que inicie la calibración del mismo.
                 */
                SpawnCalibrationPoint(new Vector2(points[i].X, points[i].Y));
                await Task.Delay(TimeSpan.FromSeconds(2));

                //Start Point
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

                //Chequeo para ver si podemos inicializar así por problemas de serialización del save manager.
                if (GazeManager.I != null && GazeManager.I.joacoA11 != null)
                {
                    GazeManager.I.joacoA11.buff = coefficient.buf;
                }
                SaveToggleOptions();
                SaveUserCalibration(coefficient.buf);
                GlobalEvents.OnCalibrationSuccess.Invoke(coefficient.buf);
                GazeManager.I?.GazeDevice?.Initialize();
            }

            //Cleanup
            isCalibrating = false;
        }

        void SaveUserCalibration(byte[] calibrationBuff)
        {
            SaveManager.SetValue<byte[]>(SaveKeys.LAST_USER_CALIBRATION_KEY, calibrationBuff);
            SaveManager.SaveGame();
        }

        public void CancelCalibration()
        {
            if (!isCalibrating) return;
            ResetPage();
            Disconnect();

        }

        private void ResetPage()
        {
            Debug.WriteLine("Resetting Page");
            isCalibrating = false;
            DestroyCurrentCalibrationPoint();
            EyeDisplay.Visibility = Visibility.Collapsed;
            EnableButtons();
            //WindowsHelper.ShowCursor(true);

            FadeText.HideMessage();
        }




        public void SpawnCalibrationPoint(Vector2 viewportPos)
        {
            DestroyCurrentCalibrationPoint();
            CalibrationPointGraphic newPoint = new CalibrationPointGraphic(20, Windows.UI.Color.FromArgb(255, 0, 255, 0));
            currentPoint = newPoint;
            MainCanvas.Children.Add(currentPoint.Element);
            MainCanvas.SetElementToViewportPositionActualSize(currentPoint.Element, viewportPos);
            points.Add(currentPoint);

            //TODO: Animation
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
            ASeeTracker._7i_cancel_calibration();
            ASeeTracker._7i_stop_tracking();
            ASeeTracker._7i_stop();
            ASeeTracker._7i_device_disconnect();
        }

        void DisableButtons()
        {
            //Agregar animaciones
            SettingsPanel.IsHitTestVisible = false;
            SettingsPanel.Visibility = Visibility.Collapsed;
            MainWindow.Instance?.HideOverlay();
            //WindowsHelper.ShowCursor(false);

        }

        void EnableButtons()
        {
            SettingsPanel.IsHitTestVisible = true;
            SettingsPanel.Visibility = Visibility.Visible;
            MainWindow.Instance?.ShowOverlay();
            //WindowsHelper.ShowCursor(true);
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

        public int GetFilterValueFromSetting()
        {
            int filterSetting = GetFilterSelection();
            int filterValue = 10;
            switch (filterSetting)
            {
                case 0: filterValue = 3; break;
                case 1: filterValue = 6; break;
                case 2: filterValue = 10; break;
            }
            return filterValue;
        }

        public void SetFilterSelectionRadioButton(int filterOption)
        {
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
