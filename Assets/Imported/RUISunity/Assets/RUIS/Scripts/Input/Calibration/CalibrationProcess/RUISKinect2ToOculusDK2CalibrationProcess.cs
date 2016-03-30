using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using CSML;
using Kinect = Windows.Kinect;
using Ovr;

public class RUISKinect2ToOculusDK2CalibrationProcess : RUISCalibrationProcess {
	
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
	private List<Vector3> samples_Kinect2, samples_OculusDK2;
	private int numberOfSamplesTaken, numberOfSamplesToTake, numberOfSamplesPerSecond;
	private float timeSinceLastSample, timeBetweenSamples, timeSinceScriptStart = 0;
	public RUISCoordinateSystem coordinateSystem;
	public RUISInputManager inputManager;
	private bool oculusChecked = false, kinect2Checked = false, calibrationFinnished = false;
	List<GameObject> calibrationSpheres;
	private GameObject calibrationPhaseObjects, calibrationResultPhaseObjects, oculusDK2CameraObject, 
	kinect2ModelObject, floorPlane, calibrationSphere, calibrationCube, depthView,
	oculusDK2Icon, kinect2Icon, deviceModelObjects, depthViewObjects, iconObjects,
	oculusRiftModel;
	
	private Vector3 lastPSMoveSample, lastKinect2Sample, lastOculusDK2Sample;
	private string xmlFilename;
	
	private Matrix4x4 rotationMatrix, transformMatrix;
	
	Kinect2SourceManager kinect2SourceManager;
	Kinect.Body[] bodyData; 
	
	private trackedBody[] trackingIDs = null; // Defined in RUISKinect2DepthView
	private Dictionary<ulong, int> trackingIDtoIndex = new Dictionary<ulong, int>();
//	private int kinectTrackingIndex;
	private ulong kinectTrackingID;
	
	Quaternion kinect2PitchRotation = Quaternion.identity;
	float kinect2DistanceFromFloor = 0;
	Vector3 kinect2FloorNormal = Vector3.up;
	RUISOVRManager ruisOvrManager;

	bool device1Error, device2Error;
	
