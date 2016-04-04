/*****************************************************************************

Content    :   A manager for display configurations
Authors    :   Mikael Matveinen, Heikki Heiskanen, Tuukka Takala
Copyright  :   Copyright 2015 Tuukka Takala, Mikael Matveinen, Heikki Heiskanen. All Rights reserved.
Licensing  :   RUIS is distributed under the LGPL Version 3 license.

******************************************************************************/

using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using Ovr;

public class RUISDisplayManager : MonoBehaviour {
    public List<RUISDisplay> displays;
    public GameObject stereoCamera;
    public Camera monoCamera;
    public int totalResolutionX = 0;
    public int totalResolutionY = 0;
    public int totalRawResolutionX = 0;
    public int totalRawResolutionY = 0;

//    public bool allowResolutionDialog;

	public GameObject ruisMenuPrefab;
	public GameObject menuCursorPrefab;
	public int menuLayer = 0;
	public int guiDisplayChoice = 0;
	
	public float guiX;  
	public float guiY; 
	public float guiZ;
	
	public float guiScaleX = 1;
	public float guiScaleY = 1;
	public bool hideMouseOnPlay = false;

	private bool hasOculusDisplay = false;
	
    public class ScreenPoint
    {
        public Vector2 coordinates;
        public Camera camera;
    }

	void Start () 
	{

        CalculateTotalResolution();

        if (Application.isEditor)
        {
            UpdateResolutionsOnTheFly();
        }
		
		hasOculusDisplay = HasOculusDisplay();


        UpdateDisplays();

        DisableUnlinkedCameras();


        LoadDisplaysFromXML();

		// Second substitution because displays might have been updated via XML etc.
		hasOculusDisplay = HasOculusDisplay();

		// Disable OVRManager script if there are no Oculus Rift displays

		if(!hasOculusDisplay)
		{
			if(GetComponent<OVRManager>())
				GetComponent<OVRManager>().enabled = false;
			if(GetComponent<Camera>())
				GetComponent<Camera>().enabled = false;
		}
		else 
		{
			if(GetComponent<OVRManager>()) 
			{
				StartCoroutine(ForceOculusSettings(1.0f));
			}
		}

		InitRUISMenu(ruisMenuPrefab, guiDisplayChoice);
		
		
	}

	// TODO: Depends on OVR version
	IEnumerator ForceOculusSettings(float waitTime) 
	{
		yield return new WaitForSeconds(waitTime);
		OVRManager.DismissHSWDisplay();

		// Enforce Low Persistence settings
		// TODO: In the distant future it might be possible to have multiple Rifts in the same computer with different LP settings?
		RUISDisplay oculusDisplay = GetOculusRiftDisplay();
		if(oculusDisplay)
		{
			// HACK: Counter hack to OVRDisplays hack which forces LP on in the first frame
			for(int i=0; i<2; ++i)
			{
				if(oculusDisplay.oculusLowPersistence != getOculusLowPersistence())
					setOculusLowPersistence(oculusDisplay.oculusLowPersistence);
				yield return new WaitForSeconds(2);
			}
		}
	}

	// TODO: Depends on OVR version
	public bool getOculusLowPersistence()
	{
		uint caps = OVRManager.capiHmd.GetEnabledCaps();
		return (caps & (uint)HmdCaps.LowPersistence) != 0;
	}

	// TODO: Depends on OVR version
	public void setOculusLowPersistence(bool enabled)
	{
		if(OVRManager.capiHmd == null)
			return;

		uint caps = OVRManager.capiHmd.GetEnabledCaps();
		
		if(enabled)
			OVRManager.capiHmd.SetEnabledCaps(caps | (uint)HmdCaps.LowPersistence);
		else
			OVRManager.capiHmd.SetEnabledCaps(caps & ~(uint)HmdCaps.LowPersistence);

		return;
	}
	
    void Update()
    {
		// Toggle Oculus LowPersistence mode
//		if(Input.GetKeyDown(KeyCode.Backspace))
//		{
//			uint caps = OVRManager.capiHmd.GetEnabledCaps();
//			uint bitmask = caps & (uint)HmdCaps.LowPersistence;
//			if(bitmask == 0) // LowPersistence is now disabled
//				setOculusLowPersistence(true);
//			else
//			{
//				setOculusLowPersistence(false);
//			}
//		}

        if (Application.isEditor && (Screen.width != totalRawResolutionX || Screen.height != totalRawResolutionY))
        {
            UpdateResolutionsOnTheFly();
            UpdateDisplays();
        }
    }	

