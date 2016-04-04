/*****************************************************************************

Content    :   Inspector behavior for RUISDisplayManager
Authors    :   Mikael Matveinen
Copyright  :   Copyright 2013 Tuukka Takala, Mikael Matveinen. All Rights reserved.
Licensing  :   RUIS is distributed under the LGPL Version 3 license.

******************************************************************************/

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

[CustomEditor(typeof(RUISDisplayManager))]
public class RUISDisplayManagerEditor : Editor {
    SerializedProperty displays;
    private GameObject displayPrefab;

//    SerializedProperty allowResolutionDialog;
	SerializedProperty ruisMenuPrefab;
	SerializedProperty guiX; 
	SerializedProperty guiY; 
	SerializedProperty guiZ;
	SerializedProperty guiScaleX;
	SerializedProperty guiScaleY;
	SerializedProperty hideMouseOnPlay;
	
    GUIStyle displayBoxStyle;

    private Texture2D monoDisplayTexture;
    private Texture2D stereoDisplayTexture;
	
	List<string> guiDisplayChoices;
	
	SerializedObject displayManagerLink; 
	SerializedProperty guiDisplayChoiceLink;
	
	
	RUISDisplayManager displayManager; 
	
	SerializedProperty menuCursorPrefab;
	SerializedProperty menuLayer;
	
