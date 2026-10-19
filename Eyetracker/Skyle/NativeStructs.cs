using System;
using System.Runtime.InteropServices;

namespace SkyleAvaloniaExample.Interop;

// ============================================================================
// Blittable mirrors of the C structs from include/skylelib/messages/...
//
// IMPORTANT FFI notes:
//  * All callback payloads are delivered in HOST byte order (big-endian is a
//    wire-only concern, decoded inside the library), so these marshal directly.
//  * Every C `bool` is 1 byte -> [MarshalAs(U1)] bool (never the default 4-byte BOOL).
//  * LayoutKind.Sequential applies the same natural alignment as C
//    (e.g. the int64 timestamp lands at offset 8; skyle_complex_gaze pads to 20).
//  The struct sizes in the comments are asserted at runtime in SkyleClient.
// ============================================================================

[StructLayout(LayoutKind.Sequential)]
public struct SkylePointF // 8 bytes
{
    public float X;
    public float Y;
}

[StructLayout(LayoutKind.Sequential)]
public struct SkyleSizeF // 8 bytes
{
    public float Width;
    public float Height;

    public SkyleSizeF()
    {
        Width = default;
        Height = default;
    }

    public SkyleSizeF(float width, float height)
    {
        this.Width = width;
        this.Height = height;
    }
}

[StructLayout(LayoutKind.Sequential)]
public struct SkyleSizeU
{
    public ushort Width;
    public ushort Height;

    public SkyleSizeU()
    {
        Width = default;
        Height = default;
    }

    public SkyleSizeU(float width, float height)
    {
        this.Width = (ushort)width;
        this.Height = (ushort)height;
    }
}

[StructLayout(LayoutKind.Sequential)]
public struct SkyleRectF // 16 bytes
{
    public float Top;
    public float Left;
    public float Bottom;
    public float Right;
}

[StructLayout(LayoutKind.Sequential)]
public struct SkyleRectU // 8 bytes
{
    public ushort Top;
    public ushort Left;
    public ushort Bottom;
    public ushort Right;
}

[StructLayout(LayoutKind.Sequential)]
public struct SkyleRotatedRect // 20 bytes
{
    public SkylePointF Center;
    public SkyleSizeF Size;
    public float Angle; // degrees, OpenCV convention
}

[StructLayout(LayoutKind.Sequential)]
public struct SkyleMessageHeader // 24 bytes
{
    public ushort MessageType;
    public ushort PayloadLength;
    public long TimestampMs;
    [MarshalAs(UnmanagedType.U1)] public bool HasTimestamp;
}

[StructLayout(LayoutKind.Sequential)]
public struct SkyleControlMessage
{
    public SkyleMessageHeader Header;

    [MarshalAs(UnmanagedType.U1)]
    public bool IsStandbyEnabled;       // byte 0

    [MarshalAs(UnmanagedType.U1)]
    public bool IsAutoPauseEnabled;    // byte 1

    [MarshalAs(UnmanagedType.U1)]
    public bool IsPauseEnabled;        // byte 2

    public byte TrackingMode;          // byte 3
    public byte GazeFilter;            // byte 4
    public byte FixationFilter;        // byte 5

    [MarshalAs(UnmanagedType.U1)]
    public bool IsAssistiveTouchEnabled; // byte 6

    [MarshalAs(UnmanagedType.U1)]
    public bool ShowTrackingDetails;     // byte 7

    [MarshalAs(UnmanagedType.U1)]
    public bool IsHidEnabled;            // byte 8

    [MarshalAs(UnmanagedType.U1)]
    public bool IsEthernetEnabled;       // byte 9
}

// ---- Gaze ----

[StructLayout(LayoutKind.Sequential)]
public struct SkyleComplexGaze // 20 bytes (17 on the wire, padded in memory)
{
    public SkylePointF Raw;
    public SkylePointF Smoothed;
    public byte Type; // SkyleEyeMovementType
}

[StructLayout(LayoutKind.Sequential)]
public struct SkyleGazeResponse
{
    public SkyleMessageHeader Header;
    public SkyleComplexGaze Left;
    public SkyleComplexGaze Right;
    public SkyleComplexGaze Both;
}

// ---- Positioning ----

[StructLayout(LayoutKind.Sequential)]
public struct SkyleComplexFeature // 44 bytes
{
    public SkylePointF Center;
    public SkyleRectF BoundingRect;
    public SkyleRotatedRect Ellipse;
}

[StructLayout(LayoutKind.Sequential)]
public struct SkyleComplexIris // 44 bytes
{
    public SkylePointF Center;
    public SkylePointF Top;
    public SkylePointF Left;
    public SkylePointF Right;
    public SkylePointF Bottom;
    public float DistanceMm;
}

[StructLayout(LayoutKind.Sequential)]
public struct SkyleComplexEye // 184 bytes
{
    public SkyleRectU BoundingRect;   // image-space (uint16)
    public SkyleComplexFeature Pupil;
    public SkyleComplexFeature LeftGlint;
    public SkyleComplexFeature RightGlint;
    public SkyleComplexIris Iris;
}

