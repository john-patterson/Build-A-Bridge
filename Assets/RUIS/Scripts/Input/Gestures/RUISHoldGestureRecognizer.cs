/*****************************************************************************

Content    :   Implements a basic hold gesture
Authors    :   Mikael Matveinen
Copyright  :   Copyright 2013 Tuukka Takala, Mikael Matveinen. All Rights reserved.
Licensing  :   RUIS is distributed under the LGPL Version 3 license.

******************************************************************************/

using UnityEngine;
using System.Collections;

[RequireComponent(typeof(RUISPointTracker))]
public class RUISHoldGestureRecognizer : RUISGestureRecognizer
{
    float gestureProgress = 0;

    public float holdLength = 2.0f;
    public float speedThreshold = 0.25f;

    bool gestureStarted = false;
    float timeSinceStart;

    bool gestureEnabled = false;

    RUISPointTracker pointTracker;

    void Awake()
    {
        pointTracker = GetComponent<RUISPointTracker>();
    }

    void Start()
    {
        ResetData();
    }

    void Update()
    {
        if (!gestureEnabled) return;

        if (gestureStarted && pointTracker.averageSpeed < speedThreshold)
        {
            timeSinceStart += Time.deltaTime;

            gestureProgress = Mathf.Clamp01(timeSinceStart / holdLength);
        }
        else if (pointTracker.averageSpeed < speedThreshold)
        {
            StartTiming();
        }
        else
        {
            ResetData();
        }
    }

    public override bool GestureIsTriggered()
    {	
        return gestureProgress >= 0.99f;
    }

	public override bool GestureWasTriggered()
	{
		return false; // Not implemented
	}

    public override float GetGestureProgress()
    {
        return gestureProgress;
    }

    public override void ResetProgress()
    {
        timeSinceStart = 0;
        gestureProgress = 0;
    }

    private void StartTiming()
    {
        ResetData();
        gestureStarted = true;
    }

    private void ResetData()
    {
        gestureStarted = false;
        gestureProgress = 0;
        timeSinceStart = 0;
    }

    public override void EnableGesture()
    {
        gestureEnabled = true;
        ResetData();
    }

    public override void DisableGesture()
    {
        gestureEnabled = false;
        ResetData();
    }
    
	
	public override bool IsBinaryGesture()
	{
		return false;
	}
}
