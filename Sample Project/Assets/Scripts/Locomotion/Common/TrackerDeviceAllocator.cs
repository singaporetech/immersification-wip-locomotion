//
// Copyright 2019-2022 Singapore Institute of Technology
//

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using Valve.VR;

/// <summary>
/// Base class for tracker detection and allocation
/// Inheritance used by WalkerTrackerDeviceAllocator.cs
/// </summary>
public class TrackerDeviceAllocator : MonoBehaviour
{
    public MovementManager movementManager;

    [Header("Actual VR track objects")]
    // To store the tracked object holder of the object
    /// <summary>
    /// 0 = left leg * 1 = right leg * 2 = left hand * 3 = right hand * 4 = chest
    /// </summary>
    public List<SteamVR_TrackedObject> trackObjectAssignment = new List<SteamVR_TrackedObject>();
    /*
     * 0 = left leg
     * 1 = right leg
     * 2 = left hand
     * 3 = right hand
     * 4 = chest
     */

    [Header("Dummy objects")]
    /// <summary>
    /// The parent containing all the dummy trackers
    /// </summary>
    public GameObject dummyTrackerHolder;
    /// <summary>
    /// List of dummy trackers.
    /// </summary>
    public List<SteamVR_TrackedObject> dummyAssignment = new List<SteamVR_TrackedObject>();

    /// <summary>
    /// List containing the tracker device Index from the dummy trackers' SteamVR_TrackedObject components.
    /// </summary>
    protected List<uint> trackerDeviceIndex = new List<uint>();
    
    /// <summary>
    /// The amount of active trackers currently powered on and detected by Steam VR.
    /// </summary>
    protected uint amtViveTrackers = 0;

    /// <summary>
    /// The minimum of trackers needed for the locomotion controls to work.
    /// </summary>
    protected uint neededTrackers = 0;

    /// <summary>
    /// The coroutine currently running for assigning of trackers.
    /// </summary>
    protected Coroutine currCoroutine;

    /// <summary>
    /// Initialize function from Unity
    /// </summary>
    protected virtual void Start()
    {
        Init();
        StartAssignment();
    }

    /// <summary>
    /// Init function to run on start to resets all values in the script.
    /// </summary>
    protected virtual void Init()
    {
        amtViveTrackers = 0;
        SetNumberOfTrackersNeeded();

        trackerDeviceIndex.Clear();

        currCoroutine = null;
    }

    /// <summary>
    /// Sets number of trackers required based on movementManager.magnitudeInputType input type.
    /// </summary>
    protected virtual void SetNumberOfTrackersNeeded()
    {
        switch (movementManager.magnitudeInputType)
        {
            case e_InputType.e_InputTypeHead:
                neededTrackers = 0;
                break;
            case e_InputType.e_InputTypeArm:
                neededTrackers = 2;
                break;
            case e_InputType.e_InputTypeLeg:
                neededTrackers = 3;
                break;
            case e_InputType.e_InputTypeFullBody:
                neededTrackers = 5;
                break;
            default:
                Debug.LogError("movementManager.magnitudeInputType is set to an invalid value.");
                break;
        }
    }

    /// <summary>
    /// Checks if the VR HMD is wore.
    /// </summary>
    /// <returns>Returns true if presence is detected. False if not.</returns>
    public virtual bool CheckUserPresence()
    {
        bool userPresent = false;

        InputDevice headDevice = InputDevices.GetDeviceAtXRNode(XRNode.Head);

        if (headDevice.isValid)
            headDevice.TryGetFeatureValue(CommonUsages.userPresence, out userPresent);

        return userPresent;
    }

    /// <summary>
    /// Search for all active trackers and assigns them to a dummy tracker object for storage.
    /// </summary>
    protected virtual void GetViveTrackers()
    {
        trackerDeviceIndex.Clear();
        amtViveTrackers = 0;

        var error = ETrackedPropertyError.TrackedProp_Success;

        for (uint i = 0; i < 16; i++)
        {
            var result = new System.Text.StringBuilder((int)64);
            OpenVR.System.GetStringTrackedDeviceProperty(i, ETrackedDeviceProperty.Prop_RenderModelName_String, result, 64, ref error);

            if (result.ToString().ToLower().Contains("tracker") && OpenVR.System.IsTrackedDeviceConnected(i) /*&& error == ETrackedPropertyError.TrackedProp_Success*/)
            {
                // Store the information
                trackerDeviceIndex.Add(i);

                // Assign the VIVE tracker to a dummy tracking object
                dummyAssignment[(int)amtViveTrackers++].SetDeviceIndex((int)i);
            }
        }
    }

