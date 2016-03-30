/*****************************************************************************

Content    :   Functionality to control a skeleton using Kinect
Authors    :   Mikael Matveinen, Tuukka Takala
Copyright  :   Copyright 2013 Tuukka Takala, Mikael Matveinen. All Rights reserved.
Licensing  :   RUIS is distributed under the LGPL Version 3 license.

******************************************************************************/

using UnityEngine;
using System.Collections.Generic;

public enum RUISAxis
{
	X, Y, Z
}

[AddComponentMenu("RUIS/Input/RUISSkeletonController")]
public class RUISSkeletonController : MonoBehaviour
{


    public Transform root;
    public Transform head;
    public Transform neck;
    public Transform torso;
    public Transform rightShoulder;
    public Transform rightElbow;
    public Transform rightHand;
    public Transform rightHip;
    public Transform rightKnee;
    public Transform rightFoot;
    public Transform leftShoulder;
    public Transform leftElbow;
    public Transform leftHand;
    public Transform leftHip;
    public Transform leftKnee;
    public Transform leftFoot;
	public Transform leftThumb;
	public Transform rightThumb;

	// Transform sources for custom motion tracking
	public Transform customRoot;
	public Transform customHead;
	public Transform customNeck;
	public Transform customTorso;
	public Transform customRightShoulder;
	public Transform customRightElbow;
	public Transform customRightHand;
	public Transform customRightHip;
	public Transform customRightKnee;
	public Transform customRightFoot;
	public Transform customLeftShoulder;
	public Transform customLeftElbow;
	public Transform customLeftHand;
	public Transform customLeftHip;
	public Transform customLeftKnee;
	public Transform customLeftFoot;
	public Transform customLeftThumb;
	public Transform customRightThumb;

	public bool fistCurlFingers = true;
	public bool trackThumbs = false;
	public bool trackWrist = true;
	public bool trackAnkle = true;
	public bool rotateWristFromElbow = true;
	
	private RUISSkeletonManager.Skeleton.handState leftHandStatus, lastLeftHandStatus;
	private RUISSkeletonManager.Skeleton.handState rightHandStatus, lastRightHandStatus;
	
	private RUISInputManager inputManager;
    public RUISSkeletonManager skeletonManager;
	private RUISCoordinateSystem coordinateSystem;
	public RUISCharacterController characterController;

	public enum bodyTrackingDeviceType
	{
		Kinect1,
		Kinect2,
		GenericMotionTracker
	}
	public bodyTrackingDeviceType bodyTrackingDevice = bodyTrackingDeviceType.Kinect1;

	public int bodyTrackingDeviceID = 0;
    public int playerId = 0;
	public bool switchToAvailableKinect = false;

    private Vector3 skeletonPosition = Vector3.zero;

	public bool updateRootPosition = true;
	public Vector3 rootSpeedScaling = Vector3.one;

    public bool updateJointPositions = true;
    public bool updateJointRotations = true;

    public bool useHierarchicalModel = false;
	public bool scaleHierarchicalModelBones = true;
	public bool scaleBoneLengthOnly = false;
	public RUISAxis boneLengthAxis = RUISAxis.X;
	public float maxScaleFactor = 0.01f;

	public float torsoThickness = 1;
	public float rightArmThickness = 1; 
	public float  leftArmThickness = 1; 
	public float rightLegThickness = 1; 
	public float  leftLegThickness = 1; 

    public float minimumConfidenceToUpdate = 0.5f;
	public float rotationDamping = 360.0f;
	
	public float handRollAngleMinimum = -180; // Constrained between [0, -180] in Unity Editor script
	public float handRollAngleMaximum =  180; // Constrained between [0,  180] in Unity Editor script
	
	public bool oculusRotatesHead = true;
	public bool followOculusController { get; private set; }
	public Quaternion trackedDeviceYawRotation { get; private set; }

	public bool followMoveController { get; private set; }
	private int followMoveID = 0;
	private RUISPSMoveWand psmove;

	private Vector3 torsoDirection = Vector3.down;
	private Quaternion torsoRotation = Quaternion.identity;

	private KalmanFilter positionKalman;
	private double[] measuredPos = {0, 0, 0};
	private double[] pos = {0, 0, 0};
	private float positionNoiseCovariance = 100; // HACK was 500

	private KalmanFilter[] fourJointsKalman = new KalmanFilter[4];
	private float fourJointsNoiseCovariance = 50; // HACK was 300
	private Vector3[] fourJointPositions = new Vector3[4];
	
	public bool filterRotations = false;
	public float rotationNoiseCovariance = 200;
	// Offset Z rotation of the thumb. Default value is 45, but it might depend on your avatar rig.
	public float thumbZRotationOffset = 45;

	private Dictionary<Transform, Quaternion> jointInitialRotations;
    private Dictionary<KeyValuePair<Transform, Transform>, float> jointInitialDistances;

	
	public float adjustVerticalTorsoLocation = 0;
	public float adjustVerticalHipsPosition  = 0;
	private Vector3 spineDirection = Vector3.zero;
	//private RUISSkeletonManager.JointData adjustedHipJoint = new RUISSkeletonManager.JointData();

    private float torsoOffset = 0.0f;

	private float torsoScale = 1.0f;

    public float neckHeightTweaker = 0.0f;
	private Vector3 neckOriginalLocalPosition;
	private Transform chest;
	private Vector3 chestOriginalLocalPosition;

    public float forearmLengthRatio = 1.0f;
	public float shinLengthRatio = 1.0f;

	private Vector3 unalteredRightForearmScale;
	private Vector3 unalteredLeftForearmScale;
	private Vector3 unalteredRightShinScale;
	private Vector3 unalteredLeftShinScale;
	
	Ovr.HmdType ovrHmdVersion = Ovr.HmdType.None;
	
	Quaternion[,,] initialFingerRotations = new Quaternion[2,5,3]; // 2 hands, 5 fingers, 3 finger bones
	Transform[,,] fingerTransforms = new Transform[2,5,3]; // For quick access to finger gameobjects

	// NOTE: The below phalange rotations are set in Start() method !!! See clause that starts with switch(boneLengthAxis)
	// Thumb phalange rotations when hand is clenched to a fist
	public Quaternion clenchedRotationThumbTM; 
	public Quaternion clenchedRotationThumbMCP;
	public Quaternion clenchedRotationThumbIP;
	
	// Phalange rotations of other fingers when hand is clenched to a fist
	public Quaternion clenchedRotationMCP;
	public Quaternion clenchedRotationPIP;
	public Quaternion clenchedRotationDIP;
	
    void Awake()
    {
		inputManager = FindObjectOfType(typeof(RUISInputManager)) as RUISInputManager;

		if(inputManager)
		{
			if(switchToAvailableKinect)
			{
				if(   bodyTrackingDevice == bodyTrackingDeviceType.Kinect1
				   && !inputManager.enableKinect && inputManager.enableKinect2)
				{
					bodyTrackingDevice = bodyTrackingDeviceType.Kinect2;
				}
				else if(   bodyTrackingDevice == bodyTrackingDeviceType.Kinect2
				   && !inputManager.enableKinect2 && inputManager.enableKinect)
				{
					bodyTrackingDevice = bodyTrackingDeviceType.Kinect1;
				}
			}
		}

		coordinateSystem = FindObjectOfType(typeof(RUISCoordinateSystem)) as RUISCoordinateSystem;
		
		if(bodyTrackingDevice == bodyTrackingDeviceType.Kinect1) bodyTrackingDeviceID = RUISSkeletonManager.kinect1SensorID;
		if(bodyTrackingDevice == bodyTrackingDeviceType.Kinect2) bodyTrackingDeviceID = RUISSkeletonManager.kinect2SensorID;
		if(bodyTrackingDevice == bodyTrackingDeviceType.GenericMotionTracker) bodyTrackingDeviceID = RUISSkeletonManager.customSensorID;

		followOculusController = false;
		followMoveController = false;
		trackedDeviceYawRotation = Quaternion.identity;
		
        jointInitialRotations = new Dictionary<Transform, Quaternion>();
        jointInitialDistances = new Dictionary<KeyValuePair<Transform, Transform>, float>();
		
		positionKalman = new KalmanFilter();
		positionKalman.initialize(3,3);

		for(int i=0; i<fourJointsKalman.Length; ++i)
		{
			fourJointsKalman[i] = new KalmanFilter();
			fourJointsKalman[i].initialize(3,3);
			fourJointPositions[i] = Vector3.zero;
		}
    }

