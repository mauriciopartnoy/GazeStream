using System.Numerics;

namespace GazeStream.Eyetracker.Filters
{
    [Serializable]
    public class KalmanFilter
    {
        private Vector2 estimatedPoint = Vector2.Zero;     //The point used after applying the filter.
        private float estimationNoise = 1;   //This value fluctuates to balance each new measurment with the previous estimate.
        private float gain;              //Scale from 0% to 100% applied to the movement delta when updating the Estimate
        private Vector2 measurement1;
        private Vector2 measurement2;
        private float measurementNoise = 2;

        public int SmoothFactor = 1;
        public bool SmoothWhenChangingGazeTarget;

        public KalmanFilter()
        {
            estimatedPoint = new Vector2(0, 0);
            estimationNoise = 1;
            measurementNoise = 2;
        }

        public void SetSmoothFactor(int smoothLevel)
        {
            SmoothFactor = smoothLevel;
            ResetStartingValues();
        }

        private void ResetStartingValues()
        {
            estimatedPoint = Vector2.Zero;
            estimationNoise = 1f;
            measurementNoise = 2f;
            gain = default;
            measurement1 = default;
            measurement2 = default;
        }


        public Vector2 GetFilteredPoint(Vector2 measurement)
        {
            if (SmoothFactor <= 0) return measurement;
            // Prediction - process model is "we haven't moved" but with some uncertainty
            // The uncertainty increases with distance of new data from current estimate - if within a fixations-distance
            // we have a narrow prior to enforce smoothness. If far away, we want a uniform prior over all positions. 
            // The exponential process noise captures this smoothly

            // == INPUT VARIABLES: this drive behavior of our model == //
            var smoothingFactor = SmoothFactor * 0.001f + .5f; //El valor .5f está agregado porque a partir de ahi empieza a funcionar el filtro. 

            // this dictates how quickly process noise scales up across all deltas
            // higher = extend smoothness for longer saccades
            float processScale = 10.0f * smoothingFactor - 5.0f;

            // this realigns the curve to ensure we always scale up smoothly from delta = 0 to max
            float curveOffset = -1.0f * smoothingFactor;

            //A weighted average of the last 3 measurements will reduce the recoil inherent with large movements
            if (SmoothWhenChangingGazeTarget)
            {
                measurement = new Vector2(
                    measurement.X * 0.45f + measurement1.X * 0.3f + measurement2.X * 0.25f,
                    measurement.Y * 0.45f + measurement1.Y * 0.3f + measurement2.Y * 0.25f);

                measurement2 = measurement1;
                measurement1 = measurement;
            }

            // == PROCESS MODEL: this encodes all our desired behavior wrt smoothness == //
            float delta = (measurement - estimatedPoint).Length();
            double currentProcessNoise = Math.Exp((delta + processScale * curveOffset) / processScale) - Math.Exp(curveOffset);

            // == KALMAN FILTER:  Standard update equations from the KF framework - these shouldn't be changed == //
            estimationNoise += (float)currentProcessNoise;
            estimationNoise = (float)Math.Min(currentProcessNoise, 1); // Prevent saturation to Inf

            gain = estimationNoise / (estimationNoise + measurementNoise);
            estimationNoise = (1.0f - gain) * estimationNoise;
            estimatedPoint += (measurement - estimatedPoint) * gain;

            float clampedX = Math.Clamp(estimatedPoint.X, 0, 1f);
            float clampedY = Math.Clamp(estimatedPoint.Y, 0, 1f);
            estimatedPoint.X = clampedX;
            estimatedPoint.Y = clampedY;

            return estimatedPoint;
        }
    }
}