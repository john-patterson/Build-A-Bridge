using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using CSML;
using Kinect = Windows.Kinect;

public class RUISKinect2ToKinectCalibrationProcess : RUISCalibrationProcess {
	
	public string getUpperText() {
		return this.guiTextUpperLocal;
	}
	
	public string getLowerText() {
		return this.guiTextLowerLocal;
	}
	
	// Abstract class variables
	private RUISDevice inputDevice1, inputDevice2;
	public string guiTextUpperLocal, guiTextLowerLocal;
	public bool useScreen1, useScreen2;
	
	public override string guiTextUpper { get{return getUpperText();} }
	public override string guiTextLower { get{return getLowerText();} }
	
	// Custom variables
	private NIPlayerManagerCOMSelection kinectSelection;
	private OpenNISettingsManager settingsManager;
	private OpenNI.SceneAnalyzer sceneAnalyzer;
	private List<Vector3> samples_Kinect1, samples_Kinect2;
	private int numberOfSamplesTaken = 0;
	private int numberOfSamplesToTake, numberOfSamplesPerSecond;
	private float timeSinceLastSample, timeBetweenSamples, timeSinceScriptStart;
	public RUISCoordinateSystem coordinateSystem;
	public RUISInputManager inputManager;
	private bool kinectChecked = false, kinect2Checked = false, calibrationFinnished = false;
	List<GameObject> calibrationSpheres;
	private GameObject calibrationPhaseObjects, calibrationResultPhaseObjects, kinect1ModelObject, 
	kinect2ModelObject, floorPlane, calibrationSphere, calibrationCube, depthView, depthView2,
	Kinect1Icon, Kinect2Icon, deviceModelObjects, depthViewObjects, iconObjects;
	
	private Vector3 lastPSMoveSample, lastKinect2Sample, lastKinectSample;
	private string xmlFilename;
	Quaternion kinect1PitchRotation = Quaternion.identity;
//	Quaternion kinect2PitchRotation = Quaternion.identity;
	float kinect1DistanceFromFloor = 0;
	float kinect2DistanceFromFloor = 0;
	Vector3 kinect1FloorNormal = Vector3.up;
	Vector3 kinect2FloorNormal = Vector3.up;
	
	private Matrix4x4 rotationMatrix, transformMatrix;
	
	Kinect2SourceManager kinect2SourceManager;
	Kinect.Body[] bodyData; 
	
	private trackedBody[] trackingIDs = null; // Defined in RUISKinect2DepthView
	private Dictionary<ulong, int> trackingIDtoIndex = new Dictionary<ulong, int>();
//	private int kinectTrackingIndex;
	private ulong kinectTrackingID;
	
	bool device1Error, device2Error;
	
