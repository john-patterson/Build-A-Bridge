/*****************************************************************************

Content    :   Inspector behaviour for RUISCharacterLocomotion script
Authors    :   Tuukka Takala
Copyright  :   Copyright 2013 Tuukka Takala, Mikael Matveinen. All Rights reserved.
Licensing  :   RUIS is distributed under the LGPL Version 3 license.

******************************************************************************/

using UnityEngine;
using UnityEditor;
using System.Collections;

[CustomEditor(typeof(RUISCharacterLocomotion))]
[CanEditMultipleObjects]
public class RUISCharacterLocomotionEditor : Editor {
	
	SerializedProperty turnRightKey;
    SerializedProperty turnLeftKey;

    SerializedProperty rotationScaler;

    SerializedProperty speed;
	SerializedProperty runAdder;
    SerializedProperty maxVelocityChange;

    SerializedProperty usePSNavigationController;
    SerializedProperty PSNaviControllerID;
	SerializedProperty strafeInsteadTurning;
	
	SerializedProperty useRazerHydra;
	SerializedProperty razerHydraID;
	
    SerializedProperty jumpStrength;
	SerializedProperty jumpSpeedEffect;
	SerializedProperty aerialAcceleration;
	SerializedProperty aerialMobility;
	SerializedProperty aerialDrag;

