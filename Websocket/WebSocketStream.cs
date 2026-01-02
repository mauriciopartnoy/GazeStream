using System.Collections;
using System.Collections.Generic;
using WebSocketSharp;
using WebSocketSharp.Server;
using System.Diagnostics;
using GazeStream.Utilities.Events;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Numerics;
using System;
using GazeStream.Utilities;
using GazeStream.Eyetracker;
using GazeStream;
using GazeStream.AppData;
using GazeStream.Windows;
public class WebSocketStream 
{
    public static WebSocketStream I {get; private set;}
    WebSocketServer ws;
    WebSocketServiceHost service;
   
    public bool streamData = false;
    public const string WEBSOCKET_PORT = "http://127.0.0.1:3000";
    public const string CALLBACK_CONNECTION_SUCCESS = "CONNECTION_SUCCESS";
    public const string CALLBACK_CONNECTION_FAILED = "CONNECTION_FAILED";
    public const string CALLBACK_CALIBRATION_STARTED = "CALIBRATION_STARTED";
    public const string CALLBACK_CALIBRATION_SUCCESS = "CALIBRATION_SUCCESS";
    public const string CALLBACK_CALIBRATION_FAILED = "CALIBRATION_FAILED";
    public const string CALLBACK_CALIBRATION_CANCELLED = "CALIBRATION_CANCELLED";
    public const string CALLBACK_CALIBRATION_FINISHED = "CALIBRATION_FINISHED";
    public const string CALLBACK_INSTANCE_QUIT = "INSTANCE_QUIT";

    public bool IsAlive
    {
        get
        {
            if (ws == null) return false;
            return ws.IsListening;
        }
    }

    bool aliveLastFrame;

    public WebSocketStream()
    {
        I = this;
    }

    public void StartWebsocketService()
    {
        Debug.WriteLine("Starting websocket server.");
        ws = new WebSocketServer(3000);
        ws.AddWebSocketService<GazeService>("/gaze");
        service = ws.WebSocketServices["/gaze"];
        ws.Start();
    }

    public void WriteLine(string message)
    {
        if (ws == null)
        {
            Debug.WriteLine("Websocket is null.");
            return;
        }
        service.Sessions.Broadcast(message);
    }

    public void TryConnect()
    {
        ws.Start();
    }

    public void OnStreamUpdate()
    {
        if (aliveLastFrame != IsAlive)
        {
            GlobalEvents.OnWebsocketStatusChanged.Invoke();
        }
        aliveLastFrame = IsAlive;
        StreamGazePoint();
    }

    private static void PixelPointTest()
    {
        Vector2 displayRes = WindowsHelper.GetDisplayResolutionInPixels();
        int xSmooth = (int)Math.Round(GazeManager.I.SmoothViewportPoint.X * (float)displayRes.X);
        int ySmooth = (int)Math.Round(GazeManager.I.SmoothViewportPoint.Y * (float)displayRes.Y);
        //Debug.Log($"Smooth X: {xSmooth} Y: {ySmooth}");
    }

    void StreamGazePoint()
    {
        if (ws == null || !IsAlive) return;
        if (!streamData) return;
        GazePoint point = GazeManager.I.GazePoint;
        if (!point.IsValid) return;

        Vector2 p = new Vector2();
        Vector2 displayRes = WindowsHelper.GetDisplayResolutionInPixels();


        p.X = (int)Math.Round(GazeManager.I.SmoothViewportPoint.X) * (float)displayRes.X;
        p.Y = (int)Math.Round(GazeManager.I.SmoothViewportPoint.Y) * (float)displayRes.Y;

        //p.t = CurrentTimeInMilliseconds;
        string jsonPoint =  JsonConvert.SerializeObject(p);
        WriteLine(jsonPoint);
        //Debug.Log($"Smooth X: {p.xSmooth} Y: {p.ySmooth}");
    }



