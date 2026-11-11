using System;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using Alea.Api;
using GazeStream.Utilities;
using GazeStream.Utilities.Events;

namespace GazeStream.Eyetracker
{
    /// <summary>
    /// Adaptador para Alea IntelliGaze. Se comunica con el servidor local de IntelliGaze mediante EtApi.dll,
    /// por lo que el programa de IntelliGaze tiene que estar instalado y abierto.
    /// </summary>
    public class GazeDeviceIntelligaze : IGazeDevice
    {
        const string SERVER_MUTEX_NAME = "Local\\AleaIntelliGaze30";
        const string API_KEY = "API-aw6oo-yrrhc";
        const string SERVER_IP = "127.0.0.1";
        const int SERVER_PORT = 27412;
        const int LISTEN_PORT = 27413;
        const int OPEN_ATTEMPTS = 3;

        //Una muestra sin actualizar por más de este tiempo se considera ausencia del usuario.
        static readonly TimeSpan PRESENCE_TIMEOUT = TimeSpan.FromSeconds(1);
        //Si no llegan datos raw válidos en este lapso, la mirada se toma de las fijaciones.
        static readonly TimeSpan RAW_GAZE_TIMEOUT = TimeSpan.FromMilliseconds(300);
        //Si IntelliGaze no avisa el fin de la calibración (por ejemplo, si el usuario la cerró), se libera el estado.
        static readonly TimeSpan CALIBRATION_TIMEOUT = TimeSpan.FromMinutes(3);
        static readonly TimeSpan CONNECTION_CHECK_INTERVAL = TimeSpan.FromSeconds(1);

        readonly EtApi api = new EtApi();

        //Los delegados se guardan en campos para que el GC no los recolecte mientras el código nativo los usa.
        readonly RawDataDelegate rawDataCB;
        readonly FixationDelegate fixationCB;
        readonly SaccadeDelegate saccadeCB;
        readonly BlinkDelegate blinkCB;
        readonly NoEventDelegate noEventCB;
        readonly CalibrationDoneDelegate calibrationDoneCB;
        readonly ResultCalibrationDelegate calibrationResultCB;
        readonly ResultCalibrationExDelegate calibrationResultExCB;
        readonly SystemMessageDelegate systemMessageCB;
        readonly EyeStatusDelegate eyeStatusCB;
        readonly EyeVisibilityDelegate eyeVisibilityCB;

        public string DeviceName => "Intelligaze";
        public bool IsConnected { get; private set; } = false;
        public bool UserIsPresent
        {
            get
            {
                lock (sampleLock)
                {
                    return lastValidSample != DateTime.MinValue && DateTime.UtcNow - lastValidSample < PRESENCE_TIMEOUT;
                }
            }
        }
        public GazePoint GazePoint { get { lock (sampleLock) return gazePointCache; } }
        public GazePoint RawGazePoint { get { lock (sampleLock) return rawGazePointCache; } }
        public EyesData Eyes { get; private set; } = new();
        public bool IsStreaming { get; private set; }

        readonly object sampleLock = new object();
        GazePoint gazePointCache = GazePoint.Invalid;
        GazePoint rawGazePointCache = GazePoint.Invalid;
        DateTime lastValidSample = DateTime.MinValue;
        DateTime lastRawGaze = DateTime.MinValue;
        DateTime lastConnectionCheck = DateTime.MinValue;

        Vector2 screenSize;
        bool isCalibrating;
        CancellationTokenSource? calibrationTimeoutCts;

        public GazeDeviceIntelligaze()
        {
            rawDataCB = OnRawData;
            fixationCB = OnFixation;
            saccadeCB = OnSaccade;
            blinkCB = OnBlink;
            noEventCB = OnNoEvent;
            calibrationDoneCB = OnCalibrationDone;
            calibrationResultCB = OnCalibrationResult;
            calibrationResultExCB = OnCalibrationResultEx;
            systemMessageCB = OnSystemMessage;
            eyeStatusCB = OnEyeStatus;
            eyeVisibilityCB = OnEyeVisibility;
        }

