using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace GazeStream.Utilities
{
    public static class WindowManager
    {
        private static readonly Dictionary<Type, Window> _windows = new();

        public static IEnumerable<Window> Windows => _windows.Values;

        public static T OpenWindow<T>() where T : Window, new()
        {
            var type = typeof(T);

            if (!_windows.TryGetValue(type, out var window))
            {
                window = new T();
                window.Closed += (_, _) => _windows.Remove(type);
                _windows[type] = window;
            }

            if (!window.IsVisible)
                window.Show();

            if (window.WindowState == WindowState.Minimized)
                window.WindowState = WindowState.Normal;

            window.Activate();
            window.Focus();

            return (T)window;
        }

        public static void HideWindow<T>() where T : Window
        {
            if (_windows.TryGetValue(typeof(T), out var window))
            {
                window.Hide();
            }
        }

        public static void CloseWindow<T>() where T : Window
        {
            if (_windows.TryGetValue(typeof(T), out var window))
            {
                window.Close();
                _windows.Remove(typeof(T));
            }
        }

        public static bool IsOpen<T>() where T : Window
        {
            return _windows.ContainsKey(typeof(T));
        }
    }
}
