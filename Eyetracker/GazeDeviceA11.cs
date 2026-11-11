using System.Runtime.InteropServices;
using System.Diagnostics;
using GazeStream.Eyetracker;
using GazeStream.Utilities;
using GazeStream.Utilities.Events;
using System.Numerics;
using GazeStream.AppData;
using GazeStream.Windows;
using System.Text;
using System.IO;
public class GazeDeviceA11 : IGazeDevice
{
    public string DeviceName => "Eyetracker Joaco A11";
    public bool IsConnected { get; private set; }
    public bool UserIsPresent => UserIsPresentStatic;
    public GazePoint GazePoint => gazePointCache;
    public GazePoint RawGazePoint => rawGazePointCache;
    public EyesData Eyes => eyesCache;

    GazePoint gazePointCache;
    GazePoint rawGazePointCache;
    EyesData eyesCache = new();

    public _7i_coefficient_t coefficient;
    public static _7i_eye_data_ex_t eyesData;
    public static Vector3 gazeOrigin;

    static ASeeTracker.gazeCallback gazeCB = new ASeeTracker.gazeCallback(OnGazeCallback);
    static float gaze_x = 0.0f;
    static float gaze_y = 0.0f;
    static float rawGaze_x = 0.0f;
    static float rawGaze_y = 0.0f;
    static readonly object sampleLock = new object();
    static bool gazePointChanged;
    static bool UserIsPresentStatic;
    static bool HasValidCalibration;
    Vector2 screenSizeMillimetres;

    //Las rutas relativas dependen del directorio de trabajo, que no es la carpeta de la app cuando se inicia con Windows.
    static string ConfigDirectory => Path.Combine(AppContext.BaseDirectory, "config");
    static string FlashDataFilePath => Path.Combine(ConfigDirectory, "Configs.dat");

    //Estado del SDK nativo, para liberar sólo lo que se llegó a abrir.
    bool sdkInitialized;
    bool deviceConnected;
    bool sdkStarted;

    //Último motivo informado por el que no se pudo iniciar, para no repetir el mismo log cada 500 ms.
    string lastInitMessage = "";

    void LogInitOnce(string message)
    {
        if (message == lastInitMessage) return;
        lastInitMessage = message;
        Debug.WriteLine(message);
    }

    public async Task<bool> Initialize()
    {
        //0. ELEGIR EL SDK según el modelo conectado (cada modelo de Joaco tiene su propio SDK).
        JoacoSdkProfile? profile = JoacoSdkLoader.SelectProfileForConnectedDevice();
        if (profile == null)
        {
            LogInitOnce("Joaco: no hay ninguna cámara conectada.");
            return false;
        }
        if (!JoacoSdkLoader.TryLoad(profile, out string loadError))
        {
            LogInitOnce("Joaco: " + loadError);
            return false;
        }
        lastInitMessage = "";

        Debug.WriteLine($"About to initialize Joaco ({profile.Description}).");
        screenSizeMillimetres = WindowsHelper.GetPhysicalScreenSizeInMillimetres();
        ResetSampleState();
        Directory.CreateDirectory(ConfigDirectory);

        bool started = profile.Api == JoacoSdkApi.Legacy ? StartLegacySdk() : StartCurrentSdk();
        if (!started) return false;
        IsConnected = true;

        //TRACKING con la última calibración (o la de fábrica).
        SetCurrentUserCalibration();
        if (!HasValidCalibration)
        {
            //El dispositivo queda conectado para poder calibrarlo, pero no enviará puntos hasta tener una calibración válida.
            Debug.WriteLine("Joaco conectado sin calibración válida. Es necesario calibrar.");
        }

        Settings.I.SmoothFilter.OnChanged -= LoadSmooth;
        Settings.I.SmoothFilter.OnChanged += LoadSmooth;
        return true;
    }

    /// <summary>
    /// SDK 11.x (aSee A8, PID 0x29D0): _7i_start(config) inicializa el SDK y abre la cámara; después se configura.
    /// No tiene _7i_init/_7i_release y no necesita _7i_device_connect.
    /// </summary>
    bool StartLegacySdk()
    {
        int startResult = ASeeTracker._7i_start_with_config(ConfigDirectory);
        Debug.WriteLine($"_7i_start(config) [SDK legacy]: {startResult}");
        if (startResult != 0)
        {
            GlobalEvents.OnEyetrackerConnectionFailed.Invoke();
            ReleaseNative();
            return false;
        }
        sdkStarted = true;

        ASeeTracker._7i_set_gaze_callback(Marshal.GetFunctionPointerForDelegate(gazeCB), IntPtr.Zero);
        int screenResult = ASeeTracker._7i_set_screen_size(screenSizeMillimetres.X, screenSizeMillimetres.Y);
        Debug.WriteLine($"_7i_set_screen_size: {screenResult}");
        SetCheckCalib(true);
        SetDetectEyelid(true);
        SetSmoothWay(0);
        //Configs.dat (flash data) es del SDK Current: no se envía a este SDK.
        return true;
    }