	public RUISKinect2ToKinectCalibrationProcess(RUISCalibrationProcessSettings calibrationSettings) {
		
		this.inputDevice1 = RUISDevice.Kinect_2;
		this.inputDevice2 = RUISDevice.Kinect_1;
		
		this.numberOfSamplesToTake = calibrationSettings.numberOfSamplesToTake;
		this.numberOfSamplesPerSecond = calibrationSettings.numberOfSamplesPerSecond;
		
		trackingIDs = new trackedBody[6]; 
		for(int y = 0; y < trackingIDs.Length; y++) {
			trackingIDs[y] = new trackedBody(-1, false, 1);
		}
		
		kinectSelection = MonoBehaviour.FindObjectOfType(typeof(NIPlayerManagerCOMSelection)) as NIPlayerManagerCOMSelection;
		settingsManager = MonoBehaviour.FindObjectOfType(typeof(OpenNISettingsManager)) as OpenNISettingsManager;
		inputManager = MonoBehaviour.FindObjectOfType(typeof(RUISInputManager)) as RUISInputManager;
		coordinateSystem = MonoBehaviour.FindObjectOfType(typeof(RUISCoordinateSystem)) as RUISCoordinateSystem;
		kinect2SourceManager = MonoBehaviour.FindObjectOfType(typeof(Kinect2SourceManager)) as Kinect2SourceManager;
		
		this.timeSinceScriptStart = 0;
		this.timeBetweenSamples = 1 / (float)numberOfSamplesPerSecond;
		
		// Limit sample rate
		if(this.timeBetweenSamples < 0.1f) {
			this.timeBetweenSamples = 0.1f;
		}
		
		calibrationSpheres = new List<GameObject>();
		
		samples_Kinect1 = new List<Vector3>();
		samples_Kinect2 = new List<Vector3>();
		
		this.calibrationCube = calibrationSettings.calibrationCubePrefab;
		this.calibrationSphere = calibrationSettings.calibrationSpherePrefab;
		this.calibrationPhaseObjects = calibrationSettings.calibrationPhaseObjects;
		this.calibrationResultPhaseObjects = calibrationSettings.calibrationResultPhaseObjects;
		
		this.deviceModelObjects = calibrationSettings.deviceModelObjects;
		this.depthViewObjects = calibrationSettings.depthViewObjects;
		this.iconObjects = calibrationSettings.iconObjects;
		
		if(GameObject.Find ("PSMoveWand") != null)
			GameObject.Find ("PSMoveWand").SetActive(false);
		
		// Models
		this.kinect1ModelObject = GameObject.Find ("KinectCamera");
		this.kinect2ModelObject = GameObject.Find ("Kinect2Camera");
		
		RUISSkeletonController skeletonController = Component.FindObjectOfType<RUISSkeletonController>();
		Transform rightHand = null;
		if(skeletonController)
			rightHand = skeletonController.rightHand;
		FixedFollowTransform followTransform = Component.FindObjectOfType<FixedFollowTransform>();
		if(followTransform && rightHand)
			followTransform.transformToFollow = rightHand;
		
		// Depth view
		this.depthView = GameObject.Find ("KinectDepthView");
		this.depthView2 = GameObject.Find ("Kinect2DepthView");
		// Icons
		this.Kinect1Icon = GameObject.Find ("Kinect Icon");
		this.Kinect2Icon = GameObject.Find ("Kinect2 Icon");

		this.floorPlane = GameObject.Find ("Floor");

		if(this.Kinect1Icon && this.Kinect1Icon.GetComponent<GUITexture>())
			this.Kinect1Icon.GetComponent<GUITexture>().pixelInset = new Rect(5.1f, 10.0f, 70.0f, 70.0f);
		
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
		
		if(this.kinect1ModelObject)
			this.kinect1ModelObject.SetActive(true);
		if(this.kinect2ModelObject)
			this.kinect2ModelObject.SetActive(true);
		if(this.Kinect1Icon)
			this.Kinect1Icon.SetActive(true);
		if(this.Kinect2Icon)
			this.Kinect2Icon.SetActive(true);
		if(this.calibrationPhaseObjects)
			this.calibrationPhaseObjects.SetActive(true);
		if(this.calibrationResultPhaseObjects)
			this.calibrationResultPhaseObjects.SetActive(false);
		if(this.depthView)
			this.depthView.SetActive(true);
		if(this.depthView2)
			this.depthView2.SetActive(true);
		this.xmlFilename = calibrationSettings.xmlFilename;
	}
	
	
	public override RUISCalibrationPhase InitialPhase(float deltaTime) {
		
		timeSinceScriptStart += deltaTime;
		
		if(timeSinceScriptStart < 3) {
			this.guiTextLowerLocal = "Calibration of Kinect 1 and Kinect 2\n\n Starting up...";
			return RUISCalibrationPhase.Initial;
		}
		
		if(timeSinceScriptStart < 4) {
			this.guiTextLowerLocal = "Connecting to Kinect 1. \n\n Please wait...";
			return RUISCalibrationPhase.Initial;
		}
		 
		if(!kinectChecked && timeSinceScriptStart > 4) {
			if (settingsManager == null) {
				this.guiTextLowerLocal = "Connecting to Kinect 1. \n\n Error: Could not start OpenNI";
				return RUISCalibrationPhase.Invalid;
			}
			else if(settingsManager.UserGenrator == null) {
				this.guiTextLowerLocal = "Connecting to Kinect 1. \n\n Error: Could not start OpenNI";
				return RUISCalibrationPhase.Invalid;
			}
			else if(!settingsManager.UserGenrator.Valid) {
				this.guiTextLowerLocal = "Connecting to Kinect 1. \n\n Error: Could not start OpenNI";
				return RUISCalibrationPhase.Invalid;
			}
			else {
				sceneAnalyzer = new OpenNI.SceneAnalyzer((MonoBehaviour.FindObjectOfType(typeof(OpenNISettingsManager)) as OpenNISettingsManager).CurrentContext.BasicContext);
				sceneAnalyzer.StartGenerating();
			}
			kinectChecked = true;	
		}	
		
		if(timeSinceScriptStart < 5) {
			this.guiTextLowerLocal = "Connecting to Kinect 2. \n\n Please wait...";
			return RUISCalibrationPhase.Initial;
		}
		
		if(!kinect2Checked && timeSinceScriptStart > 5) {
			kinect2Checked = true;	
			if (!kinect2SourceManager.GetSensor().IsOpen || !kinect2SourceManager.GetSensor().IsAvailable) {
				this.guiTextLowerLocal = "Connecting to Kinect 2. \n\n Error: Could not connect to Kinect 2.";
				return RUISCalibrationPhase.Invalid;
			}
			else {
				return RUISCalibrationPhase.Preparation;
			}
			
		}	
		
		return RUISCalibrationPhase.Invalid; // Loop should not get this far
	}
	
	
	public override RUISCalibrationPhase PreparationPhase(float deltaTime) {
		this.guiTextLowerLocal = "Step in front of the camera.";
		updateBodyData();
		kinectTrackingID = 0;
		for(int a = 0; a < trackingIDs.Length; a++) {
			if(trackingIDs[a].isTracking) {
				kinectTrackingID = trackingIDs[a].trackingId;
//				kinectTrackingIndex = trackingIDs[a].index;
			}
		}
		
		if(kinectTrackingID != 0) return RUISCalibrationPhase.ReadyToCalibrate;
		else return RUISCalibrationPhase.Preparation;
		
	}
	
	
	public override RUISCalibrationPhase ReadyToCalibratePhase(float deltaTime) {
			return RUISCalibrationPhase.Calibration;
	}
	
	
	public override RUISCalibrationPhase CalibrationPhase(float deltaTime) {
		
		this.guiTextLowerLocal = string.Format(  "Calibrating... {0}/{1} samples taken. \n\nMake wide, calm motions with your\n"
		                                       + "right hand. Have both Kinects see it.", 
		                                       numberOfSamplesTaken, numberOfSamplesToTake);
		TakeSample(deltaTime);
		
		if(numberOfSamplesTaken >= numberOfSamplesToTake) 
		{
			timeSinceScriptStart = 0;
			this.calibrationPhaseObjects.SetActive(false);
			this.calibrationResultPhaseObjects.SetActive(true);
			this.depthView.SetActive(false);
			return RUISCalibrationPhase.ShowResults;
		}
		else 
		{ 
			return RUISCalibrationPhase.Calibration;
		}
	}
	
	
	public override RUISCalibrationPhase ShowResultsPhase(float deltaTime) 
	{
		if(!calibrationFinnished) 
		{
			float totalErrorDistance, averageError;
			CalculateTransformation();
			
			float distance = 0;
			Vector3 error = Vector3.zero;
			List<float> errorMagnitudes = new List<float>();
			for (int i = 0; i < calibrationSpheres.Count; i++)
			{
				GameObject sphere = calibrationSpheres[i];
				Vector3 cubePosition =  transformMatrix.MultiplyPoint3x4(samples_Kinect2[i]);
				GameObject cube = MonoBehaviour.Instantiate(calibrationCube, cubePosition, Quaternion.identity) as GameObject;
				cube.GetComponent<RUISSampleDifferenceVisualizer>().kinectCalibrationSphere = sphere;
				
				
				distance += Vector3.Distance(sphere.transform.position, cubePosition);
				errorMagnitudes.Add(distance);
				error += cubePosition - sphere.transform.position;
				
				sphere.transform.parent = calibrationResultPhaseObjects.transform;
				cube.transform.parent = calibrationResultPhaseObjects.transform;
			}
			
			totalErrorDistance = distance;
			averageError = distance / calibrationSpheres.Count;
			
			calibrationResultPhaseObjects.SetActive(true);
			
			
			this.guiTextUpperLocal = string.Format("Calibration finished!\n\nTotal Error: {0:0.####}\nMean: {1:0.####}\n",
			                                       totalErrorDistance, averageError);
			
			calibrationFinnished = true;                                  
		}
		return RUISCalibrationPhase.ShowResults;
	}
	
