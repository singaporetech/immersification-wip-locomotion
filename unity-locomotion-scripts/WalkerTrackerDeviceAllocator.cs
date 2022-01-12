using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;
using UnityEngine.XR;

/// <summary>
/// To assign the dynamic allocated tracker number to each limb
/// </summary>
public class WalkerTrackerDeviceAllocator : TrackerDeviceAllocator
{
    [Header("Hand objects")]
    // To store the left and right hand script to switch from controller input to tracker input
    public HandInput leftHandInput;
    public HandInput rightHandInput;

    public MovementManager movementManager;

    //======================================= Methods to initiate which walker methods we are using =======================================

    // Hard coded assign of trackers function for different input types
    //------------------------------
    /// <summary>  Sort the 2 trackers to the left and right arms</summary>
    void SortArmTrackers()
    {
        // Determine left and right trackers
        AssignPairTrackers(0, dummyAssignment[0].GetComponent<Transform>().position,
                            dummyAssignment[1].GetComponent<Transform>().position);

        // Assign left arm
        AssignTrackerIndexDynamic(2, 0);
        // Assign right arm
        AssignTrackerIndexDynamic(3, 1);
    }

    /// <summary>  Sort the 2 trackers to the left and right arms</summary>
    void SortLegTrackers()
    {
        // Determine left and right trackers
        AssignPairTrackers(0, dummyAssignment[0].GetComponent<Transform>().position,
                            dummyAssignment[1].GetComponent<Transform>().position);

        // Assign left leg
        AssignTrackerIndexDynamic(0, 0);
        // Assign right leg
        AssignTrackerIndexDynamic(1, 1);
    }

    // uses SwapTrackerIndex
    /// <summary>  Sort the 4 trackers to the left and right for legs and arms</summary>
    void SortChestAndLegsTrackers()
    {
        List<Vector3> limbPos = new List<Vector3>();
        limbPos.Add(dummyAssignment[0].GetComponent<Transform>().position); // left leg
        limbPos.Add(dummyAssignment[1].GetComponent<Transform>().position); // right leg
        limbPos.Add(dummyAssignment[2].GetComponent<Transform>().position); // chest

        // Construct a centre
        Vector3 centre = (limbPos[0] + limbPos[1] + limbPos[2]) / 3.0f;

        // To use as leg counter
        int breakCounter = 0;

        // Loop to swap leg and arm index if the position y is greater than centre y
        for (; breakCounter < 2; breakCounter++)
        {
            if (limbPos[breakCounter].y > centre.y)
            {
                // Swap the index for legs and chest
                SwapTrackerIndex(breakCounter, 2);
            }
        }

        // Sort the left and right for the legs
        AssignPairTrackers(0, limbPos[0], limbPos[1]);

        // Swap the left hand with the chest
        AssignTrackerIndexDynamic(4, 2);
    }

    //uses SwapTrackerIndex
    /// <summary>  Sort the 4 trackers to the left and right for legs and arms</summary>
    void SortArmLegTrackers()
    {
        // Store the position of the 4 trackers
        List<Vector3> limbPos = new List<Vector3>();
        limbPos.Add(dummyAssignment[0].GetComponent<Transform>().position); // left leg
        limbPos.Add(dummyAssignment[1].GetComponent<Transform>().position); // right leg
        limbPos.Add(dummyAssignment[2].GetComponent<Transform>().position); // left arm
        limbPos.Add(dummyAssignment[3].GetComponent<Transform>().position); // right arm

        // Construct a centre
        Vector3 centre = (limbPos[0] + limbPos[1] + limbPos[2] + limbPos[3]) / 4.0f;

        // To use as leg counter
        int breakCounter = 0;
        // To use as arm counter
        int swapArmCounter = 2;

        // Loop to swap leg and arm index if the position y is greater than centre y
        while (breakCounter < 2 && swapArmCounter < 4)
        {
            if (limbPos[breakCounter].y > centre.y)
            {
                // Swap the position
                Vector3 swap = limbPos[breakCounter];
                limbPos[breakCounter] = limbPos[swapArmCounter];
                limbPos[swapArmCounter] = swap;

                // Swap the index
                SwapTrackerIndex(breakCounter, swapArmCounter);

                // increase arm counter
                swapArmCounter++;
            }
            else // increase leg counter
                breakCounter++;
        }

        // Sort the left and right for the legs
        AssignPairTrackers(0, limbPos[0], limbPos[1]);

        // Sort the left and right for the arms
        AssignPairTrackers(2, limbPos[2], limbPos[3]);
    }

