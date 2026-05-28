using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Alea.Api;
using System.Diagnostics;
using System.Windows.Threading;
using System.Numerics;
using GazeStream.Utilities;
using GazeStream.Windows;

namespace GazeStream.Eyetracker
{
    public class GazeDeviceIntelligaze : IGazeDevice
    {
        EtApi _api = new EtApi();

        private event RawDataDelegate RawDataReceived;
        private event FixationDelegate FixationReceived;
        private event SaccadeDelegate SaccadeReceived;
        private event BlinkDelegate BlinkReceived;
        private event NoEventDelegate NoEventReceived;
        private event CalibrationDoneDelegate CaliDoneReceived;
        private event ResultCalibrationDelegate ResultReceived;
        private event ResultCalibrationExDelegate ResultExReceived;
        private event SystemMessageDelegate SystemMessageReceived;
        private event EyeStatusDelegate EyeStatusReceived;
        private event EyeVisibilityDelegate EyeVisibilityReceived;


        public string DeviceName => "Intelligaze";
        public bool IsConnected { get; private set; } = false;
        public bool UserIsPresent => true;
        public GazePoint GazePoint => gazePointCache;
        public GazePoint RawGazePoint => rawGazePointCache;
        public EyesData Eyes { get; private set; } = new();

        Dispatcher d => App.Instance.Dispatcher;
        bool InvokeRequired => Dispatcher.FromThread(Thread.CurrentThread) == App.Current.Dispatcher;

        GazePoint gazePointCache = new();
        GazePoint rawGazePointCache = new();
        Vector2 screenSize;
        bool isCalibrating;

        public bool Initialize()
        {
            screenSize = Helper.GetPrimaryMonitorSize();

            //Intelligaze Check
            bool acquired = false;
            bool canConnect = false;
            Mutex m = new Mutex(false, "Local\\AleaIntelliGaze30");

            try
            {
                acquired = m.WaitOne(0);
            }
            catch (AbandonedMutexException e)
            {
                m.ReleaseMutex();
                IsConnected = false;
                canConnect = false;
            }
            finally
            {
                if (acquired)
                {
                    m.ReleaseMutex();
                    IsConnected = false;
                    canConnect = false;
                }
                else
                {
                    canConnect = true;
                }
            }
            if (!canConnect) return false;

            ApiError error;
            do
            {
                bool isOpen;
                error = _api.IsOpen(out isOpen);
                Debug.WriteLine($"Intelligaze server is open: {isOpen} Error: {error.ToString()}");
                error = _api.Open("API-aw6oo-yrrhc", "127.0.0.1", 27412, "127.0.0.1", 27413);
                Thread.Sleep(1000);
                Debug.WriteLine($"Trying to open Intelligaze API + {error.ToString()}");
            }
            while (error != ApiError.NoError);

            Debug.WriteLine("Intelligaze API is open. Setting callbacks!");

            //Install data callbacks. Primero se desuscribe a eventos anteriores por si hubo una salida fallida del dispositivo.
            UnsubscribeToEvents(); 
            SubscribeToEvents();

            // connect passive callbacks to API
            IntPtr p;

            if (IntPtr.Size == 8) // x64
            {
                p = Marshal.GetFunctionPointerForDelegate(RawDataReceived);
                _api.SetRawDataCB64(p.ToInt64(), IntPtr.Zero);

                p = Marshal.GetFunctionPointerForDelegate(CaliDoneReceived);
                _api.SetCalibrationDoneCB64(p.ToInt64(), IntPtr.Zero);

                p = Marshal.GetFunctionPointerForDelegate(ResultReceived);
                _api.SetCalibrationResultCB64(p.ToInt64(), IntPtr.Zero);

                p = Marshal.GetFunctionPointerForDelegate(ResultExReceived);
                _api.SetCalibrationResultExCB64(p.ToInt64(), IntPtr.Zero);

                p = Marshal.GetFunctionPointerForDelegate(SystemMessageReceived);
                _api.SetSystemMessageCB64(p.ToInt64(), IntPtr.Zero);

                p = Marshal.GetFunctionPointerForDelegate(EyeStatusReceived);
                _api.SetEyeStatusCB64(p.ToInt64(), IntPtr.Zero);

                p = Marshal.GetFunctionPointerForDelegate(EyeVisibilityReceived);
                _api.SetEyeVisibilityCB64(p.ToInt64(), IntPtr.Zero);
            }
            else  // x86
            {
                p = Marshal.GetFunctionPointerForDelegate(RawDataReceived);
                _api.SetRawDataCB(p.ToInt32(), IntPtr.Zero);

                p = Marshal.GetFunctionPointerForDelegate(CaliDoneReceived);
                _api.SetCalibrationDoneCB(p.ToInt32(), IntPtr.Zero);

                p = Marshal.GetFunctionPointerForDelegate(ResultReceived);
                _api.SetCalibrationResultCB(p.ToInt32(), IntPtr.Zero);

                p = Marshal.GetFunctionPointerForDelegate(ResultExReceived);
                _api.SetCalibrationResultExCB(p.ToInt32(), IntPtr.Zero);

                p = Marshal.GetFunctionPointerForDelegate(SystemMessageReceived);
                _api.SetSystemMessageCB(p.ToInt32(), IntPtr.Zero);

                p = Marshal.GetFunctionPointerForDelegate(EyeStatusReceived);
                _api.SetEyeStatusCB(p.ToInt32(), IntPtr.Zero);

                p = Marshal.GetFunctionPointerForDelegate(EyeVisibilityReceived);
                _api.SetEyeVisibilityCB(p.ToInt32(), IntPtr.Zero);
            }

            UpdateDataStreaming(true, true);
            IsConnected = true;
            return true;
        }

