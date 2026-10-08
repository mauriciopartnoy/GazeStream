using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Diagnostics;

namespace SkyleAvaloniaExample.Interop;

/// <summary>Decoded gaze sample handed to the UI.</summary>
public class GazeSnapshot
{
    public float X { get; }
    public float Y { get; }
    public SkyleEyeMovementType Movement { get; }
    public bool Valid { get; }

    public GazeSnapshot(float x, float y, SkyleEyeMovementType movement, bool valid)
    {
        X = x; Y = y; Movement = movement; Valid = valid;
    }
}

/// <summary>One positioning frame (full face structure) for the diagnostic view.</summary>
public class PositioningSnapshot
{
    public SkyleComplexFace Face { get; }
    public PositioningSnapshot(SkyleComplexFace face) => Face = face;
}

/// <summary>One decoded video frame; pixels copied out of the transient native buffer.</summary>
public class VideoFrame
{
    public int Width { get; }
    public int Height { get; }
    public int Channels { get; }
    public byte[] Pixels { get; }

    public VideoFrame(int width, int height, int channels, byte[] pixels)
    {
        Width = width; Height = height; Channels = channels; Pixels = pixels;
    }
}

/// <summary>
/// Managed wrapper over the skylelib C client: owns the platform transport,
/// marshals callbacks off the library's background I/O thread, and raises plain
/// C# events. All events fire on a NON-UI thread — subscribers must marshal.
/// </summary>
public class SkyleClient : IDisposable
{
    private const ushort SkyleVendorId = 0x3729;
    private const ushort SkyleProductId = 0x7333;

    // Native callback delegate types (cdecl). Instances are stored in fields so
    // the GC cannot collect them while native code holds their function pointers.
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void GazeCb(IntPtr client, IntPtr gaze, IntPtr user);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void PositioningCb(IntPtr client, IntPtr positioning, IntPtr user);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void VideoCb(IntPtr client, IntPtr video, IntPtr user);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void StateCb(IntPtr client, int oldState, int newState, IntPtr user);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void VersionCb(IntPtr client, IntPtr version, IntPtr user);



    //Calibration callbacks
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void CalibrationPointCb(IntPtr client, IntPtr version, IntPtr user);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void CalibrationProgressCb(IntPtr client, IntPtr version, IntPtr user);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void CalibrationPausedCb(IntPtr client, IntPtr user);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void CalibrationFinishedCb(IntPtr client, IntPtr version, IntPtr user);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void CalibrationAbortedCb(IntPtr client, IntPtr user);

    private IntPtr _client;
    private IntPtr _transport;
    private bool _disposed;

    private GazeCb? _gazeCb;
    private PositioningCb? _posCb;
    private VideoCb? _videoCb;
    private StateCb? _stateCb;
    private VersionCb? _versionCb;

    private CalibrationPointCb? _calibrationPointCb;
    private CalibrationProgressCb? _calibrationProgressCb;
    private CalibrationPausedCb? _calibrationPausedCb;
    private CalibrationFinishedCb? _calibrationFinishedCb;
    private CalibrationAbortedCb? _calibrationAbortedCb;
    //TODO: add ControlCb for control messages.


    public event Action<GazeSnapshot>? GazeReceived;
    public event Action<PositioningSnapshot>? PositioningReceived;
    public event Action<VideoFrame>? VideoReceived;
    public event Action<SkyleConnectionState>? StateChanged;
    public event Action<SkyleControlMessage>? OnControlSend;
    public event Action<string>? VersionReceived;

    public event Action<SkyleNextCalibrationPoint>? CalibrationPointReceived;
    public event Action<SkyleCollectingCalibrationPoints>? CalibrationProgressReceived;
    public event Action? CalibrationPausedReceived;
    public event Action<SkyleFinishedCalibration>? CalibrationFinishedReceived;
    public event Action? CalibrationAbortedReceived;


