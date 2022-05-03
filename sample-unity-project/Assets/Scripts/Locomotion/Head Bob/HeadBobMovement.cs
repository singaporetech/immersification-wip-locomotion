//
// Copyright 2019-2022 Singapore Institute of Technology
//

using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Head Bob Input Manager to process input from the HMD
/// and send the speed and direction to the movement manager
/// PAPER : HB2 Walk In Place Sensors 
/// </summary>
public class HeadBobMovement : InputManager
{
    [Header("Components")]
    public GameObject vrCamera;

    // values to determine central axis
    private readonly float upRatio = 0.06f;
    private readonly float downRatio = 0.13f;
    // spacing between the middle axis to determine min and max range.
    public float spacingOffset = 2f;

    /// <summary>
    /// The initial starting Y position of the vrCamera.
    /// </summary>
    private float camIniitalYPos = 0;

    /// <summary>
    /// The vrCamera rotation in euler angles.
    /// </summary>
    private Vector3 camRot;

    // Speed and speed multiplier variables.
    public float multiplier = 1;
    float targetSpeed;
    public float accelerationMultiplier = 1;
    public float decelerationMultiplier = 1;
    float SpeedTimer;
    public float maxSpeed = 2.5f;
    public float timeTillMaxSpeed = .3f;


    [Header("Data to calculate step")]
    // size of filter for moving average
    private readonly int filterSize = 4;
    // size of queue to check if step is made
    private readonly int queueSize = 5;

    // epsilon for checks
    /// <summary>
    /// Min peak to ensure minimum step
    /// </summary>
    public float minPeak = 0.05f;
    /// <summary>
    /// Max peak to ensure maximum step
    /// </summary>
    public float maxPeak = .4f;

    /// <summary>
    /// Temporary list of frame input data.
    /// </summary>
    List<float> tempInputList = new List<float>();

    /// <summary>
    /// List of filtered input data.
    /// </summary>
    Queue<float> filteredData = new Queue<float>();

    /// <summary>
    /// Initialize function from Unity
    /// </summary>
    void Start()
    {
        type = e_InputType.e_InputTypeHead;
    }

    /// <summary>
    /// Init function to run when enabling this movement from the movement manager
    /// </summary>
    public void Init()
    {
        camIniitalYPos = vrCamera.transform.localPosition.y;
        SetMoveDirection();
    }

    /// <summary>
    /// Update function from unity
    /// Handles detection and recording of inputs.
    /// </summary>
    void Update()
    {
        SetMoveDirection();

        //Number of frames is enough to make a valid set to update movment
        GetHeadFrameInput();
        if (tempInputList.Count >= filterSize)
        {
            float inputsStrength = 0;
            foreach (float obj in tempInputList)
                inputsStrength += obj;

            // run step recognition
            AddSetToList(inputsStrength);

            tempInputList.Clear();
        }

        SpeedTimer = Mathf.Clamp(SpeedTimer, 0, timeTillMaxSpeed);
        SetMoveSpeed();
    }

    /// <summary>
    /// FixedUpdate function from unity
    /// Handles sending the speed and direction values to the movement manager.
    /// </summary>
    private void FixedUpdate()
    {
        //Send direction and speed to the movement manager
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

        range.x = camIniitalYPos + adValue + spacingOffset;
        range.y = camIniitalYPos + adValue - spacingOffset;

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
    
    /// <summary>
    /// Run Recognition Algorithm to find velocity of step
    /// First, ensure headbob is in range
    /// then, do a moving average filter to filter noise
    /// finally, do step recognition on the filtered value
    /// </summary>
    public void GetHeadFrameInput()
    {
        Vector2 range = ReturnBobbingRange();
        float camY = vrCamera.transform.localPosition.y;

        // check if value is between the range to ensure
        // that the movement isnt too big or too small
        // x = high | y = low
        if (camY <= range.x && camY >= range.y)
        {
            // moving average filter
            tempInputList.Add(camY);
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
        camRot = vrCamera.transform.localRotation.eulerAngles;
        if (camRot.x > 180)
        {
            camRot.x -= 360;
        }
        camRot.x = -camRot.x;

        moveDirection = Vector3.Normalize(new Vector3(vrCamera.transform.forward.x, 0, vrCamera.transform.forward.z));
    }

    /// <summary>
    /// Returns the largest float in an array
    /// </summary>
    /// <param name="arr">Array containing floats</param>
    /// <returns></returns>
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

    /// <summary>
    /// Returns the smallest float in an array
    /// </summary>
    /// <param name="arr">Array containing floats</param>
    /// <returns></returns>
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

    /// <summary>
    /// Sets the targetSpeed viable according to the s parameter
    /// </summary>
    /// <param name="s">The speed to be set.</param>
    void SetTargetSpeed(float s)
    {
        targetSpeed = Mathf.Clamp(s, 0, maxSpeed);
    }
}