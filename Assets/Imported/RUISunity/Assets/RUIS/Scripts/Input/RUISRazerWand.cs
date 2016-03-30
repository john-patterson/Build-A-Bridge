/*****************************************************************************

Content    :   A class to manage the information from a Razer Hydra controller
Authors    :   Tuukka Takala
Copyright  :   Copyright 2013 Tuukka Takala. All Rights reserved.
Licensing  :   RUIS is distributed under the LGPL Version 3 license.

******************************************************************************/

using UnityEngine;
using System.Collections;

public class RUISRazerWand : RUISWand {
	
	RUISTracker headTracker;
	
	public SixenseButtons selectionButton = SixenseButtons.BUMPER;
	public SixenseHands	controller = SixenseHands.LEFT;
	SixenseInput.Controller razer;
	
	private Vector3	sensitivity = new Vector3( 0.001f, 0.001f, 0.001f );

    private Vector3 positionUpdate;
    private Vector3 rotationUpdate;
	
	private Vector3 movingBasePosition = new Vector3(0,0,0);
	private Quaternion movingBaseRotation = Quaternion.identity;
	
	private Quaternion previousRotation;
	private Vector3 angularVelocity;
	
    public Color wandColor = Color.white;
    public override Color color { get { return wandColor; } }

    public Renderer whereToCopyColor;

	public void Awake ()
    {
		previousRotation = Quaternion.identity;
		angularVelocity = Vector3.zero;
	}
	
	
	public void Start ()
    {
		// Try to find RUISTracker that uses Razer Hydra in some way
		RUISTracker[] trackers = Object.FindObjectsOfType(typeof(RUISTracker)) as RUISTracker[];
		for(int i = 0; i < trackers.Length; ++i)
		{
			// Below complicated clauses make sure that this is a properly configured Razer Hydra tracker
			if(		trackers[i].headPositionInput == RUISTracker.HeadPositionSource.RazerHydra
				||	(	trackers[i].headRotationInput == RUISTracker.HeadRotationSource.RazerHydra
					 &&	!trackers[i].useOculusRiftRotation)
				||	(	trackers[i].compass == RUISTracker.CompassSource.RazerHydra
					 && trackers[i].externalDriftCorrection
					 && (	trackers[i].useOculusRiftRotation 
						 || trackers[i].headRotationInput == RUISTracker.HeadRotationSource.InputTransform )))
				if(trackers[i].isRazerBaseMobile) // Found a Razer Hydra tracker that claims to have mobile base
					headTracker = trackers[i];
		}
	}
	
	void Update ()
    {        
        
		razer = SixenseInput.GetController( controller );
		
		
        if(!GetComponent<Rigidbody>())
		{
			// isRazerBaseMobile is false if Razer Hydra is not position, rotation, or compass source in headTracker
			if(		headTracker 
				&&	headTracker.isRazerBaseMobile )
			{
				movingBasePosition = headTracker.hydraBasePosition;
				movingBaseRotation = headTracker.hydraBaseRotation;
			}
			else
			{
				movingBasePosition = Vector3.zero;
				movingBaseRotation = Quaternion.identity;
			}
			
            transform.localPosition = movingBaseRotation * localPosition + movingBasePosition;
            transform.localRotation = movingBaseRotation * localRotation;
        }


        if (whereToCopyColor != null)
        {
            foreach (Material mat in whereToCopyColor.materials)
            {
                mat.color = color;
            }
        }
    }

    void FixedUpdate()
    {
        if (GetComponent<Rigidbody>())
        {
            // TUUKKA:
            if (transform.parent)
            {
                // If the wand has a parent, we need to apply its transformation first
                // *** FIXME: If parent is scaled, then compound objects (Watering Bottle) get weird
                GetComponent<Rigidbody>().MovePosition(transform.parent.TransformPoint(movingBaseRotation * localPosition + movingBasePosition));
                GetComponent<Rigidbody>().MoveRotation(transform.parent.rotation * movingBaseRotation * localRotation);
            }
            else
            {
                // TUUKKA: This was the original code 
                GetComponent<Rigidbody>().MovePosition(movingBaseRotation * localPosition + movingBasePosition);
                GetComponent<Rigidbody>().MoveRotation(movingBaseRotation * localRotation);
            }
        }
		
		angularVelocity = (Quaternion.Inverse(previousRotation)*localRotation).eulerAngles*Time.fixedDeltaTime;
		previousRotation = localRotation;
    }

