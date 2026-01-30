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
using GazeStream.Utilities.Save;

public class WebSocketStream : IDisposable
{
    public static WebSocketStream I {get; private set;}
    WebSocketServer ws;
    WebSocketServiceHost service;
   
    public const string WEBSOCKET_PORT = "http://127.0.0.1:3000";
    public const string CALLBACK_CONNECTION_SUCCESS = "CONNECTION_SUCCESS";
    public const string CALLBACK_CONNECTION_FAILED = "CONNECTION_FAILED";
    public const string CALLBACK_CONNECTION_DISCONNECTED = "CONNECTION_DISCONNECTED";
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
        SubscribeToGazeManager();
        HookCallbacks();
    }

    private void HookCallbacks()
    {
        GlobalEvents.OnEyetrackerConnected.Add(() => WriteLine(CALLBACK_CONNECTION_SUCCESS));
        GlobalEvents.OnEyetrackerConnectionFailed.Add(() => WriteLine(CALLBACK_CONNECTION_FAILED));
        GlobalEvents.OnEyetrackerDisconnected.Add(() => WriteLine(CALLBACK_CONNECTION_DISCONNECTED));
        GlobalEvents.OnCalibrationStart.Add(() => WriteLine(CALLBACK_CALIBRATION_STARTED));
        GlobalEvents.OnCalibrationCancel.Add(() => WriteLine(CALLBACK_CALIBRATION_CANCELLED));
        GlobalEvents.OnCalibrationFailed.Add(() => WriteLine(CALLBACK_CALIBRATION_FAILED));
        GlobalEvents.OnCalibrationSuccess.Add(() => WriteLine(CALLBACK_CALIBRATION_SUCCESS));
        GlobalEvents.OnCalibrationFinished.Add(() => WriteLine(CALLBACK_CALIBRATION_FINISHED));

        GlobalEvents.OnSettingChanged.Add(SendOnSettingChangedNotification);
    }

    public void SendOnSettingChangedNotification(string saveKey)
    {
        SettingDescriptor descriptor = Settings.Descriptors[saveKey];
        SettingDTO newSetting = new SettingDTO(descriptor.saveKey, descriptor.typeAsString, SaveManager.GetSystemSetting(descriptor.saveKey, descriptor.defaultValue));
        GetValueMessage message = new GetValueMessage(newSetting);
        string settingsSnapshotJson = JsonConvert.SerializeObject(message, Formatting.Indented);
        WriteLine(settingsSnapshotJson);
    }

    public void SubscribeToGazeManager()
    {
        GazeManager.OnGazeUpdate -= OnStreamUpdate;
        GazeManager.OnGazeUpdate += OnStreamUpdate;
    }

    public void UnsubscribeToGazeManager()
    {
        GazeManager.OnGazeUpdate -= OnStreamUpdate;
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
        GazePoint point = GazeManager.I.GazePoint;
        if (!point.IsValid) return;

        //El punto stremeado está en Screenspace a pedido. Quizá sea útil enviar el dato crudo en Viewport space también?

        Vector2 p = new Vector2();
        Vector2 displayRes = WindowsHelper.GetDisplayResolutionInPixels();
        p.X = (int)Math.Round(GazeManager.I.SmoothViewportPoint.X * (float)displayRes.X);
        p.Y = (int)Math.Round(GazeManager.I.SmoothViewportPoint.Y * (float)displayRes.Y);
        //p.t = CurrentTimeInMilliseconds;
        string jsonPoint =  JsonConvert.SerializeObject(p);
        WriteLine(jsonPoint);
        //Debug.Log($"Smooth X: {p.xSmooth} Y: {p.ySmooth}");
    }

    public void Dispose()
    {
        WriteLine(CALLBACK_INSTANCE_QUIT);
        if (ws == null) return;   
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
        commandRouter.RegisterCommand(new GetValueCommand());
        commandRouter.RegisterCommand(new SetValueCommand());     
        commandRouter.RegisterCommand(new StartCalibrationCommand());
        Debug.WriteLine("Client connected");
        SendSettingsSnapshot();

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
                case "GetSettingsSnapshot":
                    SendSettingsSnapshot();
                break;
                case "ToggleMouseControl":
                    Settings.I.MouseToggle.Value = !Settings.I.MouseToggle.Value;
                break;
                case "ToggleBubble":
                    Settings.I.BubbleToggle.Value = !Settings.I.BubbleToggle.Value;
                    break;
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
                    WindowManager.CloseWindow<CalibrationWindow>();
                    break;
                case "Maximize":
                    WindowManager.OpenWindow<CalibrationWindow>();
                    break;
                case "StartCalibration3":
                    RequestCalibration(0,0);
                    break;
                case "StartCalibration5":
                    RequestCalibration(1,0);
                    break;        
                case "StartCalibration9":
                    RequestCalibration(2,0);
                    break;
                case "StartCalibrationLeft3":
                    RequestCalibration(0,1);
                    break;
                case "StartCalibrationLeft5":
                    RequestCalibration(1,1);
                    break;              
                case "StartCalibrationLeft9":
                    RequestCalibration(2,1);
                    break;
                case "StartCalibrationRight3":
                    RequestCalibration(0,2);
                    break;
                case "StartCalibrationRight5":
                    RequestCalibration(1,2);
                    break;       
                case "StartCalibrationRight9":
                    RequestCalibration(2,2);
                    break;
                case "SetSmoothNone":
                    Settings.I.FilterProfile.Value = FilterProfile.Bajo;
                    break;
                case "SetSmoothLow":
                    Settings.I.FilterProfile.Value = FilterProfile.Bajo;
                    break;
                case "SetSmoothMedium":
                    Settings.I.FilterProfile.Value = FilterProfile.Medio;
                    break;
                case "SetSmoothHigh":
                    Settings.I.FilterProfile.Value = FilterProfile.Alto;
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
                    WriteLine("Sample rate set to 30 Hz");
                    break;
                case "SetSampleRate60":
                    Settings.I.SampleRateHZ.Value = 60;
                    break;
                default: break;
            }
        });

    }

    void ShowEyeDisplay()
    { 
        //TODO: Add eye display
    }

    void HideEyeDisplay()
    {
        //TODO: Add eye display
    }

   
    void SendSettingsSnapshot()
    {
        List<SettingDTO> settings = new List<SettingDTO>();
        foreach (SettingDescriptor s in Settings.Descriptors.Values)
        {
            SettingDTO newSetting = new SettingDTO(s.saveKey, s.typeAsString, SaveManager.GetSystemSetting(s.saveKey, s.defaultValue));
            settings.Add(newSetting);
        }

        SettingsSnapshotMessage settingsSnapshot = new SettingsSnapshotMessage(settings);
        string settingsSnapshotJson = JsonConvert.SerializeObject(settingsSnapshot, Formatting.Indented);
        WriteLine(settingsSnapshotJson);
    }

    public void RequestCalibration(int pointsArray, int eyes)
    {
        WindowManager.OpenWindow<CalibrationWindow>();
        GlobalEvents.OnStartCalibrationCommand.Invoke(pointsArray, eyes);
    }

    public void CancelCalibration()
    {
        GlobalEvents.OnCalibrationCancel.Invoke();
    }

    public void PauseEyetrackerDataStream()
    {
        WebSocketStream.I.UnsubscribeToGazeManager();
        WriteLine("Data Stream Paused");
    }

    public void ResumeEyetrackerDataStream()
    {
        WebSocketStream.I.SubscribeToGazeManager();
        WriteLine("Resuming Data Stream");
    }
}

