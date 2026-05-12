using System.Runtime.InteropServices;
using System.Diagnostics;
using GazeStream.Eyetracker;
using GazeStream.Utilities;
using GazeStream.Utilities.Events;
using System.Numerics;
using GazeStream.AppData;
using GazeStream.Windows;
using System.Text;
public class GazeDeviceA11 : IGazeDevice
{
    public string DeviceName => "Eyetracker Joaco A11";
    public bool IsConnected { get; private set; }
    public bool UserIsPresent => UserIsPresentStatic;
    public EyesData Eyes => eyesCache;


    GazePoint gazePointCache;
    GazePoint rawGazePointCache;

    _7i_coefficient_t coefficient;
    EyesData eyesCache = new();
    static _7i_eye_data_ex_t eyes;
    public static _7i_eye_data_ex_t eyesData;

    static ASeeTracker.gazeCallback gazeCB = new ASeeTracker.gazeCallback(OnGazeCallback);
    static float gaze_x = 0.0f;
    static float gaze_y = 0.0f;
    static float rawGaze_x = 0.0f;
    static float rawGaze_y = 0.0f;
    static bool gazePointChanged;
    static bool UserIsPresentStatic;
    static bool HasValidCalibration;

    public GazePoint GazePoint => gazePointCache;

    public GazePoint RawGazePoint => rawGazePointCache;

    public bool Initialize()
    {
        Debug.WriteLine("About to initialize Joaco.");

        int startResult = ASeeTracker._7i_start(AppPaths.EyetrackerConfigPath);
        ASeeTracker._7i_set_gaze_callback(Marshal.GetFunctionPointerForDelegate(gazeCB), IntPtr.Zero);

        Debug.WriteLine("Initializing Joaco. Start result: " + startResult);
        if (startResult == 0)
        {
            IsConnected = true;
            GlobalEvents.OnEyetrackerConnected.Invoke();
            SetCurrentUserCalibration();
            return true;
        }
        else
        {
            GlobalEvents.OnEyetrackerConnectionFailed.Invoke();
            HasValidCalibration = false;
            Disconnect();
            return false;
        }

    }

    void SetCurrentUserCalibration()
    {
        Debug.WriteLine("Setting current user calibration");
        if (!IsConnected) return;
        //if (App.Instance.ActiveUser == null) return;
        //Debug.WriteLine("Current user available.");

        GetCoefficient();
        LoadSmooth();
        int startTrackingResult = ASeeTracker._7i_start_tracking(ref coefficient);
        HasValidCalibration = startTrackingResult == 0 ? true : false;
        Debug.WriteLine("Start tracking result: " + startTrackingResult);

        if (HasValidCalibration)
        {
            LoadSmooth();
        }
    }

    public void SetDefaultCalibration()
    {
        //IMPORTANTE: No todos los eyetrackers pueden calibrarse externamente por lo tanto el botón va a ser solo para el joaco.
        //Pero en caso de querer un botón mas general puede agregarse este metodo a la interfaz IGazeDevice.

        coefficient = new _7i_coefficient_t();
        coefficient.buf = GetDefaultCalibrationBuff();
        ASeeTracker._7i_start_tracking(ref coefficient);
        Settings.I.LastCalibrationBuff.Value = coefficient.buf;
    }

