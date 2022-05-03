//
// Copyright 2019-2022 Singapore Institute of Technology
//

using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Get the swing input from the hand.
/// This will prompt armswing movement
/// after the user does a set amount of swings
/// </summary>
public class HandInput : MonoBehaviour
{
    [Header("Components")]
    /// <summary>
    /// The controller which inputs are sent to.
    /// </summary>
    public ArmSwingMovement ArmSwingController;
    /// <summary>
    /// The VR camera of the VR user.
    /// </summary>
    public Transform vrCamera;

    [SerializeField]
    public e_Hand hand;

    /// <summary>
    /// Forward swing captured input
    /// </summary>
    private List<Vector3> swingForwardQueue = new List<Vector3>();
    /// <summary>
    /// Backward swing captured input
    /// </summary>
    private List<Vector3> swingBackwardQueue = new List<Vector3>();

    /// <summary>
    /// Min value to determine a vaild swing
    /// </summary>
    [HideInInspector] 
    public float minSwing;

    /// <summary>
    /// Minimum sample data required. 
    /// </summary>
    [HideInInspector]
    public float minDataSetRequired;

    /// <summary>
    /// The maximum number of errors in each swing set.
    /// </summary>
    [HideInInspector]
    public float maxErrorSwing;

    /// <summary>
    /// Current number of erros in the swing.
    /// </summary>
    private float errorCount = 0;

    /// <summary>
    /// True if hand is inactive. False if hand is active, in a swing.
    /// </summary>
    private bool isDead = true;

    /// <summary>
    /// Min value to determine the controller is counted as "moving", but does not mean the swing motion is vaild
    /// </summary>
    private float DeadZone = 0.1f;

    /// <summary>
    /// Contains swing data of the captured frame
    /// </summary>
    private ArmWalkerFrameData previousFrameData;

    /// <summary>
    /// Initialize function from Unity
    /// </summary>
    private void Start()
    {
        SetUpControl();
        ResetFrameData();
        ClearAll();
    }

    /// <summary>
    /// Get input detection parameters from ArmSwingController
    /// </summary>
    public void SetUpControl()
    {
        DeadZone = ArmSwingController.movementDeadZone;
        minSwing = ArmSwingController.minSwingDistance;
        minDataSetRequired = ArmSwingController.minSwingsForCompleteSet;
        maxErrorSwing = ArmSwingController.maxErrors;
    }

    /// <summary>
    /// Resets previousFrameData.
    /// </summary>
    public void ResetFrameData()
    {
        previousFrameData.SetPosition(transform.localPosition, transform.position);
        previousFrameData.SetSwingDirection(ArmSwingDirection.Dead);
    }

    /// <summary>
    /// Update function from unity
    /// Detections if swing movements are considered vaild inputs.
    /// </summary>
    private void Update()
    {
        // Determine deadzone and get deadzone vector
        Vector3 deadZoneVec = ((transform.localPosition - previousFrameData.localPosition) / Time.deltaTime);

        // Sets a bool to detect if the hand is moving.
        isDead = (deadZoneVec.sqrMagnitude < ReturnSqrMagnitude(DeadZone)) ? true : false;

        if (!isDead)
        {
            // Getting  input data
            Vector3 frameVec = ((transform.position - previousFrameData.worldPosition) / Time.deltaTime);

            // Checks if the swing is big enough to be considered a vaild input frame
            if (frameVec.sqrMagnitude >= ReturnSqrMagnitude(minSwing))
            {
                frameVec.y = 0;
                float mag = frameVec.magnitude;
                frameVec = frameVec.normalized * mag;

                // For if the User is doing a backwards swing
                if (Vector3.Dot(vrCamera.forward, frameVec) < 0)
                {
                    // Utilize previous frame swing data to check
                    // if the user is still swinging in the same direction
                    // or in reverse.
                    switch (previousFrameData.swingDirection)
                    {
                        case (ArmSwingDirection.Forward):
                            {
                                errorCount++;
                                if (errorCount >= maxErrorSwing)
                                {
                                    swingForwardQueue.Clear();
                                    errorCount = 0;
                                }
                                break;
                            }
                        default:
                            {
                                swingBackwardQueue.Add(frameVec);
                                break;
                            }
                    }
                    previousFrameData.swingDirection = ArmSwingDirection.Backward;

                }

                // Swinging forward
                else
                {
                    switch (previousFrameData.swingDirection)
                    {
                        // Utilize previous frame swing data to check
                        // if the user is still swinging in the same direction
                        // or in reverse.
                        case (ArmSwingDirection.Backward):
                            {
                                errorCount++;
                                
                                if (errorCount >= maxErrorSwing)
                                {
                                    swingBackwardQueue.Clear();
                                    errorCount = 0;
                                }
                                break;
                            }
                        default:
                            {
                                swingForwardQueue.Add(frameVec);
                                break;
                            }
                    }
                    previousFrameData.swingDirection = ArmSwingDirection.Forward;
                }

                // Min number of valid swing inputs reached.
                // Inputs will be sent to the armSwingMovement.cs
                if (swingBackwardQueue.Count == minDataSetRequired || swingForwardQueue.Count == minDataSetRequired)
                {
                    List<Vector3> sendList = (swingBackwardQueue.Count == minDataSetRequired) ? swingBackwardQueue : swingForwardQueue;
                    SendSwingInformation(sendList);
                    ClearAll();
                }
            }

            // if the input data is too small to consider
            else
            {
                previousFrameData.swingDirection = ArmSwingDirection.Invaild;
            }
        }

        // Else if the input is dead,
        else
        {
            previousFrameData.swingDirection = ArmSwingDirection.Dead;
            ErrorAdd();
        }

        //At the end of the loop, save the current position as previous position for use later in the next update loop
        previousFrameData.localPosition = transform.localPosition;
        previousFrameData.worldPosition = transform.position;
    }

