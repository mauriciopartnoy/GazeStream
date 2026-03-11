using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using GazeStream.Utilities;
using GazeStream.Eyetracker;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Collections.Concurrent;
namespace GazeStream.Windows
{
    public partial class EyesWindow : Window
    {
        public static EyesWindow I { get; private set; } //Singleton Instance
        bool isShowingCamera;
        CancellationTokenSource cts;
        Task updateTask;

        public static ASeeTracker.imageCallback imageCB = new ASeeTracker.imageCallback(image_callback);
        private ConcurrentQueue<byte[]> queue;

        WriteableBitmap _bitmap;
        private int _imgWidth = 0;
        private int _imgHeight = 0;
        GCHandle handle;
        IntPtr ptr;

        public void SetImageSize(int width, int height)
        {
            if (_bitmap == null || _imgWidth != width || _imgHeight != height)
            {
                _imgWidth = width;
                _imgHeight = height;

                _bitmap = new WriteableBitmap(
                    width,
                    height,
                    96,
                    96,
                    PixelFormats.Gray8,   // ← likely correct for your data
                    null);

                CameraDisplay.Source = _bitmap;
            }
        }
        public void Push(byte[] buffer)
        {
            if (_bitmap == null)
                return;

            Dispatcher.BeginInvoke(new Action(() =>
            {
                _bitmap.WritePixels(
                    new Int32Rect(0, 0, _imgWidth, _imgHeight),
                    buffer,
                    _imgWidth,   // stride for Gray8
                    0);
            }));
        }


        public EyesWindow()
        {
            InitializeComponent();
            I = this;
            Loaded += OnLoaded;
            Closed += OnClosed;
            Helper.ForceTopmost(this);
        }



        void OnLoaded(object sender, RoutedEventArgs e)
        {
            SetEyeDisplayView();
            GazeManager.OnGazeUpdateUI += UpdateGazeDisplay;
            handle = GCHandle.Alloc(this);
            ptr = GCHandle.ToIntPtr(handle);
            ASeeTracker._7i_set_image_callback(Marshal.GetFunctionPointerForDelegate(imageCB), ptr);
        }

        void OnClosed(object sender, EventArgs e)
        {
            GazeManager.OnGazeUpdateUI -= UpdateGazeDisplay;
            ASeeTracker._7i_set_image_callback(IntPtr.Zero, IntPtr.Zero);
            handle.Free();
            //TODO: Cleanup
        }

        public static void image_callback(int eye, IntPtr image, int size, int width, int height, Int64 timestamp, IntPtr context)
        {
            if (context == IntPtr.Zero) return;
            var window = (EyesWindow)GCHandle.FromIntPtr(context).Target;
            if (!window.isShowingCamera) return;

            int w = (width / 4) * 4;
            int h = (height / 4) * 4;

            byte[] buffer = new byte[w * h];

            for (int i = 0; i < h; ++i)
            {
                Marshal.Copy(image + (i * width), buffer, i * w, w);
            }

            window.Dispatcher.BeginInvoke(new Action(() =>
            {
                window.SetImageSize(w, h);
                window.Push(buffer);
            }));
        }

        void Button_Salir(object e, EventArgs args)
        {
            this.Close();
        }

        void Button_CambiarVista(object e, EventArgs args)
        {
            if (isShowingCamera)
            {
                SetEyeDisplayView();
            }
            else
            {
                SetCameraView();
            }
        }

        void SetCameraView()
        {
            isShowingCamera = true;
            EyeDisplay.Visibility = Visibility.Collapsed;
            CameraDisplay.Visibility = Visibility.Visible;
        }

        void SetEyeDisplayView()
        {
            isShowingCamera = false;
            EyeDisplay.Visibility = Visibility.Visible;
            CameraDisplay.Visibility = Visibility.Collapsed;
        }

        void UpdateGazeDisplay()
        {
            if (GazeManager.I.GazeDevice == null) return;
            if (GazeManager.I.GazeDevice.Eyes == null) return;
            if (EyeDisplay.Visibility == Visibility.Collapsed) return;
            Debug.WriteLine("Trying to update display!");
            EyeDisplay.UpdateEyeDisplay(GazeDeviceA11.eyesData);
        }

    }
}