    public override bool SelectionButtonWasPressed()
    {
		if(razer == null || !razer.Enabled)
			return false;
        switch (selectionButton)
        {
            case SixenseButtons.BUMPER:
                return razer.GetButtonDown(SixenseButtons.BUMPER);
            case SixenseButtons.FOUR:
                return razer.GetButtonDown(SixenseButtons.FOUR);
            case SixenseButtons.JOYSTICK:
                return razer.GetButtonDown(SixenseButtons.JOYSTICK);
            case SixenseButtons.ONE:
                return razer.GetButtonDown(SixenseButtons.ONE);
            case SixenseButtons.START:
                return razer.GetButtonDown(SixenseButtons.START);
            case SixenseButtons.THREE:
                return razer.GetButtonDown(SixenseButtons.THREE);
            case SixenseButtons.TRIGGER:
                return razer.GetButtonDown(SixenseButtons.TRIGGER);
            case SixenseButtons.TWO:
                return razer.GetButtonDown(SixenseButtons.TWO);
            default:
                return false;
        }
    }

    public override bool SelectionButtonWasReleased()
    {
		if(razer == null || !razer.Enabled)
			return false;
        switch (selectionButton)
        {
            case SixenseButtons.BUMPER:
                return razer.GetButtonUp(SixenseButtons.BUMPER);
            case SixenseButtons.FOUR:
                return razer.GetButtonUp(SixenseButtons.FOUR);
            case SixenseButtons.JOYSTICK:
                return razer.GetButtonUp(SixenseButtons.JOYSTICK);
            case SixenseButtons.ONE:
                return razer.GetButtonUp(SixenseButtons.ONE);
            case SixenseButtons.START:
                return razer.GetButtonUp(SixenseButtons.START);
            case SixenseButtons.THREE:
                return razer.GetButtonUp(SixenseButtons.THREE);
            case SixenseButtons.TRIGGER:
                return razer.GetButtonUp(SixenseButtons.TRIGGER);
            case SixenseButtons.TWO:
                return razer.GetButtonUp(SixenseButtons.TWO);
            default:
                return false;
        }
    }

    public override bool SelectionButtonIsDown()
    {
		if(razer == null || !razer.Enabled)
			return false;
        switch (selectionButton)
        {
            case SixenseButtons.BUMPER:
                return razer.GetButton(SixenseButtons.BUMPER);
            case SixenseButtons.FOUR:
                return razer.GetButton(SixenseButtons.FOUR);
            case SixenseButtons.JOYSTICK:
                return razer.GetButton(SixenseButtons.JOYSTICK);
            case SixenseButtons.ONE:
                return razer.GetButton(SixenseButtons.ONE);
            case SixenseButtons.START:
                return razer.GetButton(SixenseButtons.START);
            case SixenseButtons.THREE:
                return razer.GetButton(SixenseButtons.THREE);
            case SixenseButtons.TRIGGER:
                return razer.GetButton(SixenseButtons.TRIGGER);
            case SixenseButtons.TWO:
                return razer.GetButton(SixenseButtons.TWO);
            default:
                return false;
        }
    }

    public override bool IsSelectionButtonStandard()
    {
        return true;
    }

    public Vector3 localPosition
    {
        get
        {
			if(razer == null || !razer.Enabled)
				return Vector3.zero;
            return new Vector3( razer.Position.x * sensitivity.x,
								razer.Position.y * sensitivity.y,
								razer.Position.z * sensitivity.z );
        }
    }
	
	// THIS NOW RETURNS WORLD VELOCITY WHILE OTHERS (E.G. position) RETURN LOCAL VALUES
//    public Vector3 velocity
//    {
//        get
//        {
//			// If the wand has a parent, we need to apply its transformation first
//			if (transform.parent)
//				return transform.parent.TransformDirection(
//											TransformVelocity(psMoveWrapper.velocity[controllerId]));
//			else 
//				return TransformVelocity(psMoveWrapper.velocity[controllerId]);
//			// TUUKKA: TransformPosition??? This is the old version 
//            //return TransformPosition(psMoveWrapper.velocity[controllerId]);
//        }
//    }
//	
//	// THIS NOW RETURNS WORLD ACCELERATION WHILE OTHERS (E.G. position) RETURN LOCAL VALUES
//    public Vector3 acceleration
//    {
//        get
//        {
//			// If the wand has a parent, we need to apply its transformation first
//			if (transform.parent)
//				return transform.parent.TransformDirection(
//											TransformVelocity(psMoveWrapper.acceleration[controllerId]));
//			else 
//				return TransformVelocity(psMoveWrapper.acceleration[controllerId]);
//			// TUUKKA: TransformPosition??? This is the old version 
//            //return TransformPosition(psMoveWrapper.acceleration[controllerId]);
//        }
//    }

