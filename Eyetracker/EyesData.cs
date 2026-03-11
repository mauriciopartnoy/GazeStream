using System.Numerics;
[Serializable]
public class EyesData 
{
    public Eye leftEye = default;
    public Eye rightEye = default;

    public EyesData()
    {
        leftEye = default;
        rightEye = default;
    }
    public EyesData(_7i_eye_data_ex_t eyesData)
    {
        leftEye = new();
        leftEye.viewportX = eyesData.left_pupil.pupil_center.x;
        leftEye.viewportY = eyesData.left_pupil.pupil_center.y;
        leftEye.pupilDiameter = eyesData.left_pupil.pupil_diameter;
        leftEye.pupilDiameterMm = eyesData.left_pupil.pupil_diameter_mm;
        leftEye.pupilDistanceMm = eyesData.left_pupil.pupil_distance;
        leftEye.isBlinking = eyesData.left_ex_data.blink == 0 ? false : true;

        rightEye = new();
        rightEye.viewportX = eyesData.right_pupil.pupil_center.x;
        rightEye.viewportY = eyesData.right_pupil.pupil_center.y;
        rightEye.pupilDiameter = eyesData.right_pupil.pupil_diameter;
        rightEye.pupilDiameterMm = eyesData.right_pupil.pupil_diameter_mm;
        rightEye.pupilDistanceMm = eyesData.right_pupil.pupil_distance;
        rightEye.isBlinking = eyesData.right_ex_data.blink == 0 ? false : true;

    }

    public void UpdateFromInvensun(_7i_eye_data_ex_t eyesData)
    {
        leftEye.viewportX = eyesData.left_pupil.pupil_center.x;
        leftEye.viewportY = eyesData.left_pupil.pupil_center.y;
        leftEye.pupilDiameter = eyesData.left_pupil.pupil_diameter;
        leftEye.pupilDiameterMm = eyesData.left_pupil.pupil_diameter_mm;
        leftEye.pupilDistanceMm = eyesData.left_pupil.pupil_distance;
        leftEye.isBlinking = eyesData.left_ex_data.blink == 0 ? false : true;

        rightEye.viewportX = eyesData.right_pupil.pupil_center.x;
        rightEye.viewportY = eyesData.right_pupil.pupil_center.y;
        rightEye.pupilDiameter = eyesData.right_pupil.pupil_diameter;
        rightEye.pupilDiameterMm = eyesData.right_pupil.pupil_diameter_mm;
        rightEye.pupilDistanceMm = eyesData.right_pupil.pupil_distance;
        rightEye.isBlinking = eyesData.right_ex_data.blink == 0 ? false : true;
    }

}

[Serializable]
public class Eye
{
    public bool isBlinking;
    public float viewportX;
    public float viewportY;
    public float pupilDistanceMm;
    public float pupilDiameterMm;
    public float pupilDiameter;
    //public float openness; //No está implementado todavía en el SDK

    public Vector2 GetViewportPos()
    {
        return new Vector2(viewportX, viewportY);
    }
}
