using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;
using Newtonsoft.Json;
using GazeStream.AppData;
using System.IO;

namespace GazeStream.Eyetracker
{
    public class CalibrationPresets
    {
        public static CalibrationPresets I;
        List<CalibrationPreset> Presets = new();
        const string FILE_NAME = "CalibrationPresets.json";
        public string FilePath => Path.Combine(AppPaths.CalibrationPresetsPath, FILE_NAME);

        public CalibrationPresets()
        {
            I = this;
            LoadPresets();
        }
        public void AddPreset(CalibrationPreset preset)
        {
            Presets.Add(preset);
            FileOps.SaveToJson(FilePath, Presets);
        }

        public void LoadPresets()
        {
            Presets = FileOps.LoadFromJson(FilePath, new List<CalibrationPreset>());
        }
    }

    public class CalibrationPreset
    {
        public string displayName;
        public Vector2 screenSize;
        public EyesData eyesData;
        public float degAngle;
        public float distanceToScreen;
        public byte[] calibrationBuff;
        public int points;
        public int eyesOption;
        public CalibrationPreset(string displayName, Vector2 screenSize, EyesData eyesData, int pointsOption, int points, byte[] calibrationBuff)
        {
            this.displayName = displayName;
            this.screenSize = screenSize;
            this.eyesData = eyesData;
            this.distanceToScreen = Math.Max(eyesData.leftEye.pupilDistanceMm, eyesData.rightEye.pupilDistanceMm);
            this.points = pointsOption;
            this.eyesOption = points;
            this.calibrationBuff = calibrationBuff;
            this.degAngle = GetEyesAngle(eyesData);
        }

        float GetEyesAngle(EyesData data)
        {
            Vector2 a = data.leftEye.GetViewportPos();
            Vector2 b = data.rightEye.GetViewportPos();
            Vector2 c = b - a;
            return (float)(Math.Atan2(c.Y, c.X) * 180f / Math.PI);
        }
    }
}