    void GetCoefficient()
    {
        coefficient = new _7i_coefficient_t();
        coefficient.buf = Settings.I.LastCalibrationBuff.Value;
        Debug.WriteLine("Las calibration buff: " + coefficient.buf.Length);

        ShowCoefficientBuffInDebugger();

        if (coefficient.buf == null || coefficient.buf.Length == 0)
        {

            coefficient.buf = GetDefaultCalibrationBuff();
            Debug.WriteLine("coe2 = " + coefficient.buf);
        }
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
        //CALIBRACIÓN PARA EL SDK ANTERIOR
        //byte[] defaultCalibration = new byte[] { 141, 19, 0, 0, 193, 134, 1, 0, 0, 0, 0, 192, 226, 172, 35, 64, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 50, 48, 50, 52, 48, 57, 48, 52, 95, 49, 48, 50, 52, 95, 67, 79, 77, 80, 85, 84, 69, 67, 72, 65, 78, 71, 69, 0, 0, 0, 0, 0, 85, 85, 85, 60, 247, 18, 122, 63, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 154, 153, 249, 64, 102, 102, 118, 64, 72, 225, 170, 63, 0, 0, 0, 0, 208, 151, 246, 188, 88, 153, 129, 191, 92, 73, 175, 19, 134, 192, 219, 191, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 128, 235, 122, 182, 93, 82, 149, 63, 89, 151, 138, 178, 46, 97, 246, 191, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 178, 79, 181, 119, 71, 8, 219, 63, 210, 216, 56, 49, 140, 129, 246, 63, 51, 51, 51, 51, 51, 51, 31, 64, 205, 204, 204, 204, 204, 204, 16, 64, 102, 102, 102, 102, 102, 102, 245, 63, 205, 204, 204, 204, 204, 204, 20, 64, 0, 0, 0, 0, 0, 0, 240, 63, 141, 19, 0, 0, 193, 134, 1, 0, 0, 0, 0, 96, 24, 111, 33, 64, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 50, 48, 50, 52, 48, 57, 48, 52, 95, 49, 48, 50, 52, 95, 67, 79, 77, 80, 85, 84, 69, 67, 72, 65, 78, 71, 69, 0, 0, 0, 0, 0, 85, 85, 85, 60, 247, 18, 122, 63, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 154, 153, 249, 64, 102, 102, 118, 64, 72, 225, 170, 63, 0, 0, 0, 0, 64, 63, 224, 125, 43, 109, 123, 191, 33, 27, 133, 108, 104, 59, 221, 191, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 54, 166, 81, 227, 240, 68, 189, 191, 246, 178, 138, 45, 169, 170, 244, 191, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 237, 249, 60, 24, 182, 114, 222, 63, 97, 121, 255, 70, 254, 108, 224, 63, 51, 51, 51, 51, 51, 51, 31, 64, 205, 204, 204, 204, 204, 204, 16, 64, 102, 102, 102, 102, 102, 102, 245, 63, 205, 204, 204, 204, 204, 204, 20, 64, 0, 0, 0, 0, 0, 0, 240, 63, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 64, 3, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

        //CALIBRACIÓN PARA LA ÚLTIMA VERSIÓN DEL SDK
        byte[] defaultCalibration = new byte[] { 232, 3, 0, 0, 232, 3, 0, 0, 0, 0, 0, 192, 116, 254, 35, 64, 0, 0, 0, 192, 116, 254, 35, 64, 0, 0, 0, 0, 0, 0, 240, 63, 0, 0, 0, 0, 0, 0, 240, 63, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 50, 48, 50, 53, 49, 50, 48, 49, 95, 50, 48, 52, 56, 95, 80, 79, 76, 89, 83, 67, 82, 69, 69, 78, 50, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 63, 0, 0, 0, 63, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 10, 89, 153, 167, 236, 215, 179, 63, 121, 221, 221, 34, 55, 30, 96, 191, 190, 96, 71, 181, 178, 43, 31, 64, 184, 207, 181, 235, 139, 243, 27, 64, 133, 235, 81, 184, 30, 133, 245, 63, 0, 0, 0, 0, 0, 0, 0, 0, 101, 125, 56, 130, 105, 117, 105, 64, 1, 18, 37, 139, 149, 111, 71, 64, 126, 123, 102, 35, 0, 186, 128, 192, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 154, 188, 27, 187, 62, 99, 239, 222, 214, 191, 70, 123, 128, 144, 40, 18, 186, 63, 196, 21, 108, 65, 96, 85, 187, 63, 24, 156, 213, 137, 103, 178, 99, 63, 0, 0, 0, 0, 0, 0, 178, 188, 234, 228, 10, 186, 10, 150, 228, 191, 177, 79, 88, 34, 116, 71, 150, 63, 250, 228, 129, 144, 132, 148, 192, 63, 99, 144, 120, 218, 62, 250, 167, 191, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 247, 230, 60, 193, 60, 89, 223, 63, 63, 77, 143, 200, 60, 125, 240, 63, 51, 51, 51, 51, 51, 51, 31, 64, 205, 204, 204, 204, 204, 204, 16, 64, 102, 102, 102, 102, 102, 102, 245, 63, 205, 204, 204, 204, 204, 204, 20, 64, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 232, 3, 0, 0, 232, 3, 0, 0, 0, 0, 0, 192, 116, 254, 35, 64, 0, 0, 0, 192, 116, 254, 35, 64, 0, 0, 0, 0, 0, 0, 240, 63, 0, 0, 0, 0, 0, 0, 240, 63, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 50, 48, 50, 53, 49, 50, 48, 49, 95, 50, 48, 52, 56, 95, 80, 79, 76, 89, 83, 67, 82, 69, 69, 78, 50, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 63, 0, 0, 0, 63, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 113, 26, 35, 234, 8, 230, 4, 192, 154, 74, 5, 134, 222, 98, 241, 191, 118, 50, 243, 81, 77, 51, 31, 64, 11, 81, 223, 251, 141, 119, 15, 64, 66, 21, 161, 179, 66, 51, 245, 63, 0, 0, 0, 0, 0, 0, 0, 0, 139, 98, 8, 17, 123, 128, 112, 64, 245, 133, 254, 6, 35, 30, 72, 64, 233, 239, 77, 178, 148, 183, 128, 192, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 128, 177, 60, 195, 60, 27, 139, 1, 210, 216, 191, 11, 193, 49, 195, 156, 134, 167, 63, 96, 95, 32, 175, 199, 47, 129, 63, 235, 33, 114, 64, 151, 158, 170, 63, 0, 0, 0, 0, 0, 0, 130, 188, 76, 206, 250, 111, 59, 35, 234, 191, 112, 23, 134, 219, 177, 2, 165, 63, 144, 55, 217, 165, 239, 232, 115, 191, 243, 140, 241, 185, 232, 80, 184, 191, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 221, 242, 10, 34, 160, 9, 215, 191, 213, 131, 141, 2, 236, 23, 238, 63, 51, 51, 51, 51, 51, 51, 31, 64, 205, 204, 204, 204, 204, 204, 16, 64, 102, 102, 102, 102, 102, 102, 245, 63, 205, 204, 204, 204, 204, 204, 20, 64, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

        Debug.WriteLine(defaultCalibration.Length);
        return defaultCalibration;
    }

    public static void OnGazeCallback(ref _7i_eye_data_ex_t eyes, IntPtr context)
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




        if (!HasValidCalibration) return;
        eyesData = eyes;
        if (1 == _is_valid_recom_eye_gaze_point(ref eyes) && eyes.recom_gaze.gaze_point.x != -1)
        {
            //if (eyes.recom_gaze.gaze_point.x == -1) return; //-1, -1 es el valor que devuelve cuando el punto es falopa.
            gazePointChanged = true;
            gaze_x = eyes.recom_gaze.gaze_point.x;
            gaze_y = eyes.recom_gaze.gaze_point.y;
            rawGaze_x = (eyes.left_gaze.gaze_point.x + eyes.right_gaze.gaze_point.x) * 0.50f;
            rawGaze_y = (eyes.left_gaze.gaze_point.y + eyes.right_gaze.gaze_point.y) * 0.50f;
            UserIsPresentStatic = true;
        }
        else
        {
            UserIsPresentStatic = false;
            gazePointChanged = false;
        }
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

    void UpdateGazePoint()
    {
        if (!HasValidCalibration) return;
        if (!gazePointChanged) return;
        if (!UserIsPresentStatic) return;
        gazePointCache = new GazePoint(new Vector2(gaze_x, gaze_y));
        rawGazePointCache = new GazePoint(new Vector2(rawGaze_x, rawGaze_y));
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
        int smoothValue = Math.Clamp(smooth, 0, 10);
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

    public void Disconnect()
    {
        try
        {
            int stopTrackingResult = ASeeTracker._7i_stop_tracking();
            Debug.WriteLine("StopTracking result: " + aSeeResults.StopTrackingResultToString(stopTrackingResult));
            int stopResult = ASeeTracker._7i_stop();
            Debug.WriteLine("Stop result: " + aSeeResults.StopResultToString(stopResult));
        }
        catch
        {

        }
        finally
        {
            int disconnectResult = ASeeTracker._7i_device_disconnect();
            Debug.WriteLine("Disconnect result: " + aSeeResults.DisconnectResultToString(disconnectResult));
            GlobalEvents.OnEyetrackerDisconnected.Invoke();
            IsConnected = false;
        }
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
}
