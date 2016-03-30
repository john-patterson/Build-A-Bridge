/*****************************************************************************

Content    :   Class for Kalman filtering a transform (position and rotation)
Authors    :   Tuukka Takala
Copyright  :   Copyright 2013 Tuukka Takala, Mikael Matveinen. All Rights reserved.
Licensing  :   RUIS is distributed under the LGPL Version 3 license.

******************************************************************************/

using UnityEngine;
using System.Collections;

public class KalmanFilteredTransform : MonoBehaviour
{
	private KalmanFilter filterPos;
	private KalmanFilteredRotation filterRot = new KalmanFilteredRotation();
	
	private double[] measuredPos = {0, 0, 0};
	
	private Vector3 inputPos;
	private Quaternion inputRot;
	
	private double[] pos = {0, 0, 0};
	
	private Transform outputTransform;
	public Transform inputTransform;
	
	public bool inputInLocalCoordinates = false;
	public bool filterInFixedUpdate = false;
	
	public float positionNoiseCovariance = 200;
	public float rotationNoiseCovariance = 100;
	
	// Use this for initialization
	void Start ()
	{
		outputTransform = transform;
		filterPos = new KalmanFilter();
		filterPos.initialize(3,3);
	}
	
	// Update is called once per frame
	void Update ()
	{
		if(!filterInFixedUpdate)
		{
			if(!inputTransform)
			{
				Debug.LogError("ERROR: inputTransform is None! Set it in the inspector.");
				return;	
			}
			if(inputInLocalCoordinates)
			{
				inputPos  = inputTransform.localPosition;
				inputRot  = inputTransform.localRotation;
			}
			else
			{
				inputPos  = inputTransform.position;
				inputRot  = inputTransform.rotation;
			}
			
			measuredPos[0] = inputPos.x;
			measuredPos[1] = inputPos.y;
			measuredPos[2] = inputPos.z;
			filterPos.setR(Time.deltaTime * positionNoiseCovariance);
		    filterPos.predict();
		    filterPos.update(measuredPos);
			pos = filterPos.getState();
			
//			measuredRot[0] = inputRot.x;
//			measuredRot[1] = inputRot.y;
//			measuredRot[2] = inputRot.z;
//			measuredRot[3] = inputRot.w;
//			filterRot.setR(Time.deltaTime * rotationNoiseCovariance);
//		    filterRot.predict();
//		    filterRot.update(measuredRot);
//			rot = filterRot.getState();
			
			filterRot.rotationNoiseCovariance = rotationNoiseCovariance;
			filterRot.Update(inputRot, Time.deltaTime);
			
			if(GetComponent<Rigidbody>() == null)
			{
				if(inputInLocalCoordinates)
				{

					outputTransform.localPosition = new Vector3((float) pos[0], (float) pos[1], (float) pos[2]);
					outputTransform.localRotation = filterRot.rotationState;
				}
				else
				{
					outputTransform.position = new Vector3((float) pos[0], (float) pos[1], (float) pos[2]);
					outputTransform.rotation = filterRot.rotationState;
				}
			}
		}
	}
	
	void FixedUpdate() 
	{
		
		if(!inputTransform)
		{
			Debug.LogError("ERROR: inputTransform is None! Set it in the inspector.");
			return;	
		}
		
		if(filterInFixedUpdate) 
		{
			if(inputInLocalCoordinates) 
			{
				inputPos  = inputTransform.localPosition;
				inputRot  = inputTransform.localRotation;
				
			}
			else 
			{
				inputPos  = inputTransform.position;
				inputRot  = inputTransform.rotation;
			}
			
			measuredPos[0] = inputPos.x;
			measuredPos[1] = inputPos.y;
			measuredPos[2] = inputPos.z;
			filterPos.setR(Time.fixedDeltaTime * positionNoiseCovariance);
		    filterPos.predict();
		    filterPos.update(measuredPos);
			pos = filterPos.getState();
			
//			measuredRot[0] = inputRot.x;
//			measuredRot[1] = inputRot.y;
//			measuredRot[2] = inputRot.z;
//			measuredRot[3] = inputRot.w;
//			filterRot.setR(Time.fixedDeltaTime * rotationNoiseCovariance);
//		    filterRot.predict();
//		    filterRot.update(measuredRot);
//			rot = filterRot.getState();
			
			filterRot.rotationNoiseCovariance = rotationNoiseCovariance;
			filterRot.Update(inputRot, Time.fixedDeltaTime);

			if(GetComponent<Rigidbody>() == null)
			{
				if(inputInLocalCoordinates)
				{
					outputTransform.localPosition = new Vector3((float) pos[0], (float) pos[1], (float) pos[2]);
					outputTransform.localRotation = filterRot.rotationState;
				}
				else
				{
					outputTransform.position = new Vector3((float) pos[0], (float) pos[1], (float) pos[2]);
					outputTransform.rotation = filterRot.rotationState;
				}
			}
		}
		if(GetComponent<Rigidbody>() != null)
		{
			if(inputInLocalCoordinates)
			{
				/* // Below does not work
				if(!(   outputTransform.localPosition.x == (float) pos[0] 
					 && outputTransform.localPosition.y == (float) pos[1] 
					 && outputTransform.localPosition.z == (float) pos[2]))
					rigidbody.MovePosition(outputTransform.localToWorldMatrix*(new Vector3((float) pos[0], 
																						   (float) pos[1], 
																						   (float) pos[2] )));
				
				if(!(   outputTransform.localRotation.x == (float) rot[0] 
					 && outputTransform.localRotation.y == (float) rot[1] 
					 && outputTransform.localRotation.z == (float) rot[2] 
					 && outputTransform.localRotation.w == (float) rot[3]))
					rigidbody.MoveRotation(MathUtil.QuaternionFromMatrix(
													outputTransform.localToWorldMatrix)*(new Quaternion ((float) rot[0], (float) rot[1], 
									   	  					      									     (float) rot[2], (float) rot[3])));
				*/
				
				outputTransform.localPosition = new Vector3((float) pos[0], (float) pos[1], (float) pos[2]);
				outputTransform.localRotation = filterRot.rotationState;
			}
			else
			{
				if(!(   outputTransform.position.x == (float) pos[0] 
					 && outputTransform.position.y == (float) pos[1] 
					 && outputTransform.position.z == (float) pos[2]))
					GetComponent<Rigidbody>().MovePosition(new Vector3((float) pos[0], (float) pos[1], (float) pos[2]));
				
				if(!(   outputTransform.rotation.x == filterRot.rotationState.x
					 && outputTransform.rotation.y == filterRot.rotationState.y
					 && outputTransform.rotation.z == filterRot.rotationState.z
					 && outputTransform.rotation.w == filterRot.rotationState.w))
					GetComponent<Rigidbody>().MoveRotation(filterRot.rotationState);
			}
		}
	}
}
