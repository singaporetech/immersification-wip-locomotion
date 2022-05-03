//
// Copyright 2019-2022 Singapore Institute of Technology
//

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// To assign allocated trackers to each limb and the torso.
/// </summary>
public class WalkerTrackerDeviceAllocator : TrackerDeviceAllocator
{
    /// <summary>
    /// Sort the 2 trackers to the left and right arms</summary>
    /// </summary>
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

    /// <summary>
    /// Sort the 3 trackers to the left and right for legs and chest
    /// </summary>
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

    /// <summary>
    /// Sort the 4 trackers to the left and right for legs and arms
    /// </summary>
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

    /// <summary>
    /// Sort the 4 trackers to the left and right for legs and arms
    /// and chest
    /// </summary>
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

    /// <summary>
    /// Assignment coroutine that assigns the dummy tracker objects to the actual tracked objects.
    /// </summary>
    protected override IEnumerator DoAssignment()
    {
        // Delay assigning for .2 seconds.
        // To enable trackers to be properly detected before assigning.
        float waitValue = .2f; 
        yield return new WaitForSeconds(waitValue);

        // Get all tracker objects and assigns to a random dummy/container first
        while (amtViveTrackers < neededTrackers)
        {
            // Gets trackers if user is supposedly ready and wearing all trackers.
            if (CheckUserPresence())
                GetViveTrackers();

            yield return new WaitForSeconds(waitValue);
        }

        yield return new WaitForEndOfFrame();

        bool farCheck;
        float stablizeTime = .5f;
        float t = 0;
        float maxDis = .3f;

        Vector3[] prevPos = new Vector3[amtViveTrackers];
        for (int i = 0; i < amtViveTrackers; i++)
        {
            prevPos[i] = dummyAssignment[i].transform.position;
        }

        // Wait for user to stop moving before assigning trackers
        while (t < stablizeTime)
        {
            farCheck = false;
            for (int i = 0; i < amtViveTrackers; i++)
            {
                if (!farCheck)
                    farCheck = DistanceCheck(maxDis, prevPos[i], dummyAssignment[i].transform.position);
                prevPos[i] = dummyAssignment[i].transform.position;
            }

            if (farCheck || !CheckUserPresence())
            {
                t = 0;
            }
            else
            {
                t += Time.deltaTime;
            }

            yield return new WaitForEndOfFrame();
        }

        // Assign dummy tracker to real tracker object
        // and according to the amount of trackers present
        switch (amtViveTrackers)
        {
            case 2: // Arm locomotion (Arms only)
                    SortArmTrackers();

                    // Remove assign of legs and chest
                    AssignTrackerIndex(0, 11);
                    AssignTrackerIndex(1, 11);
                    AssignTrackerIndex(4, 11);

                break;

            case 3: // Leg locomotion (legs & chest)
                SortChestAndLegsTrackers();

                // Remove assign of left and right hand
                AssignTrackerIndex(2, 11);
                AssignTrackerIndex(3, 11);
                break;

            case 5: // Full body locmotion (Arms, legs, and chest)
                SortAllTrackers();
                break;
        }
        // Assign to the tracker objects with script
        AssignDummyToRealTrackObject();

        // Clear data and stop
        base.DoAssignment();
        yield return null;
    }

}