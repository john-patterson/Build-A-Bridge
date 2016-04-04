/*****************************************************************************

Content    :   Custom editor script for RUISInputManager
Authors    :   Mikael Matveinen, Tuukka Takala
Copyright  :   Copyright 2013 Tuukka Takala, Mikael Matveinen. All Rights reserved.
Licensing  :   RUIS is distributed under the LGPL Version 3 license.

******************************************************************************/

using UnityEngine;
using UnityEditor;
using System.Collections;

[CustomEditor(typeof(RUISInputManager))]
[CanEditMultipleObjects]
public class RUISInputManagerEditor : Editor {
    RUISInputManager inputConfig;

    SerializedProperty xmlSchema;
    SerializedProperty filename;

    SerializedProperty loadFromTextFileInEditor;

    SerializedProperty psMoveEnabled;
    SerializedProperty connectToMoveOnStartup;
    SerializedProperty psMoveIp;
    SerializedProperty psMovePort;
    SerializedProperty inGameMoveCalibration;
    SerializedProperty amountOfPSMoveControllers;
	
	SerializedProperty delayedWandActivation;
	SerializedProperty delayTime;
	SerializedProperty moveWand0;
	SerializedProperty moveWand1;
	SerializedProperty moveWand2;
	SerializedProperty moveWand3;
	
    SerializedProperty kinectEnabled;
    SerializedProperty maxNumberOfKinectPlayers;
	SerializedProperty kinect1FloorDetectionOnSceneStart;

	SerializedProperty kinect2Enabled;
	SerializedProperty kinect2FloorDetectionOnSceneStart;

	SerializedProperty enableRazerHydra;

//    SerializedProperty riftMagnetometerMode;

    
    void OnEnable()
    {
        inputConfig = target as RUISInputManager;
		
        xmlSchema = serializedObject.FindProperty("xmlSchema");
        filename = serializedObject.FindProperty("filename");

        psMoveEnabled = serializedObject.FindProperty("enablePSMove");
        loadFromTextFileInEditor = serializedObject.FindProperty("loadFromTextFileInEditor");
        connectToMoveOnStartup = serializedObject.FindProperty("connectToPSMoveOnStartup");
        psMoveIp = serializedObject.FindProperty("PSMoveIP");
        psMovePort = serializedObject.FindProperty("PSMovePort");
        inGameMoveCalibration = serializedObject.FindProperty("enableMoveCalibrationDuringPlay");
        amountOfPSMoveControllers = serializedObject.FindProperty("amountOfPSMoveControllers");

		delayedWandActivation = serializedObject.FindProperty("delayedWandActivation");
		delayTime = serializedObject.FindProperty("delayTime");
		moveWand0 = serializedObject.FindProperty("moveWand0");
		moveWand1 = serializedObject.FindProperty("moveWand1");
		moveWand2 = serializedObject.FindProperty("moveWand2");
		moveWand3 = serializedObject.FindProperty("moveWand3");
		
        kinectEnabled = serializedObject.FindProperty("enableKinect");
        maxNumberOfKinectPlayers = serializedObject.FindProperty("maxNumberOfKinectPlayers");
		kinect1FloorDetectionOnSceneStart = serializedObject.FindProperty("kinectFloorDetection");

		kinect2Enabled = serializedObject.FindProperty("enableKinect2");
		kinect2FloorDetectionOnSceneStart = serializedObject.FindProperty("kinect2FloorDetection");

		enableRazerHydra = serializedObject.FindProperty("enableRazerHydra");

//        riftMagnetometerMode = serializedObject.FindProperty("riftMagnetometerMode");
        
	}

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.Space();
		
        EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Import from XML"))
            {
                if (Import())
                {
                    //success
                }
                else
                {
                    //failure
                }	
            }
            if (GUILayout.Button("Export to XML"))
            {
                if (Export())
                {
                    //success
                }
                else
                {
                    //failure
                }
            }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.PropertyField(filename, new GUIContent(  "Filename", "Name of the XML file where RUIS InputManager settings will be loaded/saved. If the file doesn't exist "
		                                                       + "it will be created. In Unity Editor the file will be located in the project root folder, and in a built project "
		                                                       + "the file it will be located in the same folder where the executable file is."));
        EditorGUILayout.PropertyField(xmlSchema, new GUIContent(  "XML Schema", "Location of the file that defines the XML format of the above file. You should not change this "
		                                                        + "value, which should always point to inputManager.xsd."));

        EditorGUILayout.PropertyField(loadFromTextFileInEditor, new GUIContent(  "Load from File in Editor", "Load RUIS InputManager settings (which devices are enabled and "
		                                                                       + "their configuration) from the above defined XML file while in Unity Editor. Otherwise use the "
		                                                                       + "values specified below. In built projects the values are always loaded from the external file."));


        RUISEditorUtility.HorizontalRuler();
        
        EditorGUILayout.PropertyField(psMoveEnabled, new GUIContent("PS Move Enabled"));

