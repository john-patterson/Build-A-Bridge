/*****************************************************************************

Content    :   Respawn GameObject near a Transform upon PS Move button press.
Authors    :   Tuukka Takala
Copyright  :   Copyright 2015 Tuukka Takala. All Rights reserved.
Licensing  :   RUIS is distributed under the LGPL Version 3 license.

******************************************************************************/

using UnityEngine;
using System.Collections;

public class RespawnObject : MonoBehaviour 
{
	public int moveID = 0;
	public RUISPSMoveWand.SelectionButton moveButton = RUISPSMoveWand.SelectionButton.Cross;
	public Transform respawnOrigin;
	public Vector3 originOffset;
	private RUISPSMoveWand moveWand;
	private string buttonName;
	private Rigidbody rigidBody;
	
	void Start() 
	{
		RUISInputManager inputManager = FindObjectOfType(typeof(RUISInputManager)) as RUISInputManager;
		if(inputManager)
		{
			RUISPSMoveWand[] moveWands = inputManager.GetComponentsInChildren<RUISPSMoveWand>();
			foreach(RUISPSMoveWand wand in moveWands)
			{
				if(moveID == wand.controllerId)
					moveWand = wand;
			}
		}
		rigidBody = this.gameObject.GetComponent<Rigidbody>();
	}
	
	void Update () 
	{
		if(moveWand)
		{
			bool wasButtonPressed = false;
			
			switch(moveButton)
			{
				case RUISPSMoveWand.SelectionButton.Cross:    wasButtonPressed = moveWand.crossButtonWasPressed; break;
				case RUISPSMoveWand.SelectionButton.Circle:   wasButtonPressed = moveWand.circleButtonWasPressed; break;
				case RUISPSMoveWand.SelectionButton.Move:     wasButtonPressed = moveWand.moveButtonWasPressed; break;
				case RUISPSMoveWand.SelectionButton.Select:   wasButtonPressed = moveWand.selectButtonWasPressed; break;
				case RUISPSMoveWand.SelectionButton.Square:   wasButtonPressed = moveWand.squareButtonWasPressed; break;
				case RUISPSMoveWand.SelectionButton.Start:    wasButtonPressed = moveWand.startButtonWasPressed; break;
				case RUISPSMoveWand.SelectionButton.Triangle: wasButtonPressed = moveWand.triangleButtonWasPressed; break;
				case RUISPSMoveWand.SelectionButton.Trigger:  wasButtonPressed = moveWand.triggerButtonWasPressed; break;
			}
			if(wasButtonPressed)
			{
				if(respawnOrigin)
					this.gameObject.transform.position = respawnOrigin.position + respawnOrigin.rotation * originOffset;
				else
					this.gameObject.transform.position = originOffset;

				if(rigidBody)
				{
					rigidBody.velocity        = Vector3.zero;
					rigidBody.angularVelocity = Vector3.zero;
				}

			}
		}
	}
}
