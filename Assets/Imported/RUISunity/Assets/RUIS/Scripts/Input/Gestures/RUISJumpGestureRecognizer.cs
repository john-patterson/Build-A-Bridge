/*****************************************************************************

Content    :   Implements a basic jump gesture
Authors    :   Mikael Matveinen
Copyright  :   Copyright 2013 Tuukka Takala, Mikael Matveinen. All Rights reserved.
Licensing  :   RUIS is distributed under the LGPL Version 3 license.

******************************************************************************/

using UnityEngine;
using System.Collections;

[RequireComponent(typeof(RUISPointTracker))]
public class RUISJumpGestureRecognizer : RUISGestureRecognizer
{
    public int playerId = 0;
	public int bodyTrackingDeviceID = 0;

    public float requiredUpwardVelocity = 1.0f;
    public float timeBetweenJumps = 1.0f;
    public float feetHeightThreshold = 0.1f;
    public float requiredConfidence = 1.0f;

    public enum State
    {
        WaitingForJump,
        Jumping,
        AfterJump
    }
    public State currentState { get; private set; }


    private float timeCounter = 0;
    private bool gestureEnabled = true;


    public Vector3 leftFootHeight { get; private set; }
    public Vector3 rightFootHeight { get; private set; }

    private RUISSkeletonManager skeletonManager;
    private RUISPointTracker pointTracker;
	private RUISSkeletonController skeletonController;

    private bool previousIsTracking = false;
    private bool isTrackingBufferTimeFinished = false;

    public void Awake()
    {
		skeletonController = FindObjectOfType(typeof(RUISSkeletonController)) as RUISSkeletonController;
        pointTracker = GetComponent<RUISPointTracker>();
        skeletonManager = FindObjectOfType(typeof(RUISSkeletonManager)) as RUISSkeletonManager;
		ResetProgress();
    }
	public void Start() {
		
		bodyTrackingDeviceID = skeletonController.bodyTrackingDeviceID;
	}
    public void Update()
    {
        if (!skeletonManager) return;

		bool currentIsTracking = skeletonManager.skeletons[bodyTrackingDeviceID, playerId].isTracking;

        if (!currentIsTracking)
        {
            previousIsTracking = false;
            isTrackingBufferTimeFinished = false;
            return;
        } else if (currentIsTracking != previousIsTracking)
        {
            StartCoroutine("StartCountdownTillGestureEnable");
        }

        previousIsTracking = currentIsTracking;

        if (!gestureEnabled || !isTrackingBufferTimeFinished) return;

        switch (currentState)
        {
            case State.WaitingForJump:
                DoWaitingForJump();
                break;
            case State.Jumping:
                DoJumping();
                break;
            case State.AfterJump:
                DoAfterJump();
                break;
        }
    }

    public override bool GestureIsTriggered()
    {
        return gestureEnabled && currentState == State.Jumping;
    }
    
	public override bool GestureWasTriggered()
	{
		return false; // Not implemented
	}
	
    public override float GetGestureProgress()
    {
        return (gestureEnabled && currentState == State.Jumping) ? 1 : 0;
    }

    public override void ResetProgress()
    {
        currentState = State.WaitingForJump;

        timeCounter = 0;
    }



    public override void EnableGesture()
    {
        gestureEnabled = true;
        ResetProgress();
    }

    public override void DisableGesture()
    {
        gestureEnabled = false;
    }

    private void DoJumping()
    {
        currentState = State.AfterJump;
    }

    private void DoAfterJump()
    {
        timeCounter += Time.deltaTime;

        if (timeCounter >= timeBetweenJumps)
        {
            ResetProgress();
            return;
        }
    }

    private void DoWaitingForJump()
    {
		if (skeletonManager.skeletons[bodyTrackingDeviceID, playerId].leftFoot.positionConfidence < requiredConfidence ||
		    skeletonManager.skeletons[bodyTrackingDeviceID, playerId].rightFoot.positionConfidence < requiredConfidence)
        {
            return;
        }

		leftFootHeight = skeletonManager.skeletons[bodyTrackingDeviceID, playerId].leftFoot.position;
		rightFootHeight = skeletonManager.skeletons[bodyTrackingDeviceID, playerId].rightFoot.position;

        if (leftFootHeight.y >= feetHeightThreshold && rightFootHeight.y >= feetHeightThreshold && pointTracker.averageVelocity.y >= requiredUpwardVelocity)
        {
            currentState = State.Jumping;
            timeCounter = 0;
            return;
        }
    }

    private IEnumerator StartCountdownTillGestureEnable()
    {
        yield return new WaitForSeconds(3.0f);

        isTrackingBufferTimeFinished = true;
    }
    
	public override bool IsBinaryGesture()
	{
		return true;
	}
}