    /// <summary>SDK A8_1106.x (PID 0x2CD1): _7i_init → _7i_device_connect → configuración → _7i_start().</summary>
    bool StartCurrentSdk()
    {
        //1. INICIALIZAR SDK
        int ret = ASeeTracker._7i_init(ConfigDirectory);
        Debug.WriteLine($"_7i_init: {ret}");
        if (ret != 0)
        {
            return false;
        }
        sdkInitialized = true;

        //2. CONECTAR DISPOSITIVO
        _7i_ukey_info_t keyInfo = new _7i_ukey_info_t();
        ret = ASeeTracker._7i_device_connect("", ref keyInfo);
        Debug.WriteLine($"_7i_device_connect: {ret}");
        if (ret != 0)
        {
            //Es el caso normal cuando el Joaco no está enchufado: se reintenta desde GazeManager.
            ReleaseNative();
            return false;
        }
        deviceConnected = true;

        //3. CONFIGURACIÓN (callback, pantalla y parámetros) antes de arrancar el stream.
        ASeeTracker._7i_set_gaze_callback(Marshal.GetFunctionPointerForDelegate(gazeCB), IntPtr.Zero);
        ASeeTracker._7i_set_screen_size(screenSizeMillimetres.X, screenSizeMillimetres.Y);
        SetCheckCalib(true);
        SetDetectEyelid(true);
        SetSmoothWay(0);
        PreFlashData();

        //4. ARRANCAR
        int startResult = ASeeTracker._7i_start();
        Debug.WriteLine("_7i_start: " + aSeeResults.StartResultToString(startResult));
        if (startResult != 0)
        {
            GlobalEvents.OnEyetrackerConnectionFailed.Invoke();
            ReleaseNative();
            return false;
        }
        sdkStarted = true;
        return true;
    }

    public void Disconnect()
    {
        Settings.I.SmoothFilter.OnChanged -= LoadSmooth;
        ReleaseNative();
        IsConnected = false;
        ResetSampleState();
    }

    void ReleaseNative()
    {
        try
        {
            if (sdkStarted)
            {
                int stopTrackingResult = ASeeTracker._7i_stop_tracking();
                Debug.WriteLine("StopTracking result: " + aSeeResults.StopTrackingResultToString(stopTrackingResult));
                int stopResult = ASeeTracker._7i_stop();
                Debug.WriteLine("Stop result: " + aSeeResults.StopResultToString(stopResult));
            }
            if (deviceConnected)
            {
                int disconnectResult = ASeeTracker._7i_device_disconnect();
                Debug.WriteLine("Disconnect result: " + aSeeResults.DisconnectResultToString(disconnectResult));
            }
            if (sdkInitialized)
            {
                int releaseResult = ASeeTracker._7i_release();
                Debug.WriteLine("Release result: " + releaseResult);
            }
        }
        catch (Exception e)
        {
            Debug.WriteLine("Error liberando el SDK del Joaco: " + e.Message);
        }
        finally
        {
            sdkStarted = false;
            deviceConnected = false;
            sdkInitialized = false;
            HasValidCalibration = false;
        }
    }

    static void ResetSampleState()
    {
        lock (sampleLock)
        {
            gazePointChanged = false;
            UserIsPresentStatic = false;
        }
    }

    public void SetCheckCalib(bool enable)
    {
        string strKey = "check_calib";
        byte[] asciiBytes = System.Text.Encoding.ASCII.GetBytes(strKey);
        string asciiStringFromBytes = System.Text.Encoding.ASCII.GetString(asciiBytes);

        string strData = (true == enable) ? "1" : "0";
        byte[] asciiData = System.Text.Encoding.ASCII.GetBytes(strData);
        int ret = ASeeTracker._7i_set_data(asciiStringFromBytes, asciiStringFromBytes.Length, asciiData, asciiData.Length);
        Debug.WriteLine($"_7i_set_data(check_calib): , {ret}");

    }

