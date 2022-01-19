//
// Copyright 2019-2022 Singapore Institute of Technology
//

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;

/// <summary>
/// Get the swing input from the hand.
/// This will prompt armswing movement
/// after the user does a set amount of swings
///
/// PAPER : HB2 Walk In Place Sensors 
/// todo: remove commments like this as these files will be public facing
///
/// Author: Centre for Immersification, Singapore Institute of Technology
/// Contact: Please Visit https://www.immersification.org
/// todo: place the above in all the locomotion related files
///
/// todo: please look at all the comments below in detail and revamp the
///       rest of the files in the same way. Priority is all locomotion 
///       files including the collision etc...
///       Note that there are many documentation inconsistencies in the other files
///       , e.g., in CollisionStateController
/// </summary>
public class HandInput : MonoBehaviour
{
    [Header("Controller and Camera")]
    public ArmSwingMovement ArmSwingController;
    public Transform cam;
    [SerializeField]
    public e_Hand hand;

    // swing data
    private List<Vector3> swingForwardQueue = new List<Vector3>();
    private List<Vector3> swingBackwardQueue = new List<Vector3>();

    // min amount of mag and swing
    [HideInInspector]
    public float minSwing; // Min value  to determine a vaild swing
    /// <summary>
    /// Minimum sample data obtained
    /// todo: why suddenly this format of comments for this particular attribute only?
    ///       also, why no line space for this comment?
    /// </summary>
    [HideInInspector]
    public float minDataSetRequired;
    [HideInInspector]
    public float maxErrorSwing;
    private float errorCount = 0;

    // to select controller or tracker
    public bool tracker = false;

    //check if hand is moving
    private bool isDead = true;
    private float DeadZone = 0.1f; // Min value to determine the controller is counted as "moving", but does not mean the swing motion is vaild

    private ArmWalkerFrameData previousFrameData;

    /// <summary>
    /// Operations when this component first runs.
    /// </summary>
    private void Start()
    {
        SetUpControl();
        ResetFrameData();
        ClearAll();
    }

    /// <summary>
    /// Get initial data for error from the main control
    /// todo: too brief and overly cryptic, e.g., what is "error" and "main control"?
    ///       what constitutes "initial data"?
    /// </summary>
    public void SetUpControl()
    {
        DeadZone = ArmSwingController.movementDeadZone;
        minSwing = ArmSwingController.minSwingDistance;
        minDataSetRequired = ArmSwingController.minSwingsForCompleteSet;
        maxErrorSwing = ArmSwingController.maxErrors;
    }

    /// todo: missing header
    public void ResetFrameData()
    {
        previousFrameData.SetPosition(transform.localPosition, transform.position);
        previousFrameData.SetSwingDirection(ArmSwingDirection.Dead);
    }

