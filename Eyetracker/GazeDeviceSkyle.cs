using System;
using System.Diagnostics;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using SkyleAvaloniaExample.Interop;
using GazeStream.Utilities;
using GazeStream.Windows;
using GazeStream.AppData;

namespace GazeStream.Eyetracker
{
    /// <summary>
    /// Adaptador para Skyle (eyeV) mediante skylelib.dll por USB.
    /// Si Skyle X está abierto, él es dueño del USB y esta app recibe los datos como cliente del link local.
    /// </summary>
    public class GazeDeviceSkyle : IGazeDevice
    {
        static readonly TimeSpan CONNECT_TIMEOUT = TimeSpan.FromSeconds(5);
        //Tiempo sin sincronizar (o sin muestras) tras el cual se considera desconectado.
        static readonly TimeSpan LOST_LINK_TIMEOUT = TimeSpan.FromSeconds(5);
        static readonly TimeSpan PRESENCE_TIMEOUT = TimeSpan.FromSeconds(1);

        public string DeviceName => "Skyle";
        public bool IsConnected { get; private set; }
        public bool UserIsPresent => IsConnected && DateTime.UtcNow.Ticks - Interlocked.Read(ref lastValidSampleTicks) < PRESENCE_TIMEOUT.Ticks;
        public GazePoint GazePoint { get { lock (sampleLock) return gazePointCache; } }
        public GazePoint RawGazePoint { get { lock (sampleLock) return rawGazePointCache; } }
        public EyesData Eyes => eyesCache;

        //Escritos desde el hilo de I/O de skylelib y leídos desde el bucle de GazeManager.
        readonly object sampleLock = new();
        GazePoint gazePointCache = GazePoint.Invalid;
        GazePoint rawGazePointCache = GazePoint.Invalid;
        readonly EyesData eyesCache = new();
        long lastValidSampleTicks;
        long lastSampleTicks;
        int latestState = (int)SkyleConnectionState.Disconnected;
        DateTime lastSyncedUtc;
        Vector2 screenSize;

        //Se crea un cliente nuevo en cada conexión: un SkyleClient liberado no se puede reutilizar.
        SkyleClient? client;
        public SkyleClient? Client => client;

        public async Task<bool> Initialize()
        {
            Debug.WriteLine("Initializing SKYLE");
            Disconnect();
            screenSize = Helper.GetPrimaryMonitorSize();

            var newClient = new SkyleClient();
            newClient.GazeReceived += OnGazeReceived;
            newClient.StateChanged += OnStateChanged;
            Interlocked.Exchange(ref latestState, (int)SkyleConnectionState.Disconnected);
            Interlocked.Exchange(ref lastSampleTicks, 0);

            try
            {
                newClient.Start();
            }
            catch (Exception ex)
            {
                //DllNotFound, layout de structs, etc.
                Debug.WriteLine("Could not connect to Skyle: " + ex.Message);
                ReleaseClient(newClient);
                return false;
            }
            client = newClient;

            //Espera a que el enlace quede sincronizado (o a que lleguen muestras si somos cliente del link de Skyle X).
            Stopwatch sw = Stopwatch.StartNew();
            while (sw.Elapsed < CONNECT_TIMEOUT)
            {
                if (IsLinkAlive(newClient))
                {
                    IsConnected = true;
                    lastSyncedUtc = DateTime.UtcNow;
                    //Por si el cambio a LinkSynced ocurrió antes de suscribirnos.
                    EnableStreams(newClient);
                    Debug.WriteLine($"Skyle conectado (link local: {newClient.IsLocalLink}).");
                    return true;
                }
                await Task.Delay(100);
            }

            Debug.WriteLine("Skyle: no se pudo sincronizar con el dispositivo. Estado: " + newClient.State);
            Disconnect();
            return false;
        }

        bool IsLinkAlive(SkyleClient c)
        {
            if (c.State == SkyleConnectionState.LinkSynced) return true;
            long last = Interlocked.Read(ref lastSampleTicks);
            return last != 0 && DateTime.UtcNow.Ticks - last < LOST_LINK_TIMEOUT.Ticks;
        }

