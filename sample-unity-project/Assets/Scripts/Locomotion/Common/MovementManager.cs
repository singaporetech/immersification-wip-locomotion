//
// Copyright 2019-2022 Singapore Institute of Technology
//

using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Type of movement input from user
/// </summary>
public enum e_InputType
{
    e_InputTypeHead,
    e_InputTypeArm,
    e_InputTypeLeg,
    e_InputTypeFullBody,
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
    /// <summary>
    /// The calculated direction from the selected locomotion inputs.
    /// </summary>
    Vector3 moveDirection;

    /// <summary>
    /// The calculated speed from the selected locomotion inputs.
    /// </summary>
    float moveSpeed;

    /// <summary>
    /// The Rigidbody component of the VR User.
    /// </summary>
    Rigidbody rb;

    [Header("Type of Input")]
    /// <summary>
    /// The input type to be used. Change this to swap between head, arm, leg, or full body locomotion controls.
    /// </summary>
    public e_InputType magnitudeInputType;

    /// <summary>
    /// To store the inputs to check within the restTime to ensure
    /// that all the inputs are taken into account
    /// </summary>
    Dictionary<e_InputType, float> movementInputDict = new Dictionary<e_InputType, float>();

    /// <summary>
    /// Lenght required of the movementInputDict dictionary before movement is triggered by the MovementManager.cs.
    /// </summary>
    int numOfInput = 0;

    // Timer variables to check the time between each input
    // recorded into the movementInputDict dictionary.
    // Also use to check when to clear the movementInputDict dictionary . 
    float timer = 0;
    float resetTime = 0.3f;
    bool startReset = false;

    /// <summary>
    /// Initialize function from Unity
    /// </summary>
    void Start()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody>();
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
    /// by multiplying speed with direction
    /// </summary>
    void Move()
    {
        Vector3 vec = (moveSpeed * moveDirection);
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
    /// Update which input to use based on movement type
    /// Disable the other movement as and when neccessary
    /// Enable the movement need and set the speed, direction and init
    /// speedPercentage is 0.0f to 1.0f
    /// directionPercentage is 0.0f or 1.0f
    /// </summary>
    void UpdateMovementType()
    {
        HandInput[] handInputs = GetComponentsInChildren<HandInput>();
        LegInput[] legInputs = GetComponentsInChildren<LegInput>();

        switch (magnitudeInputType)
        {
            case e_InputType.e_InputTypeHead:
                GetComponent<ArmSwingMovement>().enabled = false;
                GetComponent<LegLiftMovement>().enabled = false;

                GetComponent<HeadBobMovement>().enabled = true;
                GetComponent<HeadBobMovement>().speedPercentage = 1;
                GetComponent<HeadBobMovement>().directionPercentage = 1;
                GetComponent<HeadBobMovement>().Init();

                foreach(HandInput i in handInputs)
                    i.enabled = false;


                foreach (LegInput i in legInputs)
                    i.enabled = false;

                numOfInput = 1;
                break;

            case e_InputType.e_InputTypeArm:
                GetComponent<HeadBobMovement>().enabled = false;
                GetComponent<LegLiftMovement>().enabled = false;

                GetComponent<ArmSwingMovement>().enabled = true;
                GetComponent<ArmSwingMovement>().speedPercentage = 1;
                GetComponent<ArmSwingMovement>().directionPercentage = 1;

                foreach (HandInput i in handInputs)
                    i.enabled = true;

                foreach (LegInput i in legInputs)
                    i.enabled = false;

                numOfInput = 1;
                break;

            case e_InputType.e_InputTypeLeg:
                GetComponent<ArmSwingMovement>().enabled = false;
                GetComponent<HeadBobMovement>().enabled = false;

                GetComponent<LegLiftMovement>().enabled = true;
                GetComponent<LegLiftMovement>().speedPercentage = 1;
                GetComponent<LegLiftMovement>().directionPercentage = 1;

                foreach (HandInput i in handInputs)
                    i.enabled = false;

                foreach (LegInput i in legInputs)
                    i.enabled = true;

                numOfInput = 1;
                break;

            case e_InputType.e_InputTypeFullBody:
                GetComponent<ArmSwingMovement>().enabled = true;
                GetComponent<ArmSwingMovement>().speedPercentage = 0.33f;
                GetComponent<ArmSwingMovement>().directionPercentage = 0;

                GetComponent<HeadBobMovement>().enabled = true;
                GetComponent<HeadBobMovement>().speedPercentage = 0.33f;
                GetComponent<HeadBobMovement>().directionPercentage = 0;
                GetComponent<HeadBobMovement>().Init();

                GetComponent<LegLiftMovement>().enabled = true;
                GetComponent<LegLiftMovement>().speedPercentage = 0.34f;
                GetComponent<LegLiftMovement>().directionPercentage = 1f;

                foreach (HandInput i in handInputs)
                    i.enabled = true;

                foreach (LegInput i in legInputs)
                    i.enabled = true;

                numOfInput = 3;
                break;

            default:
                break;

        }
    }

}
