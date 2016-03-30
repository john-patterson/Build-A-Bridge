using UnityEngine;
using System.Collections;
using UnityEditor;
using System.Collections.Generic;

[CustomEditor(typeof(RUISSkeletonWand))]
[CanEditMultipleObjects]
public class RUISSkeletonWandEditor : Editor
{
	SerializedProperty playerId;
	SerializedProperty bodyTrackingDevice;
//	SerializedProperty gestureSelectionMethod;
	SerializedProperty wandStart;
	SerializedProperty wandEnd;
	SerializedProperty rotationNoiseCovariance;
	SerializedProperty visualizerThreshold;
	SerializedProperty visualizerWidth;
	SerializedProperty visualizerHeight;
	SerializedProperty wandColor;
//	SerializedProperty gestureRecognizer;
	SerializedProperty wandPositionVisualizer;
	SerializedObject gestureSelectionMethodLink; 
	SerializedProperty guiGestureSelectionMethodChoiceLink;
	SerializedProperty gestureScriptLink;
	SerializedProperty showVisualizer;
	SerializedProperty switchToAvailableKinect;
	
	RUISSkeletonWand skeletonWand;
	RUISGestureRecognizer[] gestureRecognizerScripts;
	
	void OnEnable()
	{
		playerId = serializedObject.FindProperty("playerId");
		bodyTrackingDevice = serializedObject.FindProperty("bodyTrackingDevice");
//		gestureSelectionMethod = serializedObject.FindProperty("gestureSelectionMethod");
		wandStart = serializedObject.FindProperty("wandStart");
		wandEnd = serializedObject.FindProperty("wandEnd");
		rotationNoiseCovariance = serializedObject.FindProperty("rotationNoiseCovariance");
		visualizerThreshold = serializedObject.FindProperty("visualizerThreshold");
		visualizerWidth = serializedObject.FindProperty("visualizerWidth");
		visualizerHeight = serializedObject.FindProperty("visualizerHeight");
		wandColor = serializedObject.FindProperty("wandColor");
//		gestureRecognizer = serializedObject.FindProperty("gestureRecognizer");
		wandPositionVisualizer = serializedObject.FindProperty("wandPositionVisualizer");
		showVisualizer = serializedObject.FindProperty("showVisualizer");
		switchToAvailableKinect = serializedObject.FindProperty("switchToAvailableKinect");
		
		skeletonWand = target as RUISSkeletonWand;
		
		if(skeletonWand) {
			gestureSelectionMethodLink = new SerializedObject(skeletonWand);
			guiGestureSelectionMethodChoiceLink = gestureSelectionMethodLink.FindProperty("gestureSelectionMethod");
			gestureScriptLink = gestureSelectionMethodLink.FindProperty("gestureSelectionScriptName");
		}
		
	}
	
	public override void OnInspectorGUI()
	{
		string[] _choices = { };
		
		if(skeletonWand) {
			gestureRecognizerScripts = skeletonWand.gameObject.GetComponents<RUISGestureRecognizer>();
			List<string> _dropdownElements = new List<string>();
			
			for(int i = 0; i < gestureRecognizerScripts.Length; i++) {
				_dropdownElements.Add(gestureRecognizerScripts[i].GetType().ToString().Replace("RUIS", ""));
			}
			_choices = _dropdownElements.ToArray(); 
		}
		
		serializedObject.Update();
		if(skeletonWand) gestureSelectionMethodLink.Update();
		if(skeletonWand) guiGestureSelectionMethodChoiceLink.intValue = EditorGUILayout.Popup("Gesture Recognizer", guiGestureSelectionMethodChoiceLink.intValue, _choices);
		if(skeletonWand) gestureScriptLink.stringValue = gestureRecognizerScripts[guiGestureSelectionMethodChoiceLink.intValue].ToString();
		
		EditorGUILayout.PropertyField(bodyTrackingDevice, new GUIContent("Body Tracking Device", "The source device for body tracking."));
		EditorGUILayout.PropertyField(playerId, new GUIContent("Skeleton ID", "The player ID number"));
		
		if (bodyTrackingDevice.enumValueIndex == 0 || bodyTrackingDevice.enumValueIndex == 1) 
			EditorGUILayout.PropertyField(switchToAvailableKinect, new GUIContent(  "Switch To Available Kinect", "Examine RUIS InputManager settings, and "
			                                                                      + "switch Body Tracking Device from Kinect 1 to Kinect 2 in run-time if "
			                                                                      + "the latter is enabled but the former is not, and vice versa. If both "
			                                                                      + "Kinects are disabled while this option is enabled, then this gameobject "
			                                                                      + "will be disabled upon Start()."));

		EditorGUILayout.PropertyField(wandStart, new GUIContent("Wand Start Point", "Body joint that together with Wand End Point define selection ray direction"));
		EditorGUILayout.PropertyField(wandEnd, new GUIContent("Wand End Point", "Body joint that defines the Skeleton Wand position"));
		
		EditorGUILayout.PropertyField(rotationNoiseCovariance, new GUIContent("Rotation Smoothness", "Sets the magnitude of rotation smoothing (basic Kalman filter). "
		                                                                      						+"Larger values make the rotation smoother, but makes it less "
		                                                                      						+"responsive. Default value is 500."));

		EditorGUILayout.PropertyField(showVisualizer, new GUIContent("Show Visualizer", "Show animation that illustrates gesture detection state / progress"));
		if(showVisualizer.boolValue) {
			EditorGUI.indentLevel += 2;
			EditorGUILayout.PropertyField(visualizerThreshold, new GUIContent(  "Visualizer Threshold", "Visualizer is displayed only when gesture detection "
			                                                                  + "progress is above this threshold."));
			EditorGUILayout.PropertyField(visualizerWidth, new GUIContent("Visualizer Width", "Width in pixels for the visualizer"));
			EditorGUILayout.PropertyField(visualizerHeight, new GUIContent("Visualizer Height", "Height in pixels for the visualizer"));
			EditorGUI.indentLevel -= 2;	
		}
		EditorGUILayout.PropertyField(wandColor, new GUIContent("Wand Color", "Color for the Skeleton Wand's selection ray"));
		EditorGUILayout.PropertyField(wandPositionVisualizer, new GUIContent(  "Wand Object", "The Wand Object (a gameobject) will be disabled when the skeleton for "
		                                                                     + "this Player ID is not found, and re-enabled when it is found again. Wand Object should "
		                                                                     + "be a child object of the gameobject where this " + typeof(RUISSkeletonWand) + " script "
		                                                                     + "is located, so that it correctly represents the Skeleton Wand position."));
		serializedObject.ApplyModifiedProperties();
		if(skeletonWand) gestureSelectionMethodLink.ApplyModifiedProperties();
	}
}