	public RUISKinect2ToOculusDK2CalibrationProcess(RUISCalibrationProcessSettings calibrationSettings) {
		
		this.inputDevice1 = RUISDevice.Oculus_DK2;
		this.inputDevice2 = RUISDevice.Kinect_2;
		
		this.numberOfSamplesToTake = calibrationSettings.numberOfSamplesToTake;
		this.numberOfSamplesPerSecond = calibrationSettings.numberOfSamplesPerSecond;
		
		trackingIDs = new trackedBody[6]; 
		for(int y = 0; y < trackingIDs.Length; y++) {
			trackingIDs[y] = new trackedBody(-1, false, 1);
		}
		
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
		
		samples_Kinect2 = new List<Vector3>();
		samples_OculusDK2 = new List<Vector3>();
		
		this.calibrationCube = calibrationSettings.calibrationCubePrefab;
		this.calibrationSphere = calibrationSettings.calibrationSpherePrefab;
		this.calibrationPhaseObjects = calibrationSettings.calibrationPhaseObjects;
		this.calibrationResultPhaseObjects = calibrationSettings.calibrationResultPhaseObjects;
		
		this.deviceModelObjects = calibrationSettings.deviceModelObjects;
		this.depthViewObjects = calibrationSettings.depthViewObjects;
		this.iconObjects = calibrationSettings.iconObjects;
		
		if(GameObject.Find ("PSMoveWand") != null) GameObject.Find ("PSMoveWand").SetActive(false);
		
		// Models
		this.oculusDK2CameraObject = GameObject.Find ("OculusDK2Camera");
		this.kinect2ModelObject = GameObject.Find ("Kinect2Camera");
		this.oculusRiftModel = GameObject.Find ("OculusRift");
		
		FixedFollowTransform followTransform = Component.FindObjectOfType<FixedFollowTransform>();
		if(followTransform && this.oculusRiftModel)
			followTransform.transformToFollow = this.oculusRiftModel.transform;
		
		// Depth view
		this.depthView = GameObject.Find ("Kinect2DepthView");
		
		// Icons
		this.oculusDK2Icon = GameObject.Find ("OculusDK2 Icon");
		this.kinect2Icon = GameObject.Find ("Kinect2 Icon");
		
		this.floorPlane = GameObject.Find ("Floor");
		
		if(this.oculusDK2Icon && this.oculusDK2Icon.GetComponent<GUITexture>())
			this.oculusDK2Icon.GetComponent<GUITexture>().pixelInset = new Rect(5.1f, 10.0f, 70.0f, 70.0f);
		
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
		
		if(this.oculusDK2CameraObject)
			this.oculusDK2CameraObject.SetActive(true);
		if(this.kinect2ModelObject)
			this.kinect2ModelObject.SetActive(true);
		if(this.oculusRiftModel)
			this.oculusRiftModel.SetActive(true);
		if(this.oculusDK2Icon)
			this.oculusDK2Icon.SetActive(true);
		if(this.kinect2Icon)
			this.kinect2Icon.SetActive(true);
		if(this.calibrationPhaseObjects)
			this.calibrationPhaseObjects.SetActive(true);
		if(this.calibrationResultPhaseObjects)
			this.calibrationResultPhaseObjects.SetActive(false);
		if(this.depthView)
			this.depthView.SetActive(true);
		this.xmlFilename = calibrationSettings.xmlFilename;
	}
	
	
	public override RUISCalibrationPhase InitialPhase(float deltaTime) {
		
		timeSinceScriptStart += deltaTime;
		
		if(timeSinceScriptStart < 3) {
			this.guiTextLowerLocal = "Calibration of Kinect 2 and Oculus DK2\n\n Starting up...";
			return RUISCalibrationPhase.Initial;
		}
		
		if(timeSinceScriptStart < 4) {
			this.guiTextLowerLocal = "Connecting to Oculus Rift DK2. \n\n Please wait...";
			return RUISCalibrationPhase.Initial;
		}
		
		if(!oculusChecked && timeSinceScriptStart > 4) {
			oculusChecked = true;
			
			if ((RUISOVRManager.ovrHmd.GetTrackingState().StatusFlags & (uint)StatusBits.HmdConnected) == 0) { // Code from OVRManager.cs
				this.guiTextLowerLocal = "Connecting to Oculus Rift DK2. \n\n Error: Could not connect to Oculus Rift DK2.";
				return RUISCalibrationPhase.Invalid;
			}
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
		this.guiTextLowerLocal = "Step in front of the camera. \nHold Oculus Rift in your right hand.";
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
		
		this.guiTextLowerLocal = string.Format(  "Calibrating... {0}/{1} samples taken.\n\n"
		                                       + "Keep the Oculus Rift in your right hand\n"
		                                       + "and make wide, calm motions with it.\n"
		                                       + "Have both sensors see it.", numberOfSamplesTaken, numberOfSamplesToTake);
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
				Vector3 cubePosition =  transformMatrix.MultiplyPoint3x4(samples_OculusDK2[i]);
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
		
		
		Vector3 kinect2_sample = getSample (this.inputDevice1);
		Vector3 oculusDK2_sample = getSample (this.inputDevice2);
		
		if (kinect2_sample == Vector3.zero || oculusDK2_sample == Vector3.zero) //Data not valid
		{
			return;
		}
		
		samples_Kinect2.Add(oculusDK2_sample);
		samples_OculusDK2.Add(kinect2_sample);
		calibrationSpheres.Add(MonoBehaviour.Instantiate(calibrationSphere, oculusDK2_sample, Quaternion.identity) as GameObject);
		numberOfSamplesTaken++;
	} 
	
	
	private Vector3 getSample(RUISDevice device) 
	{
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
		if(device == RUISDevice.Oculus_DK2)
		{
			Ovr.Posef headpose = RUISOVRManager.ovrHmd.GetTrackingState().HeadPose.ThePose;
			float px =  headpose.Position.x;
			float py =  headpose.Position.y;
			float pz = -headpose.Position.z; // This needs to be negated TODO: might change with future OVR version
			
			tempSample = new Vector3(px, py, pz);
			tempSample = coordinateSystem.ConvertRawOculusDK2Location(tempSample);
			
			if(   (Vector3.Distance(tempSample, lastOculusDK2Sample) > 0.1) 
			   && (RUISOVRManager.ovrHmd.GetTrackingState().StatusFlags & (uint)StatusBits.PositionTracked) != 0)  // Code from OVRManager.cs
			{
				sample = tempSample;
				lastOculusDK2Sample = sample;
				device2Error = false;
				if(!device1Error) this.guiTextUpperLocal = "";
				
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
		if (samples_Kinect2.Count != numberOfSamplesTaken || samples_OculusDK2.Count != numberOfSamplesTaken)
		{
			Debug.LogError("Mismatch in sample list lengths!");
		}
		
		Matrix oculusDK2Matrix;
		Matrix kinect2Matrix;
		
		oculusDK2Matrix = Matrix.Zeros (samples_OculusDK2.Count, 4);
		kinect2Matrix = Matrix.Zeros (samples_Kinect2.Count, 3);
		
		for (int i = 1; i <= samples_OculusDK2.Count; i++) {
			oculusDK2Matrix [i, 1] = new Complex (samples_OculusDK2 [i - 1].x);
			oculusDK2Matrix [i, 2] = new Complex (samples_OculusDK2 [i - 1].y);
			oculusDK2Matrix [i, 3] = new Complex (samples_OculusDK2 [i - 1].z);
			oculusDK2Matrix [i, 4] = new Complex (1.0f);
		}
		for (int i = 1; i <= samples_Kinect2.Count; i++) {
			kinect2Matrix [i, 1] = new Complex (samples_Kinect2 [i - 1].x);
			kinect2Matrix [i, 2] = new Complex (samples_Kinect2 [i - 1].y);
			kinect2Matrix [i, 3] = new Complex (samples_Kinect2 [i - 1].z);
		}
		
		//perform a matrix solve Ax = B. We have to get transposes and inverses because moveMatrix isn't square
		//the solution is the same with (A^T)Ax = (A^T)B -> x = ((A^T)A)'(A^T)B
		Matrix transformMatrixSolution = (oculusDK2Matrix.Transpose() * oculusDK2Matrix).Inverse() * oculusDK2Matrix.Transpose() * kinect2Matrix;
		
		Matrix error = oculusDK2Matrix * transformMatrixSolution - kinect2Matrix;
		
		transformMatrixSolution = transformMatrixSolution.Transpose();
		
		Debug.Log(transformMatrixSolution);
		Debug.Log(error);
		
		List<Vector3> orthogonalVectors = MathUtil.Orthonormalize(
			MathUtil.ExtractRotationVectors(
			MathUtil.MatrixToMatrix4x4(transformMatrixSolution)
			)
			);
		rotationMatrix = CreateRotationMatrix(orthogonalVectors);
		Debug.Log(rotationMatrix);
		
		transformMatrix = MathUtil.MatrixToMatrix4x4(transformMatrixSolution);//CreateTransformMatrix(transformMatrixSolution);
		Debug.Log(transformMatrix);
		
		UpdateFloorNormalAndDistance(); 
		
		coordinateSystem.SetDeviceToRootTransforms(transformMatrix);
		coordinateSystem.SaveTransformDataToXML(xmlFilename, RUISDevice.Oculus_DK2,  RUISDevice.Kinect_2); 
		coordinateSystem.SaveFloorData(xmlFilename, RUISDevice.Kinect_2, kinect2FloorNormal, kinect2DistanceFromFloor);
		
		/*
		string devicePairName = RUISDevice.Kinect_2.ToString() + "-" + RUISDevice.Oculus_DK2.ToString();
		coordinateSystem.RUISCalibrationResultsIn4x4Matrix[devicePairName] = transformMatrix;
		
		*/
		Quaternion rotationQuaternion = MathUtil.QuaternionFromMatrix(rotationMatrix);
		Vector3 translate = new Vector3(transformMatrix[0, 3], transformMatrix[1, 3], transformMatrix[2, 3]);
		updateDictionaries(coordinateSystem.RUISCalibrationResultsInVector3, 
		                   coordinateSystem.RUISCalibrationResultsInQuaternion,
		                   coordinateSystem.RUISCalibrationResultsIn4x4Matrix,
		                   translate, rotationQuaternion, transformMatrix,
		                   RUISDevice.Oculus_DK2, RUISDevice.Kinect_2);
		                   
		coordinateSystem.RUISCalibrationResultsDistanceFromFloor[RUISDevice.Kinect_2] = kinect2DistanceFromFloor;
		coordinateSystem.RUISCalibrationResultsFloorPitchRotation[RUISDevice.Kinect_2] = kinect2PitchRotation;     
		
		kinect2ModelObject.transform.rotation = kinect2PitchRotation;
		kinect2ModelObject.transform.localPosition = new Vector3(0, kinect2DistanceFromFloor, 0);
		
		oculusDK2CameraObject.transform.position = coordinateSystem.ConvertLocation(Vector3.zero, RUISDevice.Oculus_DK2);
		oculusDK2CameraObject.transform.rotation = coordinateSystem.ConvertRotation(Quaternion.identity, RUISDevice.Oculus_DK2);
		
		/*
		Quaternion rotationQuaternion = MathUtil.QuaternionFromMatrix(rotationMatrix);
		coordinateSystem.RUISCalibrationResultsInQuaternion[devicePairName] = rotationQuaternion;
		*/
		
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

		Quaternion kinect2FloorRotator = Quaternion.FromToRotation(kinect2FloorNormal, Vector3.up); 
		
		kinect2PitchRotation = Quaternion.Inverse (kinect2FloorRotator);
		coordinateSystem.SetDistanceFromFloor(kinect2DistanceFromFloor, RUISDevice.Kinect_2);
		coordinateSystem.SetFloorNormal(kinect2FloorNormal, RUISDevice.Kinect_2);
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
