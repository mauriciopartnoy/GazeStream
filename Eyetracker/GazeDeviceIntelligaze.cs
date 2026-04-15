using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Alea.Api;
using System.Diagnostics;
using System.Windows.Threading;

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

        public bool IsConnected => true;

        public bool UserIsPresent => true;

        public GazePoint GazePoint => throw new NotImplementedException();

        public GazePoint RawGazePoint => throw new NotImplementedException();

        public EyesData Eyes => throw new NotImplementedException();

        bool InvokeRequired => Dispatcher.FromThread(Thread.CurrentThread) == App.Current.Dispatcher;

        Dispatcher d => App.Instance.Dispatcher;

        public void Disconnect()
        {
            _api.ExitServer();
            _api.Close();
        }

        public bool Initialize()
        {
            ApiError error;
            do
            {
                bool isOpen;
                error = _api.IsOpen(out isOpen);
                Debug.WriteLine($"Intelligaze server is open: {isOpen} Error: {error.ToString()}");
                error = _api.Open("API-aw6oo-yrrhc","127.0.0.1", 27412, "127.0.0.1", 27413);
                Thread.Sleep(1000);
                Debug.WriteLine($"Trying to open Intelligaze API + {error.ToString()}");
            }
            while (error != ApiError.NoError);

            Debug.WriteLine("Intelligaze API is open. Setting callbacks!");

            // install data callbacks
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

            // connect passive callbacks to API
            IntPtr p;

            if (IntPtr.Size == 8) // x64
            {
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
            return true;
        }

        public void OpenCalibrationPage()
        {
            throw new NotImplementedException();
        }

        public void RequestCalibration(int pointsArray, int eyes)
        {
            throw new NotImplementedException();
        }

        public void UpdateData()
        {
            throw new NotImplementedException();
        }

        private void UpdateDataStreaming()
        {
            ApiError error = ApiError.NoError;
            int mode = 0;

            // stream eye events
            if (true/*eventStreamBox.Checked == true*/)
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

            //SPECIAL LICENCE RAW DATA

            //// stream raw events (special license required)
            //if (streamChk.Checked == true)
            //{
            //    mode |= 1;
            //    IntPtr p = Marshal.GetFunctionPointerForDelegate(RawDataReceived);
            //    if (IntPtr.Size == 8) // x64
            //    {
            //        _api.SetRawDataCB64(p.ToInt64(), IntPtr.Zero);
            //    }
            //    else
            //    {
            //        _api.SetRawDataCB(p.ToInt32(), IntPtr.Zero);
            //    }
            //}
            //else
            //{
            //    if (IntPtr.Size == 8) // x64
            //    {
            //        _api.SetRawDataCB64(0, IntPtr.Zero);
            //    }
            //    else
            //    {
            //        _api.SetRawDataCB(0, IntPtr.Zero);
            //    }
            //}

            error = _api.DataStreaming(mode);

            if (error != ApiError.NoError)
            {
                Debug.WriteLine($"Intelligaze ERROR: {error.ToString()}");
            }
            else
            {
                Debug.WriteLine("Intelligaze started streaming data.");
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
            if (InvokeRequired)
            {
                d.BeginInvoke(new ResultCalibrationDelegate(SharpClient_ResultReceived), new object[] { result, userData });
            }
            else
            {
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
            if (InvokeRequired)
            {
                d.BeginInvoke(new RawDataDelegate(SharpClient_RawDataReceived), new object[] { data, userData });
            }
            else
            {
                // raw data only available with special license
                //rawDataLbl.Text = String.Format("Head: pitch({0:0}) distance({1:0})", data.head.headPitch, data.head.headPosZ);
                Debug.WriteLine(String.Format("IntelliGaze ({0:0},{1:0})", data.intelliGazeX, data.intelliGazeY));
            }
        }

        private void SharpClient_FixationReceived(ref Fixation data, IntPtr userData)
        {
            if (InvokeRequired)
            {
                d.BeginInvoke(new FixationDelegate(SharpClient_FixationReceived), new object[] { data, userData });
            }
            else
            {
                Debug.WriteLine("Fix Start: " + data.timeStamp.ToString() + " Duration: " + data.duration.ToString());
            }
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
