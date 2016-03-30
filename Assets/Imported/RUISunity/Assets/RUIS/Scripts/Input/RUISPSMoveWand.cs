/*****************************************************************************

Content    :   A class to manage the information from a PS Move controller
Authors    :   Tuukka Takala, Mikael Matveinen
Copyright  :   Copyright 2013 Tuukka Takala, Mikael Matveinen. All Rights reserved.
Licensing  :   RUIS is distributed under the LGPL Version 3 license.

******************************************************************************/

using UnityEngine;
using System.Collections;


[AddComponentMenu("RUIS/Input/RUISPSMoveController")]
public class RUISPSMoveWand : RUISWand {
    public enum SelectionButton
    {
        Trigger,
        Cross,
        Circle,
        Square,
        Triangle,
        Move,
        Start,
        Select,
        None
    }

    public SelectionButton selectionButton;

    private static PSMoveWrapper psMoveWrapper;
    
    public int controllerId;

    private Vector3 positionUpdate;
    private Vector3 rotationUpdate;

    protected RUISCoordinateSystem coordinateSystem;

	public Renderer whereToCopyColor;
	[Range(0f, 1f)]
	public float rayColorAlpha = 1;
	
	public void Awake ()
    {
    
        if (psMoveWrapper == null)
        {
            psMoveWrapper = FindObjectOfType(typeof(PSMoveWrapper)) as PSMoveWrapper;
            if (!psMoveWrapper)
            {
                Debug.LogError("Could not find PSMoveWrapper");
            }
        }

        if (coordinateSystem == null)
        {
            coordinateSystem = FindObjectOfType(typeof(RUISCoordinateSystem)) as RUISCoordinateSystem;
            if (!coordinateSystem)
            {
                Debug.LogError("Could not find RUISCoordinateSystem script! It should be located in RUIS->InputManager.");
            }
        }
	}
	
