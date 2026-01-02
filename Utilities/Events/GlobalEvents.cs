using GazeStream.AppData;

namespace GazeStream.Utilities.Events
{
    internal static class GlobalEvents
    {
        //LOGIN EVENTS
        public static readonly GlobalEvent<User> OnUserChanged = new();

        //CALIBRATION EVENTS

        public static readonly GlobalEvent<CalibrationMode> OnStartCalibrationCommand = new();
        public static readonly GlobalEvent OnCalibrationStart = new();
        public static readonly GlobalEvent OnCalibrationCancel = new();
        public static readonly GlobalEvent OnCalibrationSuccess = new();
        public static readonly GlobalEvent OnCalibrationFinished = new();
        public static readonly GlobalEvent OnShowEyeDisplay = new();
        public static readonly GlobalEvent OnHideEyeDisplay = new();

        //CALIBRATION EVENTS
        public static readonly GlobalEvent OnWebsocketStatusChanged = new();
        public static readonly GlobalEvent<int> OnInvensunSmoothValueChanged = new();
    }
}