    void OnEnable()
    {
		displayManager = target as RUISDisplayManager;
		
        displays = serializedObject.FindProperty("displays");
		ruisMenuPrefab = serializedObject.FindProperty("ruisMenuPrefab");
		
		guiX = serializedObject.FindProperty("guiX");
		guiY = serializedObject.FindProperty("guiY");
		guiZ = serializedObject.FindProperty("guiZ");
		guiScaleX = serializedObject.FindProperty("guiScaleX");
		guiScaleY = serializedObject.FindProperty("guiScaleY");
		hideMouseOnPlay = serializedObject.FindProperty("hideMouseOnPlay");
		
		displayManagerLink = new SerializedObject(displayManager);
		guiDisplayChoiceLink = displayManagerLink.FindProperty("guiDisplayChoice");
		
//        allowResolutionDialog = serializedObject.FindProperty("allowResolutionDialog");
        displayPrefab = Resources.Load("RUIS/Prefabs/Main RUIS/RUISDisplay") as GameObject;

        displayBoxStyle = new GUIStyle();
        displayBoxStyle.normal.textColor = Color.white;
        displayBoxStyle.alignment = TextAnchor.MiddleCenter;
        displayBoxStyle.border = new RectOffset(2, 2, 2, 2);
        displayBoxStyle.margin = new RectOffset(1, 0, 0, 0);
        displayBoxStyle.wordWrap = true;

        monoDisplayTexture = Resources.Load("RUIS/Editor/Textures/monodisplay") as Texture2D;
        stereoDisplayTexture = Resources.Load("RUIS/Editor/Textures/stereodisplay") as Texture2D;
		
		menuCursorPrefab = serializedObject.FindProperty("menuCursorPrefab");
		menuLayer = serializedObject.FindProperty("menuLayer");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
		displayManagerLink.Update();
		
        EditorGUILayout.PropertyField(displays, true);

        EditorGUILayout.Space();

        //if there is only one display we want to give the user the opportunity to set fullscreen
        if (displays.arraySize == 1)
        {
//			allowResolutionDialog.boolValue = EditorGUILayout.Toggle(new GUIContent(  "Allow Resolution Dialog", "Enables the Resolution Dialog when your "
//			                                                                        + "standalone build starts if you have only one RUISDisplay. NOTE: In "
//			                                                                        + "all other cases the 'Display Resolution Dialog' setting in Unity's "
//			                                                                        + "Player Settings will be overriden with false. In those cases RUIS "
//			                                                                        + "will attempt to force the standalone game window to have the total "
//			                                                                        + "resolution that is the sum of your RUISDisplays arranged side-by-side."), 
//			                                                         allowResolutionDialog.boolValue);
//            if (allowResolutionDialog.boolValue)
//            {
//                PlayerSettings.displayResolutionDialog = ResolutionDialogSetting.Enabled;
//            }
//            else
//            {
//                PlayerSettings.displayResolutionDialog = ResolutionDialogSetting.Disabled;
//            }
        }
        else
		{
			EditorStyles.textField.wordWrap = true;
			EditorGUILayout.TextArea(  "Below configuration has multiple RUISDisplays. 'Display Resolution Dialog' setting is automatically disabled "
			                         + "in Unity's Player Settings.", GUILayout.Height(60));

            PlayerSettings.displayResolutionDialog = ResolutionDialogSetting.Disabled;
//            allowResolutionDialog.boolValue = false;
        }

        if (GUILayout.Button("Add Display"))
        {
            displays.arraySize++;
            SerializedProperty newDisplayProperty = displays.GetArrayElementAtIndex(displays.arraySize - 1);
            GameObject newDisplay = Instantiate(displayPrefab) as GameObject;
            newDisplay.name = "NewDisplay";
            newDisplay.GetComponent<RUISDisplay>().xmlFilename = newDisplay.name + displays.arraySize + ".xml";
            newDisplay.transform.parent = (target as RUISDisplayManager).transform;
            newDisplayProperty.objectReferenceValue = newDisplay;
        }

        EditorGUILayout.Space();

        int removeIndex = -1;
        for(int i = 0; i < displays.arraySize; i++){
            //DisplayEditor(displays.GetArrayElementAtIndex(i)); 
            EditorGUILayout.BeginHorizontal();
                //EditorGUILayout.PropertyField(displays.GetArrayElementAtIndex(i));

            RUISDisplay display = displays.GetArrayElementAtIndex(i).objectReferenceValue as RUISDisplay;
            if (display == null)
            {
                continue;
            }
            EditorGUILayout.LabelField(string.Format("{0} ({1}x{2})", display.name, display.resolutionX, display.resolutionY));

                if (displays.arraySize > 1)
                {
                    if (GUILayout.Button("Move Up", GUILayout.ExpandWidth(false)))
                    {
                        MoveUp(i);
                    }

                    if (GUILayout.Button("Move Down", GUILayout.ExpandWidth(false)))
                    {
                        MoveDown(i);
                    }
                }

                if (GUILayout.Button("Delete", GUILayout.ExpandWidth(false)))
                {
                    if (EditorUtility.DisplayDialog("Confirm deletion", "Are you sure you want to delete this display?", "Delete", "Cancel"))
                    {
                        removeIndex = i;
                    }
                }

            EditorGUILayout.EndHorizontal();
        }

        if (removeIndex != -1)
        {
            for (int i = removeIndex + 1; i < displays.arraySize; i++)
            {
                MoveUp(i);
            }
            displays.DeleteArrayElementAtIndex(displays.arraySize - 1);
            displays.arraySize--;
        }

        RUISDisplayManager displayManager = target as RUISDisplayManager;
        displayManager.CalculateTotalResolution();
        PlayerSettings.defaultScreenWidth = displayManager.totalRawResolutionX;
        PlayerSettings.defaultScreenHeight = displayManager.totalRawResolutionY;
        
        //stuff for drawing the display boxes
        int optimalWidth = Screen.width - displayManager.displays.Count;
        int optimalHeight = (int)((float)displayManager.totalResolutionY / displayManager.totalResolutionX * optimalWidth);

        EditorGUILayout.Space();
        EditorGUILayout.BeginHorizontal(GUILayout.Width(Screen.width), GUILayout.Height(optimalHeight));

        foreach(RUISDisplay display in displayManager.displays)
        {
            if (display.isStereo)
            {
                displayBoxStyle.normal.background = stereoDisplayTexture;
            }
            else
            {
                displayBoxStyle.normal.background = monoDisplayTexture;
            }
            GUILayout.Box(display.name, displayBoxStyle, GUILayout.Width(((float)display.resolutionX / displayManager.totalResolutionX) * optimalWidth), GUILayout.Height(optimalHeight * (float)display.resolutionY/displayManager.totalResolutionY));
        }

        EditorGUILayout.EndHorizontal();

		EditorGUILayout.BeginHorizontal();
		EditorGUILayout.LabelField("Display With RUIS Menu:");
		guiDisplayChoices = new List<string>();
		for(int i = 0; i < displayManager.displays.Count; ++i)
		{
			guiDisplayChoices.Add(i + " " + displayManager.displays[i].name);
		}
		//MonoBehaviour.print(guiDisplayChoiceLink + ":" + guiDisplayChoices.Count);
		if(guiDisplayChoiceLink.intValue >= guiDisplayChoices.Count) 
			guiDisplayChoiceLink.intValue = 0; 
		guiDisplayChoiceLink.intValue = EditorGUILayout.Popup(guiDisplayChoiceLink.intValue, guiDisplayChoices.ToArray());
		EditorGUILayout.EndHorizontal();
		
		EditorGUILayout.PropertyField(ruisMenuPrefab);

		
		
		EditorGUILayout.LabelField("RUIS Menu Local Coordinates:");
		EditorGUILayout.PropertyField(guiX,  new GUIContent("X", "X-offset from camera"));
		EditorGUILayout.PropertyField(guiY,  new GUIContent("Y", "Y-offset from camera"));
		EditorGUILayout.PropertyField(guiZ,  new GUIContent("Z", "Z-offset from camera"));
		
		EditorGUILayout.Space();
		
		EditorGUILayout.LabelField("RUIS Menu Scale:");
		EditorGUILayout.PropertyField(guiScaleX,  new GUIContent("X-scale", "Scales menu horizontally"));
		EditorGUILayout.PropertyField(guiScaleY,  new GUIContent("Y-scale", "Scales menu vertically"));
		EditorGUILayout.Space();
		EditorGUILayout.PropertyField(hideMouseOnPlay,  new GUIContent("Hide Mouse On Play", "Operating system mouse cursor will be hidden"));
		EditorGUILayout.Space();
		EditorGUILayout.PropertyField(menuCursorPrefab,  new GUIContent(  "Menu Cursor Prefab", "The prefab that will be instantiated when RUIS Menu is "
																		+ "invoked (ESC key)"));
		menuLayer.intValue = EditorGUILayout.LayerField(new GUIContent("Menu Layer", "RUIS Menu mouse cursor ray is cast against objects on this layer only"), menuLayer.intValue);
		

        serializedObject.ApplyModifiedProperties();
		displayManagerLink.ApplyModifiedProperties();
		
	
        //remove all child displays that aren't in the list anymore
        RUISDisplay displayToDestroy = null;
        foreach (RUISDisplay display in displayManager.GetComponentsInChildren<RUISDisplay>())
        {
            if (!displayManager.displays.Contains(display))
            {
                displayToDestroy = display;
            }
        }
        if (displayToDestroy)
        {
            DestroyImmediate(displayToDestroy.gameObject);
        }
        
		
		
    }

    private void MoveUp(int displayIndex)
    {
        if (displayIndex <= 0) return;

        displays.MoveArrayElement(displayIndex, displayIndex - 1);
    }

    private void MoveDown(int displayIndex)
    {
        if (displayIndex >= displays.arraySize - 1) return;

        displays.MoveArrayElement(displayIndex, displayIndex + 1);
    }
}