        public async Task<bool> Initialize()
        {
            screenSize = Helper.GetPrimaryMonitorSize();
            ResetSamples();

            if (!IsServerRunning())
            {
                //IntelliGaze no está abierto: GazeManager vuelve a intentar más tarde.
                return false;
            }

            ApiError error = ApiError.NotOpen;
            for (int attempt = 1; attempt <= OPEN_ATTEMPTS; attempt++)
            {
                error = api.Open(API_KEY, SERVER_IP, SERVER_PORT, SERVER_IP, LISTEN_PORT);
                Debug.WriteLine($"Intelligaze Open intento {attempt}: {error}");
                if (error == ApiError.NoError) break;
                await Task.Delay(1000);
            }
            if (error != ApiError.NoError)
            {
                return false;
            }

            Debug.WriteLine("Intelligaze API is open. Setting callbacks!");
            RegisterPassiveCallbacks(true);
            SetDataStreaming(true, true);
            IsConnected = true;
            lastConnectionCheck = DateTime.UtcNow;
            return true;
        }

        public void Disconnect()
        {
            CancelCalibrationTimeout();
            isCalibrating = false;
            try
            {
                SetDataStreaming(false, false);
                RegisterPassiveCallbacks(false);
                //Cierra la conexión con el servidor de IntelliGaze. (api.ExitServer() cerraría el programa de IntelliGaze.)
                api.Close();
            }
            catch (Exception e)
            {
                Debug.WriteLine("Intelligaze: error al desconectar. " + e.Message);
            }
            IsConnected = false;
            IsStreaming = false;
            ResetSamples();
        }

        public void UpdateData()
        {
            //No hace falta bombear datos: llegan por callbacks. Sólo se verifica que el servidor siga abierto.
            if (DateTime.UtcNow - lastConnectionCheck < CONNECTION_CHECK_INTERVAL) return;
            lastConnectionCheck = DateTime.UtcNow;

            if (!IsServerRunning())
            {
                Debug.WriteLine("Intelligaze: el servidor se cerró.");
                IsConnected = false;
            }
        }

        /// <summary>
        /// Comprueba si el servidor de IntelliGaze está abierto sin crear el mutex.
        /// Crear el mutex (como hacía la versión anterior) puede hacer que IntelliGaze crea que ya hay otra instancia.
        /// </summary>
        static bool IsServerRunning()
        {
            if (Mutex.TryOpenExisting(SERVER_MUTEX_NAME, out Mutex? mutex))
            {
                mutex.Dispose();
                return true;
            }
            return false;
        }

        void ResetSamples()
        {
            lock (sampleLock)
            {
                gazePointCache = GazePoint.Invalid;
                rawGazePointCache = GazePoint.Invalid;
                lastValidSample = DateTime.MinValue;
                lastRawGaze = DateTime.MinValue;
            }
        }

        #region Streaming

        void RegisterPassiveCallbacks(bool register)
        {
            if (IntPtr.Size == 8)
            {
                api.SetCalibrationDoneCB64(Ptr64(calibrationDoneCB, register), IntPtr.Zero);
                api.SetCalibrationResultCB64(Ptr64(calibrationResultCB, register), IntPtr.Zero);
                api.SetCalibrationResultExCB64(Ptr64(calibrationResultExCB, register), IntPtr.Zero);
                api.SetSystemMessageCB64(Ptr64(systemMessageCB, register), IntPtr.Zero);
                api.SetEyeStatusCB64(Ptr64(eyeStatusCB, register), IntPtr.Zero);
                api.SetEyeVisibilityCB64(Ptr64(eyeVisibilityCB, register), IntPtr.Zero);
            }
            else
            {
                api.SetCalibrationDoneCB(Ptr32(calibrationDoneCB, register), IntPtr.Zero);
                api.SetCalibrationResultCB(Ptr32(calibrationResultCB, register), IntPtr.Zero);
                api.SetCalibrationResultExCB(Ptr32(calibrationResultExCB, register), IntPtr.Zero);
                api.SetSystemMessageCB(Ptr32(systemMessageCB, register), IntPtr.Zero);
                api.SetEyeStatusCB(Ptr32(eyeStatusCB, register), IntPtr.Zero);
                api.SetEyeVisibilityCB(Ptr32(eyeVisibilityCB, register), IntPtr.Zero);
            }
        }