    //uses SwapTrackerIndex
    void SortAllTrackers()
    {
        // Store the position of the 4 trackers
        List<Vector3> limbPos = new List<Vector3>();
        limbPos.Add(dummyAssignment[0].GetComponent<Transform>().position); // left leg
        limbPos.Add(dummyAssignment[1].GetComponent<Transform>().position); // right leg
        limbPos.Add(dummyAssignment[2].GetComponent<Transform>().position); // left arm
        limbPos.Add(dummyAssignment[3].GetComponent<Transform>().position); // right arm
        limbPos.Add(dummyAssignment[4].GetComponent<Transform>().position); // chest

        // Construct a centre
        Vector3 centre = (limbPos[0] + limbPos[1] + limbPos[2] + limbPos[3] + limbPos[4]) / 5;

        // to store the index closest to the centre on a xz plane
        float smallestDist = float.MaxValue;
        int smallestIndex = 4;

        // loop to find the nearest tracker from centre on a xz plane
        for (int counter = 0; counter < 5; counter++)
        {
            // calculate distance on a xz plane
            Vector3 dist = limbPos[counter] - centre;
            dist.y = 0;

            if (dist.magnitude < smallestDist)
            {
                smallestDist = dist.magnitude;
                smallestIndex = counter;
            }
        }

        if (smallestIndex != 4)
        {
            // Swap the index to store at chest
            SwapTrackerIndex(smallestIndex, 4);
        }

        // Assign both arm and legs
        SortArmLegTrackers();
    }

    protected override IEnumerator DoAssignment()
    {
        float waitValue = .2f;

        // stage 1
        //------------------------------
        // To use as a delay to get better value for vive tracker transforms
        yield return new WaitForSeconds(waitValue);

        // stage 2
        //------------------------------
        // Get all tracker objects and assigns to a random dummy/container first, for reading later

        //Need to mod so it will detect trackers based on number needed.
        while (amtViveTrackers < movementManager.numberOfRequiredTrackers)
        {
            if (CheckUserPresent() /*XRDevice.isPresent*/)
            {
                GetViveTrackers();
            }
            //else
            //{
            //    //Debug.Log("No VR device detected. Waiting for detect to be attaced");
            //}

            yield return new WaitForSeconds(waitValue);
        }

        stepOneTrackersDetected = true;

        // stage 2.5
        // For Walker input only
        //------------------------------
        // If trackers == 4 indicate trackers are used instead of controller
        if (viveTrackerDeviceIndex.Count != 0 && viveTrackerDeviceIndex.Count != 3)
        {
            // Switch from controller input into tracker input for both arms
            leftHandInput.tracker = true;
            leftHandInput.SetUpControl();

            rightHandInput.tracker = true;
            rightHandInput.SetUpControl();
        }

        // stage 3
        // Wait for player to stablize / stop moving
        //------------------------------
        // Delay one frame for the assignment of the index
        yield return new WaitForEndOfFrame();

        bool farCheck = false;
        float stablizeTime = .5f;
        float t = 0;
        float maxDis = .3f;

        Vector3[] prevPos = new Vector3[amtViveTrackers];
        for (int i = 0; i < amtViveTrackers; i++)
        {
            prevPos[i] = dummyAssignment[i].transform.position;
        }

        while (t < stablizeTime)
        {
            farCheck = false;
            for (int i = 0; i < amtViveTrackers; i++)
            {
                if (!farCheck)
                    farCheck = DistanceCheck(maxDis, prevPos[i], dummyAssignment[i].transform.position);
                prevPos[i] = dummyAssignment[i].transform.position;
            }

            if (farCheck || !CheckUserPresent())
            {
                t = 0;
            }
            else
            {
                t += Time.deltaTime;
            }

            yield return new WaitForEndOfFrame();
        }
        stepTwoTrackerStablized = true;

        // Stage 4
        // Assign dummy tracker to real tracker object via offset from each other and location
        // and according to the amount of trackers present.
        //------------------------------
        switch (amtViveTrackers)
        {
            case 2: // Arm locomotion (Arms only)
                if (movementManager.magnitudeInputType == e_InputType.e_InputTypeArm)
                {
                    SortArmTrackers();

                    // Remove assign of legs and chest
                    AssignTrackerIndex(0, 11);
                    AssignTrackerIndex(1, 11);
                    AssignTrackerIndex(4, 11);
                }
                else // Default to legs only
                {
                    SortLegTrackers();

                    // Remove assign of arm and chest
                    AssignTrackerIndex(2, 11);
                    AssignTrackerIndex(3, 11);
                    AssignTrackerIndex(4, 11);
                }

                break;
            case 3: // Leg locomotion (legs & chest)
                SortChestAndLegsTrackers();

                // Remove assign of left and right hand
                AssignTrackerIndex(2, 11);
                AssignTrackerIndex(3, 11);
                break;
            case 4: // Both arms and legs trackers
                SortArmLegTrackers();

                // Remove assign of chest
                AssignTrackerIndex(4, 11);
                break;
            case 5: // Full body locmotion (Arms, legs, and chest)
                SortAllTrackers();
                break;
        }
        // Assign to the tracker objects with script
        AssignDummyToRealTrackObject();
        stepThreeTrackersAllocated = true;

        // Clear data and stop
        base.DoAssignment();
        yield return null;
    }

}