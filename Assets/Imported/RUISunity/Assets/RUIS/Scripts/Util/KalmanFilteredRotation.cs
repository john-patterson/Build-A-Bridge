/*****************************************************************************

Content    :   Class for Kalman filtering a quaternion
Authors    :   Tuukka Takala
Copyright  :   Copyright 2013 Tuukka Takala. All Rights reserved.
Licensing  :   RUIS is distributed under the LGPL Version 3 license.

******************************************************************************/

using UnityEngine;
using System.Collections;

public class KalmanFilteredRotation {

	private KalmanFilter filterRot;
	
	/// <summary>
	/// Filtered rotation
	/// </summary>
	public Quaternion rotationState {get; private set;}
	
	/// <summary>
	/// Noise covariance for the Kalman filter. Bigger values mean more smoothing (and slugginess)
	/// </summary>
	public float rotationNoiseCovariance = 100;
	
	public bool skipIdenticalMeasurements
	{
		get
		{
		    return filterRot.skipIdenticalMeasurements;
		}
		set
		{
		    filterRot.skipIdenticalMeasurements = value;
		}
	}
	
	public int identicalMeasurementsCap
	{
		get
		{
		    return filterRot.identicalMeasurementsCap;
		}
		set
		{
		    filterRot.identicalMeasurementsCap = value;
		}
	}
	
	private Quaternion lastMeasurement = Quaternion.identity;
	private double[] measurement = {0, 0, 0, 1};
	private double[] rot = {0, 0, 0, 1};
	private bool firstRun = true;
	
	/// <summary>
	/// Initialize the rotation Kalman filter
	/// </summary>
	public KalmanFilteredRotation () 
	{
		rotationState = Quaternion.identity;
		filterRot = new KalmanFilter();
		filterRot.initialize(4,4);
	}
	
	public void Reset()
	{
		filterRot.initialize(4,4);
	}

	/// <summary>
	/// Execute one Kalman predict and update step with the measured rotation
	/// </summary>
	public Quaternion Update(Quaternion measuredRotation, float deltaTime) 
	{
		if(firstRun)
		{
			lastMeasurement = measuredRotation;
			rotationState = measuredRotation;
			firstRun = false;	
		}
		
		// Do the following measurement sign flip (rotation is still the same) if the last two
		// measurements appear to have their signs flipped (as often happens around the poles).
		// This way the Kalman doesn't start to filter the quaternion towards zero.
		if(Mathf.Abs(measuredRotation.x - lastMeasurement.x) + Mathf.Abs(measuredRotation.y - lastMeasurement.y) + 
			Mathf.Abs(measuredRotation.z - lastMeasurement.z) + Mathf.Abs(measuredRotation.w - lastMeasurement.w) > 1.0f)
		{
			measuredRotation.x = -measuredRotation.x;
			measuredRotation.y = -measuredRotation.y;
			measuredRotation.z = -measuredRotation.z;
			measuredRotation.w = -measuredRotation.w;
		}
		
		// Discontinuity between last two measured quaternions
//		if( Mathf.Abs(measuredRotation.x - (float)measurement[0]) + Mathf.Abs(measuredRotation.y - (float)measurement[1]) + 
//			Mathf.Abs(measuredRotation.z - (float)measurement[2]) + Mathf.Abs(measuredRotation.w - (float)measurement[3]) > 1.0f)
//			Debug.LogError("diff diff " + (measuredRotation) + " current: diff " + measurement[0] + " " + measurement[1]
//							+ " " + measurement[2] + " " + measurement[3]);
		
		lastMeasurement = measuredRotation;
		
		measurement[0] = measuredRotation.x;
		measurement[1] = measuredRotation.y;
		measurement[2] = measuredRotation.z;
		measurement[3] = measuredRotation.w;
		filterRot.setR(deltaTime * rotationNoiseCovariance);
	    filterRot.predict();
	    filterRot.update(measurement);
		rot = filterRot.getState();
		
		rotationState = new Quaternion ((float) rot[0], (float) rot[1], 
										(float) rot[2], (float) rot[3] );
		// Normalize only when rotation is not near the poles (because Euler conversion is not continuous there)
		if(Mathf.Abs(Vector3.Dot((rotationState*Vector3.forward).normalized, Vector3.up)) < 0.7f)
			rotationState = Quaternion.Euler(rotationState.eulerAngles);
		
		return rotationState;
	}
}