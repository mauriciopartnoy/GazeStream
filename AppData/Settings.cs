using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GazeStream.Utilities.Save;

namespace GazeStream.AppData
{
    public class Settings
    {
        public static Settings I { get; private set; }

        public InvensunSmoothFilterSetting SmoothFilter { get; } = new();
        public KalmanFilterSetting KalmanFilter { get; } = new();
        public InterpolationFilterSetting InterpolationFilter { get; } = new();
        public BubbleToggleSetting BubbleToggle { get; } = new();
        public MouseToggleSetting MouseToggle { get; } = new();
        public SampleRateHzSetting SampleRateHZ { get; } = new();
        public LastCalibrationSetting LastCalibrationBuff { get; } = new();
        public LastCalibrationEyesSetting LastEyesOption { get; } = new();
        public LastCalibrationPointsOptionSetting LastPointsOption { get; } = new();
        public FilterProfileSetting FilterProfile { get; } = new();
        

        public Settings()
        {
            //IMPORTANTE: Si cambiamos a un modelo de settings por perfil migrar los valores de System Settings a User Settings en el SaveManager.
            I = this;
            SaveManager.LoadSystemSettings();
        }

        public void LoadSettings()
        {
            SmoothFilter.LoadSetting();
            KalmanFilter.LoadSetting();
            InterpolationFilter.LoadSetting();
            BubbleToggle.LoadSetting();
            MouseToggle.LoadSetting();
            SampleRateHZ.LoadSetting();
            LastCalibrationBuff.LoadSetting();
            LastEyesOption.LoadSetting();
            LastPointsOption.LoadSetting();
            FilterProfile.LoadSetting();
            FilterProfile.HookProfileSettings();
        }

        public void SaveSettings()
        {
            SaveManager.SaveSystemSettings();
            SaveManager.SaveGame();
        }
    }

    public abstract class Setting<T>
    {
        public event Action OnChanged;
        public event Action<T> OnValueChanged;
        protected abstract string SaveKey { get; }
        protected abstract T DefaultValue { get; }
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
            }
        }
        
        public void LoadSetting()
        {
            valueCache = SaveManager.GetSystemSetting<T>(SaveKey, DefaultValue);
            valueCache = NormalizeValue(valueCache);
            OnValueChanged?.Invoke(valueCache);
        }

        void SaveSetting()
        {
            SaveManager.SetSystemSetting<T>(SaveKey, Value);
        }
        protected virtual T NormalizeValue(T input) { return input; }

        
    }

    public interface ISettingsUser
    {
        void LoadSettings();
        void SubscribeToSettings();
    }

    public class InvensunSmoothFilterSetting : Setting<int>
    {
        protected override string SaveKey => "InvensunSmoothValue";

        protected override int DefaultValue => 10;

        protected override int NormalizeValue(int input)
        {
            int clampedInput = Math.Clamp(input, 1, 10);
            return clampedInput;
        }
    }

    public class InterpolationFilterSetting : Setting<float>
    {
        protected override string SaveKey => "InterpolationFilter";

        protected override float DefaultValue => 0.05f;

        protected override float NormalizeValue(float input)
        {
            float clampedInput = Math.Clamp(input, 0f, 1f);
            return clampedInput;
        }
    }

    public class KalmanFilterSetting : Setting<int>
    {
        protected override string SaveKey => "KalmanFilter";

        protected override int DefaultValue => 20;

        protected override int NormalizeValue(int input)
        {
            int clampedInput = Math.Clamp(input, 0, 100);
            return clampedInput;
        }
    }

    public class SampleRateHzSetting : Setting<int>
    {
        protected override string SaveKey => "SampleRateHertz";

        protected override int DefaultValue => 30;

        protected override int NormalizeValue(int input)
        {
            int clampedInput = Math.Clamp(input, 30, 120);
            return clampedInput;
        }
    }

    public class LastCalibrationSetting : Setting<byte[]>
    {
        protected override string SaveKey => "LastCalibration";
        protected override byte[] DefaultValue => new byte[0];
    }

    //Index de la opción seleccionada para la calibración
    public class LastCalibrationEyesSetting : Setting<int>
    {
        protected override string SaveKey => "LastCalibrationEyesOption";
        protected override int DefaultValue => 0;
    }

    //Index de la opción seleccionada para la calibración
    public class LastCalibrationPointsOptionSetting : Setting<int>
    {
        protected override string SaveKey => "LastCalibrationPointsOption";
        protected override int DefaultValue => 0;
    }

    public class MouseToggleSetting : Setting<bool>
    {
        protected override string SaveKey => "MouseToggle";
        protected override bool DefaultValue => false;
    }

    public class BubbleToggleSetting : Setting<bool>
    {
        protected override string SaveKey => "BubbleToggle";
        protected override bool DefaultValue => false;
    }

    //PROFILE SETTINGS
    public class FilterProfileSetting : Setting<FilterProfile>
    {
        public void HookProfileSettings()
        {
            this.OnValueChanged += OnFilterChanged;
            Settings.I.InterpolationFilter.OnChanged += SetCustomProfileOnFilterChange;
            Settings.I.KalmanFilter.OnChanged += SetCustomProfileOnFilterChange;
            Settings.I.SmoothFilter.OnChanged += SetCustomProfileOnFilterChange;
        }

        bool aplyingProfile;
        protected override string SaveKey => "FilterProfile";
        protected override FilterProfile DefaultValue => FilterProfile.Medio;

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
}
