using System;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Threading;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;


namespace JoacoDesktop.Utilities
{
    public static class WindowsHelper
    {

        //WINDOW
        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll")]
        private static extern bool SetLayeredWindowAttributes(IntPtr hwnd, uint crKey, byte bAlpha, uint dwFlags);

        [DllImport("user32.dll")]
        static extern IntPtr GetDC(IntPtr hWnd);

        [DllImport("gdi32.dll")]
        static extern int GetDeviceCaps(IntPtr hdc, int nIndex);

        [DllImport("user32.dll")]
        static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

        [DllImport("user32.dll")]
        static extern IntPtr GetActiveWindow();

        [DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        public static extern bool SetForegroundWindow(IntPtr window);

        [DllImport("user32.dll")]
        public static extern bool IsWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        static extern IntPtr WindowFromPoint(POINT p);

        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hwnd, int nCmdShow);

        [DllImport("user32.dll", CharSet = CharSet.Ansi)]
        static extern IntPtr FindWindow(string strClassName, string strWindowName);

        [DllImport("user32.dll")]
        private static extern int GetSystemMetrics(int nIndex);

        [DllImport("user32.dll")]
        static extern uint GetDpiForWindow(IntPtr hwnd);

        [DllImport("dwmapi.dll", CharSet = CharSet.Unicode, PreserveSig = false)]
        public static extern int DwmSetWindowAttribute(IntPtr hwnd, DWMWINDOWATTRIBUTE dwAttribute, ref uint pvAttribute, uint cbAttribute);

        [DllImport("dwmapi.dll")]
        private static extern int DwmExtendFrameIntoClientArea(IntPtr hWnd, ref MARGINS pMarInset);

        [DllImport("user32.dll")]

        //HOTKEYS
        public static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vk);

        [DllImport("user32.dll")]
        public static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        //CURSOR

        [DllImport("user32.dll")]
        static extern bool GetCursorPos(out POINT lpPoint);

        [DllImport("user32.dll", EntryPoint = "ShowCursor")]
        private static extern int _ShowCursor(bool bShow);
        [DllImport("user32.dll")]
        private static extern bool SetSystemCursor(IntPtr hCursor, uint id);

        [DllImport("user32.dll")]
        private static extern bool SystemParametersInfo(uint uiAction, uint uiParam, IntPtr pvParam, uint fWinIni);

        [DllImport("user32.dll")]
        private static extern bool DestroyCursor(IntPtr hCursor);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr CreateIconIndirect(ref ICONINFO iconInfo);

        [DllImport("gdi32.dll", SetLastError = true)]
        private static extern IntPtr CreateBitmap(int nWidth, int nHeight, uint cPlanes, uint cBitsPerPel, IntPtr lpvBits);


        [StructLayout(LayoutKind.Sequential)]
        private struct ICONINFO
        {
            public bool fIcon;
            public int xHotspot;
            public int yHotspot;
            public IntPtr hbmMask;
            public IntPtr hbmColor;
        }

        private static IntPtr? _invisibleCursor;
        private const uint OCR_NORMAL = 32512;
        private const uint SPI_SETCURSORS = 0x0057;
        private const uint SPIF_SENDCHANGE = 0x02;

        public static void SetCursorVisibility(bool visible)
        {
            if (visible)
            {
                SetCursorVisible();
            }
            else SetCursorInvisible();
        }

        public static void SetCursorInvisible()
        {
            if (_invisibleCursor == null)
            {
                _invisibleCursor = CreateInvisibleCursor();
            }

            SetSystemCursor(_invisibleCursor.Value, OCR_NORMAL);
        }

        public static void SetCursorVisible()
        {
            // Restore system cursors
            SystemParametersInfo(SPI_SETCURSORS, 0, IntPtr.Zero, SPIF_SENDCHANGE);

            if (_invisibleCursor is not null)
            {
                DestroyCursor(_invisibleCursor.Value);
                _invisibleCursor = null;
            }
        }

        private static IntPtr CreateInvisibleCursor()
        {
            IntPtr hBitmap = CreateBitmap(1, 1, 1, 1, IntPtr.Zero); // tiny empty bitmap

            ICONINFO iconInfo = new ICONINFO
            {
                fIcon = false,
                xHotspot = 0,
                yHotspot = 0,
                hbmMask = hBitmap,
                hbmColor = IntPtr.Zero
            };

            return CreateIconIndirect(ref iconInfo);
        }