    /// <summary>
    /// Converts the input vector list to swing motion data,
    /// then sends the data back to main control.
    /// </summary>
    /// <param name="inputList">A list of frame input  vectors</param>
    private void SendSwingInformation(List<Vector3> inputList)
    {
        /// add the input datas        
        Vector3 tmp = Vector3.zero;
        bool forwardSwing = (previousFrameData.swingDirection == ArmSwingDirection.Forward) ? true : false;

        foreach (Vector3 vec in inputList)
        {
            tmp += vec;
        }
        tmp /= inputList.Count;

        // send the information back the main control
        if (hand == e_Hand.e_HandLeft)
            ArmSwingController.ReceiveLeftInput(tmp, forwardSwing);
        else
            ArmSwingController.ReceiveRightInput(tmp, forwardSwing);
    }

    /// <summary>
    /// Increase the error count which indicates number of errors in a swing set.
    /// Upon reaching the thershold indicated by maxErrorSwing, calls ClearAll(). 
    /// </summary>
    private void ErrorAdd()
    {
        errorCount++;

        if (errorCount >= maxErrorSwing)
        {
            ClearAll();
        }
    }

    /// <summary>
    /// Clears swingForwardQueue, swingBackwardQueue, and resets errorCount to 0.
    /// </summary>
    private void ClearAll()
    {
        errorCount = 0;
        swingForwardQueue.Clear();
        swingBackwardQueue.Clear();
    }

    /// <summary>
    /// Checking the inputs data per update
    /// Determine whether going forward or back
    /// if the amount of data is equal to min set
    /// call function to swing
    /// </summary>

    public bool GetIsDead()
    {
        return isDead;
    }

    /// <summary>
    /// Returns the square magnitude.
    /// </summary>
    /// <param name="num">Number which square magnitude is to be returned.</param>
    /// <returns>Returns the square magnitude.</returns>
    public float ReturnSqrMagnitude(float num)
    {
        return num * num;
    }
}

/// todo: missing doc again
public struct ArmWalkerFrameData
{
    public Vector3 localPosition;
    public Vector3 worldPosition;
    public ArmSwingDirection swingDirection;

    /// <summary>
    /// Sets the local and world space position of the ArmWalkerFrameData.
    /// </summary>
    public void SetPosition(Vector3 localPos, Vector3 worldPos)
    {
        localPosition = localPos;
        worldPosition = worldPos;
    }

    /// <summary>
    /// Sets the swing direction of ArmWalkerFrameData.
    /// </summary>
    /// <param name="type">The input swing type of this frame data</param>
    public void SetSwingDirection(ArmSwingDirection type)
    {
        swingDirection = type;
    }
}

/// <summary>
/// Enum which indicates the swing direction and type of the specified ArmWalkerFrameData.
/// </summary>
public enum ArmSwingDirection
{
    Dead = 0,
    Forward = 1,
    Backward = 2,
    Invaild = 3
}
