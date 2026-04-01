//using EyeXFramework;
//using Tobii.EyeX.Framework;
//using GazeStream.Eyetracker;
//using GazeStream.Utilities.Events;
//using System.Numerics;
//using GazeStream.AppData;
//using System.Diagnostics;

//public class GazeDeviceTobiiEyeX : IGazeDevice
//{
//    public bool IsConnected { get; private set; }
//    public bool GazePointIsValid => !float.IsNaN(gazePosCache.x) && !float.IsNaN(gazePosCache.y);


//    public string DeviceName => "Tobii EyeX SDK";
//    public EyesData Eyes => null;
//    EyesData eyesCache;

//    public bool UserIsPresent 
//    {
//        get 
//        {
//            if (eyeHost == null) return false;
//            return eyeHost.UserPresence.IsValid;
//        }    
//    }

//    Vector2 gazePosCache;

//    public EyeXHost eyeHost { get; private set; }
//    public GazePointDataStream gazePointDataStream { get; private set; } = null;
//    public EyePositionDataStream eyePositionStream { get; private set; } = null;

//    GazePoint IGazeDevice.GazePoint => throw new NotImplementedException();

//    GazePoint IGazeDevice.RawGazePoint => throw new NotImplementedException();


//    //AMBOS DATOS SON CRUDOS EN ESTE CASO
//    public GazePoint RawGazePoint()
//    {
//        return GazePoint();
//    }
//    public GazePoint GazePoint()
//    {
//        GazePoint point = new GazePoint();
//        point.Viewport = gazePosCache;
//        return point;
//    }

//    public bool Initialize()
//    {
//        //Initialize
//        Disconnect();
//        eyeHost = new EyeXHost();
//        eyeHost.Start();
//        if (eyeHost.EyeTrackingDeviceStatus.IsValid == false)
//        {
//            Debug.Log("EyeX Connection failed :(");
//            Disconnect();
//            return false;
//        }
//        else
//        {
//            Debug.Log("EyeX Connected");
//            IsConnected = true;
//            gazePointDataStream = eyeHost.CreateGazePointDataStream(GazePointDataMode.Unfiltered);
//            eyePositionStream = eyeHost.CreateEyePositionDataStream();
//            gazePointDataStream.Next += OnTobiiUpdate;
//            eyePositionStream.Next += OnEyePositionUpdate;
//            return true;
//        }
//    }


//    private void OnTobiiUpdate(object sender, GazePointEventArgs e)
//    {
//        float x = (float)e.X / (float)eyeHost.ScreenBounds.Value.Width;
//        float y = (float)e.Y / (float)eyeHost.ScreenBounds.Value.Height;
//        gazePosCache = new Vector2(x, 1f -y);
//    }

//    void OnEyePositionUpdate(object sender, EyePositionEventArgs e)
//    {
//        if (eyesCache == null)
//        {
//            eyesCache = new EyesData();
//        }
//        if (eyesCache.leftEye == null)
//        {
//            eyesCache.leftEye = new Eye();
//        }
//        if (eyesCache.rightEye == null)
//        {
//            eyesCache.rightEye = new Eye();
//        }
//        eyesCache.leftEye.isBlinking = e.LeftEye.IsValid ? false : true;
//        eyesCache.leftEye.viewportX = (float)e.LeftEyeNormalized.X;
//        eyesCache.leftEye.viewportY = 1f - (float)e.LeftEyeNormalized.Y;
//        eyesCache.leftEye.pupilDistanceMm = (float)e.LeftEye.Z;
//        eyesCache.leftEye.pupilDiameter = 0;
//        eyesCache.leftEye.pupilDiameterMm = 0;

//        eyesCache.rightEye.isBlinking = e.RightEye.IsValid? false : true;
//        eyesCache.rightEye.viewportX = (float)e.RightEyeNormalized.X;
//        eyesCache.rightEye.viewportY = 1f - (float)e.RightEyeNormalized.X;
//        eyesCache.rightEye.pupilDistanceMm = (float)e.LeftEye.Z;
//        eyesCache.rightEye.pupilDiameter = 0;
//        eyesCache.rightEye.pupilDiameterMm = 0;
//    }


//    public void UpdateData()
//    {
        
//    }
//    private void OnDisable()
//    {
//        Disconnect();
//    }

//    public void Disconnect()
//    {
//        IsConnected = false;
//        if (gazePointDataStream != null)
//        {
//            gazePointDataStream.Next -= OnTobiiUpdate;
//            gazePointDataStream.Dispose();
//        }
//        if (eyePositionStream != null)
//        {
//            eyePositionStream.Next -= OnEyePositionUpdate;
//            eyePositionStream.Dispose();
//        }
//        if (eyeHost != null) eyeHost.Dispose();
//    }

//}
