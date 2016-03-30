/*****************************************************************************

Content    :   Inspector behaviour for RUISTracker script
Authors    :   Tuukka Takala
Copyright  :   Copyright 2013 Tuukka Takala, Mikael Matveinen. All Rights reserved.
Licensing  :   RUIS is distributed under the LGPL Version 3 license.

******************************************************************************/

using UnityEngine;
using UnityEditor;
using System.Collections;
using Ovr;

[CustomEditor(typeof(RUISTracker))]
[CanEditMultipleObjects]
public class RUISTrackerEditor : Editor
{
	RUISTracker trackerScript;
	OVRCameraRig ovrCameraRig;
	bool riftFound = false;
	string movingBaseAnnouncement = "";
	
	GameObject skeletonManagerGameObject;
	int maxKinectSkeletons = 4;
	int maxPSMoveControllers = 4;
	float minNoiseCovariance = 0.001f;
	float minDriftCorrectionRate = 0.001f;
	float maxDriftCorrectionRate = 1000;

    SerializedProperty defaultPosition;
    SerializedProperty skeletonManager;
    SerializedProperty headPositionInput;
    SerializedProperty headRotationInput;
	SerializedProperty pickRotationSource;
    SerializedProperty oculusID;
	SerializedProperty resetKey;
    SerializedProperty positionPlayerID;
    SerializedProperty rotationPlayerID;
    SerializedProperty positionJoint;
    SerializedProperty rotationJoint;
    SerializedProperty positionPSMoveID;
    SerializedProperty rotationPSMoveID;
	SerializedProperty positionRazerID;
	SerializedProperty rotationRazerID;
    SerializedProperty positionInput;
    SerializedProperty rotationInput;
    SerializedProperty positionOffsetKinect;
    SerializedProperty positionOffsetPSMove;
    SerializedProperty positionOffsetHydra;
	SerializedProperty positionOffsetOculus;
    SerializedProperty rotationOffsetKinect;
    SerializedProperty rotationOffsetPSMove;
    SerializedProperty rotationOffsetHydra;
	SerializedProperty filterPositionKinect;
	SerializedProperty filterPositionPSMove;
	SerializedProperty filterPositionHydra;
	SerializedProperty filterPositionTransform;
	SerializedProperty positionNoiseCovarianceKinect;
	SerializedProperty positionNoiseCovariancePSMove;
	SerializedProperty positionNoiseCovarianceHydra;
	SerializedProperty positionNoiseCovarianceTransform;
	SerializedProperty filterRotationKinect;
	SerializedProperty filterRotationPSMove;
	SerializedProperty filterRotationHydra;
	SerializedProperty filterRotationTransform;
	SerializedProperty rotationNoiseCovarianceKinect;
	SerializedProperty rotationNoiseCovariancePSMove;
	SerializedProperty rotationNoiseCovarianceHydra;
	SerializedProperty rotationNoiseCovarianceTransform;
	SerializedProperty isRazerBaseMobile;
	SerializedProperty mobileRazerBase;
	SerializedProperty hydraBaseInput;
	SerializedProperty hydraBaseJoint;
	SerializedProperty hydraBaseKinectPlayerID;	
	SerializedProperty filterHydraBasePoseKinect;
	SerializedProperty filterHydraBasePoseTransform;
	SerializedProperty inferBaseRotationFromRotationTrackerKinect;
	SerializedProperty inferBaseRotationFromRotationTrackerTransform;
	SerializedProperty hydraAtRotationTrackerOffset;
	SerializedProperty hydraBasePositionOffsetKinect;
	SerializedProperty hydraBaseRotationOffsetKinect;
	SerializedProperty hydraBasePositionCovarianceKinect;
	SerializedProperty hydraBaseRotationCovarianceKinect;
	SerializedProperty hydraBasePositionCovarianceTransform;
	SerializedProperty hydraBaseRotationCovarianceTransform;
    SerializedProperty externalDriftCorrection;
    SerializedProperty compass;
	SerializedProperty compassIsPositionTracker;
    SerializedProperty compassPlayerID;
    SerializedProperty compassJoint;
    SerializedProperty correctOnlyWhenFacingForward;
    SerializedProperty compassPSMoveID;
	SerializedProperty compassRazerID;
    SerializedProperty compassTransform;
	SerializedProperty compassRotationOffsetKinect;
	SerializedProperty compassRotationOffsetPSMove;
	SerializedProperty compassRotationOffsetHydra;
	SerializedProperty driftCorrectionRateKinect;
	SerializedProperty driftCorrectionRatePSMove;
	SerializedProperty driftCorrectionRateHydra;
	SerializedProperty driftCorrectionRateTransform;
    //SerializedProperty driftNoiseCovariance;
	SerializedProperty enableVisualizers;
    SerializedProperty driftingDirectionVisualizer;
    SerializedProperty compassDirectionVisualizer;
    SerializedProperty correctedDirectionVisualizer;
    SerializedProperty driftVisualizerPosition;

