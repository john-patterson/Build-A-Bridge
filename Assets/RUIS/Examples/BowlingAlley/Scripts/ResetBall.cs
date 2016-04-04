/*****************************************************************************

Content    :   Functionality to reset the ball position
Authors    :   Mikael Matveinen
Copyright  :   Copyright 2013 Tuukka Takala, Mikael Matveinen. All Rights reserved.
Licensing  :   RUIS is distributed under the LGPL Version 3 license.

******************************************************************************/

using UnityEngine;
using System.Collections;

public class ResetBall : MonoBehaviour {
    public RUISPSMoveWand moveController;
    public Transform ballResetSpot;

    private bool shouldResetBall = true;

    void FixedUpdate()
    {
        if (shouldResetBall || moveController.moveButtonWasPressed)
        {
            transform.position = ballResetSpot.transform.position;
            transform.rotation = ballResetSpot.transform.rotation;
            GetComponent<Rigidbody>().velocity = Vector3.zero;
            GetComponent<Rigidbody>().angularVelocity = Vector3.zero;

            shouldResetBall = false;
        }
    }

    void OnTriggerEnter(Collider collider)
    {
        shouldResetBall = true;
    }
}
