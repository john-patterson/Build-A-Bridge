using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using CSML;
using Kinect = Windows.Kinect;
using Ovr;

public class RUISKinectToOculusDK2CalibrationProcess : RUISCalibrationProcess {
	
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
	private List<Vector3> samples_Kinect1, samples_OculusDK2;
	private int numberOfSamplesTaken, numberOfSamplesToTake, numberOfSamplesPerSecond;
	private float timeSinceLastSample, timeBetweenSamples, timeSinceScriptStart = 0;
	public RUISCoordinateSystem coordinateSystem;
	public RUISInputManager inputManager;
	private bool oculusChecked = false, kinectChecked = false, calibrationFinnished = false;
	List<GameObject> calibrationSpheres;
	private GameObject calibrationPhaseObjects, calibrationResultPhaseObjects, oculusDK2CameraObject, 
	kinect1ModelObject, floorPlane, calibrationSphere, calibrationCube, depthView,
	oculusDK2Icon, kinectIcon, deviceModelObjects, depthViewObjects, iconObjects, oculusRiftModel;
	
	private Vector3 lastPSMoveSample, lastKinectSample, lastOculusDK2Sample;
	private string xmlFilename;
	
	private Matrix4x4 rotationMatrix, transformMatrix;
	
	private trackedBody[] trackingIDs = null; // Defined in RUISKinect2DepthView
//	private Dictionary<ulong, int> trackingIDtoIndex = new Dictionary<ulong, int>();
	private int kinectTrackingIndex;
	private ulong kinectTrackingID;
	
	Quaternion kinect1PitchRotation = Quaternion.identity;
	float kinect1DistanceFromFloor = 0;
	Vector3 kinect1FloorNormal = Vector3.up;
	