    public void Start()
    {
        VerifyStructLayouts();

        _client = NativeMethods.skyle_client_get_instance();
        if (_client == IntPtr.Zero)
            throw new InvalidOperationException("skyle_client_get_instance() returned NULL.");

        SetupTransport();   // phase 1: transport (starts background I/O thread)
        SetupCallbacks();   // phase 2: message callbacks

        // Skyle Link supervisor (callback-less mode): the app keeps owning the
        // registered USB transport; the supervisor elects OWNER (serve a hub over
        // it) or CLIENT (share the tracker over a loopback socket) and stashes /
        // restores the transport across the swaps itself.
        NativeMethods.skyle_link_set_identity("skyle-avalonia-example", 2 /* SKYLE_LINK_TIER_DEFAULT */, true);
        NativeMethods.skyle_link_set_supervisor_enabled(true);

        // While on a local link the supervisor owns the connection; a manual
        // connect would fight it (never force-disconnect while supervised).

        bool isLocalLink = NativeMethods.skyle_client_is_local_link(_client);
        Debug.WriteLine("Is Local Link: " + isLocalLink);
        if (!isLocalLink)
        {
            int result = NativeMethods.skyle_client_connect(_client);
            Debug.WriteLine("Connect result: " + result);
        }
    }

    private void SetupTransport()
    {
        try
        {
            IntPtr writePtr, readPtr, checkPtr;

            var cfg = new SkyleTransportUsbConfig
            {
                VendorId = SkyleVendorId,
                ProductId = SkyleProductId,
                TimeoutMs = 1000,
                Verbose = false,
            };
            NativeMethods.skyle_transport_usb_destroy(_transport);
            _transport = NativeMethods.skyle_transport_usb_create(ref cfg);
            writePtr = NativeLibrary.GetExport(NativeMethods.LibraryHandle, "skyle_transport_usb_write");
            readPtr = NativeLibrary.GetExport(NativeMethods.LibraryHandle, "skyle_transport_usb_read");
            checkPtr = NativeMethods.skyle_transport_usb_get_check_callback();

            var transport = new SkyleTransportConfig
            {
                TransportWrite = writePtr,
                TransportRead = readPtr,
                TransportUserData = _transport,
                UsbDeviceCheck = checkPtr,
                ConnectTimeoutMs = 10000,
                ReconnectIntervalMs = 2000,
                Verbose = false,
                Trace = false,
            };
            int result = NativeMethods.skyle_client_set_transport(_client, ref transport);

            Debug.WriteLine("Result of Skyle set transport: " + result);
        }
        catch
        {
            Debug.WriteLine("An exception occured: Was not able to setup transport.");
        }

    }

    private void SetupCallbacks()
    {
        _gazeCb = OnGaze;
        _posCb = OnPositioning;
        _videoCb = OnVideo;
        _stateCb = OnState;
        _versionCb = OnVersion;

        _calibrationPointCb = OnCalibrationPoint;
        _calibrationProgressCb = OnCalibrationProgress;
        _calibrationPausedCb = OnCalibrationPaused;
        _calibrationFinishedCb = OnCalibrationFinished;
        _calibrationAbortedCb = OnCalibrationAborted;

        var callbacks = new SkyleCallbackConfig
        {
            OnGaze = Marshal.GetFunctionPointerForDelegate(_gazeCb),
            OnPositioning = Marshal.GetFunctionPointerForDelegate(_posCb),
            OnVideo = Marshal.GetFunctionPointerForDelegate(_videoCb),
            OnStateChange = Marshal.GetFunctionPointerForDelegate(_stateCb),
            OnVersion = Marshal.GetFunctionPointerForDelegate(_versionCb),

            OnCalibrationPoint = Marshal.GetFunctionPointerForDelegate(_calibrationPointCb),
            OnCalibrationProgress = Marshal.GetFunctionPointerForDelegate(_calibrationProgressCb),
            OnCalibrationPaused = Marshal.GetFunctionPointerForDelegate(_calibrationPausedCb),
            OnCalibrationFinished = Marshal.GetFunctionPointerForDelegate(_calibrationFinishedCb),
            OnCalibrationAborted = Marshal.GetFunctionPointerForDelegate(_calibrationAbortedCb),


            UserData = IntPtr.Zero,
        };
        int result = NativeMethods.skyle_client_set_callbacks(_client, ref callbacks);
        Debug.WriteLine("Result of Skyle set callbacks: " + result);

    }

