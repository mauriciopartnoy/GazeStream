using System;
using System.Collections.Generic;
using System.Windows.Interop;
using GazeStream.Utilities;
using System.Windows.Input;
namespace GazeStream.AppData
{
    class Hotkeys : IDisposable
    {

        private readonly HotkeyWindow _window;
        private int _currentId = 0;
        private readonly Dictionary<int, Action> _actions = new();

        public Hotkeys()
        {
            _window = new HotkeyWindow();
            _window.HotkeyPressed += OnHotkeyPressed;
        }

        public void Register(
            HotkeyModifiers modifiers,
            Key key,
            Action action)
        {
            int id = ++_currentId;
            int vk = KeyInterop.VirtualKeyFromKey(key);

            if (!WindowsHelper.RegisterHotKey(
                    _window.Handle,
                    id,
                    (int)modifiers,
                    vk))
            {
                throw new InvalidOperationException("Failed to register hotkey.");
            }

            _actions[id] = action;
        }

        private void OnHotkeyPressed(int id)
        {
            if (_actions.TryGetValue(id, out var action))
                action.Invoke();
        }

        public void Dispose()
        {
            foreach (var id in _actions.Keys)
                WindowsHelper.UnregisterHotKey(_window.Handle, id);

            _window.Dispose();
        }

        [Flags]
        public enum HotkeyModifiers : uint
        {
            None = 0,
            Alt = 1,
            Ctrl = 2,
            Shift = 4,
            Win = 8
        }
    }

    internal sealed class HotkeyWindow : HwndSource
    {
        public event Action<int>? HotkeyPressed;
        public const int WM_HOTKEY = 0x0312;

        public HotkeyWindow() : base(new HwndSourceParameters
        {
            Width = 0,
            Height = 0,
            WindowStyle = 0,
            ParentWindow = IntPtr.Zero
        })
        {
            AddHook(WndProc);
        }

        private IntPtr WndProc(
            IntPtr hwnd,
            int msg,
            IntPtr wParam,
            IntPtr lParam,
            ref bool handled)
        {
            if (msg == WM_HOTKEY)
            {
                int id = wParam.ToInt32();
                HotkeyPressed?.Invoke(id);
                handled = true;
            }

            return IntPtr.Zero;
        }
    }

}
