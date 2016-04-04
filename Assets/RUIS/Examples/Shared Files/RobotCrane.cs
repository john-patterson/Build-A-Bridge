using UnityEngine;
using System.Collections;

public class RobotCrane : MonoBehaviour {
	
	public GameObject elevator;
	public GameObject rotatingShaft;

	public RUISSelectableHingeJoint elevationControl;
	public RUISSelectableHingeJoint rotationControl;

	public float maxElevation;
	public float minElevation;
	public float elevationMaxSpeed = 0.4f;
	public float elevationDeadZone = 0.1f;
	public float rotationMaxSpeed  = 20;
	public float rotationDeadZone = 0.1f;

	public RobotCraneRotationAxis rotationAxis = RobotCraneRotationAxis.Y;

	private Vector3 targetElevation;
	private Quaternion targetRotation;
	private Quaternion originalRotation;
	
	public enum RobotCraneRotationAxis 
	{
		X,
		Y,
		Z
	};

	// Use this for initialization
	void Start () 
	{
		if(elevator)
		{
			elevator.transform.localPosition = new Vector3(elevator.transform.localPosition.x,
			                                               Mathf.Clamp(elevator.transform.localPosition.y, minElevation, maxElevation),
			                                               elevator.transform.localPosition.z											);
			targetElevation = elevator.transform.localPosition;
		}
		if(rotatingShaft)
		{
			originalRotation = rotatingShaft.transform.localRotation;
			targetRotation = originalRotation;
		}
	}
	
	// Update is called once per frame
	void Update () 
	{
		if(elevator && elevationControl)
		{
			float elevationInput = elevationControl.GetNormalizedLeverInput();

			if(Mathf.Abs(elevationInput) > elevationDeadZone)
			{
				targetElevation = new Vector3(targetElevation.x,  
				                              Mathf.Clamp(targetElevation.y + elevationMaxSpeed * elevationInput * Time.deltaTime, minElevation, maxElevation), 
				                              targetElevation.z);
				elevator.transform.localPosition = targetElevation;
			}
		}
		
		if(rotatingShaft && rotationControl)
		{
			float rotationInput = rotationControl.GetNormalizedLeverInput();

			if(Mathf.Abs(rotationInput) > rotationDeadZone)
			{
				switch(rotationAxis)
				{
					case RobotCraneRotationAxis.X:
						targetRotation = targetRotation * Quaternion.Euler(rotationMaxSpeed * rotationInput * Time.deltaTime, 0, 0);
						break;
					case RobotCraneRotationAxis.Y:
						targetRotation = targetRotation * Quaternion.Euler(0, rotationMaxSpeed * rotationInput * Time.deltaTime, 0);
						break;
					case RobotCraneRotationAxis.Z:
						targetRotation = targetRotation * Quaternion.Euler(0, 0, rotationMaxSpeed * rotationInput * Time.deltaTime);
						break;
				}

				rotatingShaft.transform.localRotation = targetRotation;
			}
		}
	}

}
