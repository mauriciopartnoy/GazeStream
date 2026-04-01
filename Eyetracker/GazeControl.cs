using System.Windows.Controls;
using System.Windows;
using System.Diagnostics;
using GazeStream.AppData;
using B = System.Windows.Media;
using GazeStream.Utilities;
using System.Windows.Threading;
using System.ComponentModel;
namespace GazeStream.Eyetracker
{
    public abstract class GazeControl : ContentControl, IGazeTarget
    {
        public GazeControl()
        {
            Loaded += GazeButton_Loaded;
            Unloaded += GazeButton_Unloaded;
        }

        private void GazeButton_Loaded(object sender, RoutedEventArgs e)
        {
            if (DesignerProperties.GetIsInDesignMode(this)) return;

            ApplySettings();

            Settings.I.ButtonAnimationType.OnChanged += ApplySettings;
            Settings.I.ButtonAnimationColor.OnChanged += ApplySettings;

        }

        private void GazeButton_Unloaded(object sender, RoutedEventArgs e)
        {
            if (DesignerProperties.GetIsInDesignMode(this)) return;
            Settings.I.ButtonAnimationType.OnChanged -= ApplySettings;
            Settings.I.ButtonAnimationColor.OnChanged -= ApplySettings;
        }

        private void ApplySettings()
        {
            Dispatcher.Invoke(() =>
            {
                AnimationType = Settings.I.ButtonAnimationType.Value;
                AnimationColor = Helper.GetBrushFromBasicColorEnum(Settings.I.ButtonAnimationColor.Value);
            });

        }

        public B.Brush AnimationColor
        {
            get => (B.Brush)GetValue(AnimationColorProperty);
            set => SetValue(AnimationColorProperty, value);
        }

        public static readonly DependencyProperty AnimationColorProperty =
            DependencyProperty.Register(
                nameof(AnimationColor),
                typeof(B.Brush),
                typeof(GazeControl),
                new PropertyMetadata(B.Brushes.DodgerBlue));

        public Button_Animation AnimationType
        {
            get => (Button_Animation)GetValue(AnimationTypeProperty);
            set => SetValue(AnimationTypeProperty, value);
        }

        public static readonly DependencyProperty AnimationTypeProperty =
            DependencyProperty.Register(
                nameof(AnimationType),
                typeof(Button_Animation),
                typeof(GazeControl),
                new PropertyMetadata(
                    Button_Animation.Clock,
                    OnAnimationTypeChanged));

        private static void OnAnimationTypeChanged(
        DependencyObject d,
        DependencyPropertyChangedEventArgs e)
        {
            var btn = (GazeButton)d;

            // If needed, adjust internal state
            //Reset focus...?
        }

        public double FocusTimeOverride
        {
            get => (double)GetValue(FocusTimeOverrideProperty);
            set => SetValue(FocusTimeOverrideProperty, value);
        }

        public static readonly DependencyProperty FocusTimeOverrideProperty =
            DependencyProperty.Register(
                nameof(FocusTimeOverride),
                typeof(double),
                typeof(GazeControl),
                new PropertyMetadata(0d));

        public bool IgnoreRestMode
        {
            get => (bool)GetValue(IgnoreRestModeProperty);
            set => SetValue(IgnoreRestModeProperty, value);
        }

        public static readonly DependencyProperty IgnoreRestModeProperty =
            DependencyProperty.Register(
                nameof(IgnoreRestMode),
                typeof(bool),
                typeof(GazeControl),
                new PropertyMetadata(false));

        public bool RepeatAction
        {
            get => (bool)GetValue(RepeatActionProperty);
            set => SetValue(RepeatActionProperty, value);
        }

        public static readonly DependencyProperty RepeatActionProperty =
            DependencyProperty.Register(
                nameof(RepeatAction),
                typeof(bool),
                typeof(GazeControl),
                new PropertyMetadata(false));

        public double RepeatActionIntervalOverride
        {
            get => (double)GetValue(RepeatActionIntervalOverrideProperty);
            set => SetValue(RepeatActionIntervalOverrideProperty, value);
        }

