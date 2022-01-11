using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using Valve.VR;

public class TrackerDeviceAllocator : MonoBehaviour
{
    [Header("Actual VR track objects")]
    // To store the trackedobject holder of the object
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
    // Dummy Holder
    public GameObject dummyTrackerHolder;
    public List<SteamVR_TrackedObject> dummyAssignment = new List<SteamVR_TrackedObject>();

    //[HideInInspector]
    public bool stepOneTrackersDetected = false, stepTwoTrackerStablized = false, stepThreeTrackersAllocated = false;
    // to keep track on the trackers' index
    protected List<uint> viveTrackerDeviceIndex = new List<uint>();
    // Stores the amount of trackers detected in the scene
    protected uint amtViveTrackers = 0;

    protected Coroutine currCoroutine;

    protected virtual void Start()
    {
        Initialize();
        StartAssignment();
    }

    // ============== Detect functions ================

    public virtual bool CheckUserPresent()
    {
        bool userPresent = false;

        InputDevice headDevice = InputDevices.GetDeviceAtXRNode(XRNode.Head);

        if (headDevice.isValid)
        {
            //bool presenceFeatureSupported = headDevice.TryGetFeatureValue(CommonUsages.userPresence, out userPresent);
            bool presenceFeatureSupported = headDevice.TryGetFeatureValue(CommonUsages.userPresence, out userPresent);
        }

        Debug.Log("User present is " + userPresent + ".");
        return userPresent;
    }



    // ============== Setup and initialize functions ==============
    protected virtual void Initialize()
    {
        stepOneTrackersDetected = false;
        stepTwoTrackerStablized = false;
        stepThreeTrackersAllocated = false;

        amtViveTrackers = 0;
        viveTrackerDeviceIndex.Clear();

        currCoroutine = null;
    }

    // Search for all active VIVE trackers and assigns them to a dummy object
    protected virtual void GetViveTrackers()
    {
        viveTrackerDeviceIndex.Clear();
        amtViveTrackers = 0;

        var error = ETrackedPropertyError.TrackedProp_Success;

        //var error2 = ETrackedPropertyError.TrackedProp_Success;

        for (uint i = 0; i < 16; i++)
        {
            var result = new System.Text.StringBuilder((int)64);
            OpenVR.System.GetStringTrackedDeviceProperty(i, ETrackedDeviceProperty.Prop_RenderModelName_String, result, 64, ref error);

            //OpenVR.System.GetBoolTrackedDeviceProperty(i, ETrackedDeviceProperty.Prop_RegisteredDeviceType_String, ref error2);

            if (result.ToString().ToLower().Contains("tracker") && OpenVR.System.IsTrackedDeviceConnected(i) /*&& error == ETrackedPropertyError.TrackedProp_Success*/)
            {
                // Store the information
                viveTrackerDeviceIndex.Add(i);

                // Assign the VIVE tracker to a dummy tracking object
                dummyAssignment[(int)amtViveTrackers++].SetDeviceIndex((int)i);
            }
        }
        //Debug.Log(viveTrackerDeviceIndex.Count);
    }

    protected virtual void StartAssignment()
    {
        currCoroutine = StartCoroutine(DoAssignment());
    }

    // ============== Assign Vive tracker to tracked object functions =============

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
        dummyAssignment[trackerIndex].SetDeviceIndex((int)viveTrackerDeviceIndex[dynamicIndex]);
    }

    /// <summary>  A swap function for swapping the index value and assigning</summary>
    /// <param name="first">The first index variable</param>
    /// <param name="second">The second index variable</param>
    protected virtual void SwapTrackerIndex(int first, int second)
    {
        // Swap Algorithm
        uint swap = viveTrackerDeviceIndex[first];
        viveTrackerDeviceIndex[first] = viveTrackerDeviceIndex[second];
        viveTrackerDeviceIndex[second] = swap;

        // Assigning the new index
        dummyAssignment[first].SetDeviceIndex((int)viveTrackerDeviceIndex[first]);
        dummyAssignment[second].SetDeviceIndex((int)viveTrackerDeviceIndex[second]);
    }

    // Set the real object for tracking and deactivate the dummy tracker
    // Update the tracker device
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

    protected virtual IEnumerator DoAssignment()
    {
        viveTrackerDeviceIndex.Clear();
        amtViveTrackers = 0;
        currCoroutine = null;
        enabled = false;
        StopAllCoroutines();
        yield return null;
    }
}