    // ---- streaming control (safe to call from any thread) ----

    public void EnableGaze(bool enable) => SafeCall(() => NativeMethods.skyle_client_enable_gaze(_client, enable));
    public void EnablePositioning(bool enable) => SafeCall(() => NativeMethods.skyle_client_enable_positioning(_client, enable));
    public void EnableControl(bool enable) => SafeCall(() => NativeMethods.skyle_client_enable_control(_client, enable));

    public void EnableVideo(bool enable) => SafeCall(() => NativeMethods.skyle_client_enable_video(_client, enable));

    private void SafeCall(Func<int> action)
    {
        Debug.WriteLine("Starting Skyle safe call");
        if (_client == IntPtr.Zero || _disposed) return;
        try
        {
            Debug.WriteLine("Safe call succesful");
            action();
        }
        catch { /* best-effort streaming toggles */ }
    }

    // ---- Skyle Link host control (commands to the hub-hosting Skyle app) ----

    /// <summary>
    /// Fire-and-forget host-control command over the local link. Returns the raw
    /// <c>skyle_result</c>; <see cref="SkyleResult.InvalidState"/> means this app
    /// is not a link client right now (e.g. it owns the tracker itself).
    /// </summary>
    public int SendHostControl(ushort controlId, byte[]? value)
    {
        Debug.WriteLine("Starting Skyle SendHostControl");
        if (_client == IntPtr.Zero || _disposed) return (int)SkyleResult.InvalidState;
        try
        {
            int result = NativeMethods.skyle_link_send_host_control(_client, controlId, value, (ushort)(value?.Length ?? 0));
            Debug.WriteLine("Skyle link SendHostControl result: " + result);
            return result;
        }
        catch { return (int)SkyleResult.Communication; }
    }

    public int SetHostMenuBarVisible(bool visible) => SendHostControl((ushort)SkyleLinkHostControl.MenuBar, new[] { visible ? (byte)1 : (byte)0 });

    public int SetHostPointerVisible(bool visible) => SendHostControl((ushort)SkyleLinkHostControl.PointerOverlay, new[] { visible ? (byte)1 : (byte)0 });

    public int StartHostCalibration() => SendHostControl((ushort)SkyleLinkHostControl.StartCalibration, null); // empty value = app default points

    // ---- native callbacks (run on the library's background I/O thread) ----

    private void OnCalibrationPoint(IntPtr client, IntPtr calibrationPoint, IntPtr user)
    {
        var r = Marshal.PtrToStructure<SkyleNextCalibrationPoint>(calibrationPoint);
        CalibrationPointReceived?.Invoke(r);
    }

    private void OnCalibrationProgress(IntPtr client, IntPtr progress, IntPtr user)
    {
        var r = Marshal.PtrToStructure<SkyleCollectingCalibrationPoints>(progress);
        CalibrationProgressReceived?.Invoke(r);
    }

    private void OnCalibrationPaused(IntPtr client, IntPtr user)
    {
        CalibrationPausedReceived?.Invoke();
    }

    private void OnCalibrationFinished(IntPtr client, IntPtr finishedData, IntPtr user)
    {
        var r = Marshal.PtrToStructure<SkyleFinishedCalibration>(finishedData);
        CalibrationFinishedReceived?.Invoke(r);
    }

    private void OnCalibrationAborted(IntPtr client, IntPtr user)
    {
        CalibrationAbortedReceived?.Invoke();
    }


    private void OnGaze(IntPtr client, IntPtr gaze, IntPtr user)
    {
        //Debug.WriteLine("Native On Gaze received");
        var r = Marshal.PtrToStructure<SkyleGazeResponse>(gaze);
        var both = r.Both;
        bool valid = both.Smoothed.X != 0f || both.Smoothed.Y != 0f;
        GazeReceived?.Invoke(new GazeSnapshot(both.Smoothed.X, both.Smoothed.Y, (SkyleEyeMovementType)both.Type, valid));
    }

