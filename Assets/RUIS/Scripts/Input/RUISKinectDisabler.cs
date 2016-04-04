/*****************************************************************************

Content    :   A class used to disable all kinect related functionality if the kinect isn't available
Authors    :   Mikael Matveinen
Copyright  :   Copyright 2013 Tuukka Takala, Mikael Matveinen. All Rights reserved.
Licensing  :   RUIS is distributed under the LGPL Version 3 license.

******************************************************************************/

using UnityEngine;
using System.Collections;

public class RUISKinectDisabler : MonoBehaviour {
    public void KinectNotAvailable()
    {
        gameObject.SetActive(false);

		NIPlayerManagerCOMSelection[] playerManagers = FindObjectsOfType(typeof(NIPlayerManagerCOMSelection)) as NIPlayerManagerCOMSelection[];
		foreach (NIPlayerManagerCOMSelection playerManager in playerManagers)
		{
			playerManager.enabled = false;
		}
    }
}