        static long Ptr64(Delegate d, bool register) => register ? Marshal.GetFunctionPointerForDelegate(d).ToInt64() : 0;
        static int Ptr32(Delegate d, bool register) => register ? Marshal.GetFunctionPointerForDelegate(d).ToInt32() : 0;

        private void SetDataStreaming(bool streamEvents, bool streamRaw)
        {
            int mode = 0;

            //Eventos: fijaciones, sacadas y parpadeos.
            if (streamEvents) mode |= 2;
            if (IntPtr.Size == 8)
            {
                api.SetBlinkCB64(Ptr64(blinkCB, streamEvents), IntPtr.Zero);
                api.SetFixationCB64(Ptr64(fixationCB, streamEvents), IntPtr.Zero);
                api.SetSaccadeCB64(Ptr64(saccadeCB, streamEvents), IntPtr.Zero);
                api.SetNoEventCB64(Ptr64(noEventCB, streamEvents), IntPtr.Zero);
            }
            else
            {
                api.SetBlinkCB(Ptr32(blinkCB, streamEvents), IntPtr.Zero);
                api.SetFixationCB(Ptr32(fixationCB, streamEvents), IntPtr.Zero);
                api.SetSaccadeCB(Ptr32(saccadeCB, streamEvents), IntPtr.Zero);
                api.SetNoEventCB(Ptr32(noEventCB, streamEvents), IntPtr.Zero);
            }

            //Raw: requiere una licencia especial de IntelliGaze.
            if (streamRaw) mode |= 1;
            if (IntPtr.Size == 8)
            {
                api.SetRawDataCB64(Ptr64(rawDataCB, streamRaw), IntPtr.Zero);
            }
            else
            {
                api.SetRawDataCB(Ptr32(rawDataCB, streamRaw), IntPtr.Zero);
            }

            ApiError error = api.DataStreaming(mode);
            if (error != ApiError.NoError && streamRaw)
            {
                //Sin licencia raw se sigue sólo con eventos (la mirada sale de las fijaciones).
                Debug.WriteLine($"Intelligaze DataStreaming({mode}): {error}. Reintentando sin raw.");
                error = api.DataStreaming(mode & ~1);
            }
            if (error != ApiError.NoError)
            {
                Debug.WriteLine($"Intelligaze DataStreaming: {error}");
            }
            IsStreaming = streamEvents;
        }

        public void SwitchDataStreaming()
        {
            if (IsStreaming)
            {
                DisableDataStreaming();
            }
            else EnableDataStreaming();
        }

        public void EnableDataStreaming()
        {
            SetDataStreaming(true, true);
        }

        public void DisableDataStreaming()
        {
            SetDataStreaming(false, false);
        }

        #endregion

        #region Gaze data

        //Los callbacks llegan en un hilo del SDK: no deben tocar la UI.