    public void SetDetectEyelid(bool enable)
    {
        string strKey = "detect_eyelid";
        byte[] asciiBytes = System.Text.Encoding.ASCII.GetBytes(strKey);
        string asciiStringFromBytes = System.Text.Encoding.ASCII.GetString(asciiBytes);

        string strData = (true == enable) ? "1" : "0";
        byte[] asciiData = System.Text.Encoding.ASCII.GetBytes(strData);
        int ret = ASeeTracker._7i_set_data(asciiStringFromBytes, asciiStringFromBytes.Length, asciiData, asciiData.Length);
        Debug.WriteLine($"_7i_set_data(detect_eyelid): , {ret}");

    }

    public void SetSmoothWay(int way)
    {
        if (way < 0 || way > 2)
        {
            return;
        }

        string[] strData = new string[3];
        strData[0] = "0";
        strData[1] = "1";
        strData[2] = "2";

        string strKey = "smoothWay";
        byte[] asciiBytes = System.Text.Encoding.ASCII.GetBytes(strKey);
        string asciiStringFromBytes = System.Text.Encoding.ASCII.GetString(asciiBytes);

        byte[] asciiData = System.Text.Encoding.ASCII.GetBytes(strData[way]);
        int ret = ASeeTracker._7i_set_data(asciiStringFromBytes, asciiStringFromBytes.Length, asciiData, asciiData.Length);
        Console.WriteLine($"_7i_set_data(smoothWay): , {ret}");
    }

    void PreFlashData()
    {
        //Datos de fábrica del dispositivo (Configs.dat). Si no existen se omite el paso: no es obligatorio para trackear.
        try
        {
            if (!File.Exists(FlashDataFilePath))
            {
                Debug.WriteLine("Configs.dat no encontrado. Se omite _7i_set_gazepre_flash_data.");
                return;
            }

            byte[] flash_data = File.ReadAllBytes(FlashDataFilePath);
            if (flash_data.Length == 0)
            {
                Debug.WriteLine("Configs.dat vacío. Se omite _7i_set_gazepre_flash_data.");
                return;
            }

            //El array tiene que estar fijado en memoria mientras el SDK lo lee.
            GCHandle handle = GCHandle.Alloc(flash_data, GCHandleType.Pinned);
            try
            {
                int ret = ASeeTracker._7i_set_gazepre_flash_data(handle.AddrOfPinnedObject(), flash_data.Length, screenSizeMillimetres.X, screenSizeMillimetres.Y);
                Debug.WriteLine($"_7i_set_gazepre_flash_data: {ret}");
            }
            finally
            {
                handle.Free();
            }
        }
        catch (Exception e)
        {
            Debug.WriteLine("Error cargando Configs.dat: " + e.Message);
        }
    }

    void SetCurrentUserCalibration()
    {
        Debug.WriteLine("Setting current user calibration");
        if (!IsConnected) return;

        GetCoefficient();
        LoadSmooth();
        ApplyCoefficient(coefficient.buf);
    }

    /// <summary>
    /// Inicia el tracking con un buffer de calibración y actualiza HasValidCalibration según el resultado del SDK.
    /// </summary>
    bool ApplyCoefficient(byte[]? buff)
    {
        if (buff == null || buff.Length == 0) return false;

        //Un coeficiente de otro SDK hace caer la app entera dentro de _7i_start_tracking (AccessViolation, no se puede atrapar).
        if (!IsCompatibleWithLoadedSdk(buff, out string reason))
        {
            Debug.WriteLine("Joaco: se descarta la calibración: " + reason);
            return false;
        }

        //El struct nativo es de tamaño fijo: un buffer de otro largo rompería el marshalling.
        if (buff.Length != COEFFICIENT_SIZE)
        {
            byte[] resized = new byte[COEFFICIENT_SIZE];
            Array.Copy(buff, resized, Math.Min(buff.Length, COEFFICIENT_SIZE));
            Debug.WriteLine($"Buffer de calibración de {buff.Length} bytes ajustado a {COEFFICIENT_SIZE}.");
            buff = resized;
        }

        coefficient.buf = buff;
        int result = ASeeTracker._7i_start_tracking(ref coefficient);
        HasValidCalibration = result == 0;
        Debug.WriteLine("Start tracking result: " + aSeeResults.StartTrackingResultToString(result));
        if (HasValidCalibration)
        {
            LoadSmooth();
        }
        return HasValidCalibration;
    }

    const int COEFFICIENT_SIZE = 2048;
    public bool HasCalibration => HasValidCalibration;


