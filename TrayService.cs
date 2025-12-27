using System;
using System.Drawing;
using W = System.Windows;

namespace GazeStream
{
    public class TrayService : IDisposable
    {
        private readonly NotifyIcon trayIcon;

        public TrayService()
        {
            trayIcon = new NotifyIcon
            {
                Icon = SystemIcons.Application,
                Text = "Gaze Overlay",
                Visible = true,
                ContextMenuStrip = BuildMenu()
            };
        }

        private ContextMenuStrip BuildMenu()
        {
            var menu = new ContextMenuStrip();

            menu.Items.Add("Calibración", null, OnCalibration);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("Opciones", null, OnCalibration);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("Salir", null, OnExit);

            return menu;
        }

        private void OnOptions(object? sender, EventArgs e)
        {
            W.MessageBox.Show("Placeholder de opciones");
        }

        private void OnCalibration(object? sender, EventArgs e)
        {
            W.MessageBox.Show("Placeholder de calibración");
        }

        private void OnExit(object? sender, EventArgs e)
        {
            Dispose();
            W.Application.Current.Shutdown();
        }

        public void Dispose()
        {
            trayIcon.Visible = false;
            trayIcon.Dispose();
        }
    }
}