	public static Quaternion QuaternionFromMatrix(Matrix4x4 m) {
		// Source: http://answers.unity3d.com/questions/11363/converting-matrix4x4-to-quaternion-vector3.html
		// Adapted from: http://www.euclideanspace.com/maths/geometry/rotations/conversions/matrixToQuaternion/index.htm
		Quaternion q = new Quaternion();
		q.w = Mathf.Sqrt( Mathf.Max( 0, 1 + m[0,0] + m[1,1] + m[2,2] ) ) / 2;
		q.x = Mathf.Sqrt( Mathf.Max( 0, 1 + m[0,0] - m[1,1] - m[2,2] ) ) / 2;
		q.y = Mathf.Sqrt( Mathf.Max( 0, 1 - m[0,0] + m[1,1] - m[2,2] ) ) / 2;
		q.z = Mathf.Sqrt( Mathf.Max( 0, 1 - m[0,0] - m[1,1] + m[2,2] ) ) / 2;
		q.x *= Mathf.Sign( q.x * ( m[2,1] - m[1,2] ) );
		q.y *= Mathf.Sign( q.y * ( m[0,2] - m[2,0] ) );
		q.z *= Mathf.Sign( q.z * ( m[1,0] - m[0,1] ) );
		return q;
	}
	
	// Custom functionsRUISCalibrationPhase.Stopped
	private void TakeSample(float deltaTime)
	{
		timeSinceLastSample += deltaTime;
		if(timeSinceLastSample < timeBetweenSamples) return;
		timeSinceLastSample = 0;
		
		
		Vector3 Kinect2_sample = getSample (this.inputDevice1);
		Vector3 Kinect1_sample = getSample (this.inputDevice2);
		
//		if(Kinect2_sample == null || Kinect1_sample == null) return; // No data from device
		if (Kinect2_sample == Vector3.zero || Kinect1_sample == Vector3.zero) //Data not valid
		{
			return;
		}
		
		samples_Kinect1.Add(Kinect1_sample);
		samples_Kinect2.Add(Kinect2_sample);
		calibrationSpheres.Add(MonoBehaviour.Instantiate(calibrationSphere, Kinect1_sample, Quaternion.identity) as GameObject);
		numberOfSamplesTaken++;
	} 
	
	
	private Vector3 getSample(RUISDevice device) {
		Vector3 sample = new Vector3(0,0,0);
		Vector3 tempSample;
		updateBodyData();
		if(device == RUISDevice.Kinect_2) 
		{
			Kinect.Body[] data = kinect2SourceManager.GetBodyData();
			bool trackedBodyFound = false;
			int foundBodies = 0;
			foreach(var body in data) 
			{
				foundBodies++;
				if(body.IsTracked)
				{
					if(trackingIDtoIndex[body.TrackingId] == 0) 
					{
						trackedBodyFound = true;
						if(body.Joints[Kinect.JointType.HandRight].TrackingState == Kinect.TrackingState.Tracked) 
						{
							tempSample = new Vector3(body.Joints[Kinect.JointType.HandRight].Position.X,
							                         body.Joints[Kinect.JointType.HandRight].Position.Y,
							                         body.Joints[Kinect.JointType.HandRight].Position.Z);
							tempSample = coordinateSystem.ConvertRawKinect2Location(tempSample);
							if(Vector3.Distance(tempSample, lastKinect2Sample) > 0.1) 
							{
								sample = tempSample;
								lastKinect2Sample = sample;
								device1Error = false;
								if(!device2Error) this.guiTextUpperLocal = "";
							}
							else 
							{
								device1Error = true;
								this.guiTextUpperLocal = "Not enough hand movement.";
							}
						}
					}
				}
				
			}
			if(!trackedBodyFound && foundBodies > 1) 
			{
				device1Error = true;
				this.guiTextUpperLocal = "Step out of the Kinect's\nview and come back.";
			}
			
		}
		if(device == RUISDevice.Kinect_1) 
		{
			OpenNI.SkeletonJointPosition jointPosition;
			bool success = kinectSelection.GetPlayer(0).GetSkeletonJointPosition(OpenNI.SkeletonJoint.RightHand, out jointPosition);
			if(success && jointPosition.Confidence >= 0.5) 
			{ 
				tempSample = coordinateSystem.ConvertRawKinectLocation(jointPosition.Position);
				if(Vector3.Distance(tempSample, lastKinectSample) > 0.1) 
				{
					sample = tempSample;
					lastKinectSample = sample;
					device2Error = false;
					if(!device1Error) this.guiTextUpperLocal = "";
				}
				else {
					device2Error = true;
					this.guiTextUpperLocal = "Not enough hand movement.";
				}
			}
			else 
			{
				device2Error = true;
				this.guiTextUpperLocal = "Not enough hand movement.";
			}
		}
		return sample;
		
		
	}
	
