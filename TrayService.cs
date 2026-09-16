using System;
using System.Drawing;
using System.Windows.Forms;
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
        private readonly ContextMenuStrip contextMenu;
        private ToolStripMenuItem versionItem;

        // Keep references so they can be disposed when the tray is disposed.
        private readonly Image calibrationImg;
        private readonly Image restartEyetrackerImg;
        private readonly Image calibResImg;
        private readonly Image camImg;
        private readonly Image optionsImg;
        private readonly Image systemImg;

        public TrayService()
        {
            // Load images once.
            calibrationImg = LoadTrayImage(
                "pack://application:,,,/GazeStream;component/Resources/Icons/calib.png");

            restartEyetrackerImg = LoadTrayImage(
                "pack://application:,,,/GazeStream;component/Resources/Icons/arrows-clockwise.png");

            calibResImg = LoadTrayImage(
                "pack://application:,,,/GazeStream;component/Resources/Icons/carpetas.png");

            camImg = LoadTrayImage(
                "pack://application:,,,/GazeStream;component/Resources/Icons/camera.png");

            optionsImg = LoadTrayImage(
                "pack://application:,,,/GazeStream;component/Resources/Icons/eyetracker-options.png");

            systemImg = LoadTrayImage(
                "pack://application:,,,/GazeStream;component/Resources/Icons/options.png");

            // Create the menu once.
            contextMenu = BuildMenu();

            trayIcon = new NotifyIcon
            {
                Icon = LoadTrayIcon(),
                Text = "GazeStream",
                Visible = true,
                ContextMenuStrip = contextMenu
            };

            GlobalEvents.OnVersionChecked.Add(UpdateVersion);
        }

        private static Icon LoadTrayIcon()
        {
            var uri = new Uri(
                "pack://application:,,,/GazeStream;component/Resources/Icons/tray5.ico",
                UriKind.Absolute);

            using var stream =
                W.Application.GetResourceStream(uri)!.Stream;

            return new Icon(stream);
        }

        private static Image LoadTrayImage(string resourcePath)
        {
            var uri = new Uri(resourcePath, UriKind.Absolute);

            using var stream =
                W.Application.GetResourceStream(uri)!.Stream;

            // Image.FromStream() keeps a dependency on the source stream.
            // Clone it into a Bitmap so the resource stream can be disposed.
            using var source = Image.FromStream(stream);

            return new Bitmap(source);
        }

        private ContextMenuStrip BuildMenu()
        {
            var menu = new ContextMenuStrip();

            versionItem = new ToolStripMenuItem(
                $"Versión {App.CurrentVersion}");

            menu.Items.Add(versionItem);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("Reiniciar Eyetracker", restartEyetrackerImg, OnRestartEyetracker);
            menu.Items.Add("Calibración", calibrationImg, OnCalibration);
            menu.Items.Add("Resultados de Calibración", calibResImg, OnCalibrationResults);
            menu.Items.Add("Cámara", camImg, OnCamera);
            menu.Items.Add("Opciones", optionsImg, OnOptions);
            menu.Items.Add("Sistema", systemImg, OnSystemSettings);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("Salir", null, OnExit);

            menu.Font = new Font(menu.Font.FontFamily, 14f);

            foreach (ToolStripItem item in menu.Items)
            {
                if (item is ToolStripMenuItem menuItem)
                {
                    menuItem.AutoSize = false;
                    menuItem.Height = 48;
                    menuItem.ImageAlign = ContentAlignment.MiddleCenter;
                    menuItem.TextAlign = ContentAlignment.MiddleCenter;
                }
            }

            return menu;
        }

        private void UpdateVersion()
        {
            Debug.WriteLine("Version update called");
            versionItem.Text = $"Versión {App.CurrentVersion}";
        }

        private void Empty(object? sender, EventArgs e)
        {
        }

        private void OnRestartEyetracker(object? sender, EventArgs e)
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

            contextMenu.Dispose();

            calibrationImg.Dispose();
            restartEyetrackerImg.Dispose();
            calibResImg.Dispose();
            camImg.Dispose();
            optionsImg.Dispose();
            systemImg.Dispose();

            trayIcon.Dispose();
        }
    }
}