    public void OnApplicationQuit()
    {
        if (ws == null || !IsAlive) return;
        WriteLine(CALLBACK_INSTANCE_QUIT);
        ws.Stop();
    }

}




public class GazeService : WebSocketBehavior
{

    CommandRouter commandRouter;
    long CurrentTimeInMilliseconds => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
   
    public enum WebsocketCallback
    {
        Calibration_Start,
        Calibration_Success,
        Calibration_Cancelled,
        Calibration_Finshed
    }

    protected override void OnOpen()
    {
        commandRouter = new();
        commandRouter.RegisterCommand(new SetSampleRateHzCommand());
        commandRouter.RegisterCommand(new SetSmoothCommand());
        commandRouter.RegisterCommand(new SetSmoothDampFilter());
        commandRouter.RegisterCommand(new SetKalmanFilterCommand());
        commandRouter.RegisterCommand(new StartCalibrationCommand());
        Debug.WriteLine("Client connected");

    }

    protected override void OnClose(CloseEventArgs e)
    {
        Debug.WriteLine("Client disconnected");
        Debug.WriteLine("Code: " + e.Code + ", Reason: " + e.Reason);
    }

    public void WriteLine(string message)
    {
        WebSocketStream.I.WriteLine(message);
    }
    protected override void OnMessage(MessageEventArgs message)
    {
        //Debug.Log("Message received: " + message.Data);
        App.Instance.Dispatcher.BeginInvoke(() =>
        {
            //Check Single Line Commands
            commandRouter.Execute(message.Data);


            switch (message.Data)
            {
                case "CancelCalibration":
                    CancelCalibration();
                    break;
                case "PauseData":
                    PauseEyetrackerDataStream();
                    break;
                case "SendData":
                    ResumeEyetrackerDataStream();
                    break;
                case "Minimize":
                    WindowsHelper.MinimizeWindow();
                    break;
                case "Maximize":
                    WindowsHelper.ShowWindow();
                    break;
                case "StartCalibration3":
                    RequestCalibration(CalibrationMode.Binocular3);
                    break;
                case "StartCalibration5":
                    RequestCalibration(CalibrationMode.Binocular5);
                    break;
                case "StartCalibration7":
                    RequestCalibration(CalibrationMode.Binocular7);
                    break;
                case "StartCalibration9":
                    RequestCalibration(CalibrationMode.Binocular9);
                    break;
                case "StartCalibrationLeft3":
                    RequestCalibration(CalibrationMode.Left3);
                    break;
                case "StartCalibrationLeft5":
                    RequestCalibration(CalibrationMode.Left5);
                    break;
                case "StartCalibrationLeft7":
                    RequestCalibration(CalibrationMode.Left7);
                    break;
                case "StartCalibrationLeft9":
                    RequestCalibration(CalibrationMode.Left9);
                    break;
                case "StartCalibrationRight3":
                    RequestCalibration(CalibrationMode.Right3);
                    break;
                case "StartCalibrationRight5":
                    RequestCalibration(CalibrationMode.Right5);
                    break;
                case "StartCalibrationRight7":
                    RequestCalibration(CalibrationMode.Right7);
                    break;
                case "StartCalibrationRight9":
                    RequestCalibration(CalibrationMode.Right9);
                    break;
                case "SetSmoothNone":
                    Settings.I.SmoothFilter.Value = 1;
                    break;
                case "SetSmoothLow":
                    Settings.I.SmoothFilter.Value = 3;
                    break;
                case "SetSmoothMedium":
                    Settings.I.SmoothFilter.Value = 7;
                    break;
                case "SetSmoothHigh":
                    Settings.I.SmoothFilter.Value = 10;
                    break;
                case "ShowEyeDisplay":
                    GlobalEvents.OnShowEyeDisplay.Invoke();
                    break;
                case "HideEyeDisplay":
                    GlobalEvents.OnHideEyeDisplay.Invoke();
                    break;
                case "SetSampleRate20":
                    Settings.I.SampleRateHZ.Value = 20;
                    break;
                case "SetSampleRate30":
                    Settings.I.SampleRateHZ.Value = 30;
                    break;
                case "SetSampleRate60":
                    Settings.I.SampleRateHZ.Value = 60;
                    break;
                default: break;
            }
        });

    }

