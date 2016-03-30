/*****************************************************************************

Content    :   Functionality to spawn bowling pins in the correct position
Authors    :   Mikael Matveinen
Copyright  :   Copyright 2013 Tuukka Takala, Mikael Matveinen. All Rights reserved.
Licensing  :   RUIS is distributed under the LGPL Version 3 license.

******************************************************************************/

using UnityEngine;
using System.Collections;

public class SpawnBowlingPins : MonoBehaviour {
    public GameObject bowlingPinsPrefab;
    public RUISPSMoveWand moveController;

    GameObject oldBowlingPins;
	
	void Update () {
        if (moveController.triangleButtonWasPressed)
        {
            if (oldBowlingPins)
            {
                Destroy(oldBowlingPins);
            }

            oldBowlingPins = Instantiate(bowlingPinsPrefab, transform.position, transform.rotation) as GameObject;
        }
	}
}
