using System;
using System.Drawing;
using W = System.Windows;
using GazeStream.Utilities;
using GazeStream.Windows;
using GazeStream.Eyetracker;
using GazeStream.AppData;
using GazeStream.Utilities.Events;
using System.Diagnostics;

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
            GlobalEvents.OnVersionChecked.Add(UpdateVersion);
        }

        private static Icon LoadTrayIcon()
        {
            var uri = new Uri("pack://application:,,,/GazeStream;component/Resources/Icons/tray5.ico",UriKind.Absolute);
            using var stream = System.Windows.Application.GetResourceStream(uri)!.Stream;
            return new Icon(stream);
        }

        void UpdateVersion()
        {
            Debug.WriteLine("Version update called");
            trayIcon.ContextMenuStrip = BuildMenu();
        }

        private ContextMenuStrip BuildMenu()
        {
            //Images
            var calibrationUri = new Uri("pack://application:,,,/GazeStream;component/Resources/Icons/calib.png", UriKind.Absolute);
            var calibrationStream = System.Windows.Application.GetResourceStream(calibrationUri).Stream;
            Image calibrationImg = Image.FromStream(calibrationStream);

            var restartEyetrackerUri = new Uri("pack://application:,,,/GazeStream;component/Resources/Icons/arrows-clockwise.png", UriKind.Absolute);
            var restartEyetrackerStream = System.Windows.Application.GetResourceStream(restartEyetrackerUri).Stream;
            Image restartEyetrackerImg = Image.FromStream(restartEyetrackerStream);

            var calibResUri = new Uri("pack://application:,,,/GazeStream;component/Resources/Icons/carpetas.png", UriKind.Absolute);
            var calibResStream = System.Windows.Application.GetResourceStream(calibResUri).Stream;
            Image calibResImg = Image.FromStream(calibResStream);

            var camUri = new Uri("pack://application:,,,/GazeStream;component/Resources/Icons/camera.png", UriKind.Absolute);
            var camStream = System.Windows.Application.GetResourceStream(camUri).Stream;
            Image camImg = Image.FromStream(camStream);

            var optionsUri = new Uri("pack://application:,,,/GazeStream;component/Resources/Icons/eyetracker-options.png", UriKind.Absolute);
            var optionsStream = System.Windows.Application.GetResourceStream(optionsUri).Stream;
            Image optionsImg = Image.FromStream(optionsStream);

            var systemUri = new Uri("pack://application:,,,/GazeStream;component/Resources/Icons/options.png", UriKind.Absolute);
            var systemStream = System.Windows.Application.GetResourceStream(systemUri).Stream;
            Image systemImg = Image.FromStream(systemStream);

            var menu = new ContextMenuStrip();
            string version = $"Versión {App.CurrentVersion}";
            menu.Items.Add(version, null, Empty);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("Reiniciar Eyetracker", restartEyetrackerImg, OnRestartEyetracker);
            menu.Items.Add("Calibración", calibrationImg, OnCalibration);
            menu.Items.Add("Resultados de Calibración", calibResImg, OnCalibrationResults);
            menu.Items.Add("Cámara", camImg, OnCamera);
            menu.Items.Add("Opciones", optionsImg, OnOptions);
            menu.Items.Add("Sistema", systemImg, OnSystemSettings);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("Salir", null, OnExit);

            foreach (ToolStripItem item in menu.Items)
            {
                if (item is ToolStripMenuItem menuItem)
                {
                    menuItem.AutoSize = false;
                    menu.Font = new Font(menu.Font.FontFamily, 14f);
                    menuItem.Height = 48;
                    menuItem.ImageAlign = ContentAlignment.MiddleCenter;
                    menuItem.TextAlign = ContentAlignment.MiddleCenter;
                }
            }

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

        private void OnSystemSettings(object? sender, EventArgs e)
        {
            WindowManager.OpenWindow<SystemSettingsWindow>();
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
