/*****************************************************************************

Content    :   Behavior to follow a transform at a certain offset
Authors    :   Mikael Matveinen
Copyright  :   Copyright 2013 Tuukka Takala, Mikael Matveinen. All Rights reserved.
Licensing  :   RUIS is distributed under the LGPL Version 3 license.

******************************************************************************/

using UnityEngine;
using System.Collections;

public class FixedFollowTransform : MonoBehaviour {
    public Transform transformToFollow;
    public Vector3 offset;
    public bool lookAt;
	
	void LateUpdate () {
        transform.position = transformToFollow.position + offset;
        if (lookAt)
        {
            transform.rotation = Quaternion.LookRotation(transformToFollow.position - transform.position);
        }
	}
}
