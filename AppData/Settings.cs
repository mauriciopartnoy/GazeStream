using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GazeStream.Utilities.Save;
using System.Diagnostics;
using GazeStream.Utilities.Events;
namespace GazeStream.AppData
{
    public class SettingKeys
    {
        public const string SAMPLE_RATE_HZ = "SampleRateHertz";
        public const string MOUSE_TOGGLE = "MouseToggle";
        public const string BUBBLE_TOGGLE = "BubbleToggle";
        public const string BUBBLE_COLOR_SELECTION = "BubbleColorSelection";
        public const string BUBBLE_OPACITY = "BubbleOpacity";
        public const string BUBBLE_SIZE = "BubbleSize";
        public const string CURSOR_TYPE = "CursorType";
        public const string CURSOR_CUSTOM_PATH = "CursorCustomPath";
        public const string FILTER_PROFILE = "FilterProfile";
        public const string JOACO_SMOOTH_FILTER = "InvensunSmoothValue";
        public const string KALMAN_FILTER = "KalmanFilter";
        public const string INTERPOLATION_FILTER = "InterpolationFilter";
        public const string LAST_CALIBRATION_BUFF = "LastCalibrationBuff";
        public const string LAST_CALIBRATION_EYES_OPTION = "LastCalibrationEyesOption";
        public const string LAST_CALIBRATION_POINTS_OPTION = "LastCalibrationPointsOption";
    }
    public class Settings
    {
        public static Settings I { get; private set; }
        public static Dictionary<string, BaseSetting> BaseSettings { get; private set; } = new Dictionary<string, BaseSetting>();
        public static Dictionary<string, SettingDescriptor> Descriptors { get; private set; } = new Dictionary<string, SettingDescriptor>();


      
        //TRACKING
        public IntSetting SampleRateHZ { get; } = new IntSetting(SettingKeys.SAMPLE_RATE_HZ, 60, new Int2(30, 120)); 
        public BoolSetting MouseToggle { get; } = new BoolSetting(SettingKeys.MOUSE_TOGGLE, false);

        //BUBBLE CURSOR
        public BoolSetting BubbleToggle { get; } = new BoolSetting(SettingKeys.BUBBLE_TOGGLE, false);
        public IntSetting BubbleColor { get; } = new IntSetting(SettingKeys.BUBBLE_COLOR_SELECTION, 0);
        public FloatSetting BubbleOpacity { get; } = new FloatSetting(SettingKeys.BUBBLE_OPACITY, 0.6f, new Float2(0f, 1f));
        public IntSetting BubbleSize { get; } = new IntSetting(SettingKeys.BUBBLE_SIZE, 40, new Int2(1, 200));
        public Setting<CursorVisualType> CursorTypeEnum { get; } = new Setting<CursorVisualType>(SettingKeys.CURSOR_TYPE, CursorVisualType.Bubble);
        public Setting<string> CustomCursorPath { get; } = new Setting<string>(SettingKeys.CURSOR_CUSTOM_PATH, string.Empty);

        //FILTERS
        public FilterProfileSetting FilterProfile { get; } = new FilterProfileSetting(SettingKeys.FILTER_PROFILE);
        public IntSetting SmoothFilter { get; } = new IntSetting(SettingKeys.JOACO_SMOOTH_FILTER, 10, new Int2(1, 10));
        public IntSetting KalmanFilter { get; } = new IntSetting(SettingKeys.KALMAN_FILTER, 20, new Int2(0, 100));
        public FloatSetting InterpolationFilter { get; } = new FloatSetting(SettingKeys.INTERPOLATION_FILTER, 0.05f, new Float2(0, 100));

        //CALIBRATION 
        public LastCalibrationSetting LastCalibrationBuff { get; } = new LastCalibrationSetting("LastCalibrationBuff");                                 //byte[] Calibration.buff
        public IntSetting LastEyesOption { get; } = new IntSetting("LastCalibrationEyesOption", 0);         //Index de la opción
        public IntSetting LastPointsOption { get; } = new IntSetting("LastCalibrationPointsOption", 0);     //Index de la opción


        public Settings()
        {
            //IMPORTANTE: Si cambiamos a un modelo de settings por perfil migrar los valores de System Settings a User Settings en el SaveManager.
            I = this;
            SaveManager.LoadSystemSettings();
            GlobalEvents.OnSettingChangeCommand.Add(SetValue);
        }

