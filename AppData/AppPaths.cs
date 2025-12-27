using System;
using System.IO;

namespace GazeStream.AppData
{
    public static class AppPaths
    {
        public const string COMPANY_NAME = "Neufitech";
        public const string APP_NAME = "GazeStream";
        public static string BasePath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), COMPANY_NAME, APP_NAME);
        public static string EyetrackerPath => Path.Combine(BasePath, "Eyetracker");

        public static string EyetrackerConfigPath
        {
            get
            {
                string path = Path.Combine(EyetrackerPath, "config");
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
