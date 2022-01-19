//
// Copyright 2019-2022 Singapore Institute of Technology
//

using UnityEngine;

// <summary>
/// Get the swing input from the leg.
/// This will prompt leglift movement
/// PAPER : LT1 LLCM WIP 
/// </summary>
public class LegInput : MonoBehaviour
{
    // previous position of tracker
    private float lastPosUp;

    [HideInInspector]
    public float finalLiftSpeed;

    [Header("Values for scaling speed")]
    // value to offset
    public float offsetValue = 0.01f;
    // min and max speed filter noise
    public float minAbsSpeed = 0.0001f;
    public float maxAbsSpeed = 0.7f;

    public bool leftLeg = false;

    public float deadZone = 0.1f;

    private bool isDead = false;

    //to eliminate step too high excessive movement
    public float maxHeight = 0.4f;

    /// <summary>
    /// Initialize function from Unity
    /// </summary>
    void Start()
    {
        lastPosUp = transform.localPosition.y;
    }

    /// <summary>
    /// Update function and calculate speed
    /// </summary>
    void Update()
    {
        finalLiftSpeed = 0;
        float velUp = AbsGetUpSpeed();

        float smoothedVel = velUp;

        if(leftLeg)
            smoothedVel = ButterworthL.FilterLoop(velUp);
        else
            smoothedVel = ButterworthR.FilterLoop(velUp);

        finalLiftSpeed = OffSet(smoothedVel);
    }

    /// <summary>
    /// Get the abosulte magnitude of vertical velocity by taking position 
    /// divided by time. Can consider using rigidbody
    /// </summary>
    /// <returns>The absolute magnitude of the vertical velocity.</returns>
    float AbsGetUpSpeed()
    {
        float y = (transform.localPosition.y > maxHeight) ? maxHeight : transform.localPosition.y;

        // numeric differentiate to get instantaneous velocity and absolute it
        float absSpeed = Mathf.Abs(
                        (y - lastPosUp) / Time.fixedDeltaTime);

        lastPosUp = y;

        isDead = absSpeed < deadZone;

        if (absSpeed > maxAbsSpeed) return maxAbsSpeed;
        if (absSpeed < minAbsSpeed) return 0;
        
        return absSpeed;
    }

    /// <summary>
    /// Offset the smoothed speed to reduce too high values
    /// </summary>
    /// <param name="SmoothedVel">the smoothed speed</param>
    /// <returns>the offseted result</returns>
    float OffSet(float SmoothedVel)
    {
        float result = SmoothedVel - offsetValue;

        return result < 0 ? 0 : result;
    }

    public bool GetDead()
    {
        return isDead;
    }
}

/// <summary>
/// Butterworth filter class for left leg
/// pass in a value in the filter loop to get filtered through 
/// a 4th order low pass butterworth filter
/// converted C code from the following generated class:
/// http://www-users.cs.york.ac.uk/~fisher/mkfilter/
/// Butterworth / Bessel / Chebyshev
/// Parameters:
/// Filter Type  : Butterworth, lowpass
/// Filter order : 4
/// Sample Rate  : 60
/// Corner Rate  : 5 
/// </summary>
static public class ButterworthL
{
    static private readonly int NZeroes = 4;
    static private readonly int NPoles = 4;
    static private readonly float Gain = 3.881333017e+02f;

    static readonly float[] xv = new float[NZeroes + 1];
    static readonly float[] yv = new float[NPoles + 1];

    static public void Start()
    {
        for (int i = 0; i < NZeroes + 1; i++)
            xv[i] = 0;

        for (int i = 0; i < NPoles + 1; i++)
            yv[i] = 0;
    }

    /// <summary>
    /// FIlter the value through the butterworth filter
    /// </summary>
    /// <param name="Input">value to filter</param>
    /// <returns>the filtered value</returns>
    static public float FilterLoop(float Input)
    {
        xv[0] = xv[1]; xv[1] = xv[2]; xv[2] = xv[3]; xv[3] = xv[4];
        xv[4] = Input / Gain;
        yv[0] = yv[1]; yv[1] = yv[2]; yv[2] = yv[3]; yv[3] = yv[4];
        yv[4] = (xv[0] + xv[4]) + 4 * (xv[1] + xv[3]) + 6 * xv[2]
                     + (-0.2498216698f * yv[0]) + (1.3392807613f * yv[1])
                     + (-2.7693097862f * yv[2]) + (2.6386277439f * yv[3]);
        return yv[4];
    }
}

/// <summary>
/// Butterworth filter class for Right leg
/// pass in a value in the filter loop to get filtered through 
/// a 4th order low pass butterworth filter
/// converted C code from the following generated class:
/// http://www-users.cs.york.ac.uk/~fisher/mkfilter/
/// Butterworth / Bessel / Chebyshev
/// Parameters:
/// Filter Type  : Butterworth, lowpass
/// Filter order : 4
/// Sample Rate  : 60
/// Corner Rate  : 5 
/// </summary>
static public class ButterworthR
{
    static private readonly int NZeroes = 4;
    static private readonly int NPoles = 4;
    static private readonly float Gain = 3.881333017e+02f;

    static readonly float[] xv = new float[NZeroes + 1];
    static readonly float[] yv = new float[NPoles + 1];

    static public void Start()
    {
        for (int i = 0; i < NZeroes + 1; i++)
            xv[i] = 0;

        for (int i = 0; i < NPoles + 1; i++)
            yv[i] = 0;
    }

    /// <summary>
    /// FIlter the value through the butterworth filter
    /// </summary>
    /// <param name="Input">value to filter</param>
    /// <returns>the filtered value</returns>
    static public float FilterLoop(float Input)
    {
        xv[0] = xv[1]; xv[1] = xv[2]; xv[2] = xv[3]; xv[3] = xv[4];
        xv[4] = Input / Gain;
        yv[0] = yv[1]; yv[1] = yv[2]; yv[2] = yv[3]; yv[3] = yv[4];
        yv[4] = (xv[0] + xv[4]) + 4 * (xv[1] + xv[3]) + 6 * xv[2]
                     + (-0.2498216698f * yv[0]) + (1.3392807613f * yv[1])
                     + (-2.7693097862f * yv[2]) + (2.6386277439f * yv[3]);
        return yv[4];
    }
}