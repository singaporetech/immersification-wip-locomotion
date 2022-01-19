//
// Copyright 2019-2022 Singapore Institute of Technology
//

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// To get as input for left and right controller
/// </summary>
public enum e_Hand { e_HandLeft, e_HandRight };

/// <summary>
/// Arm Swing Input Manager to process input from the controllers
/// and send the speed and direction to the movement manager
/// </summary>
public class ArmSwingMovement : InputManager
{
    [Header("Components")]
    public HandInput LeftHand;
    public HandInput RightHand;

    [Header("Speed settings")]
    // min speed
    public float minSpeed = 1;
    // max speed
    public float maxSpeed = 4;
    //public float speedMultiplier = 1;
    /// <summary>
    /// Rate at which the player will slowdown when controllers hits a deadzone
    /// </summary>
    public float timeTillMaxSpeed = .3f;
    public float accelerationMultiplier = 1;
    public float decelerationMultiplier = 1;

    [Header("Swing settings")]
    // input time between swings --> both arm swigns must be done within this set time to  count as vaild.
    public float timeToCompleteSet = 0.05f;
    // min amount to consider it as swing
    public int minSwingsForCompleteSet = 5;
    public int PassiveWalkableFrameCount = 5;
    /// <summary>
    /// Represents the mininum valuve require for any movement to count as a valid swing.
    /// </summary>
    public float minSwingDistance = 0.3f;
    // max swing value to calculate speed
    public float maxSwingDistance = 1.6f;
    // error to clear swing set
    public int maxErrors = 3;

    [Header("Deadzone settings")]
    /// <summary>
    /// Player needs to move "fast" enough above this value so any movement is detected,
    /// </summary>
    public float movementDeadZone = .01f;

    Rigidbody rb;
    HandInputData leftHand, rightHand;

    float targetSpeed;
    Vector3 targetDirection;

    bool frameIsActiveInput;
    float lastActiveSpeed;
    Vector3 lastActiveDir;

    //Countdown timer for timeToCompleteSwingSet
    float SwingCompleteSetTimer = 0f;
    float SpeedTimer = 0;
    float passiveWalkCount = 0;

    /// <summary>
    /// Set up
    /// </summary>
    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        type = e_InputType.e_InputTypeArm;

