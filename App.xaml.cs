using System.Configuration;
using System.Data;
using W = System.Windows;
using GazeStream.AppData;
using System.Windows.Threading;
namespace GazeStream
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : W.Application
    {
        public static App Instance { get; private set; }
        private TrayService trayService;
        public Dispatcher UIDispatcher { get; private set; }
        public User ActiveUser { get; private set; } = default;

        public Settings SettingsManager { get; private set; }
        protected override void OnStartup(W.StartupEventArgs e)
        {
            base.OnStartup(e);
            Instance = this;
            SettingsManager = new Settings();
            SettingsManager.LoadSettings();
            trayService = new TrayService();
            UIDispatcher = this.Dispatcher;

        }
    }

}