//Data Transfer

public class SettingsSnapshotMessage
{
    public string messageType;
    public List<SettingDTO> settings;

    public SettingsSnapshotMessage(List<SettingDTO> settings)
    {
        this.messageType = "SettingsSnapshot";
        this.settings = settings;
    }
}

public class GetValueMessage
{
    public string messageType;
    public SettingDTO setting;

    public GetValueMessage(SettingDTO setting)
    {
        messageType = "GetValue";
        this.setting = setting;
    }

}

public class SettingDTO
{
    public string key;
    public string valueType;
    public object value;
    public SettingDTO(string key, string valueType, object value)
    {
        this.key = key;
        this.valueType = valueType;
        this.value = value;
    }
}

//Commands

public class GetValueCommand : BaseWebsocketCommand
{
    public override string Name => "GetValue";
    const string PARAM_KEY = "key";

    public override Dictionary<string, ParamSchema> Schema { get; } = new()
        {
            {PARAM_KEY, new ParamSchema(typeof(string), true, string.Empty) }
        };

    public override void Execute(JToken parameters)
    {
        JObject obj = (JObject)parameters;
        string key = obj[PARAM_KEY].ToObject<string>();
        GlobalEvents.OnSettingChanged.Invoke(key);
    }
}

public class SetValueCommand : BaseWebsocketCommand
{
    public override string Name => "SetValue";
    const string PARAM_KEY = "key";
    const string PARAM_VALUE = "value";

    public override Dictionary<string, ParamSchema> Schema { get; } = new()
        {
            {PARAM_KEY, new ParamSchema(typeof(string), true, string.Empty)},
            { PARAM_VALUE, new ParamSchema(typeof(object), true, default)}
        };


    public override void Execute(JToken parameters)
    {
        Debug.WriteLine("Executing SetValue command");
        JObject obj = (JObject)parameters;
        string key = obj[PARAM_KEY].ToObject<string>();

        if (!Settings.BaseSettings.TryGetValue(key, out var setting))
            return;

        var valueToken = parameters["value"];
        var typedValue = valueToken.ToObject(setting.Descriptor.type);
        setting.SetValue(typedValue);
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

    public override void Execute(JToken parameters)
    {
        JObject obj = (JObject)parameters;
        int points = parameters[PARAM_POINTS].ToObject<int>();
        int eyes = parameters[PARAM_EYES].ToObject<int>();
        RequestCalibration(points, eyes);
    }

    public void RequestCalibration(int points, int eyes)
    {
        WindowManager.OpenWindow<CalibrationWindow>();
        GlobalEvents.OnStartCalibrationCommand.Invoke(points, eyes);
    }
}