    private void OnPositioning(IntPtr client, IntPtr positioning, IntPtr user)
    {
        var r = Marshal.PtrToStructure<SkylePositioningResponse>(positioning);
        PositioningReceived?.Invoke(new PositioningSnapshot(r.Face));
    }

    private void OnVideo(IntPtr client, IntPtr video, IntPtr user)
    {
        var r = Marshal.PtrToStructure<SkyleVideoResponse>(video);
        int len = (int)r.PixelDataLength;
        var buffer = new byte[len];
        if (r.PixelData != IntPtr.Zero && len > 0)
            Marshal.Copy(r.PixelData, buffer, 0, len); // copy now — pointer is transient
        VideoReceived?.Invoke(new VideoFrame(r.Width, r.Height, r.Channels, buffer));
    }

    private void OnControl(IntPtr client, SkyleControlMessage control)
    {
        Debug.WriteLine("On Control received");
        OnControlSend?.Invoke(control);
    }

    private void OnState(IntPtr client, int oldState, int newState, IntPtr user)
    {
        Debug.WriteLine("State change received");
        StateChanged?.Invoke((SkyleConnectionState)newState);
    }

    private void OnVersion(IntPtr client, IntPtr version, IntPtr user)
    {
        var r = Marshal.PtrToStructure<SkyleVersionResponse>(version);
        string fw = CString(r.Firmware);
        VersionReceived?.Invoke(string.IsNullOrEmpty(fw)
            ? $"Skyle · SN {r.Serial}"
            : $"Skyle · FW {fw} · SN {r.Serial}");
    }

    private static string CString(byte[]? raw)
    {
        if (raw == null) return string.Empty;
        int n = Array.IndexOf(raw, (byte)0);
        if (n < 0) n = raw.Length;
        return Encoding.UTF8.GetString(raw, 0, n);
    }

    /// <summary>Cheap guard against alignment drift between C and the C# mirrors.</summary>
    private static void VerifyStructLayouts()
    {
        Check<SkylePointF>(8);
        Check<SkyleRectF>(16);
        Check<SkyleRectU>(8);
        Check<SkyleRotatedRect>(20);
        Check<SkyleComplexFeature>(44);
        Check<SkyleComplexIris>(44);
        Check<SkyleComplexEye>(184);
        Check<SkyleComplexEyes>(368);
        Check<SkyleComplexFace>(384);

        static void Check<T>(int expected)
        {
            int actual = Marshal.SizeOf<T>();
            if (actual != expected)
                throw new InvalidOperationException(
                    $"FFI layout mismatch: {typeof(T).Name} is {actual} bytes, expected {expected}.");
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        // Deliberate supervisor stop first: OWNER hands the hub over, CLIENT
        // closes the socket - before the transport underneath goes away.
        try { NativeMethods.skyle_link_set_supervisor_enabled(false); } catch { /* ignore */ }

        if (_client != IntPtr.Zero)
        {
            try { NativeMethods.skyle_client_disconnect(_client); } catch { /* ignore */ }
            try { NativeMethods.skyle_client_stop_background(_client); } catch { /* ignore */ }
        }

        if (_transport != IntPtr.Zero)
        {
            try
            {
                if (OperatingSystem.IsMacOS()) NativeMethods.skyle_transport_iokit_destroy(_transport);
                else if (OperatingSystem.IsWindows()) NativeMethods.skyle_transport_usb_destroy(_transport);
            }
            catch { /* ignore */ }
            _transport = IntPtr.Zero;
        }

        // Delegates intentionally kept referenced until after the I/O thread has stopped.
        _gazeCb = null;
        _posCb = null;
        _videoCb = null;
        _stateCb = null;
        _versionCb = null;

        _calibrationPointCb = null;
        _calibrationProgressCb = null;
        _calibrationPausedCb = null;
        _calibrationFinishedCb = null;
        _calibrationAbortedCb = null;
    }
}