    //IMPORTANTE: No todos los eyetrackers pueden calibrarse externamente por lo tanto el botón va a ser solo para el joaco.
    //Pero en caso de querer un botón mas general puede agregarse este metodo a la interfaz IGazeDevice.
    public void SetDefaultCalibration()
    {
        StartTrackingWithCoefficient(GetDefaultCalibrationBuff());
    }

    public void SetBestCalibrationFromPresets()
    {
        byte[] newBuff = FindBestPresetForThisDevice();
        StartTrackingWithCoefficient(newBuff);
    }

    /// <summary>
    /// Aplica el mejor preset sólo si es distinto de la calibración actual (usado por la calibración dinámica).
    /// </summary>
    public void ApplyBestPresetIfDifferent()
    {
        byte[] best = FindBestPresetForThisDevice();
        if (best == null || best.Length == 0) return;
        if (coefficient.buf != null && best.AsSpan().SequenceEqual(coefficient.buf)) return;
        Debug.WriteLine("Calibración dinámica: aplicando otro preset.");
        StartTrackingWithCoefficient(best);
    }

    static bool IsCompatibleWithLoadedSdk(byte[]? buff, out string reason)
    {
        JoacoSdkProfile? profile = JoacoSdkLoader.Loaded;
        if (profile == null)
        {
            reason = "no hay un SDK del Joaco cargado";
            return false;
        }
        return JoacoSdkLoader.IsCalibrationCompatible(buff, profile, out reason);
    }

    public byte[] FindBestPresetForThisDevice()
    {
        //Sólo presets generados por el SDK cargado: los de otro modelo de Joaco harían caer la app.
        var Presets = CalibrationPresets.I.Presets?
            .Where(p => IsCompatibleWithLoadedSdk(p.calibrationBuff, out _))
            .ToList();
        if (Presets == null) return new byte[0];
        if (Presets.Count == 0) return new byte[0];

        Vector2 monitorSize = Helper.GetPrimaryMonitorSize();
        Debug.WriteLine("Monitor size is: " + monitorSize);
        var elegible = Presets.Where(p => p.screenSize.X == monitorSize.X && p.screenSize.Y == monitorSize.Y).ToList();

        if (elegible.Count == 0)
        {
            elegible = Presets;
        }

        //TODO: Agregar criterios de distancia o posición para filtrar la lista
        float distanceToScreen = Math.Max(eyesCache.leftEye.pupilDistanceMm, eyesCache.rightEye.pupilDistanceMm);

        //Prefer high point count && better score
        CalibrationPreset bestPreset = elegible[0];
        //foreach (CalibrationPreset p in elegible)
        //{
        //    float absDistanceToScreen = MathF.Abs(p.distanceToScreen - distanceToScreen);
        //    float bestDistToScreen = MathF.Abs(bestPreset.distanceToScreen - distanceToScreen);
        //    if (absDistanceToScreen < bestDistToScreen)
        //    {
        //        bestPreset = p;
        //    }
        //}

        foreach (CalibrationPreset p in elegible)
        {

            if (p.calibrationPoints > bestPreset.calibrationPoints)
            {
                bestPreset = p;
            }

            if ((p.scoreLeft + p.scoreRight) > (bestPreset.scoreLeft + bestPreset.scoreRight))
            {
                bestPreset = p;
            }
        }

        return bestPreset.calibrationBuff;
    }

    public void SetDefaultCalibrationRight()
    {
        byte[] coe = GetDefaultCalibrationBuffRight();
        StartTrackingWithCoefficient(coe);
    }

    public void SetDefaultCalibrationLeft()
    {
        byte[] coe = GetDefaultCalibrationBuffLeft();
        StartTrackingWithCoefficient(coe);
    }


    public bool StartTrackingWithCoefficient(byte[] buff)
    {
        if (!IsConnected) return false;
        bool ok = ApplyCoefficient(buff);
        if (ok)
        {
            Settings.I.LastCalibrationBuff.Value = coefficient.buf;
        }
        return ok;
    }

    /// <summary>
    /// Elige la calibración con la que arrancar: la última usada, si es de este SDK; si no (por ejemplo, la última
    /// se hizo con otro modelo de Joaco), el mejor preset compatible; si no, la de fábrica (que es del SDK de la A8).
    /// </summary>
    void GetCoefficient()
    {
        coefficient = new _7i_coefficient_t();

        byte[]? last = Settings.I.LastCalibrationBuff.Value;
        if (last != null && last.Length > 0)
        {
            if (IsCompatibleWithLoadedSdk(last, out string reason))
            {
                coefficient.buf = last;
                return;
            }
            Debug.WriteLine("Joaco: la última calibración no sirve para este SDK (" + reason + "). Se busca otra.");
        }

        byte[] preset = FindBestPresetForThisDevice();
        if (preset.Length > 0)
        {
            Debug.WriteLine("Joaco: usando el mejor preset compatible.");
            coefficient.buf = preset;
            return;
        }

        Debug.WriteLine("Joaco: no hay calibración compatible guardada. Usando la de fábrica.");
        coefficient.buf = GetDefaultCalibrationBuff();
    }

