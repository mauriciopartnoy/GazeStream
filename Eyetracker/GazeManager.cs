using System.Collections.Generic;
using System.Numerics;
using System;
using System.Diagnostics;
using GazeStream.Utilities;
using GazeStream.Utilities.Save;
using GazeStream.Utilities.Events;
using GazeStream.AppData;
using System.Windows.Threading;
using GazeStream.Eyetracker.Filters;
using InputSimulatorEx;
using InputSimulatorEx.Native;

namespace GazeStream.Eyetracker
{
    public class GazeManager : ISettingsUser, IDisposable
    {
        public static GazeManager? I { get; private set; }
        public static event Action<IGazeDevice>? OnGazeDeviceChanged;
        public static event Action? OnGazeUpdate;
        public static event Action? OnGazeUpdateUI;

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

        public bool mouseControlEnabled => Settings.I.MouseToggle.Value;
        public bool IsCalibrating { get; private set; }

        KalmanFilter kalmanFilter;
        InterpolationFilter interpolationFilter;
        float sampleRateSeconds;
        const float TIMEOUT_TRESHOLD = 1f;
        Stopwatch timeOutTimer;
        InputSimulator input;

        Task loopTask;
        CancellationTokenSource loopCts;

        public bool IsThisAJoacoDevice(string deviceName)
        {
            return GazeDevice == null ? false : deviceName == GazeDevice.DeviceName;
        }

        public GazeManager()
        {
            I = this;
            timeOutTimer = new Stopwatch();
            input = new InputSimulator();
            kalmanFilter = new();
            interpolationFilter = new();
            LoadSettings();
            SubscribeToSettings();
            SubscribeToCalibrationEvents();

            AddSupportedDevices();
            StartGazeDeviceUpdateLoop();
        }

        public void LoadSettings()
        {
            UpdateInterpolationFilter(Settings.I.InterpolationFilter.Value);
            UpdateKalmanFilter(Settings.I.KalmanFilter.Value);
            UpdateSampleRateHZ(Settings.I.SampleRateHZ.Value);
        }

        public void SubscribeToSettings()
        {
            Settings.I.InterpolationFilter.OnValueChanged += UpdateInterpolationFilter;
            Settings.I.KalmanFilter.OnValueChanged += UpdateKalmanFilter;
            Settings.I.SampleRateHZ.OnValueChanged += UpdateSampleRateHZ;
        }

        public void UpdateKalmanFilter(int value)
        {
            kalmanFilter.SetSmoothFactor(value);
        }
        public void UpdateInterpolationFilter(float value01)
        {
            interpolationFilter.Value = value01;
        }
        public void UpdateSampleRateHZ(int frequencyHz)
        {
            sampleRateSeconds = 1f / frequencyHz;
        }

        void SubscribeToCalibrationEvents()
        {
            GlobalEvents.OnCalibrationStart.Add(SetIsCalibratingTrue);
            GlobalEvents.OnCalibrationCancel.Add(SetIsCalibratingFalse);
            GlobalEvents.OnCalibrationFinished.Add(SetIsCalibratingFalse);
        }

        void SetIsCalibratingTrue()
        {
            IsCalibrating = true;
        }

        void SetIsCalibratingFalse()
        {
            IsCalibrating = false;
        }

        private void AddSupportedDevices()
        {
            supportedDevices = new List<IGazeDevice>();
            joacoA11 = new GazeDeviceA11();
            supportedDevices.Add(joacoA11);
        }

        void StartGazeDeviceUpdateLoop()
        {
            if (loopTask != null && !loopTask.IsCompleted) return;

            loopCts = new CancellationTokenSource();
            loopTask = Task.Run(() => UpdateLoop(loopCts.Token));
        }
        async Task UpdateLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                if (IsCalibrating) return;
                if (GazeDevice == null || !GazeDevice.IsConnected)
                {
                    TryInitializeGazeDevice();
                    await Task.Delay(500, token);
                    if (GazeDevice == null) return;
                }
                GazeDevice.UpdateData();
                GetNewGazePointIfValid();
                UpdateTimeoutTimer();
                UpdateMousePosition();
                SendUpdateEvents();
                await Task.Delay(TimeSpan.FromSeconds(sampleRateSeconds), token);
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

            //Debug.WriteLine("GM:" + GazePoint.viewportPoint);
            SmoothViewportPoint = kalmanFilter.GetFilteredPoint(GazePoint.viewportPoint);
            SmoothViewportPoint = interpolationFilter.GetFilteredPoint(SmoothViewportPoint);
            SmoothScreenPoint = Helper.ViewportToScreenPoint(SmoothViewportPoint);
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

        public void SetMouseControl(bool enabled)
        {
            Settings.I.MouseToggle.Value = enabled;
        }

        public void ToggleMouseControl()
        {
            Settings.I.MouseToggle.Value = !Settings.I.MouseToggle.Value;
        }

        void UpdateMousePosition()
        {
            if (!mouseControlEnabled) return;
            if (input == null) return;
            if (!IsUserPresent) return;

            (double x, double y) pos = Helper.ViewportToMousePosition(GazeManager.I.SmoothViewportPoint);
            input.Mouse.MoveMouseToPositionOnVirtualDesktop(pos.x, pos.y);
        }

        public void Dispose()
        {
            if (loopCts == null) return;
            loopCts.Cancel();
            loopCts.Dispose();
            loopCts = null;
            DisconnectDevice();
        }
    }
}
