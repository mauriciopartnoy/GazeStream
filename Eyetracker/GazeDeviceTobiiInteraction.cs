using System;
using System.Diagnostics;
using System.IO;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using GazeStream.Utilities;
using GazeStream.Utilities.Events;
using Tobii.InteractionLib;

namespace GazeStream.Eyetracker
{
    /// <summary>
    /// Adaptador para eyetrackers Tobii de consumo (probado con Eye Tracker 5) mediante Tobii Interaction Library.
    /// El Eye Tracker 4C se considera obsoleto y no tiene soporte por ahora (le falta su runtime; ver CAMBIOS_PARA_CONEXION_SDK.md).
    /// Requiere que el software de Tobii (Tobii Experience) esté instalado. La calibración se delega a su herramienta.
    /// Otras líneas de Tobii (Pro, Dynavox/PCEye) usan SDK distintos y necesitan su propio adaptador.
    /// </summary>
    public class GazeDeviceTobiiInteraction : IGazeDevice
    {
        //Tiempo máximo para que Interaction Library informe que el stream de mirada está disponible.
        static readonly TimeSpan CONNECT_TIMEOUT = TimeSpan.FromSeconds(2);
        static readonly TimeSpan CAPABILITY_CHECK_INTERVAL = TimeSpan.FromSeconds(1);

        //Interaction Library no es thread-safe: Update, consultas y Dispose se serializan con este lock.
        readonly object libLock = new object();
        IInteractionLib? intlib;

        public string DeviceName => "Tobii Interaction";

        public bool IsConnected { get; private set; }

        public bool UserIsPresent { get; private set; } = false;
        public GazePoint GazePoint { get; private set; } = GazePoint.Invalid;
        public GazePoint RawGazePoint { get; private set; } = GazePoint.Invalid;
        public EyesData Eyes { get; private set; } = new EyesData();

        Vector2 screenSize;
        DateTime lastCapabilityCheck;

        public async Task<bool> Initialize()
        {
            Debug.WriteLine("Initializing Tobii Interaction");
            Disconnect();

            screenSize = Helper.GetPrimaryMonitorSize();
            Debug.WriteLine($"Monitor size {screenSize.X}*{screenSize.Y}");

            IInteractionLib lib;
            try
            {
                lib = InteractionLibFactory.CreateInteractionLib(FieldOfUse.Interactive);
            }
            catch (Exception e)
            {
                //Sin el runtime de Tobii instalado falla la creación.
                Debug.WriteLine("Tobii Interaction Library no disponible: " + e.Message);
                return false;
            }

            //Los puntos llegan en píxeles del área configurada aquí (monitor principal).
            lib.CoordinateTransformAddOrUpdateDisplayArea(screenSize.X, screenSize.Y);
            lib.CoordinateTransformSetOriginOffset(0, 0);
            lib.GazePointDataEvent += OnGazePoint;
            lib.PresenceDataEvent += OnPresence;
            lib.GazeOriginDataEvent += OnGazeOrigin;

            lock (libLock)
            {
                intlib = lib;
            }

            //Las capacidades se actualizan al procesar mensajes con Update(); hay que bombear mientras se espera.
            Stopwatch sw = Stopwatch.StartNew();
            while (sw.Elapsed < CONNECT_TIMEOUT)
            {
                lock (libLock)
                {
                    intlib?.Update();
                }
                if (HasStreamCapability())
                {
                    IsConnected = true;
                    lastCapabilityCheck = DateTime.UtcNow;
                    Debug.WriteLine("Tobii conectado.");
                    return true;
                }
                await Task.Delay(50);
            }

            Debug.WriteLine("Tobii: no hay stream de mirada disponible.");
            Disconnect();
            return false;
        }

        bool HasStreamCapability()
        {
            lock (libLock)
            {
                if (intlib == null) return false;
                intlib.GetDataStreamCapability(StreamType.GazePointData, out Capability cap);
                return cap == Capability.Enabled || cap == Capability.Available;
            }
        }

        public void UpdateData()
        {
            lock (libLock)
            {
                if (intlib == null) return;
                intlib.Update();
            }

            if (DateTime.UtcNow - lastCapabilityCheck > CAPABILITY_CHECK_INTERVAL)
            {
                lastCapabilityCheck = DateTime.UtcNow;
                if (!HasStreamCapability())
                {
                    Debug.WriteLine("Tobii: el stream de mirada dejó de estar disponible.");
                    IsConnected = false;
                }
            }
        }

        public void Disconnect()
        {
            lock (libLock)
            {
                if (intlib != null)
                {
                    intlib.GazePointDataEvent -= OnGazePoint;
                    intlib.PresenceDataEvent -= OnPresence;
                    intlib.GazeOriginDataEvent -= OnGazeOrigin;
                    try
                    {
                        intlib.Dispose();
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine("Tobii: error al liberar. " + e.Message);
                    }
                    intlib = null;
                }
            }
            IsConnected = false;
            UserIsPresent = false;
            GazePoint = GazePoint.Invalid;
            RawGazePoint = GazePoint.Invalid;
        }

        //Los eventos se disparan dentro de intlib.Update(), en el hilo del bucle de GazeManager.

        void OnGazePoint(GazePointData data)
        {
            if (data.validity != Validity.Valid) return;
            if (screenSize.X == 0 || screenSize.Y == 0) return;

            //Esta librería no provee el dato crudo: GazePoint y RawGazePoint son el punto filtrado por Tobii.
            Vector2 viewport = new Vector2(data.x / screenSize.X, data.y / screenSize.Y);
            GazePoint = new GazePoint(viewport);
            RawGazePoint = new GazePoint(viewport);
        }

        void OnPresence(PresenceData presenceData)
        {
            UserIsPresent = presenceData.presence == Presence.Present;
        }

