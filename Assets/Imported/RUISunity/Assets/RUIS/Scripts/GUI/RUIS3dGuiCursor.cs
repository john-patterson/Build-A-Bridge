/*****************************************************************************

Content    :   A manager for display configurations
Authors    :   Heikki Heiskanen, Tuukka Takala
Copyright  :   Copyright 2015 Tuukka Takala, Heikki Heiskanen. All Rights reserved.
Licensing  :   RUIS is distributed under the LGPL Version 3 license.

******************************************************************************/

using UnityEngine;
using System.Collections;

public class RUIS3dGuiCursor : MonoBehaviour {
	
	private Collider guiPlane;
	private GameObject markerObject;
	//private Camera guiCamera;
	private UICamera[] cameras;
	private RUISCamera ruisCamera;
	private RUISMenuNGUI menuScript;
	private RUISDisplayManager ruisDisplayManager;
	private GameObject instancedCursor;
	
	private Vector3 mouseInputCoordinates;
	private bool wasVisible = false;
	Quaternion wallOrientation = Quaternion.identity;
	private Vector4 translateColumn = Vector4.zero;
	private Vector3 trackerPosition = Vector3.zero;

	private Vector3 originalLocalScale = Vector3.one;

	void Start() 
	{
		menuScript = this.GetComponent<RUISMenuNGUI>();
		if(menuScript == null)
			Debug.LogError( "Did not find " + typeof(RUISMenuNGUI) + " script!");

		this.guiPlane = this.transform.Find ("NGUIControls/planeCollider").GetComponent<Collider>();
		if(this.guiPlane == null)
			Debug.LogError( "Did not find RUIS Menu collider object, onto which mouse selection ray is projected!" );

		if(menuScript.transform.parent == null)
			Debug.LogError(  "The parent of GameObject '" + menuScript.name 
			               + " is null and RUIS Menu will not function. Something is wrong with 'RUIS NGUI Menu' prefab or you "
			               + "are misusing the " + typeof(RUIS3dGuiCursor) + " script.");
		else if(menuScript.transform.parent.parent == null)
			Debug.LogError(  "The grand-parent of GameObject '" + menuScript.name 
			               + " is null and RUIS Menu will not function. Something is wrong with 'RUIS NGUI Menu' prefab or you "
			               + "are misusing the " + typeof(RUIS3dGuiCursor) + " script.");
		else
			ruisCamera = menuScript.transform.parent.parent.GetComponent<RUISCamera>();

		if(ruisCamera == null)
			Debug.LogError(  typeof(RUIS3dGuiCursor) + " script did not find "  + typeof(RUISCamera) + " from the parent of "
			               + menuScript.transform.name + "gameobject! RUIS Menu is unavailable.");
			               
		ruisDisplayManager =  FindObjectOfType(typeof(RUISDisplayManager)) as RUISDisplayManager;

		if(ruisDisplayManager == null) 
		{ 
			this.enabled = false;
			Debug.LogError("Could not find " + typeof(RUISDisplayManager) + " script, RUIS menu will not work!");
			return;
		}
		if(ruisDisplayManager.hideMouseOnPlay && menuScript.currentMenuState != RUISMenuNGUI.RUISMenuStates.calibration) 
			Cursor.visible = false;
		markerObject = ruisDisplayManager.menuCursorPrefab;

		if(markerObject)
			originalLocalScale = this.markerObject.transform.localScale;
	}
	
	void LateUpdate() 
	{
		// If we are in calibration scene, disable 3d cursor
		if(this.transform.parent == null) 
		{ 
			this.enabled = false;
			return;
		}

		cameras = menuScript.transform.parent.parent.GetComponentsInChildren<UICamera>();
		
		if(menuScript.menuIsVisible && !instancedCursor) 
		{
			instancedCursor = Instantiate(this.markerObject) as GameObject;
		}
		if(!menuScript.menuIsVisible && instancedCursor) 
		{
			Destroy (instancedCursor);
		}

		if(!menuScript.menuIsVisible)
			return;

		if(ruisCamera.associatedDisplay != null && ruisCamera.associatedDisplay.enableOculusRift) 
		{
			mouseInputCoordinates = ruisCamera.associatedDisplay.ConvertOculusScreenPoint(Input.mousePosition);
			if(instancedCursor && ruisCamera.rightCamera && ruisCamera.rightCamera.transform)
			{
				instancedCursor.transform.rotation = ruisCamera.rightCamera.transform.rotation;
			}
		}
		else 
		{
			mouseInputCoordinates = Input.mousePosition;
			instancedCursor.transform.rotation = ruisCamera.transform.rotation;	
		}

		// HACK for MecanimBlendedCharacter: Keep cursor visible size even if character is scaled
		if(menuScript.transform.parent)
			instancedCursor.transform.localScale = originalLocalScale * Mathf.Max (menuScript.transform.parent.lossyScale.x, menuScript.transform.parent.lossyScale.y);

		RaycastHit hit;	
		
		foreach(UICamera camera in cameras) 
		{
			/*
			if(!ruisCamera.associatedDisplay.isStereo
			   &&	(camera.gameObject.name == "CameraLeft"
			   ||	camera.gameObject.name == "CameraRight" 
			   || camera.gameObject.name == "guiCameraForRift"
			   ))  
			{
				camera.enabled = false;
				continue;
			}
			
			if(ruisCamera.associatedDisplay.isStereo 
				&& !ruisCamera.associatedDisplay.enableOculusRift  
				&& !(camera.gameObject.name == "CameraLeft"
				||	camera.gameObject.name == "CameraRight" 
				)) 
				{
					camera.enabled = false;
					continue;
				}
			if(ruisCamera.associatedDisplay.enableOculusRift 
				&& camera.gameObject.name != "guiCameraForRift")
				{
					camera.enabled = false;
					continue;
				} 
			*/
			
			Ray ray = camera.GetComponent<Camera>().ScreenPointToRay(mouseInputCoordinates);

			if(ruisCamera.associatedDisplay != null && ruisCamera.associatedDisplay.isObliqueFrustum)
			{
				wallOrientation = Quaternion.LookRotation(-ruisCamera.associatedDisplay.DisplayNormal, ruisCamera.associatedDisplay.DisplayUp);
				
				instancedCursor.transform.rotation = instancedCursor.transform.rotation * wallOrientation;

				//ray.origin = ruisCamera.associatedDisplay.displayCenterPosition;
				//translateColumn = ruisCamera.centerCamera.projectionMatrix.GetColumn(3);
				trackerPosition.Set(translateColumn.x, translateColumn.y, translateColumn.z);
				trackerPosition = ruisCamera.transform.position + ruisCamera.transform.rotation * ruisCamera.KeystoningHeadTrackerPosition;
				ray.origin += trackerPosition;
				ray.direction = wallOrientation * ray.direction;

			}

			if(Physics.Raycast(ray, out hit, Mathf.Infinity, LayerMask.GetMask(LayerMask.LayerToName(ruisDisplayManager.menuLayer))))
			{ 
				if(instancedCursor)
				{
					instancedCursor.transform.position = hit.point;

					if(!wasVisible)
						instancedCursor.SetActive(true);
					wasVisible = true;
				}
				Debug.DrawLine(ray.origin, hit.point);
				break;
			}
			else
			{
				if(wasVisible)
					instancedCursor.SetActive(false);
				wasVisible = false;
			}
		}
	}
}
