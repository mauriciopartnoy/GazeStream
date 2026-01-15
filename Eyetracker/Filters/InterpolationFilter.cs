using System.Numerics;
using GazeStream.Utilities;
namespace GazeStream.Eyetracker.Filters
{
    [Serializable]
    public class InterpolationFilter
    {
        float val;
        public float Value { get { return val; } set { float val = Math.Clamp(value, 0f, 1f); } }


        Vector2 historicPoint;
        bool hasHistoricPoint;

        public Vector2 GetFilteredPoint(Vector2 point)
        {
            if (Value <= 0) return point;
            if (!hasHistoricPoint)
            {
                historicPoint = point;
                hasHistoricPoint = true;
            }
            Vector2 result = Helper.Lerp(historicPoint, point, val);
            historicPoint = result;
            return result;
        }
    }
}