        void OnGazeOrigin(GazeOriginData eye)
        {
            //GazeOrigin viene en mm respecto del eyetracker; no es un viewport como en el Joaco.
            //Los diámetros de pupila no los provee esta librería: se dejan valores fijos.
            const float pupilDiameter = 1;
            const float pupilDiameterMm = 8;

            Eye left = new();
            left.viewportX = eye.left_x;
            left.viewportY = eye.left_y;
            left.pupilDiameter = pupilDiameter;
            left.pupilDiameterMm = pupilDiameterMm;
            left.pupilDistanceMm = eye.left_z;
            left.isBlinking = eye.leftValidity == Validity.Invalid;

            Eye right = new();
            right.viewportX = eye.right_x;
            right.viewportY = eye.right_y;
            right.pupilDiameter = pupilDiameter;
            right.pupilDiameterMm = pupilDiameterMm;
            right.pupilDistanceMm = eye.right_z;
            right.isBlinking = eye.rightValidity == Validity.Invalid;

            Eyes.leftEye = left;
            Eyes.rightEye = right;
        }

        // ---- Calibración y posición de ojos mediante la herramienta de Tobii Experience ----
        //Interaction Library no puede calibrar. Tobii Experience trae Tobii.Configuration.exe, que acepta:
        //  -C calibrar el perfil actual, -N perfil nuevo y calibrar, -G invitado, -S configurar pantalla, -P posición de ojos.

        const string CONFIG_TOOL_RELATIVE_PATH = @"Tobii\Tobii EyeX\Tobii.Configuration.exe";
        const int CONFIG_TOOL_EXIT_OK = 0;
        const int CONFIG_TOOL_EXIT_CANCELLED = 2;
        //Si la herramienta no se cierra en este tiempo se informa el fallo (la ventana queda abierta).
        static readonly TimeSpan CALIBRATION_TIMEOUT = TimeSpan.FromMinutes(5);

        Process? configToolProcess;

        static string? FindConfigTool()
        {
            foreach (var root in new[] {
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86) })
            {
                if (string.IsNullOrEmpty(root)) continue;
                string path = Path.Combine(root, CONFIG_TOOL_RELATIVE_PATH);
                if (File.Exists(path)) return path;
            }
            return null;
        }

        bool IsConfigToolRunning => configToolProcess != null && !configToolProcess.HasExited;

        Process? StartConfigTool(string arguments)
        {
            string? path = FindConfigTool();
            if (path == null)
            {
                Debug.WriteLine("Tobii: no se encontró Tobii.Configuration.exe. ¿Está instalado Tobii Experience?");
                return null;
            }
            try
            {
                var process = Process.Start(new ProcessStartInfo(path, arguments) { UseShellExecute = false });
                Debug.WriteLine($"Tobii: herramienta de configuración iniciada ({arguments}).");
                return process;
            }
            catch (Exception e)
            {
                Debug.WriteLine("Tobii: no se pudo abrir la herramienta de configuración. " + e.Message);
                return null;
            }
        }

        public void OpenCalibrationPage()
        {
            RequestCalibration(0, 0);
        }

        /// <summary>
        /// Abre la calibración de Tobii Experience sobre el perfil actual. Los puntos y ojos no son configurables.
        /// El resultado se informa cuando la herramienta se cierra.
        /// </summary>
        public void RequestCalibration(int pointsArray, int eyes)
        {
            if (IsConfigToolRunning)
            {
                Debug.WriteLine("Tobii: ya hay una herramienta de Tobii abierta.");
                return;
            }

            Process? process = StartConfigTool("-C");
            if (process == null)
            {
                GlobalEvents.OnCalibrationFailed.Invoke();
                return;
            }

            configToolProcess = process;
            RunOnUI(() => GlobalEvents.OnCalibrationStart.Invoke());
            _ = WaitForCalibrationAsync(process);
        }

        async Task WaitForCalibrationAsync(Process process)
        {
            bool exited;
            try
            {
                using var cts = new CancellationTokenSource(CALIBRATION_TIMEOUT);
                await process.WaitForExitAsync(cts.Token);
                exited = true;
            }
            catch (OperationCanceledException)
            {
                exited = false;
            }

            int exitCode = exited ? process.ExitCode : int.MinValue;
            Debug.WriteLine(exited ? $"Tobii: calibración terminada, código de salida {exitCode}." : "Tobii: tiempo de calibración agotado.");

            RunOnUI(() =>
            {
                //Códigos de salida comprobados con un Eye Tracker 5: 0 = calibración completa, 2 = cancelada por el usuario.
                if (exited && exitCode == CONFIG_TOOL_EXIT_OK) GlobalEvents.OnCalibrationSuccess.Invoke();
                else if (exited && exitCode == CONFIG_TOOL_EXIT_CANCELLED) GlobalEvents.OnCalibrationCancel.Invoke();
                else GlobalEvents.OnCalibrationFailed.Invoke();
                //Finished reanuda el bucle de GazeManager, que se detuvo con OnCalibrationStart.
                GlobalEvents.OnCalibrationFinished.Invoke();
            });
        }

        /// <summary>Muestra la guía de posición de ojos de Tobii Experience.</summary>
        public void ShowCameraPreview(bool show)
        {
            if (!show)
            {
                //La ventana de Tobii se cierra desde la propia herramienta.
                return;
            }
            if (IsConfigToolRunning) return;
            configToolProcess = StartConfigTool("-P");
        }

        static void RunOnUI(Action action)
        {
            var dispatcher = System.Windows.Application.Current?.Dispatcher;
            if (dispatcher == null || dispatcher.CheckAccess()) action();
            else dispatcher.BeginInvoke(action);
        }
    }
}
