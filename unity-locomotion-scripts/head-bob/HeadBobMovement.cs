using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Valve.VR;

/// <summary>
/// Head Bob Input Manager to process input from the HMD
/// and send the speed and direction to the movement manager
/// PAPER : HB2 Walk In Place Sensors 
/// </summary>
public class HeadBobMovement : InputManager
{
    [Header("Settings")]
    public GameObject camera;

    float iniitalYPos = 1.6f;


    // values to determine central axis
    private readonly float upRatio = 0.06f;
    private readonly float downRatio = 0.13f;
    // spacing between the middle axis to determine min and max range.
    public float spacingOffset = 2f;

    private Vector3 camPos;
    private Vector3 camRot;
    //Data to determine the range

    public float multiplier = 1;
    public float maxSpeed = 2.5f;
    public float timeTillMaxSpeed = .3f;
    float targetSpeed;

    public float accelerationMultiplier;
    public float decelerationMultiplier;
    float SpeedTimer;

    // to check the peaks
    private float currentTime = 0.0f;
    private bool updateTimeBetweenPeaks = false;


    [Header("Data to calculate step")]
    // size of filter for moving average
    private readonly int filterSize = 4;
    // size of queue to check if step is made
    private readonly int queueSize = 5;
    // min time to make a step
    private readonly float stepMinTime = 0.2f;
    // max time to make a step
    private readonly float stepMaxTime = 2.0f;

    // epsilon for checks
    // min peak to ensure minimum step
    public float minPeak = 0.05f;
    // max peak to ensure maximum step
    public float maxPeak = .4f;




    // data sets
    List<float> tempList = new List<float>();
    Queue<float> filteredData = new Queue<float>();

    /// <summary>
    /// Initialize function from Unity
    /// </summary>
    void Start()
    {
        type = e_InputType.e_InputTypeHead;
    }

    /// <summary>
    /// Init function to run when switching this one from the movement manager
    /// </summary>
    public void Init()
    {
        iniitalYPos = camera.transform.localPosition.y;
        SetMoveDirection();
    }

    /// <summary>
    /// Update function from unity
    /// </summary>
    void Update()
    {
        SetMoveDirection();

        //Number of frames is enough to make a valid set to update movment
        GetHeadFrameInput();
        if (tempList.Count >= filterSize)
        {
            float inputsStrength = 0;
            foreach (float obj in tempList)
                inputsStrength += obj;
            // run step recognition
            AddSetToList(inputsStrength);

            tempList.Clear();
        }

        SpeedTimer = Mathf.Clamp(SpeedTimer, 0, timeTillMaxSpeed);
        SetMoveSpeed();
    }

    private void FixedUpdate()
    {
        // send direction and speed to the movement manage/r
        SendDirection();
        SendMagnitude();

    }

    /// <summary>
    /// Calculate the range using the central axis and spacing. X is biggest Y is lowest.
    /// </summary>
    /// <returns>The range which determine that it is "a headbob"</returns>
    Vector2 ReturnBobbingRange()
    {
        Vector2 range;
        //for slight adjustments if the player is looking up or down
        float adValue = CalculateDegreeAdjustmentValue();

        range.x = iniitalYPos + adValue + spacingOffset;
        range.y = iniitalYPos + adValue - spacingOffset;

        return range;
    }

    /// <summary>
    /// Calculate Central Axis H to determine range
    /// </summary>
    /// <returns>The central axis.</returns>
    float CalculateDegreeAdjustmentValue()
    {
        float heightithink = 0.0f;

        if (camRot.x >= 0 && camRot.x < 90)
        {
            heightithink = upRatio * Mathf.Sin(camRot.x * Mathf.Deg2Rad);
        }
        else if (camRot.x > -90 && camRot.x < 0)
        {
            heightithink = downRatio * Mathf.Sin(camRot.x * Mathf.Deg2Rad);
        }

        return heightithink;
    }

    // ================ Update methods ================
    
    /// <summary>
    /// Run Recognition Algorithm to find velocity of step
    /// First, ensure headbob is in range
    /// then, do a moving average filter to filter noise
    /// finally, do step recognition on the filtered value
    /// </summary>
    public void GetHeadFrameInput()
    {
        Vector2 range = ReturnBobbingRange();
        float camY = camera.transform.localPosition.y;

        // check if value is between the range to ensure
        // that the movement isnt too big or too small
        // x = high | y = low
        if (camY <= range.x && camY >= range.y)
        {
            // moving average filter
            tempList.Add(camY);
        }
    }

    /// <summary>
    /// Recognise the step and calculate Velocity
    /// if middle of the data is the smallest, user just did a step
    /// get the diff from the min and max peaks of the graph
    /// ensure that it is between the min and max peak and
    /// that not a long time have passed
    /// run calculation of velocity using the difference
    /// </summary>
    /// <param name="RFiltered">Data that is Filtered .</param>
    void AddSetToList(float RFiltered)
    {
        // add to the queue of data
        filteredData.Enqueue(RFiltered);

        // if hit max size determined
        if (filteredData.Count == queueSize)
        {
            float[] filteredDataArray = filteredData.ToArray();

            float largestStep = ReturnLargest(filteredDataArray);
            float smallestStep = ReturnSmallest(filteredDataArray);

            // get the diff in the min and max peak
            float disStep = largestStep - smallestStep;

            // If player is moving head and is within the required threshold
            if ((disStep >= minPeak && disStep <= maxPeak))
            {
                SpeedTimer +=Time.deltaTime * accelerationMultiplier;

                SetTargetSpeed(disStep * multiplier);
            }

            // If player is not moving head or is within the deadzone or out of range
            else
            {
                SpeedTimer -= Time.deltaTime * decelerationMultiplier;
            }

            // pop the first data
            filteredData.Dequeue();
        }
    }
    
    /// <summary>
    /// Calculate Movement speed from head bobs
    /// by adding a min speed the calculation
    /// </summary>
    /// <param name="DiffInStep">Diff between top and bottom peaks</param>
    /// <param name="SetZero">To set the velocity to 0 when stopping</param>
    void SetMoveSpeed()
    {
        moveSpeed = targetSpeed * (SpeedTimer/ timeTillMaxSpeed);
    }
    
    /// <summary>
    /// Update the camera values to get 0..180 degrees
    /// </summary>
    void SetMoveDirection()
    {
        camRot = camera.transform.localRotation.eulerAngles;
        if (camRot.x > 180)
        {
            camRot.x -= 360;
        }
        camRot.x = -camRot.x;

        //moveDirection = camera.transform.forward;
        moveDirection = Vector3.Normalize(new Vector3(camera.transform.forward.x, 0, camera.transform.forward.z));
    }
    // ================ Calculations ================

    float ReturnLargest(float[] arr)
    {
        float i = arr[0];

        foreach (float ele in arr)
        {
            if (ele >= i)
            {
                float get = ele;
                i = get;
            }
        }
        return i;
    }

    float ReturnSmallest(float[] arr)
    {
        float i = arr[0];

        foreach (float ele in arr)
        {
            if (ele <= i)
            {
                float get = ele;
                i = get;
            }
        }
        return i;
    }

    void SetTargetSpeed(float s)
    {
        targetSpeed = Mathf.Clamp(s, 0, maxSpeed);
    }
}