    void Start()
    {
		
		if (skeletonManager == null)
		{
			skeletonManager = FindObjectOfType(typeof(RUISSkeletonManager)) as RUISSkeletonManager;
			if (!skeletonManager)
				Debug.LogError("The scene is missing " + typeof(RUISSkeletonManager) + " script!");
		}

		// Disable features that are only available for Kinect2 or custom motion tracker
		if (bodyTrackingDevice == bodyTrackingDeviceType.Kinect1) 
		{
			fistCurlFingers = false;
			trackThumbs = false;
			trackWrist  = false;
			trackAnkle  = false;
			rotateWristFromElbow = false;
		}

        if (useHierarchicalModel)
        {
            //fix all shoulder and hip rotations to match the default kinect rotations
            rightShoulder.rotation = FindFixingRotation(rightShoulder.position, rightElbow.position, transform.right) * rightShoulder.rotation;
            leftShoulder.rotation = FindFixingRotation(leftShoulder.position, leftElbow.position, -transform.right) * leftShoulder.rotation;
            rightHip.rotation = FindFixingRotation(rightHip.position, rightFoot.position, -transform.up) * rightHip.rotation;
            leftHip.rotation = FindFixingRotation(leftHip.position, leftFoot.position, -transform.up) * leftHip.rotation;

			Vector3 scaler = new Vector3(1/transform.lossyScale.x, 1/transform.lossyScale.y, 1/transform.lossyScale.z);
			Vector3 assumedRootPos = Vector3.Scale((rightShoulder.position + leftShoulder.position + leftHip.position + rightHip.position) / 4, scaler); 
															// (1/transform.lossyScale.x, 1/transform.lossyScale.y, 1/transform.lossyScale.z)
			Vector3 realRootPos = Vector3.Scale(torso.position, scaler);

			Vector3 torsoUp = head.position - torso.position;
			torsoUp.Normalize();
			torsoOffset = Vector3.Dot(realRootPos - assumedRootPos, torsoUp);
			//torsoOffset = (realRootPos - assumedRootPos).y;

            if (neck)
            {
                neckOriginalLocalPosition = neck.localPosition;
				if(neck.parent)
				{
					chest = neck.parent;
					if(chest == torso)
					{
						Debug.Log(	typeof(RUISSkeletonController) + ": Hierarchical model stored in GameObject " + this.name 
						          + " does not have enough joints between neck and torso for Hips Vertical Tweaker to work.");
						chest = null;
					}
					chestOriginalLocalPosition = chest.localPosition;
				}
            }
        }

        SaveInitialRotation(root);
        SaveInitialRotation(head);
        SaveInitialRotation(torso);
        SaveInitialRotation(rightShoulder);
        SaveInitialRotation(rightElbow);
        SaveInitialRotation(rightHand);
        SaveInitialRotation(leftShoulder);
        SaveInitialRotation(leftElbow);
        SaveInitialRotation(leftHand);
        SaveInitialRotation(rightHip);
        SaveInitialRotation(rightKnee);
        SaveInitialRotation(rightFoot);
        SaveInitialRotation(leftHip);
        SaveInitialRotation(leftKnee);
        SaveInitialRotation(leftFoot);

		SaveInitialRotation(leftThumb);
		SaveInitialRotation(rightThumb);

		saveInitialFingerRotations();
		
        SaveInitialDistance(rightShoulder, rightElbow);
        SaveInitialDistance(rightElbow, rightHand);
        SaveInitialDistance(leftShoulder, leftElbow);
        SaveInitialDistance(leftElbow, leftHand);

        SaveInitialDistance(rightHip, rightKnee);
        SaveInitialDistance(rightKnee, rightFoot);
        SaveInitialDistance(leftHip, leftKnee);
        SaveInitialDistance(leftKnee, leftFoot);

        SaveInitialDistance(torso, head);

        SaveInitialDistance(rightShoulder, leftShoulder);
        SaveInitialDistance(rightHip, leftHip);

		
		if (rightElbow)
			unalteredRightForearmScale = rightElbow.localScale;
		
		if (leftElbow)
			unalteredLeftForearmScale = leftElbow.localScale;
		
		if(rightKnee)
			unalteredRightShinScale = rightKnee.localScale;
		
		if(leftKnee)
			unalteredLeftShinScale = leftKnee.localScale;

		// Finger clench rotations: these depend on your animation rig
		// Also see method handleFingersCurling() and its clenchedRotationThumbTM_corrected and clenchedRotationThumbIP_corrected
		// variables, if you are not tracking thumbs with Kinect 2. They also depend on your animation rig.
		switch(boneLengthAxis)
		{
		case RUISAxis.X:
			// Thumb phalange rotations when hand is clenched to a fist
			clenchedRotationThumbTM = Quaternion.Euler (45, 0, 0); 
			clenchedRotationThumbMCP = Quaternion.Euler (0, 0, -25 );
			clenchedRotationThumbIP = Quaternion.Euler (0, 0, -80);
			// Phalange rotations of other fingers when hand is clenched to a fist
			clenchedRotationMCP = Quaternion.Euler (0, 0, -45);
			clenchedRotationPIP = Quaternion.Euler (0, 0, -100);
			clenchedRotationDIP = Quaternion.Euler (0, 0, -70);
			break;
		case RUISAxis.Y:
			// Thumb phalange rotations when hand is clenched to a fist
			clenchedRotationThumbTM = Quaternion.Euler (0, 0, 0); 
			clenchedRotationThumbMCP = Quaternion.Euler (0, 0, 0);
			clenchedRotationThumbIP = Quaternion.Euler (0, 0, 80);
			// Phalange rotations of other fingers when hand is clenched to a fist
			clenchedRotationMCP = Quaternion.Euler (45, 0, 0);
			clenchedRotationPIP = Quaternion.Euler (100, 0, 0);
			clenchedRotationDIP = Quaternion.Euler (70, 0, 0);
			break;
		case RUISAxis.Z: // TODO: Not yet tested with a real rig
			// Thumb phalange rotations when hand is clenched to a fist
			clenchedRotationThumbTM = Quaternion.Euler (45, 0, 0); 
			clenchedRotationThumbMCP = Quaternion.Euler (0, 0, -25 );
			clenchedRotationThumbIP = Quaternion.Euler (0, 0, -80);
			// Phalange rotations of other fingers when hand is clenched to a fist
			clenchedRotationMCP = Quaternion.Euler (0, -45, 0);
			clenchedRotationPIP = Quaternion.Euler (0, -100, 0);
			clenchedRotationDIP = Quaternion.Euler (0, -70, 0);
			break;
		}

		if(inputManager)
		{
			if(gameObject.transform.parent != null)
			{
				characterController = gameObject.transform.parent.GetComponent<RUISCharacterController>();
				if(characterController != null)
				{
					if(		characterController.characterPivotType == RUISCharacterController.CharacterPivotType.MoveController
						&&	inputManager.enablePSMove																			)
					{
						followMoveController = true;
						followMoveID = characterController.moveControllerId;
//						if(		 gameObject.GetComponent<RUISKinectAndMecanimCombiner>() == null 
//							||	!gameObject.GetComponent<RUISKinectAndMecanimCombiner>().enabled )
							Debug.LogWarning(	"Using PS Move controller #" + characterController.moveControllerId + " as a source "
						                 	 +	"for avatar root position of " + gameObject.name + ", because PS Move is enabled"
											 +	"and the PS Move controller has been assigned as a "
											 +	"Character Pivot in " + gameObject.name + "'s parent GameObject");
					}

					if(!inputManager.enableKinect && !inputManager.enableKinect2 && !followMoveController)
					{
						
						if(OVRManager.display != null && OVRManager.display.isPresent)
						{
							followOculusController = true;
							Debug.LogWarning(	"Using Oculus Rift HMD as a Character Pivot for " + gameObject.name
							                 +	", because Kinects are disabled and an Oculus Rift was detected.");
						}
					}
				}
			}
		}

		
		try
		{
			if(OVRManager.capiHmd != null)
				ovrHmdVersion = OVRManager.capiHmd.GetDesc().Type;
		}
		catch(UnityException e)
		{
			Debug.LogError(e);
		}

		if(oculusRotatesHead && (OVRManager.display == null || !OVRManager.display.isPresent))
		   oculusRotatesHead = false;

		// HACK for filtering Kinect 2 arm rotations
		skeletonManager.skeletons [bodyTrackingDeviceID, playerId].filterRotations = filterRotations;
		skeletonManager.skeletons [bodyTrackingDeviceID, playerId].rotationNoiseCovariance = rotationNoiseCovariance;
		for(int i=0; i < skeletonManager.skeletons [bodyTrackingDeviceID, playerId].filterRot.Length; ++i)
		{
			if(skeletonManager.skeletons [bodyTrackingDeviceID, playerId].filterRot[i] != null)
				skeletonManager.skeletons [bodyTrackingDeviceID, playerId].filterRot[i].rotationNoiseCovariance = rotationNoiseCovariance;
		}
		skeletonManager.skeletons [bodyTrackingDeviceID, playerId].thumbZRotationOffset = thumbZRotationOffset;
    }

