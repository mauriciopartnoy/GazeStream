namespace SkyleAvaloniaExample.Interop;

/// <summary>Mirror of <c>skyle_connection_state</c> (skyle_client.h).</summary>
public enum SkyleConnectionState
{
    Disconnected = 0,
    WaitingPing,
    HandshakeSent,
    WaitingSyn,
    SynAckSent,
    Connected,
    WaitingStartAck,
    LinkSynced,
    Error,
}

/// <summary>Mirror of <c>skyle_result</c> (skyle_client.h).</summary>
public enum SkyleResult
{
    Ok = 0,
    NotFound = -1,
    Timeout = -2,
    InvalidState = -3,
    Communication = -4,
    Parse = -5,
    Memory = -6,
}

public enum SkyleCalibrationFailureReason : int
{
    Unspecified = 0,
    Timeout = 1,
    PointTimeout = 2,
    FitRejected = 3,
    OtherClientCalibrating = 4,
    ConfigurationRejected = 5
}

/// <summary>Mirror of <c>skyle_link_host_control</c> (skyle_link.h).</summary>
public enum SkyleLinkHostControl : ushort
{
    MenuBar = 1,
    PointerOverlay = 2,
    StartCalibration = 3,
}

/// <summary>Mirror of <c>skyle_eye_movement_type</c> (skyle_types.h).</summary>
public enum SkyleEyeMovementType : byte
{
    Fixation = 0,
    Saccade = 1,
    Unknown = 2,
}