        private void OnRawData(ref RawData data, IntPtr userData)
        {
            //Datos de los ojos.
            Eye left = new Eye();
            Vector2 leftPos = ScreenToViewport((float)data.leftEye.eyeballPosX, (float)data.leftEye.eyeballPosY);
            left.viewportX = leftPos.X;
            left.viewportY = leftPos.Y;
            left.pupilDistanceMm = (float)data.leftEye.eyeballPosZ;   //mm relativo a la cámara
            left.pupilDiameter = (float)data.leftEye.pupilDiameter;   //Devuelve 0 cuando el ojo está cerrado.
            left.pupilDiameterMm = (float)data.leftEye.pupilDiameter;
            left.isBlinking = data.leftEye.pupilDiameter == 0;

            Eye right = new Eye();
            Vector2 rightPos = ScreenToViewport((float)data.rightEye.eyeballPosX, (float)data.rightEye.eyeballPosY);
            right.viewportX = rightPos.X;
            right.viewportY = rightPos.Y;
            right.pupilDistanceMm = (float)data.rightEye.eyeballPosZ;
            right.pupilDiameter = (float)data.rightEye.pupilDiameter;
            right.pupilDiameterMm = (float)data.rightEye.pupilDiameter;
            right.isBlinking = data.rightEye.pupilDiameter == 0;

            Eyes.leftEye = left;
            Eyes.rightEye = right;

            //Mirada raw (sólo con licencia raw). Con ambos ojos cerrados se ignora el punto.
            if (left.isBlinking && right.isBlinking) return;
            if (TryScreenToViewport((float)data.intelliGazeX, (float)data.intelliGazeY, out Vector2 gaze))
            {
                lock (sampleLock)
                {
                    lastRawGaze = DateTime.UtcNow;
                    SetGaze(gaze);
                }
            }
        }

        private void OnFixation(ref Fixation data, IntPtr userData)
        {
            if (!TryScreenToViewport((float)data.positionX, (float)data.positionY, out Vector2 gaze)) return;

            lock (sampleLock)
            {
                //Se usan fijaciones cuando no hay datos raw (licencia estándar/demo).
                if (DateTime.UtcNow - lastRawGaze < RAW_GAZE_TIMEOUT) return;
                SetGaze(gaze);
            }
        }

        //Debe llamarse dentro de sampleLock.
        void SetGaze(Vector2 viewport)
        {
            gazePointCache = new GazePoint(viewport);
            rawGazePointCache = new GazePoint(viewport);
            lastValidSample = DateTime.UtcNow;
        }

        bool TryScreenToViewport(float screenX, float screenY, out Vector2 viewport)
        {
            viewport = Vector2.Zero;
            if (screenSize.X == 0 || screenSize.Y == 0) return false;
            if (!float.IsFinite(screenX) || !float.IsFinite(screenY)) return false;
            //0 y -1 son los valores que IntelliGaze usa para "sin dato".
            if (screenX == 0 || screenY == 0) return false;
            if (screenX == -1 || screenY == -1) return false;

            viewport = ScreenToViewport(screenX, screenY);
            return true;
        }

        Vector2 ScreenToViewport(float screenX, float screenY)
        {
            return Helper.ScreenToViewportClamped(screenSize, screenX, screenY);
        }

        private void OnBlink(ref Blink data, IntPtr userData) { }
        private void OnSaccade(ref Saccade data, IntPtr userData) { }
        private void OnNoEvent(ref NoEventData data, IntPtr userData) { }
        private void OnEyeStatus(ref EyeStatus eyeStatus, IntPtr userData) { }
        private void OnEyeVisibility(ref int leftEye, ref int rightEye, IntPtr userData) { }

        private void OnSystemMessage(ref Int32 result, IntPtr userData)
        {
            Debug.WriteLine("Intelligaze system message: " + result);
        }

        #endregion

        #region Calibration

        public void OpenCalibrationPage()
        {
            RequestCalibration(2, 0);
        }