	private void CalculateTransformation()
	{
		if (samples_Kinect1.Count != numberOfSamplesTaken || samples_Kinect2.Count != numberOfSamplesTaken)
		{
			Debug.LogError("Mismatch in sample list lengths!");
		}
		
		Matrix kinect2Matrix;
		Matrix kinect1Matrix;
		
		kinect2Matrix = Matrix.Zeros (samples_Kinect2.Count, 4);
		kinect1Matrix = Matrix.Zeros (samples_Kinect1.Count, 3);
		
		for (int i = 1; i <= samples_Kinect2.Count; i++) {
			kinect2Matrix [i, 1] = new Complex (samples_Kinect2 [i - 1].x);
			kinect2Matrix [i, 2] = new Complex (samples_Kinect2 [i - 1].y);
			kinect2Matrix [i, 3] = new Complex (samples_Kinect2 [i - 1].z);
			kinect2Matrix [i, 4] = new Complex (1.0f);
		}
		for (int i = 1; i <= samples_Kinect1.Count; i++) {
			kinect1Matrix [i, 1] = new Complex (samples_Kinect1 [i - 1].x);
			kinect1Matrix [i, 2] = new Complex (samples_Kinect1 [i - 1].y);
			kinect1Matrix [i, 3] = new Complex (samples_Kinect1 [i - 1].z);
		}
		
		//perform a matrix solve Ax = B. We have to get transposes and inverses because moveMatrix isn't square
		//the solution is the same with (A^T)Ax = (A^T)B -> x = ((A^T)A)'(A^T)B
		Matrix transformMatrixSolution = (kinect2Matrix.Transpose() * kinect2Matrix).Inverse() * kinect2Matrix.Transpose() * kinect1Matrix;
		
//		Matrix error = kinect2Matrix * transformMatrixSolution - kinect1Matrix;
		
		transformMatrixSolution = transformMatrixSolution.Transpose();
		
//		Debug.Log(transformMatrixSolution);
//		Debug.Log(error);
		
		List<Vector3> orthogonalVectors = MathUtil.Orthonormalize(
			MathUtil.ExtractRotationVectors(
			MathUtil.MatrixToMatrix4x4(transformMatrixSolution)
			)
			);
		rotationMatrix = CreateRotationMatrix(orthogonalVectors);
		//Debug.Log(rotationMatrix);
		
		transformMatrix = MathUtil.MatrixToMatrix4x4(transformMatrixSolution);
		Debug.Log("transformMatrix \n" + transformMatrix);
		
		UpdateFloorNormalAndDistance();
		
		coordinateSystem.SetDeviceToRootTransforms(transformMatrix);
		coordinateSystem.SaveTransformDataToXML(xmlFilename,RUISDevice.Kinect_2, RUISDevice.Kinect_1); 
		coordinateSystem.SaveFloorData(xmlFilename, RUISDevice.Kinect_1, kinect1FloorNormal, kinect1DistanceFromFloor);
		coordinateSystem.SaveFloorData(xmlFilename, RUISDevice.Kinect_2, kinect2FloorNormal, kinect2DistanceFromFloor);
		
		Quaternion rotationQuaternion = MathUtil.QuaternionFromMatrix(rotationMatrix);
		Vector3 translate = new Vector3(transformMatrix[0, 3], transformMatrix[1, 3], transformMatrix[2, 3]);
		updateDictionaries(coordinateSystem.RUISCalibrationResultsInVector3, 
		                   coordinateSystem.RUISCalibrationResultsInQuaternion,
		                   coordinateSystem.RUISCalibrationResultsIn4x4Matrix,
		                   translate, rotationQuaternion, transformMatrix,
		                   RUISDevice.Kinect_2, RUISDevice.Kinect_1);
		                   
		coordinateSystem.RUISCalibrationResultsDistanceFromFloor[RUISDevice.Kinect_1] = kinect1DistanceFromFloor;
		coordinateSystem.RUISCalibrationResultsFloorPitchRotation[RUISDevice.Kinect_1] = kinect1PitchRotation;   
		
		kinect1ModelObject.transform.rotation = kinect1PitchRotation;
		kinect1ModelObject.transform.localPosition = new Vector3(0, kinect1DistanceFromFloor, 0);
		
//		kinect2ModelObject.transform.position = transformMatrix.MultiplyPoint3x4(kinect2ModelObject.transform.position);
//		kinect2ModelObject.transform.rotation = QuaternionFromMatrix(rotationMatrix) * kinect1PitchRotation;
//		kinect2ModelObject.transform.localPosition += new Vector3(0, kinect1DistanceFromFloor, 0);
		kinect2ModelObject.transform.position = coordinateSystem.ConvertLocation(Vector3.zero, RUISDevice.Kinect_2);
		kinect2ModelObject.transform.rotation = coordinateSystem.ConvertRotation(Quaternion.identity, RUISDevice.Kinect_2);
		
		if(this.floorPlane)
			this.floorPlane.transform.position = new Vector3(0, 0, 0);
	}
	
	
	private static Matrix4x4 CreateRotationMatrix(List<Vector3> vectors)
	{
		Matrix4x4 result = new Matrix4x4();
		result.SetColumn(0, new Vector4(vectors[0].x, vectors[0].y, vectors[0].z, 0));
		result.SetColumn(1, new Vector4(vectors[1].x, vectors[1].y, vectors[1].z, 0));
		result.SetColumn(2, new Vector4(vectors[2].x, vectors[2].y, vectors[2].z, 0));
		
		result[3, 3] = 1.0f;
		
		return result;
	}
	
