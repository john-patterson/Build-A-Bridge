/*****************************************************************************

Content    :   Functionality to send RUISCharacterLocomotionControl and RUISCharacterController info forward to a Mecanim Animator
Authors    :   Mikael Matveinen, Tuukka Takala
Copyright  :   Copyright 2013 Tuukka Takala, Mikael Matveinen. All Rights reserved.
Licensing  :   RUIS is distributed under the LGPL Version 3 license.

******************************************************************************/


using UnityEngine;
using System.Collections;

public class RUISCharacterAnimationController : MonoBehaviour 
{
	public float animationBlendStrength = 10.0f;

    public Animator animator;
    public RUISCharacterLocomotion locomotionControl;
    public RUISCharacterController characterController;
    public RUISKinectAndMecanimCombiner animationCombiner;
	
	public float forwardSpeed { get; private set; }
	public float strafeSpeed { get; private set; }

	void Awake()
	{
		if(!animator)
			Debug.LogError(typeof(Animator).Name + " is not attached to " + this.name + " script!");
		if(!locomotionControl)
			Debug.LogError(typeof(RUISCharacterLocomotion).Name + " is not attached to " + this.name + " script!");
		if(!characterController)
			Debug.LogError(typeof(RUISCharacterController).Name + " is not attached to " + this.name + " script!");
		if(!animationCombiner)
			Debug.LogError(typeof(RUISKinectAndMecanimCombiner).Name + " is not attached to " + this.name + " script!");
	}

	void Update () 
	{
		if (!animator) return;

		// Use Lerp() to filter the joystick control directions (forwardSpeed and strafeSpeed)
		// desiredVelocity is a vector with magnitude between 0 (not moving) and 2 (sprinting)
		forwardSpeed = Mathf.Lerp(forwardSpeed, locomotionControl.desiredVelocity.z, Time.deltaTime * animationBlendStrength);
		strafeSpeed = Mathf.Lerp(strafeSpeed, locomotionControl.desiredVelocity.x, Time.deltaTime * animationBlendStrength);

		// Pass parameters to animator
        animator.SetBool("Grounded", characterController.grounded);
        animator.SetFloat("ForwardSpeed", forwardSpeed);
        animator.SetFloat("StrafeSpeed", strafeSpeed);
        animator.SetFloat("Direction", locomotionControl.direction);
        animator.SetBool("Jump", locomotionControl.jump);

        if (characterController.grounded) // The character is supported from below
        {
			// Adjust blending between Kinect and Mecanim animation according to joystick control magnitude
            float maxOfForwardOrStrafe = Mathf.Max(Mathf.Abs(forwardSpeed), Mathf.Abs(strafeSpeed));
            animationCombiner.leftLegBlendWeight = Mathf.Clamp01(maxOfForwardOrStrafe);
            animationCombiner.rightLegBlendWeight = Mathf.Clamp01(maxOfForwardOrStrafe);
        }
        else // The character is in the air
        {
//            animationCombiner.leftLegBlendWeight = 1;
//            animationCombiner.rightLegBlendWeight = 1;
        }
	}
}