    void LateUpdate()
    {
		// If a custom skeleton tracking source is used, save its data into skeletonManager (which is a little 
		// topsy turvy) so we can utilize same code as we did with Kinect 1 and 2
		if(bodyTrackingDevice == bodyTrackingDeviceType.GenericMotionTracker) 
		{
			skeletonManager.skeletons [bodyTrackingDeviceID, playerId].isTracking = true;

			if(customRoot) {
				skeletonManager.skeletons [bodyTrackingDeviceID, playerId].root.rotation = customRoot.rotation;
				skeletonManager.skeletons [bodyTrackingDeviceID, playerId].root.position = customRoot.position;
				skeletonManager.skeletons [bodyTrackingDeviceID, playerId].root.positionConfidence = 1;
				skeletonManager.skeletons [bodyTrackingDeviceID, playerId].root.rotationConfidence = 1;
			}
			if(customHead) {
				skeletonManager.skeletons [bodyTrackingDeviceID, playerId].head.rotation = customHead.rotation;
				skeletonManager.skeletons [bodyTrackingDeviceID, playerId].head.position = customHead.position;
				skeletonManager.skeletons [bodyTrackingDeviceID, playerId].head.positionConfidence = 1;
				skeletonManager.skeletons [bodyTrackingDeviceID, playerId].head.rotationConfidence = 1;
			}
			if(customNeck) {
				skeletonManager.skeletons [bodyTrackingDeviceID, playerId].neck.rotation = customNeck.rotation;
				skeletonManager.skeletons [bodyTrackingDeviceID, playerId].neck.position = customNeck.position;
				skeletonManager.skeletons [bodyTrackingDeviceID, playerId].neck.positionConfidence = 1;
				skeletonManager.skeletons [bodyTrackingDeviceID, playerId].neck.rotationConfidence = 1;
			}
			if(customTorso) {
				skeletonManager.skeletons [bodyTrackingDeviceID, playerId].torso.rotation = customTorso.rotation;
				skeletonManager.skeletons [bodyTrackingDeviceID, playerId].torso.position = customTorso.position;
				skeletonManager.skeletons [bodyTrackingDeviceID, playerId].torso.positionConfidence = 1;
				skeletonManager.skeletons [bodyTrackingDeviceID, playerId].torso.rotationConfidence = 1;
			}
			if(customRightShoulder) {
				skeletonManager.skeletons [bodyTrackingDeviceID, playerId].rightShoulder.rotation = customRightShoulder.rotation;
				skeletonManager.skeletons [bodyTrackingDeviceID, playerId].rightShoulder.position = customRightShoulder.position;
				skeletonManager.skeletons [bodyTrackingDeviceID, playerId].rightShoulder.positionConfidence = 1;
				skeletonManager.skeletons [bodyTrackingDeviceID, playerId].rightShoulder.rotationConfidence = 1;
			}
			if(customLeftShoulder) {
				skeletonManager.skeletons [bodyTrackingDeviceID, playerId].leftShoulder.rotation = customLeftShoulder.rotation;
				skeletonManager.skeletons [bodyTrackingDeviceID, playerId].leftShoulder.position = customLeftShoulder.position;
				skeletonManager.skeletons [bodyTrackingDeviceID, playerId].leftShoulder.positionConfidence = 1;
				skeletonManager.skeletons [bodyTrackingDeviceID, playerId].leftShoulder.rotationConfidence = 1;
			}
			if(customRightElbow) {
				skeletonManager.skeletons [bodyTrackingDeviceID, playerId].rightElbow.rotation = customRightElbow.rotation;
				skeletonManager.skeletons [bodyTrackingDeviceID, playerId].rightElbow.position = customRightElbow.position;
				skeletonManager.skeletons [bodyTrackingDeviceID, playerId].rightElbow.positionConfidence = 1;
				skeletonManager.skeletons [bodyTrackingDeviceID, playerId].rightElbow.rotationConfidence = 1;
			}
			if(customLeftElbow) {
				skeletonManager.skeletons [bodyTrackingDeviceID, playerId].leftElbow.rotation = customLeftElbow.rotation;
				skeletonManager.skeletons [bodyTrackingDeviceID, playerId].leftElbow.position = customLeftElbow.position;
				skeletonManager.skeletons [bodyTrackingDeviceID, playerId].leftElbow.positionConfidence = 1;
				skeletonManager.skeletons [bodyTrackingDeviceID, playerId].leftElbow.rotationConfidence = 1;
			}
			if(customRightHand) {
				skeletonManager.skeletons [bodyTrackingDeviceID, playerId].rightHand.rotation = customRightHand.rotation;
				skeletonManager.skeletons [bodyTrackingDeviceID, playerId].rightHand.position = customRightHand.position;
				skeletonManager.skeletons [bodyTrackingDeviceID, playerId].rightHand.positionConfidence = 1;
				skeletonManager.skeletons [bodyTrackingDeviceID, playerId].rightHand.rotationConfidence = 1;
			}
			if(customLeftHand) {
				skeletonManager.skeletons [bodyTrackingDeviceID, playerId].leftHand.rotation = customLeftHand.rotation;
				skeletonManager.skeletons [bodyTrackingDeviceID, playerId].leftHand.position = customLeftHand.position;
				skeletonManager.skeletons [bodyTrackingDeviceID, playerId].leftHand.positionConfidence = 1;
				skeletonManager.skeletons [bodyTrackingDeviceID, playerId].leftHand.rotationConfidence = 1;
			}
			if(customRightHip) {
				skeletonManager.skeletons [bodyTrackingDeviceID, playerId].rightHip.rotation = customRightHip.rotation;
				skeletonManager.skeletons [bodyTrackingDeviceID, playerId].rightHip.position = customRightHip.position;
				skeletonManager.skeletons [bodyTrackingDeviceID, playerId].rightHip.positionConfidence = 1;
				skeletonManager.skeletons [bodyTrackingDeviceID, playerId].rightHip.rotationConfidence = 1;
			}
			if(customLeftHip) {
				skeletonManager.skeletons [bodyTrackingDeviceID, playerId].leftHip.rotation = customLeftHip.rotation;
				skeletonManager.skeletons [bodyTrackingDeviceID, playerId].leftHip.position = customLeftHip.position;
				skeletonManager.skeletons [bodyTrackingDeviceID, playerId].leftHip.positionConfidence = 1;
				skeletonManager.skeletons [bodyTrackingDeviceID, playerId].leftHip.rotationConfidence = 1;
			}
			if(customRightKnee) {
				skeletonManager.skeletons [bodyTrackingDeviceID, playerId].rightKnee.rotation = customRightKnee.rotation;
				skeletonManager.skeletons [bodyTrackingDeviceID, playerId].rightKnee.position = customRightKnee.position;
				skeletonManager.skeletons [bodyTrackingDeviceID, playerId].rightKnee.positionConfidence = 1;
				skeletonManager.skeletons [bodyTrackingDeviceID, playerId].rightKnee.rotationConfidence = 1;
			}
			if(customLeftKnee) {
				skeletonManager.skeletons [bodyTrackingDeviceID, playerId].leftKnee.rotation = customLeftKnee.rotation;
				skeletonManager.skeletons [bodyTrackingDeviceID, playerId].leftKnee.position = customLeftKnee.position;
				skeletonManager.skeletons [bodyTrackingDeviceID, playerId].leftKnee.positionConfidence = 1;
				skeletonManager.skeletons [bodyTrackingDeviceID, playerId].leftKnee.rotationConfidence = 1;
			}
			if(customRightFoot) {
				skeletonManager.skeletons [bodyTrackingDeviceID, playerId].rightFoot.rotation = customRightFoot.rotation;
				skeletonManager.skeletons [bodyTrackingDeviceID, playerId].rightFoot.position = customRightFoot.position;
				skeletonManager.skeletons [bodyTrackingDeviceID, playerId].rightFoot.positionConfidence = 1;
				skeletonManager.skeletons [bodyTrackingDeviceID, playerId].rightFoot.rotationConfidence = 1;
			}
			if(customLeftFoot) {
				skeletonManager.skeletons [bodyTrackingDeviceID, playerId].leftFoot.rotation = customLeftFoot.rotation;
				skeletonManager.skeletons [bodyTrackingDeviceID, playerId].leftFoot.position = customLeftFoot.position;
				skeletonManager.skeletons [bodyTrackingDeviceID, playerId].leftFoot.positionConfidence = 1;
				skeletonManager.skeletons [bodyTrackingDeviceID, playerId].leftFoot.rotationConfidence = 1;
			}
			if(customRightThumb) {
				skeletonManager.skeletons [bodyTrackingDeviceID, playerId].rightThumb.rotation = customRightThumb.rotation;
				skeletonManager.skeletons [bodyTrackingDeviceID, playerId].rightThumb.position = customRightThumb.position;
				skeletonManager.skeletons [bodyTrackingDeviceID, playerId].rightThumb.positionConfidence = 1;
				skeletonManager.skeletons [bodyTrackingDeviceID, playerId].rightThumb.rotationConfidence = 1;
			}
			if(customLeftThumb) {
				skeletonManager.skeletons [bodyTrackingDeviceID, playerId].leftThumb.rotation = customLeftThumb.rotation;
				skeletonManager.skeletons [bodyTrackingDeviceID, playerId].leftThumb.position = customLeftThumb.position;
				skeletonManager.skeletons [bodyTrackingDeviceID, playerId].leftThumb.positionConfidence = 1;
				skeletonManager.skeletons [bodyTrackingDeviceID, playerId].leftThumb.rotationConfidence = 1;
			}
		}

		// Update skeleton based on data fetched from skeletonManager
		if (	skeletonManager != null && skeletonManager.skeletons [bodyTrackingDeviceID, playerId] != null 
		    &&  skeletonManager.skeletons [bodyTrackingDeviceID, playerId].isTracking) 
		{
						
//			if(bodyTrackingDeviceID == RUISSkeletonManager.kinect2SensorID && !skeletonManager.isNewKinect2Frame)
//				return;

			float maxAngularVelocity;
//			if(bodyTrackingDeviceID == RUISSkeletonManager.kinect2SensorID)
//				maxAngularVelocity = skeletonManager.kinect2FrameDeltaT * rotationDamping;
//			else 
				maxAngularVelocity = Time.deltaTime * rotationDamping;


			// Obtained new body tracking data. TODO test that Kinect 1 still works
//			if(bodyTrackingDeviceID != RUISSkeletonManager.kinect2SensorID || skeletonManager.isNewKinect2Frame)
			{
				UpdateSkeletonPosition ();

				UpdateTransform (ref torso,         skeletonManager.skeletons [bodyTrackingDeviceID, playerId].torso,           maxAngularVelocity);
				UpdateTransform (ref head,          skeletonManager.skeletons [bodyTrackingDeviceID, playerId].head,            maxAngularVelocity);
			}
				
			if(oculusRotatesHead && OVRManager.display != null)
			{
				if(coordinateSystem)
				{
					Quaternion oculusRotation = Quaternion.identity;
					if(coordinateSystem.applyToRootCoordinates)
					{
						if(ovrHmdVersion == Ovr.HmdType.DK1 || ovrHmdVersion == Ovr.HmdType.DKHD)
							oculusRotation = coordinateSystem.GetOculusRiftOrientationRaw();
						else
							oculusRotation = coordinateSystem.ConvertRotation(Quaternion.Inverse(coordinateSystem.GetOculusCameraOrientationRaw()) 
								                                                  * coordinateSystem.GetOculusRiftOrientationRaw(), RUISDevice.Oculus_DK2);
					}
					else if(OVRManager.display != null)
						oculusRotation = OVRManager.display.GetHeadPose().orientation;

					if (useHierarchicalModel)
						head.rotation = transform.rotation * oculusRotation /*skeletonManager.skeletons [bodyTrackingDeviceID, playerId].head.rotation*/ *
							(jointInitialRotations.ContainsKey(head) ? jointInitialRotations[head] : Quaternion.identity);
					else
						head.localRotation = oculusRotation; //skeletonManager.skeletons [bodyTrackingDeviceID, playerId].head;
				}
			}
			
			// Obtained new body tracking data. TODO test that Kinect 1 still works
//			if(bodyTrackingDeviceID != RUISSkeletonManager.kinect2SensorID || skeletonManager.isNewKinect2Frame)
			{
				UpdateTransform (ref leftShoulder,  skeletonManager.skeletons [bodyTrackingDeviceID, playerId].leftShoulder,    maxAngularVelocity);
				UpdateTransform (ref rightShoulder, skeletonManager.skeletons [bodyTrackingDeviceID, playerId].rightShoulder,   maxAngularVelocity);

				if(trackWrist || !useHierarchicalModel)
				{
					UpdateTransform (ref leftHand,      skeletonManager.skeletons [bodyTrackingDeviceID, playerId].leftHand,      2*maxAngularVelocity);
					UpdateTransform (ref rightHand,     skeletonManager.skeletons [bodyTrackingDeviceID, playerId].rightHand,     2*maxAngularVelocity);
				}

				UpdateTransform (ref leftHip,       skeletonManager.skeletons [bodyTrackingDeviceID, playerId].leftHip,         maxAngularVelocity);
				UpdateTransform (ref rightHip,      skeletonManager.skeletons [bodyTrackingDeviceID, playerId].rightHip,        maxAngularVelocity);
				UpdateTransform (ref leftKnee,      skeletonManager.skeletons [bodyTrackingDeviceID, playerId].leftKnee,        maxAngularVelocity);
				UpdateTransform (ref rightKnee,     skeletonManager.skeletons [bodyTrackingDeviceID, playerId].rightKnee,       maxAngularVelocity);
				
				UpdateTransform (ref rightElbow,    skeletonManager.skeletons [bodyTrackingDeviceID, playerId].rightElbow,      maxAngularVelocity);
				UpdateTransform (ref leftElbow,     skeletonManager.skeletons [bodyTrackingDeviceID, playerId].leftElbow,       maxAngularVelocity);

				if(trackAnkle || !useHierarchicalModel)
				{
					UpdateTransform (ref leftFoot,  skeletonManager.skeletons [bodyTrackingDeviceID, playerId].leftFoot,        maxAngularVelocity);
					UpdateTransform (ref rightFoot, skeletonManager.skeletons [bodyTrackingDeviceID, playerId].rightFoot,       maxAngularVelocity);
				}
			
//				// TODO: Restore this when implementation is fixed
//				if(rotateWristFromElbow && bodyTrackingDevice == bodyTrackingDeviceType.Kinect2)
//				{
//					if (useHierarchicalModel)
//					{
//						if(leftElbow && leftHand)
//							leftElbow.rotation  = leftHand.rotation;
//						if(rightElbow && rightHand)
//							rightElbow.rotation = rightHand.rotation;
//					}
//					else
//					{
//						if(leftElbow && leftHand)
//							leftElbow.localRotation  = leftHand.localRotation;
//						if(rightElbow && rightHand)
//							rightElbow.localRotation = rightHand.localRotation;
//					}
//					//				UpdateTransform (ref rightElbow, skeletonManager.skeletons [bodyTrackingDeviceID, playerId].rightHand);
//					//				UpdateTransform (ref leftElbow, skeletonManager.skeletons [bodyTrackingDeviceID, playerId].leftHand);
//				}
	
				if(bodyTrackingDevice == bodyTrackingDeviceType.Kinect2 || bodyTrackingDevice == bodyTrackingDeviceType.GenericMotionTracker)
				{
					if(fistCurlFingers)
						handleFingersCurling(trackThumbs);

					if(trackThumbs) 
					{
						if(rightThumb)
							UpdateTransform (ref rightThumb, skeletonManager.skeletons [bodyTrackingDeviceID, playerId].rightThumb, maxAngularVelocity);
						if(leftThumb)
							UpdateTransform (ref leftThumb,  skeletonManager.skeletons [bodyTrackingDeviceID, playerId].leftThumb,  maxAngularVelocity);
					}
				}
			}

			if (!useHierarchicalModel) 
			{
				if (leftHand != null) 
				{
					leftHand.localRotation = leftElbow.localRotation;
				}

				if (rightHand != null) 
				{
					rightHand.localRotation = rightElbow.localRotation;
				}
			} else 
			{
				if (scaleHierarchicalModelBones) 
				{
					UpdateBoneScalings ();

					torsoRotation = Quaternion.Slerp(torsoRotation, skeletonManager.skeletons[bodyTrackingDeviceID, playerId].torso.rotation, Time.deltaTime*rotationDamping);
					torsoDirection = torsoRotation * Vector3.down;

					if(torso == root)
						torso.position = transform.TransformPoint (- torsoDirection * (torsoOffset * torsoScale + adjustVerticalHipsPosition));
					else
						torso.position = transform.TransformPoint (skeletonManager.skeletons [bodyTrackingDeviceID, playerId].torso.position - skeletonPosition 
						                                           - torsoDirection * (torsoOffset * torsoScale + adjustVerticalHipsPosition));

					spineDirection = transform.TransformPoint (skeletonManager.skeletons [bodyTrackingDeviceID, playerId].torso.position - skeletonPosition 
					                                           - torsoDirection * (torsoOffset * torsoScale + adjustVerticalHipsPosition - 1));
					
					spineDirection = torso.position - spineDirection;
					spineDirection.Normalize();

					// Obtained new body tracking data. TODO test that Kinect 1 still works
//					if(bodyTrackingDeviceID != RUISSkeletonManager.kinect2SensorID || skeletonManager.isNewKinect2Frame)
					{
						float deltaT;
//						if(bodyTrackingDeviceID == RUISSkeletonManager.kinect2SensorID)
//							deltaT = skeletonManager.kinect2FrameDeltaT;
//						else
							deltaT = Time.deltaTime;
						ForceUpdatePosition (ref rightShoulder, skeletonManager.skeletons [bodyTrackingDeviceID, playerId].rightShoulder, 0, deltaT);
						ForceUpdatePosition (ref leftShoulder, skeletonManager.skeletons [bodyTrackingDeviceID, playerId].leftShoulder, 1, deltaT);
						ForceUpdatePosition (ref rightHip, skeletonManager.skeletons [bodyTrackingDeviceID, playerId].rightHip, 2, deltaT);
						ForceUpdatePosition (ref leftHip, skeletonManager.skeletons [bodyTrackingDeviceID, playerId].leftHip, 3, deltaT);
					}

				}
			}

			if (updateRootPosition) 
			{
//				Vector3 newRootPosition = skeletonManager.skeletons [bodyTrackingDeviceID, playerId].root.position;
//				measuredPos [0] = newRootPosition.x;
//				measuredPos [1] = newRootPosition.y;
//				measuredPos [2] = newRootPosition.z;
//				positionKalman.setR (Time.deltaTime * positionNoiseCovariance);
//				positionKalman.predict ();
//				positionKalman.update (measuredPos);
//				pos = positionKalman.getState ();

				// Root speed scaling is applied here
				transform.localPosition = Vector3.Scale(skeletonPosition, rootSpeedScaling);
//				transform.localPosition = Vector3.Scale(new Vector3 ((float)pos [0], (float)pos [1], (float)pos [2]), rootSpeedScaling);
			}
		} 
		 
		if(characterController)
		{
			// If character controller pivot is PS Move
			if (followMoveController && inputManager)
			{
				psmove = inputManager.GetMoveWand (followMoveID);
				if (psmove) 
				{
					float moveYaw = psmove.localRotation.eulerAngles.y;
					trackedDeviceYawRotation = Quaternion.Euler (0, moveYaw, 0);

					if(!skeletonManager.skeletons [bodyTrackingDeviceID, playerId].isTracking)
					{
						skeletonPosition = psmove.localPosition - trackedDeviceYawRotation * characterController.psmoveOffset;
						skeletonPosition.y = 0;

						if (updateRootPosition)
							transform.localPosition = skeletonPosition;

						if(characterController.headRotatesBody)
							UpdateTransformWithTrackedDevice (ref root, moveYaw);
//							UpdateTransformWithPSMove (ref torso,  moveYaw);
//							UpdateTransformWithPSMove (ref head, moveYawRotation);
//							UpdateTransformWithPSMove (ref leftShoulder, moveYawRotation);
//							UpdateTransformWithPSMove (ref leftElbow, moveYawRotation);
//							UpdateTransformWithPSMove (ref leftHand, moveYawRotation);
//							UpdateTransformWithPSMove (ref rightShoulder, moveYawRotation);
//							UpdateTransformWithPSMove (ref rightElbow, moveYawRotation);
//							UpdateTransformWithPSMove (ref rightHand, moveYawRotation);
//							UpdateTransformWithPSMove (ref leftHip, moveYawRotation);
//							UpdateTransformWithPSMove (ref leftKnee, moveYawRotation);
//							UpdateTransformWithPSMove (ref leftFoot, moveYawRotation);
//							UpdateTransformWithPSMove (ref rightHip, moveYawRotation);
//							UpdateTransformWithPSMove (ref rightKnee, moveYawRotation);
//							UpdateTransformWithPSMove (ref rightFoot, moveYawRotation);
					}
				}
			}

			if(followOculusController)
			{
				float oculusYaw = 0;
				if(coordinateSystem)
				{
					if(coordinateSystem.applyToRootCoordinates)
					{
						if(ovrHmdVersion == Ovr.HmdType.DK1 || ovrHmdVersion == Ovr.HmdType.DKHD)
							oculusYaw = coordinateSystem.GetOculusRiftOrientationRaw().eulerAngles.y;
						else
						{
							skeletonPosition = coordinateSystem.ConvertLocation(coordinateSystem.GetOculusRiftLocation(), RUISDevice.Oculus_DK2);
							skeletonPosition.y = 0;
							oculusYaw = coordinateSystem.ConvertRotation(Quaternion.Inverse(coordinateSystem.GetOculusCameraOrientationRaw()) * coordinateSystem.GetOculusRiftOrientationRaw(),
						                                          	     RUISDevice.Oculus_DK2).eulerAngles.y;
						}
					}
					else if(OVRManager.display != null)
					{
						skeletonPosition = OVRManager.display.GetHeadPose().position;
						skeletonPosition.y = 0;
						oculusYaw = OVRManager.display.GetHeadPose().orientation.eulerAngles.y;
					}
				}

				trackedDeviceYawRotation = Quaternion.Euler (0, oculusYaw, 0);

				if(updateRootPosition)
					transform.localPosition = skeletonPosition;
				
				if(characterController.headRotatesBody)
					UpdateTransformWithTrackedDevice (ref root, oculusYaw);
			}
		}

		TweakHipPosition();
		TweakNeckHeight();
    }

