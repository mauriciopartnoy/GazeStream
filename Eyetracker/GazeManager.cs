using System.Collections.Generic;
using System.Numerics;
using System;
using System.Diagnostics;
using GazeStream.Utilities;
using GazeStream.Utilities.Save;
using GazeStream.AppData;
using System.Windows.Threading;
using GazeStream.Eyetracker.Filters;
namespace GazeStream.Eyetracker
{
    public class GazeManager
    {
        public static GazeManager? I { get; private set; }
        public static event Action<IGazeDevice>? OnGazeDeviceChanged;
        public static event Action? OnGazeUpdate;
        public static event Action? OnGazeUpdateUI;

        KalmanFilter kalmanFilter;
        InterpolationFilter interpolationFilter;

        const string SAMPLE_FREQUENCY_KEY = "SampleFrequency";

        public int FrequencyHz { get; private set; } = 60;
        float frequencySeconds;

        List<IGazeDevice> supportedDevices = new List<IGazeDevice>();
        public IGazeDevice? GazeDevice { get; private set; }
        public bool IsConnected => GazeDevice != null && GazeDevice.IsConnected;
        public bool IsUserPresent => GazeDevice != null && GazeDevice.UserIsPresent;
        public bool UserNotPresentTimeOut => timeOutTimer.Elapsed.TotalSeconds > TIMEOUT_TRESHOLD;
        public bool HasValidPoint => IsConnected && GazePoint.IsValid;

        public bool IsJoacoDevice => GazeDevice == null ? false : IsThisAJoacoDevice(GazeDevice.DeviceName);
        public GazePoint GazePoint { get; private set; }
        public Vector2 SmoothViewportPoint { get; private set; }
        public Vector2 SmoothScreenPoint { get; private set; }

        public GazeDeviceA11? joacoA11;

        const float TIMEOUT_TRESHOLD = 1f;
        Stopwatch timeOutTimer;
        Vector2 historicPoint;
        bool hasHistoricPoint;

        public bool IsThisAJoacoDevice(string deviceName)
        {
            return GazeDevice == null ? false : deviceName == GazeDevice.DeviceName;
        }

        public GazeManager()
        {
            I = this;
            timeOutTimer = new Stopwatch();
            kalmanFilter = new();
            interpolationFilter = new();
            LoadSettings();
            AddSupportedDevices();
            TryInitializeGazeDevice();
            StartUpdateLoop();
        }

        void LoadSettings()
        {
            FilterSmoothingFactor = SaveManager.GetSystemSetting(SaveKeys.SMOOTH_INTERPOLATION, .5f);
            FrequencyHz = SaveManager.GetSystemSetting(SAMPLE_FREQUENCY_KEY, 80);
            frequencySeconds = 1f / FrequencyHz;
        }

        public void SetPostProcessSmoothing(float value01)
        {
            FilterSmoothingFactor = Math.Clamp(value01, 0.1f, 1f);
            SaveManager.SetSystemSetting(SaveKeys.SMOOTH_INTERPOLATION, value01);
        }
        public void SetSampleFrequency(int frequencyHz)
        {
            frequencyHz = Math.Clamp(frequencyHz, 0, 150);
            FrequencyHz = frequencyHz;
            frequencySeconds = 1f / FrequencyHz;
            SaveManager.SetSystemSetting(SAMPLE_FREQUENCY_KEY, frequencyHz);
        }

        private void AddSupportedDevices()
        {
            supportedDevices = new List<IGazeDevice>();
            joacoA11 = new GazeDeviceA11();
            supportedDevices.Add(joacoA11);
            //Mouse...? Touch?
        }

        private void StartUpdateLoop()
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await UpdateLoop();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"UpdateLoop failed: {ex}");
                }
            });
        }
        async Task UpdateLoop()
        {
            while (true)
            {
                if (GazeDevice == null) return;
                GazeDevice.UpdateData();
                GetNewGazePointIfValid();
                UpdateTimeoutTimer();

                if (!GazeDevice.IsConnected)
                {
                    //Debug.WriteLine("Last device was disconnected. Trying to initialize another device.");
                    TryInitializeGazeDevice();
                    if (GazeDevice == null) return;
                }

                SendUpdateEvents();
                await Task.Delay(TimeSpan.FromSeconds(frequencySeconds));
            }
        }

        private static void SendUpdateEvents()
        {
            if (App.Instance == null) return;
            OnGazeUpdate?.Invoke();
            App.Instance.Dispatcher.BeginInvoke(
         DispatcherPriority.Render,
         new Action(() =>
         {
             OnGazeUpdateUI?.Invoke();
         })
     );
        }

        void GetNewGazePointIfValid()
        {
            if (GazeDevice == null) return;

            GazePoint newPoint = GazeDevice.GazePoint;
            if (newPoint.IsValid)
            {
                GazePoint = newPoint;
            }

            SmoothViewportPoint = Smoothify(GazePoint.viewportPoint);
            SmoothScreenPoint = Helper.ViewportToScreenPoint(SmoothViewportPoint);
            //Debug.WriteLine("Viewport: " + GazePoint.viewportPoint + " Screen: " + SmoothScreenPoint);
        }

        void UpdateTimeoutTimer()
        {
            if (IsUserPresent && timeOutTimer.IsRunning)
            {
                timeOutTimer.Reset();
            }

            if (!IsUserPresent && !timeOutTimer.IsRunning)
            {
                timeOutTimer.Restart();
            }
        }

        public void TryInitializeGazeDevice()
        {
            DisconnectDevice();
            Debug.WriteLine("Trying to initialize device.");


            //El orden de la lista determina el orden de chequeo de dispositivos.
            //El primer dispositivo que sea capaz de inicializar va ser asignado como GazeDevice.
            foreach (IGazeDevice device in supportedDevices)
            {
                if (device.Initialize())
                {
                    GazeDevice = device;
                    OnGazeDeviceChanged?.Invoke(GazeDevice);
                    Debug.WriteLine(GazeDevice.DeviceName + " was initialized.");
                    return;
                }
            }

            Debug.WriteLine("There is no usable device.");
        }

        public void DisconnectDevice()
        {
            if (GazeDevice != null)
            {
                GazeDevice.Disconnect();
            }

            GazeDevice = null;
        }

        private Vector2 Smoothify(Vector2 point)
        {
            if (!hasHistoricPoint)
            {
                historicPoint = point;
                hasHistoricPoint = true;
            }
            FilterSmoothingFactor = Math.Clamp(FilterSmoothingFactor, 0.1f, 1f);
            Vector2 result = Helper.Lerp(historicPoint, point, FilterSmoothingFactor);
            historicPoint = result;
            return result;
        }

    }
}
