using UnityEngine;
using System.Collections;
using UnityEditor;

[CustomEditor(typeof(RUISCoordinateSystem))]
[CanEditMultipleObjects]
public class RUISCoordinateSystemEditor : Editor
{
	SerializedProperty coordinateXmlFile;
	SerializedProperty coordinateSchema;
	SerializedProperty loadFromXML;
	SerializedProperty rootDevice; 
	SerializedProperty applyToRootCoordinates;
	SerializedProperty switchToAvailableDevice;
	SerializedProperty setKinectOriginToFloor;
	SerializedProperty positionOffset;
	SerializedProperty yawOffset;

//	SerializedProperty kinect1DistanceFromFloor;
//	SerializedProperty kinect2DistanceFromFloor;
//	SerializedProperty kinect1FloorNormal;
//	SerializedProperty kinect2FloorNormal;

//	RUISInputManager inputManager;
	
	void OnEnable()
	{
		
		coordinateXmlFile = serializedObject.FindProperty("coordinateXmlFile");
		coordinateSchema = serializedObject.FindProperty("coordinateSchema");
		loadFromXML = serializedObject.FindProperty("loadFromXML");
		rootDevice = serializedObject.FindProperty("rootDevice"); 
		applyToRootCoordinates = serializedObject.FindProperty("applyToRootCoordinates");
		switchToAvailableDevice = serializedObject.FindProperty("switchToAvailableDevice");
		setKinectOriginToFloor = serializedObject.FindProperty("setKinectOriginToFloor");
		positionOffset = serializedObject.FindProperty("positionOffset");
		yawOffset = serializedObject.FindProperty("yawOffset");

		// TODO: Display these when playing in Editor
//		kinect1DistanceFromFloor = serializedObject.FindProperty("kinect1DistanceFromFloor");
//		kinect2DistanceFromFloor = serializedObject.FindProperty("kinect2DistanceFromFloor");
//		kinect1FloorNormal = serializedObject.FindProperty("kinect1FloorNormal");
//		kinect2FloorNormal = serializedObject.FindProperty("kinect2FloorNormal");
		
//		inputManager = ...
	}
	
	public override void OnInspectorGUI()
	{
		serializedObject.Update();
		
		EditorGUILayout.PropertyField(coordinateXmlFile, new GUIContent(  "Calibration XML File", "File where the pairwise sensor calibrations and floor "
		                                                                + "data for Kinect are stored."));
		if((coordinateSchema.objectReferenceValue as TextAsset))
			EditorGUILayout.LabelField("Calibration XML Schema: " + (coordinateSchema.objectReferenceValue as TextAsset).name);
		EditorGUILayout.PropertyField(loadFromXML, new GUIContent(  "Load from XML", "If this is enabled then pairwise calibrations for sensors and floor "
		                                                          + "data for Kinect will be loaded from the above defined XML file during Awake(). "
		                                                          + "If the sensors were correctly calibrated with RUIS, then they will appear to "
		                                                          + "use the same coordinate system. E.g., a Kinect tracked hand holding a PS Move "
		                                                          + "will result into a corresponding virtual hand and virtual PS Move (if RUISSkeletonController "
		                                                          + "and RUISPSMoveWand are used), even when PS Eye and Kinect sensors are apart from each other."));
		EditorGUILayout.PropertyField(setKinectOriginToFloor, new GUIContent(  "Set Kinect Origin To Floor", "Offset Y location of Kinect 1 and/or Kinect 2 "
		                                                                     + "so that real world floor corresponds to Y=0 in Kinect coordinates (if "
		                                                                     + "Kinect floor detection has been successful). This is useful if you want "
		                                                                     + "tracked avatars to always have their feet on the ground level, regardless "
		                                                                     + "of how high your Kinect sensor is placed. The Y offset can be loaded from "
		                                                                     + "Calibration XML File or calculated upon scene start if Kinect 1/2 'Floor Detection' "
		                                                                     + "option is enabled from RUISInputManager. NOTE: If 'Use Master Coordinate System' "
		                                                                     + "is enabled, then 'Master Coordinate System Sensor' must be Kinect 1 or 2 for this "
		                                                                     + "setting to have any effect."));
		EditorGUILayout.PropertyField(rootDevice, new GUIContent(  "Master Coordinate System Sensor", "The sensor that defines the Master Coordinate System "
		                                                         + "(sensor location is the origin and its orientation aligns the coordinate system axes). All "
		                                                         + "other sensors that are pairwise calibrated with the master sensor will have their RUIS "
		                                                         + "objects tracked in the master coordinate system."));
		EditorGUI.indentLevel++;

		EditorGUILayout.PropertyField(switchToAvailableDevice, new GUIContent(  "Switch To Available Sensor", "If the above defined 'Master Coordinate System Sensor' "
			                                                         		  + "is not available upon start, switch to a sensor that is. If there are multiple "
		                                                                      + "available sensors, then the following preference order will be used: Kinect 2, Kinect 1, "
		                                                                      + "Oculus Rift DK2, PS Move."));
		EditorGUILayout.PropertyField(positionOffset, new GUIContent(  "Location Offset", "This value offsets the Master Coordinate System (and other sensor "
		                                                             + "coordinate systems as well, if 'Use Master Coordinate System' is enabled)."));
		EditorGUILayout.PropertyField(yawOffset, new GUIContent(  "Y Rotation Offset", "This value rotates the Master Coordinate System around Y axis (and "
		                                                        + "other sensor coordinate systems as well, if 'Use Master Coordinate System' is enabled)."));
		EditorGUI.indentLevel--;
		EditorGUILayout.PropertyField(applyToRootCoordinates, new GUIContent(  "Use Master Coordinate System", "Applies pairwise calibration transformations from "
		                                                                     + "Calibration XML File so that sensors that are calibrated pairwise with the Master "
		                                                                     + "Coordinate System Sensor will have their RUIS tracked objects appear in the "
		                                                                     + "same coordinate system. If this option is disabled, then each sensor will use "
		                                                                     + "their own sensor-centric coordinate system for tracked targets."));

		serializedObject.ApplyModifiedProperties();
	}
}