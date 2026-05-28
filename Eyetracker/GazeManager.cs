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

        public EyesData? Eyes => GazeDevice == null ? null : GazeDevice.Eyes;
        public Vector2 SmoothViewportPoint { get; private set; }
        public Vector2 SmoothScreenPoint { get; private set; }

        public System.Windows.Point SmoothScreenP { get; private set; }
        public System.Windows.Point SmoothWindowP { get; private set; }

        public bool ReceivedNewData { get; private set; }


        public GazeDeviceA11? joacoA11;
        public GazeDeviceIntelligaze intelligaze;
        public GazeDeviceTobiiInteraction tobiiInteraction;

        public bool mouseControlEnabled => Settings.I.MouseToggle.Value;
        public bool IsCalibrating { get; private set; }

        public GazeActivationDevice GazeActivationMode = GazeManager.GazeActivationDevice.Mouse;

        public enum GazeActivationDevice
        {
            Eyetracker,
            Mouse,
            All
        }


        public Dictionary<FrameworkElement, IGazeTarget> gazeTargets;
        public IGazeTarget CurrentGazeTarget { get; private set; }

        KalmanFilter kalmanFilter;
        InterpolationFilter interpolationFilter;
        float sampleRateSeconds;
        const float TIMEOUT_TRESHOLD = .5f;
        InputSimulator input;
        Task loopTask;
        Task getDeviceTask;
        CancellationTokenSource loopCts;
        CancellationTokenSource getDeviceCts;

        Stopwatch UIdeltaTimeWatch;
        Stopwatch timeOutTimer;

        TimeSpan UIdeltaTimeLastSample;
        TimeSpan UIdeltaTime;

        public bool IsThisAJoacoDevice(string deviceName)
        {
            return GazeDevice == null ? false : deviceName == GazeDevice.DeviceName;
        }

        public GazeManager()
        {
            I = this;
            timeOutTimer = new Stopwatch();
            UIdeltaTimeWatch = new Stopwatch();
            input = new InputSimulator();
            gazeTargets = new();
            kalmanFilter = new();
            interpolationFilter = new();
            LoadSettings();
            SubscribeToSettings();
            SubscribeToCalibrationEvents();

            AddSupportedDevices();
            StartGazeDeviceUpdateLoop();
            //getDeviceCts = new CancellationTokenSource();
            //getDeviceTask = Task.Run(() => FindGazeDeviceLoop(getDeviceCts.Token));
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

            //X64

            intelligaze = new GazeDeviceIntelligaze();
            supportedDevices.Add(intelligaze);

            tobiiInteraction = new GazeDeviceTobiiInteraction();
            supportedDevices.Add(tobiiInteraction);

            joacoA11 = new GazeDeviceA11();
            supportedDevices.Add(joacoA11);

            //X86

            //gazeDeviceTobii = new GazeDeviceTobiiEyeX();
            //supportedDevices.Add(gazeDeviceTobii);

        }

        void CancelLoop()
        {
            getDeviceCts.Cancel();
            loopCts.Cancel();
        }

        public void StartGazeDeviceUpdateLoop()
        {
            if (loopTask != null && !loopTask.IsCompleted) return;

            UIdeltaTimeWatch.Restart();
            UIdeltaTimeLastSample = new TimeSpan(0);
            UIdeltaTime = new TimeSpan(0);
            getDeviceCts = new CancellationTokenSource();
            getDeviceTask = Task.Run(() => FindGazeDeviceLoop(getDeviceCts.Token));
            loopCts = new CancellationTokenSource();
            loopTask = Task.Run(() => UpdateLoop(loopCts.Token));
        }

        public void StopGazeDeviceUpdateLoop()
        {
            getDeviceCts.Cancel();
            getDeviceTask = null;
            loopCts.Cancel();
            loopTask = null;
            DisconnectDevice();
        }

        public void RestartGazeDevice()
        {
            DisconnectDevice();
            StartGazeDeviceUpdateLoop();
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
                if (GazeDevice != null && !GazeDevice.IsConnected)
                {
                    DisconnectDevice();
                    continue;
                }

                if (GazeDevice != null)
                {
                    GazeDevice.UpdateData();
                    GetNewGazePointIfValid();
                    UpdateTimeoutTimer();
                    UpdateMousePosition();
                }

                UpdateGazeTargetsAndUI();

                await Task.Delay(TimeSpan.FromSeconds(sampleRateSeconds), token);
            }
        }

        async Task FindGazeDeviceLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                if (GazeDevice == null)
                {
                    TryInitializeGazeDevice();
                }
                await Task.Delay(500, token);
            }
        }

        private void UpdateGazeTargetsAndUI()
        {
            if (App.Instance == null) return;
            OnGazeUpdate?.Invoke();
            UpdateGazeTargets();
            UpdateUI();
        }

        bool updateUIqueue = false;
        void UpdateUI()
        {
            if (updateUIqueue) return;
            updateUIqueue = true;
            App.Current.Dispatcher.BeginInvoke(
                DispatcherPriority.Render,
                new Action(() =>
                {
                    updateUIqueue = false;
                    OnGazeUpdateUI?.Invoke();
                    UpdateGazeTargetVisuals();
                }));
        }

        bool updateUIqueue2 = false;
        void UpdateGazeTargets()
        {
            if (updateUIqueue2) return;
            updateUIqueue2 = true;
            App.Current.Dispatcher.BeginInvoke(
                DispatcherPriority.Input,
                new Action(() =>
                {
                    updateUIqueue2 = false;
                    UpdateGazeTargetsForAllWindows();
                }));
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

            if (newPoint.viewportPoint.X == 0 && newPoint.viewportPoint.Y == 0)
            {
                ReceivedNewData = false;
                return;
            }
            else
            {
                ReceivedNewData = true;
            }

                //Debug.WriteLine("GM:" + GazePoint.viewportPoint);
                SmoothViewportPoint = kalmanFilter.GetFilteredPoint(GazePoint.viewportPoint);
            SmoothViewportPoint = interpolationFilter.GetFilteredPoint(SmoothViewportPoint);
            SmoothScreenPoint = Helper.ViewportToScreenVector2(SmoothViewportPoint);
            SmoothScreenP = Helper.ViewportToScreenPoint(SmoothViewportPoint);
        }

        void UpdateUIDeltaTime()
        {
            TimeSpan now = UIdeltaTimeWatch.Elapsed;
            UIdeltaTime = now - UIdeltaTimeLastSample;
            UIdeltaTimeLastSample = now;
            if (UIdeltaTime > TimeSpan.FromMilliseconds(100))
            {
                UIdeltaTime = TimeSpan.FromMilliseconds(100);
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
            kalmanFilter.ResetStartingValues();
            DisconnectDevice();
            Debug.WriteLine("Trying to initialize device.");


            //El orden de la lista determina el orden de chequeo de dispositivos.
            //El primer dispositivo que sea capaz de inicializar va ser asignado como GazeDevice.
            foreach (IGazeDevice device in supportedDevices)
            {
                Debug.WriteLine("Trying to initialize: " + device.DeviceName);
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
            if (UserNotPresentTimeOut) return;

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

        IGazeTarget target;
        IGazeTarget _previous;
        readonly HashSet<IGazeTarget> activeGazeObjects = new();
        void UpdateGazeTargetsForAllWindows()
        {
            foreach (Window w in WindowManager.Windows)
            {
                //Debug.WriteLine($"Updating window: {w.Name} Delta time: {UIdeltaTime.TotalSeconds}");
                if (w == null) continue;

                //Activación solo por mouse.
                System.Drawing.Point screenPos = System.Windows.Forms.Control.MousePosition;
                UpdateGazeTargetsForWindow(new System.Windows.Point(screenPos.X, screenPos.Y), w);
                //UpdateGazeTargetsForWindow(SmoothScreenP, w);

                //MODOS DE ACTIVACION (WIP)

                //switch (GazeActivationMode)
                //{
                //    case GazeActivationDevice.Mouse:
                //        System.Drawing.Point screenPos = System.Windows.Forms.Control.MousePosition;
                //        UpdateGazeTargetsForWindow(new System.Windows.Point(screenPos.X, screenPos.Y), w);
                //        break;
                //    case GazeActivationDevice.Eyetracker:
                //        UpdateGazeTargetsForWindow(SmoothScreenP, w);
                //        break;
                //    case GazeActivationDevice.All:
                //        System.Drawing.Point screenPos2 = System.Windows.Forms.Control.MousePosition;
                //        UpdateGazeTargetsForWindow(new System.Windows.Point(screenPos2.X, screenPos2.Y), w);
                //        UpdateGazeTargetsForWindow(SmoothScreenP, w);

                //        break;
                //}
            }
        }

        IGazeTarget GetTarget(System.Windows.Point screen)
        {
            IGazeTarget target = null;
            foreach (Window window in WindowManager.Windows)
            {
                //Debug.WriteLine($"Updating window: {w.Name} Delta time: {UIdeltaTime.TotalSeconds}");
                if (window == null) continue;
                System.Windows.Point windowPoint = window.PointFromScreen(screen);
                target = GazeHitTest.FindGazeTarget(window, windowPoint);
                if (target != null) return target;
            }
            return target;
        }

        public void UpdateGazeTargetsForWindow(System.Windows.Point screen, Window window)
        {
            target = GetTarget(screen);

            foreach (var t in activeGazeObjects)
            {
                if (t == target)
                {
                    if (t.HasGaze) continue;
                    t.SetHasGaze(true);
                }
                else
                {
                    if (!t.HasGaze) continue;
                    t.SetHasGaze(false);
                }
            }

            if (target != null)
            {
                target.SetHasGaze(true);
                if (!activeGazeObjects.Contains(target))
                {
                    activeGazeObjects.Add(target);
                }
            }


            //CLEANUP
            foreach (var t in activeGazeObjects)
            {
                if (!t.HasGaze && t is GazeControl gc && gc.IsFullyInactive)
                {
                    Debug.WriteLine("Gaze button fully inactive. Removing from list!");
                    activeGazeObjects.Remove(t);
                }
            }
        }

        void UpdateGazeTargetVisuals()
        {
            UpdateUIDeltaTime();

            foreach (var t in activeGazeObjects)
            {
                t.OnGazeUpdateInternal(UIdeltaTime.TotalSeconds);
            }
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


public static class GazeHitTest
{
    public static IGazeTarget FindGazeTarget(Window window, System.Windows.Point point)
    {
        IGazeTarget found = null;

        HitTestResultCallback resultCallback = result =>
        {
            //Debug.WriteLine("VisualHit: " + result.VisualHit.GetType().Name);

            if (result.VisualHit is DependencyObject d)
            {
                DependencyObject current = d;

                while (current != null)
                {
                    if (current is IGazeTarget gaze)
                    {
                        found = gaze;
                        return HitTestResultBehavior.Stop;
                    }

                    current = VisualTreeHelper.GetParent(current);
                }
            }

            return HitTestResultBehavior.Continue;
        };

        VisualTreeHelper.HitTest(window, null, resultCallback, new PointHitTestParameters(point));
        //Debug.WriteLine(found == null ? "NULL TARGET" : "TARGET: " + found.ToString());

        return found;
    }

    //public static IGazeTarget FindGazeTarget(Window window, System.Windows.Point windowPoint)
    //{
    //    IInputElement hit = window.InputHitTest(windowPoint);
    //    DependencyObject current = hit as DependencyObject;

    //    while (current != null)
    //    {
    //        if (current is IGazeTarget gaze /*&& gaze.IsGazeEnabled*/)
    //        {
    //            return gaze;
    //        }
    //        current = VisualTreeHelper.GetParent(current);
    //    }

    //    return null;
    //}
}