    /// <summary>
    /// Begins the coroutine that starts assigning the dummy tracker objects to the actual tracked objects.
    /// </summary>
    protected virtual void StartAssignment()
    {
        currCoroutine = StartCoroutine(DoAssignment());
    }

    /// <summary>  Assigning tracker index with index</summary>
    /// <param name="trackerIndex">The first index variable</param>
    /// <param name="Index">The second index variable</param>
    protected virtual void AssignTrackerIndex(int trackerIndex, int Index)
    {
        // Assigning the new index
        dummyAssignment[trackerIndex].SetDeviceIndex(Index);
    }

    /// <summary>  Assigning tracker index from the dynamic assign index</summary>
    /// <param name="trackerIndex">The first index variable</param>
    /// <param name="dynamicIndex">The second index variable</param>
    protected virtual void AssignTrackerIndexDynamic(int trackerIndex, int dynamicIndex)
    {
        // Assigning the new index
        dummyAssignment[trackerIndex].SetDeviceIndex((int)trackerDeviceIndex[dynamicIndex]);
    }

    /// <summary>  A swap function for swapping the index value and assigning</summary>
    /// <param name="first">The first index variable</param>
    /// <param name="second">The second index variable</param>
    protected virtual void SwapTrackerIndex(int first, int second)
    {
        // Swap Algorithm
        uint swap = trackerDeviceIndex[first];
        trackerDeviceIndex[first] = trackerDeviceIndex[second];
        trackerDeviceIndex[second] = swap;

        // Assigning the new index
        dummyAssignment[first].SetDeviceIndex((int)trackerDeviceIndex[first]);
        dummyAssignment[second].SetDeviceIndex((int)trackerDeviceIndex[second]);
    }

    /// <summary>
    /// Set the real object for tracking and deactivate the dummy tracker
    /// Update the tracker device
    /// </summary>
    protected virtual void AssignDummyToRealTrackObject()
    {
        for (int i = 0; i < trackObjectAssignment.Count; i++)
        {
            trackObjectAssignment[i].SetDeviceIndex((int)dummyAssignment[i].index);
        }
        dummyTrackerHolder.SetActive(false);
    }

    /// <summary>  To sort the left and right tracker based off the combined center position</summary>
    /// <param name="limbType"> 0 = Legs , 2 = Arms </param>
    /// <param name="left">The suppose left position vector</param>
    /// <param name="right">The suppose right position vector</param>
    protected virtual void AssignPairTrackers(int limbType, Vector3 left, Vector3 right)
    {
        // Construct a centre position based off the left and right position
        Vector3 centre = (left + right) / 2.0f;

        // Check if the suppose left position is incorrect
        if (Vector3.Dot(left - centre, transform.right) > 0)
        {
            // Swap the index of the suppose left and right tracker
            SwapTrackerIndex(limbType + 0, limbType + 1);
        }
    }

    /// <summary>
    /// Returns true if the distance is more than the amount given.
    /// </summary>
    /// <param name="distance"></param>
    /// <param name="vecOne"></param>
    /// <param name="vecTwo"></param>
    /// <returns></returns>
    protected virtual bool DistanceCheck(float distance, Vector3 vecOne, Vector3 vecTwo)
    {
        return Vector3.Distance(vecOne, vecTwo) >= distance;
    }

    /// <summary>
    /// Base method to be overridden for tracker assignment and setup.
    /// </summary>
    protected virtual IEnumerator DoAssignment()
    {
        trackerDeviceIndex.Clear();
        amtViveTrackers = 0;
        currCoroutine = null;
        enabled = false;
        StopAllCoroutines();
        yield return null;
    }
}
