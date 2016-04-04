using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class RUISFistGestureRecognizer : RUISGestureRecognizer {
	
	public float fistClosedDuration = 0.3f; // In seconds
	public float fistOpenDuration = 0.3f; // In seconds
	public int fistClosedSignalLimit = 3;
	public int fistOpenSignalLimit = 3;
	private float foundValidSignals, validTimeWindow;
	
	// Stores timestamps when open/closed signals arrived 
	private float[] fistClosedSignalTimestampBuffer; // Store closed signals
	private float[] fistOpenSignalTimestampBuffer;// Store open signals
	private int closedBufferIndex = 0;
	private int openBufferIndex = 0;
	
	public float lostFistReleaseDuration = 3; // In seconds
	private float lastClosedSignalTimestamp = 0;
	
	RUISSkeletonWand skeletonWand;
	RUISSkeletonManager.Skeleton.handState fistStatusInSensor;
	bool gestureEnabled = false;
	
	float fistClosedTime, fistOpenTime;
	RUISSkeletonManager.Skeleton.handState leftFistStatusInSensor, rightFistStatusInSensor;
	[HideInInspector]
	public bool handClosed;
	
	private bool gestureWasTriggered;
	
	private bool currentHandStatus, newHandStatus;
	
	RUISSkeletonManager ruisSkeletonManager;
	
	public fistSide leftOrRightFist;
	public enum fistSide {
		InferFromName,
		RightFist,
		LeftFist
	}
	
	void Awake()
	{
		fistClosedSignalTimestampBuffer = new float[fistClosedSignalLimit];
		fistOpenSignalTimestampBuffer = new float[fistOpenSignalLimit]; 
		ruisSkeletonManager = FindObjectOfType(typeof(RUISSkeletonManager)) as RUISSkeletonManager;
		skeletonWand = GetComponent<RUISSkeletonWand>();
		handClosed = false;
		gestureWasTriggered = false;
		
		if(leftOrRightFist == fistSide.InferFromName) {
			if(skeletonWand.wandStart.ToString().IndexOf("Right") != -1) leftOrRightFist = fistSide.RightFist;
			if(skeletonWand.wandStart.ToString().IndexOf("Left") != -1) leftOrRightFist = fistSide.LeftFist;
		}
	}
	
	void Start()
	{
		// Not used
	}
	
	void LateUpdate()
	{
		
//		if (!gestureEnabled) 
//		{
//			handClosed = false;
//			return;
//		}

		rightFistStatusInSensor = ruisSkeletonManager.skeletons[skeletonWand.bodyTrackingDeviceID, skeletonWand.playerId].rightHandStatus;
		leftFistStatusInSensor  = ruisSkeletonManager.skeletons[skeletonWand.bodyTrackingDeviceID, skeletonWand.playerId].leftHandStatus;
		
		if(leftOrRightFist == fistSide.LeftFist) fistStatusInSensor = leftFistStatusInSensor;
		else fistStatusInSensor = rightFistStatusInSensor;
		
		if(!ruisSkeletonManager.isNewKinect2Frame) return; 
		
		currentHandStatus = handClosed;
		
		if(handClosed)
		{
			// If received closed signal, reset buffer
			if(fistStatusInSensor == RUISSkeletonManager.Skeleton.handState.closed) 
			{
				fistOpenSignalTimestampBuffer = new float[fistOpenSignalLimit];
				lastClosedSignalTimestamp = Time.time;
			}
			// If last signal was open, check if array is full of recent enough signals
			else if(fistStatusInSensor == RUISSkeletonManager.Skeleton.handState.open) 
			{
				fistOpenSignalTimestampBuffer[openBufferIndex] = Time.time;
				openBufferIndex = (openBufferIndex + 1) % fistOpenSignalLimit;
				foundValidSignals = 0;
				validTimeWindow = Time.time - fistOpenDuration;
				for(int i = 0; i < fistOpenSignalTimestampBuffer.Length; i++) 
				{
					if(fistOpenSignalTimestampBuffer[i] > validTimeWindow) foundValidSignals++;
				}
				// Trigger opening of hand
				if(foundValidSignals >= fistOpenSignalLimit && handClosed) 
				{ 
					fistOpenSignalTimestampBuffer = new float[fistOpenSignalLimit];
					fistClosedSignalTimestampBuffer = new float[fistClosedSignalLimit];
					handClosed = false; 
				}
			}	
		}
		else 
		{	
			// If received open signal, reset buffer
			if(fistStatusInSensor == RUISSkeletonManager.Skeleton.handState.open) fistClosedSignalTimestampBuffer = new float[fistClosedSignalLimit];
			// If last signal was open, check if array is full of recent enough signals
			else if(fistStatusInSensor == RUISSkeletonManager.Skeleton.handState.closed) 
			{
				fistClosedSignalTimestampBuffer[closedBufferIndex] = Time.time;
				closedBufferIndex = (closedBufferIndex + 1) % fistClosedSignalLimit;
				foundValidSignals = 0;
				validTimeWindow = Time.time - fistClosedDuration;
				for(int i = 0; i < fistClosedSignalTimestampBuffer.Length; i++) 
				{
					if(fistClosedSignalTimestampBuffer[i] > validTimeWindow) foundValidSignals++;
				}
				// Trigger opening of hand
				if(foundValidSignals >= fistClosedSignalLimit && !handClosed) 
				{ 
					fistOpenSignalTimestampBuffer = new float[fistOpenSignalLimit];
					fistClosedSignalTimestampBuffer = new float[fistClosedSignalLimit];
					handClosed = true; 
					lastClosedSignalTimestamp = Time.time;
				}
			}	
		}
		// If no close signal detected for certaint amount of time, assume open hand.
		if(Time.time - lastClosedSignalTimestamp > fistOpenSignalLimit && handClosed) 
		{
			lastClosedSignalTimestamp = Time.time;
			fistOpenSignalTimestampBuffer = new float[fistOpenSignalLimit];
			fistClosedSignalTimestampBuffer = new float[fistClosedSignalLimit];
			handClosed = false;		
		}
		
		newHandStatus = handClosed;
		
		if(currentHandStatus == false && newHandStatus == true) 
			gestureWasTriggered = !gestureWasTriggered;
		
	}
	
	public override bool GestureIsTriggered()
	{
		// return handClosed;
		return handClosed && gestureEnabled;
	}
	
	public override bool GestureWasTriggered()
	{
		return gestureWasTriggered;
	}
	
	public override float GetGestureProgress()
	{
		if(handClosed)
			return 1;
		else 
			return 0;
	}
	
	public override void ResetProgress()
	{
		// Not used
	}
	
	private void StartTiming()	
	{
		// Not used
	}
	
	private void ResetData()
	{
		// Not used
	}
	
	public override void EnableGesture()
	{
		gestureEnabled = true;
	}
	
	public override void DisableGesture()
	{	
		gestureEnabled = false;
	}

	public override bool IsBinaryGesture()
	{
		return true;
	}

}