        static void EnableStreams(SkyleClient c)
        {
            c.EnableGaze(true);
            c.EnablePositioning(true);
            c.EnableControl(true);
        }

        public void Disconnect()
        {
            SkyleClient? old = client;
            client = null;
            IsConnected = false;
            lock (sampleLock)
            {
                gazePointCache = GazePoint.Invalid;
                rawGazePointCache = GazePoint.Invalid;
            }
            Interlocked.Exchange(ref lastValidSampleTicks, 0);
            if (old != null) ReleaseClient(old);
        }

        void ReleaseClient(SkyleClient c)
        {
            c.GazeReceived -= OnGazeReceived;
            c.StateChanged -= OnStateChanged;
            try
            {
                Debug.WriteLine("Skyle client is being disposed");
                c.Dispose();
            }
            catch (Exception e)
            {
                Debug.WriteLine("Dispose Skyle was unsuccessful: " + e.Message);
            }
        }

        //Corre en el hilo de I/O de skylelib.
        void OnGazeReceived(GazeSnapshot gaze)
        {
            long now = DateTime.UtcNow.Ticks;
            Interlocked.Exchange(ref lastSampleTicks, now);
            if (!gaze.Valid) return;

            //Skyle entrega píxeles físicos de la pantalla principal.
            var smoothed = new GazePoint(Helper.ScreenToViewportClamped(screenSize, gaze.X, gaze.Y));
            var raw = new GazePoint(Helper.ScreenToViewportClamped(screenSize, gaze.RawX, gaze.RawY));
            lock (sampleLock)
            {
                gazePointCache = smoothed;
                rawGazePointCache = raw;
            }
            Interlocked.Exchange(ref lastValidSampleTicks, now);
        }

        //Corre en el hilo de I/O de skylelib.
        void OnStateChanged(SkyleConnectionState state)
        {
            Debug.WriteLine("Skyle State: " + state);
            Interlocked.Exchange(ref latestState, (int)state);
            SkyleClient? c = client;
            if (state == SkyleConnectionState.LinkSynced && c != null)
            {
                EnableStreams(c);
            }
        }

        public void UpdateData()
        {
            SkyleClient? c = client;
            if (c == null) { IsConnected = false; return; }

            //skylelib reconecta solo; si el enlace no vuelve en LOST_LINK_TIMEOUT se avisa la desconexión.
            if (IsLinkAlive(c))
            {
                lastSyncedUtc = DateTime.UtcNow;
            }
            else if (DateTime.UtcNow - lastSyncedUtc > LOST_LINK_TIMEOUT)
            {
                Debug.WriteLine("Skyle: se perdió el enlace con el dispositivo.");
                IsConnected = false;
            }
        }

        public void OpenCalibrationPage()
        {
            RunOnUI(() => WindowManager.OpenWindow<SkyleCalibrationWindow>());
        }

        /// <param name="pointsArray">0: 3 puntos, 1: 5 puntos, 2: 9 puntos.</param>
        /// <param name="eyes">Skyle calibra siempre ambos ojos; se guarda sólo como preferencia.</param>
        public void RequestCalibration(int pointsArray, int eyes)
        {
            Settings.I.LastPointsOption.Value = pointsArray;
            Settings.I.LastEyesOption.Value = eyes;
            RunOnUI(() =>
            {
                var window = WindowManager.OpenWindow<SkyleCalibrationWindow>();
                window.StartCalibration(pointsArray, closeOnFinished: true);
            });
        }

        public static byte[] GetCalibrationPoints(int pointsArray) => pointsArray switch
        {
            0 => SkyleClient.CalibrationPoints3,
            1 => SkyleClient.CalibrationPoints5,
            _ => SkyleClient.CalibrationPoints9,
        };

        public void ShowCameraPreview(bool show)
        {
        }

        static void RunOnUI(Action action)
        {
            var dispatcher = App.Current?.Dispatcher;
            if (dispatcher == null || dispatcher.CheckAccess()) action();
            else dispatcher.BeginInvoke(action);
        }
    }
}
