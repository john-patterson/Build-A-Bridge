/*****************************************************************************

Content    :   Inspector behavior for a RUISDisplay
Authors    :   Mikael Matveinen
Copyright  :   Copyright 2013 Tuukka Takala, Mikael Matveinen. All Rights reserved.
Licensing  :   RUIS is distributed under the LGPL Version 3 license.

******************************************************************************/

using UnityEngine;
using System.Collections;
using UnityEditor;

[CustomEditor(typeof(RUISDisplay))]
[CanEditMultipleObjects]
public class RUISDisplayEditor : Editor {
    SerializedProperty xmlFilename;
    SerializedProperty displaySchema;
    SerializedProperty loadFromFileInEditor;

    SerializedProperty resolutionX;
    SerializedProperty resolutionY;
    SerializedProperty displayWidth;
    SerializedProperty displayHeight;

    SerializedProperty isStereo;
	SerializedProperty enableOculusRift;
	SerializedProperty oculusLowPersistence;
	SerializedProperty oculusMirrorMode;
    SerializedProperty isObliqueFrustum;
    SerializedProperty isKeystoneCorrected;
    SerializedProperty camera;
    SerializedProperty eyeSeparation;
    SerializedProperty stereoType;
    SerializedProperty useDoubleTheSpace;
    GUIStyle displayBoxStyle;

    SerializedProperty headTracker;
    SerializedProperty displayCenterPosition;
    SerializedProperty displayNormal;
    SerializedProperty displayUp;

    private Texture2D monoDisplayTexture;
    private Texture2D stereoDisplayTexture;

	private bool previousOculusLowPersistenceValue;

    RUISDisplayManager displayManager;

    void OnEnable()
    {
        xmlFilename = serializedObject.FindProperty("xmlFilename");
        displaySchema = serializedObject.FindProperty("displaySchema");
        loadFromFileInEditor = serializedObject.FindProperty("loadFromFileInEditor");

        resolutionX = serializedObject.FindProperty("resolutionX");
        resolutionY = serializedObject.FindProperty("resolutionY");
        displayWidth = serializedObject.FindProperty("width");
        displayHeight = serializedObject.FindProperty("height");

        isStereo = serializedObject.FindProperty("isStereo");
        enableOculusRift = serializedObject.FindProperty("enableOculusRift");
		oculusLowPersistence = serializedObject.FindProperty("oculusLowPersistence");
		oculusMirrorMode = serializedObject.FindProperty("oculusMirrorMode");
        isObliqueFrustum = serializedObject.FindProperty("isObliqueFrustum");
        isKeystoneCorrected = serializedObject.FindProperty("isKeystoneCorrected");
        camera = serializedObject.FindProperty("_linkedCamera");
        eyeSeparation = serializedObject.FindProperty("eyeSeparation");
        stereoType = serializedObject.FindProperty("stereoType");
        useDoubleTheSpace = serializedObject.FindProperty("useDoubleTheSpace");

        headTracker = serializedObject.FindProperty("headTracker");
        displayCenterPosition = serializedObject.FindProperty("displayCenterPosition");
        displayNormal = serializedObject.FindProperty("displayNormalInternal");
        displayUp = serializedObject.FindProperty("displayUpInternal");

        displayBoxStyle = new GUIStyle();
        displayBoxStyle.normal.textColor = Color.white;
        displayBoxStyle.alignment = TextAnchor.MiddleCenter;
        displayBoxStyle.border = new RectOffset(2, 2, 2, 2);
        displayBoxStyle.margin = new RectOffset(10, 10, 2, 2);

        monoDisplayTexture = Resources.Load("RUIS/Editor/Textures/monodisplay") as Texture2D;
        stereoDisplayTexture = Resources.Load("RUIS/Editor/Textures/stereodisplay") as Texture2D;

        displayManager = FindObjectOfType(typeof(RUISDisplayManager)) as RUISDisplayManager;

		previousOculusLowPersistenceValue = oculusLowPersistence.boolValue;
    }

    public void OnGUI()
    {
    }

    public override void OnInspectorGUI()
    {


        serializedObject.Update();

        EditorGUILayout.PropertyField(displaySchema, new GUIContent("XML Schema", "Do not modify this unless you know what you're doing"));
        EditorGUILayout.PropertyField(xmlFilename, new GUIContent("XML filename", "The XML file with the display specifications"));
        EditorGUILayout.PropertyField(loadFromFileInEditor, new GUIContent("Load from File in Editor", "Load the information from the xml file while in editor mode."));

        EditorGUILayout.PropertyField(resolutionX, new GUIContent("Resolution X", "The pixel width of the display"));
        EditorGUILayout.PropertyField(resolutionY, new GUIContent("Resolution Y", "The pixel height of the display"));

        EditorGUILayout.PropertyField(isStereo, new GUIContent("Stereo Display", "Is this display stereo?"));
        if (isStereo.boolValue)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(eyeSeparation, new GUIContent("Eye Separation", "Eye separation for the stereo image"));
            EditorGUILayout.PropertyField(stereoType, new GUIContent("Stereo Type", "The type of stereo to use"));
            EditorGUILayout.PropertyField(useDoubleTheSpace, new GUIContent("Double the Space used", "Calculate the total resolution of the display based on stereo type. \nSideBySide: Double horizontal resolution \nTopAndBottom: Double vertical resolution."));
            EditorGUI.indentLevel--;
        }
        
