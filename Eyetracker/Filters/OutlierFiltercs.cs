using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System;
using System.Numerics;

namespace GazeStream.Eyetracker.Filters
{
    [Serializable]
    public class OutlierFilter
    {
        private Vector2 previousMeasurement;
        private Vector2 previousPreviousMeasurement;
        private Vector2 lastTrustedPoint;

        private int measurementCount;

        /// <summary>
        /// Maximum deviation from the predicted trajectory
        /// before a measurement is considered an outlier.
        /// Coordinates are expected to be normalized viewport coordinates (0..1).
        /// </summary>
        public float Threshold { get; set; } = 0.05f;

        public Vector2 LastTrustedPoint => lastTrustedPoint;

        public Vector2 Update(Vector2 measurement)
        {
            // First measurement: always trust it.
            if (measurementCount == 0)
            {
                lastTrustedPoint = measurement;
                previousPreviousMeasurement = measurement;
                previousMeasurement = measurement;
                measurementCount = 1;

                return lastTrustedPoint;
            }

            // Second measurement: always trust it.
            if (measurementCount == 1)
            {
                lastTrustedPoint = measurement;
                previousPreviousMeasurement = previousMeasurement;
                previousMeasurement = measurement;
                measurementCount = 2;

                return lastTrustedPoint;
            }

            // Predict where the current measurement should be
            // based on the previous movement.
            Vector2 velocity =
                previousMeasurement - previousPreviousMeasurement;

            Vector2 predicted =
                previousMeasurement + velocity;

            float predictionError =
                Vector2.Distance(measurement, predicted);

            if (predictionError <= Threshold)
            {
                // Current measurement confirms the trajectory.
                lastTrustedPoint = previousMeasurement;
            }

            // Advance history regardless of whether the measurement
            // was trusted. This lets us recognize that a suspicious
            // point was actually an isolated spike.
            previousPreviousMeasurement = previousMeasurement;
            previousMeasurement = measurement;

            return lastTrustedPoint;
        }

        public void Reset()
        {
            previousMeasurement = default;
            previousPreviousMeasurement = default;
            lastTrustedPoint = default;
            measurementCount = 0;
        }
    }
}
