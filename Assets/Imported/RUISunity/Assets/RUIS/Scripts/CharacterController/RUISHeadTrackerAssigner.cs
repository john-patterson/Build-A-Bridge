/*****************************************************************************

Content    :	Leaves one head tracker enabled (that best matches RUISInputManager 
				settings) from a input list of GameObjects with RUISTracker script.
Authors    :	Tuukka Takala
Copyright  :	Copyright 2015 Tuukka Takala, Heikki Heiskanen. All Rights reserved.
Licensing  :	RUIS is distributed under the LGPL Version 3 license.

******************************************************************************/
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Ovr;

public class RUISHeadTrackerAssigner : MonoBehaviour {
	
	RUISInputManager inputManager;

	
	[Tooltip(  "You can disable this script if necessary and it won't be run upon Awake().")]
	public bool scriptEnabled = true;
	[Tooltip(  "The script will iterate through the below list of RUISTrackers, and leave the best candidate enabled, "
	         + "based on your RUISInputManager settings. If no suitable tracking devices are enabled in "
	         + "RUISInputManager, then the first item on the list is chosen. If multiple tracking devices are enabled, "
	         + "then the following preference order is used: Oculus DK2, PS Move, Kinect 2, Kinect 1, Razer Hydra.")]
	public List<RUISTracker> headTrackers = new List<RUISTracker>(6);
	[Tooltip(  "The chosen RUISTracker's child RUISCamera will draw to this display (if it doesn't have an attached "
	         + "RUISCamera already). NOTE: If this member is None, then the first RUISDisplay without an attached "
	         + "RUISCamera will be used.")]
	public RUISDisplay display;
	[Tooltip(  "If disabled, only one RUISHeadTrackerAssigner (the first one to be invoked) will be run.")]
	public bool allowMultipleAssigners = false;
	private bool applyKinectDriftCorrectionPreference = false;
	[Tooltip(  "If a RUISCharacterController script is found from this gameobject, and both Kinects are disabled "
	         + "and PS Move is enabled in RUISInputManager, and the chosen RUISTracker is a PS Move controller, "
	         + "then force the Character Pivot to that PS Move.")]
	public bool changePivotIfNoKinect = true;
	[Tooltip(  "If the chosen RUISTracker is Razer Hydra, this offset will be applied to its parent, "
	         + "and to the below 'Razer Wand Parent' gameobject. You can ue this to offset the Razer "
	         + "Hydra coordinate system.")]
	public Vector3 onlyRazerOffset = Vector3.zero;
	//	public Vector3 onlyMouseOffset = Vector3.zero;
	[Tooltip(  "If the chosen RUISTracker is Razer Hydra, this Transform will receive the above "
	         + "'Only Razer Offset' translation. Place the RazerHydraWand(s) under that gameobject, "
	         + "if the 'Only Razer Offset' is non-zero.")]
	public Transform razerWandParent;
	
	Ovr.HmdType ovrHmdVersion;
	