    public Quaternion localRotation
    {
        get
        {
			if(razer == null || !razer.Enabled)
				return Quaternion.identity;
            return razer.Rotation;
        }
    }
	// THIS NOW RETURNS WORLD angularVelocity WHILE OTHERS (E.G. position) RETURN LOCAL VALUES
//    public Vector3 angularVelocity
//    {
//        get
//        {	
//			// If the wand has a parent, we need to apply its transformation first
//			if (transform.parent)
//				return transform.parent.TransformDirection(
//					coordinateSystem.ConvertMoveAngularVelocity(psMoveWrapper.angularVelocity[controllerId]));
//			else 
//            	return coordinateSystem.ConvertMoveAngularVelocity(psMoveWrapper.angularVelocity[controllerId]);
//        }
//    }
//	// THIS NOW RETURNS WORLD angularAcceleration WHILE OTHERS (E.G. position) RETURN LOCAL VALUES
//    public Vector3 angularAcceleration
//    {
//        get
//        {
//			// If the wand has a parent, we need to apply its transformation first
//			if (transform.parent)
//				return transform.parent.TransformDirection(
//					coordinateSystem.ConvertMoveAngularVelocity(psMoveWrapper.angularAcceleration[controllerId]));
//			else 
//            	return coordinateSystem.ConvertMoveAngularVelocity(psMoveWrapper.angularAcceleration[controllerId]);
//        }
//    }

    public bool bumperButtonDown { get { return razer.GetButton(SixenseButtons.BUMPER); } }
    public bool fourButtonDown { get { return razer.GetButton(SixenseButtons.FOUR); } }
    public bool joystickButtonDown { get { return razer.GetButton(SixenseButtons.JOYSTICK); } }
    public bool oneButtonDown { get { return razer.GetButton(SixenseButtons.ONE); } }
    public bool startButtonDown { get { return razer.GetButton(SixenseButtons.START); } }
    public bool threeButtonDown { get { return razer.GetButton(SixenseButtons.THREE); } }
    public bool triggerButtonDown { get { return razer.GetButton(SixenseButtons.TRIGGER); } }
    public bool twoButtonDown { get { return razer.GetButton(SixenseButtons.TWO); } }
	
    public bool bumperButtonWasPressed { get { return razer.GetButtonDown(SixenseButtons.BUMPER); } }
    public bool fourButtonWasPressed { get { return razer.GetButtonDown(SixenseButtons.FOUR); } }
    public bool joystickButtonWasPressed { get { return razer.GetButtonDown(SixenseButtons.JOYSTICK); } }
    public bool oneButtonWasPressed { get { return razer.GetButtonDown(SixenseButtons.ONE); } }
    public bool startButtonWasPressed { get { return razer.GetButtonDown(SixenseButtons.START); } }
    public bool threeButtonWasPressed { get { return razer.GetButtonDown(SixenseButtons.THREE); } }
    public bool triggerButtonWasPressed { get { return razer.GetButtonDown(SixenseButtons.TRIGGER); } }
    public bool twoButtonWasPressed { get { return razer.GetButtonDown(SixenseButtons.TWO); } }
	
    public bool bumperButtonWasReleased { get { return razer.GetButtonUp(SixenseButtons.BUMPER); } }
    public bool fourButtonWasReleased { get { return razer.GetButtonUp(SixenseButtons.FOUR); } }
    public bool joystickButtonWasReleased { get { return razer.GetButtonUp(SixenseButtons.JOYSTICK); } }
    public bool oneButtonWasReleased { get { return razer.GetButtonUp(SixenseButtons.ONE); } }
    public bool startButtonWasReleased { get { return razer.GetButtonUp(SixenseButtons.START); } }
    public bool threeButtonWasReleased { get { return razer.GetButtonUp(SixenseButtons.THREE); } }
    public bool triggerButtonWasReleased { get { return razer.GetButtonUp(SixenseButtons.TRIGGER); } }
    public bool twoButtonWasReleased { get { return razer.GetButtonUp(SixenseButtons.TWO); } }


    public float triggerValue { get {	if(razer == null || !razer.Enabled)
											return 0;
										return razer.Trigger; } }
	
    public float joystickX { get {	if(razer == null || !razer.Enabled)
											return 0;
										return razer.JoystickX; } }
	
    public float joystickY { get {	if(razer == null || !razer.Enabled)
											return 0;
										return razer.JoystickY; } }
	
	// TUUKKA:
//    private Vector3 TransformVelocity(Vector3 value)
//    {
//        return coordinateSystem.ConvertMoveVelocity(value);
//    }
//	
//    private Vector3 TransformPosition(Vector3 value)
//    {
//        return coordinateSystem.ConvertMovePosition(value);
//    }
//
//    public override Vector3 GetAngularVelocity()
//    {
//        return angularVelocity;
//    }
    public override Vector3 GetAngularVelocity()
    {
		// If the wand has a parent, we need to apply its transformation first
		if (transform.parent)
			return transform.parent.TransformDirection(angularVelocity);
		else 
			return angularVelocity;
    }
}