        public static void RegisterDescriptor(SettingDescriptor descriptor)
        {
            Descriptors.Add(descriptor.saveKey, descriptor);
        }

        void SetValue(string key, object value)
        {
            Debug.WriteLine($"Set value was called for key. {key} {value} ");
            BaseSettings[key].SetValue(value);

        }

        public void Initialize()
        {
            RegisterSettings();
            LoadAllSettings();
            HookProfileSettings();
        }

        private void HookProfileSettings()
        {
            //TODO: Los profiles son una clase especial de Setting que responde a cambios en otros settings. Si escalan hacer una clase aparte. 
            FilterProfile.HookProfileSettings();
        }

        private void RegisterSettings()
        {
            RegisterSetting(SampleRateHZ);
            RegisterSetting(MouseToggle);

            RegisterSetting(BubbleToggle);
            RegisterSetting(BubbleColor);
            RegisterSetting(BubbleOpacity);
            RegisterSetting(BubbleSize);
            RegisterSetting(CursorTypeEnum);
            RegisterSetting(CustomCursorPath);

            RegisterSetting(SmoothFilter);
            RegisterSetting(KalmanFilter);
            RegisterSetting(InterpolationFilter);

            RegisterSetting(LastCalibrationBuff);
            RegisterSetting(LastEyesOption);
            RegisterSetting(LastPointsOption);

            RegisterSetting(FilterProfile);
        }

        public void RegisterSetting(BaseSetting setting)
        {
            BaseSettings.Add(setting.Descriptor.saveKey, setting);
        }

        public void LoadAllSettings()
        {
            foreach (var setting in BaseSettings.Values)
            {
                setting.LoadSetting();
            }
        }

        public void SaveSettings()
        {
            SaveManager.SaveSystemSettings();
            SaveManager.SaveGame();
        }
    }

    public abstract class BaseSetting
    {
        public abstract void SetValue(object value);
        public abstract void LoadSetting();
        public abstract SettingDescriptor Descriptor { get; }
    }
    public abstract class BaseSetting<T> : BaseSetting
    {
        public event Action OnChanged;
        public event Action<T> OnValueChanged;

        T valueCache;
        public T Value 
        { 
            get { return valueCache; }
            set 
            {
                if (EqualityComparer<T>.Default.Equals(value)) return;
                valueCache = NormalizeValue(value);
                SaveSetting();
                OnChanged?.Invoke();
                OnValueChanged?.Invoke(valueCache);
                GlobalEvents.OnSettingChanged.Invoke(Descriptor.saveKey);
            }
        }

        public override void SetValue(object value)
        {
            if (value is T typed)
            {
                Value = typed;
            }
            else
            {
                Debug.WriteLine($"Invalid value type. Expected {typeof(T)}, got {value?.GetType()}");
            }
        }
        public override void LoadSetting()
        {
            Settings.RegisterDescriptor(Descriptor);
            valueCache = SaveManager.GetSystemSetting<T>(Descriptor.saveKey, (T)Descriptor.defaultValue);
            valueCache = NormalizeValue(valueCache);
            OnValueChanged?.Invoke(valueCache);
        }

        void SaveSetting()
        {
            SaveManager.SetSystemSetting<T>(Descriptor.saveKey, Value);
            SaveManager.SaveSystemSettings();
        }
        protected virtual T NormalizeValue(T input) { return input; }

        
    }

    public struct SettingDescriptor
    {
        public string saveKey;
        public Type type;
        public string typeAsString;
        public object defaultValue;

        public SettingDescriptor(string key, Type type, object defaultValue)
        {
            this.saveKey = key;
            this.type = type;
            this.typeAsString = type.Name;
            this.defaultValue = defaultValue;
        }
    }

    public interface ISettingsUser
    {
        void LoadSettings();
        void SubscribeToSettings();
    }

    public class Setting<T> : BaseSetting<T>
    {
        public Setting(string saveKey, T defaultValue)
        {
            this.descriptor = new SettingDescriptor(saveKey, typeof(T), defaultValue);
        }
        public override SettingDescriptor Descriptor => descriptor;
        SettingDescriptor descriptor;      
    }

    public class IntSetting : BaseSetting<int>
    {
        SettingDescriptor descriptor;
        Int2? clampRange;
        public override SettingDescriptor Descriptor => descriptor;

