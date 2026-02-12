using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GazeStream.Utilities.Save;
using System.Diagnostics;
using GazeStream.Utilities.Events;
using GazeStream.AppData;

namespace GazeStream.AppData
{
    public class SettingKeys
    {
        public const string ACCESSIBILY_METHOD = "MetodoDeAccesibilidad";
        public const string LANGUAGE = "Language";
        public const string VOLUME = "Volume";

        public const string VOICE = "Voice";
        public const string VOICE_SPEED = "VoiceSpeed";
        public const string VOICE_VOLUME = "VoiceVolume";
        public const string VOICE_DEVICE = "VoiceDeviceIndex";

        public const string CLICK_FEEDBACK_TOGGLE = "ClickFeedbackToggle";
        public const string CLICK_ZOOM_TOGGLE = "ClickZoomToggle";
        public const string CLICK_ZOOM_LEVEL = "ClickZoomLevel";

        public const string SAMPLE_RATE_HZ = "SampleRateHertz";
        public const string MOUSE_TOGGLE = "MouseToggle";
        public const string BUBBLE_TOGGLE = "BubbleToggle";
        public const string BUBBLE_COLOR_SELECTION = "BubbleColorSelection";
        public const string BUBBLE_OPACITY = "BubbleOpacity";
        public const string BUBBLE_SIZE = "BubbleSize";
        public const string BUBBLE_OPACITY_ENUM = "BubbleOpacityEnum";
        public const string CURSOR_STYLE = "CursorStyle";
        public const string CURSOR_CUSTOM_PATH = "CursorCustomPath";
        public const string SHOW_RAW_GAZE = "ShowRawGazeAsDot";

        public const string FILTER_PROFILE = "FilterProfile";
        public const string JOACO_SMOOTH_FILTER = "InvensunSmoothValue";
        public const string KALMAN_FILTER = "KalmanFilter";
        public const string INTERPOLATION_FILTER = "InterpolationFilter";

        public const string LAST_CALIBRATION_BUFF = "LastCalibrationBuff";
        public const string LAST_CALIBRATION_EYES_OPTION = "LastCalibrationEyesOption";
        public const string LAST_CALIBRATION_POINTS_OPTION = "LastCalibrationPointsOption";
        public const string SAVE_CALIBRATION_AS_PRESET = "SaveCalibrationAsPresetToggle";
        public const string CALIBRATION_POINT_STYLE = "CalibrationPointStyle";
        public const string CALIBRATION_POINT_CUSTOM_IMAGE = "CalibrationPointCustomImagePath";


        //BOTONES (En castellano porque recibí una planilla de settings en castellano :v)
        public const string TIEMPO_INICIO_ACTIVACION = "TiempoDeInicioDeActivacion";
        public const string TIEMPO_ACTIVACION = "TiempoDeActivacion";
        public const string TIEMPO_ACTIVACION_TECLADO = "TiempoDeActivacionDeTeclado";
        public const string MULTIPLICADOR_VELOCIDAD_DESACTIVACION = "MultiplicadorDeVelocidadDeDesactivacion";
        public const string TIEMPO_PERMANENCIA = "TiempoDePermanenciaDeActivacionIncompleta";
        public const string MOSTRAR_PERMANENCIA = "MostrarPermanenciaDeActivacionIncompleta";

        public const string COLOR_SCHEME = "ColorScheme";
        public const string USER_INTERFACE_SIZE = "UserInterfaceSize";
        public const string INTERACTION_BAR_POSITION = "InteractionBarPosition";

        public const string KEYBOARD_SIZE = "KeyboardSize";
        public const string KEYBOARD_FONT_SIZE = "KeyboardFontSize";
        public const string KEYBOARD_TYPE = "KeyboardType";
        public const string WORD_PREDICTION = "WordPrediction";
        public const string WORD_PREDICTION_LEARNING = "WordPredictionLearning";

        public const string AUTO_HELP_CALL = "AutomaticHelpCall";
        public const string AUTO_HELP_CALL_TIME = "AutomaticHelpCallTime";
        public const string HELP_CALL_ACTION = "HelpCallAction";

    }
    public enum FilterProfile { Bajo, Medio, Alto, Custom }
    public enum AccessibilityMethod { Mouse, EyeTracker, Barrido, Touch }
    public enum Voices { Hombre, Mujer, Personalizada }
    public enum BubbleOpacityType { Regular, Oscuro, Contraste }
    public enum CalibrationPointStyle { Point, Image, Animation }
    public enum ColorScheme { System, Light, Dark }
    public enum InteractionBarPosition { Left, Right, Top, Bottom }
    public enum Size { Small, Medium, Large }
    public enum SizeModifier { x50, x75, x100 }
    public enum KeyboardType { QWERTY, ABC, PICTO }
    public enum HelpCallMode { DefaultSound, CustomSound, ExecuteFile }
    public enum Language { ESP, POR, ENG }

