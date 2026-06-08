using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tobii.InteractionLib;
using GazeStream.Utilities;
using System.Numerics;
using System.Diagnostics;

namespace GazeStream.Eyetracker
{
    public class GazeDeviceTobiiInteraction : IGazeDevice
    {
        IInteractionLib intlib;

        public string DeviceName => "Tobii Interaction";

        public bool IsConnected => HasStreamCapability();

        public bool UserIsPresent { get; private set; } = false;
        public GazePoint GazePoint => gazePointCache;
        GazePoint gazePointCache = new();
        public GazePoint RawGazePoint { get; private set; } = new GazePoint(Vector2.Zero);

        DateTime lastPointReceived;
        Vector2 screenSize;


        public EyesData Eyes { get; private set; } = new EyesData();
        public bool Initialize()
        {
            Debug.WriteLine("Initializing Tobii Interaction");
            intlib = InteractionLibFactory.CreateInteractionLib(FieldOfUse.Interactive);
            screenSize = Helper.GetPrimaryMonitorSize();
            Debug.WriteLine($"Monitor size {screenSize.X}*{screenSize.Y}");
            intlib.CoordinateTransformAddOrUpdateDisplayArea(screenSize.X, screenSize.Y);
            intlib.CoordinateTransformSetOriginOffset(0, 0);
            UnsubscribeToEvents();
            SubscribeToEvents();
            Thread.Sleep(500);
            return HasStreamCapability();
        }

        bool HasStreamCapability()
        {
            if (intlib == null) return false;
            Capability cap;
            intlib.GetDataStreamCapability(StreamType.GazePointData, out cap);
            return cap == Capability.Enabled || cap == Capability.Available;
        }

        void UnsubscribeToEvents()
        {
            if (intlib == null) return;

            intlib.GazePointDataEvent -= UpdateGazePoint;
            intlib.PresenceDataEvent -= Intlib_PresenceDataEvent;
            intlib.GazeOriginDataEvent -= Intlib_GazeOriginDataEvent;
            intlib.HeadPoseDataEvent -= Intlib_HeadPoseDataEvent;
        }

        void SubscribeToEvents()
        {
            if (intlib == null) return;
            UnsubscribeToEvents();
            intlib.GazePointDataEvent += UpdateGazePoint;
            intlib.PresenceDataEvent += Intlib_PresenceDataEvent;
            intlib.GazeOriginDataEvent += Intlib_GazeOriginDataEvent;
            intlib.HeadPoseDataEvent += Intlib_HeadPoseDataEvent;
        }

       

        private void Intlib_HeadPoseDataEvent(HeadPoseData gazePointData)
        {
            //Head Pose Data
        }

        private void Intlib_GazeOriginDataEvent(GazeOriginData eye)
        {
            int pupilDiameter = 1;
            int pupilDiameterMm = 8;

            Eye left = new();
            left.viewportX = eye.left_x;
            left.viewportY = eye.left_y;
            left.pupilDiameter = pupilDiameter;
            left.pupilDiameterMm = pupilDiameterMm;
            left.pupilDistanceMm = eye.left_z;
            left.isBlinking = (eye.leftValidity == Validity.Invalid);

            Eye right = new();
            right.viewportX = eye.right_x;
            right.viewportY = eye.right_y;
            right.pupilDiameter = pupilDiameter;
            right.pupilDiameterMm = pupilDiameterMm;
            right.pupilDistanceMm = eye.right_z;
            right.isBlinking = (eye.rightValidity == Validity.Invalid);

            Eyes.leftEye = left;
            Eyes.rightEye = right;
        }

        private void Intlib_PresenceDataEvent(PresenceData presenceData)
        {
            UserIsPresent = (presenceData.presence == Presence.Present);
            Debug.WriteLine("Tobii user is present: " + UserIsPresent);
        }

        void UpdateGazePoint(GazePointData data)
        {
            if (data.validity == Validity.Valid)
            {
                //Esta librería no provee el dato crudo del eyetracker por lo cual se envía el punto con el filtro por default.
                float x = data.x / screenSize.X;
                float y = data.y / screenSize.Y;
                lastPointReceived = DateTime.Now;
                gazePointCache = new GazePoint(new Vector2(x, y));
                RawGazePoint = new GazePoint(new Vector2(x, y)); 
            }
        }

        public void Disconnect()
        {
            if (intlib == null) return;
            UnsubscribeToEvents();
            intlib.Dispose();
            intlib = null;
        }
        public void UpdateData()
        {
            if (intlib == null) return;
            intlib.Update();
        }


        public void OpenCalibrationPage()
        {
            return;
        }

        public void RequestCalibration(int pointsArray, int eyes)
        {
            return;
        }

        public void ShowCameraPreview(bool show)
        {
            return;
        }
    }
}
