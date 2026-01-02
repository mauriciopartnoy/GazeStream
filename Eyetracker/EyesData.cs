using UnityEngine;
using System;

[Serializable]
public class EyesData 
{
    public Eye leftEye = default;
    public Eye rightEye = default;
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
