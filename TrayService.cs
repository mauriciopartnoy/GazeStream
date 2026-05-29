using System;
using System.Drawing;
using W = System.Windows;
using GazeStream.Utilities;
using GazeStream.Windows;
using GazeStream.Eyetracker;
using GazeStream.AppData;


namespace GazeStream
{
    public class TrayService : IDisposable
    {
        private readonly NotifyIcon trayIcon;

        public TrayService()
        {
            trayIcon = new NotifyIcon
            {
                Icon = LoadTrayIcon(),
                Text = "GazeStream",
                Visible = true,
                ContextMenuStrip = BuildMenu()
            };
        }

        private static Icon LoadTrayIcon()
        {
            var uri = new Uri("pack://application:,,,/GazeStream;component/Resources/Icons/tray.ico",UriKind.Absolute);
            using var stream = System.Windows.Application.GetResourceStream(uri)!.Stream;
            return new Icon(stream);
        }

        private ContextMenuStrip BuildMenu()
        {
            var menu = new ContextMenuStrip();
            string version = $"Versión {App.CurrentVersion}" + " =)";
            menu.Items.Add(version, null, Empty);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("Reiniciar Eyetracker", null, OnRestartEyetracker);
            menu.Items.Add("Calibración", null, OnCalibration);
            menu.Items.Add("Resultados de Calibración", null, OnCalibrationResults);
            menu.Items.Add("Cámara", null, OnCamera);
            menu.Items.Add("Opciones", null, OnOptions);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("Salir", null, OnExit);

            return menu;
        }

       

        void Empty(object? sender, EventArgs e)
        { 
        
        }

        void OnRestartEyetracker(object? sender, EventArgs e)
        {
            GazeManager.I.DisconnectDevice();
        }

        private void OnOptions(object? sender, EventArgs e)
        {
            WindowManager.OpenWindow<OptionsWindow>();
        }

        private void OnCalibration(object? sender, EventArgs e)
        {
            //DEFAULT
            if (GazeManager.I.GazeDevice == null)
            {
                GazeManager.I.joacoA11.OpenCalibrationPage();
                return;
            }

            GazeManager.I.GazeDevice.OpenCalibrationPage();
        }

        private void OnCalibrationResults(object? sender, EventArgs e)
        {
            FileOps.OpenDirectory(AppPaths.CalibrationPresetsPath);
        }

        private void OnCamera(object? sender, EventArgs e)
        {
            WindowManager.OpenWindow<EyesWindow>();
        }
        private void OnExit(object? sender, EventArgs e)
        {
            W.Application.Current.Shutdown();
        }

        public void Dispose()
        {
            trayIcon.Visible = false;
            trayIcon.Dispose();
        }
    }
}