	private void UpdateTransform(ref Transform transformToUpdate, RUISSkeletonManager.JointData jointToGet, float maxAngularVelocity)
    {
        if (transformToUpdate == null)
		{
			return;
		}

        if (updateJointPositions && jointToGet.positionConfidence >= minimumConfidenceToUpdate)
        {
            transformToUpdate.localPosition = jointToGet.position - skeletonPosition;
        }

        if (updateJointRotations && jointToGet.rotationConfidence >= minimumConfidenceToUpdate)
        {
            if (useHierarchicalModel)
            {
                Quaternion newRotation = transform.rotation * jointToGet.rotation *
                    (jointInitialRotations.ContainsKey(transformToUpdate) ? jointInitialRotations[transformToUpdate] : Quaternion.identity);
				transformToUpdate.rotation = Quaternion.RotateTowards(transformToUpdate.rotation, newRotation, maxAngularVelocity);
            }
            else
            {
				transformToUpdate.localRotation = Quaternion.RotateTowards(transformToUpdate.localRotation, jointToGet.rotation, maxAngularVelocity);
            }
        }
    }

	// Here tracked device can mean PS Move or Oculus Rift DK2+
	private void UpdateTransformWithTrackedDevice(ref Transform transformToUpdate, float controllerYaw)
    {
		if (transformToUpdate == null) return;
		
		//if (updateJointPositions) ;
		
		if (updateJointRotations)
		{
			if (useHierarchicalModel)
            {
//                Quaternion newRotation = transform.rotation * jointToGet.rotation *
//                    (jointInitialRotations.ContainsKey(transformToUpdate) ? jointInitialRotations[transformToUpdate] : Quaternion.identity);
//                transformToUpdate.rotation = Quaternion.Slerp(transformToUpdate.rotation, newRotation, Time.deltaTime * rotationDamping);
				Quaternion newRotation = transform.rotation * Quaternion.Euler(new Vector3(0, controllerYaw, 0)) *
                    (jointInitialRotations.ContainsKey(transformToUpdate) ? jointInitialRotations[transformToUpdate] : Quaternion.identity);
                transformToUpdate.rotation = newRotation;
            }
            else
            {
				transformToUpdate.localRotation = Quaternion.Euler(new Vector3(0, controllerYaw, 0));
//                transformToUpdate.localRotation = Quaternion.Slerp(transformToUpdate.localRotation, jointToGet.rotation, Time.deltaTime * rotationDamping);
            }
		}
	}

