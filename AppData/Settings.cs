using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GazeStream.Utilities.Save;
using System.Diagnostics;
using GazeStream.Utilities.Events;
using GazeStream.AppData;
using System.ComponentModel;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace GazeStream.AppData
{
    public class SettingKeys
    {
        public const string FONT_SIZE_TITLES = "FontSizeTitles";
        public const string FONT_SIZE_TITLES_OPTION = "FontSizeTitlesOption";

        public const string ACCESSIBILY_METHOD = "MetodoDeAccesibilidad";
        public const string LANGUAGE = "Language";
        public const string VOLUME = "Volume";
        public const string VOLUME_OPTION = "VolumeOption";

        public const string VOICE = "Voice";
        public const string VOICE_SPEED = "VoiceSpeed";
        public const string VOICE_VOLUME = "VoiceVolume";
        public const string VOICE_VOLUME_OPTION = "VoiceVolumeOption";
        public const string VOICE_DEVICE = "VoiceDeviceIndex";
        public const string VOICE_FEEDBACK = "VoiceFeedback";

        public const string CLICK_FEEDBACK_TOGGLE = "ClickFeedbackToggle";
        public const string CLICK_ZOOM_TOGGLE = "ClickZoomToggle";
        public const string CLICK_ZOOM_LEVEL = "ClickZoomLevel";

        public const string SAMPLE_RATE_HZ = "SampleRateHertz";
        public const string SAMPLE_RATE_OPTION = "SampleRateOption";
        public const string REST_MODE_TOGGLE = "RestModeToggle";
        public const string MOUSE_TOGGLE = "MouseToggle";
        public const string BUBBLE_TOGGLE = "BubbleToggle";
        public const string BUBBLE_COLOR_OPTION = "BubbleColorSelection";
        public const string BUBBLE_OPACITY = "BubbleOpacity";
        public const string BUBBLE_SIZE = "BubbleSizeDouble";
        public const string CURSOR_STYLE = "CursorStyle";
        public const string CURSOR_CUSTOM_PATH = "CursorCustomPath";
        public const string SHOW_RAW_GAZE = "ShowRawGazeAsDot";
        public const string BUBBLE_OPACITY_OPTION = "BubbleOpacityOption";
        public const string BUBBLE_SIZE_OPTION = "BubbleSizeOption";

        public const string FILTER_PROFILE = "FilterProfile";
        public const string JOACO_SMOOTH_FILTER = "InvensunSmoothValue";
        public const string KALMAN_FILTER = "KalmanFilter";
        public const string INTERPOLATION_FILTER = "InterpolationFilter";

        public const string LAST_CALIBRATION = "LastCalibration";
        public const string LAST_CALIBRATION_EYES_OPTION = "LastCalibrationEyesOption";
        public const string LAST_CALIBRATION_POINTS_OPTION = "LastCalibrationPointsOption";
        public const string SAVE_CALIBRATION_AS_PRESET = "SaveCalibrationAsPresetToggle";
        public const string CALIBRATION_POINT_STYLE = "CalibrationPointStyle";
        public const string CALIBRATION_POINT_CUSTOM_IMAGE = "CalibrationPointCustomImagePath";


        //BOTONES (Algunos están en castellano para imitar los nombres del SAI.)
        public const string TIEMPO_INICIO_ACTIVACION = "TiempoDeInicioDeActivacion";
        public const string TIEMPO_ACTIVACION = "TiempoDeActivacion";
        public const string TIEMPO_ACTIVACION_TECLADO = "TiempoDeActivacionDeTeclado";
        public const string MULTIPLICADOR_VELOCIDAD_DESACTIVACION = "MultiplicadorDeVelocidadDeDesactivacion";
        public const string TIEMPO_PERMANENCIA = "TiempoDePermanenciaDeActivacionIncompleta";
        public const string REPEAT_ACTION_INTERVAL = "RepeatActionInterval";
        public const string MOSTRAR_PERMANENCIA = "MostrarPermanenciaDeActivacionIncompleta";
        public const string BUTTON_ANIMATION_TYPE = "ButtonAnimationType";
        public const string BUTTON_ANIMATION_COLOR = "ButtonAnimationColor";
        public const string FOCUS_TIME_OPTION = "OpcionTiempoDeActivacion";
        public const string START_FOCUS_TIME_OPTION = "OpcionTiempoDeInicioDeActivacion";
        public const string INCOMPLETE_FOCUS_TIME_OPTION = "OpcionTiempoDePermanenciaDeActivacionIncompleta";
        public const string KEYBOARD_FOCUS_TIME_OPTION = "OpcionTiempoDeActivacionDeTeclado";
        public const string REPEAT_ACTION_INTERVAL_OPTION = "RepeatActionIntervalOption";
        public const string DECAY_MULTIPLIER_OPTION = "OpcionMultiplicadorDeVelocidadDeDesactivacion";

        public const string COLOR_SCHEME = "ColorScheme";
        public const string USER_INTERFACE_SIZE = "UserInterfaceSize";
        public const string INTERACTION_BAR_POSITION = "InteractionBarPosition";

        public const string KEYBOARD_SIZE = "KeyboardSize";
        public const string KEYBOARD_FONT_SIZE = "KeyboardFontSize";
        public const string KEYBOARD_TYPE = "KeyboardType";
        public const string KEYBOARD_VOICE_FEEDBACK = "KeyboardVoiceFeedback";
        public const string WORD_PREDICTION = "WordPrediction";
        public const string WORD_PREDICTION_LEARNING = "WordPredictionLearning";

        public const string BARRIDO_VOICE_FEEDBACK = "BarridoVoiceFeedback";
        public const string BARRIDO_DELAY = "BarridoTiempoDeEspera";
        public const string BARRIDO_DELAY_OPTION = "BarridoTiempoDeEsperaOption";
        public const string BARRIDO_EYETRACKER = "BarridoEyetracker";

        public const string AUTO_HELP_CALL = "AutomaticHelpCall";
        public const string AUTO_HELP_CALL_TIME = "AutomaticHelpCallTime";
        public const string AUTO_HELP_CALL_TIME_OPTION = "AutomaticHelpCallTimeOption";
        public const string HELP_CALL_ACTION = "HelpCallAction";

    }

    public enum BarridoEyetrackerMode
    {
        [Description("<- / ->")]
        LeftRight,
        [Description("Esquinas")]
        Esquinas,
        [Description("Cerrar Ojos")]
        CerrarOjos,
        [Description("Combinado")]
        Combinado
    }
    public enum Button_Animation 
    {
        [Description("Circulo")]
        Clock,
        [Description("Contraer")]
        Shrink,
        [Description("Expandir")]
        Expand,
        [Description("Llenar")]
        FillLeft,
        [Description("Marco")]
        Frame
    }
    public enum BasicColor 
    {
        [ColorBrush("Rojo")]
        Rojo,
        [ColorBrush("Verde")]
        Verde,
        [ColorBrush("Azul")]
        Azul,
        [ColorBrush("Amarillo")]
        Amarillo,
        [ColorBrush("Cyan")]
        Cyan,
        [ColorBrush("Magenta")]
        Magenta,
        [ColorBrush("Violeta")]
        Violeta
    }
    public enum FilterProfile 
    {
        Bajo,
        Medio,
        Alto,
        Custom
    }
    public enum AccessibilityMethod 
    {
        [Icon("pack://application:,,,/Resources/Icons/mouse.png")]
        Mouse,
        [Icon("pack://application:,,,/Resources/Icons/calib.png")]
        EyeTracker,
        [Icon("pack://application:,,,/Resources/Icons/crosshair.png")]
        Barrido,
        [Icon("pack://application:,,,/Resources/Icons/hand-pointing.png")]
        Touch
    }
    public enum Voices 
    {
        [Icon("pack://application:,,,/Resources/Icons/gender-male.png")]
        Hombre,
        [Icon("pack://application:,,,/Resources/Icons/gender-female.png")]
        Mujer,
        [Icon("pack://application:,,,/Resources/Icons/gender-neuter.png")]
        Personalizada
    }
    public enum CalibrationPointStyle { Point, Image, Animation }
    public enum ColorScheme 
    {
        [Icon("pack://application:,,,/Resources/Icons/sun.png")]
        Regular,
        [Icon("pack://application:,,,/Resources/Icons/moon.png")]
        Oscuro,
        [Icon("pack://application:,,,/Resources/Icons/moon-stars.png")]
        Contraste
    }
    public enum InteractionBarPosition { Left, Right, Top, Bottom }
    public enum Size 
    {
        [Description("Muy Chico")]
        VerySmall,
        [Description("Chico")]
        Small,
        [Description("Medio")]
        Medium,
        [Description("Grande")]
        Large,
        [Description("Muy Grande")]
        VeryLarge
    }
    public enum SizeModifier { x50, x75, x100 }
    public enum KeyboardType 
    {
        [Icon("pack://application:,,,/Resources/Icons/keyboard.png")]
        QWERTY,
        [Icon("pack://application:,,,/Resources/Icons/keyboard.png")]
        ABC,
        [Icon("pack://application:,,,/Resources/Icons/keyboard.png")]
        PICTO
    }
    public enum HelpCallMode 
    {
        [Icon("pack://application:,,,/Resources/Icons/chat-circle-text.png")]
        [Description("Sonido por defecto")]
        DefaultSound,
        [Icon("pack://application:,,,/Resources/Icons/chat-circle-dots.png")]
        [Description("Sonido personalizado")]
        CustomSound,
        [Icon("pack://application:,,,/Resources/Icons/options.png")]
        [Description("Ejecutar archivo")]
        ExecuteFile
    }
    public enum HelpCallTime
    {
        [Description("3 min")]
        Three,
        [Description("5 min")]
        Five,
        [Description("10min")]
        Ten,
        [Description("15min")]
        Fifteen,
    }
    public enum Language { ESP, POR, ENG }
    public enum Volume 
    {
        [Icon("pack://application:,,,/Resources/Icons/mute.png")]
        [Description("Mute")]
        Mute,
        [Icon("pack://application:,,,/Resources/Icons/sound0.png")]
        [Description("25%")]
        x25,
        [Icon("pack://application:,,,/Resources/Icons/sound1.png")]
        [Description("50%")]
        x50,
        [Icon("pack://application:,,,/Resources/Icons/sound2.png")]
        [Description("75%")]
        x75,
        [Icon("pack://application:,,,/Resources/Icons/sound3.png")]
        [Description("100%")]
        x100
    }

    public enum FocusTimeOption
    {
        [Description("0.5 s")]
        HalfSecond,
        [Description("1 s")]
        OneSecond,
        [Description("1.5 s")]
        OneAndAHalfSecond,
        [Description("2 s")]
        TwoSeconds,
        [Description("3 s")]
        ThreeSeconds,
        [Description("5 s")]
        FiveSeconds
    }

    public enum FocusStartOption
    {
        [Description("0 s")]
        Low,
        [Description("0.2 s")]
        Medium,
        [Description("0.5 s")]
        High
    }

    public enum DecayMultiplier
    {
        x1,
        x2,
        x3
    }

    public enum OpacityOption 
    {
        [Description("Baja")]
        Low,
        [Description("Media")]
        Medium,
        [Description("Alta")]
        High,
        [Description("Ultra")]
        Ultra
    }

    public enum CursorVisualStyle
    {
        [Icon("pack://application:,,,/Resources/Icons/circle-fill.png")]
        [Description("Burbuja")]
        Bubble,
        [Icon("pack://application:,,,/Resources/Icons/circle.png")]
        [Description("Círculo")]
        Outline,
        [Icon("pack://application:,,,/Resources/Icons/image.png")]
        [Description("Imagen Custom")]
        CustomImage
    }

    public enum FontSize
    {
        [Description("Chica")]
        Small,
        [Description("Mediana")]
        Medium,
        [Description("Grande")]
        Large
    }

    public enum SampleRate
    {
        [Description("20 FPS")]
        x20,
        [Description("30 FPS")]
        x30,
        [Description("60 FPS")]
        x60
    }
    public class Settings
    {
        public static Settings I { get; private set; }
        public static Dictionary<string, BaseSetting> BaseSettings { get; private set; } = new Dictionary<string, BaseSetting>();
        public static Dictionary<string, SettingDescriptor> Descriptors { get; private set; } = new Dictionary<string, SettingDescriptor>();

        //STYLE PREFERENCES

        public DoubleSetting FontSizeTitles { get; } = new DoubleSetting(SettingKeys.FONT_SIZE_TITLES, 24, new Double2(20, 60));
        public FontSizeTitlesOptionSetting FontSizeTitlesOption { get; } = new FontSizeTitlesOptionSetting(SettingKeys.FONT_SIZE_TITLES_OPTION, FontSize.Medium);

        //GENERAL
        public EnumSetting<AccessibilityMethod> AccessibilityMethod { get; } = new EnumSetting<AccessibilityMethod>(SettingKeys.ACCESSIBILY_METHOD, AppData.AccessibilityMethod.Mouse);
        public EnumSetting<Language> Language { get; } = new EnumSetting<Language>(SettingKeys.LANGUAGE, AppData.Language.ESP);
        public IntSetting Volume { get; } = new IntSetting(SettingKeys.VOLUME, 100, new Int2(0, 100));
        public EnumSetting<Volume> VolumeOption { get; } = new EnumSetting<Volume>(SettingKeys.VOLUME_OPTION, AppData.Volume.x100);

        //VOICE
        public EnumSetting<Voices> Voice { get; } = new EnumSetting<Voices>(SettingKeys.VOICE, Voices.Hombre);
        public IntSetting VoiceVolume { get; } = new IntSetting(SettingKeys.VOICE_VOLUME, 100, new Int2(0, 100));
        public EnumSetting<Volume> VoiceVolumeOption { get; } = new EnumSetting<Volume>(SettingKeys.VOICE_VOLUME_OPTION, AppData.Volume.x100);
        public IntSetting VoiceSpeed { get; } = new IntSetting(SettingKeys.VOICE_SPEED, 0, new Int2(-10, 10));
        public IntSetting VoiceDeviceIndex { get; } = new IntSetting(SettingKeys.VOICE_DEVICE, 0);
        public BoolSetting VoiceFeedbackToggle { get; } = new BoolSetting(SettingKeys.VOICE_FEEDBACK, true);

        //TRACKING
        public IntSetting SampleRateHZ { get; } = new IntSetting(SettingKeys.SAMPLE_RATE_HZ, 60, new Int2(30, 120));
        public SampleRateOption SampleRateOption { get; } = new SampleRateOption(SettingKeys.SAMPLE_RATE_OPTION, SampleRate.x60);
        public BoolSetting RestModeToggle { get; } = new BoolSetting(SettingKeys.REST_MODE_TOGGLE, false);
        public BoolSetting MouseToggle { get; } = new BoolSetting(SettingKeys.MOUSE_TOGGLE, false);

        //CLICKS
        public BoolSetting ClickFeedbackToggle { get; } = new BoolSetting(SettingKeys.CLICK_FEEDBACK_TOGGLE, true);
        public BoolSetting ClickZoomToggle { get; } = new BoolSetting(SettingKeys.CLICK_ZOOM_TOGGLE, true);
        public IntSetting ClickZoomLevel { get; } = new IntSetting(SettingKeys.CLICK_ZOOM_LEVEL, 2, new Int2(2, 6));

        //BUBBLE CURSOR
        public BoolSetting BubbleToggle { get; } = new BoolSetting(SettingKeys.BUBBLE_TOGGLE, false);
        public EnumSetting<BasicColor> BubbleColor { get; } = new EnumSetting<BasicColor>(SettingKeys.BUBBLE_COLOR_OPTION, BasicColor.Azul);
        public DoubleSetting BubbleOpacity { get; } = new DoubleSetting(SettingKeys.BUBBLE_OPACITY, 0.6f, new Double2(0d, 1d));
        public BubbleOpacityOptionSetting BubbleOpacityOption { get; } = new BubbleOpacityOptionSetting(SettingKeys.BUBBLE_OPACITY_OPTION, OpacityOption.Medium);
        public DoubleSetting BubbleSize { get; } = new DoubleSetting(SettingKeys.BUBBLE_SIZE, 40, new Double2(1, 200));
        public BubbleSizeOptionSetting BubbleSizeOption { get; } = new BubbleSizeOptionSetting(SettingKeys.BUBBLE_SIZE_OPTION, Size.Small);
        public EnumSetting<OpacityOption> BubbleOpacityEnum { get; } = new EnumSetting<OpacityOption>(SettingKeys.BUBBLE_OPACITY_OPTION, OpacityOption.Medium);
        public EnumSetting<CursorVisualStyle> CursorStyle { get; } = new EnumSetting<CursorVisualStyle>(SettingKeys.CURSOR_STYLE, CursorVisualStyle.Bubble);
        public Setting<string> CustomCursorPath { get; } = new Setting<string>(SettingKeys.CURSOR_CUSTOM_PATH, string.Empty);
        public BoolSetting ShowRawGazeAsPoint { get; } = new BoolSetting(SettingKeys.SHOW_RAW_GAZE, false);

        //FILTERS
        public FilterProfileSetting FilterProfile { get; } = new FilterProfileSetting(SettingKeys.FILTER_PROFILE);
        public IntSetting SmoothFilter { get; } = new IntSetting(SettingKeys.JOACO_SMOOTH_FILTER, 10, new Int2(1, 10));
        public IntSetting KalmanFilter { get; } = new IntSetting(SettingKeys.KALMAN_FILTER, 20, new Int2(0, 100));
        public FloatSetting InterpolationFilter { get; } = new FloatSetting(SettingKeys.INTERPOLATION_FILTER, 0.05f, new Float2(0, 100));

        //CALIBRATION 
        public LastCalibration LastCalibrationBuff { get; } = new LastCalibration(SettingKeys.LAST_CALIBRATION, new byte[0]);                                 //byte[] Calibration.buff
        public IntSetting LastEyesOption { get; } = new IntSetting(SettingKeys.LAST_CALIBRATION_EYES_OPTION, 0);         //Index de la opción
        public IntSetting LastPointsOption { get; } = new IntSetting(SettingKeys.LAST_CALIBRATION_POINTS_OPTION, 0);     //Index de la opción
        public BoolSetting SaveCalibrationAsPresetToggle { get; } = new BoolSetting(SettingKeys.SAVE_CALIBRATION_AS_PRESET, false);
        public EnumSetting<CalibrationPointStyle> CalibrationPointStyle { get; } = new EnumSetting<CalibrationPointStyle>(SettingKeys.CALIBRATION_POINT_STYLE, AppData.CalibrationPointStyle.Point);
        public Setting<string> CalibrationPointCustomImagePath { get; } = new Setting<string>(SettingKeys.CALIBRATION_POINT_CUSTOM_IMAGE, string.Empty);

        //GAZE BUTTON ACTIVATION
        public FloatSetting TiempoDeInicioDeActivacion { get; } = new FloatSetting(SettingKeys.TIEMPO_INICIO_ACTIVACION, .2f, new Float2(0f, 5f));
        public FloatSetting TiempoDeActivacion { get; } = new FloatSetting(SettingKeys.TIEMPO_ACTIVACION, 1.5f, new Float2(0f, 5f));
        public FloatSetting TiempoDeActivacionDeTeclado { get; } = new FloatSetting(SettingKeys.TIEMPO_ACTIVACION_TECLADO, 1.5f, new Float2(0f, 5f));
        public FloatSetting MultiplicadorDeVelocidadDeDesactivacion { get; } = new FloatSetting(SettingKeys.MULTIPLICADOR_VELOCIDAD_DESACTIVACION, 1.5f, new Float2(.5f, 5f));
        public FloatSetting PermanenciaDeFijacionesIncompletas { get; } = new FloatSetting(SettingKeys.TIEMPO_PERMANENCIA, .3f, new Float2(0f, 5f));
        public FloatSetting RepeatActionInterval { get; } = new FloatSetting(SettingKeys.REPEAT_ACTION_INTERVAL, 1f, new Float2(0f, 5f));
        public BoolSetting MostrarPermanenciaDeFijacionesIncompletas { get; } = new BoolSetting(SettingKeys.MOSTRAR_PERMANENCIA, true);
        public EnumSetting<BasicColor> ButtonAnimationColor { get; } = new EnumSetting<BasicColor>(SettingKeys.BUTTON_ANIMATION_COLOR, BasicColor.Azul);
        public EnumSetting<Button_Animation> ButtonAnimationType { get; } = new EnumSetting<Button_Animation>(SettingKeys.BUTTON_ANIMATION_TYPE, Button_Animation.Clock);

        public FocusTimeOptionSetting OpcionTiempoDeActivacion { get; } = new FocusTimeOptionSetting(SettingKeys.FOCUS_TIME_OPTION, FocusTimeOption.OneAndAHalfSecond);
        public StartFocusTimeOptionSetting OpcionTiempoDeInicioDeActivacion { get; } = new StartFocusTimeOptionSetting(SettingKeys.START_FOCUS_TIME_OPTION, FocusStartOption.Medium);
        public IncompleteFocusOptionSetting OpcionPermanenciaDeFijacionesIncompletas { get; } = new IncompleteFocusOptionSetting(SettingKeys.INCOMPLETE_FOCUS_TIME_OPTION, FocusStartOption.Medium);
        public KeyboardFocusTimeOptionSetting OpcionTiempoDeActivacionDeTeclado { get; } = new KeyboardFocusTimeOptionSetting(SettingKeys.KEYBOARD_FOCUS_TIME_OPTION, FocusTimeOption.OneAndAHalfSecond);
        public RepeatActionIntervalOptionSetting RepeatActionIntervalOption { get; } = new RepeatActionIntervalOptionSetting(SettingKeys.REPEAT_ACTION_INTERVAL_OPTION, FocusTimeOption.OneAndAHalfSecond);
        public DecayMultiplierOptionSetting OpcionMultiplicadorDeVelocidadDeDesactivacion { get; } = new DecayMultiplierOptionSetting(SettingKeys.DECAY_MULTIPLIER_OPTION, DecayMultiplier.x1);


        //INTERACCIÓN
        public EnumSetting<ColorScheme> ColorScheme { get; } = new EnumSetting<ColorScheme>(SettingKeys.COLOR_SCHEME, AppData.ColorScheme.Regular);
        public EnumSetting<SizeModifier> UserInterfaceSize { get; } = new EnumSetting<SizeModifier>(SettingKeys.USER_INTERFACE_SIZE, SizeModifier.x100);
        public EnumSetting<InteractionBarPosition> BarPosition { get; } = new EnumSetting<InteractionBarPosition>(SettingKeys.INTERACTION_BAR_POSITION, AppData.InteractionBarPosition.Right);

        //TECLADO
        public EnumSetting<SizeModifier> KeyboardSize { get; } = new EnumSetting<SizeModifier>(SettingKeys.KEYBOARD_SIZE, SizeModifier.x100);
        public EnumSetting<Size> KeyboardFontSize { get; } = new EnumSetting<Size>(SettingKeys.KEYBOARD_FONT_SIZE, Size.Medium);
        public EnumSetting<KeyboardType> KeyboardType { get; } = new EnumSetting<KeyboardType>(SettingKeys.KEYBOARD_TYPE, AppData.KeyboardType.QWERTY);
        public BoolSetting WordPrediction { get; } = new BoolSetting(SettingKeys.WORD_PREDICTION, true);
        public BoolSetting WordPredictionLearning { get; } = new BoolSetting(SettingKeys.WORD_PREDICTION_LEARNING, true);
        public BoolSetting KeyboardVoiceFeedback { get; } = new BoolSetting(SettingKeys.KEYBOARD_VOICE_FEEDBACK, true);

        //BARRIDO
        public BoolSetting BarridoVoiceFeedback { get; } = new BoolSetting(SettingKeys.BARRIDO_VOICE_FEEDBACK, true);
        public EnumSetting<BarridoEyetrackerMode> BarridoEyetrackerMode { get; } = new EnumSetting<BarridoEyetrackerMode>(SettingKeys.BARRIDO_EYETRACKER, AppData.BarridoEyetrackerMode.CerrarOjos);
        public FloatSetting BarridoTiempoDeEspera { get; } = new FloatSetting(SettingKeys.BARRIDO_DELAY, .05f, new Float2(.5f, 5));
        public BarridoTiempoDeEsperaOption BarridoTiempoDeEsperaOption { get; } = new BarridoTiempoDeEsperaOption(SettingKeys.BARRIDO_DELAY_OPTION, FocusTimeOption.OneAndAHalfSecond);

        //PEDIDO DE AYUDA
        public BoolSetting AutoHelpCall { get; } = new BoolSetting(SettingKeys.AUTO_HELP_CALL, false);
        public IntSetting AutoHelpCallTimeInMinutes { get; } = new IntSetting(SettingKeys.AUTO_HELP_CALL_TIME, 3);
        public EnumSetting<HelpCallMode> HelpCallAction { get; } = new EnumSetting<HelpCallMode>(SettingKeys.HELP_CALL_ACTION, HelpCallMode.DefaultSound);
        public HelpCallTimeOption HelpCallTimeOption { get; } = new HelpCallTimeOption(SettingKeys.AUTO_HELP_CALL_TIME_OPTION, HelpCallTime.Five);
        

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
            //CreateDocumentationFile(); //TODO: Elegir un directorio para generarlo automáticamente.
        }

        //TODO: Customs. Los customs serían sonidos o gráficos personalizables. 
        //Podría reemplazarse un único archivo en la carpeta del usuario usando una ventana de seleccion de path para copiar y pegar la selección en una carpeta local.
        //Ejemplos: CustomCalibrationPoint, CustomPointer, CustomHelpCall

        void CreateDocumentationFile()
        {
            var builder = new StringBuilder();
            foreach (BaseSetting setting in BaseSettings.Values)
            {
                builder.Append(setting.GetDocumentationReference());
                builder.AppendLine();
            }
            string content = builder.ToString();
            FileOps.SaveToText(AppPaths.BasePath + "/documentation.txt", content);
            FileOps.OpenDirectory(AppPaths.BasePath);
        }
        private void HookProfileSettings()
        {
            //TODO: Los profiles son una clase especial de Setting que responde a cambios en otros settings. Si escalan hacer una clase aparte. 
            FilterProfile.HookProfileSettings();
        }

        private void RegisterSettings()
        {
            RegisterSetting(FontSizeTitles);
            RegisterSetting(FontSizeTitlesOption);

            RegisterSetting(AccessibilityMethod);
            RegisterSetting(Language);
            RegisterSetting(Volume);
            RegisterSetting(VolumeOption);

            RegisterSetting(Voice);
            RegisterSetting(VoiceVolume);
            RegisterSetting(VoiceVolumeOption);
            RegisterSetting(VoiceSpeed);
            RegisterSetting(VoiceDeviceIndex);
            RegisterSetting(VoiceFeedbackToggle);

            RegisterSetting(SampleRateHZ);
            RegisterSetting(SampleRateOption);
            RegisterSetting(RestModeToggle);
            RegisterSetting(MouseToggle);

            RegisterSetting(ClickFeedbackToggle);
            RegisterSetting(ClickZoomToggle);
            RegisterSetting(ClickZoomLevel);

            RegisterSetting(BubbleToggle);
            RegisterSetting(BubbleColor);
            RegisterSetting(BubbleOpacity);
            RegisterSetting(BubbleOpacityOption);
            RegisterSetting(BubbleSize);
            RegisterSetting(BubbleSizeOption);
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
            RegisterSetting(RepeatActionInterval);
            RegisterSetting(MostrarPermanenciaDeFijacionesIncompletas);
            RegisterSetting(ButtonAnimationType);
            RegisterSetting(ButtonAnimationColor);
            RegisterSetting(OpcionTiempoDeActivacion);
            RegisterSetting(OpcionTiempoDeInicioDeActivacion);
            RegisterSetting(OpcionPermanenciaDeFijacionesIncompletas);
            RegisterSetting(OpcionTiempoDeActivacionDeTeclado);
            RegisterSetting(OpcionMultiplicadorDeVelocidadDeDesactivacion);

            RegisterSetting(ColorScheme);
            RegisterSetting(UserInterfaceSize);
            RegisterSetting(BarPosition);

            RegisterSetting(KeyboardSize);
            RegisterSetting(KeyboardFontSize);
            RegisterSetting(KeyboardType);
            RegisterSetting(WordPrediction);
            RegisterSetting(WordPredictionLearning);
            RegisterSetting(KeyboardVoiceFeedback);

            RegisterSetting(BarridoEyetrackerMode);
            RegisterSetting(BarridoTiempoDeEspera);
            RegisterSetting(BarridoTiempoDeEsperaOption);
            RegisterSetting(BarridoVoiceFeedback);

            RegisterSetting(AutoHelpCall);
            RegisterSetting(AutoHelpCallTimeInMinutes);
            RegisterSetting(HelpCallAction);
            RegisterSetting(HelpCallTimeOption);
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



    #region STATE CHANGE SETTINGS
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

        public override string GetDocumentationReference()
        {

            StringBuilder builder = new StringBuilder();
            builder.AppendLine($"Key: {descriptor.saveKey}");
            builder.AppendLine($"Type: {descriptor.typeAsString}");
            builder.Append($"Values: ");

            string[] options = Enum.GetNames(typeof(FilterProfile));
            foreach (string option in options)
            {
                builder.Append(option + ", ");
            }
            return builder.ToString();
        }
    }

    public class FontSizeTitlesOptionSetting : EnumSetting<FontSize>
    {
        public FontSizeTitlesOptionSetting(string saveKey, FontSize defaultValue) : base(saveKey, defaultValue)
        {
        }

        public override void OnValueChangedAction(FontSize value)
        {
            switch (value)
            {
                case FontSize.Small:
                    Settings.I.FontSizeTitles.Value = 20d;
                    break;
                case FontSize.Medium:
                    Settings.I.FontSizeTitles.Value = 28d;
                    break;
                case FontSize.Large:
                    Settings.I.FontSizeTitles.Value = 40d;
                    break;             
            }
        }
    }

    public class FocusTimeOptionSetting : EnumSetting<FocusTimeOption>
    {
        public FocusTimeOptionSetting(string saveKey, FocusTimeOption defaultValue) : base(saveKey, defaultValue)
        {
        }

        public override void OnValueChangedAction(FocusTimeOption value)
        {
            switch (value)
            {
                case FocusTimeOption.HalfSecond:
                    Settings.I.TiempoDeActivacion.Value = .5f;
                    break;
                case FocusTimeOption.OneSecond:
                    Settings.I.TiempoDeActivacion.Value = 1f;
                    break;
                case FocusTimeOption.OneAndAHalfSecond:
                    Settings.I.TiempoDeActivacion.Value = 1.5f;
                    break;
                case FocusTimeOption.TwoSeconds:
                    Settings.I.TiempoDeActivacion.Value = 2f;
                    break;
                case FocusTimeOption.ThreeSeconds:
                    Settings.I.TiempoDeActivacion.Value = 3f;
                    break;
                case FocusTimeOption.FiveSeconds:
                    Settings.I.TiempoDeActivacion.Value = 5f;
                    break;
            }
        }
    }

    public class KeyboardFocusTimeOptionSetting : EnumSetting<FocusTimeOption>
    {
        public KeyboardFocusTimeOptionSetting(string saveKey, FocusTimeOption defaultValue) : base(saveKey, defaultValue)
        {
        }

        public override void OnValueChangedAction(FocusTimeOption value)
        {
            switch (value)
            {
                case FocusTimeOption.HalfSecond:
                    Settings.I.TiempoDeActivacionDeTeclado.Value = .5f;
                    break;
                case FocusTimeOption.OneSecond:
                    Settings.I.TiempoDeActivacionDeTeclado.Value = 1f;
                    break;
                case FocusTimeOption.OneAndAHalfSecond:
                    Settings.I.TiempoDeActivacionDeTeclado.Value = 1.5f;
                    break;
                case FocusTimeOption.TwoSeconds:
                    Settings.I.TiempoDeActivacionDeTeclado.Value = 2f;
                    break;
                case FocusTimeOption.ThreeSeconds:
                    Settings.I.TiempoDeActivacionDeTeclado.Value = 3f;
                    break;
                case FocusTimeOption.FiveSeconds:
                    Settings.I.TiempoDeActivacionDeTeclado.Value = 5f;
                    break;
            }
        }
    }

    public class RepeatActionIntervalOptionSetting : EnumSetting<FocusTimeOption>
    {
        public RepeatActionIntervalOptionSetting(string saveKey, FocusTimeOption defaultValue) : base(saveKey, defaultValue)
        {
        }

        public override void OnValueChangedAction(FocusTimeOption value)
        {
            switch (value)
            {
                case FocusTimeOption.HalfSecond:
                    Settings.I.RepeatActionInterval.Value = .5f;
                    break;
                case FocusTimeOption.OneSecond:
                    Settings.I.RepeatActionInterval.Value = 1f;
                    break;
                case FocusTimeOption.OneAndAHalfSecond:
                    Settings.I.RepeatActionInterval.Value = 1.5f;
                    break;
                case FocusTimeOption.TwoSeconds:
                    Settings.I.RepeatActionInterval.Value = 2f;
                    break;
                case FocusTimeOption.ThreeSeconds:
                    Settings.I.RepeatActionInterval.Value = 3f;
                    break;
                case FocusTimeOption.FiveSeconds:
                    Settings.I.RepeatActionInterval.Value = 5f;
                    break;
            }
        }
    }

    public class BarridoTiempoDeEsperaOption : EnumSetting<FocusTimeOption>
    {
        public BarridoTiempoDeEsperaOption(string saveKey, FocusTimeOption defaultValue) : base(saveKey, defaultValue)
        {
        }

        public override void OnValueChangedAction(FocusTimeOption value)
        {
            switch (value)
            {
                case FocusTimeOption.HalfSecond:
                    Settings.I.BarridoTiempoDeEspera.Value = .5f;
                    break;
                case FocusTimeOption.OneSecond:
                    Settings.I.BarridoTiempoDeEspera.Value = 1f;
                    break;
                case FocusTimeOption.OneAndAHalfSecond:
                    Settings.I.BarridoTiempoDeEspera.Value = 1.5f;
                    break;
                case FocusTimeOption.TwoSeconds:
                    Settings.I.BarridoTiempoDeEspera.Value = 2f;
                    break;
                case FocusTimeOption.ThreeSeconds:
                    Settings.I.BarridoTiempoDeEspera.Value = 3f;
                    break;
                case FocusTimeOption.FiveSeconds:
                    Settings.I.BarridoTiempoDeEspera.Value = 5f;
                    break;
            }
        }
    }

    public class IncompleteFocusOptionSetting : EnumSetting<FocusStartOption>
    {
        public IncompleteFocusOptionSetting(string saveKey, FocusStartOption defaultValue) : base(saveKey, defaultValue)
        {
        }

        public override void OnValueChangedAction(FocusStartOption value)
        {
            switch (value)
            {
                case FocusStartOption.Low:
                    Settings.I.PermanenciaDeFijacionesIncompletas.Value = .01f;
                    break;
                case FocusStartOption.Medium:
                    Settings.I.PermanenciaDeFijacionesIncompletas.Value = .2f;
                    break;
                case FocusStartOption.High:
                    Settings.I.PermanenciaDeFijacionesIncompletas.Value = .5f;
                    break;
            }
        }
    }

    public class StartFocusTimeOptionSetting : EnumSetting<FocusStartOption>
    {
        public StartFocusTimeOptionSetting(string saveKey, FocusStartOption defaultValue) : base(saveKey, defaultValue)
        {
        }

        public override void OnValueChangedAction(FocusStartOption value)
        {
            switch (value)
            {
                case FocusStartOption.Low:
                    Settings.I.TiempoDeInicioDeActivacion.Value = .01f;
                    break;
                case FocusStartOption.Medium:
                    Settings.I.TiempoDeInicioDeActivacion.Value = .2f;
                    break;
                case FocusStartOption.High:
                    Settings.I.TiempoDeInicioDeActivacion.Value = .5f;
                    break;
            }
        }
    }

    public class DecayMultiplierOptionSetting : EnumSetting<DecayMultiplier>
    {
        public DecayMultiplierOptionSetting(string saveKey, DecayMultiplier defaultValue) : base(saveKey, defaultValue)
        {
        }

        public override void OnValueChangedAction(DecayMultiplier value)
        {
            switch (value)
            {
                case DecayMultiplier.x1:
                    Settings.I.MultiplicadorDeVelocidadDeDesactivacion.Value = 1f;
                    break;
                case DecayMultiplier.x2:
                    Settings.I.MultiplicadorDeVelocidadDeDesactivacion.Value = 2f;
                    break;
                case DecayMultiplier.x3:
                    Settings.I.MultiplicadorDeVelocidadDeDesactivacion.Value = 3f;
                    break;
            }
        }
    }

    public class BubbleOpacityOptionSetting : EnumSetting<OpacityOption>
    {
        public BubbleOpacityOptionSetting(string saveKey, OpacityOption defaultValue) : base(saveKey, defaultValue)
        {
        }

        public override void OnValueChangedAction(OpacityOption value)
        {
            switch (value)
            {
                case OpacityOption.Low:
                    Settings.I.BubbleOpacity.Value = .1d;
                    break;
                case OpacityOption.Medium:
                    Settings.I.BubbleOpacity.Value = .25d;
                    break;
                case OpacityOption.High:
                    Settings.I.BubbleOpacity.Value = .5d;
                    break;
                case OpacityOption.Ultra:
                    Settings.I.BubbleOpacity.Value = 1d;
                    break;
            }
        }
    }

    public class BubbleSizeOptionSetting : EnumSetting<Size>
    {
        public BubbleSizeOptionSetting(string saveKey, Size defaultValue) : base(saveKey, defaultValue)
        {
        }

        public override void OnValueChangedAction(Size value)
        {
            switch (value)
            {
                case Size.VerySmall:
                    Settings.I.BubbleSize.Value = 10d;
                    break;
                case Size.Small:
                    Settings.I.BubbleSize.Value = 15d;
                    break;
                case Size.Medium:
                    Settings.I.BubbleSize.Value = 40d;
                    break;
                case Size.Large:
                    Settings.I.BubbleSize.Value = 60d;
                    break;
                case Size.VeryLarge:
                    Settings.I.BubbleSize.Value = 100d;
                    break;
            }
        }
    }

    public class HelpCallTimeOption : EnumSetting<HelpCallTime>
    {
        public HelpCallTimeOption(string saveKey, HelpCallTime defaultValue) : base(saveKey, defaultValue)
        {
        }

        public override void OnValueChangedAction(HelpCallTime value)
        {
            switch (value)
            {
                case HelpCallTime.Three:
                    Settings.I.AutoHelpCallTimeInMinutes.Value = 3;
                    break;
                case HelpCallTime.Five:
                    Settings.I.AutoHelpCallTimeInMinutes.Value = 5;
                    break;
                case HelpCallTime.Ten:
                    Settings.I.AutoHelpCallTimeInMinutes.Value = 10;
                    break;
                case HelpCallTime.Fifteen:
                    Settings.I.AutoHelpCallTimeInMinutes.Value = 15;
                    break;
              
            }
        }
    }

    public class SampleRateOption : EnumSetting<SampleRate>
    {
        public SampleRateOption(string saveKey, SampleRate defaultValue) : base(saveKey, defaultValue)
        {
        }

        public override void OnValueChangedAction(SampleRate value)
        {
            switch (value)
            {
                case SampleRate.x20:
                    Settings.I.SampleRateHZ.Value = 20;
                    break;
                case SampleRate.x30:
                    Settings.I.SampleRateHZ.Value = 30;
                    break;
                case SampleRate.x60:
                    Settings.I.SampleRateHZ.Value = 60;
                    break;             
            }
        }
    }

    #endregion

    #region SETTING TYPES
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
    public abstract class BaseSetting : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void NotifyPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public abstract void SetValue(object value);
        public abstract object GetValue();
        public abstract void LoadSetting();
        public abstract SettingDescriptor Descriptor { get; }

        public abstract string GetDocumentationReference();
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
                NotifyPropertyChanged(nameof(Value));
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

        public override string GetDocumentationReference()
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine($"Key: {Descriptor.saveKey}");
            builder.AppendLine($"Type: {Descriptor.typeAsString}");
            return builder.ToString();
        }
    }
    public class Setting<T> : BaseSetting<T>
    {
        public Setting(string saveKey, T defaultValue)
        {
            this.descriptor = new SettingDescriptor(saveKey, typeof(T), defaultValue);
        }
        public override SettingDescriptor Descriptor => descriptor;
        SettingDescriptor descriptor;

        public override string GetDocumentationReference()
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine($"Key: {descriptor.saveKey}");
            builder.AppendLine($"Type: {descriptor.typeAsString}");
            return builder.ToString();
        }
    }
    public class EnumSetting<T> : BaseSetting where T : Enum
    {
        public event Action OnChanged;
        public event Action<T> OnValueChanged;
        public override SettingDescriptor Descriptor => descriptor;
        SettingDescriptor descriptor;
        public Array EnumValues => Enum.GetValues(typeof(T));
        T valueCache;
        public T Value
        {
            get { return valueCache; }
            set
            {
                if (EqualityComparer<T>.Default.Equals(valueCache, value)) return;
                valueCache = value;
                SaveSetting();

                NotifyPropertyChanged(nameof(Value));
                OnChanged?.Invoke();
                OnValueChanged?.Invoke(valueCache);
                GlobalEvents.OnSettingChanged.Invoke(Descriptor.saveKey);
                OnValueChangedAction(value);
            }
        }
        public EnumSetting(string saveKey, T defaultValue)
        {
            descriptor = new SettingDescriptor(saveKey, typeof(T), defaultValue);
        }

        public override string GetDocumentationReference()
        {

            StringBuilder builder = new StringBuilder();
            builder.AppendLine($"Key: {descriptor.saveKey}");
            builder.AppendLine($"Type: {descriptor.typeAsString}");
            builder.Append($"Values: ");

            string[] options = Enum.GetNames(typeof(T));
            foreach (string option in options)
            {
                builder.Append(option + ", ");
            }
            return builder.ToString();
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

        public virtual void OnValueChangedAction(T value)
        {
            //Para implementar con herencia, especialmente para opciones de gaze que modifiquen el valor de otros settings mas generales.
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

        public override string GetDocumentationReference()
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine($"Key: {descriptor.saveKey}");
            builder.AppendLine($"Type: {descriptor.typeAsString}");
            if (clampRange != null)
            {
                builder.AppendLine($"Range: {clampRange.Value.x},{clampRange.Value.y}");
            }
            return builder.ToString();
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

        public override string GetDocumentationReference()
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine($"Key: {descriptor.saveKey}");
            builder.AppendLine($"Type: {descriptor.typeAsString}");
            if (clampRange != null)
            {
                builder.AppendLine($"Range: {clampRange.Value.x},{clampRange.Value.y}");
            }
            return builder.ToString();
        }
    }
    public class DoubleSetting : BaseSetting<double>
    {

        SettingDescriptor descriptor;
        Double2? clampRange;
        public override SettingDescriptor Descriptor => descriptor;

        public DoubleSetting(string saveKey, double defaultValue = 0, Double2? clampRange = null)
        {
            if (clampRange.HasValue && clampRange.Value.x > clampRange.Value.y)
            {
                throw new Exception($"Setting value {saveKey} has an invalid value range.");
            }


            this.descriptor = new SettingDescriptor(saveKey, typeof(double), NormalizeValue(defaultValue));
            this.clampRange = clampRange;
        }

        protected override double NormalizeValue(double input)
        {
            if (!clampRange.HasValue) return input;

            Double2 clamp = clampRange.Value;
            double normalized = Math.Clamp(input, clamp.x, clamp.y);
            return normalized;
        }

        public override string GetDocumentationReference()
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine($"Key: {descriptor.saveKey}");
            builder.AppendLine($"Type: {descriptor.typeAsString}");
            if (clampRange != null)
            {
                builder.AppendLine($"Range: {clampRange.Value.x},{clampRange.Value.y}");
            }
            return builder.ToString();
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
    public class LastCalibration : BaseSetting
    {
        public event Action OnChanged;
        public event Action<byte[]> OnValueChanged;

        byte[] valueCache;
        public byte[] Value
        {
            get { return valueCache; }
            set
            {
                if (EqualityComparer<byte[]>.Default.Equals(valueCache, value)) return;
                valueCache = value;
                SaveSetting();
                NotifyPropertyChanged(nameof(Value));
                OnChanged?.Invoke();
                OnValueChanged?.Invoke(valueCache);
                GlobalEvents.OnSettingChanged.Invoke(Descriptor.saveKey);
            }
        }
        SettingDescriptor descriptor;
        public override SettingDescriptor Descriptor => descriptor;
        public LastCalibration(string saveKey, byte[] defaultValue)
        {
            descriptor = new SettingDescriptor(saveKey, typeof(byte[]), defaultValue);
        }

        public override void SetValue(object value)
        {
            if (value == null) return;
            string jsonValue = value.ToString();
            Debug.WriteLine($"Setting Enum JsonValue: {jsonValue}");
            byte[] calibBuff = Convert.FromBase64String(jsonValue);
            Value = calibBuff;
        }
        public override object GetValue()
        {
            return Value;
        }
        public override void LoadSetting()
        {
            Settings.RegisterDescriptor(Descriptor);
            valueCache = SaveManager.GetSystemSetting<byte[]>(Descriptor.saveKey, (byte[])Descriptor.defaultValue);
            OnValueChanged?.Invoke(valueCache);
        }

        void SaveSetting()
        {
            SaveManager.SetSystemSetting<byte[]>(Descriptor.saveKey, Value);
            SaveManager.SaveSystemSettings();
        }

        public override string GetDocumentationReference()
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine($"Key: {descriptor.saveKey}");
            builder.AppendLine($"Type: {descriptor.typeAsString}");
            return builder.ToString();
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

    public struct Double2
    {
        public double x;
        public double y;

        public Double2(double x, double y)
        {
            this.x = x;
            this.y = y;
        }
    }

    [AttributeUsage(AttributeTargets.Field)]
    public class IconAttribute : Attribute
    {
        public ImageSource Icon { get; }

        public IconAttribute(string uri)
        {
            Icon = new BitmapImage(new Uri(uri, UriKind.RelativeOrAbsolute));
        }
    }

    [AttributeUsage(AttributeTargets.Field)]
    public class ColorBrushAttribute : Attribute
    {
        public string BrushKey { get; }

        public ColorBrushAttribute(string brushKey)
        {
            BrushKey = brushKey;
        }
    }
    #endregion
}
