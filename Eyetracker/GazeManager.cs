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
using System.Threading.Tasks;

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
        public GazeDeviceSkyle skyle;
        public bool mouseControlEnabled => Settings.I.MouseToggle.Value;
        public bool IsCalibrating { get; private set; }

        public GazeActivationDevice GazeActivationMode = GazeManager.GazeActivationDevice.Mouse;

        public enum GazeActivationDevice
        {
            Eyetracker,
            Mouse,
            All
        }

        bool previousMouseControlToggle;
        bool previousGazeBubbleState;

        public Dictionary<FrameworkElement, IGazeTarget> gazeTargets;
        public IGazeTarget CurrentGazeTarget { get; private set; }

        KalmanFilter kalmanFilter;
        InterpolationFilter interpolationFilter;
        OutlierFilter outlierFilter;
        float sampleRateSeconds;
        const float TIMEOUT_TRESHOLD = .5f;
        InputSimulator input;
        Task? loopTask;
        Task? getDeviceTask;
        //Una sola cancelación para los dos bucles de la sesión actual.
        CancellationTokenSource? loopCts;

        Stopwatch UIdeltaTimeWatch;
        Stopwatch timeOutTimer;

        TimeSpan UIdeltaTimeLastSample;
        TimeSpan UIdeltaTime;

        public bool IsThisAJoacoDevice(string deviceName)
        {
            //Antes comparaba el nombre recibido con el del dispositivo activo, por lo que devolvía true para cualquier dispositivo.
            return joacoA11 != null && deviceName == joacoA11.DeviceName;
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
            outlierFilter = new();
            LoadSettings();
            SubscribeToSettings();
            SubscribeToCalibrationEvents();

            StartLoopWithSelectedDevice();
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
            Settings.I.EyetrackerDevice.OnChanged += StartLoopWithSelectedDevice;
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

        void StartLoopWithSelectedDevice()
        {
            StopGazeDeviceUpdateLoop();
            AddSupportedDevices();
            StartGazeDeviceUpdateLoop();
        }

        private void AddSupportedDevices()
        {
            supportedDevices = new List<IGazeDevice>();

            //X64

            switch (Settings.I.EyetrackerDevice.Value)
            {
                case Eyetracker_Device.Ninguno:
                    break;
                case Eyetracker_Device.Joaco:
                    if (joacoA11 == null) { joacoA11 = new GazeDeviceA11(); }
                    supportedDevices.Add(joacoA11);
                    break;
                case Eyetracker_Device.Intelligaze:
                    if (intelligaze == null) { intelligaze = new GazeDeviceIntelligaze(); }
                    supportedDevices.Add(intelligaze);
                    break;
                case Eyetracker_Device.Skyle:
                    if (skyle == null) { skyle = new GazeDeviceSkyle(); }
                    supportedDevices.Add(skyle);
                    break;
                case Eyetracker_Device.Otro:
                    if (tobiiInteraction == null) { tobiiInteraction = new GazeDeviceTobiiInteraction(); }
                    supportedDevices.Add(tobiiInteraction);
                    break;
            }

            //X86

            //gazeDeviceTobii = new GazeDeviceTobiiEyeX();
            //supportedDevices.Add(gazeDeviceTobii);

        }

        // ---- Ciclo de vida de los bucles y del dispositivo ----
        //
        // Hay dos bucles en el thread pool: FindGazeDeviceLoop (conecta el dispositivo) y UpdateLoop (lee datos).
        // Los dos comparten un único CancellationTokenSource por "sesión" de bucles.
        //
        // deviceGate serializa todo lo que toca el SDK nativo del dispositivo: Initialize, UpdateData y Disconnect.
        // Antes podían correr a la vez (por ejemplo, UpdateData en un hilo mientras otro desconectaba), y una
        // inicialización lenta de un bucle ya cancelado podía terminar dejando conectado un dispositivo viejo.

        readonly SemaphoreSlim deviceGate = new(1, 1);
        readonly object loopGate = new();
        bool disposed;

        //Al cerrar la app se espera a que los bucles terminen; una inicialización en curso (Skyle, Intelligaze) puede tardar varios segundos.
        static readonly TimeSpan LOOP_STOP_TIMEOUT = TimeSpan.FromSeconds(6);
        static readonly TimeSpan DEVICE_GATE_TIMEOUT_ON_EXIT = TimeSpan.FromSeconds(3);

        void CancelLoop()
        {
            lock (loopGate)
            {
                loopCts?.Cancel();
            }
        }

        public void StartGazeDeviceUpdateLoop()
        {
            lock (loopGate)
            {
                if (disposed) return;
                //Si hay una sesión activa no se crea otra. Si la anterior se canceló (por ejemplo al calibrar) se arranca
                //una nueva aunque la vieja todavía esté terminando: deviceGate y el token evitan que se pisen.
                if (loopCts != null && !loopCts.IsCancellationRequested) return;

                UIdeltaTimeWatch.Restart();
                UIdeltaTimeLastSample = new TimeSpan(0);
                UIdeltaTime = new TimeSpan(0);

                var cts = new CancellationTokenSource();
                loopCts = cts;
                getDeviceTask = Task.Run(() => FindGazeDeviceLoop(cts.Token));
                loopTask = Task.Run(() => UpdateLoop(cts.Token));
            }
        }

        public void StopGazeDeviceUpdateLoop()
        {
            CancelLoop();
            DisconnectDevice();
        }

        public void RestartGazeDevice()
        {
            DisconnectDevice();
            StartGazeDeviceUpdateLoop();
        }

        int automaticCalibrationCounter = 0;
        async Task UpdateLoop(CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    if (IsCalibrating || App.IsUpdating)
                    {
                        //Sin este delay el bucle giraba sin pausa y consumía un núcleo entero.
                        await Task.Delay(100, token);
                        continue;
                    }

                    await deviceGate.WaitAsync(token);
                    try
                    {
                        UpdateDeviceStep();
                    }
                    catch (Exception e)
                    {
                        //Antes una excepción terminaba el bucle en silencio y la mirada dejaba de enviarse para siempre.
                        Debug.WriteLine("GazeManager: error en el bucle de actualización. " + e);
                    }
                    finally
                    {
                        deviceGate.Release();
                    }

                    UpdateGazeTargetsAndUI();

                    await Task.Delay(TimeSpan.FromSeconds(sampleRateSeconds), token);
                }
            }
            catch (OperationCanceledException)
            {
                //Cancelación normal.
            }
        }

        /// <summary>Una iteración de lectura del dispositivo. Se llama con deviceGate tomado.</summary>
        void UpdateDeviceStep()
        {
            IGazeDevice? device = GazeDevice;
            if (device == null) return;

            if (!device.IsConnected)
            {
                Debug.WriteLine(device.DeviceName + " lost connection.");
                DisconnectDeviceCore(device);
                return;
            }

            device.UpdateData();
            GetNewGazePointIfValid();
            UpdateTimeoutTimer();
            UpdateMousePosition();
            UpdateCalibrationUsingLocalDatabase();
        }

        private void UpdateCalibrationUsingLocalDatabase()
        {
            if (Settings.I.DynamicCalibrationToggle.Value == false) return;
            if (IsJoacoDevice)
            {
                automaticCalibrationCounter++;
                if (automaticCalibrationCounter > 60)
                {
                    automaticCalibrationCounter = 0;
                    //Antes se calculaba el mejor preset pero se descartaba el resultado.
                    joacoA11?.ApplyBestPresetIfDifferent();
                }
            }
        }

        async Task FindGazeDeviceLoop(CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    if (GazeDevice == null)
                    {
                        try
                        {
                            await TryInitializeGazeDevice(token);
                        }
                        catch (OperationCanceledException)
                        {
                            throw;
                        }
                        catch (Exception e)
                        {
                            Debug.WriteLine("GazeManager: error buscando dispositivos. " + e);
                        }
                    }
                    await Task.Delay(500, token);
                }
            }
            catch (OperationCanceledException)
            {
                //Cancelación normal.
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

        public void UpdatePointer(double x, double y)
        {
            App.Current.Dispatcher.BeginInvoke(() =>
              {
                  App.Instance.OverlayWindow.UpdateBubblePosition(x, y);

              });
        }

        Vector2 targetPoint;

        void GetNewGazePointIfValid()
        {
            if (GazeDevice == null) return;

            RawGazePoint = GazeDevice.RawGazePoint;
            GazePoint newPoint = GazeDevice.GazePoint;

            if (newPoint.IsValid)
            {
                GazePoint = newPoint;
            }

            //if (newPoint.viewportPoint.X == 0 || newPoint.viewportPoint.Y == 0)
            //{
            //    ReceivedNewData = false;
            //    return;
            //}
            //else
            //{
            //    ReceivedNewData = true;
            //}

            //Debug.WriteLine("GM:" + GazePoint.viewportPoint);
            //Los filtros se aplican a todos los dispositivos. Antes dependían de IsJoacoDevice,
            //que por error era true para cualquier equipo; al corregirlo, Tobii e Intelligaze se habrían quedado sin suavizado.
            {
                targetPoint = GazePoint.viewportPoint;
                //Filter outlier points
                if (Settings.I.FilterOutlierPoints.Value == true)
                {
                    targetPoint = outlierFilter.Update(targetPoint);
                }
                //Kalman Filter Desactivado en favor de usar solo interpolación.

                //if (Settings.I.KalmanFilter.Value > 0)
                //{
                //    targetPoint = kalmanFilter.GetFilteredPoint(targetPoint);
                //}
                //Interpolation Filter

                if (Settings.I.InterpolationFilter.Value > 0)
                {
                    SmoothViewportPoint = Helper.Lerp(SmoothViewportPoint, targetPoint, Settings.I.InterpolationFilter.Value);
                }
                else
                {
                    SmoothViewportPoint = targetPoint;
                }
            }


            //TEST FILTERS
            if (Settings.I.ClampBubblePosition.Value == true)
            {
                SmoothViewportPoint = ApplyEdgeBias(SmoothViewportPoint);
                Vector2 padding = Helper.DipsToViewport01(Settings.I.ClampDistance.Value);
                SmoothViewportPoint = new Vector2(Math.Clamp(SmoothViewportPoint.X, 0f + padding.X, 1f - padding.X), Math.Clamp(SmoothViewportPoint.Y, 0f + padding.Y, 1f - padding.Y));
            }
            SmoothScreenPoint = Helper.ViewportToScreenVector2(SmoothViewportPoint);
            SmoothScreenP = Helper.ViewportToScreenPoint(SmoothViewportPoint);

            //SmoothViewportPoint = GazePoint.viewportPoint;
            //if (IsJoacoDevice)
            //{
            //    SmoothViewportPoint = outlierFilter.Update(SmoothViewportPoint);
            //    SmoothViewportPoint = kalmanFilter.GetFilteredPoint(SmoothViewportPoint);
            //    //SmoothViewportPoint = interpolationFilter.GetFilteredPoint(SmoothViewportPoint);
            //}


            ////TEST FILTERS
            //if (Settings.I.ClampBubblePosition.Value == true)
            //{
            //    SmoothViewportPoint = ApplyEdgeBias(SmoothViewportPoint);
            //    Vector2 padding = Helper.DipsToViewport01(Settings.I.ClampDistance.Value);
            //    SmoothViewportPoint = new Vector2(Math.Clamp(SmoothViewportPoint.X, 0f + padding.X, 1f - padding.X), Math.Clamp(SmoothViewportPoint.Y, 0f + padding.Y, 1f - padding.Y));
            //}
            //SmoothScreenPoint = Helper.ViewportToScreenVector2(SmoothViewportPoint);
            //SmoothScreenP = Helper.ViewportToScreenPoint(SmoothViewportPoint);
        }

        Vector2 ApplyEdgeBias(Vector2 coord)
        {
            Vector2 padding = Helper.DipsToViewport01(Settings.I.EdgeBiasFilter.Value);
            float x = coord.X;
            float y = coord.Y;
            if (coord.X < padding.X || coord.X > (1f - padding.X))
            {
                x = EdgeBias(coord.X, .03f);
            }
            if (coord.Y < padding.Y || coord.X > (1f - padding.Y))
            {
                y = EdgeBias(coord.Y, .03f);
            }
            return new Vector2(x, y);
        }

        float EdgeBias(float t, float amount)
        {

            float centered = (t - 0.5f) * 2f; // -1 .. +1
            float bias = centered * centered * centered;

            return t + bias * amount;
        }

        //float EdgeBias(float t, float amount)
        //{
        //    return Helper.Lerp(t, t + amount * (t - 0.5f) * Math.Abs(t - 0.5f) * 2f, 1f);
        //}


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

        async Task TryInitializeGazeDevice(CancellationToken token)
        {
            await deviceGate.WaitAsync(token);
            try
            {
                //Otro bucle (de una sesión anterior) pudo haber conectado mientras se esperaba el gate.
                if (GazeDevice != null) return;

                kalmanFilter.ResetStartingValues();

                //El orden de la lista determina el orden de chequeo de dispositivos.
                //El primer dispositivo que sea capaz de inicializar va ser asignado como GazeDevice.
                foreach (IGazeDevice device in supportedDevices.ToList())
                {
                    if (token.IsCancellationRequested) return;

                    bool initializeSucessful = false;
                    try
                    {
                        initializeSucessful = await device.Initialize();
                    }
                    catch (Exception e) when (e is DllNotFoundException || e is EntryPointNotFoundException || e is BadImageFormatException)
                    {
                        //Falta una DLL nativa, o la arquitectura del proceso no coincide (los SDK son x64).
                        Debug.WriteLine($"{device.DeviceName}: SDK nativo no disponible. {e.Message}");
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine($"{device.DeviceName}: error al inicializar. {e}");
                    }

                    if (!initializeSucessful) continue;

                    //Si mientras se inicializaba se canceló la sesión (cambio de dispositivo, calibración o cierre de la app),
                    //no se adopta: se libera para no dejar un SDK abierto sin dueño.
                    if (token.IsCancellationRequested)
                    {
                        Debug.WriteLine($"{device.DeviceName}: inicializado tras la cancelación; se libera.");
                        SafeDisconnect(device);
                        return;
                    }

                    GazeDevice = device;
                    OnGazeDeviceChanged?.Invoke(device);
                    GlobalEvents.OnEyetrackerConnected.Invoke();
                    Debug.WriteLine(device.DeviceName + " was initialized.");
                    return;
                }
            }
            finally
            {
                deviceGate.Release();
            }
        }

        /// <summary>
        /// Desconecta el dispositivo actual. Se ejecuta en segundo plano para no bloquear la UI mientras otro hilo
        /// usa el SDK (una inicialización puede tardar varios segundos). Sólo desconecta el dispositivo que estaba
        /// activo al llamar: si para entonces ya se conectó otro, no lo toca.
        /// </summary>
        public void DisconnectDevice()
        {
            IGazeDevice? expected = GazeDevice;
            if (expected == null) return;

            _ = Task.Run(async () =>
            {
                await deviceGate.WaitAsync();
                try
                {
                    if (GazeDevice == expected) DisconnectDeviceCore(expected);
                }
                finally
                {
                    deviceGate.Release();
                }
            });
        }

        /// <summary>Se llama con deviceGate tomado.</summary>
        void DisconnectDeviceCore(IGazeDevice device)
        {
            if (GazeDevice == device) GazeDevice = null;
            SafeDisconnect(device);
            GlobalEvents.OnEyetrackerDisconnected.Invoke();
        }

        static void SafeDisconnect(IGazeDevice device)
        {
            try
            {
                device.Disconnect();
            }
            catch (Exception e)
            {
                Debug.WriteLine($"{device.DeviceName}: error al desconectar. {e.Message}");
            }
        }

        public void SetMouseControl(bool enabled)
        {
            Settings.I.MouseToggle.Value = enabled;
        }

        public void ToggleMouseControl()
        {
            Settings.I.MouseToggle.Value = !Settings.I.MouseToggle.Value;
        }

        public void RequestCalibration(int pointsArray, int eyes)
        {
            //DEFAULT: con el Joaco seleccionado se abre su ventana de calibración aunque todavía no esté conectado.
            if (GazeDevice == null && joacoA11 != null && Settings.I.EyetrackerDevice.Value == Eyetracker_Device.Joaco)
            {
                joacoA11.RequestCalibration(pointsArray, eyes);
                return;
            }

            if (GazeDevice != null)
            {
                GazeDevice.RequestCalibration(pointsArray, eyes);
            }
        }

        public void ShowCameraPreview(bool show)
        {
            if (GazeDevice == null) return;
            GazeDevice.ShowCameraPreview(show);
        }

        public void EnableIntelligazeGUI(bool enable)
        {
            if (GazeDevice == null) return;
            if (GazeDevice.DeviceName != intelligaze.DeviceName) return;
            if (enable)
            {
                intelligaze.EnableDataStreaming();
                Settings.I.BubbleToggle.Value = previousGazeBubbleState;
                Settings.I.MouseToggle.Value = previousMouseControlToggle;
            }
            else
            {
                previousGazeBubbleState = Settings.I.BubbleToggle.Value;
                previousMouseControlToggle = Settings.I.MouseToggle.Value;
                Settings.I.BubbleToggle.Value = false;
                Settings.I.MouseToggle.Value = false;
                intelligaze.DisableDataStreaming();
            }
        }

        public void SwitchIntelligazeGUI()
        {
            if (GazeDevice == null) return;
            if (GazeDevice.DeviceName != intelligaze.DeviceName) return;
            //intelligaze.SwitchDataStreaming();
            if (intelligaze.IsStreaming)
            {
                EnableIntelligazeGUI(false);
            }
            else EnableIntelligazeGUI(true);
        }



        void UpdateMousePosition()
        {
            if (!mouseControlEnabled) return;
            if (input == null) return;
            if (UserNotPresentTimeOut) return;

            try
            {
                (double x, double y) pos = Helper.ViewportToMousePosition(GazeManager.I.SmoothViewportPoint);
                input.Mouse.MoveMouseToPositionOnVirtualDesktop(pos.x, pos.y);
            }
            catch
            {
                Debug.WriteLine("Windows simulator exception");
            }
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

        /// <summary>
        /// Cierre ordenado al salir de la app: cancela los dos bucles, espera a que terminen y libera el SDK del dispositivo.
        /// Antes sólo se cancelaba el bucle de lectura (el de búsqueda de dispositivos seguía vivo y podía inicializar
        /// un SDK durante el cierre), no se esperaba a ninguno y se desconectaba en paralelo a una lectura en curso.
        /// </summary>
        public void Dispose()
        {
            Task?[] tasks;
            lock (loopGate)
            {
                if (disposed) return;
                disposed = true;
                loopCts?.Cancel();
                tasks = new[] { loopTask, getDeviceTask };
            }

            Settings.I.EyetrackerDevice.OnChanged -= StartLoopWithSelectedDevice;

            //Los bucles sólo hacen BeginInvoke a la UI, así que esperar acá (en el hilo de UI) no los bloquea.
            Task[] running = tasks.Where(t => t != null).Cast<Task>().ToArray();
            try
            {
                if (running.Length > 0 && !Task.WaitAll(running, LOOP_STOP_TIMEOUT))
                {
                    Debug.WriteLine("GazeManager: los bucles no terminaron a tiempo; se libera el dispositivo igual.");
                }
            }
            catch (AggregateException e)
            {
                Debug.WriteLine("GazeManager: error al terminar los bucles. " + e.Flatten().InnerException?.Message);
            }

            bool gateTaken = deviceGate.Wait(DEVICE_GATE_TIMEOUT_ON_EXIT);
            try
            {
                IGazeDevice? device = GazeDevice;
                GazeDevice = null;
                if (device != null) SafeDisconnect(device);
            }
            finally
            {
                if (gateTaken) deviceGate.Release();
            }
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
}
