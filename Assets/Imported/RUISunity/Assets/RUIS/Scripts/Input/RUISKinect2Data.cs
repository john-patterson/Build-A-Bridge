/*****************************************************************************

Content    :   Wrapper for getting Kinect 2 data
Authors    :   Heikki Heiskanen
Copyright  :   Copyright 2013 Tuukka Takala, Heikki Heiskanen. All Rights reserved.
Licensing  :   RUIS is distributed under the LGPL Version 3 license.

******************************************************************************/

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Kinect = Windows.Kinect;

public class RUISKinect2Data : MonoBehaviour {
	

	public GameObject SourceManager;

	private Kinect2SourceManager _SourceManager;
	
	public void Awake()
	{
		_SourceManager = SourceManager.GetComponent<Kinect2SourceManager>();
	}
//	private Dictionary<Kinect.JointType, Kinect.JointType> _BoneMap = new Dictionary<Kinect.JointType, Kinect.JointType>()
//	{
//		{ Kinect.JointType.FootLeft, Kinect.JointType.AnkleLeft },
//		{ Kinect.JointType.AnkleLeft, Kinect.JointType.KneeLeft },
//		{ Kinect.JointType.KneeLeft, Kinect.JointType.HipLeft },
//		{ Kinect.JointType.HipLeft, Kinect.JointType.SpineBase },
//		
//		{ Kinect.JointType.FootRight, Kinect.JointType.AnkleRight },
//		{ Kinect.JointType.AnkleRight, Kinect.JointType.KneeRight },
//		{ Kinect.JointType.KneeRight, Kinect.JointType.HipRight },
//		{ Kinect.JointType.HipRight, Kinect.JointType.SpineBase },
//		
//		{ Kinect.JointType.HandTipLeft, Kinect.JointType.HandLeft },
//		{ Kinect.JointType.ThumbLeft, Kinect.JointType.HandLeft },
//		{ Kinect.JointType.HandLeft, Kinect.JointType.WristLeft },
//		{ Kinect.JointType.WristLeft, Kinect.JointType.ElbowLeft },
//		{ Kinect.JointType.ElbowLeft, Kinect.JointType.ShoulderLeft },
//		{ Kinect.JointType.ShoulderLeft, Kinect.JointType.SpineShoulder },
//		
//		{ Kinect.JointType.HandTipRight, Kinect.JointType.HandRight },
//		{ Kinect.JointType.ThumbRight, Kinect.JointType.HandRight },
//		{ Kinect.JointType.HandRight, Kinect.JointType.WristRight },
//		{ Kinect.JointType.WristRight, Kinect.JointType.ElbowRight },
//		{ Kinect.JointType.ElbowRight, Kinect.JointType.ShoulderRight },
//		{ Kinect.JointType.ShoulderRight, Kinect.JointType.SpineShoulder },
//		
//		{ Kinect.JointType.SpineBase, Kinect.JointType.SpineMid },
//		{ Kinect.JointType.SpineMid, Kinect.JointType.SpineShoulder },
//		{ Kinect.JointType.SpineShoulder, Kinect.JointType.Neck },
//		{ Kinect.JointType.Neck, Kinect.JointType.Head },
//	};
	
	public Kinect.Body[] getData(out bool newFrame) 
	{
		newFrame = _SourceManager.isNewFrame;
		if(_SourceManager)
			return _SourceManager.GetBodyData();
		else
			return null;

	}

}
