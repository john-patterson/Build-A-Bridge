/*****************************************************************************

Content    :   A custom class to start OVR plugin without displaying the Rift image
Authors    :   Heikki Heiskanen
Copyright  :   Copyright 2013 Tuukka Takala, Mikael Matveinen, Heikki Heiskanen. All Rights reserved.
Licensing  :   RUIS is distributed under the LGPL Version 3 license.

******************************************************************************/

using UnityEngine;
using System.Collections;
using Ovr;

using System;
using System.Runtime.InteropServices;
using System.Linq;
using System.Text.RegularExpressions;

public class RUISOVRManager : MonoBehaviour {

	// Code from OVRManager.cs
	private const string LibOVR = "OculusPlugin";
	[DllImport(LibOVR, CallingConvention = CallingConvention.Cdecl)]
	private static extern void OVR_GetHMD(ref IntPtr hmdPtr);
	[DllImport(LibOVR, CallingConvention = CallingConvention.Cdecl)]
	private static extern void OVR_Initialize();
	[DllImport(LibOVR, CallingConvention = CallingConvention.Cdecl)]
	private static extern void OVR_Destroy();
	public static Hmd ovrHmd;
	
	void Awake () 
	{
		if(OVRManager.capiHmd == null) 
		{
		 OVR_Initialize();
		}
		IntPtr hmdPtr = IntPtr.Zero;
		OVR_GetHMD(ref hmdPtr);
		ovrHmd = (hmdPtr != IntPtr.Zero) ? new Hmd(hmdPtr) : null;
	}
}