    public void OnEnable()
    {
        turnRightKey = serializedObject.FindProperty("turnRightKey");
        turnLeftKey = serializedObject.FindProperty("turnLeftKey");
        rotationScaler = serializedObject.FindProperty("rotationScaler");
        speed = serializedObject.FindProperty("speed");
		runAdder = serializedObject.FindProperty("runAdder");
        maxVelocityChange = serializedObject.FindProperty("maxVelocityChange");
        usePSNavigationController = serializedObject.FindProperty("usePSNavigationController");
        PSNaviControllerID = serializedObject.FindProperty("PSNaviControllerID");
        strafeInsteadTurning = serializedObject.FindProperty("strafeInsteadTurning");
        jumpStrength = serializedObject.FindProperty("jumpStrength");
		useRazerHydra = serializedObject.FindProperty("useRazerHydra");
		razerHydraID = serializedObject.FindProperty("razerHydraID");
		jumpSpeedEffect = serializedObject.FindProperty("jumpSpeedEffect");
		aerialAcceleration = serializedObject.FindProperty("aerialAcceleration");
		aerialMobility = serializedObject.FindProperty("aerialMobility");
		aerialDrag = serializedObject.FindProperty("aerialDrag");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
		
		if(rotationScaler.floatValue < 0)
			rotationScaler.floatValue = 0;

		if(speed.floatValue < 0)
			speed.floatValue = 0;

		if(maxVelocityChange.floatValue < 0)
			maxVelocityChange.floatValue = 0;

		if(jumpSpeedEffect.floatValue < 0)
			jumpSpeedEffect.floatValue = 0;

		if(aerialAcceleration.floatValue < 0)
			aerialAcceleration.floatValue = 0;

		if(aerialDrag.floatValue < 0)
			aerialDrag.floatValue = 0;

		if(aerialMobility.floatValue < 0)
			aerialMobility.floatValue = 0;

        EditorGUILayout.PropertyField(turnRightKey, new GUIContent("Turn Right Key", "Which key is used to rotate the character to rigth"));
        EditorGUILayout.PropertyField(turnLeftKey, new GUIContent("Turn Left Key", "Which key is used to rotate the character to left"));
		EditorGUILayout.PropertyField(rotationScaler, new GUIContent(  "Rotation Speed", "How fast is the character rotates (degrees/s) when pressing 'Turn Keys' "
		                                                             + "on keyboard, PS Move, or Razer Hydra."));
        EditorGUILayout.PropertyField(speed, new GUIContent(  "Moving Speed", "How fast is the character's maximum walking speed (m/s). The character is "
		                                                    + "controlled via Unity Input's 'Vertical' and 'Horizontal' axes"));
		EditorGUILayout.PropertyField(runAdder, new GUIContent(  "Sprint Effect", "How much the 'Sprint' button affects the Moving Speed. Value 0 means no effect, "
		                                                       + "value 1 means double speed and so on."));
		EditorGUILayout.PropertyField(maxVelocityChange, new GUIContent(  "Max Acceleration", "The maximum rate of velocity change (m/s^2) when "
		                                                                + "starting or stopping locomotion while the character is grounded."));
		EditorGUILayout.PropertyField(jumpStrength, new GUIContent(  "Jump Strength", "Mass-invariant impulse force that is applied when jumping. Note that this "
		                                                           + "value has a non-linear relationship with the resulting jump height (which also depends on "
		                                                           + "the gravity)."));
		EditorGUILayout.PropertyField(jumpSpeedEffect, new GUIContent("Speed Effect on Jump", "How much speed affects the Jump Strength. Value 0 means no effect, "
		                                                              + "value 1 means double strength when moving at sprint speed and so on."));
		EditorGUILayout.PropertyField(aerialAcceleration, new GUIContent(  "Aerial Acceleration", "The maximum rate of velocity change (m/s^2) that "
		                                                                 + "the character can be controlled when he is not grounded. Set to 0 if "
		                                                                 + "you don't want your character to be controllable while falling (realistic). "
		                                                                 + "In platformer games this value should be positive, but usually not larger "
		                                                                 + "than 'Max Acceleration'."));
		EditorGUILayout.PropertyField(aerialDrag, new GUIContent(  "Aerial Drag", "The maximum rate of velocity change (m/s^2) that slows down the character "
		                                                         + "in the air. This variable is independent of Rigidbody component's 'Drag' variable. The below "
		                                                         + "'Aerial Mobility' has to be at least 2 if you want that a sprinting character who jumped can "
		                                                         + "come to full stup in the air due to drag."));
		EditorGUILayout.PropertyField(aerialMobility, new GUIContent(  "Aerial Mobility", "This velocity factor sets the maximum velocity that limits how much the "
		                                                             + "character's velocity can be changed in the air with player controls. The value is relative "
		                                                             + "to the 'Moving Speed': Value 0 means that the character can't be controlled in the air, "
		                                                             + "value 1 means that the character can change its velocity up to 'Moving Speed' in the air, "
		                                                             + "value 2 means double of that and so on. Value 1.5 is a good default."));
		
        EditorGUILayout.PropertyField(useRazerHydra, new GUIContent("Use Razer Hydra", "Enable locomotion controls with a Razer Hydra controller"));
		
		if(useRazerHydra.boolValue)
		{
	        EditorGUI.indentLevel += 2;
			
	        EditorGUILayout.PropertyField(razerHydraID, new GUIContent("Controller ID", "LEFT or RIGHT"));
			EditorGUILayout.PropertyField(strafeInsteadTurning, new GUIContent(  "Strafe, Don't Turn", "If enabled, then the analog stick's horizontal axis makes "
			                                                                   + "the character strafe (sidestep) instead of turning."));
	        
	        EditorGUI.indentLevel -= 2;
		}
		
		EditorGUILayout.PropertyField(usePSNavigationController, new GUIContent("Use PS Navi Controller", "Enable locomotion controls with a PS Navigation controller"));
		
		if(usePSNavigationController.boolValue)
		{
	        EditorGUI.indentLevel += 2;
			
			PSNaviControllerID.intValue = Mathf.Clamp(PSNaviControllerID.intValue, 1, 7);
	        EditorGUILayout.PropertyField(PSNaviControllerID, new GUIContent(  "Controller ID", "Between 1 and 7. Press and hold the PlayStation button to see "
			                                                                 + "Controller Settings and change the ID from PlayStation."));
			EditorGUILayout.PropertyField(strafeInsteadTurning, new GUIContent(  "Strafe, Don't Turn", "If enabled, then the analog stick's horizontal axis makes "
			                                                                   + "the character strafe (sidestep) instead of turning."));
	        
	        EditorGUI.indentLevel -= 2;
		}
		
        serializedObject.ApplyModifiedProperties();
    }
}
