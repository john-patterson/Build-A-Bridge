/*****************************************************************************

Content    :   Spawn balls at a certain location
Authors    :   Mikael Matveinen
Copyright  :   Copyright 2015 Tuukka Takala, Mikael Matveinen. All Rights reserved.
Licensing  :   RUIS is distributed under the LGPL Version 3 license.

******************************************************************************/

using UnityEngine;
using System.Collections;

public class BallSpawner : MonoBehaviour {
    public GameObject ball;
    private PSMoveWrapper psMoveWrapper;

	void Awake () {
        psMoveWrapper = FindObjectOfType(typeof(PSMoveWrapper)) as PSMoveWrapper;	
	}
	
	void Update () {
	    for(int i = 0; i < psMoveWrapper.moveCount; i++){
            if (psMoveWrapper.moveConnected[i] && psMoveWrapper.WasPressed(i, PSMoveWrapper.CIRCLE))
            {
                Instantiate(ball, transform.position, transform.rotation);
            }
        }
	}
}
