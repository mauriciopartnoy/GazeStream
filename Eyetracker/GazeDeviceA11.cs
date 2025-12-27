using System.Runtime.InteropServices;
using System;
using System.IO;
using System.Diagnostics;
using GazeStream.Eyetracker;
using GazeStream.Utilities;
using GazeStream.Utilities.Save;
using GazeStream.Utilities.Events;
using System.Numerics;
using GazeStream.AppData;
using GazeStream;
public class GazeDeviceA11 : IGazeDevice
{
    public string DeviceName => "Eyetracker Joaco A11";
    public bool IsConnected { get; private set; }
    public bool UserIsPresent => UserIsPresentStatic;


    GazePoint gazePointCache;
    _7i_coefficient_t coefficient;
    public byte[] buff = new byte[0];
    static _7i_eye_data_ex_t eyes;
    static ASeeTracker.gazeCallback gazeCB = new ASeeTracker.gazeCallback(OnGazeCallback);
    static float gaze_x = 0.0f;
    static float gaze_y = 0.0f;
    static bool gazePointChanged;
    static bool UserIsPresentStatic;
    static bool HasValidCalibration;

    public GazePoint GazePoint => gazePointCache;


    public bool Initialize()
    {
        int startResult = ASeeTracker._7i_start(AppPaths.EyetrackerConfigPath);
        ASeeTracker._7i_set_gaze_callback(Marshal.GetFunctionPointerForDelegate(gazeCB), IntPtr.Zero);

        Debug.WriteLine("Initializing Joaco. Start result: " + startResult);
        if (startResult == 0)
        {
            IsConnected = true;
            SetCurrentUserCalibration();
            return true;
        }
        else
        {
            HasValidCalibration = false;
            Disconnect();
            return false;
        }

    }

     void SetCurrentUserCalibration()
    {
        Debug.WriteLine("Setting current user calibration");
        if (!IsConnected) return;
        if (App.Instance.ActiveUser == null) return;
        Debug.WriteLine("Current user available.");

        if (!TryLoadCoefficient())
        {
            HasValidCalibration = false;
            Debug.WriteLine("Joaco Eyetracker must be calibrated before Initializing.");
            return;
        }

        ASeeTracker._7i_set_smooth(10);
        int startTrackingResult = ASeeTracker._7i_start_tracking(ref coefficient);
        HasValidCalibration = startTrackingResult == 0 ? true : false;
        Debug.WriteLine("Start tracking result: " + startTrackingResult);

        if (HasValidCalibration)
        {
            LoadSmooth();
        }
    }

    private bool TryLoadCoefficient()
    {
        coefficient = new _7i_coefficient_t();
        coefficient.buf = buff;
        //coefficient.buf = App.Instance.ActiveUser == null? new byte[0] : SaveManager.GetValue<byte[]>(SaveKeys.LAST_USER_CALIBRATION_KEY, new byte[0]);
        if (coefficient.buf.Length == 0) return false;
        else return true;
    }


    public static void OnGazeCallback(ref _7i_eye_data_ex_t eyes, IntPtr context)
    {
        if (!HasValidCalibration) return;

        if (1 == _is_valid_recom_eye_gaze_point(ref eyes) && eyes.recom_gaze.gaze_point.x != -1)
        {
            //if (eyes.recom_gaze.gaze_point.x == -1) return; //-1, -1 es el valor que devuelve cuando el punto es falopa.
            gazePointChanged = true;
            gaze_x = eyes.recom_gaze.gaze_point.x;
            gaze_y = eyes.recom_gaze.gaze_point.y;
            UserIsPresentStatic = true;
            //Debug.Log($"Valid Point: X: {gaze_x} Y: {gaze_y} ");

        }
        else
        {
            //Debug.Log($"Invalid Point: X: {eyes.recom_gaze.gaze_point.x} Y: {eyes.recom_gaze.gaze_point.y} ");
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
    }

    void UpdateGazePoint()
    {
        if (!HasValidCalibration) return;
        if (!gazePointChanged) return;
        if (!UserIsPresentStatic) return;
        gaze_x = Math.Clamp(gaze_x, 0, 1);
        gaze_y = Math.Clamp(gaze_y, 0, 1);
        gazePointCache = new GazePoint(new Vector2(gaze_x, gaze_y));
    }

    static bool IsInBounds(float viewportX, float viewportY)
    {
        if (viewportX < 0 || viewportX > 1 || viewportY < 0 || viewportY > 1)
        {
            return false;
        }
        return true;
    }

    public static void LoadSmooth()
    {
        int smooth = SaveManager.GetSystemSetting(SaveKeys.SMOOTH_VALUE_KEY, 10);
        int smoothValue = Math.Clamp(smooth, 0, 10);
        int setSmoothResult = ASeeTracker._7i_set_smooth(smoothValue);
        Debug.WriteLine("Set smooth result: " + setSmoothResult);
    }

    public static void SetSmooth(int smooth)
    {
        int smoothValue = Math.Clamp(smooth, 0, 10);
        int setSmoothResult = ASeeTracker._7i_set_smooth(smoothValue);
        Debug.WriteLine("Set smooth result: " + setSmoothResult);
        SaveManager.SetSystemSetting(SaveKeys.SMOOTH_VALUE_KEY, smoothValue);
        SaveManager.SaveSystemSettings();
        GlobalEvents.OnInvensunSmoothValueChanged.Invoke(smoothValue);
    }

    public void Disconnect()
    {
        IsConnected = false;
        ASeeTracker._7i_stop_tracking();
        ASeeTracker._7i_stop();
        ASeeTracker._7i_device_disconnect();
    }
}
