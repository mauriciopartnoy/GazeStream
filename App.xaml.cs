using System.Configuration;
using System.Data;
using W = System.Windows;
using GazeStream.AppData;
using System.Windows.Threading;
using GazeStream.Utilities;
using GazeStream.Windows;
using System.Windows;
using System.Windows.Input;
using System.Diagnostics;
using System.Threading;
using System;
using GazeStream.Eyetracker;
using InputSimulatorEx;
using System.Reflection;

namespace GazeStream
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : W.Application
    {
        public static App Instance { get; private set; }
        public Dispatcher UIDispatcher { get; private set; }
        public User ActiveUser { get; private set; } = User.GetDefaultUser();
        public Settings SettingsManager { get; private set; }
        public WebSocketStream Websocket { get; private set; }
        public CalibrationPresets CalibrationPresets { get; private set; }
        public GazeManager GazeManager { get; private set; }
        public TrayService Tray { get; private set; }

        public InputSimulator InputSim { get; private set; }
        public OverlayWindow OverlayWindow { get; private set; }

        Hotkeys hotkeys;
        static Mutex? singleInstanceMutex;

        protected override void OnStartup(W.StartupEventArgs e)
        {
            if (e.Args.Contains("--restart"))
            {
                Thread.Sleep(1000); // wait for previous instance to exit
            }

            if (ForcedSingleInstance()) return;

            base.OnStartup(e);
            Instance = this;
            SettingsManager = new Settings();
            SettingsManager.Initialize();
            
            SettingsManager.SampleRateHZ.Value = 60;

            InputSim = new InputSimulator();
            Tray = new TrayService();
            UIDispatcher = this.Dispatcher;
            CalibrationPresets = new CalibrationPresets();
            GazeManager = new GazeManager();
            Websocket = new WebSocketStream();
            Websocket.StartWebsocketService();

            HookHotkeys();
            OverlayWindow = WindowManager.OpenWindow<OverlayWindow>();
        }

        public static void RestartApp()
        {
            var exePath = Process.GetCurrentProcess().MainModule.FileName;

            Process.Start(new ProcessStartInfo
            {
                FileName = exePath,
                Arguments = "--restart",
                UseShellExecute = true
            });

            Current.Shutdown();
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            DisposeMutex();
            base.OnExit(e);
            if (hotkeys != null) hotkeys.Dispose();
            if (Websocket != null) Websocket.Dispose();
            if (GazeManager != null) GazeManager.Dispose();
            if (Tray != null) Tray.Dispose();
        }

        private static void DisposeMutex()
        {
            singleInstanceMutex?.ReleaseMutex();
            singleInstanceMutex?.Dispose();
            singleInstanceMutex = null;
        }

        bool ForcedSingleInstance()
        {
            const string mutexName = "GazeStream.TrayApp.SingleInstance";
            bool createdNew;
            singleInstanceMutex = new Mutex(true, mutexName, out createdNew);
            if (!createdNew)
            {
                // Another instance is already running
                Shutdown();
                return true;
            }
            return false;
        }

        void HookHotkeys()
        {
            hotkeys = new Hotkeys();
            hotkeys.Register(Hotkeys.HotkeyModifiers.None, Key.F10, ToggleMouseControl);
            hotkeys.Register(Hotkeys.HotkeyModifiers.None, Key.F9, ToggleBubble);

        }

        void ToggleMouseControl()
        {
            Settings.I.MouseToggle.Value = !Settings.I.MouseToggle.Value;
        }

        void ToggleBubble()
        {
            Settings.I.BubbleToggle.Value = !Settings.I.BubbleToggle.Value;
        }
    }

}