	public RUISKinectToOculusDK2CalibrationProcess(RUISCalibrationProcessSettings calibrationSettings) {
				
		this.inputDevice1 = RUISDevice.Oculus_DK2;
		this.inputDevice2 = RUISDevice.Kinect_1;
		
		this.numberOfSamplesToTake = calibrationSettings.numberOfSamplesToTake;
		this.numberOfSamplesPerSecond = calibrationSettings.numberOfSamplesPerSecond;
		
		trackingIDs = new trackedBody[6]; 
		for(int y = 0; y < trackingIDs.Length; y++) {
			trackingIDs[y] = new trackedBody(-1, false, 1);
		}
		
		inputManager = MonoBehaviour.FindObjectOfType(typeof(RUISInputManager)) as RUISInputManager;
		coordinateSystem = MonoBehaviour.FindObjectOfType(typeof(RUISCoordinateSystem)) as RUISCoordinateSystem;
		kinectSelection = MonoBehaviour.FindObjectOfType(typeof(NIPlayerManagerCOMSelection)) as NIPlayerManagerCOMSelection;
		settingsManager = MonoBehaviour.FindObjectOfType(typeof(OpenNISettingsManager)) as OpenNISettingsManager;
		
		this.timeSinceScriptStart = 0;
		this.timeBetweenSamples = 1 / (float)numberOfSamplesPerSecond;
		
		// Limit sample rate
		if(this.timeBetweenSamples < 0.1f) {
			this.timeBetweenSamples = 0.1f;
		}
		
		calibrationSpheres = new List<GameObject>();
		
		samples_Kinect1 = new List<Vector3>();
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
		this.kinect1ModelObject = GameObject.Find ("KinectCamera");
		this.oculusRiftModel = GameObject.Find ("OculusRift");
		
		FixedFollowTransform followTransform = Component.FindObjectOfType<FixedFollowTransform>();
		if(followTransform && this.oculusRiftModel)
			followTransform.transformToFollow = this.oculusRiftModel.transform;
		
		// Depth view
		this.depthView = GameObject.Find ("KinectDepthView");
		
		// Icons
		this.oculusDK2Icon = GameObject.Find ("OculusDK2 Icon");
		this.kinectIcon = GameObject.Find ("Kinect Icon");
		
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
		if(this.kinect1ModelObject)
			this.kinect1ModelObject.SetActive(true);
		if(this.oculusRiftModel)
			this.oculusRiftModel.SetActive(true);
		if(this.oculusDK2Icon)
			this.oculusDK2Icon.SetActive(true);
		if(this.kinectIcon)
			this.kinectIcon.SetActive(true);
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
			this.guiTextLowerLocal = "Calibration of Kinect 1 and Oculus DK2\n\n Starting up...";
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
	
	
	public override RUISCalibrationPhase PreparationPhase(float deltaTime) {
		this.guiTextLowerLocal = "Step in front of the camera. \nHold Oculus Rift in your right hand.";
		
		if (kinectSelection.GetNumberOfSelectedPlayers() < 1) {
			return RUISCalibrationPhase.Preparation;
		}
		else return RUISCalibrationPhase.ReadyToCalibrate;
		
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
		
		samples_Kinect1.Add(oculusDK2_sample);
		samples_OculusDK2.Add(kinect2_sample);
		calibrationSpheres.Add(MonoBehaviour.Instantiate(calibrationSphere, oculusDK2_sample, Quaternion.identity) as GameObject);
		numberOfSamplesTaken++;
	} 
	
	
	private Vector3 getSample(RUISDevice device) 
	{
		Vector3 sample = new Vector3(0,0,0);
		Vector3 tempSample;
		
		if(device == RUISDevice.Kinect_1) {
			OpenNI.SkeletonJointPosition jointPosition;
			bool success = kinectSelection.GetPlayer(0).GetSkeletonJointPosition(OpenNI.SkeletonJoint.RightHand, out jointPosition);
			if(success && jointPosition.Confidence >= 0.5) { 
				tempSample = coordinateSystem.ConvertRawKinectLocation(jointPosition.Position);
				if(Vector3.Distance(tempSample, lastKinectSample) > 0.1) {
					sample = tempSample;
					lastKinectSample = sample;
					this.guiTextUpperLocal = "";
				}
				else {
					this.guiTextUpperLocal = "Not enough hand movement.";
				}
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
				this.guiTextUpperLocal = "";
			}
			else {
				this.guiTextUpperLocal = "Not enough hand movement.";
			}

		}
		
		return sample;
		
		
	}
	
	private void CalculateTransformation()
	{
		if (samples_Kinect1.Count != numberOfSamplesTaken || samples_OculusDK2.Count != numberOfSamplesTaken)
		{
			Debug.LogError("Mismatch in sample list lengths!");
		}
		
		Matrix oculusMatrix;
		Matrix kinectMatrix;
		
		oculusMatrix = Matrix.Zeros (samples_OculusDK2.Count, 4);
		kinectMatrix = Matrix.Zeros (samples_Kinect1.Count, 3);
		
		
		for (int i = 1; i <= samples_OculusDK2.Count; i++) {
			oculusMatrix [i, 1] = new Complex (samples_OculusDK2 [i - 1].x);
			oculusMatrix [i, 2] = new Complex (samples_OculusDK2 [i - 1].y);
			oculusMatrix [i, 3] = new Complex (samples_OculusDK2 [i - 1].z);
			oculusMatrix [i, 4] = new Complex (1.0f);
		}
		for (int i = 1; i <= samples_Kinect1.Count; i++) {
			kinectMatrix [i, 1] = new Complex (samples_Kinect1 [i - 1].x);
			kinectMatrix [i, 2] = new Complex (samples_Kinect1 [i - 1].y);
			kinectMatrix [i, 3] = new Complex (samples_Kinect1 [i - 1].z);
		}
		
		//perform a matrix solve Ax = B. We have to get transposes and inverses because moveMatrix isn't square
		//the solution is the same with (A^T)Ax = (A^T)B -> x = ((A^T)A)'(A^T)B
		Matrix transformMatrixSolution = (oculusMatrix.Transpose() * oculusMatrix).Inverse() * oculusMatrix.Transpose() * kinectMatrix;
		
		Matrix error = oculusMatrix * transformMatrixSolution - kinectMatrix;
		
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
		coordinateSystem.SaveTransformDataToXML(xmlFilename, RUISDevice.Oculus_DK2, RUISDevice.Kinect_1); 
		coordinateSystem.SaveFloorData(xmlFilename, RUISDevice.Kinect_1, kinect1FloorNormal, kinect1DistanceFromFloor);
		
		Quaternion rotationQuaternion = MathUtil.QuaternionFromMatrix(rotationMatrix);
		Vector3 translate = new Vector3(transformMatrix[0, 3], transformMatrix[1, 3], transformMatrix[2, 3]);
		updateDictionaries(coordinateSystem.RUISCalibrationResultsInVector3, 
		                   coordinateSystem.RUISCalibrationResultsInQuaternion,
		                   coordinateSystem.RUISCalibrationResultsIn4x4Matrix,
		                   translate, rotationQuaternion, transformMatrix,
		                   RUISDevice.Oculus_DK2, RUISDevice.Kinect_1);
		
		coordinateSystem.RUISCalibrationResultsDistanceFromFloor[RUISDevice.Kinect_1] = kinect1DistanceFromFloor;
		coordinateSystem.RUISCalibrationResultsFloorPitchRotation[RUISDevice.Kinect_1] = kinect1PitchRotation;     
		
		kinect1ModelObject.transform.rotation = kinect1PitchRotation;
		kinect1ModelObject.transform.localPosition = new Vector3(0, kinect1DistanceFromFloor, 0);
		
		oculusDK2CameraObject.transform.position = coordinateSystem.ConvertLocation(Vector3.zero, RUISDevice.Oculus_DK2);
		oculusDK2CameraObject.transform.rotation = coordinateSystem.ConvertRotation(Quaternion.identity, RUISDevice.Oculus_DK2);
		
		
		/*
		string devicePairName = RUISDevice.Kinect_1.ToString() + "-" + RUISDevice.Oculus_DK2.ToString();
		coordinateSystem.RUISCalibrationResultsIn4x4Matrix[devicePairName] = transformMatrix;
		
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
		coordinateSystem.ResetFloorNormal(RUISDevice.Kinect_1);
		
		OpenNI.Plane3D floor;
		
		try{
			floor = sceneAnalyzer.Floor;
		}
		catch(System.Exception e)
		{
			Debug.LogError(e.TargetSite + ": Failed to get OpenNI.SceneAnalyzer.Floor.");
			return;
		}
		
		Quaternion kinectFloorRotator = Quaternion.identity;
		kinect1FloorNormal= new Vector3(floor.Normal.X, floor.Normal.Y, floor.Normal.Z);
		
		if(kinect1FloorNormal.sqrMagnitude < 0.1f)
			kinect1FloorNormal = Vector3.up;

		Vector3 floorPoint = new Vector3(floor.Point.X, floor.Point.Y, floor.Point.Z);
		kinectFloorRotator = Quaternion.FromToRotation(kinect1FloorNormal, Vector3.up); 
		kinect1DistanceFromFloor = closestDistanceFromFloor(kinect1FloorNormal, floorPoint, RUISCoordinateSystem.kinectToUnityScale);

		if(float.IsNaN(kinect1DistanceFromFloor))
			kinect1DistanceFromFloor = 0;

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
		
		return closestDistanceFromFloor;
	}
		
}