        EditorGUILayout.PropertyField(camera, new GUIContent("Attached Camera", "The RUISCamera that renders to this display"));

        

		EditorGUILayout.PropertyField(enableOculusRift, new GUIContent("Enable Oculus Rift", "Is this display an Oculus Rift?"));

		// TODO: Depends on OVR version
		EditorGUI.indentLevel++;
		GUI.enabled = enableOculusRift.boolValue;
		EditorGUILayout.PropertyField(oculusLowPersistence, new GUIContent(  "Low Persistence", "Low persistence reduces pixel blur. Try disabling this option if "
		                                                                   + "the Oculus Rift view suffers from 'judder' when rotating your head. NOTE: Disabling "
		                                                                   + "this option might cause issues with Oculus runtime 0.4.4 if you're using DX11!"));
		EditorGUILayout.PropertyField(oculusMirrorMode, new GUIContent(  "Mirror Mode", "Draw the Oculus viewports also to the main display when Direct Display Mode "
		                                                               + "(Direct HMD Access) is enabled from the Oculus Configuration Utility. This setting has no "
		                                                               + "effect when your application is playing inside the Unity Editor."));
		if(enableOculusRift.boolValue && EditorApplication.isPlaying)
		{
			if(previousOculusLowPersistenceValue != oculusLowPersistence.boolValue && displayManager)
			{
				// Low Persistence value changed, enforce it if application is running in Editor
				displayManager.setOculusLowPersistence(oculusLowPersistence.boolValue);
			}
		}

		EditorGUI.indentLevel--;

		GUI.enabled = !enableOculusRift.boolValue;
		
		RUISEditorUtility.HorizontalRuler();
		EditorGUILayout.PropertyField(isObliqueFrustum, new GUIContent("Head Tracked CAVE Display",   "Should the projection matrix be skewed to use this display "
		                                                               								+ "as a head tracked CAVE viewport"));

		GUI.enabled = true;

        if (!enableOculusRift.boolValue)
        {
            //disabled for now EditorGUILayout.PropertyField(isKeystoneCorrected, new GUIContent("Keystone Correction", "Should this display be keystone corrected?"));
            if (isObliqueFrustum.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(headTracker, new GUIContent("CAVE Head Tracker", "The head tracker object to use for perspective "
                                                                      + "distortion with CAVE-like displays. This is used only if the associated "
                                                                      + "RUISDisplay has 'Head Tracked CAVE Display' enabled."));
                EditorGUILayout.PropertyField(displayWidth, new GUIContent("Display Width", "The real-world width of the display"));
                EditorGUILayout.PropertyField(displayHeight, new GUIContent("Display Height", "The real-world height of the display"));
                EditorGUILayout.PropertyField(displayCenterPosition, new GUIContent("Display Center Position", "The location of the screen center in Unity coordinates"));
                EditorGUILayout.PropertyField(displayNormal, new GUIContent("Display Normal Vector", "The normal vector of the display (will be normalized)"));
                EditorGUILayout.PropertyField(displayUp, new GUIContent("Display Up Vector", "The up vector of the display (will be normalized)"));
                EditorGUI.indentLevel--;
            }
        }
        else
        {
            isObliqueFrustum.boolValue = false;
            isKeystoneCorrected.boolValue = false;
        }

		previousOculusLowPersistenceValue = oculusLowPersistence.boolValue;

        serializedObject.ApplyModifiedProperties();


        int optimalWidth = Screen.width - 4;
        int optimalHeight = (int)((float)resolutionY.intValue / resolutionX.intValue * optimalWidth);

        EditorGUILayout.Space();
        EditorGUILayout.BeginHorizontal(GUILayout.Height(optimalHeight));

        
        if (isStereo.boolValue)
        {
            displayBoxStyle.normal.background = stereoDisplayTexture;
        }
        else
        {
            displayBoxStyle.normal.background = monoDisplayTexture;
        }

        RUISDisplay display = target as RUISDisplay;
        int requiredX = display.rawResolutionX;
        int requiredY = display.rawResolutionY;
        string boxText = string.Format("{0}\nTotal required resolution {1}x{2}", target.name, requiredX, requiredY);

        GUILayout.Box(boxText, displayBoxStyle, GUILayout.Width(optimalWidth), GUILayout.Height(optimalHeight));

        EditorGUILayout.EndHorizontal();

        displayManager.CalculateTotalResolution();
        PlayerSettings.defaultScreenWidth = displayManager.totalRawResolutionX;
        PlayerSettings.defaultScreenHeight = displayManager.totalRawResolutionY;
    }

}
