using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GazeStream.AppData;

namespace GazeStream.Utilities.Save
{
    internal static class SaveManager
    {

        public static SaveData ActiveSlot { get { if (activeSlot == null) { activeSlot = new SaveData(); } return activeSlot; } private set { activeSlot = value; } }
        public static bool IsProcessing { get; private set; }

        static SaveData activeSlot = new SaveData();
        static Dictionary<string, object> systemSettings = new Dictionary<string, object>();

        //PATHS
        public static string SaveDirectory => AppPaths.UsersPath;

        public static string ActiveUserSavePath => Path.Combine(AppPaths.ActiveUserPath, "UserSettings.dat");
        public static string SystemSettingsFilePath => Path.Combine(AppPaths.BasePath, "SystemSettings.dat");

        //SERVICES
        static IFileSaver FileSaver = new LocalFileSaver();
        //EVENTS
        public static event Action? OnSaveFinished;
        public static event Action? OnLoadFinished;
        public static event Action? OnBeforeSave;
        public static event Action? OnBeforeLoad;
        public static event Action? OnPersistentDataCapture;
        public static event Action? OnPersistentDataRestore;

        public static void SaveGame()
        {
            SaveGame(ActiveUserSavePath);
        }

        public static void LoadGame()
        {
            LoadGame(ActiveUserSavePath);
        }

        public static void SaveGame(string path, Action? onFinished = null)
        {
            if (IsProcessing) return;
            IsProcessing = true;
            OnBeforeSave?.Invoke();
            SaveSystemSettings();
            FileSaver.SaveToBinary<SaveData>(path, ActiveSlot);
            OnSaveFinished?.Invoke();
            onFinished?.Invoke();
            IsProcessing = false;
            Debug.WriteLine("Game Saved at: " + path);
        }

        public static void LoadGame(string path, Action? onFinished = null)
        {
            if (IsProcessing) return;
            IsProcessing = true;
            OnBeforeLoad?.Invoke();
            LoadSystemSettings();
            ActiveSlot = FileSaver.LoadFromBinary(path, new SaveData());
            OnLoadFinished?.Invoke();
            onFinished?.Invoke();
            OnPersistentDataRestore?.Invoke();
            IsProcessing = false;
            Debug.WriteLine("Game Loaded at: " + path);
        }

        public static void SaveSystemSettings()
        {
            Debug.WriteLine("Saving system settings.");
            FileSaver.SaveToBinary(SystemSettingsFilePath, systemSettings);
        }

        public static void LoadSystemSettings()
        {
            systemSettings = FileSaver.LoadFromBinary(SystemSettingsFilePath, new Dictionary<string, object>());
            Debug.WriteLine("System Settings Loaded.");
        }

        public static List<SaveData> GetAllSaveSlots()
        {
            List<SaveData> slots = new List<SaveData>();
            if (!Directory.Exists(SaveDirectory))
            {
                return slots;
            }

            string[] paths = Directory.GetFiles(SaveDirectory, "*.txt");

            foreach (string filePath in paths)
            {
                SaveData slot = FileSaver.LoadFromJson(filePath, new SaveData());
                slots.Add(slot);
            }
            return slots;
        }

        public static void DeleteSlot(string filePath)
        {
            FileSaver.DeleteFile(filePath);
        }

        public static void DeleteSaveDirectory()
        {
            FileSaver.DeleteDirectory(SaveDirectory);
        }


        #region SAVE VALUES

        public static void SetValue<T>(string key, T value)
        {
            if (activeSlot == null)
            {
                activeSlot = new SaveData();
            }
            ActiveSlot.GameData[key] = value;
        }

        public static T GetValue<T>(string key)
        {
            T val = default(T);
            if (activeSlot != null && ActiveSlot.GameData.ContainsKey(key))
            {
                val = (T)ActiveSlot.GameData[key];
            }
            return val;
        }

        public static T GetValue<T>(string key, T defaultValue)
        {
            T val = defaultValue;
            if (activeSlot != null && ActiveSlot.GameData.ContainsKey(key))
            {
                val = (T)ActiveSlot.GameData[key];
            }
            else
            {
                //Debug.WriteLine(key + " not found. Returning default value.");
            }
            return val;
        }

        //System Settings son settings generales independientes del slot.
        public static void SetSystemSetting<T>(string key, T value)
        {
            if (systemSettings == null)
            {
                systemSettings = new Dictionary<string, object>();
            }
            systemSettings[key] = value;
        }

        public static T GetSystemSetting<T>(string key)
        {
            T val = default(T);
            if (systemSettings != null && systemSettings.ContainsKey(key))
            {
                val = (T)systemSettings[key];
            }
            return val;
        }

        public static T GetSystemSetting<T>(string key, T defaultValue)
        {
            T val = defaultValue;
            if (systemSettings != null && systemSettings.ContainsKey(key))
            {
                val = (T)systemSettings[key];
            }
            return val;
        }

        #endregion
    }


[System.Serializable]
    public class SaveData
    {
        public Dictionary<string, object> GameData = new Dictionary<string, object>();
    }

}
