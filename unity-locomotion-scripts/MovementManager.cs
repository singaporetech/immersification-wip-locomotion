using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

/// <summary>
/// Type of movement input from user
/// </summary>
public enum e_InputType
{
    e_InputTypeController,
    e_InputTypeHead,
    e_InputTypeArm,
    e_InputTypeLeg,
    e_InputTypeArmHead,
    e_InputTypeArmLeg,
    e_InputTypeHeadLeg,
    e_InputTypeArmLegHead,
    e_InputTypeArm2
}

/// <summary>
/// Manager for movement.
/// The manager takes in the main player controller, and takes in input
/// from ArmSwing, HeadBob or LegLift.
/// It will then take the direction and magnitude from the inputs and
/// apply it to the rigidbody
/// </summary>
public class MovementManager : MonoBehaviour
{
    // for moving the character
    Vector3 moveDirection;
    float moveSpeed;

    // components
    Rigidbody rb;

    [Header("Type of Input")]
    public e_InputType magnitudeInputType;
    public e_InputType directionInputType;
    public int numberOfRequiredTrackers = 2;

    [Header("Trackers/Controller")]
    public GameObject LeftHand;
    public GameObject RightHand;
    public GameObject LeftFoot;
    public GameObject RightFoot;

    // slope detection manager
    [HideInInspector]
    public SlopeDetection slopeInput;

    // to store the inputs to check within the restTime to ensure
    // that all the inputs are taken into account
    Dictionary<e_InputType, float> movementInputDict =
                                        new Dictionary<e_InputType, float>();
    // number of input required at a single itme
    int numOfInput;

    // timer to reset the checking
    float timer = 0;
    float resetTime = 0.3f;
    bool startReset = false;

    public TrackerDeviceAllocator trackerManager;

