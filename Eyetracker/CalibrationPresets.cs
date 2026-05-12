using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;
using Newtonsoft.Json;
using GazeStream.AppData;
using System.IO;
using System.Diagnostics;

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
            LoadPresets();
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
        public int calibrationBuffSize;
        public int calibrationPoints;
        public int eyeOption;
        public float scoreLeft;
        public float scoreRight;
        public float distanceToScreen;
        public Vector3 gazeOrigin;
        public float degAngle;
        public EyesData eyesData;
        public byte[] calibrationBuff;
        public string hardCodeBufString;
        public CalibrationPreset(string displayName, float scoreLeft, float scoreRight, Vector2 screenSize, Vector3 gazeOrigin, EyesData eyesData, int points, int eyeOption, byte[] calibrationBuff)
        {
            this.displayName = displayName;
            this.screenSize = screenSize;
            this.calibrationBuffSize = calibrationBuff.Length;
            this.calibrationPoints = points;
            this.eyeOption = eyeOption;
            this.scoreLeft = scoreLeft;
            this.scoreRight = scoreRight;
            this.distanceToScreen = Math.Max(eyesData.leftEye.pupilDistanceMm, eyesData.rightEye.pupilDistanceMm);
            this.gazeOrigin = gazeOrigin;
            this.degAngle = GetEyesAngle(eyesData);
            this.eyesData = eyesData;
            this.hardCodeBufString = HardCodeString(displayName, calibrationBuff);
            this.calibrationBuff = calibrationBuff;
        }

        float GetEyesAngle(EyesData data)
        {
            Vector2 a = data.leftEye.GetViewportPos();
            Vector2 b = data.rightEye.GetViewportPos();
            Vector2 c = b - a;
            return (float)(Math.Atan2(c.Y, c.X) * 180f / Math.PI);
        }

        private string HardCodeString(string name, byte[] buf)
        {
            StringBuilder bufBytesArray = new StringBuilder("byte[] ");
            bufBytesArray.Append(name);
            bufBytesArray.Append(" new byte[] {");
            for (int i = 0; i < buf.Length; i++)
            {
                bufBytesArray.Append(buf[i]);
                bufBytesArray.Append(",");
            }
            bufBytesArray.Remove(bufBytesArray.Length - 1, 1);
            bufBytesArray.Append("}");
            Debug.WriteLine(bufBytesArray.ToString());
            return bufBytesArray.ToString();
        }

    }
}