    public void OnEnable()
    {
		skeletonManagerGameObject = GameObject.Find("SkeletonManager");
		
		if(skeletonManagerGameObject != null)
		{
			RUISSkeletonManager playerManager = skeletonManagerGameObject.GetComponent<RUISSkeletonManager>();
			if(playerManager != null)
				maxKinectSkeletons = playerManager.skeletonsHardwareLimit;
		}
		
		defaultPosition = serializedObject.FindProperty("defaultPosition");
	    //skeletonManager = serializedObject.FindProperty("skeletonManager");
	    headPositionInput = serializedObject.FindProperty("headPositionInput");
	    headRotationInput = serializedObject.FindProperty("headRotationInput");
        pickRotationSource = serializedObject.FindProperty("pickRotationSource");
	    oculusID = serializedObject.FindProperty("oculusID"); //
		resetKey = serializedObject.FindProperty("resetKey");
	    positionPlayerID = serializedObject.FindProperty("positionPlayerID");
	    rotationPlayerID = serializedObject.FindProperty("rotationPlayerID");
	    positionJoint = serializedObject.FindProperty("positionJoint");
	    rotationJoint = serializedObject.FindProperty("rotationJoint");
	    positionPSMoveID = serializedObject.FindProperty("positionPSMoveID");
	    rotationPSMoveID = serializedObject.FindProperty("rotationPSMoveID");
		positionRazerID = serializedObject.FindProperty("positionRazerID");
		rotationRazerID = serializedObject.FindProperty("rotationRazerID");
	    positionInput = serializedObject.FindProperty("positionInput");
	    rotationInput = serializedObject.FindProperty("rotationInput");
	    positionOffsetKinect = serializedObject.FindProperty("positionOffsetKinect");
	    positionOffsetPSMove = serializedObject.FindProperty("positionOffsetPSMove");
	    positionOffsetHydra = serializedObject.FindProperty("positionOffsetHydra");
		positionOffsetOculus = serializedObject.FindProperty("positionOffsetOculus");
	    rotationOffsetKinect = serializedObject.FindProperty("rotationOffsetKinect");
	    rotationOffsetPSMove = serializedObject.FindProperty("rotationOffsetPSMove");
	    rotationOffsetHydra = serializedObject.FindProperty("rotationOffsetHydra");
		filterPositionKinect = serializedObject.FindProperty("filterPositionKinect");
		filterPositionPSMove = serializedObject.FindProperty("filterPositionPSMove");
		filterPositionHydra = serializedObject.FindProperty("filterPositionHydra");
		filterPositionTransform = serializedObject.FindProperty("filterPositionTransform");
		positionNoiseCovarianceKinect = serializedObject.FindProperty("positionNoiseCovarianceKinect");
		positionNoiseCovariancePSMove = serializedObject.FindProperty("positionNoiseCovariancePSMove");
		positionNoiseCovarianceHydra = serializedObject.FindProperty("positionNoiseCovarianceHydra");
		positionNoiseCovarianceTransform = serializedObject.FindProperty("positionNoiseCovarianceTransform");
		filterRotationKinect = serializedObject.FindProperty("filterRotationKinect");
		filterRotationPSMove = serializedObject.FindProperty("filterRotationPSMove");
		filterRotationHydra = serializedObject.FindProperty("filterRotationHydra");
		filterRotationTransform = serializedObject.FindProperty("filterRotationTransform");
		rotationNoiseCovarianceKinect = serializedObject.FindProperty("rotationNoiseCovarianceKinect");
		rotationNoiseCovariancePSMove = serializedObject.FindProperty("rotationNoiseCovariancePSMove");
		rotationNoiseCovarianceHydra = serializedObject.FindProperty("rotationNoiseCovarianceHydra");
		rotationNoiseCovarianceTransform = serializedObject.FindProperty("rotationNoiseCovarianceTransform");
		isRazerBaseMobile = serializedObject.FindProperty("isRazerBaseMobile");
		mobileRazerBase = serializedObject.FindProperty("mobileRazerBase");
		hydraBaseJoint = serializedObject.FindProperty("hydraBaseJoint");
		hydraBaseKinectPlayerID = serializedObject.FindProperty("hydraBaseKinectPlayerID");
		hydraBaseInput = serializedObject.FindProperty("hydraBaseInput");
		filterHydraBasePoseKinect = serializedObject.FindProperty("filterHydraBasePoseKinect");
		filterHydraBasePoseTransform = serializedObject.FindProperty("filterHydraBasePoseTransform");
		inferBaseRotationFromRotationTrackerKinect = serializedObject.FindProperty("inferBaseRotationFromRotationTrackerKinect");
		inferBaseRotationFromRotationTrackerTransform = serializedObject.FindProperty("inferBaseRotationFromRotationTrackerTransform");
		hydraAtRotationTrackerOffset = serializedObject.FindProperty("hydraAtRotationTrackerOffset");
		hydraBasePositionOffsetKinect = serializedObject.FindProperty("hydraBasePositionOffsetKinect");
		hydraBaseRotationOffsetKinect = serializedObject.FindProperty("hydraBaseRotationOffsetKinect");
		hydraBasePositionCovarianceKinect = serializedObject.FindProperty("hydraBasePositionCovarianceKinect");
		hydraBaseRotationCovarianceKinect = serializedObject.FindProperty("hydraBaseRotationCovarianceKinect");
		hydraBasePositionCovarianceTransform = serializedObject.FindProperty("hydraBasePositionCovarianceTransform");
		hydraBaseRotationCovarianceTransform = serializedObject.FindProperty("hydraBaseRotationCovarianceTransform");
	    externalDriftCorrection = serializedObject.FindProperty("externalDriftCorrection");
	    compass = serializedObject.FindProperty("compass");
		compassIsPositionTracker = serializedObject.FindProperty("compassIsPositionTracker");
	    compassPlayerID = serializedObject.FindProperty("compassPlayerID");
	    compassJoint = serializedObject.FindProperty("compassJoint");
	    correctOnlyWhenFacingForward = serializedObject.FindProperty("correctOnlyWhenFacingForward");
	    compassPSMoveID = serializedObject.FindProperty("compassPSMoveID");
		compassRazerID = serializedObject.FindProperty("compassRazerID");
	    compassTransform = serializedObject.FindProperty("compassTransform");
		compassRotationOffsetKinect = serializedObject.FindProperty("compassRotationOffsetKinect");
		compassRotationOffsetPSMove = serializedObject.FindProperty("compassRotationOffsetPSMove");
		compassRotationOffsetHydra = serializedObject.FindProperty("compassRotationOffsetHydra");
		driftCorrectionRateKinect 		= serializedObject.FindProperty("driftCorrectionRateKinect");
		driftCorrectionRatePSMove 		= serializedObject.FindProperty("driftCorrectionRatePSMove");
		driftCorrectionRateHydra 		= serializedObject.FindProperty("driftCorrectionRateHydra");
		driftCorrectionRateTransform 	= serializedObject.FindProperty("driftCorrectionRateTransform");
	    //driftNoiseCovariance = serializedObject.FindProperty("driftNoiseCovariance");
		enableVisualizers = serializedObject.FindProperty("enableVisualizers");
	    driftingDirectionVisualizer = serializedObject.FindProperty("driftingDirectionVisualizer");
	    compassDirectionVisualizer = serializedObject.FindProperty("compassDirectionVisualizer");
	    correctedDirectionVisualizer = serializedObject.FindProperty("correctedDirectionVisualizer");
	    driftVisualizerPosition = serializedObject.FindProperty("driftVisualizerPosition");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(defaultPosition, new GUIContent("Default Position (meters)", "Head position before tracking starts"));
        //EditorGUILayout.PropertyField(skeletonManager, new GUIContent("skeletonManager", "Can be None"));

		if(serializedObject.targetObject is RUISTracker)
		{
			trackerScript = (RUISTracker) serializedObject.targetObject;
			if(trackerScript)
				ovrCameraRig = trackerScript.gameObject.GetComponentInChildren<OVRCameraRig>();
			if(ovrCameraRig)
			{
				riftFound = true;
			}
			else
			{
				riftFound = false;
			}
		}
		
		if(!riftFound)
		{
			EditorGUILayout.PropertyField(pickRotationSource, new GUIContent( "Pick Rotation Source", "If disabled, then the Rotation "
																			+ "Tracker is same as Position Tracker"));
		}
		
        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(headPositionInput, new GUIContent("Position Tracker", "Device that tracks the head position"));
		
        EditorGUI.indentLevel += 2;
        switch (headPositionInput.enumValueIndex)
        {
			case (int)RUISTracker.HeadPositionSource.OculusDK2:
				EditorGUILayout.PropertyField(positionOffsetOculus, new GUIContent("Position Offset (meters)", "Adds an position offset to Oculus Rift's "
				                                                                   + "tracked position. This should be zero when using Oculus Rift positional "
				                                                                   + "tracking together with Kinect skeleton tracking."));

				break;
            case (int)RUISTracker.HeadPositionSource.Kinect1:
			case (int)RUISTracker.HeadPositionSource.Kinect2:
				positionPlayerID.intValue = Mathf.Clamp(positionPlayerID.intValue, 0, maxKinectSkeletons - 1);
				if(positionNoiseCovarianceKinect.floatValue < minNoiseCovariance)
					positionNoiseCovarianceKinect.floatValue = minNoiseCovariance;
                EditorGUILayout.PropertyField(positionPlayerID, new GUIContent("Kinect Player Id", "Between 0 and 3"));

                EditorGUILayout.PropertyField(positionJoint, new GUIContent("Joint", "Head is the best joint for tracking head position"));
                EditorGUILayout.PropertyField(positionOffsetKinect, new GUIContent("Position Offset (meters)", "Kinect joint's position in "
                															+ "the tracked object's local coordinate system. Set these values "
																			+ "according to the joint's offset from the tracked object's "
																			+ "origin (head etc.). When using Kinect for head tracking, then zero " 
																			+ "vector is the best choice if head is the position Joint."));
		        EditorGUILayout.PropertyField(filterPositionKinect, new GUIContent("Filter Position", "Enables simple Kalman filtering for position "
																			+ "tracking. Recommended for Kinect."));
				if(filterPositionKinect.boolValue)
			        EditorGUILayout.PropertyField(positionNoiseCovarianceKinect, new GUIContent("Filter Strength", "Noise covariance of Kalman filtering: " 
																			+ "a bigger value means smoother results but a slower "
																			+ "response to changes."));
                break;
            case (int)RUISTracker.HeadPositionSource.PSMove:
				positionPSMoveID.intValue = Mathf.Clamp(positionPSMoveID.intValue, 0, maxPSMoveControllers - 1);
				if(positionNoiseCovariancePSMove.floatValue < minNoiseCovariance)
					positionNoiseCovariancePSMove.floatValue = minNoiseCovariance;
                EditorGUILayout.PropertyField(positionPSMoveID, new GUIContent("PS Move ID", "Between 0 and 3"));
                EditorGUILayout.PropertyField(positionOffsetPSMove, new GUIContent("Position Offset (meters)", "PS Move controller's position in "
                															+ "the tracked object's local coordinate system. Set these values "
																			+ "according to the controller's offset from the tracked object's "
																			+ "origin (head etc.)."));
		        EditorGUILayout.PropertyField(filterPositionPSMove, new GUIContent("Filter Position", "Enables simple Kalman filtering for position "
																			+ "tracking. Best left disabled for PS Move."));
				if(filterPositionPSMove.boolValue)
			        EditorGUILayout.PropertyField(positionNoiseCovariancePSMove, new GUIContent("Filter Strength", "Noise covariance of Kalman filtering: " 
																			+ "a bigger value means smoother results but a slower "
																			+ "response to changes."));
                break;
            case (int)RUISTracker.HeadPositionSource.RazerHydra:
				if(positionNoiseCovarianceHydra.floatValue < minNoiseCovariance)
					positionNoiseCovarianceHydra.floatValue = minNoiseCovariance;
			
		        EditorGUILayout.PropertyField(isRazerBaseMobile, new GUIContent("Moving Base Station", "Enable this if the Razer Hydra base station is "
																			+ "attached to something that is moving (e.g. Kinect tracked player's belt)"));
				
			    EditorGUILayout.PropertyField(positionRazerID, new GUIContent("Razer Hydra ID", "Either LEFT or RIGHT"));
                EditorGUILayout.PropertyField(positionOffsetHydra, new GUIContent("Position Offset (meters)", "Razer Hydra controller's position in "
                															+ "the tracked object's local coordinate system. Set these values "
																			+ "according to the controller's offset from the tracked object's "
																			+ "origin (head etc.)."));
		        EditorGUILayout.PropertyField(filterPositionHydra, new GUIContent("Filter Position", "Enables simple Kalman filtering for position "
																			+ "tracking. Best left disabled for Razer Hydra."));
				if(filterPositionHydra.boolValue)
			        EditorGUILayout.PropertyField(positionNoiseCovarianceHydra, new GUIContent("Filter Strength", "Noise covariance of Kalman filtering: " 
																			+ "a bigger value means smoother results but a slower "
																			+ "response to changes."));
				break;
            case (int)RUISTracker.HeadPositionSource.InputTransform:
				if(positionNoiseCovarianceTransform.floatValue < minNoiseCovariance)
					positionNoiseCovarianceTransform.floatValue = minNoiseCovariance;
                EditorGUILayout.PropertyField(positionInput, new GUIContent("Input Transform", "All other position trackers are supported "
																			+ "through this transform. Drag and drop here a transform "
																			+ "whose position is controlled by a tracking device."));
		        EditorGUILayout.PropertyField(filterPositionTransform, new GUIContent("Filter Position", "Enables simple Kalman filtering for position "
																			+ "tracking."));
				if(filterPositionTransform.boolValue)
			        EditorGUILayout.PropertyField(positionNoiseCovarianceTransform, new GUIContent("Filter Strength", "Noise covariance of Kalman filtering: " 
																			+ "a bigger value means smoother results but a slower "
																			+ "response to changes."));
				break;
        }
		
//		if(headPositionInput.enumValueIndex != (int)RUISTracker.HeadPositionSource.None)
//		{
//	        EditorGUILayout.PropertyField(filterPosition, new GUIContent("Filter Position", "Enables simple Kalman filtering for position "
//																					+ "tracking. Only recommended for Kinect."));
//	        EditorGUILayout.PropertyField(positionNoiseCovariance, new GUIContent("Filter Strength", "Noise covariance of Kalman filtering: " 
//																					+ "a bigger value means smoother results but a slower "
//																					+ "response to changes."));
//		}
				
        EditorGUI.indentLevel -= 2;

		
        EditorGUILayout.Space();
		
		if(riftFound)
		{
			
			EditorGUILayout.LabelField("Rotation Tracker:    Oculus Rift", EditorStyles.boldLabel);
        	EditorGUI.indentLevel += 2;
			
        	EditorGUILayout.PropertyField(oculusID, new GUIContent("Oculus Rift ID", "Choose which Rift is the source of the head tracking. "
																	+ "Leave this to 0 (multiple Rifts are not supported yet)."));
			
			
			EditorStyles.textField.wordWrap = true;
			EditorGUILayout.TextArea(typeof(OVRCameraRig) + " script detected in a child object of this " + trackerScript.gameObject.name
										+ ". Assuming that you want to use rotation from Oculus Rift. Disabling other Rotation Tracker "
										+ "options. You can access other rotation trackers when you remove or disable the child object "
			                         + "that has the " + typeof(OVRCameraRig) + " component.", GUILayout.Height(120));
			
			EditorGUILayout.LabelField( new GUIContent("Reset Orientation Button(s):", "The button(s) that reset Oculus Rift's yaw "
										+ "rotation to zero."), 
										EditorStyles.boldLabel);
        	EditorGUI.indentLevel += 1;
			EditorGUILayout.PropertyField(resetKey, new GUIContent("KeyCode", "The button that resets Oculus Rift's yaw rotation to zero."));
			
			if(externalDriftCorrection.boolValue && compass.enumValueIndex == (int)RUISTracker.CompassSource.RazerHydra)
				EditorGUILayout.LabelField(new GUIContent(("BUMPER+START Razer Hydra " + compassRazerID.enumNames[compassRazerID.enumValueIndex]),
										   "BUMPER and START of the Razer Hydra controller that you use for Yaw Drift Correction."),
										   EditorStyles.label);
			else if(headPositionInput.enumValueIndex == (int)RUISTracker.HeadPositionSource.RazerHydra)
				EditorGUILayout.LabelField(new GUIContent(("BUMPER+START Razer Hydra " + positionRazerID.enumNames[positionRazerID.enumValueIndex]),
										   "BUMPER and START of the Razer Hydra controller that you use for position tracking."),
										   EditorStyles.label);
			if(externalDriftCorrection.boolValue && compass.enumValueIndex == (int)RUISTracker.CompassSource.PSMove)
				EditorGUILayout.LabelField(new GUIContent(("MOVE button on PS Move #" + compassPSMoveID.intValue),
										   "MOVE button of the PS Move controller that you use for Yaw Drift Correction."),
										   EditorStyles.label);
			else if(headPositionInput.enumValueIndex == (int)RUISTracker.HeadPositionSource.PSMove)
				EditorGUILayout.LabelField(new GUIContent(("MOVE button on PS Move #" + positionPSMoveID.intValue),
										   "MOVE button of the PS Move controller that you use for position tracking."),
										   EditorStyles.label);
        	EditorGUI.indentLevel -= 1;
		}
		else
		{
			if(!pickRotationSource.boolValue)
			{
				switch (headPositionInput.enumValueIndex)
        		{	
					case (int)RUISTracker.HeadPositionSource.Kinect1:
					{
						headRotationInput.enumValueIndex = (int)RUISTracker.HeadRotationSource.Kinect1;
						rotationPlayerID.intValue = positionPlayerID.intValue;
						rotationJoint.enumValueIndex = positionJoint.enumValueIndex;
						break;
					}
					case (int)RUISTracker.HeadPositionSource.Kinect2:
					{
						headRotationInput.enumValueIndex = (int)RUISTracker.HeadRotationSource.Kinect2;
						rotationPlayerID.intValue = positionPlayerID.intValue;
						rotationJoint.enumValueIndex = positionJoint.enumValueIndex;
						break;
					}
					case (int)RUISTracker.HeadPositionSource.PSMove:
					{
						headRotationInput.enumValueIndex = (int)RUISTracker.HeadRotationSource.PSMove;
						rotationPSMoveID.intValue = positionPSMoveID.intValue;
						break;
					}
					case (int)RUISTracker.HeadPositionSource.RazerHydra:
					{
						headRotationInput.enumValueIndex = (int)RUISTracker.HeadRotationSource.RazerHydra;
						rotationRazerID.intValue = positionRazerID.intValue;
						break;
					}
					case (int)RUISTracker.HeadPositionSource.InputTransform:
					{
						headRotationInput.enumValueIndex = (int)RUISTracker.HeadRotationSource.InputTransform;
						rotationInput.objectReferenceValue = positionInput.objectReferenceValue;
						break;
					}
					case (int)RUISTracker.HeadPositionSource.None:
					{
						headRotationInput.enumValueIndex = (int)RUISTracker.HeadRotationSource.None;
						break;
					}
				}
			}
			
			EditorGUI.BeginDisabledGroup(!pickRotationSource.boolValue);
        	EditorGUILayout.PropertyField(headRotationInput, new GUIContent("Rotation Tracker", "Device that tracks the head rotation"));
			EditorGUI.EndDisabledGroup();
			
        	EditorGUI.indentLevel += 2;
			
	        switch (headRotationInput.enumValueIndex)
	        {
	            case (int)RUISTracker.HeadRotationSource.Kinect1:
				case (int)RUISTracker.HeadRotationSource.Kinect2:
					rotationPlayerID.intValue = Mathf.Clamp(rotationPlayerID.intValue, 0, maxKinectSkeletons - 1);
					if(rotationNoiseCovarianceKinect.floatValue < minNoiseCovariance)
						rotationNoiseCovarianceKinect.floatValue = minNoiseCovariance;
					EditorGUI.BeginDisabledGroup(!pickRotationSource.boolValue);
	                EditorGUILayout.PropertyField(rotationPlayerID, new GUIContent("Kinect Player ID", "Between 0 and 3"));
	                EditorGUILayout.PropertyField(rotationJoint, new GUIContent("Joint", "ATTENTION: Torso has most stable joint rotation "
																				+ "for head tracking! Currently OpenNI's head joint rotation "
																				+ "is always the same as torso rotation, except its tracking "
																				+ "fails more often."));
					EditorGUI.EndDisabledGroup();
	                EditorGUILayout.PropertyField(rotationOffsetKinect, new GUIContent("Rotation Offset", "Tracked joint's rotation in tracked "
																				+ "object's local coordinate system. If using Kinect for head "
																				+ "tracking, then zero vector is usually the best choice if "
																				+ "torso or head is the Rotation Joint."));
			        EditorGUILayout.PropertyField(filterRotationKinect, new GUIContent("Filter Rotation", "Enables simple Kalman filtering for rotation "
																				+ "tracking. Recommended for Kinect."));
					if(filterRotationKinect.boolValue)
				        EditorGUILayout.PropertyField(rotationNoiseCovarianceKinect, new GUIContent("Filter Strength", "Noise covariance of Kalman filtering: " 
																				+ "a bigger value means smoother results but a slower "
																				+ "response to changes."));
	                break;
	            case (int)RUISTracker.HeadRotationSource.PSMove:
					rotationPSMoveID.intValue = Mathf.Clamp(rotationPSMoveID.intValue, 0, maxPSMoveControllers - 1);
					if(rotationNoiseCovariancePSMove.floatValue < minNoiseCovariance)
						rotationNoiseCovariancePSMove.floatValue = minNoiseCovariance;
					EditorGUI.BeginDisabledGroup(!pickRotationSource.boolValue);
	                EditorGUILayout.PropertyField(rotationPSMoveID, new GUIContent("PS Move ID", "Between 0 and 3"));
					EditorGUI.EndDisabledGroup();
	                EditorGUILayout.PropertyField(rotationOffsetPSMove, new GUIContent("Rotation Offset", "Tracked PS Move controller's "
																				+ "rotation in tracked object's local coordinate system. "
																				+ "Set these euler angles according to the orientation in "
																				+ "which Move is attached to the tracked object (head etc.)."));
			        EditorGUILayout.PropertyField(filterRotationPSMove, new GUIContent("Filter Rotation", "Enables simple Kalman filtering for rotation "
																				+ "tracking. Best left disabled for PS Move."));
					if(filterRotationPSMove.boolValue)
				        EditorGUILayout.PropertyField(rotationNoiseCovariancePSMove, new GUIContent("Filter Strength", "Noise covariance of Kalman filtering: " 
																				+ "a bigger value means smoother results but a slower "
																				+ "response to changes."));
	                break;
	            case (int)RUISTracker.HeadRotationSource.RazerHydra:
					if(rotationNoiseCovarianceHydra.floatValue < minNoiseCovariance)
						rotationNoiseCovarianceHydra.floatValue = minNoiseCovariance;
					
					EditorGUI.BeginDisabledGroup(!pickRotationSource.boolValue);
			        EditorGUILayout.PropertyField(isRazerBaseMobile, new GUIContent("Moving Base Station", "Enable this if the Razer Hydra base station is "
																				+ "attached to something that is moving (e.g. Kinect tracked player's "
																				+ "belt)"));
	                EditorGUILayout.PropertyField(rotationRazerID, new GUIContent("Razer Hydra ID", "Either LEFT or RIGHT"));
					EditorGUI.EndDisabledGroup();
	                EditorGUILayout.PropertyField(rotationOffsetHydra, new GUIContent("Rotation Offset", "Tracked Razer Hydra controller's "
																				+ "rotation in tracked object's local coordinate system. "
																				+ "Set these euler angles according to the orientation in which "
																				+ "the Razer Hydra is attached to the tracked object (head etc.)."));
			        EditorGUILayout.PropertyField(filterRotationHydra, new GUIContent("Filter Rotation", "Enables simple Kalman filtering for rotation "
																				+ "tracking. Best left disabled for Razer Hydra."));
					if(filterRotationHydra.boolValue)
				        EditorGUILayout.PropertyField(rotationNoiseCovarianceHydra, new GUIContent("Filter Strength", "Noise covariance of Kalman filtering: " 
																				+ "a bigger value means smoother results but a slower "
																				+ "response to changes."));
					break;
	            case (int)RUISTracker.HeadRotationSource.InputTransform:
					if(rotationNoiseCovarianceTransform.floatValue < minNoiseCovariance)
						rotationNoiseCovarianceTransform.floatValue = minNoiseCovariance;
					EditorGUI.BeginDisabledGroup(!pickRotationSource.boolValue);
	                EditorGUILayout.PropertyField(rotationInput, new GUIContent("Input Transform", "All other rotation trackers are supported "
																				+ "through this transform. Drag and drop here a transform "
																				+ "whose rotation is controlled by a tracking device."));
					EditorGUI.EndDisabledGroup();
			        EditorGUILayout.PropertyField(filterRotationTransform, new GUIContent("Filter Rotation", "Enables simple Kalman filtering "
																				+ "for rotation tracking."));
					if(filterRotationTransform.boolValue)
				        EditorGUILayout.PropertyField(rotationNoiseCovarianceTransform, new GUIContent("Filter Strength", "Noise covariance of Kalman " 
																				+ "filtering: a bigger value means smoother results but a slower "
																				+ "response to changes."));
					break;
	        }
			
//			if(headRotationInput.enumValueIndex != (int)RUISTracker.HeadRotationSource.None)
//			{
//		        EditorGUILayout.PropertyField(filterRotation, new GUIContent("Filter Rotation", "Enables simple Kalman filtering for rotation "
//																			+ "tracking. Only recommended for Kinect."));
//		        EditorGUILayout.PropertyField(rotationNoiseCovariance, new GUIContent("Filter Strength", "Noise covariance of Kalman filtering: " 
//																			+ "a bigger value means smoother results but a slower "
//																			+ "response to changes."));
//			}
		}
		
        EditorGUI.indentLevel -= 2;
				
        EditorGUILayout.Space();

		
		if(!riftFound && headRotationInput.enumValueIndex != (int)RUISTracker.HeadRotationSource.InputTransform)
		{
			EditorGUI.BeginDisabledGroup(true);
	        EditorGUILayout.PropertyField(externalDriftCorrection, new GUIContent("Yaw Drift Correction", "Enables external yaw drift correction "
																		+ "using Kinect, PS Move, or some other device"));
			if(	  headRotationInput.enumValueIndex == (int)RUISTracker.HeadRotationSource.Kinect1
			   || headRotationInput.enumValueIndex == (int)RUISTracker.HeadRotationSource.Kinect2)
				EditorGUILayout.LabelField("Kinect joints don't need drift correction");
			if(headRotationInput.enumValueIndex == (int)RUISTracker.HeadRotationSource.PSMove)
				EditorGUILayout.LabelField("PS Move doesn't need drift correction");
			if(headRotationInput.enumValueIndex == (int)RUISTracker.HeadRotationSource.RazerHydra)
				EditorGUILayout.LabelField("Razer Hydra doesn't need drift correction");
			if(headRotationInput.enumValueIndex == (int)RUISTracker.HeadRotationSource.None)
				EditorGUILayout.LabelField("No Rotation Tracker: Drift correction disabled");
			EditorGUI.EndDisabledGroup();
		}
		else
		{
	        EditorGUILayout.PropertyField(externalDriftCorrection, new GUIContent("Yaw Drift Correction", "Enables external yaw drift correction "
																				+ "using Kinect, PS Move, or some other device"));
			if(externalDriftCorrection.boolValue)
			{
			
		        EditorGUI.indentLevel += 2;
				
				EditorGUILayout.PropertyField(compassIsPositionTracker, new GUIContent("Use Position Tracker", "If enabled, rotation from the "
																					+ "above Position Tracker will act as a compass that "
																					+ "corrects yaw drift of the Rotation Tracker"));
				
				if(compassIsPositionTracker.boolValue)
				{
					switch(headPositionInput.enumValueIndex)
					{
						case (int)RUISTracker.HeadPositionSource.Kinect1:
						{
							compass.enumValueIndex = (int)RUISTracker.CompassSource.Kinect1;
							compassPlayerID.intValue = positionPlayerID.intValue;
							compassJoint.enumValueIndex = positionJoint.enumValueIndex;
							break;
						}
						case (int)RUISTracker.HeadPositionSource.Kinect2:
						{
							compass.enumValueIndex = (int)RUISTracker.CompassSource.Kinect2;
							compassPlayerID.intValue = positionPlayerID.intValue;
							compassJoint.enumValueIndex = positionJoint.enumValueIndex;
							break;
						}
						case (int)RUISTracker.HeadPositionSource.PSMove:
						{
							compass.enumValueIndex = (int)RUISTracker.CompassSource.PSMove;
							compassPSMoveID.intValue = positionPSMoveID.intValue;
							break;
						}
						case (int)RUISTracker.HeadPositionSource.RazerHydra:
						{
							compass.enumValueIndex = (int)RUISTracker.CompassSource.RazerHydra;
							compassRazerID.enumValueIndex = positionRazerID.enumValueIndex;
							break;
						}
						case (int)RUISTracker.HeadPositionSource.InputTransform:
						{
							compass.enumValueIndex = (int)RUISTracker.CompassSource.InputTransform;
							compassTransform.objectReferenceValue = positionInput.objectReferenceValue;
							break;
						}
						case (int)RUISTracker.HeadPositionSource.None:
						{
							compass.enumValueIndex = (int)RUISTracker.CompassSource.None;
							break;
						}
					}
				}
				
				
				EditorGUI.BeginDisabledGroup(compassIsPositionTracker.boolValue);
		        EditorGUILayout.PropertyField(compass, new GUIContent("Compass Tracker", "Tracker that will be used to correct the yaw drift of "
																	+ "Rotation Tracker"));
				EditorGUI.EndDisabledGroup();
				
				if(compassIsPositionTracker.boolValue && headPositionInput.enumValueIndex == (int)RUISTracker.HeadPositionSource.None)
					EditorGUILayout.LabelField("Position Tracker is set to None!");
				else
			        switch (compass.enumValueIndex)
			        {
						case (int)RUISTracker.CompassSource.Kinect1:
						case (int)RUISTracker.CompassSource.Kinect2:
							compassPlayerID.intValue = Mathf.Clamp(compassPlayerID.intValue, 0, maxKinectSkeletons - 1);
							driftCorrectionRateKinect.floatValue = Mathf.Clamp(driftCorrectionRateKinect.floatValue, minDriftCorrectionRate, 
																													 maxDriftCorrectionRate );
							EditorGUI.BeginDisabledGroup(compassIsPositionTracker.boolValue);
			                EditorGUILayout.PropertyField(compassPlayerID, new GUIContent("Kinect Player ID", "Between 0 and 3"));
			                EditorGUILayout.PropertyField(compassJoint, new GUIContent("Compass Joint", "ATTENTION: Torso has most stable "
																				+ "joint rotation for drift correction! Currently OpenNI's "
																				+ "head joint rotation is always the same as torso rotation, "
																				+ "except its tracking fails more often."));
							EditorGUI.EndDisabledGroup();
			                EditorGUILayout.PropertyField(compassRotationOffsetKinect, new GUIContent("Compass Rotation Offset", "Kinect joint's "
																				+ "rotation in tracked object's local coordinate system. If using "
																				+ "Kinect for head tracking yaw drif correction, then zero vector is "
																				+ "usually the best choice if torso or head is the Compass Joint. "
																				+ "IT IS IMPORTANT THAT THIS PARAMETER IS CORRECT."
																				+ "Use 'Optional visualizers' to help finding the right values."));
			                EditorGUILayout.PropertyField(correctOnlyWhenFacingForward, new GUIContent("Only Forward Corrections", "Allows drift "
																				+ "correction to occur only when the player is detected as "
																				+ "standing towards Kinect (+-90 degrees). This is useful when "
																				+ "you know that the players will be mostly facing towards Kinect "
																				+ "and you want to improve drift correction by ignoring OpenNI's "
																				+ "tracking errors where the player is detected falsely "
																				+ "standing backwards (happens often)."));
					        EditorGUILayout.PropertyField(driftCorrectionRateKinect, new GUIContent("Correction Rate", "Positive values only. How fast "
																				+ "the drifting rotation is shifted towards the compass' "
																				+ "rotation. Kinect tracked skeleton is quite inaccurate as a " 
																				+ "compass, and the default 0.08 might be good. You might want "
																				+ "to adjust this to suit your liking."));
			                break;
			            case (int)RUISTracker.CompassSource.PSMove:
							compassPSMoveID.intValue = Mathf.Clamp(compassPSMoveID.intValue, 0, maxPSMoveControllers - 1);
							driftCorrectionRatePSMove.floatValue = Mathf.Clamp(driftCorrectionRatePSMove.floatValue, minDriftCorrectionRate, 
																													 maxDriftCorrectionRate );
							EditorGUI.BeginDisabledGroup(compassIsPositionTracker.boolValue);
		                	EditorGUILayout.PropertyField(compassPSMoveID, new GUIContent("PS Move ID", "Between 0 and 3"));
							EditorGUI.EndDisabledGroup();
			                EditorGUILayout.PropertyField(compassRotationOffsetPSMove, new GUIContent("Compass Rotation Offset", "Tracked PS Move "
																				+ "controller's rotation in tracked object's local coordinate "
																				+ "system. Set these euler angles according to the orientation "
																				+ "in which Move Compass is attached to your tracked object "
																				+ "(head etc.). IT IS IMPORTANT THAT THIS PARAMETER IS CORRECT. "
																				+ "Use 'Optional visualizers' to help finding the right values."));
					        EditorGUILayout.PropertyField(driftCorrectionRatePSMove, new GUIContent("Correction Rate", "Positive values only. How fast "
																				+ "the drifting rotation is shifted towards the compass' "
																				+ "rotation. Default of 0.1 is good."));
			                break;
			            case (int)RUISTracker.CompassSource.RazerHydra:
							driftCorrectionRateHydra.floatValue = Mathf.Clamp(driftCorrectionRateHydra.floatValue,  minDriftCorrectionRate, 
																													maxDriftCorrectionRate );

							EditorGUI.BeginDisabledGroup(compassIsPositionTracker.boolValue);
					        EditorGUILayout.PropertyField(isRazerBaseMobile, new GUIContent("Moving Base Station", "Enable this if the Razer Hydra "
																				+ "base station is attached to something that is "
																				+ "moving (e.g. Kinect tracked player's belt)"));
			
			                EditorGUILayout.PropertyField(compassRazerID, new GUIContent("Razer Hydra ID", "Either LEFT or RIGHT"));
							EditorGUI.EndDisabledGroup();
			                EditorGUILayout.PropertyField(compassRotationOffsetHydra, new GUIContent("Compass Rotation Offset", "Tracked Razer Hydra "
																				+ "controller's rotation in tracked object's local "
																				+ "coordinate system.  Set these euler angles according "
																				+ "to the orientation in which the Razer Hydra is "
																				+ "attached to your tracked object (head etc.). "
																				+ "IT IS IMPORTANT THAT THIS PARAMETER IS CORRECT."
																				+ "Use 'Optional visualizers' to help finding the right values."));
					        EditorGUILayout.PropertyField(driftCorrectionRateHydra, new GUIContent("Correction Rate", "Positive values only. How fast "
																				+ "the drifting rotation is shifted towards the compass' "
																				+ "rotation. Default of 0.1 is good."));
			                break;
			            case (int)RUISTracker.CompassSource.InputTransform:
							driftCorrectionRateTransform.floatValue = Mathf.Clamp(driftCorrectionRateTransform.floatValue,  minDriftCorrectionRate, 
																															maxDriftCorrectionRate );
						
							EditorGUI.BeginDisabledGroup(compassIsPositionTracker.boolValue);
			                EditorGUILayout.PropertyField(compassTransform, new GUIContent("Input Transform", "Drift correction via all other "
																				+ "trackers is supported through this transform. Drag "
																				+ "and drop here a transform whose rotation cannot drift."));
							EditorGUI.EndDisabledGroup();
					        EditorGUILayout.PropertyField(driftCorrectionRateTransform, new GUIContent("Correction Rate", "Positive values only. "
																				+ "How fast the drifting rotation is shifted towards the "
																				+ "compass' rotation."));
							break;
			        }
				
	       		EditorGUILayout.Space();
				EditorGUILayout.LabelField("Optional visualizers:");
		        EditorGUILayout.PropertyField(enableVisualizers, new GUIContent("Enable Visualizers", "Below visualizers are optional and meant to"
																	+ "illustrate the performance of the drift correction."));
				if(enableVisualizers.boolValue)
				{
					
		        	EditorGUI.indentLevel += 1;
			        EditorGUILayout.PropertyField(driftingDirectionVisualizer, new GUIContent("Drifter Rotation Visualizer", "Drag and drop a Game "
																		+ "Object here to visualize rotation from Rotation Tracker"));
			        EditorGUILayout.PropertyField(compassDirectionVisualizer, new GUIContent("Compass Yaw Visualizer", "Drag and drop a Game Object "
																		+ "here to visualize yaw rotation from Compass Tracker"));
			        EditorGUILayout.PropertyField(correctedDirectionVisualizer, new GUIContent("Corrected Rotation Visualizer", "Drag and drop a Game "
																		+ "Object here to visualize the final, corrected rotation"));
			        EditorGUILayout.PropertyField(driftVisualizerPosition, new GUIContent("Visualizer Position", "Drag and drop a Transform here "
																		+ "that defines the position where the above three "
																		+ "visualizers will appear"));
	        		EditorGUI.indentLevel -= 1;
				}
				
	        	EditorGUI.indentLevel -= 2;
			}
		
		}
		
		if(isRazerBaseMobile.boolValue)
		{
			if(headPositionInput.enumValueIndex == (int)RUISTracker.HeadPositionSource.RazerHydra)
				movingBaseAnnouncement = "Razer Hydra base station set as moving in Position Tracker";
			else if(headRotationInput.enumValueIndex == (int)RUISTracker.HeadRotationSource.RazerHydra && !riftFound)
				movingBaseAnnouncement = "Razer Hydra base station set as moving in Rotation Tracker";
			else if(	compass.enumValueIndex == (int)RUISTracker.CompassSource.RazerHydra && externalDriftCorrection.boolValue
					&&	(riftFound || headRotationInput.enumValueIndex == (int)RUISTracker.HeadRotationSource.InputTransform)	)
				movingBaseAnnouncement = "Razer Hydra base station set as moving in Yaw Drift Correction";
			else
			{
				movingBaseAnnouncement = "";
				isRazerBaseMobile.boolValue = false; // When all the ifs fail, we can set isRazerBaseMobile to false
			}
		}
		
		if(isRazerBaseMobile.boolValue)
		{
			
    		EditorGUILayout.PropertyField(mobileRazerBase, new GUIContent("Razer Base Tracker", "The tracker onto which the Razer Hydra "
																		+ "base station is attached to"));
			EditorGUI.indentLevel += 2;
			
			if(movingBaseAnnouncement.Length > 0)
			{
				EditorStyles.textField.wordWrap = true;
				EditorGUILayout.TextArea(movingBaseAnnouncement, GUILayout.Height(30));
			}
			switch(mobileRazerBase.enumValueIndex)
			{
				case (int) RUISTracker.RazerHydraBase.Kinect1:
					hydraBaseKinectPlayerID.intValue = Mathf.Clamp(hydraBaseKinectPlayerID.intValue, 0, maxKinectSkeletons - 1);
					if(hydraBasePositionCovarianceKinect.floatValue < minNoiseCovariance)
						hydraBasePositionCovarianceKinect.floatValue = minNoiseCovariance;
					if(hydraBaseRotationCovarianceKinect.floatValue < minNoiseCovariance)
						hydraBaseRotationCovarianceKinect.floatValue = minNoiseCovariance;
					EditorGUILayout.PropertyField(hydraBaseKinectPlayerID, new GUIContent("Kinect Player Id", "Between 0 and 3"));
					EditorGUILayout.PropertyField(hydraBaseJoint, new GUIContent("Joint", "Kinect joint onto which Razer Hydra base station "
																		+ "is attached to"));
			        EditorGUILayout.PropertyField(hydraBasePositionOffsetKinect, new GUIContent("Base Position Offset (meters)", "Razer Hydra "
            															+ "base station's position in the tracked joint's local coordinate "
																		+ "system. Set these values according to the base station's position "
																		+ "offset from the tracked joint's origin."));
			        EditorGUILayout.PropertyField(hydraBaseRotationOffsetKinect, new GUIContent("Base Rotation Offset", "Razer Hydra "
																		+ "base station's rotation in the tracked joint's local coordinate "
																		+ "system. Set these euler angles according to the orientation in which "
																		+ "Razer Hydra base station is attached to the tracked joint. "
																		+ "IT IS IMPORTANT THAT THIS PARAMETER IS CORRECT."));
					EditorGUILayout.PropertyField(inferBaseRotationFromRotationTrackerKinect, new GUIContent("Use Rotation Tracker", "If the "
																		+ "above Position Tracker or Compass Razer Hydra is attached to the "
																		+ "Rotation Tracker (e.g. Oculus Rift), then use them together to "
																		+ "calculate the base station's rotation. Recommended for "
																		+ "Kinect."));
					if(inferBaseRotationFromRotationTrackerKinect.boolValue)
					{
						EditorGUI.indentLevel += 1;
						EditorGUILayout.PropertyField(hydraAtRotationTrackerOffset, new GUIContent("Razer Hydra Rotation Offset", "Tracked "
																		+ "Razer Hydra controller's rotation in tracked object's local coordinate "
																		+ "system. Set these euler angles according to the orientation in which "
																		+ "the Razer Hydra is attached to the tracked object (head etc.). "
																		+ "IT IS IMPORTANT THAT THIS PARAMETER IS CORRECT."));
						EditorGUI.indentLevel -= 1;
					
					}
					EditorGUILayout.PropertyField(filterHydraBasePoseKinect, new GUIContent("Filter Tracking", "Enables simple "
																		+ "Kalman filtering for position and rotation tracking of the Razer "
																		+ "Hydra base station. Recommended for Kinect."));
					if(filterHydraBasePoseKinect.boolValue)
					{
						EditorGUILayout.PropertyField(hydraBasePositionCovarianceKinect, new GUIContent("Filter Position Strength", "Position " 
																		+ "noise covariance of Kalman filtering: a bigger value means "
																		+ "smoother results but a slower response to changes."));
						EditorGUILayout.PropertyField(hydraBaseRotationCovarianceKinect, new GUIContent("Filter Rotation Strength", "Rotation " 
																		+ "noise covariance of Kalman filtering: a bigger value means "
																		+ "smoother results but a slower response to changes."));
					}
				break;
			
				case (int) RUISTracker.RazerHydraBase.InputTransform:
					if(hydraBasePositionCovarianceTransform.floatValue < minNoiseCovariance)
						hydraBasePositionCovarianceTransform.floatValue = minNoiseCovariance;
					if(hydraBaseRotationCovarianceTransform.floatValue < minNoiseCovariance)
						hydraBaseRotationCovarianceTransform.floatValue = minNoiseCovariance;
					EditorGUILayout.PropertyField(hydraBaseInput, new GUIContent("Input Transform", "All other trackers are supported "
																		+ "through this transform. Drag and drop here a transform "
																		+ "whose position and rotation is controlled by a tracking device."));
				
					EditorGUILayout.PropertyField(inferBaseRotationFromRotationTrackerTransform, new GUIContent("Use Rotation Tracker", "If the "
																		+ "above Position Tracker or Compass Razer Hydra is attached to the "
																		+ "Rotation Tracker (e.g. Oculus Rift), then use them together to "
																		+ "calculate the base station's rotation."));
					if(inferBaseRotationFromRotationTrackerTransform.boolValue)
					{
						EditorGUI.indentLevel += 1;
						EditorGUILayout.PropertyField(hydraAtRotationTrackerOffset, new GUIContent("Razer Hydra Rotation Offset", "Tracked "
																		+ "Razer Hydra controller's rotation in tracked object's local coordinate "
																		+ "system. Set these euler angles according to the orientation in which "
																		+ "the Razer Hydra is attached to the tracked object (head etc.). "
																		+ "IT IS IMPORTANT THAT THIS PARAMETER IS CORRECT."));
						EditorGUI.indentLevel -= 1;
					
					}
					EditorGUILayout.PropertyField(filterHydraBasePoseTransform, new GUIContent("Filter Tracking", "Enables simple "
																		+ "Kalman filtering for position and rotation tracking of the Razer "
																		+ "Hydra base station."));
					if(filterHydraBasePoseTransform.boolValue)
					{
						EditorGUILayout.PropertyField(hydraBasePositionCovarianceTransform, new GUIContent("Filter Position Strength", "Position " 
																		+ "noise covariance of Kalman filtering: a bigger value means "
																		+ "smoother results but a slower response to changes."));
						EditorGUILayout.PropertyField(hydraBaseRotationCovarianceTransform, new GUIContent("Filter Rotation Strength", "Rotation " 
																		+ "noise covariance of Kalman filtering: a bigger value means "
																		+ "smoother results but a slower response to changes."));
					}
				break;
			}
			EditorGUI.indentLevel -= 2;
			
		}
		
		
        serializedObject.ApplyModifiedProperties();
    }
}
