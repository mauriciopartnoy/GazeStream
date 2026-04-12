using EyeXFramework;
using Tobii.EyeX.Framework;
using GazeStream.Eyetracker;
using System.Numerics;
using System.Diagnostics;
using System.Windows;
public class GazeDeviceTobiiEyeX : IGazeDevice
{
    public bool IsConnected { get; private set; }
    public bool GazePointIsValid => !float.IsNaN(gazePosCache.viewportPoint.X) && !float.IsNaN(gazePosCache.viewportPoint.Y);


    public string DeviceName => "Tobii EyeX SDK";
    public EyesData Eyes => null;
    EyesData eyesCache;

    public bool UserIsPresent
    {
        get
        {
            if (eyeHost == null) return false;
            return eyeHost.UserPresence.IsValid;
        }
    }

    GazePoint gazePosCache = new();

    public EyeXHost eyeHost { get; private set; }
    public GazePointDataStream gazePointDataStream { get; private set; } = null;
    public EyePositionDataStream eyePositionStream { get; private set; } = null;

    GazePoint IGazeDevice.GazePoint => gazePosCache;

    GazePoint IGazeDevice.RawGazePoint => gazePosCache;

    public bool Initialize()
    {
        //Initialize
        Disconnect();
        eyeHost = new EyeXHost();
        eyeHost.Start();

        IsConnected = true;
        gazePointDataStream = eyeHost.CreateGazePointDataStream(GazePointDataMode.Unfiltered);
        eyePositionStream = eyeHost.CreateEyePositionDataStream();
        gazePointDataStream.Next += OnTobiiUpdate;
        eyePositionStream.Next += OnEyePositionUpdate;

        Thread.Sleep(100);

        if (eyeHost.EyeTrackingDeviceStatus.IsValid == false)
        {
            Debug.WriteLine("EyeX Connection failed :(");
            Disconnect();
            return false;
        }
        else
        {
            Debug.WriteLine("EyeX Connected");
            IsConnected = true;
            gazePointDataStream = eyeHost.CreateGazePointDataStream(GazePointDataMode.Unfiltered);
            eyePositionStream = eyeHost.CreateEyePositionDataStream();
            gazePointDataStream.Next += OnTobiiUpdate;
            eyePositionStream.Next += OnEyePositionUpdate;
            return true;
        }
    }


    private void OnTobiiUpdate(object sender, GazePointEventArgs e)
    {
        float x = (float)e.X / (float)eyeHost.ScreenBounds.Value.Width;
        float y = (float)e.Y / (float)eyeHost.ScreenBounds.Value.Height;
        Vector2 pos = new Vector2(x, y);
        gazePosCache = new GazePoint(pos);
       
    }

    void OnEyePositionUpdate(object sender, EyePositionEventArgs e)
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
        eyesCache.leftEye.isBlinking = e.LeftEye.IsValid ? false : true;
        eyesCache.leftEye.viewportX = (float)e.LeftEyeNormalized.X;
        eyesCache.leftEye.viewportY = (float)e.LeftEyeNormalized.Y;
        eyesCache.leftEye.pupilDistanceMm = (float)e.LeftEye.Z;
        eyesCache.leftEye.pupilDiameter = 0;
        eyesCache.leftEye.pupilDiameterMm = 0;

        eyesCache.rightEye.isBlinking = e.RightEye.IsValid ? false : true;
        eyesCache.rightEye.viewportX = (float)e.RightEyeNormalized.X;
        eyesCache.rightEye.viewportY = (float)e.RightEyeNormalized.X;
        eyesCache.rightEye.pupilDistanceMm = (float)e.LeftEye.Z;
        eyesCache.rightEye.pupilDiameter = 0;
        eyesCache.rightEye.pupilDiameterMm = 0;
    }


    public void UpdateData()
    {

    }
    private void OnDisable()
    {
        Disconnect();
    }

    public void Disconnect()
    {
        IsConnected = false;
        if (gazePointDataStream != null)
        {
            gazePointDataStream.Next -= OnTobiiUpdate;
            gazePointDataStream.Dispose();
        }
        if (eyePositionStream != null)
        {
            eyePositionStream.Next -= OnEyePositionUpdate;
            eyePositionStream.Dispose();
        }
        if (eyeHost != null) eyeHost.Dispose();
    }

    public void OpenCalibrationPage()
    {
        System.Windows.MessageBox.Show("Para calibrar un dispositivo Tobii utilice el software del propietario: Tobii Core o Tobii Experience.");
        return;
    }

    public void RequestCalibration(int pointsArray, int eyes)
    {
        System.Windows.MessageBox.Show("Para calibrar un dispositivo Tobii utilice el software del propietario: Tobii Core o Tobii Experience.");
        return;
    }
}
