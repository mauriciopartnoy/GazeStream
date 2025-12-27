namespace GazeStream.Eyetracker
{
    public class GazeSettings
    {
        public byte[] calibrationBuff = new byte[0];
        public int smooth = 10;
        public int calibrationPointsOption = 2; //0:3, 1:5, 2:9
        public int eyes = 0; //0:Ambos, 1:Izq, 2:Der
        float pointerSizeMultiplier = 1;
        float focusTimeMultiplier = 1;
    }
}
