/*****************************************************************************

Content    :   A class used to follow a certain kinect joint around
Authors    :   Mikael Matveinen
Copyright  :   Copyright 2013 Tuukka Takala, Mikael Matveinen. All Rights reserved.
Licensing  :   RUIS is distributed under the LGPL Version 3 license.

******************************************************************************/

using UnityEngine;
using System.Collections;

public class RUISKinectJointFollower : MonoBehaviour {
    private RUISSkeletonManager skeletonManager;
    public int playerId;
	public int bodyTrackingDeviceID;

    public RUISSkeletonManager.Joint jointToFollow;

    public float minimumConfidenceToUpdate = 0.5f;

    public float positionSmoothing = 5.0f;
    public float rotationSmoothing = 5.0f;

	void Awake () {
        if (skeletonManager == null)
        {
            skeletonManager = FindObjectOfType(typeof(RUISSkeletonManager)) as RUISSkeletonManager;
        }
	}
	
	void Update () {
		if (!skeletonManager || !skeletonManager.skeletons[bodyTrackingDeviceID, playerId].isTracking) return;

		RUISSkeletonManager.JointData jointData = skeletonManager.GetJointData(jointToFollow, playerId, bodyTrackingDeviceID);
        if(jointData.positionConfidence > minimumConfidenceToUpdate)
        {
            transform.localPosition = Vector3.Lerp(transform.localPosition, jointData.position, positionSmoothing * Time.deltaTime);
        }
        if(jointData.rotationConfidence > minimumConfidenceToUpdate)
        {
            transform.localRotation = Quaternion.Slerp(transform.localRotation, jointData.rotation, rotationSmoothing * Time.deltaTime);
        }
	}
}