    private void ForceUpdatePosition(ref Transform transformToUpdate, RUISSkeletonManager.JointData jointToGet, int jointID, float deltaT)
    {
        if (transformToUpdate == null)
			return;

		if(jointID == 2 || jointID == 3) // HACK: for now saving performance by not filtering hips
			transformToUpdate.position = transform.TransformPoint(jointToGet.position - skeletonPosition);
		else
		{

			measuredPos [0] = jointToGet.position.x;
			measuredPos [1] = jointToGet.position.y;
			measuredPos [2] = jointToGet.position.z;

			fourJointsKalman[jointID].setR (deltaT * fourJointsNoiseCovariance);
			fourJointsKalman[jointID].predict ();
			fourJointsKalman[jointID].update (measuredPos);
			pos = fourJointsKalman[jointID].getState ();

			fourJointPositions[jointID].Set((float)pos [0], (float)pos [1], (float)pos [2]);

			transformToUpdate.position = transform.TransformPoint(fourJointPositions[jointID] - skeletonPosition);
		}
//		transformToUpdate.position = transform.TransformPoint(jointToGet.position - skeletonPosition);
    }

    //gets the main position of the skeleton inside the world, the rest of the joint positions will be calculated in relation to this one
    private void UpdateSkeletonPosition()
    {
		
		Vector3 newRootPosition = skeletonManager.skeletons [bodyTrackingDeviceID, playerId].root.position;
		
		measuredPos [0] = newRootPosition.x;
		measuredPos [1] = newRootPosition.y;
		measuredPos [2] = newRootPosition.z;
		positionKalman.setR (Time.deltaTime * positionNoiseCovariance); // HACK doesn't take into account Kinect's own update deltaT
		positionKalman.predict ();
		positionKalman.update (measuredPos);
		pos = positionKalman.getState ();

		skeletonPosition = new Vector3 ((float)pos [0], (float)pos [1], (float)pos [2]);

//		if (skeletonManager.skeletons[bodyTrackingDeviceID, playerId].root.positionConfidence >= minimumConfidenceToUpdate)
//        {
//			skeletonPosition = skeletonManager.skeletons[bodyTrackingDeviceID, playerId].root.position;
//        }
    }