[StructLayout(LayoutKind.Sequential)]
public struct SkyleComplexEyes // 368 bytes
{
    public SkyleComplexEye Left;
    public SkyleComplexEye Right;
}

[StructLayout(LayoutKind.Sequential)]
public struct SkyleComplexFace // 384 bytes
{
    public SkyleRectF BoundingRect;   // screen-space
    public SkyleComplexEyes Eyes;
}

[StructLayout(LayoutKind.Sequential)]
public struct SkylePositioningResponse
{
    public SkyleMessageHeader Header;
    public SkyleComplexFace Face;
}

// ---- Video ----

[StructLayout(LayoutKind.Sequential)]
public struct SkyleVideoResponse
{
    public ushort Width;
    public ushort Height;
    public byte Channels;
    public IntPtr PixelData;        // valid only during the callback
    public uint PixelDataLength;
}

// ---- Version ----

[StructLayout(LayoutKind.Sequential)]
public struct SkyleVersionResponse
{
    public SkyleMessageHeader Header;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)] public byte[] Firmware;
    public ulong Serial;
    [MarshalAs(UnmanagedType.U1)] public bool IsDemoDevice;
    public byte DeviceType;
    public byte DevicePlatform;
    public byte DeviceGeneration;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)] public byte[] ProtocolVersion;
}

// ---- Transport / callback configuration (passed to the library) ----

[StructLayout(LayoutKind.Sequential)]
public struct SkyleTransportIokitConfig
{
    public ushort VendorId;
    public ushort ProductId;
    public uint TimeoutMs;
    [MarshalAs(UnmanagedType.U1)] public bool Verbose;
}

[StructLayout(LayoutKind.Sequential)]
public struct SkyleTransportUsbConfig
{
    public ushort VendorId;
    public ushort ProductId;
    public uint TimeoutMs;
    [MarshalAs(UnmanagedType.U1)] public bool Verbose;
}

[StructLayout(LayoutKind.Sequential)]
public struct SkyleTransportConfig
{
    public IntPtr TransportWrite;       // native fn pointer
    public IntPtr TransportRead;        // native fn pointer
    public IntPtr TransportUserData;    // the transport handle
    public IntPtr UsbDeviceCheck;       // native fn pointer
    public uint ConnectTimeoutMs;
    public uint ReconnectIntervalMs;
    [MarshalAs(UnmanagedType.U1)] public bool Verbose;
    [MarshalAs(UnmanagedType.U1)] public bool Trace;
}

[StructLayout(LayoutKind.Sequential)]
public struct SkyleCallbackConfig
{
    public IntPtr OnGaze;
    public IntPtr OnPositioning;
    public IntPtr OnVersion;
    public IntPtr OnControl;
    public IntPtr OnCalibrationPoint;
    public IntPtr OnCalibrationProgress;
    public IntPtr OnCalibrationPaused;
    public IntPtr OnCalibrationFinished;
    public IntPtr OnCalibrationAborted;
    public IntPtr OnVideo;
    public IntPtr OnFileStatus;
    public IntPtr OnLogging;
    public IntPtr OnStateChange;
    public IntPtr OnError;
    public IntPtr UserData;
}

[StructLayout(LayoutKind.Sequential)]
public struct SkyleCalibrationConfig
{
    public ushort PointsCount;
    public IntPtr Points;

    public ushort CoordinatesCount;
    public IntPtr Coordinates;

    public SkyleSizeU Resolution; //Screen pixels
    public SkyleSizeF Size;       //Physical size in mm

    [MarshalAs(UnmanagedType.U1)]
    public bool Improve;
}

[StructLayout(LayoutKind.Sequential)]
public struct SkyleNextCalibrationPoint
{
    public SkyleMessageHeader Header;
    public byte Index;
    public SkylePointF Point;
}

[StructLayout(LayoutKind.Sequential)]
public struct SkyleCollectingCalibrationPoints
{
    public SkyleMessageHeader Header;
    public byte Index;
    public byte Progress;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct SkyleQualityPoint
{
    public byte Index;
    public SkylePointF Accuracy;
    public float Precision;
    public byte Quality;
}

[StructLayout(LayoutKind.Sequential)]
public struct SkyleFinishedCalibration
{
    public SkyleMessageHeader header;

    public ushort left_count;
    public IntPtr left;

    public ushort right_count;
    public IntPtr right;

    ////Los siguientes campos aparecian en la documentación pero no en los includes de c++. ¬¬ 
    //[MarshalAs(UnmanagedType.I1)]
    //public bool HasDetails;

    //[MarshalAs(UnmanagedType.I1)]
    //public bool Success;

    //public SkyleCalibrationFailureReason Reason;

    //public float ZeroQualityAccuracyDeg;
}
