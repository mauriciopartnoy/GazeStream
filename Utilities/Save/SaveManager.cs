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
        static SaveData systemSettingsData = new SaveData();
        static Dictionary<string, object> systemSettings => systemSettingsData.data;

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
            FileSaver.SaveToBinary(SystemSettingsFilePath, systemSettingsData);
        }

        public static void LoadSystemSettings()
        {
            SaveData newSave = new SaveData();

            //GET FILE
            try
            {
                newSave = FileSaver.LoadFromBinary(SystemSettingsFilePath, new SaveData());
            }
            catch
            {
                newSave = new SaveData();
            }

            //CHECK VERSION
            if (newSave.version < SaveData.CURRENT_VERSION)
            {
                //Save old backup...?
                Debug.WriteLine("Save Version is older: Updating save.");
                newSave = new SaveData();
            }

            newSave.version = SaveData.CURRENT_VERSION;
            systemSettingsData = newSave;
            
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
            if (activeSlot == null || activeSlot.data == null)
            {
                activeSlot = new SaveData();
            }
            ActiveSlot.data[key] = value;
        }

        public static T GetValue<T>(string key)
        {
            T defaultValue = default(T);
            if (activeSlot != null && activeSlot.data != null && activeSlot.data.TryGetValue(key, out object value))
            {
                if (value is T typedValue)
                {
                    return typedValue;
                }

            }
            return defaultValue;
        }

        public static T GetValue<T>(string key, T defaultValue)
        {
            if (activeSlot != null && activeSlot.data != null && activeSlot.data.TryGetValue(key, out object value))
            {
                if (value is T typedValue)
                {
                    return typedValue;
                }

            }
            return defaultValue;
        }

        //System Settings son settings generales independientes del slot.
        public static void SetSystemSetting<T>(string key, T value)
        {
            if (systemSettingsData == null || systemSettingsData.data == null)
            {
                systemSettingsData.data = new Dictionary<string, object>();
            }
            systemSettingsData.data[key] = value;
        }

        public static T GetSystemSetting<T>(string key)
        {
            T defaultValue = default(T);
            if (systemSettings != null && systemSettings.TryGetValue(key, out object value))
            {
                if (value is T typedValue)
                {
                    return typedValue;
                }

            }
            return defaultValue;
        }

        public static T GetSystemSetting<T>(string key, T defaultValue)
        {
            if (systemSettings != null && systemSettings.TryGetValue(key, out object value))
            {
                if (value is T typedValue)
                {
                    return typedValue;
                }
                
            }
            return defaultValue;
        }

        #endregion
    }


    [System.Serializable]
    public class SaveData
    {
        public const int CURRENT_VERSION = 1;
        public int version; //Version updated when saving.
        public Dictionary<string, object> data = new Dictionary<string, object>();
    }

}
