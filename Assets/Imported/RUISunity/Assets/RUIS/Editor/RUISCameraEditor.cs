using UnityEngine;
using System.Collections;
using UnityEditor;

[CustomEditor(typeof(RUISCamera))]
[CanEditMultipleObjects]
public class RUISCameraEditor : Editor
{
    SerializedProperty near;
    SerializedProperty far;
    SerializedProperty horizontalFOV;
    SerializedProperty verticalFOV;
    SerializedProperty cullingMask;

    RUISCamera camera;

    void OnEnable()
    {
        near = serializedObject.FindProperty("near");
        far = serializedObject.FindProperty("far");

        horizontalFOV = serializedObject.FindProperty("horizontalFOV");
        verticalFOV = serializedObject.FindProperty("verticalFOV");
        
        cullingMask = serializedObject.FindProperty("cullingMask");

        camera = target as RUISCamera;
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(cullingMask, new GUIContent("Culling Mask", "Camera culling mask"));

        EditorGUILayout.LabelField("Clipping Planes");
        EditorGUI.indentLevel++;
        EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(near, new GUIContent("Near", "Near clipping plane distance"));
            EditorGUILayout.PropertyField(far, new GUIContent("Far", "Far clipping plane distance"));
        EditorGUILayout.EndHorizontal();
        EditorGUI.indentLevel--;
        EditorGUILayout.LabelField("Fields of View");
        EditorGUI.indentLevel++;
        EditorGUILayout.BeginHorizontal();
            horizontalFOV.floatValue = EditorGUILayout.Slider(new GUIContent("Horizontal", "Horizontal Field of View"), horizontalFOV.floatValue, 0, 179);
            verticalFOV.floatValue = EditorGUILayout.Slider(new GUIContent("Vertical", "Vertical Field of View"), verticalFOV.floatValue, 0, 179);
        EditorGUILayout.EndHorizontal();
        EditorGUI.indentLevel--;

        camera.GetComponent<Camera>().nearClipPlane = near.floatValue;
        camera.GetComponent<Camera>().farClipPlane = far.floatValue;
        camera.GetComponent<Camera>().fieldOfView = horizontalFOV.floatValue;

        serializedObject.ApplyModifiedProperties();
	}
}
