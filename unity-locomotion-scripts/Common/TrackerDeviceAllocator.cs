//
// Copyright 2019-2022 Singapore Institute of Technology
//

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using Valve.VR;

/// <summary>
/// Base class for tracker detection and allocation inherited by WalkerTrackerDeviceAllocator.
/// </summary>
public class TrackerDeviceAllocator : MonoBehaviour
{
    public MovementManager movementManager;

    [Header("Actual VR track objects")]
    /// <summary>
    /// List of all tracked objects.
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
    /// Parent object containing all the dummy trackers.
    /// </summary>
    public GameObject dummyTrackerHolder;

    /// <summary>
    /// List of dummy trackers.
    /// </summary>
    public List<SteamVR_TrackedObject> dummyAssignment = new List<SteamVR_TrackedObject>();

    /// <summary>
    /// List of tracker device indices from the dummy trackers' SteamVR_TrackedObject components.
    /// </summary>
    protected List<uint> trackerDeviceIndex = new List<uint>();
    
    /// <summary>
    /// Amount of active trackers currently powered on and detected by Steam VR.
    /// </summary>
    protected uint amtViveTrackers = 0;

    /// <summary>
    /// Minimum trackers needed for the locomotion controls to work.
    /// </summary>
    protected uint neededTrackers = 0;

    /// <summary>
    /// The coroutine currently running for assignment of trackers.
    /// </summary>
    protected Coroutine currCoroutine;

    /// <summary>
    /// Implement Unity component Start function to initialize and start the tracker assignment.
    /// </summary>
    protected virtual void Start()
    {
        Init();
        StartAssignment();
    }

    /// <summary>
    /// Implement Unity component Init function to reset all values.
    /// </summary>
    protected virtual void Init()
    {
        amtViveTrackers = 0;
        SetNumberOfTrackersNeeded();
        trackerDeviceIndex.Clear();
        currCoroutine = null;
    }

    /// <summary>
    /// Set number of trackers required based on magnitudeInputType from MovementManager.
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
    /// Check if the VR HMD is currently worn.
    /// </summary>
    /// <returns>A boolean: true if worn, false otherwise</returns>
    public virtual bool CheckUserPresence()
    {
        bool userPresent = false;

        InputDevice headDevice = InputDevices.GetDeviceAtXRNode(XRNode.Head);

        if (headDevice.isValid)
            headDevice.TryGetFeatureValue(CommonUsages.userPresence, out userPresent);

        return userPresent;
    }

    /// <summary>
    /// Search for all active trackers and assigns them to the dummy tracker object.
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
    /// Begin the coroutine that starts assigning the dummy tracker objects to the actual tracked objects.
    /// </summary>
    protected virtual void StartAssignment()
    {
        currCoroutine = StartCoroutine(DoAssignment());
    }

    /// <summary>Assigning tracker index with index.</summary>
    /// <param name="trackerIndex">The first index variable</param>
    /// <param name="Index">The second index variable</param>
    protected virtual void AssignTrackerIndex(int trackerIndex, int Index)
    {
        // Assigning the new index
        dummyAssignment[trackerIndex].SetDeviceIndex(Index);
    }

    /// <summary>Assigning tracker index from the dynamically assigned index.</summary>
    /// <param name="trackerIndex">The tracker index to be assigned</param>
    /// <param name="dynamicIndex">The dynamically assigned index</param>
    protected virtual void AssignTrackerIndexDynamic(int trackerIndex, int dynamicIndex)
    {
        // Assigning the new index
        dummyAssignment[trackerIndex].SetDeviceIndex((int)trackerDeviceIndex[dynamicIndex]);
    }

    /// <summary>Swap two tracker device indices.</summary>
    /// <param name="first">The first index</param>
    /// <param name="second">The second index</param>
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
    /// Assign dummy tracker object to the real object for tracking and deactivate the dummy tracker.
    /// </summary>
    protected virtual void AssignDummyToRealTrackObject()
    {
        for (int i = 0; i < trackObjectAssignment.Count; i++)
        {
            trackObjectAssignment[i].SetDeviceIndex((int)dummyAssignment[i].index);
        }
        dummyTrackerHolder.SetActive(false);
    }

    /// <summary>Sort the left and right tracker based on the combined center position.</summary>
    /// <param name="limbType">0 = Legs , 2 = Arms </param>
    /// <param name="left">The supposed left position vector</param>
    /// <param name="right">The supposed right position vector</param>
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
    /// Check if the computed distance between two vectors is more than a given distance.
    /// </summary>
    /// <param name="distance">Given distance to check with</param>
    /// <param name="vecOne">1st of the two vectors</param>
    /// <param name="vecTwo">2nd of the two vectors</param>
    /// <returns>A boolean: true if computed difference is more than the given distance, false otherwise</returns>
    protected virtual bool DistanceCheck(float distance, Vector3 vecOne, Vector3 vecTwo)
    {
        return Vector3.Distance(vecOne, vecTwo) >= distance;
    }

    /// <summary>
    /// Default assignment method to be overridden for tracker assignment and setup.
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