    public class Settings
    {
        public static Settings I { get; private set; }
        public static Dictionary<string, BaseSetting> BaseSettings { get; private set; } = new Dictionary<string, BaseSetting>();
        public static Dictionary<string, SettingDescriptor> Descriptors { get; private set; } = new Dictionary<string, SettingDescriptor>();

        //GENERAL
        public EnumSetting<AccessibilityMethod> AccessibilityMethod { get; } = new EnumSetting<AccessibilityMethod>(SettingKeys.ACCESSIBILY_METHOD, AppData.AccessibilityMethod.Mouse);
        public EnumSetting<Language> Language { get; } = new EnumSetting<Language>(SettingKeys.LANGUAGE, AppData.Language.ESP);
        public IntSetting Volume { get; } = new IntSetting(SettingKeys.VOLUME, 100, new Int2(0, 100));

        //VOICE
        public EnumSetting<Voices> Voice { get; } = new EnumSetting<Voices>(SettingKeys.VOICE, Voices.Hombre);
        public IntSetting VoiceVolume { get; } = new IntSetting(SettingKeys.VOICE_VOLUME, 100, new Int2(0, 100));
        public IntSetting VoiceSpeed { get; } = new IntSetting(SettingKeys.VOICE_SPEED, 0, new Int2(-10, 10));
        public IntSetting VoiceDeviceIndex { get; } = new IntSetting(SettingKeys.VOICE_DEVICE, 0);

        //TRACKING
        public IntSetting SampleRateHZ { get; } = new IntSetting(SettingKeys.SAMPLE_RATE_HZ, 60, new Int2(30, 120)); 
        public BoolSetting MouseToggle { get; } = new BoolSetting(SettingKeys.MOUSE_TOGGLE, false);

        //CLICKS
        public BoolSetting ClickFeedbackToggle { get; } = new BoolSetting(SettingKeys.CLICK_FEEDBACK_TOGGLE, true);
        public BoolSetting ClickZoomToggle { get; } = new BoolSetting(SettingKeys.CLICK_ZOOM_TOGGLE, true);
        public IntSetting ClickZoomLevel { get; } = new IntSetting(SettingKeys.CLICK_ZOOM_LEVEL, 2, new Int2(2,6));

        //BUBBLE CURSOR
        public BoolSetting BubbleToggle { get; } = new BoolSetting(SettingKeys.BUBBLE_TOGGLE, false);
        public IntSetting BubbleColor { get; } = new IntSetting(SettingKeys.BUBBLE_COLOR_SELECTION, 0);
        public FloatSetting BubbleOpacity { get; } = new FloatSetting(SettingKeys.BUBBLE_OPACITY, 0.6f, new Float2(0f, 1f));
        public IntSetting BubbleSize { get; } = new IntSetting(SettingKeys.BUBBLE_SIZE, 40, new Int2(1, 200));
        public EnumSetting<BubbleOpacityType> BubbleOpacityEnum { get; } = new EnumSetting<BubbleOpacityType>(SettingKeys.BUBBLE_OPACITY_ENUM, BubbleOpacityType.Regular);
        public EnumSetting<CursorVisualStyle> CursorStyle { get; } = new EnumSetting<CursorVisualStyle>(SettingKeys.CURSOR_STYLE, CursorVisualStyle.Bubble);
        public Setting<string> CustomCursorPath { get; } = new Setting<string>(SettingKeys.CURSOR_CUSTOM_PATH, string.Empty);
        public BoolSetting ShowRawGazeAsPoint { get; } = new BoolSetting(SettingKeys.SHOW_RAW_GAZE, false);

        //FILTERS
        public FilterProfileSetting FilterProfile { get; } = new FilterProfileSetting(SettingKeys.FILTER_PROFILE);
        public IntSetting SmoothFilter { get; } = new IntSetting(SettingKeys.JOACO_SMOOTH_FILTER, 10, new Int2(1, 10));
        public IntSetting KalmanFilter { get; } = new IntSetting(SettingKeys.KALMAN_FILTER, 20, new Int2(0, 100));
        public FloatSetting InterpolationFilter { get; } = new FloatSetting(SettingKeys.INTERPOLATION_FILTER, 0.05f, new Float2(0, 100));

