using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using CSML;
using Kinect = Windows.Kinect;

public class RUISKinect2FloorDataCalibrationProcess : RUISCalibrationProcess {
	 
	public string getUpperText() 
	{
		return this.guiTextUpperLocal;
	}
	
	public string getLowerText() 
	{
		return this.guiTextLowerLocal;
	}
	
	public string guiTextUpperLocal, guiTextLowerLocal;
	public override string guiTextUpper { get{return getUpperText();} }
	public override string guiTextLower { get{return getLowerText();} }
	
	private GameObject calibrationPhaseObjects, calibrationResultPhaseObjects, psEyeModelObject, 
	oculusDK2Object, floorPlane, depthView,
	KinectIcon, deviceModelObjects, depthViewObjects, iconObjects;
	
	private string xmlFilename;
	
	private float timeSinceScriptStart;
//	Quaternion kinect2PitchRotation = Quaternion.identity;
	float kinect2DistanceFromFloor = 0;
	
	private bool kinectChecked = false, calibrationFinnished = false;
	
	public RUISCoordinateSystem coordinateSystem;
	
	private Vector3 kinect2FloorNormal;
	Kinect2SourceManager kinect2SourceManager;
	Kinect.Body[] bodyData; 
	
	public RUISKinect2FloorDataCalibrationProcess(RUISCalibrationProcessSettings calibrationSettings) 
	{
		this.calibrationPhaseObjects = calibrationSettings.calibrationPhaseObjects;
		this.calibrationResultPhaseObjects = calibrationSettings.calibrationResultPhaseObjects;
		
		this.deviceModelObjects = calibrationSettings.deviceModelObjects;
		this.depthViewObjects = calibrationSettings.depthViewObjects;
		this.iconObjects = calibrationSettings.iconObjects;
		
		this.floorPlane = GameObject.Find ("Floor");
		
		foreach (Transform child in this.deviceModelObjects.transform)
		{
			child.gameObject.SetActive(false);
		}
		
		foreach (Transform child in this.depthViewObjects.transform)
		{
			child.gameObject.SetActive(false);
		}
		
		foreach (Transform child in this.iconObjects.transform)
		{
			child.gameObject.SetActive(false);
		}
		
		if(this.calibrationPhaseObjects)
			this.calibrationPhaseObjects.SetActive(true);
		if(this.calibrationResultPhaseObjects)
			this.calibrationResultPhaseObjects.SetActive(false);
		this.xmlFilename = calibrationSettings.xmlFilename;
		coordinateSystem = MonoBehaviour.FindObjectOfType(typeof(RUISCoordinateSystem)) as RUISCoordinateSystem;
		
		kinect2SourceManager = MonoBehaviour.FindObjectOfType(typeof(Kinect2SourceManager)) as Kinect2SourceManager;
	}
	
	
	public override RUISCalibrationPhase InitialPhase(float deltaTime) 
	{
		timeSinceScriptStart += deltaTime;
		
		if(timeSinceScriptStart < 3) {
			this.guiTextLowerLocal = "Calibration of Kinect 2 floor data\n\n Starting up...";
			return RUISCalibrationPhase.Initial;
		}
		
		if(timeSinceScriptStart < 4) {
			this.guiTextLowerLocal = "Connecting to Kinect 2. \n\n Please wait...";
			return RUISCalibrationPhase.Initial;
		}
		
		if(!kinectChecked && timeSinceScriptStart > 4) {
			if (!kinect2SourceManager.GetSensor().IsOpen || !kinect2SourceManager.GetSensor().IsAvailable) {
				this.guiTextLowerLocal = "Connecting to Kinect 2. \n\n Error: Could not connect to Kinect 2.";
				return RUISCalibrationPhase.Invalid;
			}
			else  {
				return RUISCalibrationPhase.Preparation;
			}
		}
		
		return RUISCalibrationPhase.Invalid; // Loop should not get this far
	}
	
	
	public override RUISCalibrationPhase PreparationPhase(float deltaTime) 
	{
		this.guiTextLowerLocal = "";
		return RUISCalibrationPhase.ReadyToCalibrate;
	}
	
	
	public override RUISCalibrationPhase ReadyToCalibratePhase(float deltaTime) 
	{
		return RUISCalibrationPhase.Calibration;
	}
	
	
	public override RUISCalibrationPhase CalibrationPhase(float deltaTime) 
	{
		UpdateFloorNormalAndDistance(); 
		if(this.floorPlane)
			this.floorPlane.transform.position = new Vector3(0, 0, 0);
		return RUISCalibrationPhase.ShowResults;
	}
	
	
	public override RUISCalibrationPhase ShowResultsPhase(float deltaTime) 
	{
		if(!calibrationFinnished ) {
			this.guiTextLowerLocal = "Calibration finished!\n\nDistance from floor: " + kinect2DistanceFromFloor + "\n\nFloor normal: " + kinect2FloorNormal.ToString();
			coordinateSystem.SaveFloorData(xmlFilename, RUISDevice.Kinect_2, kinect2FloorNormal, kinect2DistanceFromFloor);
			calibrationFinnished = true;
		}
		return RUISCalibrationPhase.ShowResults;
	}
	
	private void UpdateFloorNormalAndDistance()
	{
		coordinateSystem.ResetFloorNormal(RUISDevice.Kinect_2);
		
		Windows.Kinect.Vector4 kinect2FloorPlane = kinect2SourceManager.GetFlootClipPlane();
		kinect2FloorNormal = new Vector3(kinect2FloorPlane.X, kinect2FloorPlane.Y, kinect2FloorPlane.Z);
		kinect2FloorNormal.Normalize();
		
		if(kinect2FloorNormal.sqrMagnitude < 0.1f)
			kinect2FloorNormal = Vector3.up;

		kinect2DistanceFromFloor = kinect2FloorPlane.W / Mathf.Sqrt(kinect2FloorNormal.sqrMagnitude);
		
//		Quaternion kinect2FloorRotator = Quaternion.FromToRotation(kinect2FloorNormal, Vector3.up); 
		
//		kinect2PitchRotation = Quaternion.Inverse (kinect2FloorRotator);

		if(float.IsNaN(kinect2DistanceFromFloor))
			kinect2DistanceFromFloor = 0;

		coordinateSystem.SetDistanceFromFloor(kinect2DistanceFromFloor, RUISDevice.Kinect_2);
		coordinateSystem.SetFloorNormal(kinect2FloorNormal, RUISDevice.Kinect_2);
	}
	
}