        public void Disconnect()
        {
            UpdateDataStreaming(false, false);
            UnsubscribeToEvents();
            _api.ExitServer();
            _api.Close();
        }

        private void UnsubscribeToEvents()
        {
            RawDataReceived -= new RawDataDelegate(SharpClient_RawDataReceived);
            FixationReceived -= new FixationDelegate(SharpClient_FixationReceived);
            SaccadeReceived -= new SaccadeDelegate(SharpClient_SaccadeReceived);
            BlinkReceived -= new BlinkDelegate(SharpClient_BlinkReceived);
            NoEventReceived -= new NoEventDelegate(SharpClient_NoEventReceived);
            CaliDoneReceived -= new CalibrationDoneDelegate(SharpClient_CaliDoneReceived);
            ResultReceived -= new ResultCalibrationDelegate(SharpClient_ResultReceived);
            ResultExReceived -= new ResultCalibrationExDelegate(SharpClient_ResultExReceived);
            SystemMessageReceived -= new SystemMessageDelegate(SharpClient_SystemMessageReceived);
            EyeStatusReceived -= new EyeStatusDelegate(SharpClient_EyeStatusReceived);
            EyeVisibilityReceived -= new EyeVisibilityDelegate(SharpClient_EyeVisibilityReceived);
        }
        private void SubscribeToEvents()
        {
            RawDataReceived += new RawDataDelegate(SharpClient_RawDataReceived);
            FixationReceived += new FixationDelegate(SharpClient_FixationReceived);
            SaccadeReceived += new SaccadeDelegate(SharpClient_SaccadeReceived);
            BlinkReceived += new BlinkDelegate(SharpClient_BlinkReceived);
            NoEventReceived += new NoEventDelegate(SharpClient_NoEventReceived);
            CaliDoneReceived += new CalibrationDoneDelegate(SharpClient_CaliDoneReceived);
            ResultReceived += new ResultCalibrationDelegate(SharpClient_ResultReceived);
            ResultExReceived += new ResultCalibrationExDelegate(SharpClient_ResultExReceived);
            SystemMessageReceived += new SystemMessageDelegate(SharpClient_SystemMessageReceived);
            EyeStatusReceived += new EyeStatusDelegate(SharpClient_EyeStatusReceived);
            EyeVisibilityReceived += new EyeVisibilityDelegate(SharpClient_EyeVisibilityReceived);
        }