        public IntSetting(string saveKey, int defaultValue = 0, Int2? clampRange = null)
        {
            if (clampRange.HasValue && clampRange.Value.x > clampRange.Value.y)
            {
                throw new Exception($"Setting value {saveKey} has an invalid value range.");
            }

            this.descriptor = new SettingDescriptor(saveKey, typeof(int), NormalizeValue(defaultValue));
            this.clampRange = clampRange;
        }

        protected override int NormalizeValue(int input)
        {
            if (!clampRange.HasValue) return input;

            Int2 clamp = clampRange.Value;
            int normalized = Math.Clamp(input, clamp.x, clamp.y);
            return normalized;
        }
    }

    public class FloatSetting : BaseSetting<float>
    {

        SettingDescriptor descriptor;
        Float2? clampRange;
        public override SettingDescriptor Descriptor => descriptor;

        public FloatSetting(string saveKey, float defaultValue = 0, Float2? clampRange = null)
        {
            if (clampRange.HasValue && clampRange.Value.x > clampRange.Value.y)
            {
                throw new Exception($"Setting value {saveKey} has an invalid value range.");
            }


            this.descriptor = new SettingDescriptor(saveKey, typeof(float), NormalizeValue(defaultValue));
            this.clampRange = clampRange;
        }

        protected override float NormalizeValue(float input)
        {
            if (!clampRange.HasValue) return input;

            Float2 clamp = clampRange.Value;
            float normalized = Math.Clamp(input, clamp.x, clamp.y);
            return normalized;
        }
    }

    public class BoolSetting : BaseSetting<bool>
    {
        SettingDescriptor descriptor;
        public override SettingDescriptor Descriptor => descriptor;
        public BoolSetting(string saveKey, bool defaultValue = false)
        {
            this.descriptor = new SettingDescriptor(saveKey, typeof(bool), defaultValue);

        }
    }

    public class LastCalibrationSetting : BaseSetting<byte[]>
    {
        SettingDescriptor descriptor;
        public override SettingDescriptor Descriptor => descriptor;
        public LastCalibrationSetting(string saveKey)
        {
            descriptor = new SettingDescriptor(saveKey, typeof(byte[]), new byte[0]);
        }
      
    }

    //PROFILE SETTINGS
    public class FilterProfileSetting : BaseSetting<FilterProfile>
    {
        SettingDescriptor descriptor;
        public override SettingDescriptor Descriptor => descriptor;

        public FilterProfileSetting(string saveKey)
        {
            descriptor = new SettingDescriptor(saveKey, typeof(FilterProfile), FilterProfile.Medio);

        }
        public void HookProfileSettings()
        {
            this.OnValueChanged += OnFilterChanged;
            Settings.I.InterpolationFilter.OnChanged += SetCustomProfileOnFilterChange;
            Settings.I.KalmanFilter.OnChanged += SetCustomProfileOnFilterChange;
            Settings.I.SmoothFilter.OnChanged += SetCustomProfileOnFilterChange;
        }

        bool aplyingProfile;
        void OnFilterChanged(FilterProfile profile)
        {
            //TODO: Agregar Interpolation/Smooth Damp
            aplyingProfile = true;
            switch (profile)
            {
                case FilterProfile.Bajo:
                    Settings.I.KalmanFilter.Value = 0;
                    Settings.I.SmoothFilter.Value = 1;
                    break;
                case FilterProfile.Medio:
                    Settings.I.KalmanFilter.Value = 20;
                    Settings.I.SmoothFilter.Value = 10;
                    break;
                case FilterProfile.Alto:
                    Settings.I.KalmanFilter.Value = 30;
                    Settings.I.SmoothFilter.Value = 10;
                    break;
                case FilterProfile.Custom:
                    break;
            }
            aplyingProfile = false;
        }

      
        void SetCustomProfileOnFilterChange()
        {
            if (aplyingProfile) return;
            this.Value = FilterProfile.Custom;
        }
    }

    public enum FilterProfile { Bajo, Medio, Alto, Custom  }

    public struct Float2
    {
        public float x;
        public float y;

        public Float2(float x, float y)
        {
            this.x = x;
            this.y = y;
        }
    }

    public struct Int2
    {
        public int x;
        public int y;

        public Int2(int x, int y)
        {
            this.x = x;
            this.y = y;
        }
    }
}
