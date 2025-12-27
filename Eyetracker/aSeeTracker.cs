using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Threading;

public enum _7I_EYE_TYPE
{
    L_EYE = 1,
    R_EYE = 2
}

public enum _7I_MODE
{
    TRACK = 3,
    IRIS = 5
}

public enum _7I_GAZE_STATE
{
    NONE = 0,
    MOVING,
    MOVE_END,
    STAYING,
    STAY_END
}

public enum _7I_EYE_GAZE_VALIDITY
{
    ID_EYE_GAZE_POINT,        /*!<注视点坐标标识*/
    ID_EYE_GAZE_RAW_POINT,    /*!<注视点平滑前坐标标识*/
    ID_EYE_GAZE_SMOOTH_POINT, /*!<注视点平滑后坐标标识*/
    ID_EYE_GAZE_ORIGIN,       /*!<注视瞳孔中心坐标标识*/
    ID_EYE_GAZE_DIRECTION,    /*!<注视矢量标识*/
    ID_EYE_GAZE_RE            /*!<注视点re值*/
}

[StructLayout(LayoutKind.Sequential)]
public struct _7i_point2d_t
{
    public float x;
    public float y;
}

[StructLayout(LayoutKind.Sequential)]
public struct _7i_point3d_t
{
    public float x;
    public float y;
    public float z;
}

[StructLayout(LayoutKind.Sequential)]
public struct aSeeInitParam
{
    _7I_MODE mode;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 260)]
    public char[] path;
}

[StructLayout(LayoutKind.Sequential)]
public struct _7i_coefficient_t
{
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1024)]
    public byte[] buf;
}

[StructLayout(LayoutKind.Sequential)]
public struct _7i_gaze_point_t
{
    public UInt32 gaze_bit_mask;
    public _7i_point3d_t gaze_point;
    public _7i_point3d_t raw_point;
    public _7i_point3d_t smooth_point;
    public _7i_point3d_t gaze_origin;
    public _7i_point3d_t gaze_direction;
    public float re;
    public UInt32 ex_data_bit_mask;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
    public float[] ex_data;
}

[StructLayout(LayoutKind.Sequential)]
public struct _7i_pupil_info_t
{
    public UInt32 pupil_bit_mask;
    public _7i_point2d_t pupil_center;
    public float pupil_distance;
    public float pupil_diameter;
    public float pupil_diameter_mm;
    public float pupil_minor_axis;
    public float pupil_minor_axis_mm;
    public UInt32 ex_data_bit_mask;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
    public float[] ex_data;
}

[StructLayout(LayoutKind.Sequential)]
public struct _7i_eye_ex_data_t
{
    public UInt32 eye_data_ex_bit_mask;
    public Int32 blink;
    public float openness;
    public float eyelid_up;
    public float eyelid_down;
    public UInt32 ex_data_bit_mask;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
    public float[] ex_data;
}

[StructLayout(LayoutKind.Sequential)]
public struct _7i_fixation_saccade_t
{
    public Int64 timestamp;
    public Int64 duration;
    public Int32 count;
    public Int32 state;
    public _7i_point2d_t center;
}

[StructLayout(LayoutKind.Sequential)]
public struct _7i_eye_data_ex_t
{
    public UInt64 timestamp;
    public Int32 recommend;
    public _7i_gaze_point_t recom_gaze;
    public _7i_gaze_point_t left_gaze;
    public _7i_gaze_point_t right_gaze;
    public _7i_pupil_info_t left_pupil;
    public _7i_pupil_info_t right_pupil;
    public _7i_eye_ex_data_t left_ex_data;
    public _7i_eye_ex_data_t right_ex_data;
    public _7i_fixation_saccade_t stats;
}

[StructLayout(LayoutKind.Sequential)]
public struct _7i_date_time_t
{
    public Int16 year;
    public byte month;
    public byte day;
    public byte hour;
    public byte minute;
    public byte second;
}

[StructLayout(LayoutKind.Sequential)]
public struct _7i_ukey_info_t
{
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
    public byte[] device_id;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
    public byte[] app_id;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
    public byte[] version;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
    public byte[] tip_id;

    public _7i_date_time_t endTime;
}


public class ASeeTracker
{

    [DllImport("aSeeX.dll", EntryPoint = "_7i_set_camera_state_callback",
CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
    public static extern int _7i_set_camera_state_callback(IntPtr cb, IntPtr context);

    [DllImport("aSeeX.dll", EntryPoint = "_7i_set_image_callback",
CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
    public static extern int _7i_set_image_callback(IntPtr cb, IntPtr context);

    [DllImport("aSeeX.dll", EntryPoint = "_7i_set_gaze_callback",
CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
    public static extern int _7i_set_gaze_callback(IntPtr cb, IntPtr context);

    [DllImport("aSeeX.dll", EntryPoint = "_7i_set_screen_size",
CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
    public static extern int _7i_set_screen_size(double width, double height);

    [DllImport("aSeeX.dll", EntryPoint = "_7i_set_smooth",
CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
    public static extern int _7i_set_smooth(Int32 smooth);

    [DllImport("aSeeX.dll", EntryPoint = "_7i_device_connect",
CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
    public static extern int _7i_device_connect([MarshalAs(UnmanagedType.LPStr)] string password, ref _7i_ukey_info_t info);

    [DllImport("aSeeX.dll", EntryPoint = "_7i_device_disconnect",
CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
    public static extern int _7i_device_disconnect();

    [DllImport("aSeeX.dll", EntryPoint = "_7i_start",
CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
    public static extern int _7i_start([MarshalAs(UnmanagedType.LPStr)] string config_path);

    [DllImport("aSeeX.dll", EntryPoint = "_7i_stop",
CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
    public static extern int _7i_stop();

    [DllImport("aSeeX.dll", EntryPoint = "_7i_start_tracking",
CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
    public static extern int _7i_start_tracking(ref _7i_coefficient_t coe);

    [DllImport("aSeeX.dll", EntryPoint = "_7i_stop_tracking",
CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
    public static extern int _7i_stop_tracking();

    [DllImport("aSeeX.dll", EntryPoint = "_7i_start_calibration",
CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
    public static extern int _7i_start_calibration(Int32 points);

    [DllImport("aSeeX.dll", EntryPoint = "_7i_start_calibration_point",
CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
    public static extern int _7i_start_calibration_point(Int32 index, ref _7i_point2d_t point,
        IntPtr cb1, IntPtr context1, IntPtr cb2, IntPtr context2);

    [DllImport("aSeeX.dll", EntryPoint = "_7i_cancel_calibration",
CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
    public static extern int _7i_cancel_calibration();

    [DllImport("aSeeX.dll", EntryPoint = "_7i_compute_calibration",
CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
    public static extern int _7i_compute_calibration(ref _7i_coefficient_t out_coe);

    [DllImport("aSeeX.dll", EntryPoint = "_7i_complete_calibration",
CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
    public static extern int _7i_complete_calibration();

    [DllImport("aSeeX.dll", EntryPoint = "_7i_get_calibration_score",
CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
    public static extern int _7i_get_calibration_score(ref float left, ref float right);

    [DllImport("aSeeX.dll", EntryPoint = "_7i_set_calibration_mode",
CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
    public static extern int _7i_set_calibration_mode(Int32 mode);

    public delegate void imageCallback(int eye, IntPtr image, int size, int width, int height, Int64 timestamp, IntPtr context);

    public delegate void gazeCallback(ref _7i_eye_data_ex_t eyes, IntPtr context);

    public delegate void processCallback(int index, int percent, IntPtr context);

    public delegate void finishCallback(int index, int error, IntPtr context);
}