        private void UpdateDataStreaming(bool streamEvents, bool streamRaw)
        {
            ApiError error = ApiError.NoError;
            int mode = 0;

            // stream eye events
            if (streamEvents)
            {
                mode |= 2;

                IntPtr p;

                if (IntPtr.Size == 8) // x64
                {
                    p = Marshal.GetFunctionPointerForDelegate(BlinkReceived);
                    _api.SetBlinkCB64(p.ToInt64(), IntPtr.Zero);

                    p = Marshal.GetFunctionPointerForDelegate(FixationReceived);
                    _api.SetFixationCB64(p.ToInt64(), IntPtr.Zero);

                    p = Marshal.GetFunctionPointerForDelegate(SaccadeReceived);
                    _api.SetSaccadeCB64(p.ToInt64(), IntPtr.Zero);

                    p = Marshal.GetFunctionPointerForDelegate(NoEventReceived);
                    _api.SetNoEventCB64(p.ToInt64(), IntPtr.Zero);
                }
                else
                {
                    p = Marshal.GetFunctionPointerForDelegate(BlinkReceived);
                    _api.SetBlinkCB(p.ToInt32(), IntPtr.Zero);

                    p = Marshal.GetFunctionPointerForDelegate(FixationReceived);
                    _api.SetFixationCB(p.ToInt32(), IntPtr.Zero);

                    p = Marshal.GetFunctionPointerForDelegate(SaccadeReceived);
                    _api.SetSaccadeCB(p.ToInt32(), IntPtr.Zero);

                    p = Marshal.GetFunctionPointerForDelegate(NoEventReceived);
                    _api.SetNoEventCB(p.ToInt32(), IntPtr.Zero);
                }
            }
            else
            {
                if (IntPtr.Size == 8) // x64
                {
                    _api.SetBlinkCB64(0, IntPtr.Zero);
                    _api.SetFixationCB64(0, IntPtr.Zero);
                    _api.SetSaccadeCB64(0, IntPtr.Zero);
                    _api.SetNoEventCB64(0, IntPtr.Zero);
                }
                else
                {
                    _api.SetBlinkCB(0, IntPtr.Zero);
                    _api.SetFixationCB(0, IntPtr.Zero);
                    _api.SetSaccadeCB(0, IntPtr.Zero);
                    _api.SetNoEventCB(0, IntPtr.Zero);
                }
            }

            // stream raw events (special license required)
            if (streamRaw)
            {
                mode |= 1;
                IntPtr p = Marshal.GetFunctionPointerForDelegate(RawDataReceived);
                if (IntPtr.Size == 8) // x64
                {
                    _api.SetRawDataCB64(p.ToInt64(), IntPtr.Zero);
                }
                else
                {
                    _api.SetRawDataCB(p.ToInt32(), IntPtr.Zero);
                }
            }
            else
            {
                if (IntPtr.Size == 8) // x64
                {
                    _api.SetRawDataCB64(0, IntPtr.Zero);
                }
                else
                {
                    _api.SetRawDataCB(0, IntPtr.Zero);
                }
            }

            error = _api.DataStreaming(mode);

            if (error != ApiError.NoError)
                Debug.WriteLine(error.ToString());
            else
                AddReport("Data Streaming");
        }

        public void OpenCalibrationPage()
        {
            return;
        }

        public void RequestCalibration(int pointsArray, int eyes)
        {
            if (isCalibrating) return;

            isCalibrating = true;
            int points = 2;
            EyeTypeEnum eyeType = EyeTypeEnum.Binocular;

            switch (pointsArray)
            {
                case 0: points = 1; break;
                case 1: points = 5; break;
                case 2: points = 9; break;
            }

            switch (eyes)
            {
                case 0: eyeType = EyeTypeEnum.Binocular; break;
                case 1: eyeType = EyeTypeEnum.CalibrateLeft; break;
                case 2: eyeType = EyeTypeEnum.CalibrateRight; break;
            }

            _api.PerformCalibration(points, PointLocationEnum.Full, false, false, true, eyeType, false, false, true, 0, 2, "ANIMATION:PARROT");

        }

        public void UpdateData()
        {
            UpdateIsConnected();
        }

        private void UpdateIsConnected()
        {
            bool acquired = false;
            Mutex m = new Mutex(false, "Local\\AleaIntelliGaze30");

            try
            {
                acquired = m.WaitOne(0);
            }
            catch (AbandonedMutexException e)
            {
                m.ReleaseMutex();
                IsConnected = false;
            }
            finally
            {
                if (acquired)
                {
                    m.ReleaseMutex();
                    IsConnected = false;
                }
            }
        }

        void AddReport(string report)
        {
            Debug.WriteLine(report);
        }

