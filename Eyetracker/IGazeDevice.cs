namespace GazeStream.Eyetracker
{

    public interface IGazeDevice
    {
        string DeviceName { get; }
        bool IsConnected { get; }
        bool Initialize();
        void Disconnect();
        void UpdateData();
        void OpenCalibrationPage();
        void RequestCalibration(int pointsArray, int eyes);
        void ShowCameraPreview(bool show);
        bool UserIsPresent { get; }
        GazePoint GazePoint { get; }
        GazePoint RawGazePoint { get; }
        EyesData Eyes { get; }

    }
}