    //Este método se usa para extraer una calibración y dejarla hard codeada por default.
    private void ShowCoefficientBuffInDebugger()
    {
        StringBuilder bufBytesArray = new StringBuilder("new byte[] {");
        for (int i = 0; i < coefficient.buf.Length; i++)
        {
            bufBytesArray.Append(coefficient.buf[i]);
            bufBytesArray.Append(",");
        }
        bufBytesArray.Remove(bufBytesArray.Length - 1, 1);
        bufBytesArray.Append("}");
        Debug.WriteLine(bufBytesArray.ToString());
    }
    public byte[] GetDefaultCalibrationBuff()
    {
        byte[] Preset_2880_1920 = new byte[] { 141, 19, 0, 0, 193, 134, 1, 0, 0, 0, 0, 32, 69, 178, 35, 64, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 50, 48, 50, 52, 48, 57, 48, 52, 95, 49, 48, 50, 52, 95, 67, 79, 77, 80, 85, 84, 69, 67, 72, 65, 78, 71, 69, 0, 0, 0, 0, 0, 0, 0, 0, 63, 0, 0, 0, 63, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 154, 153, 249, 64, 102, 102, 118, 64, 72, 225, 170, 63, 0, 0, 0, 0, 206, 218, 157, 124, 30, 62, 115, 191, 80, 57, 195, 114, 87, 131, 229, 191, 182, 121, 92, 239, 223, 1, 115, 191, 232, 41, 136, 25, 152, 213, 177, 63, 108, 18, 33, 143, 186, 209, 192, 63, 7, 100, 202, 109, 125, 117, 142, 63, 17, 213, 22, 16, 22, 15, 253, 191, 204, 245, 130, 63, 10, 12, 149, 191, 227, 79, 102, 11, 7, 110, 193, 63, 21, 27, 156, 137, 139, 113, 194, 191, 198, 106, 126, 86, 1, 66, 161, 191, 138, 11, 76, 51, 90, 8, 190, 63, 111, 1, 228, 188, 146, 237, 217, 63, 223, 161, 192, 154, 224, 17, 167, 63, 51, 51, 51, 51, 51, 51, 31, 64, 205, 204, 204, 204, 204, 204, 16, 64, 102, 102, 102, 102, 102, 102, 245, 63, 205, 204, 204, 204, 204, 204, 20, 64, 0, 0, 0, 0, 0, 0, 240, 63, 141, 19, 0, 0, 193, 134, 1, 0, 0, 0, 0, 224, 207, 165, 35, 64, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 50, 48, 50, 52, 48, 57, 48, 52, 95, 49, 48, 50, 52, 95, 67, 79, 77, 80, 85, 84, 69, 67, 72, 65, 78, 71, 69, 0, 0, 0, 0, 0, 0, 0, 0, 63, 0, 0, 0, 63, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 154, 153, 249, 64, 102, 102, 118, 64, 72, 225, 170, 63, 0, 0, 0, 0, 138, 160, 172, 237, 177, 255, 100, 63, 189, 91, 164, 195, 57, 137, 229, 191, 255, 49, 68, 240, 83, 133, 141, 63, 167, 80, 142, 245, 162, 152, 149, 63, 220, 8, 149, 179, 89, 220, 202, 191, 67, 51, 201, 190, 124, 172, 132, 63, 6, 33, 240, 184, 148, 60, 253, 191, 219, 3, 96, 149, 209, 4, 182, 63, 44, 49, 148, 4, 90, 102, 160, 63, 239, 59, 109, 60, 174, 203, 187, 191, 48, 225, 115, 155, 226, 69, 197, 191, 45, 214, 110, 166, 216, 54, 208, 191, 160, 249, 56, 154, 21, 230, 219, 63, 240, 71, 62, 117, 33, 171, 196, 191, 51, 51, 51, 51, 51, 51, 31, 64, 205, 204, 204, 204, 204, 204, 16, 64, 102, 102, 102, 102, 102, 102, 245, 63, 205, 204, 204, 204, 204, 204, 20, 64, 0, 0, 0, 0, 0, 0, 240, 63, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 64, 3, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
        return Preset_2880_1920;
    }

    public byte[] GetDefaultCalibrationBuffLeft()
    {
        byte[] Preset_2880_1920 = new byte[] { 141, 19, 0, 0, 193, 134, 1, 0, 0, 0, 0, 64, 10, 132, 35, 64, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 50, 48, 50, 52, 48, 57, 48, 52, 95, 49, 48, 50, 52, 95, 67, 79, 77, 80, 85, 84, 69, 67, 72, 65, 78, 71, 69, 0, 0, 0, 0, 0, 0, 0, 0, 63, 0, 0, 0, 63, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 154, 153, 249, 64, 102, 102, 118, 64, 72, 225, 170, 63, 0, 0, 0, 0, 169, 158, 21, 212, 22, 87, 112, 63, 33, 184, 88, 185, 167, 47, 229, 191, 80, 107, 3, 76, 198, 127, 157, 191, 23, 163, 134, 50, 43, 133, 128, 191, 21, 32, 123, 194, 24, 213, 190, 191, 127, 143, 6, 10, 125, 40, 157, 191, 109, 109, 67, 226, 163, 12, 252, 191, 52, 166, 54, 243, 175, 45, 164, 191, 60, 93, 44, 109, 232, 82, 214, 63, 122, 195, 237, 210, 117, 81, 166, 191, 164, 133, 75, 69, 77, 87, 214, 63, 241, 244, 159, 188, 61, 120, 224, 191, 64, 88, 206, 250, 71, 7, 218, 63, 186, 163, 60, 49, 30, 38, 166, 63, 51, 51, 51, 51, 51, 51, 31, 64, 205, 204, 204, 204, 204, 204, 16, 64, 102, 102, 102, 102, 102, 102, 245, 63, 205, 204, 204, 204, 204, 204, 20, 64, 0, 0, 0, 0, 0, 0, 240, 63, 141, 19, 0, 0, 193, 134, 1, 0, 0, 0, 0, 64, 132, 192, 35, 64, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 64, 3, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
        return Preset_2880_1920;
    }

    public byte[] GetDefaultCalibrationBuffRight()
    {
        byte[] Preset_2880_1920 = new byte[] { 141, 19, 0, 0, 193, 134, 1, 0, 0, 0, 0, 64, 10, 132, 35, 64, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 141, 19, 0, 0, 193, 134, 1, 0, 0, 0, 0, 160, 97, 214, 35, 64, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 50, 48, 50, 52, 48, 57, 48, 52, 95, 49, 48, 50, 52, 95, 67, 79, 77, 80, 85, 84, 69, 67, 72, 65, 78, 71, 69, 0, 0, 0, 0, 0, 0, 0, 0, 63, 0, 0, 0, 63, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 154, 153, 249, 64, 102, 102, 118, 64, 72, 225, 170, 63, 0, 0, 0, 0, 45, 13, 190, 136, 93, 230, 139, 191, 17, 172, 211, 102, 117, 121, 229, 191, 249, 200, 78, 229, 92, 217, 171, 63, 163, 91, 25, 107, 26, 241, 144, 63, 214, 161, 212, 81, 138, 212, 210, 63, 227, 235, 207, 0, 30, 153, 110, 191, 179, 66, 163, 222, 163, 84, 251, 191, 92, 140, 52, 102, 206, 101, 171, 63, 224, 11, 31, 102, 255, 128, 198, 63, 31, 221, 16, 169, 21, 214, 153, 191, 22, 139, 97, 139, 17, 230, 213, 63, 35, 72, 171, 47, 129, 194, 224, 191, 230, 242, 103, 194, 113, 153, 219, 63, 140, 129, 190, 111, 172, 147, 194, 191, 51, 51, 51, 51, 51, 51, 31, 64, 205, 204, 204, 204, 204, 204, 16, 64, 102, 102, 102, 102, 102, 102, 245, 63, 205, 204, 204, 204, 204, 204, 20, 64, 0, 0, 0, 0, 0, 0, 240, 63, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 64, 3, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
        return Preset_2880_1920;
    }


    public static void OnGazeCallback(ref _7i_eye_data_ex_t eyes, IntPtr context)
    {
        //ShowDebugData(eyes);
        //Los datos de los ojos se guardan siempre: la ventana de calibración los muestra aunque todavía no haya calibración.
        lock (sampleLock)
        {
            eyesData = eyes;
        }
        if (!HasValidCalibration) return;

        float xDist = (eyes.left_gaze.gaze_origin.x + eyes.right_gaze.gaze_origin.x) / 2;
        float yDist = (eyes.left_gaze.gaze_origin.y + eyes.right_gaze.gaze_origin.y) / 2;
        float zDist = (eyes.left_pupil.pupil_distance + eyes.right_pupil.pupil_distance) / 2;   //Este valor está en mm.
        gazeOrigin = new Vector3(xDist, yDist, zDist);

        bool valid = 1 == _is_valid_recom_eye_gaze_point(ref eyes);
        float x = eyes.recom_gaze.gaze_point.x;
        float y = eyes.recom_gaze.gaze_point.y;

        //-1 es el valor que devuelve el SDK cuando el punto no es válido.
        if (valid && (!float.IsFinite(x) || !float.IsFinite(y) || x == -1f || y == -1f || (x == 0f && y == 0f)))
        {
            valid = false;
        }

        lock (sampleLock)
        {
            if (valid)
            {
                gazePointChanged = true;
                gaze_x = x;
                gaze_y = y;

                //Promedio de ambos ojos; si uno no es válido se usa el otro, y si ninguno, el recomendado.
                bool leftOk = IsValidPoint(eyes.left_gaze.gaze_point);
                bool rightOk = IsValidPoint(eyes.right_gaze.gaze_point);
                if (leftOk && rightOk)
                {
                    rawGaze_x = (eyes.left_gaze.gaze_point.x + eyes.right_gaze.gaze_point.x) * 0.50f;
                    rawGaze_y = (eyes.left_gaze.gaze_point.y + eyes.right_gaze.gaze_point.y) * 0.50f;
                }
                else if (leftOk)
                {
                    rawGaze_x = eyes.left_gaze.gaze_point.x;
                    rawGaze_y = eyes.left_gaze.gaze_point.y;
                }
                else if (rightOk)
                {
                    rawGaze_x = eyes.right_gaze.gaze_point.x;
                    rawGaze_y = eyes.right_gaze.gaze_point.y;
                }
                else
                {
                    rawGaze_x = x;
                    rawGaze_y = y;
                }
                UserIsPresentStatic = true;
            }
            else
            {
                UserIsPresentStatic = false;
            }
        }



        //GazeManager.I.UpdatePointer(gaze_x, gaze_y);
    }

    private static void ShowDebugData(_7i_eye_data_ex_t eyes)
    {
        Debug.WriteLine($"Gaze Origin: X {eyes.recom_gaze.gaze_origin.x} Z {eyes.recom_gaze.gaze_origin.z} " +
              $"GazeOriginLeft: X {eyes.left_gaze.gaze_origin.x} Z {eyes.left_gaze.gaze_origin.z} " +
              $"PupilDistLeft: {eyes.left_pupil.pupil_distance} " +
              $"LeftDiameter: {eyes.left_pupil.pupil_diameter} " +
              $"LeftDmm: {eyes.left_pupil.pupil_diameter_mm}");
        Debug.WriteLine("Right pupil diameter: " + eyes.right_pupil.pupil_diameter);
        Debug.WriteLine("Right pupil distance: " + eyes.right_pupil.pupil_distance);
        Debug.WriteLine("Right pupil center X: " + eyes.right_pupil.pupil_center.x);
        Vector3 direction = new Vector3(eyes.right_gaze.gaze_direction.x, eyes.right_gaze.gaze_direction.y, eyes.right_gaze.gaze_direction.z);
        Debug.WriteLine("Right gaze vector length: " + direction.Length());
        Debug.WriteLine("Right pupil center XYZ: " + eyes.right_gaze.gaze_origin.x);
    }

    public static int _is_valid_recom_eye_gaze_point(ref _7i_eye_data_ex_t eyes)
    {
        return (int)_get_valid_value((byte)_7I_EYE_GAZE_VALIDITY.ID_EYE_GAZE_POINT, eyes.recom_gaze.gaze_bit_mask);
    }

    public static UInt32 _get_valid_value(byte position, UInt32 bits)
    {
        UInt32 the_mask = (((UInt32)1) << position);
        return (the_mask &= bits) >> position;
    }

    public static int _is_valid_left_eye_gaze_point(ref _7i_eye_data_ex_t eyes)
    {
        return (int)_get_valid_value((byte)_7I_EYE_GAZE_VALIDITY.ID_EYE_GAZE_POINT, eyes.left_gaze.ex_data_bit_mask);
    }

    public static int _is_valid_right_eye_gaze_point(ref _7i_eye_data_ex_t eyes)
    {
        return (int)_get_valid_value((byte)_7I_EYE_GAZE_VALIDITY.ID_EYE_GAZE_POINT, eyes.right_gaze.ex_data_bit_mask);
    }

    public void UpdateData()
    {
        UpdateGazePoint();
        UpdateEyeData();
    }

    static bool IsValidPoint(_7i_point3d_t p)
    {
        return float.IsFinite(p.x) && float.IsFinite(p.y) && p.x != -1f && p.y != -1f && !(p.x == 0f && p.y == 0f);
    }

    void UpdateGazePoint()
    {
        if (!HasValidCalibration) return;

        lock (sampleLock)
        {
            //Se consume la muestra: si no llega una nueva, no se re-emite la anterior con un timestamp nuevo.
            if (!gazePointChanged) return;
            gazePointChanged = false;

            gazePointCache = new GazePoint(new Vector2(gaze_x, gaze_y));
            rawGazePointCache = new GazePoint(new Vector2(rawGaze_x, rawGaze_y));
        }
    }

    void UpdateEyeData()
    {

        if (eyesCache == null)
        {
            eyesCache = new EyesData();
        }
        if (eyesCache.leftEye == null)
        {
            eyesCache.leftEye = new Eye();
        }
        if (eyesCache.rightEye == null)
        {
            eyesCache.rightEye = new Eye();
        }
        _7i_eye_data_ex_t eyesData;
        lock (sampleLock)
        {
            eyesData = GazeDeviceA11.eyesData;
        }
        eyesCache.leftEye.isBlinking = eyesData.left_ex_data.blink == 0 ? false : true;
        eyesCache.leftEye.viewportX = eyesData.left_pupil.pupil_center.x;
        eyesCache.leftEye.viewportY = eyesData.left_pupil.pupil_center.y;
        eyesCache.leftEye.pupilDistanceMm = eyesData.left_pupil.pupil_distance;
        eyesCache.leftEye.pupilDiameter = eyesData.left_pupil.pupil_diameter;
        eyesCache.leftEye.pupilDiameterMm = eyesData.left_pupil.pupil_diameter_mm;

        eyesCache.rightEye.isBlinking = eyesData.right_ex_data.blink == 0 ? false : true;
        eyesCache.rightEye.viewportX = eyesData.right_pupil.pupil_center.x;
        eyesCache.rightEye.viewportY = eyesData.right_pupil.pupil_center.y;
        eyesCache.rightEye.pupilDistanceMm = eyesData.right_pupil.pupil_distance;
        eyesCache.rightEye.pupilDiameter = eyesData.right_pupil.pupil_diameter;
        eyesCache.rightEye.pupilDiameterMm = eyesData.right_pupil.pupil_diameter_mm;
    }

    public static void LoadSmooth()
    {
        int smooth = Settings.I.SmoothFilter.Value;
        int smoothValue = Math.Clamp(smooth, 1, 10);
        int setSmoothResult = ASeeTracker._7i_set_smooth(smoothValue);
        Debug.WriteLine($"Setting Smooth to: {smoothValue} SetSmooth result: " + aSeeResults.SetSmoothResultToString(setSmoothResult));
    }

    public static void SetSmooth(int smooth)
    {
        int smoothValue = Math.Clamp(smooth, 1, 10);
        int setSmoothResult = ASeeTracker._7i_set_smooth(smoothValue);
        Debug.WriteLine($"Setting Smooth to: {smoothValue} SetSmooth result: " + aSeeResults.SetSmoothResultToString(setSmoothResult));
        Settings.I.SmoothFilter.Value = smoothValue;
        Settings.I.SaveSettings();
    }




    public void OpenCalibrationPage()
    {
        WindowManager.OpenWindow<CalibrationWindow>();
    }

    public void RequestCalibration(int pointsArray, int eyes)
    {
        Settings.I.LastPointsOption.Value = pointsArray;
        Settings.I.LastEyesOption.Value = eyes;
        OpenCalibrationPage();
        GlobalEvents.OnStartCalibrationCommand.Invoke(pointsArray, eyes);
    }

    public void ShowCameraPreview(bool open)
    {
        if (open)
        {
            ShowCamera();
        }
        else HideCamera();
    }

    void ShowCamera()
    {
        WindowManager.OpenWindow<EyesWindow>();
    }

    void HideCamera()
    {
        WindowManager.CloseWindow<EyesWindow>();
    }

}
