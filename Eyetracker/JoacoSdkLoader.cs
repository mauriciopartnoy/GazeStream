using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace GazeStream.Eyetracker
{
    /// <summary>Las dos APIs del SDK aSee (7invensun) que soporta GazeStream.</summary>
    public enum JoacoSdkApi
    {
        /// <summary>SDK 11.x: no tiene _7i_init; _7i_start(config_path) inicializa y abre la cámara.</summary>
        Legacy,
        /// <summary>SDK A8_1106.x: _7i_init(config) → _7i_device_connect → _7i_start().</summary>
        Current,
    }

    /// <summary>
    /// Un SDK del Joaco y el modelo de cámara (PID USB) para el que fue hecho.
    /// <paramref name="CalibrationVersion"/> es el texto de versión que el SDK escribe dentro de cada coeficiente
    /// de calibración que genera. Sirve para no pasarle a un SDK una calibración hecha con el otro.
    /// </summary>
    public sealed record JoacoSdkProfile(ushort ProductId, string Folder, JoacoSdkApi Api, string Description, string CalibrationVersion);

    /// <summary>
    /// Elige y carga el SDK del Joaco según la cámara conectada.
    /// Los SDK usan los mismos nombres de DLL (aSeeX.dll, USERSDK.dll, FdDetect.dll...), y Windows no puede tener
    /// dos módulos con el mismo nombre en un proceso: cada SDK vive en sdk\joaco\pid_XXXX y se carga UNO solo por
    /// ejecución. Cambiar a un modelo con otro SDK requiere reiniciar GazeStream.
    /// </summary>
    public static class JoacoSdkLoader
    {
        public const ushort VendorId = 0x3014;

        //Orden de preferencia para un PID desconocido: el primero de la lista.
        public static readonly IReadOnlyList<JoacoSdkProfile> Profiles = new[]
        {
            new JoacoSdkProfile(0x29D0, "pid_29D0", JoacoSdkApi.Legacy, "aSee A8 (SDK 11.0.2.006)", "20240904_1024_COMPUTECHANGE"),
            new JoacoSdkProfile(0x2CD1, "pid_2CD1", JoacoSdkApi.Current, "SDK A8_1106.202608", "20260602_2048_F300"),
        };

        // ---- Compatibilidad de calibraciones ----
        //
        // Un coeficiente generado por un SDK hace caer al otro con AccessViolationException dentro de
        // _7i_start_tracking, y esa excepción no se puede atrapar en .NET: cierra toda la app. Pasó al calibrar con un
        // modelo y después conectar el otro, porque la última calibración y los presets se comparten entre modelos.
        // Cada coeficiente lleva el texto de versión del SDK que lo generó (por ejemplo "20240904_1024_COMPUTECHANGE"),
        // así que se compara con el del SDK cargado antes de usarlo.

        static readonly Regex CalibrationVersionRegex = new(@"\d{8}_\d{4}_[A-Z0-9_]+", RegexOptions.Compiled);

        /// <summary>Textos de versión de SDK que contiene un coeficiente de calibración.</summary>
        public static List<string> GetCalibrationVersions(byte[]? coefficient)
        {
            var versions = new List<string>();
            if (coefficient == null || coefficient.Length == 0) return versions;
            string ascii = System.Text.Encoding.ASCII.GetString(coefficient);
            foreach (Match m in CalibrationVersionRegex.Matches(ascii))
            {
                if (!versions.Contains(m.Value)) versions.Add(m.Value);
            }
            return versions;
        }

        /// <summary>
        /// True si el coeficiente fue generado por el SDK de <paramref name="profile"/>.
        /// Un coeficiente sin versión (vacío o dañado) no se considera compatible.
        /// </summary>
        public static bool IsCalibrationCompatible(byte[]? coefficient, JoacoSdkProfile profile, out string reason)
        {
            List<string> versions = GetCalibrationVersions(coefficient);
            if (versions.Count == 0)
            {
                reason = "la calibración no tiene versión de SDK (vacía o dañada)";
                return false;
            }
            if (versions.Any(v => v != profile.CalibrationVersion))
            {
                reason = $"la calibración es del SDK {string.Join(", ", versions)} y el cargado es {profile.CalibrationVersion}";
                return false;
            }
            reason = "";
            return true;
        }

        static readonly object gate = new();
        static IntPtr handle;

        /// <summary>SDK cargado en este proceso, o null si todavía no se cargó ninguno.</summary>
        public static JoacoSdkProfile? Loaded { get; private set; }

        static JoacoSdkLoader()
        {
            //El nombre tiene que coincidir con el de los [DllImport] de ASeeTracker.
            NativeLibraryResolver.Register("aSeeX.dll", () => handle);
        }

        /// <summary>Asegura que el resolver esté registrado antes de la primera llamada a ASeeTracker.</summary>
        public static void EnsureResolverRegistered() { }

        static string SdkDirectory(JoacoSdkProfile profile) =>
            Path.Combine(AppContext.BaseDirectory, "sdk", "joaco", profile.Folder);

        /// <summary>
        /// Devuelve el SDK que corresponde a la cámara Joaco conectada, o null si no hay ninguna.
        /// Si el PID no está en la lista se usa el primer perfil, como intento.
        /// </summary>
        public static JoacoSdkProfile? SelectProfileForConnectedDevice()
        {
            List<ushort> productIds = GetConnectedProductIds();
            if (productIds.Count == 0) return null;

            foreach (ushort pid in productIds)
            {
                JoacoSdkProfile? known = Profiles.FirstOrDefault(p => p.ProductId == pid);
                if (known != null) return known;
            }

            JoacoSdkProfile fallback = Profiles[0];
            Debug.WriteLine($"Joaco con PID desconocido ({string.Join(", ", productIds.Select(p => "0x" + p.ToString("X4")))}). Se intenta con {fallback.Folder}.");
            return fallback;
        }

        /// <summary>
        /// Carga el SDK indicado. Si ya hay otro SDK cargado en el proceso devuelve false: Windows no permite
        /// tener los dos a la vez y descargar uno no es seguro (deja hilos y memoria nativa del SDK).
        /// </summary>
        public static bool TryLoad(JoacoSdkProfile profile, out string error)
        {
            lock (gate)
            {
                if (Loaded != null)
                {
                    if (Loaded == profile) { error = ""; return true; }
                    error = $"Está conectado un Joaco que usa el SDK {profile.Folder}, pero este proceso ya cargó {Loaded.Folder}. Hay que reiniciar GazeStream.";
                    return false;
                }

                string path = Path.Combine(SdkDirectory(profile), "aSeeX.dll");
                if (!File.Exists(path))
                {
                    error = "No se encontró el SDK del Joaco: " + path;
                    return false;
                }

                //LOAD_WITH_ALTERED_SEARCH_PATH: las dependencias (USERSDK.dll, FdDetect.dll...) se buscan
                //primero en la carpeta de este aSeeX.dll y no en la de la app.
                IntPtr h = LoadLibraryEx(path, IntPtr.Zero, LOAD_WITH_ALTERED_SEARCH_PATH);
                if (h == IntPtr.Zero)
                {
                    error = $"No se pudo cargar {path} (error de Windows {Marshal.GetLastWin32Error()}).";
                    return false;
                }

                handle = h;
                Loaded = profile;
                error = "";
                Debug.WriteLine($"SDK del Joaco cargado: {profile.Description} desde {SdkDirectory(profile)}");
                return true;
            }
        }

        // ---- Detección de la cámara por USB ----

        static readonly Regex JoacoIdRegex = new($@"^USB\\VID_{VendorId:X4}&PID_([0-9A-F]{{4}})", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// <summary>PIDs de las cámaras con el VID de 7invensun conectadas ahora.</summary>
        public static List<ushort> GetConnectedProductIds()
        {
            var result = new List<ushort>();
            try
            {
                const uint flags = CM_GETIDLIST_FILTER_ENUMERATOR | CM_GETIDLIST_FILTER_PRESENT;
                if (CM_Get_Device_ID_List_SizeW(out uint length, "USB", flags) != CR_SUCCESS || length == 0) return result;

                char[] buffer = new char[length];
                if (CM_Get_Device_ID_ListW("USB", buffer, length, flags) != CR_SUCCESS) return result;

                //Lista de cadenas separadas por '\0'.
                foreach (string id in new string(buffer).Split('\0', StringSplitOptions.RemoveEmptyEntries))
                {
                    Match m = JoacoIdRegex.Match(id);
                    if (!m.Success) continue;
                    ushort pid = Convert.ToUInt16(m.Groups[1].Value, 16);
                    if (!result.Contains(pid)) result.Add(pid);
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine("No se pudieron enumerar los dispositivos USB: " + e.Message);
            }
            return result;
        }

        const uint LOAD_WITH_ALTERED_SEARCH_PATH = 0x00000008;
        const uint CM_GETIDLIST_FILTER_ENUMERATOR = 0x00000001;
        const uint CM_GETIDLIST_FILTER_PRESENT = 0x00000100;
        const int CR_SUCCESS = 0;

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        static extern IntPtr LoadLibraryEx(string lpLibFileName, IntPtr hFile, uint dwFlags);

        [DllImport("cfgmgr32.dll", CharSet = CharSet.Unicode)]
        static extern int CM_Get_Device_ID_List_SizeW(out uint pulLen, string pszFilter, uint ulFlags);

        [DllImport("cfgmgr32.dll", CharSet = CharSet.Unicode)]
        static extern int CM_Get_Device_ID_ListW(string pszFilter, [Out] char[] buffer, uint bufferLen, uint ulFlags);
    }
}
