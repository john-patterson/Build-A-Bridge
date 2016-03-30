/*****************************************************************************

Content    :   Updates the ps move camera tilt angle text in the calibration screen
Authors    :   Mikael Matveinen
Copyright  :   Copyright 2013 Tuukka Takala, Mikael Matveinen. All Rights reserved.
Licensing  :   RUIS is distributed under the LGPL Version 3 license.

******************************************************************************/

using UnityEngine;
using System.Collections;

public class RUISCameraTiltTextUpdater : MonoBehaviour {
    PSMoveWrapper psMoveWrapper;

	void Awake () {
        psMoveWrapper = FindObjectOfType(typeof(PSMoveWrapper)) as PSMoveWrapper;
        psMoveWrapper.CameraFrameResume();
	}
	
	void Update () {
        if (psMoveWrapper.isConnected)
        {
            GetComponent<GUIText>().text = string.Format("PSMove camera pitch angle: {0}", Mathf.Rad2Deg * psMoveWrapper.state.gemStates[0].camera_pitch_angle);
        }
        else
        {
            GetComponent<GUIText>().text = string.Format("Unable to connect to Move.Me server at " + psMoveWrapper.ipAddress + ":" + psMoveWrapper.port);
        }
	}
}