    private void SaveInitialRotation(Transform bodyPart)
    {
        if (bodyPart)
            jointInitialRotations[bodyPart] = GetInitialRotation(bodyPart);
    }

    private void SaveInitialDistance(Transform rootTransform, Transform distanceTo)
    {
		Vector3 scaler = new Vector3(1/transform.lossyScale.x, 1/transform.lossyScale.y, 1/transform.lossyScale.z);
        jointInitialDistances.Add(new KeyValuePair<Transform, Transform>(rootTransform, distanceTo), 
		                          Vector3.Distance(Vector3.Scale(rootTransform.position, scaler), Vector3.Scale(distanceTo.position, scaler)));
    }

    private Quaternion GetInitialRotation(Transform bodyPart)
    {
        return Quaternion.Inverse(transform.rotation) * bodyPart.rotation;
    }

    private void UpdateBoneScalings()
    {
        if (!ConfidenceGoodEnoughForScaling()) return;

        torsoScale = UpdateTorsoScale();

		// Revert to previous localScale values becore tweaking
		if(rightElbow)
			rightElbow.localScale = unalteredRightForearmScale;
		if(leftElbow)
			leftElbow.localScale  = unalteredLeftForearmScale;
		if(rightKnee)
			rightKnee.localScale  = unalteredRightShinScale;
		if(leftKnee)
			leftKnee.localScale   = unalteredLeftShinScale;

		float rightArmCumulativeScale = UpdateBoneScaling(rightShoulder, rightElbow, skeletonManager.skeletons[bodyTrackingDeviceID, playerId].rightShoulder, 
		                                                  skeletonManager.skeletons[bodyTrackingDeviceID, playerId].rightElbow, torsoScale);
		UpdateBoneScaling(rightElbow, rightHand, skeletonManager.skeletons[bodyTrackingDeviceID, playerId].rightElbow, skeletonManager.skeletons[bodyTrackingDeviceID, playerId].rightHand, 
		                  rightArmCumulativeScale);

		float leftArmCumulativeScale = UpdateBoneScaling(leftShoulder, leftElbow, skeletonManager.skeletons[bodyTrackingDeviceID, playerId].leftShoulder, 
		                                                 skeletonManager.skeletons[bodyTrackingDeviceID, playerId].leftElbow, torsoScale);
		UpdateBoneScaling(leftElbow, leftHand, skeletonManager.skeletons[bodyTrackingDeviceID, playerId].leftElbow, skeletonManager.skeletons[bodyTrackingDeviceID, playerId].leftHand, 
		                  leftArmCumulativeScale);

		float rightLegCumulativeScale = UpdateBoneScaling(rightHip, rightKnee, skeletonManager.skeletons[bodyTrackingDeviceID, playerId].rightHip, 
		                                                  skeletonManager.skeletons[bodyTrackingDeviceID, playerId].rightKnee, torsoScale);
		UpdateBoneScaling(rightKnee, rightFoot, skeletonManager.skeletons[bodyTrackingDeviceID, playerId].rightKnee, skeletonManager.skeletons[bodyTrackingDeviceID, playerId].rightFoot, 
		                  rightLegCumulativeScale);

		float leftLegCumulativeScale = UpdateBoneScaling(leftHip, leftKnee, skeletonManager.skeletons[bodyTrackingDeviceID, playerId].leftHip, 
		                                                 skeletonManager.skeletons[bodyTrackingDeviceID, playerId].leftKnee, torsoScale);
		UpdateBoneScaling(leftKnee, leftFoot, skeletonManager.skeletons[bodyTrackingDeviceID, playerId].leftKnee, skeletonManager.skeletons[bodyTrackingDeviceID, playerId].leftFoot, 
		                  leftLegCumulativeScale);

//		switch(boneLengthAxis)
//		{
//		case RUISAxis.X: 
//			if(rightElbow)
//				rightElbow.localScale = new Vector3(rightElbow.localScale.x, ;
//			if(leftElbow)
//				leftElbow.localScale;
//			if(rightKnee)
//				rightKnee.localScale;
//			if(leftKnee)
//				leftKnee.localScale;
//		case RUISAxis.Y: return boneToScale.localScale.y;
//		case RUISAxis.Z: return boneToScale.localScale.z;
//		}
    }

