using System;
using System.IO;

namespace GazeStream.AppData
{
    public static class AppPaths
    {
        public const string COMPANY_NAME = "Neufitech";
        public const string APP_NAME = "GazeStream";
        public const string GIT_REPOSITORY_URL = "https://github.com/mauriciopartnoy/GazeStream";
        public const string GIT_USERNAME = "mauriciopartnoy@gmail.com";
        public static string BasePath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), COMPANY_NAME, APP_NAME);
        public static string EyetrackerPath => Path.Combine(BasePath, "Eyetracker");

        public static string CalibrationPresetsPath
        {
            get
            {
                string path = Path.Combine(EyetrackerPath, "presets");
                Directory.CreateDirectory(path);
                return path;
            }
        }

        public static string EyetrackerConfigPath
        {
            get
            {
                string path = Path.Combine(EyetrackerPath, "config");
                Directory.CreateDirectory(path);
                return path;
            }
        }

        public static string CustomPath
        {
            get
            {
                string path = Path.Combine(BasePath, "custom");
                Directory.CreateDirectory(path);
                return path;
            }
        }

        public static string CustomCursorsPath
        {
            get
            {
                string path = Path.Combine(CustomPath, "cursors");
                Directory.CreateDirectory(path);
                return path;
            }
        }

        public static string CustomCalibrationPointsPath
        {
            get
            {
                string path = Path.Combine(CustomPath, "calibrationPoints");
                Directory.CreateDirectory(path);
                return path;
            }
        }

        public static string UsersPath
        {
            get
            {
                string path = Path.Combine(BasePath, "users");
                Directory.CreateDirectory(path);
                return path;
            }
        }

        public static string ActiveUserPath
        {
            get
            {
                string userFolderName = App.Instance.ActiveUser.UniqueFolderName;
                string path = Path.Combine(UsersPath, userFolderName);
                Directory.CreateDirectory(path);
                return path;
            }
        }
    }
}
