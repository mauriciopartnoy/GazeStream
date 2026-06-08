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
using System.Threading.Tasks;
using System;
using GazeStream.Eyetracker;
using InputSimulatorEx;
using System.Reflection;
using Velopack;
using Velopack.Sources;
using Velopack.Locators;
using System.IO;

namespace GazeStream
{
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

        static IVelopackLocator locator;
        public static string CurrentVersion { get; private set; }
        public static string NewestVersion { get; private set; }

        public static bool IsUpdating { get; private set; }

        static CancellationTokenSource updateCts;
        static UpdateWindow updateWindow;

        protected override void OnStartup(W.StartupEventArgs e)
        {
            //CHECK FOR UPDATES
            //LOCAL UPDATE TEST
            //locator = new TestVelopackLocator(appId: "GazeStream", version: "0.0.0", packagesDir: Path.Combine(Environment.CurrentDirectory, "VelopackTestPackages"));
            //VelopackApp.Build().SetLocator(locator).Run();

            VelopackApp.Build().Run();
        
            //COMMENTED OUT PARA CHEQUEAR SI INTERFIERE CON LOS UPDATES.            
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

            //El chequeo de Updates se hace con delay para dar tiempo a que windows inicie y conecte a la interné.
            _ = UpdateApp(5000);
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

        public static async Task CheckNewestUpdate()
        {
            Debug.WriteLine("Checking Updates");

            //LOCAL UPDATE TEST
            //var mgr = new UpdateManager(new GithubSource(AppPaths.GIT_REPOSITORY_URL, null, false), null, locator);

            try
            {
                var mgr = new UpdateManager(new GithubSource(AppPaths.GIT_REPOSITORY_URL, null, false));
                if (mgr.IsInstalled && mgr.CurrentVersion != null)
                {
                    CurrentVersion = mgr.CurrentVersion.Version.ToString();
                }
                var newVersion = await mgr.CheckForUpdatesAsync();
                if (newVersion == null)
                {
                    Debug.WriteLine("There is no new version online.");
                    return;
                }
                if (newVersion != null)
                {
                    NewestVersion = newVersion.TargetFullRelease.Version.ToNormalizedString();
                    System.Windows.MessageBox.Show("Update detectado!" + NewestVersion);
                }
            }
            catch
            {
                System.Windows.MessageBox.Show("There was an exception. Velopack could not check for updates.");
                Debug.WriteLine("There was an exception. Velopack could not check for updates.");
            }
        }

        public static void CancelUpdate()
        {
            if (updateCts.IsCancellationRequested) return;
            updateCts.Cancel();
        }

            //COMANDOS PARA GENERAR EL UPDATE DESCARGABLE, PRIMERO TENER VPK: dotnet tool install -g vpk 

            //dotnet publish --self-contained -r win-x64 -o .\publish
            //vpk pack --packId GazeStream --packVersion 1.0.0 --packDir ./publish
        public static async Task UpdateApp(int delay = 0)
        {
            updateCts = new();
            await Task.Delay(delay, updateCts.Token);
            try
            {
                IsUpdating = true;
                var mgr = new UpdateManager(new GithubSource(AppPaths.GIT_REPOSITORY_URL, null, false));
                if (mgr.IsInstalled && mgr.CurrentVersion != null)
                {
                    CurrentVersion = mgr.CurrentVersion.Version.ToString();
                }
                var newVersion = await mgr.CheckForUpdatesAsync();

                if (newVersion == null) return;
                NewestVersion = newVersion.TargetFullRelease.Version.ToNormalizedString();

                //OPEN UPDATE WINDOW
                updateWindow = new UpdateWindow();
                updateWindow.ViewModel.CurrentVersion = CurrentVersion;
                updateWindow.ViewModel.NewVersion = NewestVersion;
                updateWindow.Show();

                await mgr.DownloadUpdatesAsync(newVersion, UpdateProgressValue, updateCts.Token);
                mgr.ApplyUpdatesAndRestart(newVersion);
            }
            catch (Exception e)
            {
                System.Windows.MessageBox.Show("No pudo hacerse la actualización: " + e.Message);
            }
            finally
            {
                //CLOSE UPDATE WINDOW
                updateWindow?.Close();
                updateWindow = null;
                IsUpdating = false;
            }
        }

        static void UpdateProgressValue(int progress)
        {
            App.Current.Dispatcher.Invoke(() =>
            {
            if (updateWindow == null) return;

            updateWindow.ViewModel.Progress = progress;
          
            if (progress < 100)
                {
                    updateWindow.ViewModel.Status =
                        "Descargando actualización...";
                }
                else
                {
                    updateWindow.ViewModel.Status =
                        "Preparando instalación. Por favor, espere...";
                }
            });
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