    public void UpdateDisplays()
    {
        CalculateTotalResolution();

        int currentResolutionX = 0;
        foreach (RUISDisplay display in displays)
        {
            display.SetupViewports(currentResolutionX, new Vector2(totalRawResolutionX, totalRawResolutionY));
            currentResolutionX += display.rawResolutionX;
        }

        if (displays.Count > 1 || (displays.Count == 1 /* && !allowResolutionDialog */))
        {
			if(!hasOculusDisplay) // TODO: if external oculus mode, and we have multiple displays, then execute below clause anyway
			{
				Screen.SetResolution(totalRawResolutionX, totalRawResolutionY, false);
			}
        }
    }

	public bool HasOculusDisplay()
	{
		foreach (RUISDisplay display in displays)
		{
			if(display.linkedCamera && display.enableOculusRift)
			{
				return true;
			}
		}
		return false;
	}

    public void CalculateTotalResolution()
    {
        totalResolutionX = 0;
        totalResolutionY = 0;
        totalRawResolutionX = 0;
        totalRawResolutionY = 0;

        foreach (RUISDisplay display in displays)
        {
            totalResolutionX += display.resolutionX;
            totalResolutionY = Mathf.Max(totalResolutionY, display.resolutionY);

            totalRawResolutionX += display.rawResolutionX;
            totalRawResolutionY = Mathf.Max(totalRawResolutionY, display.rawResolutionY);
        }
    }

	public Ray ScreenPointToRay(Vector2 screenPoint)
    {
        RUISDisplay display = GetDisplayForScreenPoint(screenPoint);


        if (display)
        {
            Camera camera = display.GetCameraForScreenPoint(screenPoint);
			
            if (camera)
            {   
				if(display.enableOculusRift)
				{
					screenPoint = display.ConvertOculusScreenPoint(screenPoint);
					return camera.ScreenPointToRay(screenPoint);
				}
				else
                	return camera.ScreenPointToRay(screenPoint);
            }
        }
         
        return new Ray(Vector3.zero, Vector3.zero);
    }

    public List<ScreenPoint> WorldPointToScreenPoints(Vector3 worldPoint)
    {
        List<ScreenPoint> screenPoints = new List<ScreenPoint>();

        foreach (RUISDisplay display in displays)
        {
            display.WorldPointToScreenPoints(worldPoint, ref screenPoints);
        }

        return screenPoints;
    }

    public RUISDisplay GetDisplayForScreenPoint(Vector2 screenPoint/*, ref Vector2 relativeScreenPoint*/)
    {
        //relativeScreenPoint = Vector2.zero;

         int currentResolutionX = 0;
         foreach (RUISDisplay display in displays)
         {

             if (currentResolutionX + display.rawResolutionX >= screenPoint.x)
             {
                 //relativeScreenPoint = new Vector2(screenPoint.x - currentResolutionX, totalRawResolutionY - screenPoint.y);
                 return display;
             }

             currentResolutionX += display.rawResolutionX;
         }

         return null;
    }
    /*
    public Camera GetCameraForScreenPoint(Vector2 screenPoint)
    {
        Vector2 relativeScreenPoint = Vector2.zero;
        RUISDisplay display = GetDisplayForScreenPoint(screenPoint);
        //Debug.Log(relativeScreenPoint);
        if (display)
            return display.GetCameraForScreenPoint(relativeScreenPoint, totalRawResolutionY);
        else
            return null;
    }*/

    private void UpdateResolutionsOnTheFly()
    {
        int trueWidth = Screen.width;
        int trueHeight = Screen.height;

        float widthScaler = (float)trueWidth / totalRawResolutionX;
        float heightScaler = (float)trueHeight / totalRawResolutionY;

        foreach (RUISDisplay display in displays)
        {
            display.resolutionX = (int)(display.resolutionX * widthScaler);
            display.resolutionY = (int)(display.resolutionY * heightScaler);
        }
    }

    private void DisableUnlinkedCameras()
    {
        RUISCamera[] allCameras = FindObjectsOfType(typeof(RUISCamera)) as RUISCamera[];

        foreach (RUISCamera ruisCamera in allCameras)
        {
            if (ruisCamera.associatedDisplay == null)
            {
                Debug.LogWarning("Disabling RUISCamera '" + ruisCamera.name + "' because it isn't linked into a RUISDisplay.");
                ruisCamera.gameObject.SetActive(false);
            }
        }
    }

    public void LoadDisplaysFromXML(bool refresh = false)
    {
        foreach (RUISDisplay display in displays)
        {
            display.LoadFromXML();
        }

        if (refresh)
        {
            UpdateDisplays();
        }
    }

    public void SaveDisplaysToXML()
    {
        foreach (RUISDisplay display in displays)
        {
            display.SaveToXML();
        }
    }

    public RUISDisplay GetOculusRiftDisplay()
    {
        foreach (RUISDisplay display in displays)
        {
            if (display.linkedCamera && display.enableOculusRift)
            {
                return display;
            }
        }

        return null;
    }
    
