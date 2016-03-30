/*****************************************************************************

Content    :   Class for locomotion of Kinect controlled character
Authors    :   Mikael Matveinen, Tuukka Takala
Copyright  :   Copyright 2013 Tuukka Takala, Mikael Matveinen. All Rights reserved.
Licensing  :   RUIS is distributed under the LGPL Version 3 license.

******************************************************************************/

using UnityEngine;
using System.Collections;

[RequireComponent(typeof(RUISCharacterController))]
[RequireComponent(typeof(Rigidbody))]
public class RUISCharacterLocomotion : MonoBehaviour
{
    RUISCharacterController characterController;
	//RUISInputManager inputManager;
	
    public KeyCode turnRightKey = KeyCode.E;
    public KeyCode turnLeftKey = KeyCode.Q;

    public float rotationScaler = 60.0f;
	private float turnMagnitude = 0;

    public float speed = 2.0f;
	public float runAdder = 1.0f;
    public float maxVelocityChange = 20.0f;

    public bool usePSNavigationController = true;
    public int PSNaviControllerID = 1;
	public bool strafeInsteadTurning = false;

    public float jumpStrength = 10f;
	public float jumpSpeedEffect = 0;
	public float aerialAcceleration = 20;
	public float aerialMobility = 1.5f;
	public float aerialDrag = 4;
	
	private Vector3 velocity = new Vector3(0, 0, 0);
	private Vector3 velocityChange = new Vector3(0, 0, 0);
	private Vector3 proposedVelocity = new Vector3(0, 0, 0);
	private bool airborne = false;
	private bool grounded = true;
	private bool colliding = true;
	private Vector3 proposedAcceleration = new Vector3(0, 0, 0);
	private Vector3 jumpTimeVelocity = new Vector3(0, 0, 0);
	private Vector3 targetVelocity = new Vector3(0, 0, 0);
	private Vector3 controlDirection = new Vector3(0, 0, 0);
	
	private Vector3 airborneAccumulatedVelocity = new Vector3(0, 0, 0);
	private Vector3 tempAcceleration = new Vector3(0, 0, 0);
	private Vector3 tempVelocity = new Vector3(0, 0, 0);

	public Vector3 desiredVelocity = new Vector3(0, 0, 0);
	
	float extraSpeed = 0;

    private RUISJumpGestureRecognizer jumpGesture;

    public float direction { get; private set; }
    public bool jump { get; private set; }
	
    // TUUKKA
	public bool useRazerHydra = true;
	public SixenseHands razerHydraID = SixenseHands.RIGHT;
	SixenseInput.Controller razerController;
	
    PSMoveWrapper moveWrapper;

    bool shouldJump = false;

    void Awake()
    {
        characterController = GetComponent<RUISCharacterController>();
        jumpGesture = GetComponentInChildren<RUISJumpGestureRecognizer>();

        moveWrapper = FindObjectOfType(typeof(PSMoveWrapper)) as PSMoveWrapper;

        try
        {
            Input.GetAxis("Sprint");
        }
        catch (UnityException)
        {
			Debug.LogWarning("'Sprint' not defined in Unity Input settings");
        }

        try
        {
            Input.GetAxis("Turn");
        }
        catch (UnityException)
        {
			Debug.LogWarning("'Turn' not defined in Unity Input settings");
        }
		
		try
		{
			Input.GetAxis("Jump");
		}
		catch (UnityException)
		{
			Debug.LogWarning("'Jump' not defined in Unity Input settings");
		}
    }
	
	void Start()
	{
		RUISInputManager inputManager = FindObjectOfType(typeof(RUISInputManager)) as RUISInputManager;
		
		if(useRazerHydra && inputManager && !inputManager.enableRazerHydra)
		{
			useRazerHydra = false;
//			Debug.LogWarning(	"Your settings indicate that you want to use Razer Hydra for "
//							 +	"character locomotion controls, but you have disabled Razer "
//							 +	"Hydra from " + typeof(RUISInputManager));
		}

		if(usePSNavigationController && inputManager && !inputManager.enablePSMove)
		{
			usePSNavigationController = false;
//			Debug.LogWarning(	"Your settings indicate that you want to use PS Navigation "
//							 +	"controller for character locomotion controls, but you have "
//							 +	"disabled PS Move from RUIS InputManager.");
		}
		
	}