    /// todo: missing header and all the internal comments need to clean up and make sure a new programmer understand
    ///       most importantly the header summary needs to be clear and comprehensible
    private void Update()
    {
        //Determine deadzone and get deadzone vector
        Vector3 deadZoneVec = ((transform.localPosition - previousFrameData.localPosition) / Time.deltaTime);
        //Sets a bool to detect if the hand is not moving...
        isDead = (deadZoneVec.sqrMagnitude < ReturnSqrMagnitude(DeadZone)) ? true : false;

        //If not dead, means the user is doing some action
        if (!isDead)
        {
            ///Getting the input data from hand object
            Vector3 frameVec = ((transform.position - previousFrameData.position) / Time.deltaTime);

            //But does not gurantee the action is vaild for a "swing" so more checks are needed.
            // So check if the swing is big enough to be considered a vaild input frame
            if (frameVec.sqrMagnitude >= ReturnSqrMagnitude(minSwing))
            {
                frameVec.y = 0;
                float mag = frameVec.magnitude;
                frameVec = frameVec.normalized * mag;

                // swinging backwards
                if (Vector3.Dot(cam.forward, frameVec) < 0)
                {
                    switch (previousFrameData.direction)
                    {
                        //Previous frame was forward motion, current frame is backward motion
                        //So count current frame as error as it does not match the previous frame
                        case (ArmSwingDirection.Forward):
                            {
                                //if (errorCount == 0)
                                //    swingBackwardQueue.Clear();

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
                    previousFrameData.direction = ArmSwingDirection.Backward;

                }
                // swinging forward
                else
                {
                    switch (previousFrameData.direction)
                    {
                        //Previous frame was backward motion, current frame is forward motion
                        //So count current frame as error as it does not match the previous frame
                        case (ArmSwingDirection.Backward):
                            {
                                //if (errorCount == 0)
                                //    swingBackwardQueue.Clear();

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
                    previousFrameData.direction = ArmSwingDirection.Forward;
                }

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
                previousFrameData.direction = ArmSwingDirection.Invaild;
            }
        }
        //Else if the input is dead,
        else
        {
            previousFrameData.direction = ArmSwingDirection.Dead;
            ErrorAdd();
        }

        //At the end of the loop, find the current hand position as previous position for use later in next update loop
        previousFrameData.localPosition = transform.localPosition;
        previousFrameData.position = transform.position;
    }

    /// <summary>
    /// Converting the input datas to the a swing motion data
    /// and send the information back the main control
    /// </summary>
    private void SendSwingInformation()
    {
        /// add the input datas
        Vector3 tmp = Vector3.zero;
        bool forwardSwing = (previousFrameData.direction == ArmSwingDirection.Forward) ? true : false;

        if (forwardSwing)
        {
            foreach (Vector3 vec in swingForwardQueue)
            {
                tmp += vec;
            }
            tmp /= swingForwardQueue.Count;
        }
        else
        {
            foreach (Vector3 vec in swingBackwardQueue)
            {
                tmp += vec;
            }
            tmp /= swingBackwardQueue.Count;
        }

        // send the information back the main control
        if (hand == e_Hand.e_HandLeft)
            ArmSwingController.ReceiveLeftInput(tmp, forwardSwing);
        else
            ArmSwingController.ReceiveRightInput(tmp, forwardSwing);
    }

    /// <summary>
    /// Converts the input vector list to swing motion data,
    /// then sends the data back to main control.
    /// </summary>
    /// <param name=inputList>
    /// A list of Vector3s.
    /// </param>
    /// todo: all other func headers need the param and return tags where appropriate
    private void SendSwingInformation(List<Vector3> inputList)
    {
        /// add the input datas        
        Vector3 tmp = Vector3.zero;
        bool forwardSwing = (previousFrameData.direction == ArmSwingDirection.Forward) ? true : false;

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

    /// todo: too many missing headers
    private void ErrorAdd()
    {
        errorCount++;

        // Reset all if error count is max or more
        if (errorCount >= maxErrorSwing)
        {
            //forward = !forward;
            ClearAll();
            //Debug.LogWarning("トワまじ大天使");
        }
    }

    /// <summary>
    /// Clears  all input data and the error count.
    /// todo: the code does not really relate to "input data". Need a short description of the 2 Queues.
    /// </summary>
    private void ClearAll()
    {
        /// todo: need to annotate all commented code, what they are used for if we uncomment
        //invalidCount = 0;
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

    // ====================== Calculation functions ======================

    public float ReturnSqrMagnitude(float num)
    {
        return num * num;
    }
}

/// todo: missing doc again
public struct ArmWalkerFrameData
{
    public Vector3 localPosition;
    public Vector3 position;
    public ArmSwingDirection direction;
    /// todo: make all var names self-documenting, e.g., direction -> armSwingDirection

    /// missing doc
    public void SetPosition(Vector3 locPos, Vector3 pos)
    {
        localPosition = locPos;
        position = pos;
    }

    /// missing doc
    public void SetSwingDirection(int num)
    {
        direction = (ArmSwingDirection)num;
    }
    
    /// missing doc
    public void SetSwingDirection(ArmSwingDirection type)
    {
        direction = type;
    }
}

/// todo: missing doc
public enum ArmSwingDirection
{
    Dead = 0,
    Forward = 1,
    Backward = 2,
    Invaild = 3
}
