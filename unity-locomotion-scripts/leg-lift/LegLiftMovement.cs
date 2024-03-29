﻿//
// Copyright 2019-2022 Singapore Institute of Technology
//

using UnityEngine;

/// <summary>
/// Leg Lift Input Manager to process input from the leg input
/// and send the speed and direction to the movement manager
/// PAPER : LT1 LLCM WIP 
/// </summary>
public class LegLiftMovement : InputManager
{
    [Header("Speed to scale")]
    public float speedScale = 1.0f;

    [Header("Left Leg, Right Leg, chest trackers")]
    public GameObject leftLeg;
    public GameObject rightLeg;
    public GameObject chest;
    private LegInput leftLegTracker, rightLegTracker;

    [Header("Camera")]
    public GameObject vrCamera;

    private float objectiveSpeed = 0f;

    public float lerp_t = 0.01f;

    /// <summary>
    /// Initialize function from Unity
    /// </summary>
    void Start()
    {
        leftLegTracker = leftLeg.GetComponent<LegInput>();
        rightLegTracker = rightLeg.GetComponent<LegInput>();

        moveDirection = vrCamera.transform.forward;
        type = e_InputType.e_InputTypeLeg;
    }

    /// <summary>
    /// Update speed
    /// add both left and righ leg speed and send it to the movement manager
    /// </summary>
    void Update()
    {
        float leftLegVel = leftLegTracker.finalLiftSpeed;

        float rightLegVel = rightLegTracker.finalLiftSpeed;

        float sum = leftLegVel + rightLegVel;

        objectiveSpeed = sum * speedScale;

        //Added in check to only set a valid move speed if HMD is on
        moveSpeed = Mathf.Lerp(moveSpeed, objectiveSpeed, lerp_t * (checkStaticLegs() ? 10f : 1f) * Time.deltaTime);

        moveDirection = new Vector3(chest.transform.forward.x, 0, chest.transform.forward.z);
    }

    void FixedUpdate()
    {
        // send to the movement manager
        SendDirection();
        SendMagnitude();
    }

    /// <summary>
    /// Checks if legs are moving. Used to alter acceleration depending on leg movement
    /// </summary>
    /// <returns>true if legs are moving, false if not</returns>
    bool checkStaticLegs()
    {
        return (leftLegTracker.GetDead()) && (rightLegTracker.GetDead());
    }
}