    void Update()
    {
        jump = false;

        if(characterController == null || !characterController.grounded)
			return;
        
        if ((Input.GetButtonDown("Jump") || JumpGestureTriggered()))
        {
            shouldJump = true;
        }
		
		if(useRazerHydra)
		{
			razerController = SixenseInput.GetController(razerHydraID);
			if(razerController != null && razerController.Enabled)
			{
				if(razerController.GetButtonDown(SixenseButtons.BUMPER))
					shouldJump = true;
			}
		}
		
		// Check if jumping with PS Move Navigation controller
		if (usePSNavigationController && moveWrapper && moveWrapper.isConnected)
        {
            if (PSNaviControllerID <= moveWrapper.navConnected.Length && PSNaviControllerID >= 1)
            {
                if (moveWrapper.navConnected[PSNaviControllerID-1])
                {
                    if(moveWrapper.WasPressed(PSNaviControllerID-1, "NavL1"))
					{
                    	shouldJump = true;
					}
                }
            }
		}

        if (shouldJump)
        {
            jump = true;
        }
    }

    void FixedUpdate()
    {
        //characterController.ApplyForceInCharacterDirection(translation);
		
		float locomotionScale = Mathf.Max(transform.lossyScale.x, transform.lossyScale.z);

        direction = 0;
		turnMagnitude = 0;
		
		if(characterController != null)
		{
			grounded  = characterController.grounded;
			colliding = characterController.colliding;
		}
		
		if(grounded || colliding)
		{
			airborne = false;
		}
		else
		{
			if(!airborne)
			{
				jumpTimeVelocity = GetComponent<Rigidbody>().velocity;
				jumpTimeVelocity.y = 0;
				jumpTimeVelocity = Vector3.ClampMagnitude(jumpTimeVelocity, aerialMobility*speed*locomotionScale);
				airborneAccumulatedVelocity = jumpTimeVelocity;
			}
			airborne = true;
		}
			
		targetVelocity = Vector2.zero;
		extraSpeed = 0;

		try
		{
			targetVelocity = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
		}
		catch (UnityException) { }

        try
        {
	        extraSpeed = Input.GetAxis("Sprint");
	        if (!airborne)
				targetVelocity *= 1 + extraSpeed*runAdder;
        }
        catch (UnityException) { }

        // Check if moving with PS Move Navigation controller
        if (PSNaviControllerID < 1)
        {
            Debug.LogError("PSNaviControllerID was set to " + PSNaviControllerID
                            + " which is incorrect value: It must be positive!");
        }
        else if (usePSNavigationController && moveWrapper && moveWrapper.isConnected)
        {
            if (PSNaviControllerID <= moveWrapper.navConnected.Length)
            {
                if (moveWrapper.navConnected[PSNaviControllerID-1])
                {
					int horiz = moveWrapper.valueNavAnalogX[PSNaviControllerID-1];
					int verti = moveWrapper.valueNavAnalogY[PSNaviControllerID-1];
					if(!airborne)
						extraSpeed = ((float)moveWrapper.valueNavL2[PSNaviControllerID-1]) / 255f;
					else
						extraSpeed = 0;
                    if (Mathf.Abs(verti) > 20)
						targetVelocity += new Vector3(0, 0, -((float)verti) / 128f * (1 + extraSpeed*runAdder));

                    if (strafeInsteadTurning)
                    {
                        if (Mathf.Abs(horiz) > 20)
							targetVelocity += new Vector3(((float)horiz) / 128f * (1 + extraSpeed*runAdder), 0, 0);
                    }
                    else
                    {
                        if (Mathf.Abs(horiz) > 10)
                        {
							turnMagnitude += ((float)horiz) / 128f;
                        }
                    }
					
					if(moveWrapper.isNavButtonCross[PSNaviControllerID-1])
						turnMagnitude -= 1;
					if(moveWrapper.isNavButtonCircle[PSNaviControllerID-1])
						turnMagnitude += 1;
                }
            }
            else
            {
                Debug.LogError("PSNaviControllerID was set to " + PSNaviControllerID
                                + " which is too big value: It must be below 8.");
            }
        }

        // TUUKKA
        if (useRazerHydra) // Check if moving with Razer Hydra controller
        {
            razerController = SixenseInput.GetController(razerHydraID);
            if (razerController != null && razerController.Enabled)
            {
				if(!airborne)
					if(razerController.GetButton(SixenseButtons.JOYSTICK))
		                extraSpeed = 1; //razerController.Trigger;
				else
					extraSpeed = 0;
				
                if (Mathf.Abs(razerController.JoystickY) > 0.15f)
					targetVelocity += new Vector3(0, 0, razerController.JoystickY * (1 + extraSpeed*runAdder));

                if (strafeInsteadTurning)
                {
                    if (Mathf.Abs(razerController.JoystickX) > 0.15f)
						targetVelocity += new Vector3(razerController.JoystickX * (1 + extraSpeed*runAdder), 0, 0);
				}
                else
                {
                    if (Mathf.Abs(razerController.JoystickX) > 0.075f)
                    {
						turnMagnitude += razerController.JoystickX;
                    }
                }
				
				if(razerController.GetButton(SixenseButtons.THREE))
					turnMagnitude -= 1;
				if(razerController.GetButton(SixenseButtons.FOUR))
					turnMagnitude += 1;
            }
        }

		// controlDirection is a unit vector that shows the direction where the joystick is pressed
		controlDirection = Vector3.ClampMagnitude(targetVelocity, 1);
		
		// desiredVelocity is a vector with magnitude between 0 (not moving) and 2 (sprinting)
		desiredVelocity = Vector3.ClampMagnitude(targetVelocity, 1 + extraSpeed);

		// Limit comes from [0,1] + extraSpeed*runAdder
		targetVelocity = Vector3.ClampMagnitude(targetVelocity, 1 + extraSpeed*runAdder);

		
        targetVelocity = characterController.TransformDirection(targetVelocity);
		targetVelocity *= speed*locomotionScale;

        velocity = GetComponent<Rigidbody>().velocity;
        velocityChange = (targetVelocity - velocity);

        velocityChange.y = 0;
		velocityChange = Vector3.ClampMagnitude(velocityChange, Time.fixedDeltaTime * maxVelocityChange * locomotionScale);
		
		if(!airborne)
		{
        	GetComponent<Rigidbody>().AddForce(velocityChange, ForceMode.VelocityChange);
		}
		else
		{
			// Calculate constant air drag whose direction is opposite to the current horizontal velocity vector
			tempVelocity = velocity;
			tempVelocity.y = 0;
			tempAcceleration = -aerialDrag * tempVelocity.normalized ;
			tempAcceleration.y = 0;

			// The drag should only stop the character, not push him like a wind in the opposite direction
			// This condition is true when the tempVelocity is close to zero and its normalization fails
			if(Vector3.Dot(tempVelocity, tempAcceleration) >= 0)
			{
				tempAcceleration = Vector3.zero;
			}

			// Calculate proposed acceleration as a sum of player controls and air drag
			proposedAcceleration = (aerialAcceleration*characterController.TransformDirection(desiredVelocity) + tempAcceleration) * locomotionScale;

			// Integrate proposed total velocity = old velocity + proposed acceleration * deltaT
			proposedVelocity = (airborneAccumulatedVelocity + proposedAcceleration*Time.fixedDeltaTime);

			// If the proposed total velocity is not inside "aerial velocity disc", then shorten the proposed velocity
			// with length of [proposed acceleration * deltaT]. This allows aerial maneuvers along the edge of the disc (circle).
			// In other words: If you have reach maximum aerial velocity to certain direction, you can still control the 
			// velocity in the axis that is perpendicular to that direction
			if(proposedVelocity.magnitude >= aerialMobility*speed* locomotionScale)
			{
				proposedVelocity     -=  1.01f*airborneAccumulatedVelocity.normalized*proposedAcceleration.magnitude*Time.fixedDeltaTime;
				proposedAcceleration -=  1.01f*airborneAccumulatedVelocity.normalized*proposedAcceleration.magnitude;
			}

			// If the proposed total velocity is within allowed "aerial velocity disc", then add the proposed 
			// acceleration to the character and update the accumulatedAerialSpeed accordingly
			if(proposedVelocity.magnitude < aerialMobility*speed*locomotionScale)
			{
				GetComponent<Rigidbody>().AddForce(proposedAcceleration, ForceMode.Acceleration);
				airborneAccumulatedVelocity = proposedVelocity;
			}
		}

        try
        {
			turnMagnitude += Input.GetAxis("Turn");
        }
        catch (UnityException) { }

		if(Input.GetKey(turnLeftKey))
            turnMagnitude -= 1;
        if(Input.GetKey(turnRightKey))
            turnMagnitude += 1;
			
		if(turnMagnitude != 0)
			characterController.RotateAroundCharacterPivot(new Vector3(0, turnMagnitude * rotationScaler * Time.fixedDeltaTime, 0));

        if (shouldJump)
		{
			GetComponent<Rigidbody>().AddForce(
				new Vector3(0, Mathf.Sqrt((1 + 0.5f*(controlDirection.magnitude + extraSpeed)*jumpSpeedEffect)*jumpStrength*locomotionScale)
								* GetComponent<Rigidbody>().mass, 0), ForceMode.Impulse);
			if(characterController)
				characterController.lastJumpTime = Time.fixedTime;
			
            shouldJump = false;
        }
		
    }

    bool JumpGestureTriggered()
    {
        if (jumpGesture == null) return false;

        return jumpGesture.GestureIsTriggered();
    }
}