    private float UpdateBoneScaling(Transform boneToScale, Transform comparisonBone, RUISSkeletonManager.JointData boneToScaleTracker, RUISSkeletonManager.JointData comparisonBoneTracker, float cumulativeScale)
    {
        float modelBoneLength = 1;
		float playerBoneLength = Vector3.Distance(boneToScaleTracker.position, comparisonBoneTracker.position);
		float newScale = 1;
		float thickness = 1;
		float parentBoneThickness = 1;
		float extremityTweaker = 1;
		float skewedScaleTweak = 1;

		if(boneToScale && comparisonBone)
			modelBoneLength = jointInitialDistances[new KeyValuePair<Transform, Transform>(boneToScale, comparisonBone)];

		if(!boneToScale)
			return cumulativeScale;

		newScale = playerBoneLength / modelBoneLength / cumulativeScale;
		
		
		switch(boneToScaleTracker.jointID)
		{
			case RUISSkeletonManager.Joint.LeftHip:       thickness = leftLegThickness;  break;
			case RUISSkeletonManager.Joint.RightHip:      thickness = rightLegThickness; break;
			case RUISSkeletonManager.Joint.LeftShoulder:  thickness = leftArmThickness;  break;
			case RUISSkeletonManager.Joint.RightShoulder: thickness = rightArmThickness; break;
			case RUISSkeletonManager.Joint.LeftKnee:      thickness = leftLegThickness;  extremityTweaker = shinLengthRatio; break;
			case RUISSkeletonManager.Joint.RightKnee:     thickness = rightLegThickness; extremityTweaker = shinLengthRatio; break;
			case RUISSkeletonManager.Joint.LeftElbow:     thickness = leftArmThickness;  extremityTweaker = forearmLengthRatio; break;
			case RUISSkeletonManager.Joint.RightElbow:    thickness = rightArmThickness; extremityTweaker = forearmLengthRatio; break;
		}

		if(scaleBoneLengthOnly)
		{
			bool isExtremityJoint =  (    boneToScaleTracker.jointID == RUISSkeletonManager.Joint.LeftElbow || boneToScaleTracker.jointID == RUISSkeletonManager.Joint.RightElbow
									   || boneToScaleTracker.jointID == RUISSkeletonManager.Joint.LeftKnee  || boneToScaleTracker.jointID == RUISSkeletonManager.Joint.RightKnee );

			if(isExtremityJoint && boneToScale.parent && comparisonBone)
			{
				float jointAngle = Vector3.Angle(boneToScale.position - comparisonBone.position, boneToScale.parent.position - boneToScale.position);
				float cosAngle = Mathf.Cos(Mathf.Deg2Rad * jointAngle);
				float sinAngle = Mathf.Sin(Mathf.Deg2Rad * jointAngle);
				switch(boneLengthAxis)
				{
					case RUISAxis.X: parentBoneThickness = boneToScale.parent.localScale.y; break;
					case RUISAxis.Y: parentBoneThickness = boneToScale.parent.localScale.z; break;
					case RUISAxis.Z: parentBoneThickness = boneToScale.parent.localScale.x; break;
				}

				newScale = ( playerBoneLength / modelBoneLength ); // / ( cumulativeScale * cosAngle * cosAngle + parentBoneThickness * sinAngle * sinAngle );
				thickness = Mathf.Pow (thickness, sinAngle * sinAngle); //cosAngle * cosAngle + sinAngle * sinAngle * thickness;
				skewedScaleTweak = 1 / ( cumulativeScale * cosAngle * cosAngle + parentBoneThickness * sinAngle * sinAngle );
			}

			switch(boneLengthAxis)
			{
				case RUISAxis.X:
					boneToScale.localScale = Vector3.MoveTowards(boneToScale.localScale, new Vector3(newScale, thickness, thickness), maxScaleFactor * Time.deltaTime);
					boneToScale.localScale = new Vector3(boneToScale.localScale.x, thickness, thickness);
					break;
				case RUISAxis.Y:
					boneToScale.localScale = Vector3.MoveTowards(boneToScale.localScale, new Vector3(thickness, newScale, thickness), maxScaleFactor * Time.deltaTime);
					boneToScale.localScale = new Vector3(thickness, boneToScale.localScale.y, thickness);
					break;
				case RUISAxis.Z:
					boneToScale.localScale = Vector3.MoveTowards(boneToScale.localScale, new Vector3(thickness, thickness, newScale), maxScaleFactor * Time.deltaTime);
					boneToScale.localScale = new Vector3(thickness, thickness, boneToScale.localScale.z);
					break;
			}

			if(isExtremityJoint)
			{
				// Save untweaked scales
				switch(boneToScaleTracker.jointID)
				{
					case RUISSkeletonManager.Joint.LeftKnee:  
						unalteredLeftShinScale     = boneToScale.localScale;
						break;
					case RUISSkeletonManager.Joint.RightKnee:   
						unalteredRightShinScale    = boneToScale.localScale;
						break;
					case RUISSkeletonManager.Joint.LeftElbow:   
						unalteredLeftForearmScale  = boneToScale.localScale;
						break;
					case RUISSkeletonManager.Joint.RightElbow: 
						unalteredRightForearmScale = boneToScale.localScale;
						break;
				}

				// Apply bone length tweaks
				switch(boneLengthAxis)
				{
					case RUISAxis.X:
						boneToScale.localScale = new Vector3(extremityTweaker * skewedScaleTweak * boneToScale.localScale.x, thickness, thickness);
						break;
					case RUISAxis.Y:
						boneToScale.localScale = new Vector3(thickness, extremityTweaker * skewedScaleTweak * boneToScale.localScale.y, thickness);
						break;
					case RUISAxis.Z:
						boneToScale.localScale = new Vector3(thickness, thickness, extremityTweaker * skewedScaleTweak * boneToScale.localScale.z);
						break;
				}
			}
		}
		else
			boneToScale.localScale = extremityTweaker * Vector3.MoveTowards(boneToScale.localScale, new Vector3(newScale, newScale, newScale), maxScaleFactor * Time.deltaTime);

		switch(boneLengthAxis)
		{
			case RUISAxis.X: return boneToScale.localScale.x;
			case RUISAxis.Y: return boneToScale.localScale.y;
			case RUISAxis.Z: return boneToScale.localScale.z;
		}
		return boneToScale.localScale.x;
    }

    private float UpdateTorsoScale()
    {
        //average hip to shoulder length and compare it to the one found in the model - scale accordingly
        //we can assume hips and shoulders are set quite correctly, while we cannot be sure about the spine positions
        float modelLength = (jointInitialDistances[new KeyValuePair<Transform, Transform>(rightHip, leftHip)] +
                            jointInitialDistances[new KeyValuePair<Transform, Transform>(rightShoulder, leftShoulder)]) / 2;
//		float playerLength = (Vector3.Distance(skeletonManager.skeletons[bodyTrackingDeviceID, playerId].rightShoulder.position, 
//		                                       skeletonManager.skeletons[bodyTrackingDeviceID, playerId].leftShoulder.position) +
//		                      Vector3.Distance(skeletonManager.skeletons[bodyTrackingDeviceID, playerId].rightHip.position, 
//		                 					   skeletonManager.skeletons[bodyTrackingDeviceID, playerId].leftHip.position)) / 2;
//		float playerLength = (Vector3.Distance( rightShoulder.position,  leftShoulder.position) + // *** THIS IS WRONG, SCALING APPLIES ON THESE TRANSFORMS
//		                      Vector3.Distance(      rightHip.position,       leftHip.position)  ) / 2;
		float playerLength = (Vector3.Distance( fourJointPositions[0], fourJointPositions[1]) +
		                      Vector3.Distance( skeletonManager.skeletons [bodyTrackingDeviceID, playerId].rightHip.position,
		                 						skeletonManager.skeletons [bodyTrackingDeviceID, playerId].leftHip.position)  ) / 2;
//		float playerLength = (Vector3.Distance( fourJointPositions[0], fourJointPositions[1]) +
//		                      Vector3.Distance( fourJointPositions[2], fourJointPositions[3])  ) / 2;
		
		float newScale = Mathf.Abs(playerLength / modelLength);

		// Here we halve the maxScaleFactor because the torso is bigger than the limbs
		torsoScale = Mathf.Lerp(torsoScale, newScale, 0.5f*maxScaleFactor * Time.deltaTime);

		if(scaleBoneLengthOnly)
		{
			switch(boneLengthAxis)
			{
			case RUISAxis.X: torso.localScale = new Vector3(torsoScale, torsoThickness*torsoScale, torsoThickness*torsoScale); break;
			case RUISAxis.Y: torso.localScale = new Vector3(torsoThickness*torsoScale, torsoScale, torsoThickness*torsoScale); break;
			case RUISAxis.Z: torso.localScale = new Vector3(torsoThickness*torsoScale, torsoThickness*torsoScale, torsoScale); break;
			}
		}
		else
			torso.localScale = new Vector3(torsoScale, torsoScale, torsoScale);
		return torsoScale;
	}
	
    private Quaternion FindFixingRotation(Vector3 fromJoint, Vector3 toJoint, Vector3 wantedDirection)
    {
        Vector3 boneVector = toJoint - fromJoint;
        return Quaternion.FromToRotation(boneVector, wantedDirection);
    }

    private void TweakNeckHeight()
    {
        if (!neck)
			return;
		neck.localPosition = neckOriginalLocalPosition + neck.InverseTransformDirection(spineDirection) * neckHeightTweaker/torsoScale;
    }

	private void TweakHipPosition()
	{
		if (!chest)
			return;
		// TODO: Below needs to be modified
		//chest.position -= hipOffset;
		chest.localPosition = chestOriginalLocalPosition - chest.InverseTransformDirection(spineDirection.normalized) * adjustVerticalHipsPosition/torsoScale;

	}

    public bool ConfidenceGoodEnoughForScaling()
    {
		return !(skeletonManager.skeletons[bodyTrackingDeviceID, playerId].rightShoulder.positionConfidence < minimumConfidenceToUpdate ||
		         skeletonManager.skeletons[bodyTrackingDeviceID, playerId].leftShoulder.positionConfidence < minimumConfidenceToUpdate ||
		         skeletonManager.skeletons[bodyTrackingDeviceID, playerId].rightHip.positionConfidence < minimumConfidenceToUpdate ||
		         skeletonManager.skeletons[bodyTrackingDeviceID, playerId].leftHip.positionConfidence < minimumConfidenceToUpdate);
    }