        private void LoadCalibration(string calibName)
        {
            ApiError error = _api.LoadCalibration(calibName);

            if (error != ApiError.NoError)
                Debug.WriteLine(error.ToString());
            else
                AddReport("Load Calibration");
        }

        private void SaveCalibration(string calibName)
        {
            ApiError error = _api.SaveCalibration(calibName);

            if (error != ApiError.NoError)
                Debug.WriteLine(error.ToString());
            else
                AddReport("Save Calibration");
        }


        void SharpClient_ResultReceived(ref ServerCalibrationResult result, IntPtr userData)
        {
            isCalibrating = false;

            if (InvokeRequired)
            {
                d.BeginInvoke(new ResultCalibrationDelegate(SharpClient_ResultReceived), new object[] { result, userData });
            }
            else
            {
                isCalibrating = false;
                AddReport("Calibration Result: " + result.P1 + " " + result.P2 + " " + result.P3 + " " + result.P4 + " " + result.P5 + " " + result.P6 + " " + result.P7 + " " + result.P8 + " " + result.P9);
            }
        }

        void SharpClient_SystemMessageReceived(ref Int32 result, IntPtr userData)
        {
            if (InvokeRequired)
            {
                d.BeginInvoke(new SystemMessageDelegate(SystemMessageReceived), new object[] { result, userData });
            }
            else
            {
                AddReport("Systemmessage: " + result);
            }
        }

        void SharpClient_ResultExReceived(ref ServerCalibrationResultEx result, IntPtr userData)
        {
            if (InvokeRequired)
            {
                d.BeginInvoke(new ResultCalibrationExDelegate(SharpClient_ResultExReceived), new object[] { result, userData });
            }
            else
            {
                AddReport("CalibrationEx Left Result: " + result.P1l[0] + "," + result.P1l[1] + " " +
                                                        +result.P2l[0] + "," + result.P2l[1] + " " +
                                                        +result.P3l[0] + "," + result.P3l[1] + " " +
                                                        +result.P4l[0] + "," + result.P4l[1] + " " +
                                                        +result.P5l[0] + "," + result.P5l[1] + " " +
                                                        +result.P6l[0] + "," + result.P6l[1] + " " +
                                                        +result.P7l[0] + "," + result.P7l[1] + " " +
                                                        +result.P8l[0] + "," + result.P8l[1] + " " +
                                                        +result.P9l[0] + "," + result.P9l[1] + " " +
                                                        +result.P10l[0] + "," + result.P10l[1] + " " +
                                                        +result.P11l[0] + "," + result.P11l[1] + " " +
                                                        +result.P12l[0] + "," + result.P12l[1] + " " +
                                                        +result.P13l[0] + "," + result.P13l[1] + " " +
                                                        +result.P14l[0] + "," + result.P14l[1] + " " +
                                                        +result.P15l[0] + "," + result.P15l[1] + " " +
                                                        +result.P16l[0] + "," + result.P16l[1]);

                AddReport("CalibrationEx Right Result: " + result.P1r[0] + "," + result.P1r[1] + " " +
                                                        +result.P2r[0] + "," + result.P2r[1] + " " +
                                                        +result.P3r[0] + "," + result.P3r[1] + " " +
                                                        +result.P4r[0] + "," + result.P4r[1] + " " +
                                                        +result.P5r[0] + "," + result.P5r[1] + " " +
                                                        +result.P6r[0] + "," + result.P6r[1] + " " +
                                                        +result.P7r[0] + "," + result.P7r[1] + " " +
                                                        +result.P8r[0] + "," + result.P8r[1] + " " +
                                                        +result.P9r[0] + "," + result.P9r[1] + " " +
                                                        +result.P10r[0] + "," + result.P10r[1] + " " +
                                                        +result.P11r[0] + "," + result.P11r[1] + " " +
                                                        +result.P12r[0] + "," + result.P12r[1] + " " +
                                                        +result.P13r[0] + "," + result.P13r[1] + " " +
                                                        +result.P14r[0] + "," + result.P14r[1] + " " +
                                                        +result.P15r[0] + "," + result.P15r[1] + " " +
                                                        +result.P16r[0] + "," + result.P16r[1]);
            }
        }