        //CALIBRATION 
        public LastCalibrationSetting LastCalibrationBuff { get; } = new LastCalibrationSetting(SettingKeys.LAST_CALIBRATION_BUFF);                                 //byte[] Calibration.buff
        public IntSetting LastEyesOption { get; } = new IntSetting(SettingKeys.LAST_CALIBRATION_EYES_OPTION, 0);         //Index de la opción
        public IntSetting LastPointsOption { get; } = new IntSetting(SettingKeys.LAST_CALIBRATION_POINTS_OPTION, 0);     //Index de la opción
        public BoolSetting SaveCalibrationAsPresetToggle { get; } = new BoolSetting(SettingKeys.SAVE_CALIBRATION_AS_PRESET, false);
        public EnumSetting<CalibrationPointStyle> CalibrationPointStyle { get;} = new EnumSetting<CalibrationPointStyle>(SettingKeys.CALIBRATION_POINT_STYLE, AppData.CalibrationPointStyle.Point);
        public Setting<string> CalibrationPointCustomImagePath { get; } = new Setting<string>(SettingKeys.CALIBRATION_POINT_CUSTOM_IMAGE, string.Empty);

        //GAZE BUTTON ACTIVATION
        public FloatSetting TiempoDeInicioDeActivacion { get; } = new FloatSetting(SettingKeys.TIEMPO_INICIO_ACTIVACION, .4f, new Float2(0f, 5f));
        public FloatSetting TiempoDeActivacion { get; } = new FloatSetting(SettingKeys.TIEMPO_ACTIVACION, 1.5f, new Float2(0f, 5f));
        public FloatSetting TiempoDeActivacionDeTeclado { get; } = new FloatSetting(SettingKeys.TIEMPO_ACTIVACION_TECLADO, 1.5f, new Float2(0f, 5f));
        public FloatSetting MultiplicadorDeVelocidadDeDesactivacion { get; } = new FloatSetting(SettingKeys.MULTIPLICADOR_VELOCIDAD_DESACTIVACION, 1.5f, new Float2(0f, 5f));
        public FloatSetting PermanenciaDeFijacionesIncompletas { get; } = new FloatSetting(SettingKeys.TIEMPO_PERMANENCIA, .5f, new Float2(0f, 5f));
        public BoolSetting MostrarPermanenciaDeFijacionesIncompletas { get; } = new BoolSetting(SettingKeys.MOSTRAR_PERMANENCIA, true);

        //INTERACCIÓN
        public EnumSetting<ColorScheme> ColorScheme { get; } = new EnumSetting<ColorScheme>(SettingKeys.COLOR_SCHEME, AppData.ColorScheme.System);
        public EnumSetting<SizeModifier> UserInterfaceSize { get; } = new EnumSetting<SizeModifier>(SettingKeys.USER_INTERFACE_SIZE, SizeModifier.x100);
        public EnumSetting<InteractionBarPosition> BarPosition { get; } = new EnumSetting<InteractionBarPosition>(SettingKeys.INTERACTION_BAR_POSITION, AppData.InteractionBarPosition.Right);

        //TECLADO
        public EnumSetting<SizeModifier> KeyboardSize { get; } = new EnumSetting<SizeModifier>(SettingKeys.KEYBOARD_SIZE, SizeModifier.x100);
        public EnumSetting<Size> KeyboardFontSize { get; } = new EnumSetting<Size>(SettingKeys.KEYBOARD_FONT_SIZE, Size.Medium);
        public EnumSetting<KeyboardType> KeyboardType { get; } = new EnumSetting<KeyboardType>(SettingKeys.KEYBOARD_TYPE, AppData.KeyboardType.QWERTY);
        public BoolSetting WordPrediction { get; } = new BoolSetting(SettingKeys.WORD_PREDICTION, true);
        public BoolSetting WordPredictionLearning { get; } = new BoolSetting(SettingKeys.WORD_PREDICTION_LEARNING, true);

        //PEDIDO DE AYUDA
        public BoolSetting AutoHelpCall { get; } = new BoolSetting(SettingKeys.AUTO_HELP_CALL, false);
        public IntSetting AutoHelpCallTimeInMinutes { get; } = new IntSetting(SettingKeys.AUTO_HELP_CALL_TIME, 3);
        public EnumSetting<HelpCallMode> HelpCallAction { get; } = new EnumSetting<HelpCallMode>(SettingKeys.HELP_CALL_ACTION, HelpCallMode.DefaultSound);
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

        //TODO: Customs. Los customs serían sonidos o gráficos personalizables. 
        //Podría reemplazarse un único archivo en la carpeta del usuario usando una ventana de seleccion de path para copiar y pegar la selección en una carpeta local.
        //Ejemplos: CustomCalibrationPoint, CustomPointer, CustomHelpCall

        private void HookProfileSettings()
        {
            //TODO: Los profiles son una clase especial de Setting que responde a cambios en otros settings. Si escalan hacer una clase aparte. 
            FilterProfile.HookProfileSettings();
        }