    void Awake()
    {
		if(!scriptEnabled)
			return;

		inputManager = FindObjectOfType(typeof(RUISInputManager)) as RUISInputManager;

		bool kinect2 = false;
		bool kinect = false;
		bool psmove = false;
		bool razer = false;
		bool oculusDK2 = false;

		bool isRiftConnected = false;
		
		//#if UNITY_EDITOR
		//if(UnityEditorInternal.InternalEditorUtility.HasPro())
		//#endif
		{
			try
			{
				// Find out if an Oculus HMD is connected
				if(OVRManager.display != null)
					isRiftConnected = OVRManager.display.isPresent;

				// Find out the Oculus HMD version
				if(OVRManager.capiHmd != null)
					ovrHmdVersion = OVRManager.capiHmd.GetDesc().Type;
			}
			catch(UnityException e)
			{
				Debug.LogError(e);
			}
		}

		if(inputManager)
		{
			if(isRiftConnected && (ovrHmdVersion == Ovr.HmdType.DK2 || ovrHmdVersion == Ovr.HmdType.Other)) 
				oculusDK2 = true;
			
			kinect2 = inputManager.enableKinect2;
			kinect  = inputManager.enableKinect;
			psmove  = inputManager.enablePSMove;
			razer   = inputManager.enableRazerHydra;
			
			int trackerCount = 0;
			RUISTracker closestMatch = null;
			int currentMatchScore = 0;

			RUISHeadTrackerAssigner[] assigners = FindObjectsOfType(typeof(RUISHeadTrackerAssigner)) as RUISHeadTrackerAssigner[];
			if(!allowMultipleAssigners && assigners.Length > 1)
			{
				Debug.LogError(  "Multiple active RUISHeadTrackerAssigner scripts found while 'Allow Multiple Assigners' is false: "
				               + "Disabling all headtrackers and their child objects that are listed in the RUISHeadTrackerAssigner "
				               + "component of '" + gameObject.name + "' object.");

				for(int i = 0; i < headTrackers.Capacity; ++i)
				{
					if(headTrackers[i] && headTrackers[i].gameObject.activeInHierarchy)
						headTrackers[i].gameObject.SetActive(false);
				}
				return;
			}

			foreach(RUISTracker trackerScript in headTrackers)
			{
				if(trackerScript && trackerScript.gameObject.activeInHierarchy)
				{
					++trackerCount;
					int foundTrackerScore = 0;
					
					// Give score to found head trackers
					if(oculusDK2 && trackerScript.headPositionInput == RUISTracker.HeadPositionSource.OculusDK2)
					{
						foundTrackerScore = 7;
						print (trackerScript);
					}
					else if(psmove && trackerScript.headPositionInput == RUISTracker.HeadPositionSource.PSMove)
					{
						foundTrackerScore = 6;
					}
					else if(	razer && trackerScript.isRazerBaseMobile // Legacy: Mobile Hydra Base (custom tracker) 
							&&	trackerScript.headPositionInput == RUISTracker.HeadPositionSource.RazerHydra
							&&	trackerScript.mobileRazerBase == RUISTracker.RazerHydraBase.InputTransform	)
					{
						foundTrackerScore = 5;
					}
					else if(kinect2 && trackerScript.headPositionInput == RUISTracker.HeadPositionSource.Kinect2)
					{
						foundTrackerScore = 4;
					}
					else if(	kinect && razer && trackerScript.isRazerBaseMobile // Legacy: Mobile Hydra Base (Kinect) 
							&&	trackerScript.headPositionInput == RUISTracker.HeadPositionSource.RazerHydra
							&&	trackerScript.mobileRazerBase == RUISTracker.RazerHydraBase.Kinect1			)
					{
						foundTrackerScore = 3;
					}
					else if(kinect && trackerScript.headPositionInput == RUISTracker.HeadPositionSource.Kinect1)
					{
						foundTrackerScore = 2;
					}
					else if(	razer && trackerScript.headPositionInput == RUISTracker.HeadPositionSource.RazerHydra // Plain ol' Razer Hydra
							&&	!trackerScript.isRazerBaseMobile															)
					{
						foundTrackerScore = 1;
					}
						
					// Assign new best head tracker candidate if it is better than the previously found
					if(currentMatchScore < foundTrackerScore)
					{
						closestMatch = trackerScript;
						currentMatchScore = foundTrackerScore;
					}
				}
			}
			
			if(trackerCount == 0 && Application.isEditor)
				Debug.LogError("No active GameObjects with RUISTracker script found from headTrackers list!");
			
			string positionTracker = "<None>";
			string logString = "";
			string names = "";
			RUISCamera ruisCamera = null;
			
			if(closestMatch == null)
			{
				// Disable all but the first active head tracker from the headTrackers list
				logString =   "Could not find a suitable head tracker with regard to "
							+ "enabled devices in RUISInputManager!";
				
				bool disabling = false;
				int leftEnabledIndex = -1;
				for(int i = 0; i < headTrackers.Capacity; ++i)
				{
					if(headTrackers[i] && headTrackers[i].gameObject.activeInHierarchy)
					{
						if(disabling)
						{
							if(names.Length > 0)
								names = names + ", ";
							names = names + headTrackers[i].gameObject.name;
							headTrackers[i].gameObject.SetActive(false);
						}
						else
						{
							leftEnabledIndex = i;
							closestMatch = headTrackers[leftEnabledIndex];
							positionTracker = headTrackers[leftEnabledIndex].gameObject.name;
							disabling = true;
						}
					}
				}
				if(leftEnabledIndex >= 0)
				{
					logString =   logString + " Choosing the first head tracker in the list. Using "
								+ positionTracker + " for tracking head position";
					if(names.Length > 0)
						logString = logString + ", and disabling the following: " + names;
					logString =   logString + ". This choice was made using a pre-selected list of "
								+ "head trackers.";
				
					ruisCamera = headTrackers[leftEnabledIndex].gameObject.GetComponentInChildren<RUISCamera>();
				}
				Debug.LogError(logString);
			}
			else
			{
				// Disable all but the closest match head tracker from the headTrackers list
				for(int i = 0; i < headTrackers.Capacity; ++i)
				{
					if(headTrackers[i] && headTrackers[i].gameObject.activeInHierarchy)
					{
						if(headTrackers[i] != closestMatch)
						{
							if(names.Length > 0)
								names = names + ", ";
							names = names + headTrackers[i].gameObject.name;
							headTrackers[i].gameObject.SetActive(false);
							
						}
						else
						{
							positionTracker = headTrackers[i].gameObject.name;
						}
					}
				}
				logString =   "Found the best head tracker with regard to enabled devices in "
							+ "RUISInputManager! Using " + positionTracker + " for tracking head position";
				if(names.Length > 0)
					logString = logString + ", and disabling the following: " + names;
				Debug.Log(logString + ". This choice was made using a pre-selected list of head trackers.");
				
				ruisCamera = closestMatch.gameObject.GetComponentInChildren<RUISCamera>();
				
				if(		changePivotIfNoKinect && psmove && !kinect && !kinect2
					&&  closestMatch.headPositionInput == RUISTracker.HeadPositionSource.PSMove )
				{
					RUISCharacterController characterController = gameObject.GetComponentInChildren<RUISCharacterController>();
					if(		characterController != null 
						&&  characterController.characterPivotType != RUISCharacterController.CharacterPivotType.MoveController )
					{
						characterController.characterPivotType = RUISCharacterController.CharacterPivotType.MoveController;
						characterController.moveControllerId = closestMatch.positionPSMoveID;
						Debug.Log(	  "PS Move enabled and Kinect disabled. Setting " + characterController.name 
									+ "'s Character Pivot as PS Move controller #" + closestMatch.positionPSMoveID
									+ ". PS Move position offset for this pivot is " + characterController.psmoveOffset);
					}
				}
			}
			
			
			if(ruisCamera)
			{
				if(display == null)
				{
					Debug.LogWarning( "No RUISDisplay attached to the RUISHeadTrackerAssigner script!");
					RUISDisplay[] displays = FindObjectsOfType(typeof(RUISDisplay)) as RUISDisplay[];
					for(int i = 0; i < displays.Length; ++i)
					{
						if(displays[i].linkedCamera == null)
						{
							Debug.LogWarning(  "Assigned RUISCamera component from the child of " + positionTracker
							                 + " to render on " + displays[i].gameObject.name + " because that "
							                 + "RUISDisplay component's RUISCamera field was empty.");
							displays[i].linkedCamera = ruisCamera;
							break;
						}
					}
				}
				else
				{
					if(display.linkedCamera == null)
					{
						Debug.Log(	  "Assigned RUISCamera component from the child of " + positionTracker
									+ " to render on " + display.gameObject.name							);
						display.linkedCamera = ruisCamera;
					}
					else
						Debug.LogWarning(  	  "RUISDisplay " + display.gameObject.name + " is already connected with a "
											+ "RUISCamera object! Leave the RUISCamera field empty in your RUISDisplay "
											+ "component if you want RUISHeadTrackerAssigner script to automatically "
											+ "assign a RUISCamera to your RUISDisplay.");
				}
			}
			else
			{
				if(closestMatch)
					Debug.LogError(  positionTracker + " did not have a child with RUISCamera component, "
								   + "and therefore it is not used to draw on any of the displays in "
								   + "DisplayManager.");
			}
			
			// If we are using Razer with a static base for head tracking, then apply onlyRazerOffset
			// on the parent objects of the Razer head tracker and the hand-held Razer
			if(		closestMatch != null && razer 
				&&	closestMatch.headPositionInput == RUISTracker.HeadPositionSource.RazerHydra
				&&	!closestMatch.isRazerBaseMobile													)
			{
				// The parent object of the Razer head tracker must not have RUISCharacterConroller,
				// because that script will modify the object's position
				if(		closestMatch.transform.parent != null 
					&&	closestMatch.transform.parent.GetComponent<RUISCharacterController>() == null
					&& (onlyRazerOffset.x != 0 || onlyRazerOffset.y != 0 || onlyRazerOffset.z != 0)  )
				{
					string razerWandOffsetInfo = "";
					closestMatch.transform.parent.localPosition += onlyRazerOffset;
					if(razerWandParent != null)
					{
						razerWandParent.localPosition += onlyRazerOffset;
						razerWandOffsetInfo =  " and " + razerWandParent.gameObject.name + " (parent of hand-held Razer "
											 + "Hydra)";
					}
					Debug.Log(  "Applying offset of " + onlyRazerOffset + " to " 
							   + closestMatch.transform.parent.gameObject.name + " (parent of Razer Hydra head tracker)"
							   + razerWandOffsetInfo + "." );
				}
			}
			
			// If no Razer, Kinect, or PS Move is available, then apply onlyMouseOffset
			// on the parent object of the head tracker that is left enabled
//			if(		closestMatch != null && !razer && !kinect && !psmove)
//			{
//				// The parent object of the Razer head tracker must not have RUISCharacterConroller,
//				// because that script will modify the object's position
//				if(		closestMatch.transform.parent != null 
//					&&	closestMatch.transform.parent.GetComponent<RUISCharacterController>() == null 
//					&& (onlyMouseOffset.x != 0 || onlyMouseOffset.y != 0 || onlyMouseOffset.z != 0)  )
//				{
//					closestMatch.transform.parent.localPosition += onlyMouseOffset;
//					Debug.Log(  "Applying offset of " + onlyMouseOffset + " to " 
//							   + closestMatch.transform.parent.gameObject.name + " (parent of assigned head tracker).");
//				}
//			}
				
			// *** TODO: Below is slightly hacky
			// Read inputConfig.xml to see if Kinect yaw drift correction for Oculus Rift should be enabled
			if(	   closestMatch != null
				&& closestMatch.useOculusRiftRotation && applyKinectDriftCorrectionPreference)
			{
				if(inputManager.kinectDriftCorrectionPreferred)
				{
					// Preference is to use Kinect for drift correction (if PS Move is not used for head tracking)
					switch(closestMatch.headPositionInput)
					{
						case RUISTracker.HeadPositionSource.Kinect1:
							if(!psmove && kinect)
							{
								closestMatch.externalDriftCorrection = true;
								closestMatch.compass = RUISTracker.CompassSource.Kinect1;
							}
							break;
						
						case RUISTracker.HeadPositionSource.RazerHydra:
							if(!psmove && kinect && razer)
							{
								if(closestMatch.isRazerBaseMobile)
								{
									closestMatch.externalDriftCorrection = true;
									closestMatch.compass = RUISTracker.CompassSource.Kinect1;
								}
							}
							break;
					}
				}
				else
				{
					// Preference is NOT to use Kinect for drift correction
					if(		closestMatch.headPositionInput == RUISTracker.HeadPositionSource.Kinect1
						&&  !psmove && kinect															)
						closestMatch.externalDriftCorrection = false;
				}
			}
		}
	}
}