        public static readonly DependencyProperty RepeatActionIntervalOverrideProperty =
            DependencyProperty.Register(
                nameof(RepeatActionIntervalOverride),
                typeof(double),
                typeof(GazeControl),
                new PropertyMetadata(0d));


        private static readonly DependencyPropertyKey HasGazePropertyKey =
        DependencyProperty.RegisterReadOnly(
        nameof(HasGaze),
        typeof(bool),
        typeof(GazeControl),
        new PropertyMetadata(false));

        public static readonly DependencyProperty HasGazeProperty =
            HasGazePropertyKey.DependencyProperty;

        public bool HasGaze
        {
            get => (bool)GetValue(HasGazeProperty);
            protected set => SetValue(HasGazePropertyKey, value);
        }

        private static readonly DependencyPropertyKey HasFocusPropertyKey =
      DependencyProperty.RegisterReadOnly(
          nameof(HasFocus),
          typeof(bool),
          typeof(GazeControl),
          new PropertyMetadata(false));

        public static readonly DependencyProperty HasFocusProperty =
            HasFocusPropertyKey.DependencyProperty;

        public bool HasFocus
        {
            get => (bool)GetValue(HasFocusProperty);
            protected set => SetValue(HasFocusPropertyKey, value);
        }

        private static readonly DependencyPropertyKey FocusProgressPropertyKey =
    DependencyProperty.RegisterReadOnly(
        nameof(FocusProgress),
        typeof(double),
        typeof(GazeControl),
        new PropertyMetadata(0.0));

        public static readonly DependencyProperty FocusProgressProperty =
            FocusProgressPropertyKey.DependencyProperty;

        public double FocusProgress
        {
            get => (double)GetValue(FocusProgressProperty);
            protected set => SetValue(FocusProgressPropertyKey, value);
        }

        private static readonly DependencyPropertyKey Progress01PropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(Progress01),
                typeof(double),
                typeof(GazeControl),
                new PropertyMetadata(0.0));

        public static readonly DependencyProperty Progress01Property =
            Progress01PropertyKey.DependencyProperty;

        public double Progress01
        {
            get => (double)GetValue(Progress01Property);
            protected set => SetValue(Progress01PropertyKey, value);
        }


        public bool IsFullyInactive => !HasGaze && FocusProgress <= 0 && repeatActionTimer <= 0;

        bool isRepeatingAction;
        bool hadGazeLastFrame;
        bool hadFocusLastFrame;

        bool canStartFocusing => startFocusTimer >= StartFocusTime;
        bool canStartDecay => startDecayTimer >= StartDecayTime;

        double repeatActionTimer;
        double startFocusTimer;
        double startDecayTimer;

        public virtual double StartFocusTime => Settings.I.TiempoDeInicioDeActivacion.Value;
        public virtual double FocusTime => FocusTimeOverride > 0 ? FocusTimeOverride : Settings.I.TiempoDeActivacion.Value;
        public virtual double StartDecayTime => Settings.I.PermanenciaDeFijacionesIncompletas.Value;
        public virtual double RepeatActionInterval => RepeatActionIntervalOverride > 0 ? RepeatActionIntervalOverride : Settings.I.RepeatActionInterval.Value;

        //public bool IsGazeEnabled => IsEnabled && Visibility == Visibility.Visible;

        public void SetHasGaze(bool hasGaze)
        {
            //if (!IsGazeEnabled) return;
            if (IgnoreRestMode == false && Settings.I.RestModeToggle.Value == true)
            {
                HasGaze = false;
                return;
            }
            if (HasGaze == hasGaze) return;
            HasGaze = hasGaze;
        }
    
        public void OnGazeUpdateInternal(double deltaTime)
        {
            //if (!IsGazeEnabled) return;
            SendOnGazeEvents();

            if (HasGaze)
            {
                OnGazeUpdate(deltaTime);
                startDecayTimer = 0;
                UpdateStartFocusTimer(deltaTime);
                UpdateFocus(deltaTime);
            }
            else
            {
                startFocusTimer = 0;
                UpdateStartDecayTimer(deltaTime);
                UpdateDecay(deltaTime);
            }

            SendFocusEvents();
        }

