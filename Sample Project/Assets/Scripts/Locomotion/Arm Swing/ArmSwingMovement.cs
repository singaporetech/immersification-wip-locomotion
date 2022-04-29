//
// Copyright 2019-2022 Singapore Institute of Technology
//

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
    public HandInput leftHandInput;
    public HandInput RightHandInput;

    [Header("Speed settings")]
    // min speed
    public float minSpeed = 1;
    // max speed
    public float maxSpeed = 4;
    /// <summary>
    /// Rate at which the player will slowdown when hitting a deadzone
    /// </summary>
    public float timeTillMaxSpeed = .3f;
    public float accelerationMultiplier = 1;
    public float decelerationMultiplier = 1;
    /// <summary>
    /// The max possible speed based on the given inputs.
    /// </summary>
    float targetSpeed;
    /// <summary>
    /// The intended direction based on the given inputs.
    /// </summary>
    Vector3 targetDirection;

    [Header("Swing settings")]
    /// <summary>
    /// Input time between swings --> both arm swigns must be done within this set time to  count as vaild.
    /// </summary>
    public float timeToCompleteSet = 0.05f;
    /// <summary>
    /// min amount of input recieved by the HandInput to consider it as swing
    /// </summary>
    public int minSwingsForCompleteSet = 5;
    /// <summary>
    /// Represents the mininum valuve require for any movement to count as a valid swing.
    /// </summary>
    public float minSwingDistance = 0.3f;
    /// <summary>
    /// max swing value to calculate speed
    /// </summary>
    public float maxSwingDistance = 1.6f;
    /// <summary>
    /// error to clear swing set
    /// </summary>
    public int maxErrors = 3;

    //Countdown timer for timeToCompleteSwingSet
    float SwingCompleteSetTimer = 0f;
    float SpeedTimer = 0;

    [Header("Deadzone settings")]
    /// <summary>
    /// Player needs to move "fast" enough above this value so any movement is detected,
    /// </summary>
    public float movementDeadZone = .01f;

    /// <summary>
    /// How many frames the movement will continue to move without input from the left and right HandInput.cs.
    /// </summary>
    public int PassiveWalkableFrameCount = 5;
    float passiveWalkCount = 0;

    /// <summary>
    /// Used to indicate if data was been recieved from the the respective hand HandInput.cs
    /// </summary>
    HandInputData leftHand, rightHand;

    /// <summary>
    /// Records if the current frame has detected a valid input from either HandInput.cs.
    /// </summary>
    bool frameHasActiveInput;

    /// <summary>
    /// Previously recorded targetSpeed
    /// </summary>
    float lastActiveSpeed;

    /// <summary>
    /// Previously recorded targetDirection
    /// </summary>
    Vector3 lastActiveDir;

    /// <summary>
    /// Set up
    /// </summary>
    private void Start()
    {
        type = e_InputType.e_InputTypeArm;
        InitInputData();
    }

    /// <summary>
    /// Resets the input data for recording of swings.
    /// </summary>
    public void InitInputData()
    {
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
            InitInputData();

        SwingCompleteSetTimer += Time.deltaTime;

        //For when the player has sent 2 sets of active inputs
        if (frameHasActiveInput)
        {
            //passiveWalkCount += 1;
            //if (UseTimedAcceleration)
            SpeedTimer += Time.deltaTime * accelerationMultiplier;
            UpdateMovementActive();

            //Reset everything
            InitInputData();
            frameHasActiveInput = false;
        }
        // For when no set of valid inputs are present
        else if (!frameHasActiveInput)
        {
            // At least one hand is swinging but input(s) are not valid to count as a full "active" swing
            // passiveWalkLerpTimer < passiveWalkRate will allow the user to "walk passively" based
            // on the frame required for valid inputs

            if (!InactiveHandsCheck() && passiveWalkCount < PassiveWalkableFrameCount)
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

    /// <summary>
    /// Updates targetSpeed to the last achived speed via lastActiveSpeed.
    /// </summary>
    private void UpdateStoppingMovement()
    {
        targetSpeed = lastActiveSpeed;
    }

    /// <summary>
    /// Updates frameHasActive.
    /// </summary>
    public void DetectActiveInput()
    {
        frameHasActiveInput = (leftHand.inputTaken && rightHand.inputTaken) && (leftHand.isForwardSwing ^ rightHand.isForwardSwing) ? true : false;
    }

    /// <summary>
    /// Sets moveSpeed via targetSpeed, and SpeedTimer/timeTillMaxSpeed.
    /// </summary>
    public void SetMoveSpeed()
    {
        moveSpeed = targetSpeed * (SpeedTimer / timeTillMaxSpeed);
    }

    /// <summary>
    /// Sets moveDirection via targetDirection.
    /// </summary>
    public void SetMoveDirection()
    {
        moveDirection = targetDirection;
    }

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

    /// <summary>
    /// Calculates assumed direction based on direction input from leftHand and rightHand.
    /// </summary>
    /// <returns>Movement direction</returns>
    private Vector3 calculateSwingDirection()
    {
        Vector3 moveDir = leftHand.isForwardSwing ? (leftHand.direction.normalized - rightHand.direction.normalized) :
                                                    (rightHand.direction.normalized - leftHand.direction.normalized);
        moveDir.y = 0;
        return moveDir;
    }

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
        DetectActiveInput();
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

        // Try and set the current update movement type to active(input driven) update, or static (non-input driven) update
        DetectActiveInput();
    }

   /// <summary>
   /// Checks if either hand is inactive.
   /// </summary>
   /// <returns>true if both hands are inactive. False if at least one is active. </returns>
    bool InactiveHandsCheck()
    {
        if (leftHandInput.GetIsDead() && RightHandInput.GetIsDead())
        {
            return true;
        }
        else
        {
            return false;
        }
    }
}

/// <summary>
/// Houses the recieved data from a HandInput.cs
/// </summary>
public struct HandInputData
{
    public Vector3 direction;

    public bool isForwardSwing;
    public bool inputTaken;

}