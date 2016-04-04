/*****************************************************************************

Content    :   Modifies the normal NIDepthmapViewerUtility to fit the screen automatically
Authors    :   Mikael Matveinen
Copyright  :   Copyright 2013 Tuukka Takala, Mikael Matveinen. All Rights reserved.
Licensing  :   RUIS is distributed under the LGPL Version 3 license.

******************************************************************************/

using UnityEngine;
using System.Collections;

public class RUISCalibrationDepthMap : MonoBehaviour {
    NIDepthmapViewerUtility depthMapViewer;
	
	void Start () {
        depthMapViewer = GetComponent<NIDepthmapViewerUtility>();
        depthMapViewer.m_placeToDraw.height = Screen.height / 2;
        depthMapViewer.m_placeToDraw.width = Screen.width / 2;
	}
}