        InitializeInputData();
    }

    public void InitializeInputData()
    {
        //accelerationTimer = 0;

        SwingCompleteSetTimer = 0;
        leftHand.direction = Vector3.zero;
        leftHand.isForwardSwing = false;
        leftHand.inputTaken = false;

        rightHand.direction = Vector3.zero;
        rightHand.isForwardSwing = false;
        rightHand.inputTaken = false;
    }

    /// <summary>
    /// To be use as a timer between the left and right swing
    /// to consider if use for moving forward
    /// </summary>
    private void Update()
    {
        //Timer to reset swing
        //If player does not swing within the time allocated, the timer will reset the entire motion
        //and all valid actions are erased.
        if (SwingCompleteSetTimer >= timeToCompleteSet)
            InitializeInputData();
        SwingCompleteSetTimer += Time.deltaTime;

        //For when the player has sent 2 sets of active inputs
        if (frameIsActiveInput)
        {
            //passiveWalkCount += 1;
            //if (UseTimedAcceleration)
            SpeedTimer += Time.deltaTime * accelerationMultiplier;
            UpdateMovementActive();

            //Reset everything
            InitializeInputData();
            frameIsActiveInput = false;
            //Debug.Log("I am active. ");
        }
        // For when no set of valid inputs are present
        else if (!frameIsActiveInput)
        {
            // At least one hand is swinging but input(s) are not valid to count as a full "active" swing
            // passiveWalkLerpTimer < passiveWalkRate will allow the user to "walk passively" based
            // on the frame required for valid inputs

            if (!CheckEitherIsDead() && passiveWalkCount < PassiveWalkableFrameCount)
            {
                passiveWalkCount += 1;
                SpeedTimer += Time.deltaTime * accelerationMultiplier;

                UpdateMovementPassive();
            }
            else
            {
                SpeedTimer -= Time.deltaTime * decelerationMultiplier;

                UpdateStoppingMovement();
            }
        }

        SpeedTimer = Mathf.Clamp(SpeedTimer, 0, timeTillMaxSpeed);
        SetMoveSpeed();
        SetMoveDirection();
    }

    private void FixedUpdate()
    {
        // send to the movement manager
        SendDirection();
        SendMagnitude();
    }

    // =============== Update movement speed functions ===============

    /// <summary>
    /// if one of the swing is forward and one is backwards
    /// calculate speed
    /// </summary>
    private void UpdateMovementActive()
    {
        passiveWalkCount = 0;
        
        // calculating speed
        lastActiveSpeed = targetSpeed = CalculateSwingSpeed((leftHand.direction.magnitude + rightHand.direction.magnitude) / 2f);
        lastActiveDir = targetDirection = calculateSwingDirection();
    }

    /// <summary>
    /// Pasive movement, will graduatelly slow down
    /// </summary>
    private void UpdateMovementPassive()
    {
        // calculating speed
        targetSpeed = lastActiveSpeed;
        targetDirection = lastActiveDir;
    }

    private void UpdateStoppingMovement()
    {
        targetSpeed = lastActiveSpeed;
    }

    public void SetUpdateMovementType()
    {
        frameIsActiveInput = (leftHand.inputTaken && rightHand.inputTaken) && (leftHand.isForwardSwing ^ rightHand.isForwardSwing) ? true : false;
    }

    public void SetMoveSpeed()
    {
        moveSpeed = targetSpeed * (SpeedTimer / timeTillMaxSpeed);
    }

    public void SetMoveDirection()
    {
        moveDirection = targetDirection;
    }

    // =============== Calculation functions ===============

    /// <summary>
    /// Converting magnitude of vector into a speed vector
    /// </summary>
    /// <param name="Mag">  Magnitude of swing</param>
    /// <returns>Converted speed from swing magnitude</returns>
    private float CalculateSwingSpeed(float Mag)
    {
        if (Mag > maxSwingDistance)
        {
            Mag = maxSwingDistance;
        }

        float a = (Mag - minSwingDistance) / (maxSwingDistance - minSwingDistance) * (maxSpeed - minSpeed) + minSpeed;

        if (float.IsNaN(a))
        {
            a = 0;
        }

        return a;
    }

    private Vector3 calculateSwingDirection()
    {
        Vector3 moveDir = leftHand.isForwardSwing ? (leftHand.direction.normalized - rightHand.direction.normalized) :
                                                    (rightHand.direction.normalized - leftHand.direction.normalized);

        // removing the y vector
        moveDir.y = 0;
        return moveDir;
    }

    // ================= Messenger and getter functions ==================    

    /// <summary>
    /// Take in the swing data from the left controller
    /// </summary>
    /// <param name="Dir">The direction vector</param>
    /// <param name="Forward"> if vector is moving along the camera</param>
    public void ReceiveLeftInput(Vector3 Dir, bool Forward)
    {
        SwingCompleteSetTimer = 0;

        leftHand.direction = Dir;
        leftHand.isForwardSwing = Forward;
        leftHand.inputTaken = true;

        //Try and set the current update movement type to active (input driven) update, or static(non-input driven) update
        SetUpdateMovementType();
    }

    /// <summary>
    /// Take in the swing data from the right controller
    /// </summary>
    /// <param name="Dir">The direction vector</param>
    /// <param name="Forward">if vector is moving along the camera</param>
    public void ReceiveRightInput(Vector3 Dir, bool Forward)
    {
        SwingCompleteSetTimer = 0;

        rightHand.direction = Dir;
        rightHand.isForwardSwing = Forward;
        rightHand.inputTaken = true;

        //Try and set the current update movement type to active (input driven) update, or static(non-input driven) update
        SetUpdateMovementType();
    }

    //see if player not swinging arms
    bool CheckEitherIsDead()
    {
        if (LeftHand.GetIsDead() && RightHand.GetIsDead())
        {
            return true;
        }
        else
        {
            return false;
        }
    }
}

public struct HandInputData
{
    public Vector3 direction;

    public bool isForwardSwing;
    public bool inputTaken;

}