	private void InitRUISMenu(GameObject ruisMenuPrefab, int guiDisplayChoice)
	{
		if(ruisMenuPrefab == null)
			return;
		
		// HACK: displays is a list and accessing components by index might break if we modify the list in run-time
		if(displays.Count <= guiDisplayChoice)
		{
			Debug.LogError(  "displays.Count is too small: " + displays.Count + ", because guiDisplayChoice == " + guiDisplayChoice 
			               + ". Fix the guiDisplayChoice implementation so that it conforms to the displays variable (dynamic List<>).");
			return;
		}

		if(	   displays[guiDisplayChoice] == null
		    || displays[guiDisplayChoice].GetComponent<RUISDisplay>() == null
		   	|| displays[guiDisplayChoice].GetComponent<RUISDisplay>().linkedCamera == null )
		{
			return;
		}
		
		GameObject ruisMenu = Instantiate(ruisMenuPrefab) as GameObject;
		if(ruisMenu == null)
			return;
	
		if(menuLayer == -1)
			Debug.LogError(  "Could not find layer '" + LayerMask.LayerToName(menuLayer) + "', the RUIS menu cursor will not work without this layer! "
			               + "The prefab '" + ruisMenuPrefab.name + "' and its children should be on this layer.");

		if(!displays[guiDisplayChoice].GetComponent<RUISDisplay>().isStereo
		   && !displays[guiDisplayChoice].GetComponent<RUISDisplay>().enableOculusRift)
		{
			displays[guiDisplayChoice].GetComponent<RUISDisplay>().linkedCamera.transform.gameObject.AddComponent<UICamera>();
		}
		else 
		{
			if(displays[guiDisplayChoice].GetComponent<RUISDisplay>().linkedCamera.transform.Find("CameraRight"))
				displays[guiDisplayChoice].GetComponent<RUISDisplay>().linkedCamera.transform.Find("CameraRight").transform.gameObject.AddComponent<UICamera>();
			if(displays[guiDisplayChoice].GetComponent<RUISDisplay>().linkedCamera.transform.Find("CameraLeft"))
				displays[guiDisplayChoice].GetComponent<RUISDisplay>().linkedCamera.transform.Find("CameraLeft").transform.gameObject.AddComponent<UICamera>();
		}

		UICamera[] NGUIcameras = displays[guiDisplayChoice].GetComponent<RUISDisplay>().linkedCamera.GetComponentsInChildren<UICamera>();

		foreach(UICamera camera in NGUIcameras)
		{
			camera.eventReceiverMask = LayerMask.GetMask(LayerMask.LayerToName(menuLayer));
		}

		string primaryMenuParent   = "CenterEyeAnchor";
		string secondaryMenuParent = "CameraRight";
		string tertiaryMenuParent  = "CameraLeft";
		if(displays[guiDisplayChoice].GetComponent<RUISDisplay>().linkedCamera.transform.Find(primaryMenuParent))
			ruisMenu.transform.parent = displays[guiDisplayChoice].GetComponent<RUISDisplay>().linkedCamera.transform.Find(primaryMenuParent).transform;
		else 
		{
			if(displays[guiDisplayChoice].GetComponent<RUISDisplay>().linkedCamera.transform.Find(secondaryMenuParent))
				ruisMenu.transform.parent = displays[guiDisplayChoice].GetComponent<RUISDisplay>().linkedCamera.transform.Find(secondaryMenuParent).transform;
			else
			{
				if(displays[guiDisplayChoice].GetComponent<RUISDisplay>().linkedCamera.transform.Find(tertiaryMenuParent))
					ruisMenu.transform.parent = displays[guiDisplayChoice].GetComponent<RUISDisplay>().linkedCamera.transform.Find(tertiaryMenuParent).transform;
				else
				{
					Debug.LogError(  "Could not find any of the following gameObjects under " 
					               + displays[guiDisplayChoice].GetComponent<RUISDisplay>().linkedCamera.gameObject.name
					               + ": " + primaryMenuParent + ", " + secondaryMenuParent + ", " + tertiaryMenuParent + ". RUIS Menu will be parented "
					               + "directly under " + displays[guiDisplayChoice].GetComponent<RUISDisplay>().linkedCamera.gameObject.name + ".");
					ruisMenu.transform.parent = displays[guiDisplayChoice].GetComponent<RUISDisplay>().linkedCamera.transform;
				}
			}
		}
		
		ruisMenu.transform.localRotation = Quaternion.identity;
		ruisMenu.transform.localPosition = new Vector3(guiX,guiY,guiZ);

		if(ruisMenu.GetComponent<RUISMenuNGUI>())
			ruisMenu.GetComponent<RUISMenuNGUI>().Hide3DGUI();
	}
}
