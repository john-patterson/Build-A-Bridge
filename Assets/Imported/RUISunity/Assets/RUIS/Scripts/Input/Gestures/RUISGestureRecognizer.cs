/*****************************************************************************

Content    :   A base class for all gestures recognizers
Authors    :   Mikael Matveinen
Copyright  :   Copyright 2013 Tuukka Takala, Mikael Matveinen. All Rights reserved.
Licensing  :   RUIS is distributed under the LGPL Version 3 license.

******************************************************************************/

using UnityEngine;
using System.Collections;

[RequireComponent(typeof(RUISPointTracker))]
public abstract class RUISGestureRecognizer : MonoBehaviour {
    public abstract bool GestureIsTriggered();
	public abstract bool GestureWasTriggered(); 
	
    public abstract float GetGestureProgress();
    public abstract void ResetProgress();

    public abstract void EnableGesture();
	public abstract void DisableGesture();
	
	public abstract bool IsBinaryGesture();

}