    /// <summary>
    /// Initialize function from Unity
    /// </summary>
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        slopeInput = GetComponent<SlopeDetection>();
        UpdateMovementType();
    }

    /// <summary>
    /// Update function from unity
    /// check for input
    /// after that check if time has reached
    /// if so clear the dictionary to reset checking
    /// </summary>
    void Update()
    {
        UpdateInput();

        if (startReset)
        {
            timer += Time.deltaTime;
            if (timer > resetTime)
            {
                timer = 0;
                startReset = false;
                movementInputDict.Clear();
            }
        }
    }

    /// <summary>
    /// Move user using velocity
    /// by multiplying speed with direction and a slope modifier
    /// </summary>
    void Move()
    {
        Vector3 vec = (moveSpeed * moveDirection * (slopeInput.effortModiftier));
        vec.y += rb.velocity.y;
        rb.velocity = vec;
    }

    /// <summary>
    /// Receive direction from input manager
    /// </summary>
    /// <param name="Dir">Direction to move towards</param>
    public void ReceiveDirection(Vector3 Dir)
    {
        moveDirection = Dir;
    }

    public Vector3 GetMoveDirection()
    {
        return moveDirection;
    }

    /// <summary>
    /// Receive speed from input manager and call move to move the player
    /// get the type of movement and update it into the dictionary
    /// when the dictionary reach the num of input, move
    /// </summary>
    /// <param name="InputType">Type of movement</param>
    /// <param name="Mag">Speed to move</param>
    public void ReceiveMagnitude(e_InputType InputType, float Mag)
    {
        if (movementInputDict.Count == 0)
        {
            startReset = true;
        }
        movementInputDict[InputType] = Mag;

        if (movementInputDict.Count == numOfInput)
        {
            foreach (var obj in movementInputDict)
            {
                moveSpeed += obj.Value;
            }
            Move();
            movementInputDict.Clear();
            moveSpeed = 0;
            startReset = false;
            timer = 0;
        }
    }

    /// <summary>
    /// Keyboard input for easy switching.
    /// Both Numpad and default number keys works.
    /// 0: Controller
    /// 1: ArmSwing
    /// 2: HeadBob
    /// 3: LegLift
    /// 4: ArmSwing + LegLift
    /// 5: ArmSwing + HeadBob
    /// 6: HeadBob + LegLift
    /// 7: ArmSwing + HeadBob + LegLift
    /// 8: Wheelchair(To be added when working and tested)
    /// </summary>
    void UpdateInput()
    {
        if (Input.GetKeyUp(KeyCode.Alpha0) || Input.GetKeyUp(KeyCode.Keypad0))
        {
            magnitudeInputType = directionInputType
                                = e_InputType.e_InputTypeController;
            UpdateMovementType();
        }
        if (Input.GetKeyUp(KeyCode.Alpha1) || Input.GetKeyUp(KeyCode.Keypad1))
        {
            magnitudeInputType = directionInputType
                                = e_InputType.e_InputTypeArm;
            UpdateMovementType();
        }
        if (Input.GetKeyUp(KeyCode.Alpha2) || Input.GetKeyUp(KeyCode.Keypad2))
        {
            magnitudeInputType = directionInputType
                                = e_InputType.e_InputTypeHead;
            UpdateMovementType();
        }
        if (Input.GetKeyUp(KeyCode.Alpha3) || Input.GetKeyUp(KeyCode.Keypad3))
        {
            magnitudeInputType = directionInputType
                                = e_InputType.e_InputTypeLeg;
            UpdateMovementType();
        }
        if (Input.GetKeyUp(KeyCode.Alpha4) || Input.GetKeyUp(KeyCode.Keypad4))
        {
            magnitudeInputType = directionInputType
                                = e_InputType.e_InputTypeArmLeg;
            UpdateMovementType();
        }
        if (Input.GetKeyUp(KeyCode.Alpha5) || Input.GetKeyUp(KeyCode.Keypad5))
        {
            magnitudeInputType = directionInputType
                                = e_InputType.e_InputTypeArmHead;
            UpdateMovementType();
        }
        if (Input.GetKeyUp(KeyCode.Alpha6) || Input.GetKeyUp(KeyCode.Keypad6))
        {
            magnitudeInputType = directionInputType
                                = e_InputType.e_InputTypeHeadLeg;
            UpdateMovementType();
        }
        if (Input.GetKeyUp(KeyCode.Alpha7) || Input.GetKeyUp(KeyCode.Keypad7))
        {
            magnitudeInputType = directionInputType
                                = e_InputType.e_InputTypeArmLegHead;
            UpdateMovementType();
        }
        if(Input.GetKeyUp(KeyCode.R))
        {
            trackerManager.enabled = true;
        }
    }

    /// <summary>
    /// Update which input to use based on movement type
    /// Disable the other movement as and when neccessary
    /// Enable the movement need and set the speed, direction and init
    /// speedPercentage is 0.0f to 1.0f
    /// directionPercentage is 0.0f or 1.0f
    /// </summary>
    void UpdateMovementType()
    {
        switch (magnitudeInputType)
        {
            case e_InputType.e_InputTypeController:
                GetComponent<HeadBobMovement>().enabled = false;
                GetComponent<LegLiftMovement>().enabled = false;
                GetComponent<ArmSwingMovement>().enabled = false;

                GetComponent<ControllerMovement>().enabled = true;
                GetComponent<ControllerMovement>().speedPercentage = 1;
                GetComponent<ControllerMovement>().directionPercentage = 1;

                LeftHand.GetComponent<HandInput>().enabled = false;
                RightHand.GetComponent<HandInput>().enabled = false;
                LeftFoot.GetComponent<LegInput>().enabled = false;
                RightFoot.GetComponent<LegInput>().enabled = false;

                numOfInput = 1;
                break;

            case e_InputType.e_InputTypeArm:
                GetComponent<ControllerMovement>().enabled = false;
                GetComponent<HeadBobMovement>().enabled = false;
                GetComponent<LegLiftMovement>().enabled = false;

                GetComponent<ArmSwingMovement>().enabled = true;
                GetComponent<ArmSwingMovement>().speedPercentage = 1;
                GetComponent<ArmSwingMovement>().directionPercentage = 1;

                LeftHand.GetComponent<HandInput>().enabled = true;
                RightHand.GetComponent<HandInput>().enabled = true;
                LeftFoot.GetComponent<LegInput>().enabled = false;
                RightFoot.GetComponent<LegInput>().enabled = false;


                numOfInput = 1;
                break;

            case e_InputType.e_InputTypeHead:
                GetComponent<ControllerMovement>().enabled = false;
                GetComponent<ArmSwingMovement>().enabled = false;
                GetComponent<LegLiftMovement>().enabled = false;

                GetComponent<HeadBobMovement>().enabled = true;
                GetComponent<HeadBobMovement>().speedPercentage = 1;
                GetComponent<HeadBobMovement>().directionPercentage = 1;
                GetComponent<HeadBobMovement>().Init();

                LeftHand.GetComponent<HandInput>().enabled = false;
                RightHand.GetComponent<HandInput>().enabled = false;
                LeftFoot.GetComponent<LegInput>().enabled = false;
                RightFoot.GetComponent<LegInput>().enabled = false;

                numOfInput = 1;
                break;

            case e_InputType.e_InputTypeLeg:
                GetComponent<ControllerMovement>().enabled = false;
                GetComponent<ArmSwingMovement>().enabled = false;
                GetComponent<HeadBobMovement>().enabled = false;

                GetComponent<LegLiftMovement>().enabled = true;
                GetComponent<LegLiftMovement>().speedPercentage = 1;
                GetComponent<LegLiftMovement>().directionPercentage = 1;

                LeftHand.GetComponent<HandInput>().enabled = false;
                RightHand.GetComponent<HandInput>().enabled = false;
                LeftFoot.GetComponent<LegInput>().enabled = true;
                RightFoot.GetComponent<LegInput>().enabled = true;

                numOfInput = 1;
                break;

            case e_InputType.e_InputTypeArmHead:
                GetComponent<ControllerMovement>().enabled = false;
                GetComponent<LegLiftMovement>().enabled = false;

                GetComponent<ArmSwingMovement>().enabled = true;
                GetComponent<ArmSwingMovement>().speedPercentage = 0.2f;
                GetComponent<ArmSwingMovement>().directionPercentage = 1;

                GetComponent<HeadBobMovement>().enabled = true;
                GetComponent<HeadBobMovement>().speedPercentage = 0.8f;
                GetComponent<HeadBobMovement>().directionPercentage = 0;
                GetComponent<HeadBobMovement>().Init();

                LeftHand.GetComponent<HandInput>().enabled = true;
                RightHand.GetComponent<HandInput>().enabled = true;
                LeftFoot.GetComponent<LegInput>().enabled = false;
                RightFoot.GetComponent<LegInput>().enabled = false;

                numOfInput = 2;
                break;

            case e_InputType.e_InputTypeArmLeg:
                GetComponent<ControllerMovement>().enabled = false;
                GetComponent<HeadBobMovement>().enabled = false;
                GetComponent<ArmSwingMovement>().enabled = true;
                GetComponent<ArmSwingMovement>().speedPercentage = 0.5f;
                GetComponent<ArmSwingMovement>().directionPercentage = 1;

                GetComponent<LegLiftMovement>().enabled = true;
                GetComponent<LegLiftMovement>().speedPercentage = 0.5f;
                GetComponent<LegLiftMovement>().directionPercentage = 0;

                LeftHand.GetComponent<HandInput>().enabled = true;
                RightHand.GetComponent<HandInput>().enabled = true;
                LeftFoot.GetComponent<LegInput>().enabled = true;
                RightFoot.GetComponent<LegInput>().enabled = true;

                numOfInput = 2;
                break;

            case e_InputType.e_InputTypeHeadLeg:
                GetComponent<ControllerMovement>().enabled = false;
                GetComponent<ArmSwingMovement>().enabled = false;

                GetComponent<HeadBobMovement>().enabled = true;
                GetComponent<HeadBobMovement>().speedPercentage = 0.8f;
                GetComponent<HeadBobMovement>().directionPercentage = 1;
                GetComponent<HeadBobMovement>().Init();

                GetComponent<LegLiftMovement>().enabled = true;
                GetComponent<LegLiftMovement>().speedPercentage = 0.2f;
                GetComponent<LegLiftMovement>().directionPercentage = 0;

                LeftHand.GetComponent<HandInput>().enabled = false;
                RightHand.GetComponent<HandInput>().enabled = false;
                LeftFoot.GetComponent<LegInput>().enabled = true;
                RightFoot.GetComponent<LegInput>().enabled = true;

                numOfInput = 2;
                break;

            case e_InputType.e_InputTypeArmLegHead:
                GetComponent<ControllerMovement>().enabled = false;
                GetComponent<ArmSwingMovement>().enabled = true;
                GetComponent<ArmSwingMovement>().speedPercentage = 0.3f;
                GetComponent<ArmSwingMovement>().directionPercentage = 1;

                GetComponent<HeadBobMovement>().enabled = true;
                GetComponent<HeadBobMovement>().speedPercentage = 0.5f;
                GetComponent<HeadBobMovement>().directionPercentage = 0;
                GetComponent<HeadBobMovement>().Init();

                GetComponent<LegLiftMovement>().enabled = true;
                GetComponent<LegLiftMovement>().speedPercentage = 0.2f;
                GetComponent<LegLiftMovement>().directionPercentage = 0;

                LeftHand.GetComponent<HandInput>().enabled = true;
                RightHand.GetComponent<HandInput>().enabled = true;
                LeftFoot.GetComponent<LegInput>().enabled = true;
                RightFoot.GetComponent<LegInput>().enabled = true;

                numOfInput = 3;
                break;

            default:
                break;

        }
    }
}
