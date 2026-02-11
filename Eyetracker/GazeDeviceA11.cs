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
    public static _7i_eye_data_ex_t eyes;

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

    void GetCoefficient()
    {
        coefficient = new _7i_coefficient_t();
        coefficient.buf = Settings.I.LastCalibrationBuff.Value;
        if (coefficient.buf == null || coefficient.buf.Length == 0)
        {
            coefficient.buf = GetDefaultCalibration();
        }
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
            rawGaze_x = eyes.recom_gaze.raw_point.x;
            rawGaze_y = eyes.recom_gaze.raw_point.y;
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
        gaze_x = Math.Clamp(gaze_x, 0, 1);
        gaze_y = Math.Clamp(gaze_y, 0, 1);
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
        eyesCache.leftEye.isBlinking = eyes.left_ex_data.blink == 0 ? false : true;
        eyesCache.leftEye.viewportX = eyes.left_pupil.pupil_center.x;
        eyesCache.leftEye.viewportY = 1f - eyes.left_pupil.pupil_center.y;
        eyesCache.leftEye.pupilDistanceMm = eyes.left_pupil.pupil_distance;
        eyesCache.leftEye.pupilDiameter = eyes.left_pupil.pupil_diameter;
        eyesCache.leftEye.pupilDiameterMm = eyes.left_pupil.pupil_diameter_mm;

        eyesCache.rightEye.isBlinking = eyes.right_ex_data.blink == 0 ? false : true;
        eyesCache.rightEye.viewportX = eyes.right_pupil.pupil_center.x;
        eyesCache.rightEye.viewportY = 1f - eyes.right_pupil.pupil_center.y;
        eyesCache.rightEye.pupilDistanceMm = eyes.right_pupil.pupil_distance;
        eyesCache.rightEye.pupilDiameter = eyes.right_pupil.pupil_diameter;
        eyesCache.rightEye.pupilDiameterMm = eyes.right_pupil.pupil_diameter_mm;
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
        IsConnected = false;
        int stopTrackingResult = ASeeTracker._7i_stop_tracking();
        Debug.WriteLine("StopTracking result: " + aSeeResults.StopTrackingResultToString(stopTrackingResult));
        int stopResult = ASeeTracker._7i_stop();
        Debug.WriteLine("Stop result: " + aSeeResults.StopResultToString(stopResult));
        int disconnectResult = ASeeTracker._7i_device_disconnect();
        Debug.WriteLine("Disconnect result: " + aSeeResults.DisconnectResultToString(disconnectResult));
        GlobalEvents.OnEyetrackerDisconnected.Invoke();
    }

    public byte[] GetDefaultCalibration()
    {
        byte[] calibBuff = Convert.FromBase64String("jRMAAMGGAQAAAABg62EiQAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAyMDI0MDkwNF8xMDI0X0NPTVBVVEVDSEFOR0UAAAAAAM3MTD0zM3M/AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAACamflAZmZ2QEjhqj8AAAAAAI0OlDfHXr+h53OqjivXvwAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAOAeGAStkrE/YiBl9yyt878AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA0nmLIZ032j8EHn844LH8PzMzMzMzMx9AzczMzMzMEEBmZmZmZmb1P83MzMzMzBRAAAAAAAAA8D+NEwAAwYYBAAAAAIC6DCNAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAADIwMjQwOTA0XzEwMjRfQ09NUFVURUNIQU5HRQAAAAAAzcxMPTMzcz8AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAJqZ+UBmZnZASOGqPwAAAACAkQEWMqpdP2Dq5a6CGde/AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA0OkpUJqfqr/RXh1bdE/zvwAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAABX+A/jemjfP/VofG+a+eQ/MzMzMzMzH0DNzMzMzMwQQGZmZmZmZvU/zczMzMzMFEAAAAAAAADwPwAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA==");
        return calibBuff;
    }
}
