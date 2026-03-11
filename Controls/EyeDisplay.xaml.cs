using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Numerics;

namespace GazeStream.Controls
{
    public partial class EyeDisplay : System.Windows.Controls.UserControl
    {
        public EyeDisplay()
        {
            InitializeComponent();
            leftTimer = new Stopwatch();
            rightTimer = new Stopwatch();
        }

        // =====================
        // State
        // =====================

        private Vector2 leftEyePos;
        private Vector2 rightEyePos;
        private Vector2 leftLastPos;
        private Vector2 rightLastPos;

        private bool leftOpen;
        private bool rightOpen;

        private const double TIMEOUT_THRESHOLD = 0.3;

        private readonly Stopwatch leftTimer;
        private readonly Stopwatch rightTimer;

        private bool LeftTimeout => leftTimer.Elapsed.TotalSeconds > TIMEOUT_THRESHOLD;
        private bool RightTimeout => rightTimer.Elapsed.TotalSeconds > TIMEOUT_THRESHOLD;

        // =====================
        // Public API
        // =====================

        public void UpdateEyeDisplay(_7i_eye_data_ex_t eyes)
        {
            leftEyePos = new Vector2(
                eyes.left_pupil.pupil_center.x,
                eyes.left_pupil.pupil_center.y);

            rightEyePos = new Vector2(
                eyes.right_pupil.pupil_center.x,
                eyes.right_pupil.pupil_center.y);

            leftOpen = eyes.left_ex_data.blink == 0;
            rightOpen = eyes.right_ex_data.blink == 0;

            UpdateTimeoutTimer(eyes);

            LeftEyePoint.Visibility =
                (leftOpen && !LeftTimeout) ? Visibility.Visible : Visibility.Collapsed;

            RightEyePoint.Visibility =
                (rightOpen && !RightTimeout) ? Visibility.Visible : Visibility.Collapsed;

            if (eyes.left_pupil.pupil_center.y != 0)
            {
                SetElementToViewportPosition(LeftEyePoint, leftEyePos);
                leftLastPos = leftEyePos;
            }
            else
            {
                SetElementToViewportPosition(LeftEyePoint, leftLastPos);
            }

            if (eyes.right_pupil.pupil_center.y != 0)
            {
                SetElementToViewportPosition(RightEyePoint, rightEyePos);
                rightLastPos = rightEyePos;
            }
            else
            {
                SetElementToViewportPosition(RightEyePoint, rightLastPos);
            }
        }

        public void UpdateEyeDisplay(EyesData eyes)
        {
            if (eyes == null) return;

            leftEyePos = eyes.leftEye.GetViewportPos();
            rightEyePos = eyes.rightEye.GetViewportPos();

            leftOpen = eyes.leftEye.isBlinking;
            rightOpen = eyes.rightEye.isBlinking;

            UpdateTimeoutTimer(eyes);

            LeftEyePoint.Visibility = (leftOpen && !LeftTimeout) ? Visibility.Visible : Visibility.Collapsed;
            RightEyePoint.Visibility = (rightOpen && !RightTimeout) ? Visibility.Visible : Visibility.Collapsed;

            if (eyes.leftEye.viewportY != 0)
            {
                SetElementToViewportPosition(LeftEyePoint, leftEyePos);
                leftLastPos = leftEyePos;
            }
            else
            {
                SetElementToViewportPosition(LeftEyePoint, leftLastPos);
            }

            if (eyes.rightEye.viewportY != 0)
            {
                SetElementToViewportPosition(RightEyePoint, rightEyePos);
                rightLastPos = rightEyePos;
            }
            else
            {
                SetElementToViewportPosition(RightEyePoint, rightLastPos);
            }
        }

        public void ResetDisplay()
        {
            leftTimer.Reset();
            rightTimer.Reset();

            LeftEyePoint.Visibility = Visibility.Collapsed;
            RightEyePoint.Visibility = Visibility.Collapsed;
        }

        // =====================
        // Internals
        // =====================

        private void UpdateTimeoutTimer(_7i_eye_data_ex_t eyes)
        {
            UpdateTimer(leftTimer, eyes.left_pupil.pupil_center.y);
            UpdateTimer(rightTimer, eyes.right_pupil.pupil_center.y);
        }

        private void UpdateTimeoutTimer(EyesData eyes)
        {
            UpdateTimer(leftTimer, eyes.leftEye.viewportY);
            UpdateTimer(rightTimer, eyes.rightEye.viewportY);
        }

        private static void UpdateTimer(Stopwatch timer, float y)
        {
            if (y != 0)
            {
                timer.Reset();
            }
            else if (!timer.IsRunning)
            {
                timer.Start();
            }
        }

        // =====================
        // Canvas positioning helper
        // =====================

        private void SetElementToViewportPosition(FrameworkElement element, Vector2 viewportPos)
        {
            double x = viewportPos.X * DisplayCanvas.ActualWidth;
            double y = viewportPos.Y * DisplayCanvas.ActualHeight;

            Canvas.SetLeft(element, x - element.Width / 2);
            Canvas.SetTop(element, y - element.Height / 2);
        }
    }
}