        if (psMoveEnabled.boolValue)
        {
            EditorGUI.indentLevel += 2;

            EditorGUILayout.PropertyField(psMoveIp, new GUIContent("PS Move IP", "PS Move IP address"));
			EditorGUILayout.PropertyField(psMovePort, new GUIContent("PS Move Port", "In most cases you should use the default value of 7899."));

            EditorGUILayout.PropertyField(connectToMoveOnStartup, new GUIContent("Auto-connect to Move.Me", "Connect to the Move.me server on startup."));

            EditorGUILayout.PropertyField(inGameMoveCalibration, new GUIContent("In-game Move calibration", "Enables the default Move Calibration by pressing the home button. Caution: Recalibration may change the coordinate system! Recommended setting is to keep this unchecked."));

            EditorGUILayout.PropertyField(amountOfPSMoveControllers, new GUIContent("Max amount of controllers connected", "Maximum amount of controllers connected. All RUISPSMoveControllers with a controller id outside of the range will get disabled to prevent accidents."));
            amountOfPSMoveControllers.intValue = Mathf.Clamp(amountOfPSMoveControllers.intValue, 0, 4);
			
			EditorGUILayout.PropertyField(delayedWandActivation, new GUIContent(  "Delayed Wand Activation", "Delayed PS Move Wand activation is useful when "
																				+ "you do not know beforehand how many PS Move controllers the user has calibrated. "
																				+ "If you mark a controller as delayed, then all GameObjects with a RUISPSMoveWand "
																				+ "script that has the same controller ID will be disabled at the beginning, and "
																				+ "re-activated after delay if the said controller is connected. Effectively this "
																				+ "disables those objects whose associated PS Move controller is not connected, "
																				+ "removing 'dead' input device representations."));
			if (delayedWandActivation.boolValue)
			{
				EditorGUI.indentLevel += 1;
				if(delayTime.floatValue < 5)
					delayTime.floatValue = 5;
				EditorGUILayout.PropertyField(delayTime, new GUIContent("Delay Duration", "Number of seconds from the start of the scene (minimum of 5)"));
				EditorGUILayout.PropertyField(moveWand0, new GUIContent("PS Move #0", "Delayed wand activation for PS Move controller 0"));
				EditorGUILayout.PropertyField(moveWand1, new GUIContent("PS Move #1", "Delayed wand activation for PS Move controller 1"));
				EditorGUILayout.PropertyField(moveWand2, new GUIContent("PS Move #2", "Delayed wand activation for PS Move controller 2"));
				EditorGUILayout.PropertyField(moveWand3, new GUIContent("PS Move #3", "Delayed wand activation for PS Move controller 3"));
            	EditorGUI.indentLevel -= 1;
			}
			
            EditorGUI.indentLevel -= 2;
        }

        EditorGUILayout.Space();
		EditorGUILayout.PropertyField(enableRazerHydra, new GUIContent("Razer Hydra Enabled"));
		
        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(kinectEnabled, new GUIContent("Kinect Enabled"));
        if (kinectEnabled.boolValue)
        {
            EditorGUI.indentLevel += 2;

            EditorGUILayout.PropertyField(maxNumberOfKinectPlayers, new GUIContent("Max Kinect Players", "Number of concurrently tracked skeletons"));
			EditorGUILayout.PropertyField(kinect1FloorDetectionOnSceneStart, new GUIContent(  "Floor Detection On Scene Start", "Kinect tries to detect "
			                                                                         + "floor and adjusts the coordinate system automatically when "
			                                                                         + "the scene is run. You should DISABLE this if you intend to "
			                                                                         + "use Kinect and another sensor (e.g. PS Move) in a calibrated "
			                                                                         + "coordinate system. Enabling this setting ignores whatever normal "
			                                                                         + "is stored in 'calibration.xml'."));
            EditorGUI.indentLevel -= 2;
        }
		EditorGUILayout.Space();

		EditorGUILayout.PropertyField(kinect2Enabled, new GUIContent("Kinect 2 Enabled"));
		
		if (kinect2Enabled.boolValue)
		{
			EditorGUI.indentLevel += 2;
			
			EditorGUILayout.PropertyField(kinect2FloorDetectionOnSceneStart, new GUIContent(  "Floor Detection On Scene Start", "Kinect 2 tries to detect "
			                                                                                + "floor and adjusts the coordinate system automatically when "
			                                                                                + "the scene is run. You should DISABLE this if you intend to "
			                                                                                + "use Kinect 2 and another sensor (e.g. PS Move) in a calibrated "
			                                                                                + "coordinate system. Enabling this setting ignores whatever normal "
			                                                                                + "is stored in 'calibration.xml'."));
			EditorGUI.indentLevel -= 2;
		}

        EditorGUILayout.Space();
		
//        EditorGUILayout.PropertyField(riftMagnetometerMode, new GUIContent("Rift Drift Correction", "Choose whether Oculus Rift's "
 //                                                                   + "magnetometer is calibrated at the beginning of the scene (for yaw "
  //                                                                  + "drift correction). It can always be (re)calibrated in-game with the "
   //                                                                 + "buttons defined in RUISOculusHUD component of RUISMenu."));


        serializedObject.ApplyModifiedProperties();
    }

    private bool Import()
    {
        string filename = EditorUtility.OpenFilePanel("Import Input Configuration", null, "xml");
        if (filename.Length != 0)
        {
            return inputConfig.Import(filename);
        }
        else
        {
            return false;
        }
    }

    private bool Export()
    {
        string filename = EditorUtility.SaveFilePanel("Export Input Configuration", null, "inputConfig", "xml");
        if (filename.Length != 0)
            return inputConfig.Export(filename);
        else
            return false;
    }
}