        private void SendOnGazeEvents()
        {
            if (!hadGazeLastFrame && HasGaze)
            {
                OnGazeEnter();
            }
            else if (hadGazeLastFrame && !HasGaze)
            {
                OnGazeExit();
            }
            hadGazeLastFrame = HasGaze;
        }


        private void UpdateStartDecayTimer(double deltaTime)
        {
            startDecayTimer += deltaTime;
            startDecayTimer = Math.Clamp(startDecayTimer, 0, StartDecayTime);
            Debug.WriteLine("Decay timer: " + startDecayTimer);
        }

        private void UpdateStartFocusTimer(double deltaTime)
        {
            startFocusTimer += deltaTime;
            startFocusTimer = Math.Clamp(startFocusTimer, 0, StartFocusTime);
        }

        private void UpdateRepeatActionTimer(double deltaTime)
        {
            if (!RepeatAction) return;
            if (!HasFocus) { repeatActionTimer = 0; isRepeatingAction = false; return; }
            isRepeatingAction = true;
            repeatActionTimer += deltaTime;
            repeatActionTimer = Math.Clamp(repeatActionTimer, 0, RepeatActionInterval);
            Progress01 = Math.Clamp(repeatActionTimer / RepeatActionInterval, 0, 1);
        }

        private void UpdateFocus(double deltaTime)
        {
            if (!canStartFocusing) return;
            if (!HasFocus)
            {
                FocusProgress += deltaTime;
                FocusProgress = Math.Clamp(FocusProgress, 0, FocusTime);
                Progress01 = FocusTime <= 0 ? 0 : Math.Clamp(FocusProgress / FocusTime, 0, 1);
                Debug.WriteLine("Focus: " + FocusProgress);
            }
            else
            {
                UpdateRepeatActionTimer(deltaTime);
            }
        }

        void SendFocusEvents()
        {
            if (!HasFocus && FocusProgress >= FocusTime)
            {
                Debug.WriteLine("OnFocus called!");
                HasFocus = true;
                Progress01 = 0;
                App.Current.Dispatcher.BeginInvoke(DispatcherPriority.Input, OnFocus);
            }
            else if (RepeatAction && HasFocus && repeatActionTimer >= RepeatActionInterval)
            {
                repeatActionTimer = 0;
                FocusProgress = 0;
                App.Current.Dispatcher.BeginInvoke(DispatcherPriority.Input, OnFocus);
            }
            else if (HasFocus && !HasGaze)
            {
                Debug.WriteLine("OnUnfocus called!");

                HasFocus = false;
                FocusProgress = 0;
                App.Current.Dispatcher.BeginInvoke(DispatcherPriority.Input, OnUnfocus);
            }
        }

        private void UpdateDecay(double deltaTime)
        {
            if (!canStartDecay) return;
            double decay = deltaTime * Settings.I.MultiplicadorDeVelocidadDeDesactivacion.Value;
            if (isRepeatingAction)
            {
                repeatActionTimer -= decay;
                repeatActionTimer = Math.Clamp(repeatActionTimer, 0, RepeatActionInterval);
                Progress01 = repeatActionTimer <= 0 ? 0 : Math.Clamp(repeatActionTimer / RepeatActionInterval, 0, 1);
                if (repeatActionTimer <= 0)
                {
                    isRepeatingAction = false;
                }
            }
            else
            {
                FocusProgress -= decay;
                FocusProgress = Math.Clamp(FocusProgress, 0, FocusTime);
                Progress01 = FocusTime <= 0 ? 0 : Math.Clamp(FocusProgress / FocusTime, 0, 1);
            }
        }

        protected virtual void OnGazeUpdate(double deltaTime) { }
        protected virtual void OnGazeEnter() { }
        protected virtual void OnGazeExit() { }
        protected virtual void OnFocus() { }
        protected virtual void OnUnfocus() { }

    }
    public interface IGazeTarget
    {
        bool HasGaze { get; }
        //bool IsGazeEnabled { get; }
        void SetHasGaze(bool hasGaze);
        void OnGazeUpdateInternal(double deltaTime);
    }

}
