/*****************************************************************************

Content    :   Inspector behaviour for RUISKinectAndMecanimCombiner script
Authors    :   Mikael Matveinen
Copyright  :   Copyright 2013 Tuukka Takala, Mikael Matveinen. All Rights reserved.
Licensing  :   RUIS is distributed under the LGPL Version 3 license.

******************************************************************************/

using UnityEngine;
using UnityEditor;
using System.Collections;

[CustomEditor(typeof(RUISKinectAndMecanimCombiner))]
[CanEditMultipleObjects]
public class RUISKinectAndMecanimCombinerEditor : Editor
{
    SerializedProperty rootBlendWeight;
    SerializedProperty torsoBlendWeight;
    SerializedProperty headBlendWeight;
    SerializedProperty rightArmBlendWeight;
    SerializedProperty leftArmBlendWeight;
    SerializedProperty rightLegBlendWeight;
    SerializedProperty leftLegBlendWeight;

    SerializedProperty forceArmStartPosition;
    SerializedProperty forceLegStartPosition;

    public void OnEnable()
    {
        rootBlendWeight = serializedObject.FindProperty("rootBlendWeight");
        torsoBlendWeight = serializedObject.FindProperty("torsoBlendWeight");
        headBlendWeight = serializedObject.FindProperty("headBlendWeight");
        rightArmBlendWeight = serializedObject.FindProperty("rightArmBlendWeight");
        leftArmBlendWeight = serializedObject.FindProperty("leftArmBlendWeight");
        rightLegBlendWeight = serializedObject.FindProperty("rightLegBlendWeight");
        leftLegBlendWeight = serializedObject.FindProperty("leftLegBlendWeight");
        forceArmStartPosition = serializedObject.FindProperty("forceArmStartPosition");
        forceLegStartPosition = serializedObject.FindProperty("forceLegStartPosition");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.LabelField("Blend Weights");
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("", GUILayout.MaxWidth(130), GUILayout.MinWidth(130));
        EditorGUILayout.LabelField("Kinect", GUILayout.ExpandWidth(true));
        EditorGUILayout.LabelField("Mecanim", GUILayout.MaxWidth(60));
        EditorGUILayout.EndHorizontal();
        EditorGUI.indentLevel++;
        EditorGUILayout.Slider(rootBlendWeight, 0, 1, new GUIContent("Root", "Blend weight for skeleton root"));
        EditorGUILayout.Slider(torsoBlendWeight, 0, 1, new GUIContent("Torso", "Blend weight for torso"));
        EditorGUILayout.Slider(headBlendWeight, 0, 1, new GUIContent("Head", "Blend weight for head"));
        EditorGUILayout.Slider(rightArmBlendWeight, 0, 1, new GUIContent("Right Arm", "Blend weight for right arm"));
        EditorGUILayout.Slider(leftArmBlendWeight, 0, 1, new GUIContent("Left Arm", "Blend weight for left arm"));
        EditorGUILayout.Slider(rightLegBlendWeight, 0, 1, new GUIContent("Right Leg", "Blend weight for right leg"));
        EditorGUILayout.Slider(leftLegBlendWeight, 0, 1, new GUIContent("Left Leg", "Blend weight for left leg"));
        EditorGUI.indentLevel--;

		EditorGUILayout.PropertyField(forceArmStartPosition, new GUIContent(  "Force Arm Position", "If enabled, the blended pose's shoulder positions "
		                                                                    + "follow exactly the Kinect tracked shoulder positions. This way the avatar's "
		                                                                    + "shoulders are as wide as Kinect sees them, even when playing arm animation."));
		EditorGUILayout.PropertyField(forceLegStartPosition, new GUIContent(  "Force Leg Position", "If enabled, the blended pose's hip positions "
		                                                                    + "follow exactly the Kinect tracked hip positions. This way the avatar's "
		                                                                    + "hips are as wide as Kinect sees them, even when playing leg animation."));

        serializedObject.ApplyModifiedProperties();
    }
}
