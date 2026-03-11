using System;
using System.Numerics;
using GazeStream.Utilities;

namespace GazeStream.Eyetracker
{
    public struct GazePoint
    {
        public Vector2 viewportPoint;
        public long timeInTicks;

        public Vector2 screenPoint => Helper.ViewportToScreenVector2(viewportPoint);

        public GazePoint(Vector2 viewportSpacePoint)
        {
            viewportPoint = viewportSpacePoint;
            timeInTicks = DateTime.Now.Ticks;
        }

        public bool IsValid
        {
            get { return !float.IsNaN(viewportPoint.X) && !float.IsNaN(viewportPoint.Y); }

        }

        public static GazePoint Invalid
        {
            get
            {
                return new GazePoint(new Vector2(float.NaN, float.NaN));
            }
        }
    }
}