        private void SharpClient_RawDataReceived(ref RawData data, IntPtr userData)
        {
            //Debug.WriteLine($"RAW DATA RECEIVED: Pupil Diameter: {data.leftEye.pupilDiameter} Distance {data.head.headPosZ}");
            //Debug.WriteLine($"RAW DATA MICROLOSS: X: {data.intelliGazeX} Y: {data.intelliGazeY}");

            //UpdateGazePointFromScreen((float)data.intelliGazeX, (float)data.intelliGazeY);

            if (data.leftEye.pupilDiameter == 0 && data.rightEye.pupilDiameter == 0)
            {
                //Blinking. Ignore point.
            }
            else if (data.intelliGazeX == 0 || data.intelliGazeX == -1 || data.intelliGazeY == 0 || data.intelliGazeY == -1)
            {
                //Ignore points.
            }
            else if (data.leftEye.gazePositionY == 0)
            {
                UpdateGazePointFromScreen((float)data.rightEye.gazePositionX, (float)data.rightEye.gazePositionY);
            }
            else if (data.rightEye.gazePositionY == 0)
            {
                UpdateGazePointFromScreen((float)data.leftEye.gazePositionX, (float)data.leftEye.gazePositionY);
            }
            else
            {
                //Both eyes point.
                UpdateGazePointFromScreen((float)data.intelliGazeX, (float)data.intelliGazeY);
            }
            //UPDATE EYE DATA (El IsBlinking se llena en el callback BlinkReceived. Alternativamente chequear pupil diameter.)

            Vector2 lefteyePos = ScreenToViewport((float)data.leftEye.eyeballPosX, (float)data.leftEye.eyeballPosY);
            Eyes.leftEye.viewportX = lefteyePos.X;                          //mm relativo a la camara
            Eyes.leftEye.viewportY = lefteyePos.Y;                          //mm relativo a la camara
            Eyes.leftEye.pupilDistanceMm = (float)data.leftEye.eyeballPosZ; //mm relativo a la camara
            Eyes.leftEye.pupilDiameter = (float)data.leftEye.pupilDiameter; //En pixeles. Devuelve 0 cuando el ojo está cerrado. (Usar para el IsBlinking)
            Eyes.leftEye.pupilDiameterMm = (float)data.leftEye.pupilDiameter;
            Eyes.leftEye.isBlinking = data.leftEye.pupilDiameter == 0 ? true : false;

            Vector2 righteyePos = ScreenToViewport((float)data.rightEye.eyeballPosX, (float)data.rightEye.eyeballPosY);
            Eyes.rightEye.viewportX = righteyePos.X;                          //mm relativo a la camara
            Eyes.rightEye.viewportY = righteyePos.Y;                          //mm relativo a la camara
            Eyes.rightEye.pupilDistanceMm = (float)data.rightEye.eyeballPosZ; //mm relativo a la camara
            Eyes.rightEye.pupilDiameter = (float)data.rightEye.pupilDiameter; //En pixeles.
            Eyes.rightEye.pupilDiameterMm = (float)data.rightEye.pupilDiameter;
            Eyes.rightEye.isBlinking = data.rightEye.pupilDiameter == 0 ? true : false;



            if (InvokeRequired)
            {
                d.BeginInvoke(new RawDataDelegate(SharpClient_RawDataReceived), new object[] { data, userData });
            }
            else
            {
                // raw data only available with special license
                //rawDataLbl.Text = String.Format("Head: pitch({0:0}) distance({1:0})", data.head.headPitch, data.head.headPosZ);
                Debug.WriteLine($"RAW DATA RECEIVED no invoke: X: {data.intelliGazeX} Y: {data.intelliGazeY} LX: {data.leftEye.gazePositionX}");
                Debug.WriteLine(String.Format("IntelliGaze ({0:0},{1:0})", data.intelliGazeX, data.intelliGazeY));
            }
        }

        private void SharpClient_FixationReceived(ref Fixation data, IntPtr userData)
        {
            //USAR FIJACIONES COMO REEMPLAZO CUANDO LA LICENCIA SEA "DEMO"

            //Debug.WriteLine($"FIXATION received:  {data.timeStamp.ToString()} Duration: {data.duration.ToString()}");
            //Debug.WriteLine($"FIXATION COORD: X: {data.positionX} Y: {data.positionY}");
            //UpdateGazePointFromScreen((float)data.positionX, (float)data.positionY);

            if (InvokeRequired)
            {
                d.BeginInvoke(new FixationDelegate(SharpClient_FixationReceived), new object[] { data, userData });
            }
            else
            {
                Debug.WriteLine("Fix Start: " + data.timeStamp.ToString() + " Duration: " + data.duration.ToString());
            }
        }

