/*****************************************************************************

Content    :   Inspector behaviour for RUISCoordinateCalibrationEditor script
Authors    :   Mikael Matveinen
Copyright  :   Copyright 2013 Tuukka Takala, Mikael Matveinen. All Rights reserved.
Licensing  :   RUIS is distributed under the LGPL Version 3 license.

******************************************************************************/

using UnityEngine;
using UnityEditor;
using System.Collections;
 
[CustomEditor(typeof(RUISCoordinateCalibration))]
[CanEditMultipleObjects]
public class RUISCoordinateCalibrationEditor : Editor
{
	SerializedProperty firstDevice;
	SerializedProperty secondDevice;
	SerializedProperty numberOfSamplesToTake;
	SerializedProperty samplesPerSecond;
	SerializedProperty xmlFilename;
	SerializedProperty sampleCube;
	SerializedProperty sampleSphere;
	
	public void OnEnable()
	{
		firstDevice = serializedObject.FindProperty("firstDevice");
		secondDevice  = serializedObject.FindProperty("secondDevice");
		numberOfSamplesToTake = serializedObject.FindProperty("numberOfSamplesToTake");
		samplesPerSecond = serializedObject.FindProperty("samplesPerSecond");
		xmlFilename = serializedObject.FindProperty("xmlFilename");
		sampleCube = serializedObject.FindProperty("sampleCube");
		sampleSphere = serializedObject.FindProperty("sampleSphere");
	}
	
	public override void OnInspectorGUI()
	{
		serializedObject.Update();
		EditorGUILayout.Space();
		EditorGUILayout.PropertyField(firstDevice, new GUIContent("First Calibration Device (Mannequin)", ""));
		EditorGUILayout.PropertyField(secondDevice, new GUIContent("Second Calibration Device (Wand)", ""));
		EditorGUILayout.PropertyField(numberOfSamplesToTake, new GUIContent("Number Of Samples To Take", ""));
		EditorGUILayout.PropertyField(samplesPerSecond, new GUIContent("Samples Per Second", ""));
		
		RUISEditorUtility.HorizontalRuler();
		EditorGUILayout.PropertyField(xmlFilename, new GUIContent("Calibration XML File Name", ""));
		RUISEditorUtility.HorizontalRuler();
		EditorGUILayout.Space();
		EditorGUILayout.PropertyField(sampleCube, new GUIContent("Cube prefab", "Prefab of a cube that is used to visualize device samples"));
		EditorGUILayout.PropertyField(sampleSphere, new GUIContent("Sphere prefab", "Prefab of a sphere that is used to visualize device samples"));
		serializedObject.ApplyModifiedProperties();
	}

}