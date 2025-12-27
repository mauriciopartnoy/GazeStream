using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

namespace GazeStream.Utilities
{
    internal static class Helper
    {
        public static float Lerp(float a, float b, float t)
        {
            return a + (b - a) * t;
        }

        public static float LerpClamped(float a, float b, float t)
        {
            t = Math.Clamp(t, 0f, 1f);
            return a + (b - a) * t;
        }

        public static float InverseLerp(float a, float b, float value)
        {
            if (a == b) return 0f; // Avoid divide by zero
            return (value - a) / (b - a);
        }

        public static Vector2 Lerp(Vector2 a, Vector2 b, float value)
        {
            float x = Lerp(a.X, b.X, value);
            float y = Lerp(a.Y, b.Y, value);
            return new Vector2(x, y);
        }

        public static Vector2 LerpClamped(Vector2 a, Vector2 b, float value)
        {
            float x = LerpClamped(a.X, b.X, value);
            float y = LerpClamped(a.Y, b.Y, value);
            return new Vector2(x, y);
        }

        public static Vector2 ScreenPointToViewport(Vector2 screenSize, Vector2 screenPoint)
        {
            float x = InverseLerp(0, screenSize.X, screenPoint.X);
            float y = InverseLerp(0, screenSize.Y, screenPoint.Y);
            return new Vector2(x, y);
        }

        public static Vector2 ViewportToScreenPoint(Vector2 screenSize, Vector2 viewportPoint)
        {
            float x = Lerp(0, screenSize.X, viewportPoint.X);
            float y = Lerp(0, screenSize.Y, viewportPoint.Y);
            return new Vector2(x, y);
        }

        public static Vector2 ScreenPointToViewport(Vector2 screenPoint)
        {
            Vector2 screenSize = GetPrimaryMonitorSize();
            float x = InverseLerp(0, screenSize.X, screenPoint.X);
            float y = InverseLerp(0, screenSize.Y, screenPoint.Y);
            return new Vector2(x, y);
        }

        public static Vector2 ViewportToScreenPoint(Vector2 viewportPoint)
        {
            Vector2 screenSize = GetPrimaryMonitorSize();
            float x = Lerp(0, screenSize.X, viewportPoint.X);
            float y = Lerp(0, screenSize.Y, viewportPoint.Y);
            return new Vector2(x, y);
        }

        public static System.Windows.Point ViewportToScreenPixels(Window window, Vector2 viewport)
        {
            // 1) Viewport → WPF DIPs
            var dips = new System.Windows.Point(
                window.ActualWidth * viewport.X,
                window.ActualHeight * viewport.Y
            );

            // 2) DIPs → physical pixels
            return DipsToPixels(window, dips);
        }

        public static Vector2 ViewportToUIElementPoint(FrameworkElement element, Vector2 viewportPoint)
        {
            float x = Lerp(0f, (float)element.ActualWidth, viewportPoint.X);
            float y = Lerp(0f, (float)element.ActualHeight, viewportPoint.Y);
            return new Vector2(x, y);
        }

        public static System.Windows.Point ViewportToWindowDips(Window window, Vector2 viewport)
        {
            return new System.Windows.Point(
                window.ActualWidth * viewport.X,
                window.ActualHeight * viewport.Y
            );
        }

        public static (double X, double Y) ViewportToMousePosition(Vector2 viewportPos)
        {
            double absX = Math.Clamp(viewportPos.X, 0.0, 1.0) * 65535.0;
            double absY = Math.Clamp(viewportPos.Y, 0.0, 1.0) * 65535.0;
            return (absX, absY);
        }

        public static (double ScaleX, double ScaleY) GetDpiScale(Window window)
        {
            var source = PresentationSource.FromVisual(window);
            if (source?.CompositionTarget == null)
                return (1.0, 1.0);

            var transform = source.CompositionTarget.TransformToDevice;
            return (transform.M11, transform.M22);
        }

        public static System.Windows.Point DipsToPixels(Window window, System.Windows.Point dips)
        {
            var (scaleX, scaleY) = GetDpiScale(window);
            return new System.Windows.Point(dips.X * scaleX, dips.Y * scaleY);
        }

        public static System.Windows.Point PixelsToDips(Window window, System.Windows.Point pixels)
        {
            var (scaleX, scaleY) = GetDpiScale(window);
            return new System.Windows.Point(pixels.X / scaleX,pixels.Y / scaleY);
        }


        public static Vector2 GetCurrentWindowSize(Window window)
        {
            return new Vector2((float)window.ActualWidth, (float)window.ActualHeight);
        }

        public static Vector2 GetScreenSize(Window window)
        {
            var hwnd = new WindowInteropHelper(window).Handle;
            var screen = Screen.FromHandle(hwnd);

            return new Vector2(
                screen.Bounds.Width,
                screen.Bounds.Height
            );
        }

        public static Vector2 GetPrimaryMonitorSize()
        {
            var bounds = Screen.PrimaryScreen.Bounds;
            return new Vector2(bounds.Width, bounds.Height);
        }
    }
}