    public void SetSmooth(int value)
    {
        GazeDeviceA11.SetSmooth(value);
    }

    public void RequestCalibration(CalibrationMode mode)
    {
        WindowManager.OpenWindow<CalibrationWindow>();
        GlobalEvents.OnStartCalibrationCommand.Invoke(mode);
    }

    public void CancelCalibration()
    {
        GlobalEvents.OnCalibrationCancel.Invoke();
    }

    public void PauseEyetrackerDataStream()
    {
        WebSocketStream.I.streamData = false;
        WriteLine("Data Stream Paused");
    }

    public void ResumeEyetrackerDataStream()
    {
        WebSocketStream.I.streamData = true;
        WriteLine("Data Stream Paused");
    }
}

//Commands
public class SetSmoothCommand : BaseWebsocketCommand
{
    public override string Name => "SetSmooth";

    public override Dictionary<string, ParamSchema> Schema { get; } = new()
        {
            {"level", new ParamSchema(typeof(int), true, 10) }
        };

    public override void Execute(JObject parameters)
    {
        int smoothLevel = parameters["level"].Value<int>();
        GazeDeviceA11.SetSmooth(smoothLevel);
    }
}

public class SetKalmanFilterCommand : BaseWebsocketCommand
{
    public override string Name => "SetKalmanFilter";

    public override Dictionary<string, ParamSchema> Schema { get; } = new()
        {
            {"level", new ParamSchema(typeof(int), true, 15) }
        };

    public override void Execute(JObject parameters)
    {
        int smoothLevel = parameters["level"].Value<int>();
        Settings.I.KalmanFilter.Value = smoothLevel;
    }
}

public class SetSampleRateHzCommand : BaseWebsocketCommand
{
    public override string Name => "SetSampleRate";

    public override Dictionary<string, ParamSchema> Schema { get; } = new()
        {
            {"hz", new ParamSchema(typeof(int), true, 30) }
        };

    public override void Execute(JObject parameters)
    {
        int sampleRate = parameters["hz"].Value<int>();
        Settings.I.SampleRateHZ.Value = sampleRate;
    }
}

public class SetSmoothDampFilter : BaseWebsocketCommand
{
    public override string Name => "SetSmoothDampFilter";

    const string PARAM_LEVEL = "level";

    public override Dictionary<string, ParamSchema> Schema { get; } = new()
        {
            {PARAM_LEVEL, new ParamSchema(typeof(float), true, 0.05f) }
        };

    public override void Execute(JObject parameters)
    {
        float smoothStep = parameters[PARAM_LEVEL].Value<float>();
        Settings.I.InterpolationFilter.Value = smoothStep;
    }
}

public class StartCalibrationCommand : BaseWebsocketCommand
{
    public override string Name => "StartCalibration";

    const string PARAM_EYES = "eyesOption";
    const string PARAM_POINTS = "pointsOption";

    public override Dictionary<string, ParamSchema> Schema { get; } = new()
        {
            {PARAM_EYES, new ParamSchema(typeof(int), true, 0) },
            { PARAM_POINTS, new ParamSchema(typeof(int), true, 0)}
        };

    public override void Execute(JObject parameters)
    {
        int points = parameters[PARAM_POINTS].Value<int>();
        int eyes = parameters[PARAM_EYES].Value<int>();
        RequestCalibration(points, eyes);
    }

    public void RequestCalibration(int points, int eyes)
    {
        WindowManager.OpenWindow<CalibrationWindow>();
        CalibrationWindow.I.startca(points, eyes);
    }
}