        /// <param name="pointsArray">0: 1 punto, 1: 5 puntos, 2: 9 puntos. (En Joaco el índice 0 son 3 puntos.)</param>
        /// <param name="eyes">0: ambos, 1: izquierdo, 2: derecho.</param>
        public void RequestCalibration(int pointsArray, int eyes)
        {
            if (!IsConnected)
            {
                Debug.WriteLine("Intelligaze: no se puede calibrar sin conexión.");
                GlobalEvents.OnCalibrationFailed.Invoke();
                return;
            }
            if (isCalibrating) return;

            int points = pointsArray switch
            {
                0 => 1,
                1 => 5,
                _ => 9,
            };

            EyeTypeEnum eyeType = eyes switch
            {
                1 => EyeTypeEnum.CalibrateLeft,
                2 => EyeTypeEnum.CalibrateRight,
                _ => EyeTypeEnum.Binocular,
            };

            //La calibración la dibuja el propio IntelliGaze.
            ApiError error = api.PerformCalibration(points, PointLocationEnum.Full, false, false, true, eyeType, false, false, true, 0, 2, "ANIMATION:PARROT");
            if (error != ApiError.NoError)
            {
                Debug.WriteLine("Intelligaze PerformCalibration: " + error);
                GlobalEvents.OnCalibrationFailed.Invoke();
                return;
            }

            isCalibrating = true;
            RunOnUI(() => GlobalEvents.OnCalibrationStart.Invoke());
            StartCalibrationTimeout();
        }

        void StartCalibrationTimeout()
        {
            CancelCalibrationTimeout();
            calibrationTimeoutCts = new CancellationTokenSource();
            CancellationToken token = calibrationTimeoutCts.Token;
            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(CALIBRATION_TIMEOUT, token);
                }
                catch (TaskCanceledException)
                {
                    return;
                }
                if (!isCalibrating) return;
                Debug.WriteLine("Intelligaze: no se recibió el fin de la calibración.");
                isCalibrating = false;
                RunOnUI(() =>
                {
                    GlobalEvents.OnCalibrationCancel.Invoke();
                    GlobalEvents.OnCalibrationFinished.Invoke();
                });
            });
        }

        void CancelCalibrationTimeout()
        {
            calibrationTimeoutCts?.Cancel();
            calibrationTimeoutCts = null;
        }

        private void OnCalibrationDone(ref Int32 data, Boolean improvementSuggestion, IntPtr userData)
        {
            int result = data;
            Debug.WriteLine($"Intelligaze calibration done: {result} improvement suggested: {improvementSuggestion}");
            if (!isCalibrating) return;
            isCalibrating = false;
            CancelCalibrationTimeout();
            RunOnUI(() =>
            {
                GlobalEvents.OnCalibrationSuccess.Invoke();
                GlobalEvents.OnCalibrationFinished.Invoke();
            });
        }

        private void OnCalibrationResult(ref ServerCalibrationResult result, IntPtr userData)
        {
            Debug.WriteLine($"Intelligaze calibration result: {result.P1} {result.P2} {result.P3} {result.P4} {result.P5} {result.P6} {result.P7} {result.P8} {result.P9}");
        }

        private void OnCalibrationResultEx(ref ServerCalibrationResultEx result, IntPtr userData) { }

        public void LoadCalibration(string calibName)
        {
            ApiError error = api.LoadCalibration(calibName);
            Debug.WriteLine($"Intelligaze LoadCalibration({calibName}): {error}");
        }

        public void SaveCalibration(string calibName)
        {
            ApiError error = api.SaveCalibration(calibName);
            Debug.WriteLine($"Intelligaze SaveCalibration({calibName}): {error}");
        }

        #endregion

        #region Camera

        public void ShowCameraPreview(bool open)
        {
            if (!IsConnected) return;
            if (open)
            {
                int posX = (int)(screenSize.X / 2f);
                int posY = (int)(screenSize.Y / 2f);
                int size = (int)(screenSize.Y / 2f);
                api.ShowStatusWindow(posX, posY, size, 1);
            }
            else
            {
                api.HideStatusWindow();
            }
        }

        #endregion

        static void RunOnUI(Action action)
        {
            var dispatcher = App.Current?.Dispatcher;
            if (dispatcher == null || dispatcher.CheckAccess())
            {
                action();
            }
            else
            {
                dispatcher.BeginInvoke(action);
            }
        }
    }
}
