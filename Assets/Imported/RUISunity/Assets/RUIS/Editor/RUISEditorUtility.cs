/*****************************************************************************

Content    :   A class to draw a horizontal ruler in Unity Inspector
Authors    :   Heikki Heiskanen
Copyright  :   Copyright 2013 Tuukka Takala, Heikki Heiskanen. All Rights reserved.
Licensing  :   RUIS is distributed under the LGPL Version 3 license.

******************************************************************************/
using UnityEngine;
using UnityEditor;

public static class RUISEditorUtility
{
	public static void HorizontalRuler()
	{
		GUI.color = new Color(1, 1, 1, 0.3f);
		EditorGUILayout.Space();
		GUILayout.Box("", "textArea", GUILayout.Height(8));
		EditorGUILayout.Space();
		GUI.color = Color.white;
	}
}

