namespace GazeStream.Eyetracker
{

    public interface IGazeDevice
    {
        string DeviceName { get; }
        bool IsConnected { get; }
        bool Initialize();
        void Disconnect();
        void UpdateData();
        bool UserIsPresent { get; }
        GazePoint GazePoint { get; }
        GazePoint RawGazePoint { get; }

    }
}