        public static IntPtr FocusWindowAtCursor()
        {
            POINT cursorPos;
            GetCursorPos(out cursorPos);
            IntPtr hWnd = WindowFromPoint(cursorPos);

            // Optionally filter out invisible windows
            if (hWnd != IntPtr.Zero && IsWindowVisible(hWnd))
            {
                SetForegroundWindow(hWnd);
                return hWnd;
            }

            return IntPtr.Zero;
        }

        public static IntPtr FocusWindowAtCursorAndWaitForActivation()
        {
            GetCursorPos(out POINT cursorPos);
            IntPtr hWnd = WindowFromPoint(cursorPos);

            if (hWnd != IntPtr.Zero && IsWindowVisible(hWnd))
            {
                SetForegroundWindow(hWnd);

                // Wait until it's the foreground window (up to 1500ms)
                for (int i = 0; i < 10; i++)
                {
                    if (GetForegroundWindow() == hWnd)
                        break;

                    Thread.Sleep(10);
                }

                return hWnd;
            }

            return IntPtr.Zero;
        }

        public static async Task FocusWindowAtCursorAndWaitForActivationTask()
        {
            GetCursorPos(out POINT cursorPos);
            IntPtr hWnd = WindowFromPoint(cursorPos);

            if (hWnd != IntPtr.Zero && IsWindowVisible(hWnd))
            {
                SetForegroundWindow(hWnd);

                // Wait until it's the foreground window (up to 1500ms)
                for (int i = 0; i < 150; i++)
                {
                    if (GetForegroundWindow() == hWnd)
                        break;

                    await Task.Delay(10);
                }
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        struct POINT
        {
            public int X;
            public int Y;
        }

        //For transparency
        public const int GWL_EXSTYLE = -20;
        public const int WS_EX_TRANSPARENT = 0x00000020;
        public const int WS_EX_LAYERED = 0x00080000;

        // Device capability constants
        const int HORZSIZE = 4; // Physical width in millimeters
        const int VERTSIZE = 6; // Physical height in millimeters
        const int HORZRES = 8;  // Horizontal screen resolution in pixels
        const int VERTRES = 10; // Vertical screen resolution in pixels

        [DllImport("user32.dll")]
        static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        const uint SWP_NOSIZE = 0x0001;
        const uint SWP_NOMOVE = 0x0002;
        const uint SWP_NOACTIVATE = 0x0010;
        const uint SWP_SHOWWINDOW = 0x0040;

        public static void KeepOverlayOnTop(IntPtr hwnd)
        {
            SetWindowPos(hwnd, HWND_TOPMOST, 0, 0, 0, 0,
                SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE | SWP_SHOWWINDOW);
        }

        public static int GetWindowStyle(IntPtr hwnd)
        {
            return GetWindowLong(hwnd, GWL_EXSTYLE);
        }

        public static bool IsClickThroughEnabled(IntPtr hwnd)
        {
            int style = GetWindowLong(hwnd, GWL_EXSTYLE);
            return (style & WS_EX_TRANSPARENT) != 0;
        }

        public static void ToggleClickThrough(IntPtr hwnd, int baseStyle)
        {
            if (IsClickThroughEnabled(hwnd))
            {
                DisableClickThrough(hwnd, baseStyle);
            }
            else EnableClickThrough(hwnd, baseStyle);
        }

        public static void EnableClickThrough(IntPtr hwnd, int baseStyle)
        {
            int newStyle = baseStyle | WS_EX_TRANSPARENT;
            SetWindowLong(hwnd, GWL_EXSTYLE, newStyle);
        }

        public static async Task EnableClickThroughTask(IntPtr hwnd, int baseStyle, int timeoutMs = 500, int pollInterval = 10)
        {
            int newStyle = baseStyle | WS_EX_TRANSPARENT;
            SetWindowLong(hwnd, GWL_EXSTYLE, newStyle);

            int elapsed = 0;
            while (elapsed < timeoutMs)
            {
                int currentStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
                if ((currentStyle == newStyle))
                    return;

                await Task.Delay(pollInterval);
                elapsed += pollInterval;
            }

            // Optionally, log a warning or throw an exception if timeout reached
            Debug.WriteLine("Warning: Timed out waiting for click-through style to apply.");
        }

        public static void DisableClickThrough(IntPtr hwnd, int baseStyle)
        {
            int newStyle = baseStyle & ~WS_EX_TRANSPARENT;
            SetWindowLong(hwnd, GWL_EXSTYLE, newStyle);
        }

        public static Vector2 GetFullScreenSize()
        {
            // Get full screen dimensions (including taskbar area)
            int screenWidth = GetSystemMetrics(0);
            int screenHeight = GetSystemMetrics(1);
            return new Vector2(screenWidth, screenHeight);
        }


        public static Vector2 GetFullScreenSizeAdjustedForDPI(IntPtr hwnd)
        {
            // Get full screen dimensions (including taskbar area)
            int screenWidth = GetSystemMetrics(0);
            int screenHeight = GetSystemMetrics(1);

            // Get DPI scale factor
            float scale = GetScaleFactorForWindow(hwnd);

            // Adjust for DPI (optional if you want *logical* size)
            int logicalWidth = (int)(screenWidth / scale);
            int logicalHeight = (int)(screenHeight / scale);
            return new Vector2(logicalWidth, logicalHeight);
        }

        public static Vector2 GetMousePositionForInputSimulator()
        {

            // Get current mouse position
            GetCursorPos(out POINT pt);
            Vector2 screenSize = GetFullScreenSize();
            double normalizedX = pt.X * 65535.0 / screenSize.X;
            double normalizedY = pt.Y * 65535.0 / screenSize.Y;
            return new Vector2((float)normalizedX, (float)normalizedY);
        }

        public static Vector2 GetMousePositionForInputSimulator(IntPtr hwnd)
        {

            // Get current mouse position
            GetCursorPos(out POINT pt);
            Vector2 screenSize = GetFullScreenSizeAdjustedForDPI(hwnd);
            double normalizedX = pt.X * 65535.0 / screenSize.X;
            double normalizedY = pt.Y * 65535.0 / screenSize.Y;
            return new Vector2((float)normalizedX, (float)normalizedY);
        }

        public static void ShowCursor(bool visible)
        {
            if (visible)
            {
                ShowCursor();
            }
            else HideCursor();
        }

        public static void ShowCursor()
        {
            while (_ShowCursor(true) <= 0) { }          
        }

        public static void HideCursor()
        {
            while (_ShowCursor(false) >= 0) { }
        }

        static float GetScaleFactorForWindow(IntPtr hwnd)
        {
            uint dpi = GetDpiForWindow(hwnd);
            return dpi / 96.0f; // 96 is the default DPI (100%)
        }

        public static IntPtr GetHWNDfromWinUIWindow(Window window)
        {
            var hwnd = new WindowInteropHelper(window).Handle;

            return hwnd;
        }

        public static void ShowWindow(IntPtr windowPointer)
        {
            ShowWindow(windowPointer, 1);
            SetForegroundWindow(windowPointer);
        }

        public static void ShowWindowMaximized(IntPtr windowPointer)
        {
            ShowWindow(windowPointer, 3);
            SetForegroundWindow(windowPointer);
        }

        public static void MinimizeActiveWindow()
        {
            IntPtr windowPointer = GetActiveWindow();
            ShowWindow(windowPointer, 6);
        }

        public static void MaximizeActiveWindow()
        {
            IntPtr windowPointer = GetActiveWindow();
            ShowWindow(windowPointer, 3);
        }

        public static Vector2 GetDisplayResolutionInPixels()
        {
            // Get the device context of the screen
            IntPtr hdc = GetDC(IntPtr.Zero);

            // Get the screen resolution in pixels
            int screenWidthPixels = GetDeviceCaps(hdc, HORZRES);
            int screenHeightPixels = GetDeviceCaps(hdc, VERTRES);

            // Release the device context
            ReleaseDC(IntPtr.Zero, hdc);

            return new Vector2(screenWidthPixels, screenHeightPixels);
        }
        public static Vector2 GetPhysicalScreenSizeInInches()
        {
            // Get the device context of the screen
            IntPtr hdc = GetDC(IntPtr.Zero);

            // Get the physical width and height in millimeters
            int physicalWidthMm = GetDeviceCaps(hdc, HORZSIZE);
            int physicalHeightMm = GetDeviceCaps(hdc, VERTSIZE);

            // Get the screen resolution in pixels
            int screenWidthPixels = GetDeviceCaps(hdc, HORZRES);
            int screenHeightPixels = GetDeviceCaps(hdc, VERTRES);

            // Release the device context
            ReleaseDC(IntPtr.Zero, hdc);

            // Convert millimeters to inches
            float physicalWidthInches = physicalWidthMm / 25.4f;
            float physicalHeightInches = physicalHeightMm / 25.4f;

            return new Vector2(physicalWidthInches, physicalHeightInches);
        }

        public static void SetWindowCornerRadius(IntPtr hwnd, DWM_WINDOW_CORNER_PREFERENCE cornerPreference)
        {
            var attribute = DWMWINDOWATTRIBUTE.DWMWA_WINDOW_CORNER_PREFERENCE;
            var preference = (uint)cornerPreference;
            DwmSetWindowAttribute(hwnd, attribute, ref preference, sizeof(uint));
        }


        [Flags]
        public enum DWMWINDOWATTRIBUTE : uint
        {
            DWMWA_WINDOW_CORNER_PREFERENCE = 33,
            DWMWA_BORDER_COLOR,
            DWMWA_VISIBLE_FRAME_BORDER_THICKNESS
        }

        public enum DWM_WINDOW_CORNER_PREFERENCE
        {
            DWMWCP_DEFAULT = 0,
            DWMWCP_DONOTROUND = 1,
            DWMWCP_ROUND = 2,
            DWMWCP_ROUNDSMALL = 3
        }


        public static void MakeWindowTransparentOld(IntPtr hwnd)
        {
            int GWL_EXSTYLE = -20;
            int WS_EX_LAYERED = 0x80000;
            uint LWA_ALPHA = 0x2;

            // Make window layered and allow alpha transparency
            int exStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
            SetWindowLong(hwnd, GWL_EXSTYLE, exStyle | WS_EX_LAYERED);

            // Set full alpha (255 = opaque, 0 = invisible)
            SetLayeredWindowAttributes(hwnd, 0, 255, LWA_ALPHA);

            // Extend frame into client area to allow glass effects / full transparency
            var margins = new MARGINS
            {
                cxLeftWidth = -1,
                cxRightWidth = -1,
                cyTopHeight = -1,
                cyBottomHeight = -1
            };

            DwmExtendFrameIntoClientArea(hwnd, ref margins);
        }

        public static void MakeWindowTransparentOld2(IntPtr hwnd)
        {
            int GWL_EXSTYLE = -20;
            int WS_EX_LAYERED = 0x00080000;
            int WS_EX_TRANSPARENT = 0x00000020;
            int WS_EX_NOREDIRECTIONBITMAP = 0x00200000;

            // Get current extended window styles
            int exStyle = GetWindowLong(hwnd, GWL_EXSTYLE);

            // Add the desired flags
            exStyle |= WS_EX_LAYERED | WS_EX_TRANSPARENT | WS_EX_NOREDIRECTIONBITMAP;

            // Set the new styles
            SetWindowLong(hwnd, GWL_EXSTYLE, exStyle);
        }

        public static void MakeWindowTransparent(IntPtr hwnd)
        {
            int GWL_EXSTYLE = -20;
            int WS_EX_LAYERED = 0x00080000;
            int WS_EX_TRANSPARENT = 0x00000020;
            int WS_EX_NOREDIRECTIONBITMAP = 0x00200000;
            int WS_EX_TOOLWINDOW = 0x00000080;

            // Get current extended window styles
            int exStyle = GetWindowLong(hwnd, GWL_EXSTYLE);

            // Add the desired flags
            exStyle |= WS_EX_LAYERED | WS_EX_TRANSPARENT | WS_EX_NOREDIRECTIONBITMAP | WS_EX_TOOLWINDOW;

            // Set the new styles
            SetWindowLong(hwnd, GWL_EXSTYLE, exStyle);
        }

        public static void MakeDebugFriendlyTransparentWindow(IntPtr hwnd)
        {
            int GWL_EXSTYLE = -20;
            int WS_EX_LAYERED = 0x00080000;
            int WS_EX_TRANSPARENT = 0x00000020;
            int WS_EX_NOREDIRECTIONBITMAP = 0x00200000;
            nint HWND_NOTOPMOST = new IntPtr(-2);

            int exStyle = GetWindowLong(hwnd, GWL_EXSTYLE);

            // Omit TOOLWINDOW for visibility in taskbar, and avoid TOPMOST
            exStyle |= WS_EX_LAYERED | WS_EX_TRANSPARENT | WS_EX_NOREDIRECTIONBITMAP;

            SetWindowLong(hwnd, GWL_EXSTYLE, exStyle);

            // NOT setting HWND_TOPMOST — so it can go behind the IDE
            SetWindowPos(hwnd, HWND_NOTOPMOST, 0, 0, 0, 0,
                SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
        }
    }



    [StructLayout(LayoutKind.Sequential)]
    public struct MARGINS
    {
        public int cxLeftWidth;
        public int cxRightWidth;
        public int cyTopHeight;
        public int cyBottomHeight;
    }
}

public static class NativeKeys
{
    public const int VK_LBUTTON = 0x01;
    public const int VK_RBUTTON = 0x02;
    public const int VK_CANCEL = 0x03;
    public const int VK_MBUTTON = 0x04;

    public const int VK_BACK = 0x08;
    public const int VK_TAB = 0x09;

    public const int VK_RETURN = 0x0D;
    public const int VK_SHIFT = 0x10;
    public const int VK_CONTROL = 0x11;
    public const int VK_MENU = 0x12; // Alt
    public const int VK_PAUSE = 0x13;
    public const int VK_CAPITAL = 0x14;

    public const int VK_ESCAPE = 0x1B;
    public const int VK_SPACE = 0x20;
    public const int VK_PRIOR = 0x21; // Page Up
    public const int VK_NEXT = 0x22;  // Page Down
    public const int VK_END = 0x23;
    public const int VK_HOME = 0x24;
    public const int VK_LEFT = 0x25;
    public const int VK_UP = 0x26;
    public const int VK_RIGHT = 0x27;
    public const int VK_DOWN = 0x28;
    public const int VK_SELECT = 0x29;
    public const int VK_PRINT = 0x2A;
    public const int VK_EXECUTE = 0x2B;
    public const int VK_SNAPSHOT = 0x2C; // Print Screen
    public const int VK_INSERT = 0x2D;
    public const int VK_DELETE = 0x2E;
    public const int VK_HELP = 0x2F;

    // 0–9
    public const int VK_0 = 0x30;
    public const int VK_1 = 0x31;
    public const int VK_2 = 0x32;
    public const int VK_3 = 0x33;
    public const int VK_4 = 0x34;
    public const int VK_5 = 0x35;
    public const int VK_6 = 0x36;
    public const int VK_7 = 0x37;
    public const int VK_8 = 0x38;
    public const int VK_9 = 0x39;

    // A–Z
    public const int VK_A = 0x41;
    public const int VK_B = 0x42;
    public const int VK_C = 0x43;
    public const int VK_D = 0x44;
    public const int VK_E = 0x45;
    public const int VK_F = 0x46;
    public const int VK_G = 0x47;
    public const int VK_H = 0x48;
    public const int VK_I = 0x49;
    public const int VK_J = 0x4A;
    public const int VK_K = 0x4B;
    public const int VK_L = 0x4C;
    public const int VK_M = 0x4D;
    public const int VK_N = 0x4E;
    public const int VK_O = 0x4F;
    public const int VK_P = 0x50;
    public const int VK_Q = 0x51;
    public const int VK_R = 0x52;
    public const int VK_S = 0x53;
    public const int VK_T = 0x54;
    public const int VK_U = 0x55;
    public const int VK_V = 0x56;
    public const int VK_W = 0x57;
    public const int VK_X = 0x58;
    public const int VK_Y = 0x59;
    public const int VK_Z = 0x5A;

    // Function keys
    public const int VK_F1 = 0x70;
    public const int VK_F2 = 0x71;
    public const int VK_F3 = 0x72;
    public const int VK_F4 = 0x73;
    public const int VK_F5 = 0x74;
    public const int VK_F6 = 0x75;
    public const int VK_F7 = 0x76;
    public const int VK_F8 = 0x77;
    public const int VK_F9 = 0x78;
    public const int VK_F10 = 0x79;
    public const int VK_F11 = 0x7A;
    public const int VK_F12 = 0x7B;

    // Modifier keys (can be used as hotkey modifiers)
    public const int MOD_ALT = 0x0001;
    public const int MOD_CONTROL = 0x0002;
    public const int MOD_SHIFT = 0x0004;
    public const int MOD_WIN = 0x0008;

    //Windows Hotkey Process identifier
    public const uint WM_HOTKEY = 0x0312;
}

