/*****************************************************************************

Content    :   A class to move the transform around using the keyboard
Authors    :   Mikael Matveinen, Tuukka Takala
Copyright  :   Copyright 2013 Tuukka Takala, Mikael Matveinen. All Rights reserved.
Licensing  :   RUIS is distributed under the LGPL Version 3 license.

******************************************************************************/

using UnityEngine;
using System.Collections;

public class ShiftViewpointWithKeyboard : MonoBehaviour {
    public float movementScaler = 1;
	public float rotationScaler = 180f;

	public KeyCode moveForward = KeyCode.I;
	public KeyCode moveBackward = KeyCode.K;
	public KeyCode moveLeft = KeyCode.J;
	public KeyCode moveRight = KeyCode.L;
	public KeyCode moveUp = KeyCode.U;
	public KeyCode moveDown = KeyCode.O;

	// Effects of rotation are only seen with stereo displays (two eyes rotate around their shared center of mass)
	public KeyCode rotateLeft = KeyCode.N;
	public KeyCode rotateRight = KeyCode.M;

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () 
	{
		if (Input.GetKey(moveForward))
		{
			transform.Translate( transform.forward * Time.deltaTime * movementScaler);
		}
		if (Input.GetKey(moveBackward))
		{
			transform.Translate(-transform.forward * Time.deltaTime * movementScaler);
		}
		if (Input.GetKey(moveLeft))
		{
			transform.Translate(-transform.right * Time.deltaTime * movementScaler);
		}
		if (Input.GetKey(moveRight))
		{
			transform.Translate( transform.right * Time.deltaTime * movementScaler);
		}
		if (Input.GetKey(moveUp))
        {
            transform.Translate(transform.up * Time.deltaTime * movementScaler);
        }
		else if (Input.GetKey(moveDown))
        {
            transform.Translate(-transform.up * Time.deltaTime * movementScaler);
        }

		transform.Rotate (transform.up * (Input.GetKey (rotateLeft ) ? -1 : 0) * Time.deltaTime * rotationScaler);
		transform.Rotate (transform.up * (Input.GetKey (rotateRight) ?  1 : 0) * Time.deltaTime * rotationScaler);
	}
	
}