	private static Matrix4x4 CreateTransformMatrix(Matrix transformMatrix)
	{
		Matrix4x4 result = new Matrix4x4();
		
		result.SetRow(0, new Vector4((float)transformMatrix[1, 1].Re, (float)transformMatrix[1, 2].Re, (float)transformMatrix[1, 3].Re, (float)transformMatrix[4, 1].Re));
		result.SetRow(1, new Vector4((float)transformMatrix[2, 1].Re, (float)transformMatrix[2, 2].Re, (float)transformMatrix[2, 3].Re, (float)transformMatrix[4, 2].Re));
		result.SetRow(2, new Vector4((float)transformMatrix[3, 1].Re, (float)transformMatrix[3, 2].Re, (float)transformMatrix[3, 3].Re, (float)transformMatrix[4, 3].Re));
		
		result.m33 = 1.0f;
		
		return result;
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
		
		if(float.IsNaN(kinect2DistanceFromFloor))
			kinect2DistanceFromFloor = 0;

//		Quaternion kinect2FloorRotator = Quaternion.FromToRotation(kinect2FloorNormal, Vector3.up); 
		
//		kinect2PitchRotation = Quaternion.Inverse (kinect2FloorRotator);
		
		coordinateSystem.SetDistanceFromFloor(kinect2DistanceFromFloor, RUISDevice.Kinect_2);
		coordinateSystem.SetFloorNormal(kinect2FloorNormal, RUISDevice.Kinect_2);
		
		
		OpenNI.Plane3D floor;
		
		try{
			floor = sceneAnalyzer.Floor;
		}
		catch(System.Exception e)
		{
			Debug.LogError(e.TargetSite + ": Failed to get OpenNI.SceneAnalyzer.Floor.");
			return;
			//throw e;
		}
		
		Quaternion kinectFloorRotator = Quaternion.identity;
		kinect1FloorNormal = new Vector3(floor.Normal.X, floor.Normal.Y, floor.Normal.Z);
		
		if(kinect1FloorNormal.sqrMagnitude < 0.1f)
			kinect1FloorNormal = Vector3.up;

		Vector3 floorPoint = new Vector3(floor.Point.X, floor.Point.Y, floor.Point.Z);
		kinectFloorRotator = Quaternion.FromToRotation(kinect1FloorNormal, Vector3.up); 
		kinect1DistanceFromFloor = closestDistanceFromFloor(kinect1FloorNormal, floorPoint, RUISCoordinateSystem.kinectToUnityScale);
		kinect1PitchRotation = Quaternion.Inverse (kinectFloorRotator);
		
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

		if(float.IsNaN(closestDistanceFromFloor))
			closestDistanceFromFloor = 0;

		return closestDistanceFromFloor;
	}
	
