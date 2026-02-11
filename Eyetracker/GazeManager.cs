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
using System.Windows;
using GazeStream.Eyetracker;
using System.Windows.Controls;
using System.Windows.Media;

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
        public GazePoint RawGazePoint { get; private set; }

        public Vector2 SmoothViewportPoint { get; private set; }
        public Vector2 SmoothScreenPoint { get; private set; }

        public System.Windows.Point SmoothScreenP { get; private set; }
        public System.Windows.Point SmoothWindowP { get; private set; }


        public GazeDeviceA11? joacoA11;

        public bool mouseControlEnabled => Settings.I.MouseToggle.Value;
        public bool IsCalibrating { get; private set; }



        public Dictionary<FrameworkElement, IGazeTarget> gazeTargets;
        public IGazeTarget CurrentGazeTarget { get; private set; }

        KalmanFilter kalmanFilter;
        InterpolationFilter interpolationFilter;
        float sampleRateSeconds;
        const float TIMEOUT_TRESHOLD = 1f;
        InputSimulator input;
        Task loopTask;
        CancellationTokenSource loopCts;

        Stopwatch deltaTimeWatch;
        Stopwatch timeOutTimer;

        TimeSpan lastTimeSample;
        TimeSpan deltaTime;

        public bool IsThisAJoacoDevice(string deviceName)
        {
            return GazeDevice == null ? false : deviceName == GazeDevice.DeviceName;
        }

        public GazeManager()
        {
            I = this;
            timeOutTimer = new Stopwatch();
            deltaTimeWatch = new Stopwatch();
            input = new InputSimulator();
            gazeTargets = new();
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
            GlobalEvents.OnCalibrationStart.Add(CancelLoop);
            GlobalEvents.OnCalibrationCancel.Add(StartGazeDeviceUpdateLoop);
            GlobalEvents.OnCalibrationFinished.Add(StartGazeDeviceUpdateLoop);
        }

        void SetIsCalibratingTrue()
        {
            StartGazeDeviceUpdateLoop();
        }

        void SetIsCalibratingFalse()
        {
            CancelLoop();
        }

        private void AddSupportedDevices()
        {
            supportedDevices = new List<IGazeDevice>();
            joacoA11 = new GazeDeviceA11();
            supportedDevices.Add(joacoA11);
        }

        void CancelLoop()
        {
            loopCts.Cancel();
        }

        void StartGazeDeviceUpdateLoop()
        {
            if (loopTask != null && !loopTask.IsCompleted) return;

            deltaTimeWatch.Restart();
            lastTimeSample = new TimeSpan(0);
            deltaTime = new TimeSpan(0);
            loopCts = new CancellationTokenSource();
            loopTask = Task.Run(() => UpdateLoop(loopCts.Token));
        }
        async Task UpdateLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                if (IsCalibrating)
                {
                    Debug.WriteLine("IsCalibrating...");
                    continue;
                }
                if (GazeDevice == null || !GazeDevice.IsConnected)
                {
                    TryInitializeGazeDevice();
                    await Task.Delay(500, token);
                    if (GazeDevice == null) continue;
                }
                UpdateDeltaTime();
                GazeDevice.UpdateData();
                GetNewGazePointIfValid();
                UpdateTimeoutTimer();
                UpdateMousePosition();
                UpdateGazeTargetsForWindow(SmoothScreenP, deltaTime.TotalSeconds, App.Instance.OverlayWindow);
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

            RawGazePoint = GazeDevice.RawGazePoint;
            GazePoint newPoint = GazeDevice.GazePoint;
            if (newPoint.IsValid)
            {
                GazePoint = newPoint;
            }

            //Debug.WriteLine("GM:" + GazePoint.viewportPoint);
            SmoothViewportPoint = kalmanFilter.GetFilteredPoint(GazePoint.viewportPoint);
            SmoothViewportPoint = interpolationFilter.GetFilteredPoint(SmoothViewportPoint);
            SmoothScreenPoint = Helper.ViewportToScreenPoint(SmoothViewportPoint);
            SmoothScreenP = Helper.ViewportToScreen(SmoothViewportPoint);
        }

        void UpdateDeltaTime()
        {
            TimeSpan now = deltaTimeWatch.Elapsed;
            deltaTime = now - lastTimeSample;
            lastTimeSample = now;
            if (deltaTime > TimeSpan.FromMilliseconds(100))
            {
                deltaTime = TimeSpan.FromMilliseconds(100);
            }
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

        public void RegisterGazeTarget(FrameworkElement element, IGazeTarget target)
        { 
            gazeTargets.Add(element, target);     
        }

        public void UnregisterGazeTarget(FrameworkElement element, IGazeTarget target)
        {        
            gazeTargets.Remove(element);
        }

        IGazeTarget _current;
        IGazeTarget _previous;
        public void UpdateGazeTargetsForWindow(System.Windows.Point screen, double deltaTime, Window window)
        {
            System.Windows.Point windowPoint = window.PointFromScreen(screen);

            _current = GazeHitTest.FindGazeTarget(window, windowPoint);

            if (_current != _previous)
            {
                _previous?.OnGazeExit();
                _current?.OnGazeEnter();
            }

            _current?.OnGazeUpdate(deltaTime);

            _previous = _current;
        }

        public void Clear()
        {
            CurrentGazeTarget = null;
            gazeTargets.Clear();
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

//MUDAR TODO ESTO A SU PROPIA CLASE PARA LA PARTE DE INTERACCION
public static class GazeTargetTag
{
    public static readonly DependencyProperty IsTargetProperty =
        DependencyProperty.RegisterAttached(
            "IsGazeInteractable",
            typeof(bool),
            typeof(GazeTargetTag),
            new PropertyMetadata(false));

    public static bool GetIsTarget(DependencyObject obj)
        => (bool)obj.GetValue(IsTargetProperty);

    public static void SetIsTarget(DependencyObject obj, bool value)
        => obj.SetValue(IsTargetProperty, value);
}

public interface IGazeTarget
{
    void OnGazeEnter();
    void OnGazeExit();
    void OnGazeUpdate(double deltaTime);

    void OnFocus();
    void OnUnfocus();

    double StartActivationTime { get; }
    double ActivationDuration { get; }
    double PartialActivationStayTime { get; }

    bool IsGazeEnabled { get; }
}

public static class GazeHitTest
{
    public static IGazeTarget FindGazeTarget(Window window, System.Windows.Point windowPoint)
    {
        IInputElement hit = window.InputHitTest(windowPoint);
        DependencyObject current = hit as DependencyObject;

        while (current != null)
        {
            if (current is IGazeTarget gaze && gaze.IsGazeEnabled)
                return gaze;

            current = VisualTreeHelper.GetParent(current);
        }

        return null;
    }
}
