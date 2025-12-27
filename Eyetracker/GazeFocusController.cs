using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace JoacoDesktop.Eyetracker
{
    public class GazeFocusController
    {
        public event Action? OnGazeEnter;
        public event Action? OnGazeExit;
        public event Action<float>? OnFocusUpdate01;
        public event Action? OnFocus;
        public event Action? OnUnfocus;

        float currentFocusTime;
        float decayDelayTimer;
        bool wasFocusedLastFrame;
        bool isTriggered;
        bool hasGaze;
        bool isPaused;
        float deltaTime;
        public float Focus01 => Math.Clamp(currentFocusTime / FocusTime, 0f, 1f);
        public bool HasFocus => currentFocusTime >= FocusTime;


        float _focusTime;
        public float FocusTime
        {
            get => _focusTime;
            set => _focusTime = Math.Max(.1f, value);
        }

        public float DecaySpeed { get; set; }

        public float DecayDelay { get; set; }

        public GazeFocusController()
        {
            StartUpdateLoop();
        }


        public void OnPointerEnter()
        {
            if (!hasGaze)
            {
                OnGazeEnter?.Invoke();
                Debug.WriteLine("On Gaze Enter called!");
            }
            hasGaze = true;
        }

        public void OnPointerExit()
        {
            if (hasGaze)
            {
                if (DecaySpeed == 0)
                {
                    currentFocusTime = 0;
                }
                decayDelayTimer = DecayDelay;
                OnGazeExit?.Invoke();
                Debug.WriteLine("On Gaze Exit called!");
            }
            hasGaze = false;
        }

        async void StartUpdateLoop()
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            long previousTime = stopwatch.ElapsedMilliseconds;

            while (true)
            {
                if (isPaused) return;

                await Task.Delay(16); // ~60 FPS
                long currentTime = stopwatch.ElapsedMilliseconds;
                deltaTime = (currentTime - previousTime) / 1000f;
                previousTime = currentTime;
                Update();
            }
        }


        void Update()
        {
            UpdateFocusValue();
            UpdateDecayDelayTimer();
            UpdateDecay();
            UnfocusEventCheck();
            OnFocusUpdate01?.Invoke(Focus01);
            wasFocusedLastFrame = HasFocus;
        }

        void UnfocusEventCheck()
        {
            if (wasFocusedLastFrame && !HasFocus)
            {
                OnUnfocus?.Invoke();
            }
        }

        void UpdateDecay()
        {
            if (!hasGaze && DecaySpeed > 0 && decayDelayTimer <= 0)
            {
                currentFocusTime -= DecaySpeed * deltaTime;
                currentFocusTime = Math.Max(currentFocusTime, 0);
            }
        }

        void UpdateDecayDelayTimer()
        {
            if (!hasGaze && decayDelayTimer > 0)
            {
                decayDelayTimer -= deltaTime;
            }
        }

        void UpdateFocusValue()
        {
            if (HasFocus) return;
            if (!hasGaze) return;

            currentFocusTime += deltaTime;
            currentFocusTime = Math.Clamp(currentFocusTime, 0f, FocusTime); // Clamp between 0 and 1

            if (currentFocusTime >= FocusTime)
            {
                Debug.WriteLine("Focused!");
                currentFocusTime = FocusTime;
                OnFocus?.Invoke();
                isTriggered = true;
            }
        }

        public void ResetFocus()
        {
            isTriggered = false;
            currentFocusTime = 0;
            wasFocusedLastFrame = false;
            OnFocusUpdate01?.Invoke(0);
        }
    }
}