	private void handleFingersCurling(bool trackThumbs)
	{

		bool closeHand;
		int invert = 1;
		float rotationSpeed = 10.0f; // Per second
		Quaternion clenchedRotationThumbTM_corrected = Quaternion.identity;
		Quaternion clenchedRotationThumbIP_corrected = Quaternion.identity;
		
		leftHandStatus = (skeletonManager.skeletons [bodyTrackingDeviceID, playerId].leftHandStatus);
		rightHandStatus = (skeletonManager.skeletons [bodyTrackingDeviceID, playerId].rightHandStatus);
		
		if(leftHandStatus == RUISSkeletonManager.Skeleton.handState.unknown || leftHandStatus ==  RUISSkeletonManager.Skeleton.handState.pointing) 
		{
			leftHandStatus = lastLeftHandStatus;
		}
		
		if(rightHandStatus == RUISSkeletonManager.Skeleton.handState.unknown || rightHandStatus ==  RUISSkeletonManager.Skeleton.handState.pointing) 
		{
			rightHandStatus = lastRightHandStatus;
		}
		
		lastLeftHandStatus = leftHandStatus ;
		lastRightHandStatus = rightHandStatus;
		
		for (int i = 0; i < 2; i++)  
		{ // Hands
			if (i == 0) 
			{
				closeHand = (rightHandStatus  == RUISSkeletonManager.Skeleton.handState.closed);
				invert = -1;
			}
			else 
			{
				closeHand = (leftHandStatus == RUISSkeletonManager.Skeleton.handState.closed);	
				invert = 1;
			}
			// Thumb rotation correction: these depend on your animation rig
			switch(boneLengthAxis)
			{
			case RUISAxis.X:
				clenchedRotationThumbTM_corrected = Quaternion.Euler(clenchedRotationThumbTM.eulerAngles.x 
				                                                     * invert, clenchedRotationThumbTM.eulerAngles.y, clenchedRotationThumbTM.eulerAngles.z);
				clenchedRotationThumbIP_corrected = clenchedRotationThumbTM;
				break;
			case RUISAxis.Y:
				clenchedRotationThumbTM_corrected = clenchedRotationThumbTM;
				clenchedRotationThumbIP_corrected = Quaternion.Euler(clenchedRotationThumbIP.eulerAngles.x, 
				                                                     clenchedRotationThumbIP.eulerAngles.y, clenchedRotationThumbIP.eulerAngles.z * invert);
				break;
			case RUISAxis.Z:
				clenchedRotationThumbTM_corrected = Quaternion.Euler(clenchedRotationThumbTM.eulerAngles.x,
				                                                     clenchedRotationThumbTM.eulerAngles.y * invert, clenchedRotationThumbTM.eulerAngles.z);
				clenchedRotationThumbIP_corrected = clenchedRotationThumbTM;
				break;
			}

			for(int a = 0; a < 5; a++) 
			{ // Fingers
				if(!closeHand && !(a == 4 && trackThumbs)) 
				{
					if(fingerTransforms[i, a, 0])
						fingerTransforms[i, a, 0].localRotation = Quaternion.Slerp(fingerTransforms[i, a, 0].localRotation, initialFingerRotations[i, a, 0], Time.deltaTime * rotationSpeed);
					if(fingerTransforms[i, a, 1])
						fingerTransforms[i, a, 1].localRotation = Quaternion.Slerp(fingerTransforms[i, a, 1].localRotation, initialFingerRotations[i, a, 1], Time.deltaTime * rotationSpeed);
					if(fingerTransforms[i, a, 2])
						fingerTransforms[i, a, 2].localRotation = Quaternion.Slerp(fingerTransforms[i, a, 2].localRotation, initialFingerRotations[i, a, 2], Time.deltaTime * rotationSpeed);
					}
				else 
				{
					if(a != 4) 
					{
						if(fingerTransforms[i, a, 0])
							fingerTransforms[i, a, 0].localRotation = Quaternion.Slerp(fingerTransforms[i, a, 0].localRotation, clenchedRotationMCP, Time.deltaTime * rotationSpeed);
						if(fingerTransforms[i, a, 1])
							fingerTransforms[i, a, 1].localRotation = Quaternion.Slerp(fingerTransforms[i, a, 1].localRotation, clenchedRotationPIP, Time.deltaTime * rotationSpeed);
						if(fingerTransforms[i, a, 2])
							fingerTransforms[i, a, 2].localRotation = Quaternion.Slerp(fingerTransforms[i, a, 2].localRotation, clenchedRotationDIP, Time.deltaTime * rotationSpeed);
					}
					else if(!trackThumbs) 
					{ // Thumbs (if separate thumb  tracking is not enabled)
						if(fingerTransforms[i, a, 0])
							fingerTransforms[i, a, 0].localRotation = Quaternion.Slerp(fingerTransforms[i, a, 0].localRotation, clenchedRotationThumbTM_corrected, Time.deltaTime*rotationSpeed);
						if(fingerTransforms[i, a, 1])
							fingerTransforms[i, a, 1].localRotation = Quaternion.Slerp(fingerTransforms[i, a, 1].localRotation, clenchedRotationThumbMCP, Time.deltaTime * rotationSpeed);
						if(fingerTransforms[i, a, 2])
							fingerTransforms[i, a, 2].localRotation = Quaternion.Slerp(fingerTransforms[i, a, 2].localRotation, clenchedRotationThumbIP_corrected, Time.deltaTime * rotationSpeed);
					}	
				}	
			}
		}
	}

	private void saveInitialFingerRotations() 
	{	
		Transform handObject;
		
		for (int i = 0; i < 2; i++) { 
			if (i == 0) handObject = rightHand;
			else handObject = leftHand;

			if(handObject == null)
				continue;

			Transform[] fingers = handObject.GetComponentsInChildren<Transform> ();
			
			int fingerIndex = 0;
			int index = 0;
			foreach (Transform finger in fingers) 
			{
				if (finger.parent.transform.gameObject == handObject.transform.gameObject
				    && (finger.gameObject.name.Contains("finger") || finger.gameObject.name.Contains("Finger") || finger.gameObject.name.Contains("FINGER"))) 
				{
				
					if(fingerIndex > 4) break; // No mutant fingers allowed!
					
					if(finger == rightThumb || finger == leftThumb)
						index = 4; // Force thumb to have index == 4
					else 
					{
						index = fingerIndex;
						fingerIndex++;
					}
				
					// First bone
					initialFingerRotations[i, index, 0] = finger.localRotation;
					fingerTransforms[i, index, 0] = finger;
					Transform[] nextFingerParts = finger.gameObject.GetComponentsInChildren<Transform> ();
					foreach (Transform part1 in nextFingerParts) 
					{
						if (part1.parent.transform.gameObject == finger.gameObject
						    && (part1.gameObject.name.Contains("finger") || part1.gameObject.name.Contains("Finger") || part1.gameObject.name.Contains("FINGER"))) 
						{
							// Second bone
							initialFingerRotations[i, index, 1] = part1.localRotation;
							fingerTransforms[i, index, 1] = part1;
							Transform[] nextFingerParts2 = finger.gameObject.GetComponentsInChildren<Transform> ();
							foreach (Transform part2 in nextFingerParts2) 
							{
								if (part2.parent.transform.gameObject == part1.gameObject
								    && (part2.gameObject.name.Contains("finger") || part2.gameObject.name.Contains("Finger") || part2.gameObject.name.Contains("FINGER"))) 
								{
									// Third bone
									initialFingerRotations[i, index, 2] = part2.localRotation;
									fingerTransforms[i, index, 2] = part2; 
								}
							}
						}
					}
				}
			}	
		}
	}
	
	
	private Quaternion limitZRotation(Quaternion inputRotation, float rollMinimum, float rollMaximum)
	{
		/**
		 * Argument inputRotation's roll angle (rotation around Z axis) is clamped between [rollMinimum, rollMaximum].
		 * Works only if effective rotation around Y axis is zero. Rotation around X axis is allowed though.
		 **/
	 
		float rollAngle = 0;
		Vector3 limitedRoll = Vector3.zero;
		Quaternion outputRotation = inputRotation;
		
		// Calculate the rotation of inputRotation where roll (rotation around Z axis) is omitted
		Quaternion rotationWithoutRoll = Quaternion.LookRotation(inputRotation * Vector3.forward, 
		                                                         Vector3.Cross( inputRotation * Vector3.forward, Vector3.right)); 
		rollAngle = Quaternion.Angle(inputRotation, rotationWithoutRoll);
		
		// Is the roll to the left or to the right? Quaternion.Angle returns only positive values and omits rotation "direction"
		if((inputRotation*Vector3.up).x > 0)
			rollAngle *= -1;
		
		if(rollAngle > rollMaximum) // Rolling towards or over maximum angle
		{
			// Clamp to nearest limit
			if(rollAngle - rollMaximum < 0.5f*(360 - rollMaximum + rollMinimum))
				limitedRoll.z = rollMaximum;
			else
				limitedRoll.z = rollMinimum;
			outputRotation = rotationWithoutRoll * Quaternion.Euler(limitedRoll);
		}
		if(rollAngle < rollMinimum) // Rolling towards or below minimum angle
		{
			// Clamp to nearest limit
			if(rollMinimum - rollAngle < 0.5f*(360 - rollMaximum + rollMinimum))
				limitedRoll.z = rollMinimum;
			else
				limitedRoll.z = rollMaximum;
			outputRotation = rotationWithoutRoll * Quaternion.Euler(limitedRoll);
		}
		
		print (rollAngle + " " + rotationWithoutRoll.eulerAngles);
		
		return outputRotation;
	}
	
}