        void UpdateGazePointFromScreen(float screenX, float screenY)
        {
            if (screenSize.X == 0 || screenSize.Y == 0) return;
            if (screenX == 0 || screenY == 0) return;
            if (screenX == -1 || screenY == -1) return;

            screenX = Math.Clamp(screenX, 0, screenSize.X);
            screenY = Math.Clamp(screenY, 0, screenSize.Y);
            float x = screenX / screenSize.X;
            float y = screenY / screenSize.Y;
            gazePointCache = new GazePoint(new Vector2(x, y));
            rawGazePointCache = new GazePoint(new Vector2(x, y));
        }

        Vector2 ScreenToViewport(float screenX, float screenY)
        {
            if (screenSize.X == 0 || screenSize.Y == 0) return Vector2.Zero;

            screenX = Math.Clamp(screenX, 0, screenSize.X);
            screenY = Math.Clamp(screenY, 0, screenSize.Y);
            float x = screenX / screenSize.X;
            float y = screenY / screenSize.Y;
            return new Vector2(x, y);
        }

        private void SharpClient_BlinkReceived(ref Blink data, IntPtr userData)
        {

            if (InvokeRequired)
            {
                d.BeginInvoke(new BlinkDelegate(SharpClient_BlinkReceived), new object[] { data, userData });
            }
            else
            {
                Debug.WriteLine("Blink Start: " + data.timeStamp.ToString() + " Duration: " + data.duration.ToString());
            }
        }

        private void SharpClient_SaccadeReceived(ref Saccade data, IntPtr userData)
        {
            if (InvokeRequired)
            {
                d.BeginInvoke(new SaccadeDelegate(SharpClient_SaccadeReceived), new object[] { data, userData });
            }
            else
            {
                Debug.WriteLine("Saccade Start: " + data.timeStamp.ToString() + " Duration: " + data.duration.ToString());
            }
        }

        private void SharpClient_NoEventReceived(ref NoEventData data, IntPtr userData)
        {
            if (InvokeRequired)
            {
                d.BeginInvoke(new NoEventDelegate(SharpClient_NoEventReceived), new object[] { data, userData });
            }
            else
            {
                Debug.WriteLine("NoEvent Start: " + data.timeStamp.ToString());
            }
        }

        private void SharpClient_EyeStatusReceived(ref EyeStatus eyeStatus, IntPtr userData)
        {
            if (InvokeRequired)
            {
                d.BeginInvoke(new EyeStatusDelegate(SharpClient_EyeStatusReceived), new object[] { eyeStatus, userData });
            }
            else
            {
                String s = String.Format("LX({0:0.00})  LY({1:0.00})  LZ({2:0.00}) RX({3:0.00})  RY({4:0.00})  RZ({5:0.00})", eyeStatus.leftEyeX, eyeStatus.leftEyeY, eyeStatus.leftEyeZ, eyeStatus.rightEyeX, eyeStatus.rightEyeY, eyeStatus.rightEyeZ);
                Debug.WriteLine(s);
            }
        }

        private void SharpClient_EyeVisibilityReceived(ref int leftEye, ref int rightEye, IntPtr userData)
        {
            if (InvokeRequired)
            {
                d.BeginInvoke(new EyeVisibilityDelegate(SharpClient_EyeVisibilityReceived), new object[] { leftEye, rightEye, userData });
            }
            else
            {
                String s = String.Format("Left: {0:0} Right: {1:0}", leftEye, rightEye);
                Debug.WriteLine(s);
            }
        }

        private void SharpClient_CaliDoneReceived(ref Int32 data, Boolean improvement, IntPtr userData)
        {
            if (InvokeRequired)
            {
                d.BeginInvoke(new CalibrationDoneDelegate(SharpClient_CaliDoneReceived), new object[] { data, improvement, userData });
            }
            else
            {
                AddReport("Calibration Done: " + data.ToString());
            }
        }

    }
}
