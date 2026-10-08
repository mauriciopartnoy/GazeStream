using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;
using SkyleAvaloniaExample.Interop;
using System.Diagnostics;
using System.Numerics;
using GazeStream.Utilities;

namespace GazeStream.Eyetracker
{
    public class GazeDeviceSkyle : IGazeDevice
    {
        public string DeviceName => "Skyle";
        public bool IsConnected => true;
        public bool UserIsPresent => UserIsPresentStatic;
        public GazePoint GazePoint => gazePointCache;
        public GazePoint RawGazePoint => rawGazePointCache;
        public EyesData Eyes => eyesCache;

        GazePoint gazePointCache = new();
        GazePoint rawGazePointCache = new();
        EyesData eyesCache = new();
        static bool UserIsPresentStatic;
        Vector2 screenSize;

        private readonly SkyleClient client = new();
        private readonly DispatcherTimer dispatcherTimer = new(); //no se si lo necesito

        public SkyleClient Client => client;

        // Latest values written from the library's I/O thread (reference swaps are atomic).
        private volatile PositioningSnapshot? _latestPos;
        private volatile GazeSnapshot? _latestGaze;
        private volatile VideoFrame? _latestVideo;
        private volatile string _latestVersion = string.Empty;
        private int _latestState = (int)SkyleConnectionState.Disconnected;

        // What the UI has already consumed.
        private PositioningSnapshot? _posShown;
        private GazeSnapshot? _gazeShown;
        private VideoFrame? _videoShown;
        private SkyleConnectionState _appliedState = (SkyleConnectionState)(-1);


        public void Disconnect()
        {
            App.Instance.UIDispatcher.BeginInvoke(()=> {
                try
                {
                    Debug.WriteLine("Skyle client is being disposed");
                    DisposeCallbacks();
                    client.Dispose();
                }
                catch
                {
                    Debug.WriteLine("Dispose Skyle was unsuccesful");
                }
            });
       
        }

        public async Task<bool> Initialize()
        {
            screenSize = Helper.GetPrimaryMonitorSize();
            Debug.WriteLine("Initializing SKYLE");
            SetCallbacks();
            try
            {
                client.Start();
                dispatcherTimer.Start();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Could not connect to Skyle: " + ex.Message);
                return false;
            }
            return true;
        }

        void SetCallbacks()
        {

            client.PositioningReceived += OnPositionReceived;
            client.GazeReceived += OnGazeReceived;
            client.VideoReceived += v => _latestVideo = v;
            client.VersionReceived += v => _latestVersion = v;
            client.StateChanged += OnStateChanged;
        }

        void DisposeCallbacks()
        {
            client.PositioningReceived -= OnPositionReceived;
            client.GazeReceived -= OnGazeReceived;
            client.StateChanged -= OnStateChanged;

        }

        void OnPositionReceived(PositioningSnapshot pos)
        {
        }

        void OnGazeReceived(GazeSnapshot gaze)
        {
            if (!gaze.Valid) return;
            Vector2 gazePos = Helper.ScreenToViewportClamped(screenSize, gaze.X, gaze.Y);
            gazePointCache = new GazePoint(gazePos);
            //Debug.WriteLine("Gaze pos: " + gazePos.X);
        }

        void OnStateChanged(SkyleConnectionState state)
        {
            Debug.WriteLine("Skyle State: " + state.ToString());
            Interlocked.Exchange(ref _latestState, (int)state);
            if (state == SkyleConnectionState.LinkSynced)
            {
                client.SetHostPointerVisible(true);
                client.SetHostMenuBarVisible(true);
                client.EnableGaze(true);
                client.EnablePositioning(true);
                client.EnableControl(true);
               
            }
        }

        public void OpenCalibrationPage()
        {
            TryCalibrate();
        }


        public void RequestCalibration(int pointsArray, int eyes)
        {
            TryCalibrate();
        }
        private void TryCalibrate()
        {
            int result = client.StartHostCalibration();
            Debug.WriteLine("Current state: " + _appliedState.ToString());
            Debug.WriteLine("Skyle Host Calibration result: " + result);
        }

        public void ShowCameraPreview(bool show)
        {
        }

        public void UpdateData()
        {
            var state = (SkyleConnectionState)Volatile.Read(ref _latestState);
            if (state != _appliedState)
            {
                _appliedState = state;             
            }

            //UPDATE
            if (state == SkyleConnectionState.LinkSynced)
            {
                //DO STUFF AT UI
            }
        }

        public void StartHostCalibration() => NoteHostControl("Calibrate", client.StartHostCalibration());

        private string _hostControlNote = string.Empty;

        private void NoteHostControl(string what, int result)
        {
            _hostControlNote = result == (int)SkyleResult.Ok ? $"{what}: sent" : $"{what}: {(SkyleResult)result} (not a link client)";
            if (_appliedState != (SkyleConnectionState)(-1))
            {
                Debug.WriteLine(_appliedState);
            }
        }

    }
}