	private void updateBodyData() {
		
		bodyData = kinect2SourceManager.GetBodyData();
		
		if(bodyData != null) {
			// Update tracking ID array
			for(int y = 0; y < trackingIDs.Length; y++) {
				trackingIDs[y].isTracking = false; 
				trackingIDs[y].index = -1;
			}
			
			// Check tracking status and assing old indexes
			var arrayIndex = 0;
			foreach(var body in bodyData) {
				
				if(body.IsTracked) {
					for(int y = 0; y < trackingIDs.Length; y++) {
						if(trackingIDs[y].trackingId == body.TrackingId) { // Body found in tracking IDs array
							trackingIDs[y].isTracking = true;			   // Reset as tracked
							trackingIDs[y].kinect2ArrayIndex = arrayIndex; // Set current kinect2 array index
							
							if(trackingIDtoIndex.ContainsKey(body.TrackingId)) { // If key added to trackingIDtoIndex array earlier...
								trackingIDs[y].index = trackingIDtoIndex[body.TrackingId]; // Set old index
							}
						}
					}
					
				}
				
				
				arrayIndex++;
			}
			
			// Add new bodies
			arrayIndex = 0;
			foreach(var body in bodyData) {
				if(body.IsTracked) {
					if(!trackingIDtoIndex.ContainsKey(body.TrackingId)) { // A new body
						for(int y = 0; y < trackingIDs.Length; y++) {
							if(!trackingIDs[y].isTracking) {			// Find an array slot that does not have a tracked body
								trackingIDs[y].index = y;				// Set index to trackingIDs array index
								trackingIDs[y].trackingId = body.TrackingId;	
								trackingIDtoIndex[body.TrackingId] = y;		// Add tracking id to trackingIDtoIndex array
								trackingIDs[y].kinect2ArrayIndex = arrayIndex;
								trackingIDs[y].isTracking = true;
								break;
							}
						}	
					}
				}	
				arrayIndex++;	
			}
		}
	}	
}
