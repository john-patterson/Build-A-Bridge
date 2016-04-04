/*****************************************************************************

Content    :   Displays the image given by the ps eye
Authors    :   Mikael Matveinen
Copyright  :   Copyright 2013 Tuukka Takala, Mikael Matveinen. All Rights reserved.
Licensing  :   RUIS is distributed under the LGPL Version 3 license.

******************************************************************************/

using UnityEngine;
using System.Collections;

public class RUISCalibrationMoveImage : MonoBehaviour {
    PSMoveWrapper psMoveWrapper;

    Texture2D texture;

    void Awake()
    {
        psMoveWrapper = FindObjectOfType(typeof(PSMoveWrapper)) as PSMoveWrapper;
        texture = new Texture2D(640, 480, TextureFormat.ARGB32, false);
    }
	
	// Update is called once per frame
	void Update () {
        Color32[] image = psMoveWrapper.GetCameraImage();
        if (image != null && image.Length == 640 * 480)
        {
            texture.SetPixels32(image);
            texture.Apply(false);
        }
	}

    void OnGUI()
    {
        GUI.DrawTexture(new Rect(0, Screen.height/2+1, Screen.width/2, Screen.height/2), texture);
    }
}