	void Update ()
    {        
        if(!GetComponent<Rigidbody>())
		{
            transform.localPosition = localPosition;
            transform.localRotation = localRotation;
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
                GetComponent<Rigidbody>().MovePosition(transform.parent.TransformPoint(localPosition));
                GetComponent<Rigidbody>().MoveRotation(transform.parent.rotation * localRotation);
            }
            else
            {
                // TUUKKA: This was the original code 
                GetComponent<Rigidbody>().MovePosition(localPosition);
                GetComponent<Rigidbody>().MoveRotation(localRotation);
            }
        }
    }

    public void RumbleOn(int strength)
    {
        psMoveWrapper.SetRumble(controllerId, strength);
    }

    public void RumbleOff() 
    {
        psMoveWrapper.SetRumble(controllerId, 0);
    }

    public void Rumble(int strength, float duration)
    {
        StartCoroutine(DoRumble(strength, duration));
    }

    private IEnumerator DoRumble(int strength, float duration)
    {
        psMoveWrapper.SetRumble(controllerId, strength);
        yield return new WaitForSeconds(duration);
        RumbleOff();
    }

    public void SetColor(Color color)
    {
        psMoveWrapper.SetColorAndTrack(controllerId, color);
    }

    public override bool SelectionButtonWasPressed()
    {
        switch (selectionButton)
        {
            case SelectionButton.Trigger:
                return triggerButtonWasPressed;
            case SelectionButton.Cross:
                return crossButtonWasPressed;
            case SelectionButton.Circle:
                return circleButtonWasPressed;
            case SelectionButton.Square:
                return squareButtonWasPressed;
            case SelectionButton.Triangle:
                return triangleButtonWasPressed;
            case SelectionButton.Move:
                return moveButtonWasPressed;
            case SelectionButton.Start:
                return startButtonWasPressed;
            case SelectionButton.Select:
                return selectButtonWasPressed;
            default:
                return false;
        }
    }

    public override bool SelectionButtonWasReleased()
    {
        switch (selectionButton)
        {
            case SelectionButton.Trigger:
                return triggerButtonWasReleased;
            case SelectionButton.Cross:
                return crossButtonWasReleased;
            case SelectionButton.Circle:
                return circleButtonWasReleased;
            case SelectionButton.Square:
                return squareButtonWasReleased;
            case SelectionButton.Triangle:
                return triangleButtonWasReleased;
            case SelectionButton.Move:
                return moveButtonWasReleased;
            case SelectionButton.Start:
                return startButtonWasReleased;
            case SelectionButton.Select:
                return selectButtonWasReleased;
            default:
                return false;
        }
    }

    public override bool SelectionButtonIsDown()
    {
        switch (selectionButton)
        {
            case SelectionButton.Trigger:
                return triggerValue > 0.99f;
            case SelectionButton.Cross:
                return crossButtonDown;
            case SelectionButton.Circle:
                return circleButtonDown;
            case SelectionButton.Square:
                return squareButtonDown;
            case SelectionButton.Triangle:
                return triangleButtonDown;
            case SelectionButton.Move:
                return moveButtonDown;
            case SelectionButton.Start:
                return startButtonDown;
            case SelectionButton.Select:
                return selectButtonDown;
            default:
                return false;
        }
    }

    public override bool IsSelectionButtonStandard()
    {
        return true;
    }


	public bool motionSmoothing = false;
	public float refreshRate = 50;
	private float lastUpdateTime = 0;
	private Vector3 previousPosition = Vector3.zero;
	private Vector3 candidatePosition = Vector3.zero;
	private Vector3 latestPosition = Vector3.zero;

    public Vector3 localPosition
    {
        get
        {
			if(!motionSmoothing)
				 return TransformPosition(psMoveWrapper.position[controllerId]);
			else // HACK: PSMoveWrapper doesn't provide info about when a new "frame" arrived. Anyway, this hack doesn't seem to help.
			{
				float lerp = 0;
				candidatePosition = psMoveWrapper.position[controllerId];
				if(latestPosition.x == candidatePosition.x && latestPosition.y == candidatePosition.y && latestPosition.z == candidatePosition.z)
					lerp = refreshRate * (Time.time - lastUpdateTime);
				else
				{
					refreshRate = Mathf.Clamp(1/(Time.time - lastUpdateTime), 1, 100);
					lastUpdateTime = Time.time;
					previousPosition = latestPosition;
					latestPosition = candidatePosition;
				}
				return TransformPosition(Vector3.Lerp(previousPosition, latestPosition, lerp));
			}
        }
    }
	
	// Returns velocity in wand's local coordinate system, unaffected my parent's scale
    public Vector3 velocity
    {
        get
        {
			// If the wand has a parent, we need to apply its transformation first
			/* if (transform.parent)
				return transform.parent.TransformDirection(
											TransformVelocity(psMoveWrapper.velocity[controllerId]));
			else */
				return TransformVelocity(psMoveWrapper.velocity[controllerId]);
			// TUUKKA: TransformPosition??? This is the old version 
            //return TransformPosition(psMoveWrapper.velocity[controllerId]);
        }
    }
	
	// Returns acceleration in wand's local coordinate system, unaffected my parent's scale
    public Vector3 acceleration
    {
        get
        {
			// If the wand has a parent, we need to apply its transformation first
			/* if (transform.parent)
				return transform.parent.TransformDirection(
											TransformVelocity(psMoveWrapper.acceleration[controllerId]));
			else */
				return TransformVelocity(psMoveWrapper.acceleration[controllerId]);
			// TUUKKA: TransformPosition??? This is the old version 
            //return TransformPosition(psMoveWrapper.acceleration[controllerId]);
        }
    }

    public Quaternion localRotation
    {
        get
        {
			return coordinateSystem.ConvertRotation(coordinateSystem.ConvertRawPSMoveRotation(psMoveWrapper.qOrientation[controllerId]),RUISDevice.PS_Move);
        }
    }

	// Returns angularVelocity in wand's local coordinate system, unaffected by parent's scale
    public Vector3 angularVelocity
    {
        get
        {	
			// If the wand has a parent, we need to apply its transformation first
			/* if (transform.parent)
				return transform.parent.TransformDirection(
					coordinateSystem.ConvertMoveAngularVelocity(psMoveWrapper.angularVelocity[controllerId]));
			else */
			return coordinateSystem.ConvertMoveAngularVelocity(psMoveWrapper.angularVelocity[controllerId]);
        }
	}

	// Returns angularAcceleration in wand's local coordinate system, unaffected my parent's scale
    public Vector3 angularAcceleration
    {
        get
        {
			// If the wand has a parent, we need to apply its transformation first
			/* if (transform.parent)
				return transform.parent.TransformDirection(
					coordinateSystem.ConvertMoveAngularVelocity(psMoveWrapper.angularAcceleration[controllerId]));
			else */
			return coordinateSystem.ConvertMoveAngularVelocity(psMoveWrapper.angularAcceleration[controllerId]);
        }
    }
	
	// Returns handlePosition in wand's local coordinate system, unaffected my parent's scale
    public Vector3 handlePosition
    {
        get
        {
            return TransformPosition(psMoveWrapper.handlePosition[controllerId]);
        }
    }
	
	// Returns handleVelocity in wand's local coordinate system, unaffected my parent's scale
    public Vector3 handleVelocity
    {
        get
        {
			return TransformVelocity(psMoveWrapper.handleVelocity[controllerId]);
        }
	}

	// Returns handleAcceleration in wand's local coordinate system, unaffected my parent's scale
    public Vector3 handleAcceleration
    {
        get
        {
			return TransformVelocity(psMoveWrapper.handleAcceleration[controllerId]);
        }
    }

    public bool squareButtonDown { get { return psMoveWrapper.isButtonSquare[controllerId]; } }
    public bool crossButtonDown { get { return psMoveWrapper.isButtonCross[controllerId]; } }
    public bool circleButtonDown { get { return psMoveWrapper.isButtonCircle[controllerId]; } }
    public bool triangleButtonDown { get { return psMoveWrapper.isButtonTriangle[controllerId]; } }
    public bool moveButtonDown { get { return psMoveWrapper.isButtonMove[controllerId]; } }
    public bool startButtonDown { get { return psMoveWrapper.isButtonStart[controllerId]; } }
    public bool selectButtonDown { get { return psMoveWrapper.isButtonSelect[controllerId]; } }

    public bool squareButtonWasPressed { get { return psMoveWrapper.WasPressed(controllerId, PSMoveWrapper.SQUARE); } }
    public bool crossButtonWasPressed { get { return psMoveWrapper.WasPressed(controllerId, PSMoveWrapper.CROSS); } }
    public bool circleButtonWasPressed { get { return psMoveWrapper.WasPressed(controllerId, PSMoveWrapper.CIRCLE); } }
    public bool triangleButtonWasPressed { get { return psMoveWrapper.WasPressed(controllerId, PSMoveWrapper.TRIANGLE); } }
    public bool moveButtonWasPressed { get { return psMoveWrapper.WasPressed(controllerId, PSMoveWrapper.MOVE); } }
    public bool triggerButtonWasPressed { get { return psMoveWrapper.WasPressed(controllerId, PSMoveWrapper.T); } }
    public bool startButtonWasPressed { get { return psMoveWrapper.WasPressed(controllerId, PSMoveWrapper.START); } }
    public bool selectButtonWasPressed { get { return psMoveWrapper.WasPressed(controllerId, PSMoveWrapper.SELECT); } }

    public bool squareButtonWasReleased { get { return psMoveWrapper.WasReleased(controllerId, PSMoveWrapper.SQUARE); } }
    public bool crossButtonWasReleased { get { return psMoveWrapper.WasReleased(controllerId, PSMoveWrapper.CROSS); } }
    public bool circleButtonWasReleased { get { return psMoveWrapper.WasReleased(controllerId, PSMoveWrapper.CIRCLE); } }
    public bool triangleButtonWasReleased { get { return psMoveWrapper.WasReleased(controllerId, PSMoveWrapper.TRIANGLE); } }
    public bool moveButtonWasReleased { get { return psMoveWrapper.WasReleased(controllerId, PSMoveWrapper.MOVE); } }
    public bool triggerButtonWasReleased { get { return psMoveWrapper.WasReleased(controllerId, PSMoveWrapper.T); } }
    public bool startButtonWasReleased { get { return psMoveWrapper.WasReleased(controllerId, PSMoveWrapper.START); } }
    public bool selectButtonWasReleased { get { return psMoveWrapper.WasReleased(controllerId, PSMoveWrapper.SELECT); } }

    public override Color color { 
		get { 
				return new Color(psMoveWrapper.sphereColor[controllerId].r, psMoveWrapper.sphereColor[controllerId].g, 
			                 	 psMoveWrapper.sphereColor[controllerId].b, rayColorAlpha);
//				return psMoveWrapper.sphereColor[controllerId]; 
		} 
		set { SetColor(value); } }

    public float triggerValue { get { return psMoveWrapper.valueT[controllerId] / 255.0f; } }
	
	// TUUKKA:
    private Vector3 TransformVelocity(Vector3 value)
    {
		return coordinateSystem.ConvertMoveVelocity(value);
    }
	
    private Vector3 TransformPosition(Vector3 value)
    {
		return coordinateSystem.ConvertLocation(coordinateSystem.ConvertRawPSMoveLocation(value), RUISDevice.PS_Move);
    }
	
	// Returns angularVelocity in wand's local coordinate system
    public override Vector3 GetAngularVelocity()
    {
        return angularVelocity;
    }
}
