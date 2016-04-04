using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using CSML;
using Kinect = Windows.Kinect;

public class RUISKinectFloorDataCalibrationProcess : RUISCalibrationProcess {
	 
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
//	Quaternion kinect1PitchRotation = Quaternion.identity;
	float kinect1DistanceFromFloor = 0;
	
	private bool kinectChecked = false, calibrationFinnished = false;
	
//	private NIPlayerManagerCOMSelection kinectSelection;
	private OpenNISettingsManager settingsManager;
	private OpenNI.SceneAnalyzer sceneAnalyzer;
	public RUISCoordinateSystem coordinateSystem;
	
	private Vector3 normalVector;
	private bool kinectError = false;
	
	public RUISKinectFloorDataCalibrationProcess(RUISCalibrationProcessSettings calibrationSettings) 
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
//		kinectSelection = MonoBehaviour.FindObjectOfType(typeof(NIPlayerManagerCOMSelection)) as NIPlayerManagerCOMSelection;
		settingsManager = MonoBehaviour.FindObjectOfType(typeof(OpenNISettingsManager)) as OpenNISettingsManager;
	}
	
	
	public override RUISCalibrationPhase InitialPhase(float deltaTime) 
	{
		timeSinceScriptStart += deltaTime;
		
		if(timeSinceScriptStart < 3) {
			this.guiTextLowerLocal = "Calibration of Kinect 1 floor data\n\n Starting up...";
			return RUISCalibrationPhase.Initial;
		}
		
		if(timeSinceScriptStart < 4) {
			this.guiTextLowerLocal = "Connecting to Kinect 1. \n\n Please wait...";
			return RUISCalibrationPhase.Initial;
		}
		
		if(!kinectChecked && timeSinceScriptStart < 5) {
			if (settingsManager == null) {
				this.guiTextLowerLocal = "Connecting to Kinect. \n\n Error: Could not start OpenNI";
				return RUISCalibrationPhase.Invalid;
			}
			else if(settingsManager.UserGenrator == null) {
				this.guiTextLowerLocal = "Connecting to Kinect. \n\n Error: Could not start OpenNI";
				return RUISCalibrationPhase.Invalid;
			}
			else if(!settingsManager.UserGenrator.Valid) {
				this.guiTextLowerLocal = "Connecting to Kinect. \n\n Error: Could not start OpenNI";
				return RUISCalibrationPhase.Invalid;
			}
			else {
				sceneAnalyzer = new OpenNI.SceneAnalyzer((MonoBehaviour.FindObjectOfType(typeof(OpenNISettingsManager)) as OpenNISettingsManager).CurrentContext.BasicContext);
				sceneAnalyzer.StartGenerating();
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
		if(!calibrationFinnished) {
			if(kinectError) this.guiTextLowerLocal = "Error: Could not read Kinect floor data!";
			else 
			{
				this.guiTextLowerLocal =   "Calibration finished!\n\nDistance from floor: " + kinect1DistanceFromFloor 
										 + "\n\nFloor normal: " + normalVector.ToString();
				coordinateSystem.SaveFloorData(xmlFilename, RUISDevice.Kinect_1, normalVector, kinect1DistanceFromFloor);
			}
			calibrationFinnished = true;
		}	
		return RUISCalibrationPhase.ShowResults;
	}
	
	private void UpdateFloorNormalAndDistance()
	{
		coordinateSystem.ResetFloorNormal(RUISDevice.Kinect_1);
		
		OpenNI.Plane3D floor;
		
		try{
			floor = sceneAnalyzer.Floor;
		}
		catch(System.Exception e)
		{
			Debug.LogError(e.TargetSite + ": Failed to get OpenNI.SceneAnalyzer.Floor.");
			kinectError = true;
			return;
			//throw e;
		}
		
//		Quaternion kinectFloorRotator = Quaternion.identity;
		normalVector = new Vector3(floor.Normal.X, floor.Normal.Y, floor.Normal.Z);

		if(normalVector.sqrMagnitude < 0.1f)
			normalVector = Vector3.up;

		Vector3 floorPoint = new Vector3(floor.Point.X, floor.Point.Y, floor.Point.Z);
//		kinectFloorRotator = Quaternion.FromToRotation(normalVector, Vector3.up); 
		kinect1DistanceFromFloor = closestDistanceFromFloor(normalVector, floorPoint, RUISCoordinateSystem.kinectToUnityScale);
//		kinect1PitchRotation = Quaternion.Inverse (kinectFloorRotator);
		
		if(float.IsNaN(kinect1DistanceFromFloor))
			kinect1DistanceFromFloor = 0;
		
		coordinateSystem.SetDistanceFromFloor(kinect1DistanceFromFloor, RUISDevice.Kinect_1);
		coordinateSystem.SetFloorNormal(normalVector, RUISDevice.Kinect_1);
	}
	
	public float closestDistanceFromFloor(Vector3 floorNormal, Vector3 floorPoint, float scaling) 
	{
		
		float closestDistanceFromFloor = 0;
		
		floorNormal = floorNormal.normalized;
		Vector3 newFloorPosition = (new Vector3(floorPoint.x, floorPoint.y, floorPoint.z)) * scaling; 
		//Project the position of the kinect camera onto the floor
		//http://en.wikipedia.org/wiki/Point_on_plane_closest_to_origin
		//http://en.wikipedia.org/wiki/Plane_(geometry)
		float d = floorNormal.x * newFloorPosition.x + floorNormal.y * newFloorPosition.y + floorNormal.z * newFloorPosition.z;
		Vector3 closestFloorPoint = new Vector3(floorNormal.x, floorNormal.y, floorNormal.z);
		closestFloorPoint = (closestFloorPoint * d) / closestFloorPoint.sqrMagnitude;
		//transform the point from Kinect's coordinate system rotation to Unity's rotation
		closestDistanceFromFloor = closestFloorPoint.magnitude;
		
		return closestDistanceFromFloor;
	}
	
	
	
	
	
	
	
	
	
}











