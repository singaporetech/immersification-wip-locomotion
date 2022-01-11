# What is this repo?

This repo contains the essential Unity code scripts used to implement the HEAD-BOB, ARM-SWING, LEG-LIFT and FULL-BODY walking-in-place (WIP) methods used in the forthcoming paper on "Understanding User Experiences Across VR Walking-in-place Locomotion Methods".

The paper will be linked once it has been published.

Essential details (extracted from the paper) for each algorithm are given in the subsections below. Please read the paper for more details on the science behind these implementations, as well as all the associated references that these implementations were built upon. 

Implementation details can be found within the code (and comments) itself, and feel free to post any issues you find in this repo.

## Head-bob Implementation

Position and orientation of the VR HMD (a HTC Vive Pro) are tracked using its embedded accelerometer and gyroscope sensor. The vertical translations of the HMD translate to forward speeds to move the user forward in virtual space, while the orientation of the HMD translate to the virtual movement direction. The orientation also determines where the user will be facing which means that the user has to look in the direction of intended movement. This implementation is based on the assumption that users will naturally bob their bodies (and hence their heads) when they walk and will do the same when they perform WIP.

## Arm-swing Implementation

The swinging action of the arms are tracked using a consumer-friendly setup that includes beacons (SteamVR Base Stations) and IMU-based tracking devices (HTC Vive Trackers) strapped to the arms. Movement direction is determined from the same motion sensors, as they are strapped in a fixed orientation relative to the user’s intended move- ment direction. This implementation is based on the assumption that users swing their arms during WIP, similar to how they would in real-life walking.

## Leg-lift Implementation

The lifting action of the legs are tracked using the same tracking setup but with the trackers now strapped to the ankles. Movement direction is deter- mined from an additional tracker on the waist as the leg sensors will not be able to reliably maintain a fixed orientation relative to the user’s intended movement direction. This implementation is based on the assumption that users lift their legs during WIP, similar to how they would in real-life walking.

## Full-body Implementation

This is a combination of the Head-bob, Arm-swing and Leg-lift methods, to investigate any experiential differences provided by a sensor fusion approach. For example, one possible advantage was for users to freely look around (versus in pure Head-bob where they have to look at where they are going) while still incorporating the bobbing action tracked by the HMD for translation into walking velocities. This method collects tracked positional and orientation data from the various sensors used in all the previous methods and translate the combined averaged values to virtual speeds and directions in the virtual envi- ronment. In general, this provides another source of comparison to solicit experiential feedback from participants.

## Other details

For all the four implementations, the HTC Vive Pro HMD and Vive Trackers were used in a SteamVR Tracking setup within two SteamVR Base Stations that demarcated an 4m by 4m VR play- space for participants. The Vive Trackers were paired with different straps for the arms, ankles and waist as needed for each setup.

The study in the paper focuses on WIP setups that only require tracking components that are easily accessible to end consumers, and hence can be reasonably applied to designs using similar off-the-shelf tracking mechanisms. For this study, a SteamVR Tracking setup was used, which combines both tracking from internal IMUs in the HTC Vive Trackers and external tracking with beacons to determine rotation and position data to enable WIP. Alternative systems using similar forms of tracking such as the StonX system will likely produce similar results to the studies presented here. Systems that use only internal IMUs for tracking, e.g., SlimeVR, may also infer some insights from this study, but in a more cautious manner due to technical differences. For example, the lower tracking precision may not provide the same extent of affordances mentioned in Section 4.3.

# How to use the code

These are Unity C# component scripts that developers can use in a Unity3D project. Currently, we do not provide an example project but it should be straightfoward for experienced Unity developers to include in their projects.

The code can also be treated as a source of information for the implementation details of the various WIP methods discussed in the paper.
