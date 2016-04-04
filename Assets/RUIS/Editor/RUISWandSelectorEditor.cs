/*****************************************************************************

Content    :   Inspector behavior for a RUISDisplay
Authors    :   Mikael Matveinen
Copyright  :   Copyright 2013 Tuukka Takala, Mikael Matveinen. All Rights reserved.
Licensing  :   RUIS is distributed under the LGPL Version 3 license.

******************************************************************************/

using UnityEngine;
using System.Collections;
using UnityEditor;

[CustomEditor(typeof(RUISWandSelector))]
[CanEditMultipleObjects]
public class RUISWandSelectorEditor : Editor
{
    SerializedProperty selectionRayType;
    SerializedProperty selectionRayLength;
    SerializedProperty selectionRayStartDistance;

    SerializedProperty headTransform;

    SerializedProperty toggleSelection;
    SerializedProperty grabWhileButtonDown;

    SerializedProperty ignoredLayers;
    SerializedProperty selectedGameObjectsLayer;
    
    SerializedProperty positionGrabType;
    SerializedProperty rotationGrabType;

    void OnEnable()
    {
        selectionRayType = serializedObject.FindProperty("selectionRayType");
        selectionRayLength = serializedObject.FindProperty("selectionRayLength");
        selectionRayStartDistance = serializedObject.FindProperty("selectionRayStartDistance");

        headTransform = serializedObject.FindProperty("headTransform");
        
        toggleSelection = serializedObject.FindProperty("toggleSelection");
        grabWhileButtonDown = serializedObject.FindProperty("grabWhileButtonDown");

        ignoredLayers = serializedObject.FindProperty("ignoredLayers");
        selectedGameObjectsLayer = serializedObject.FindProperty("selectedGameObjectsLayer");
        
        positionGrabType = serializedObject.FindProperty("positionSelectionGrabType");
        rotationGrabType = serializedObject.FindProperty("rotationSelectionGrabType");
    }

    public void OnGUI()
    {
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(selectionRayType, new GUIContent("Selection Ray Type", "Type of selection ray.\nWandDirection: Selection ray will start at the wand (according to Ray Start Distance) and point in the direction of the wand.\nHeadToWand: Selection ray will be formed between the head (Head Transform) and the wand, enabling bow and arrow type aiming."));
        EditorGUILayout.PropertyField(selectionRayLength, new GUIContent("Ray Length", "The length of the selection ray."));
        EditorGUILayout.PropertyField(selectionRayStartDistance, new GUIContent("Ray Start Distance", "The distance at which the selection ray starts, useful for example to account for different visual wand models."));

        bool headToWandMode = selectionRayType.enumNames[selectionRayType.enumValueIndex] == "HeadToWand";
        GUI.enabled = headToWandMode;
        EditorGUILayout.PropertyField(headTransform, new GUIContent("Head Transform", "The head transform to use for HeadToWand selection"));
        if (!headToWandMode)
        {
            headTransform.objectReferenceValue = null;
        }


        GUI.enabled = true;

        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(toggleSelection, new GUIContent("Toggle Selection", "Toggle selection status when pressing button. Otherwise selection will last as long as the user holds the selection button down."));
        EditorGUILayout.PropertyField(grabWhileButtonDown, new GUIContent("Grab While Button Down", "The user can grab objects by sweeping with the selection ray while holding down the selection button. Otherwise the user will have to point at the object and only then press the button."));

        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(ignoredLayers, new GUIContent("Ignored Layers", "The layers to ignore when performing raycast checks for selection."));
        selectedGameObjectsLayer.intValue = EditorGUILayout.LayerField(new GUIContent("Selection Layer", "The layer selected GameObjects will be put on temporarily to avoid unwanted collisions."), selectedGameObjectsLayer.intValue);

        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(positionGrabType, new GUIContent("Position Grab", "The positional grab type:\nSnapToWand: Object will be positioned at the wand location.\nRelativeToWand: Object will start at its current position and be moved relative to the wand movements.\nAlongSelectionRay: Object will be moved along the selection ray at a certain distance, as if it was on the end of a long stick."));
        EditorGUILayout.PropertyField(rotationGrabType, new GUIContent("Rotation Grab", "The rotational grab type:\nSnapToWand: Object will be rotated exactly like the wand.\nRelativeToWand: Object will start at its current rotation and be rotated relative to the wand rotation.\nAlongSelectionRay: Object will face the selection ray."));

        serializedObject.ApplyModifiedProperties();
    }

}
