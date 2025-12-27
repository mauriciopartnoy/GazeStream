using GazeStream.AppData;

namespace GazeStream.Utilities.Events
{
    internal static class GlobalEvents
    {
        //LOGIN EVENTS
        public static readonly GlobalEvent<User> OnUserChanged = new();

        //CALIBRATION EVENTS
        public static readonly GlobalEvent OnCalibrationStart = new();
        public static readonly GlobalEvent OnCalibrationCancel = new();
        public static readonly GlobalEvent<byte[]> OnCalibrationSuccess = new();

        //OPTIONS CHANGED EVENTS
        public static readonly GlobalEvent<int> OnInvensunSmoothValueChanged = new();
    }
}
