/*****************************************************************************

Content    :   Functionality to reel in a RUISSelectable once it's selected
Authors    :   Mikael Matveinen
Copyright  :   Copyright 2013 Tuukka Takala, Mikael Matveinen. All Rights reserved.
Licensing  :   RUIS is distributed under the LGPL Version 3 license.

******************************************************************************/

using UnityEngine;
using System.Collections;

public class ReelInSelection : MonoBehaviour {
    public float reelSpeed = 1.0f;
    
    RUISPSMoveWand psMoveController;
    RUISWandSelector wandSelector;

    RUISSelectable selection;

    bool wasClampedToCertainDistance;
    float distanceClampedTo;

    float currentDistance = 1;

	void Awake () {
        psMoveController = GetComponent<RUISPSMoveWand>();
        wandSelector = GetComponent<RUISWandSelector>();
	}

    void Update () {
        if (!wandSelector.Selection && selection)
        {
            selection.clampToCertainDistance = wasClampedToCertainDistance;
            selection.distanceToClampTo = distanceClampedTo;
        }

        if (!wandSelector.Selection || wandSelector.positionSelectionGrabType != RUISWandSelector.SelectionGrabType.AlongSelectionRay)
        {
            selection = null;
            return;
        }

        if(!selection){
            selection = wandSelector.Selection;
            
            wasClampedToCertainDistance = selection.clampToCertainDistance;
            selection.clampToCertainDistance = true;

            distanceClampedTo = selection.distanceToClampTo;

            currentDistance = 1;
        }

        float currentDistanceChange = ((1 - psMoveController.triggerValue) - currentDistance) * Time.deltaTime * reelSpeed;
        currentDistance += currentDistanceChange;
        selection.distanceToClampTo = currentDistance * selection.DistanceFromSelectionRayOrigin;
	}
}