        private void RegisterSettings()
        {
            RegisterSetting(AccessibilityMethod);
            RegisterSetting(Language);
            RegisterSetting(Volume);

            RegisterSetting(Voice);
            RegisterSetting(VoiceVolume);
            RegisterSetting(VoiceSpeed);
            RegisterSetting(VoiceDeviceIndex);

            RegisterSetting(SampleRateHZ);
            RegisterSetting(MouseToggle);

            RegisterSetting(ClickFeedbackToggle);
            RegisterSetting(ClickZoomToggle);
            RegisterSetting(ClickZoomLevel);

            RegisterSetting(BubbleToggle);
            RegisterSetting(BubbleColor);
            RegisterSetting(BubbleOpacity);
            RegisterSetting(BubbleSize);
            RegisterSetting(CursorStyle);
            RegisterSetting(CustomCursorPath);
            RegisterSetting(ShowRawGazeAsPoint);

            RegisterSetting(FilterProfile);
            RegisterSetting(SmoothFilter);
            RegisterSetting(KalmanFilter);
            RegisterSetting(InterpolationFilter);

            RegisterSetting(LastCalibrationBuff);
            RegisterSetting(LastEyesOption);
            RegisterSetting(LastPointsOption);
            RegisterSetting(SaveCalibrationAsPresetToggle);
            RegisterSetting(CalibrationPointStyle);
            RegisterSetting(CalibrationPointCustomImagePath);

            RegisterSetting(TiempoDeInicioDeActivacion);
            RegisterSetting(TiempoDeActivacion);
            RegisterSetting(TiempoDeActivacionDeTeclado);
            RegisterSetting(MultiplicadorDeVelocidadDeDesactivacion);
            RegisterSetting(PermanenciaDeFijacionesIncompletas);
            RegisterSetting(MostrarPermanenciaDeFijacionesIncompletas);

            RegisterSetting(ColorScheme);
            RegisterSetting(UserInterfaceSize);
            RegisterSetting(BarPosition);

            RegisterSetting(KeyboardSize);
            RegisterSetting(KeyboardFontSize);
            RegisterSetting(KeyboardType);
            RegisterSetting(WordPrediction);
            RegisterSetting(WordPredictionLearning);

            RegisterSetting(AutoHelpCall);
            RegisterSetting(AutoHelpCallTimeInMinutes);
            RegisterSetting(HelpCallAction);
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
        public abstract object GetValue();
        public abstract void LoadSetting();
        public abstract SettingDescriptor Descriptor { get; }
    }

    public class EnumSetting<T> : BaseSetting where T : Enum
    {
        public event Action OnChanged;
        public event Action<T> OnValueChanged;
        public override SettingDescriptor Descriptor => descriptor;
        SettingDescriptor descriptor;

        T valueCache;
        public T Value
        {
            get { return valueCache; }
            set
            {
                if (EqualityComparer<T>.Default.Equals(valueCache, value)) return;
                valueCache = value;
                SaveSetting();
                OnChanged?.Invoke();
                OnValueChanged?.Invoke(valueCache);
                GlobalEvents.OnSettingChanged.Invoke(Descriptor.saveKey);
            }
        }
        public EnumSetting(string saveKey, T defaultValue)
        {
            descriptor = new SettingDescriptor(saveKey, typeof(T), defaultValue);
        }

        public override void SetValue(object value)
        {
            if (value == null) return;
            string jsonValue = value.ToString();
            Debug.WriteLine($"Setting Enum JsonValue: {jsonValue}");
            if (Enum.TryParse(typeof(T), jsonValue, true, out var parsed) && Enum.IsDefined(typeof(T), parsed))
            {
                Value = (T)parsed;
                Debug.WriteLine($"Setting Enum JsonValue: {Value}");

            }
            else
            {
                Debug.WriteLine($"Invalid value type. Expected {typeof(T)}, got {value?.GetType()}");
            }
        }
        public override object GetValue()
        {
            return Value.ToString();
        }
        public override void LoadSetting()
        {
            Settings.RegisterDescriptor(Descriptor);
            valueCache = SaveManager.GetSystemSetting<T>(Descriptor.saveKey, (T)Descriptor.defaultValue);
            OnValueChanged?.Invoke(valueCache);
        }

        void SaveSetting()
        {
            SaveManager.SetSystemSetting<T>(Descriptor.saveKey, Value);
            SaveManager.SaveSystemSettings();
        }

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
                if (EqualityComparer<T>.Default.Equals(valueCache, value)) return;
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

        public override object GetValue()
        {
            return Value;
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
    public class StringSetting : BaseSetting<string>
    {
        SettingDescriptor descriptor;
        public override SettingDescriptor Descriptor => descriptor;
        public StringSetting(string saveKey, string defaultValue)
        {
            this.descriptor = new SettingDescriptor(saveKey, typeof(string), defaultValue);

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
