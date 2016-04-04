/*****************************************************************************

Content    :   Class for managing input devices of RUIS
Authors    :   Mikael Matveinen, Tuukka Takala
Copyright  :   Copyright 2013 Tuukka Takala, Mikael Matveinen. All Rights reserved.
Licensing  :   RUIS is distributed under the LGPL Version 3 license.

******************************************************************************/

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;
using System.Xml;

public class RUISInputManager : MonoBehaviour
{
    public enum RiftMagnetometer
    {
        Off = 0,
        AutomaticCalibration = 1
    };

    public TextAsset xmlSchema;
    public string filename = "inputConfig.xml";

    public bool loadFromTextFileInEditor = false;

    public bool enablePSMove = false;
    public string PSMoveIP = "127.0.0.1";
    public int PSMovePort = 7899;
    public bool connectToPSMoveOnStartup = true;
    public PSMoveWrapper psMoveWrapper;
    public int amountOfPSMoveControllers = 4;
    public bool enableMoveCalibrationDuringPlay = false;
	
	public bool delayedWandActivation = false;
	public float delayTime = 5;
	bool[] wandDelayed; // Following creates IndexOutOfRangeException: = new bool[4] {false, false, true, false};
	List<GameObject> disabledWands;
	public bool moveWand0 = false;
	public bool moveWand1 = false;
	public bool moveWand2 = false;
	public bool moveWand3 = false;
	
	public bool enableKinect = false;
    public int maxNumberOfKinectPlayers = 2;
    public bool kinectFloorDetection = true;
	public bool kinectDriftCorrectionPreferred = false;

	public bool enableKinect2 = false;
	public bool kinect2FloorDetection = true;
	
	public bool enableRazerHydra = false;
	private SixenseInput sixense = null;
	
	private RUISCoordinateSystem coordinateSystem = null;
    public OpenNI.SceneAnalyzer sceneAnalyzer = null;
	//private bool usingExistingSceneAnalyzer = false;
	
    public RUISPSMoveWand[] moveControllers;

   // public RiftMagnetometer riftMagnetometerMode = RiftMagnetometer.Off;

    public bool jumpGestureEnabled = false;

	RUISMenuNGUI ruisNGUIMenu;

    public void Awake()
    {
    	// Check if we are in calibration scene
		ruisNGUIMenu = FindObjectOfType(typeof(RUISMenuNGUI)) as RUISMenuNGUI;
    	
		if(ruisNGUIMenu != null) 
		{
			if(ruisNGUIMenu.currentMenuState == RUISMenuNGUI.RUISMenuStates.calibration) 
			{
				this.enablePSMove = ruisNGUIMenu.originalEnablePSMove;
				this.enableKinect = ruisNGUIMenu.originalEnableKinect;
				this.enableKinect2 = ruisNGUIMenu.originalEnableKinect2;
				this.jumpGestureEnabled = ruisNGUIMenu.originalEnableJumpGesture;
				this.enableRazerHydra = ruisNGUIMenu.originalEnableHydra;
				this.kinectDriftCorrectionPreferred = ruisNGUIMenu.originalKinectDriftCorrection;
				this.PSMoveIP = ruisNGUIMenu.originalPSMoveIP;
				this.PSMovePort = ruisNGUIMenu.originalPSMovePort;
			}
		}
    	
		wandDelayed = new bool[4] {moveWand0, moveWand1, moveWand2, moveWand3};
		disabledWands = new List<GameObject>();
		
        if (!Application.isEditor || loadFromTextFileInEditor)
        {
            if (!Import(filename))
            {
                Debug.LogError("Could not load input configuration file. Creating file based on current settings.");
                Export(filename);
            }
        }

        if (!enableKinect)
        {
            Debug.Log("Kinect is disabled from RUISInputManager.");
            GetComponentInChildren<RUISKinectDisabler>().KinectNotAvailable();
        }
        else
        {
            GetComponentInChildren<NIPlayerManagerCOMSelection>().m_MaxNumberOfPlayers = maxNumberOfKinectPlayers;
        }
		
        psMoveWrapper = GetComponentInChildren<PSMoveWrapper>();
        if (enablePSMove)
        {

            if (psMoveWrapper && connectToPSMoveOnStartup)
            {
                StartCoroutine("CheckForMoveConnection");

                psMoveWrapper.Connect(PSMoveIP, PSMovePort);

                psMoveWrapper.enableDefaultInGameCalibrate = enableMoveCalibrationDuringPlay;
				
				if(delayedWandActivation)
				{	
					string names = "";
					foreach (RUISPSMoveWand moveController in FindObjectsOfType(typeof(RUISPSMoveWand)) as RUISPSMoveWand[])
			        {
			            if(		moveController != null && moveController.controllerId < 4 
							&&	moveController.controllerId >= 0 && wandDelayed[moveController.controllerId] )
						{
							// Make sure that the found RUISPSMoveWand is not under InputManager->MoveControllers GameObject
							if(		moveController.gameObject.transform.parent == null
								||	moveController.gameObject.transform.parent.parent == null
								||	(	moveController.gameObject.transform.parent.GetComponent<RUISInputManager>() == null
									 &&	moveController.gameObject.transform.parent.parent.GetComponent<RUISInputManager>() == null))
							{
								moveController.gameObject.SetActive(false);
								disabledWands.Add(moveController.gameObject);
								if(names.Length > 0)
									names += ", ";
								names += moveController.gameObject.name;
							}
						}
			        }
					if(disabledWands.Count > 0)
					{
						Debug.Log(	  "DELAYED CONTROLLER ACTIVATION INITIALIZATION: Following objects are disabled: " 
									+ names + ". If their input devices are found, they will be re-activated in "
									+ delayTime + " seconds, as configured in RUISInputManager.");
						StartCoroutine("DelayedWandActivation");
					}
				}
            }
        }
        else
        {
			Debug.Log("PS Move is disabled from RUISInputManager.");
            //psMoveWrapper.gameObject.SetActiveRecursively(false);
        }
		
		
		sixense = FindObjectOfType(typeof(SixenseInput)) as SixenseInput;
		if(enableRazerHydra)
		{
			if(sixense == null)
				Debug.LogError(		"Could not connect with Razer Hydra! Your RUIS InputManager settings indicate "
								+ 	"that you want to use Razer Hydra, but this scene does not have a gameobject "
								+	"with SixenseInput script, which is required. Add SixenseInput prefab "
								+	"into the scene.");
			// IsBaseConnected() seems to crash Unity at least when called here
//			else if(!SixenseInput.IsBaseConnected(0)) // TODO: *** Apparently there can be multiple bases
//				Debug.LogError(		"Could not connect with Razer Hydra! Check the USB connection.");
		}
		else
		{
			if(sixense != null)
			{
				sixense.gameObject.SetActive(false);
				Debug.Log(	"Razer Hydra is disabled from RUISInputManager. Disabling object " + sixense.name
						  + " that has the SixenseInput script.");
			}
		}
		
        DisableUnneededMoveWands();
		
		DisableUnneededRazerHydraWands();
        
    }

    void Start()
    {
		coordinateSystem = FindObjectOfType(typeof(RUISCoordinateSystem)) as RUISCoordinateSystem;
		Quaternion dictionaryFloorNormal = coordinateSystem.RUISCalibrationResultsFloorPitchRotation[coordinateSystem.rootDevice];
		float dictionaryDistanceFromFloor = coordinateSystem.RUISCalibrationResultsDistanceFromFloor[coordinateSystem.rootDevice];
		Vector3 normalVector = dictionaryFloorNormal * Vector3.up;
		coordinateSystem.SetFloorNormal(normalVector, coordinateSystem.rootDevice);
		coordinateSystem.SetDistanceFromFloor(dictionaryDistanceFromFloor, coordinateSystem.rootDevice);
			
		//check whether the kinect camera is actually connected
        if (enableKinect)
        {
            OpenNISettingsManager settingsManager = FindObjectOfType(typeof(OpenNISettingsManager)) as OpenNISettingsManager;
            if (settingsManager.UserGenrator == null || !settingsManager.UserGenrator.Valid)
            {
                Debug.LogError("Could not start OpenNI! Check your Kinect connection.");
                GetComponentInChildren<RUISKinectDisabler>().KinectNotAvailable();
            }
        }

		if (!enableKinect2)
		{
			Debug.Log("Kinect 2 is disabled from RUISInputManager.");
			BodySourceManager kinect2Manager = GetComponentInChildren<BodySourceManager>();
			if(kinect2Manager)
				kinect2Manager.gameObject.SetActive(false);
		}

		if((enableKinect && kinectFloorDetection) || (enableKinect2 && kinect2FloorDetection))
			StartFloorDetection();
		
		if (enablePSMove)
        {
            RUISPSMoveWand[] controllers = GetComponentsInChildren<RUISPSMoveWand>();
            moveControllers = new RUISPSMoveWand[controllers.Length];
            foreach (RUISPSMoveWand controller in controllers)
            {
                moveControllers[controller.controllerId] = controller;
            }
        }
    }

    public void OnApplicationQuit()
    {
        if(enablePSMove && psMoveWrapper && psMoveWrapper.isConnected)
            psMoveWrapper.Disconnect(false);
    }
	
	// Doesn't seem to matter whether floor normal and point is constantly improved or just once in the beginning
//	void Update()
//	{
//		if(enableKinect && kinectFloorDetection && !enablePSMove)
//		{
//			updateKinectFloorData();
//		}
//	}
	
	public void StartFloorDetection()
	{
		bool startDetection = false;
		if (enableKinect)
		{
			kinectFloorDetection = true;
			
			if(sceneAnalyzer == null)
			{
				try
				{
					OpenNISettingsManager niSettings = FindObjectOfType(typeof(OpenNISettingsManager)) as OpenNISettingsManager;
					if(niSettings != null && niSettings.CurrentContext != null && niSettings.CurrentContext.BasicContext != null)
						sceneAnalyzer = new OpenNI.SceneAnalyzer(niSettings.CurrentContext.BasicContext);
					if(sceneAnalyzer != null)
						sceneAnalyzer.StartGenerating();
				}
				catch(System.Exception e)
				{
					Debug.LogError(e.Message);
				}
				Debug.Log ("OpenNI: Starting sceneAnalyzer for floor detection purposes.");
			}
			startDetection = true;
		}

		if(enableKinect2)
			startDetection = true;
		
		if(startDetection)
			StartCoroutine("attemptUpdatingFloorNormal");
		else
			Debug.LogError("Kinect is not enabled! You can enable it from RUIS InputManager.");
	}

    public bool Import(string filename)
    {
        return XmlImportExport.ImportInputManager(this, filename, xmlSchema);
    }

    public bool Export(string filename)
    {
        return XmlImportExport.ExportInputManager(this, filename);
    }

    private IEnumerator CheckForMoveConnection()
    {
        yield return new WaitForSeconds(5.0f);
        if (!psMoveWrapper.isConnected)
        {
            Debug.LogError("Could not connect to PS Move server at: " + PSMoveIP + ":" + PSMovePort);
        }
    }
	
    private IEnumerator DelayedWandActivation()
    {
        yield return new WaitForSeconds(delayTime);
        if (enablePSMove && psMoveWrapper.isConnected)
        {
			string activated = "";
			string leftDisabled = "";
	        foreach (GameObject moveWand in disabledWands)
	        {
				RUISPSMoveWand moveWandScript = moveWand.GetComponent<RUISPSMoveWand>();
				if(psMoveWrapper.moveConnected[moveWandScript.controllerId])
				{
					moveWand.SetActive(true);
					if(activated.Length > 0)
						activated += ", ";
					activated += moveWand.name;
				}
				else
				{
					if(leftDisabled.Length > 0)
						leftDisabled += ", ";
					leftDisabled += moveWand.name;
				}
	        }
			string report = "DELAYED CONTROLLER ACTIVATION REPORT: ";
			if(activated.Length > 0)
				report += "Following GameObjects were re-activated: " + activated + ". ";
			if(leftDisabled.Length > 0)
				report += "Following GameObjects will stay inactive because the controllers associated to them "
						  + "are not connected: " + leftDisabled + ".";
			Debug.Log(report);
		}
    }
	
    private IEnumerator attemptUpdatingFloorNormal()
    {
        yield return new WaitForSeconds(5.0f);
        
		if (!coordinateSystem)
	    {
	        Debug.LogError("Could not find coordinate system!");
	    }
    	else
    	{
			if(enableKinect && kinectFloorDetection )
			{
				if(sceneAnalyzer == null)
					Debug.LogError("Failed to access OpenNI sceneAnalyzer!");
				else 
				{
					updateKinectFloorData();
				}
			}
	
			if(enableKinect2 && kinect2FloorDetection)
			{
				updateKinect2FloorData();
			}
		}
	}
	
	private void updateKinectFloorData()
	{
        if(coordinateSystem)
        {
			if(coordinateSystem.rootDevice == RUISDevice.Kinect_1)
			{
				coordinateSystem.ResetFloorNormal(RUISDevice.Kinect_1);
				coordinateSystem.ResetDistanceFromFloor(RUISDevice.Kinect_1);
			}
	
			OpenNI.Plane3D floor;
			try
			{
				floor = sceneAnalyzer.Floor;
			}
			catch(System.Exception e)
			{
				Debug.LogError(e.TargetSite + ": Failed to get OpenNI.SceneAnalyzer.Floor.");
				return;
			}
	
	        Vector3 newFloorNormal = new Vector3(floor.Normal.X, floor.Normal.Y, floor.Normal.Z).normalized;
			Vector3 newFloorPosition = (new Vector3(floor.Point.X, floor.Point.Y, floor.Point.Z))*RUISCoordinateSystem.kinectToUnityScale; 
			//Vector3 newFloorPosition = coordinateSystem.ConvertKinectPosition(floor.Point);
	        
	        //Project the position of the kinect camera onto the floor
	        //http://en.wikipedia.org/wiki/Point_on_plane_closest_to_origin
	        //http://en.wikipedia.org/wiki/Plane_(geometry)
	        float d = newFloorNormal.x * newFloorPosition.x + newFloorNormal.y * newFloorPosition.y + newFloorNormal.z * newFloorPosition.z;
	        Vector3 closestFloorPointToKinect = new Vector3(newFloorNormal.x, newFloorNormal.y, newFloorNormal.z);
	        closestFloorPointToKinect = (closestFloorPointToKinect * d) / closestFloorPointToKinect.sqrMagnitude;
	
	        //floorPlane.transform.position = closestFloorPointToKinect;

			Quaternion kinectFloorRotator = Quaternion.FromToRotation(newFloorNormal, Vector3.up);

			//transform the point from Kinect's coordinate system rotation to Unity's rotation
			closestFloorPointToKinect = kinectFloorRotator * closestFloorPointToKinect;

			if(float.IsNaN(closestFloorPointToKinect.magnitude))
				closestFloorPointToKinect = Vector3.zero;
			if(newFloorNormal.sqrMagnitude < 0.1f)
				newFloorNormal = Vector3.up;

			if(coordinateSystem.rootDevice == RUISDevice.Kinect_1)
			{
				coordinateSystem.SetFloorNormal(newFloorNormal, RUISDevice.Kinect_1);
				coordinateSystem.SetDistanceFromFloor(closestFloorPointToKinect.magnitude, RUISDevice.Kinect_1);
			}
			Debug.Log ("Updated Kinect floor normal " + newFloorNormal + " and floor distance (" + closestFloorPointToKinect.magnitude + ")");
		}
	}

	private void updateKinect2FloorData()
	{
		Kinect2SourceManager kinect2SourceManager = FindObjectOfType(typeof(Kinect2SourceManager)) as Kinect2SourceManager;

		if(coordinateSystem != null && kinect2SourceManager != null)
		{
			if(coordinateSystem.rootDevice == RUISDevice.Kinect_2)
			{
				coordinateSystem.ResetFloorNormal(RUISDevice.Kinect_2);
				coordinateSystem.ResetDistanceFromFloor(RUISDevice.Kinect_2);
			}


			Windows.Kinect.Vector4 kinect2FloorPlane = kinect2SourceManager.GetFlootClipPlane();
			
			//print (kinect2FloorPlane.X + " " + kinect2FloorPlane.Y + " " + kinect2FloorPlane.Z  + " " +  kinect2FloorPlane.W);
			
			Vector3 kinect2FloorNormal = new Vector3(kinect2FloorPlane.X, kinect2FloorPlane.Y, kinect2FloorPlane.Z);
			kinect2FloorNormal.Normalize();
			
			float kinect2DistanceFromFloor = kinect2FloorPlane.W / Mathf.Sqrt(kinect2FloorNormal.sqrMagnitude);

			if(float.IsNaN(kinect2DistanceFromFloor))
				kinect2DistanceFromFloor = 0;
			if(float.IsNaN(kinect2FloorNormal.x) || kinect2FloorNormal.sqrMagnitude < 0.1f)
				kinect2FloorNormal = Vector3.up;

			if(coordinateSystem.rootDevice == RUISDevice.Kinect_2)
			{
				coordinateSystem.SetFloorNormal(kinect2FloorNormal, RUISDevice.Kinect_2);
				coordinateSystem.SetDistanceFromFloor(kinect2DistanceFromFloor,  RUISDevice.Kinect_2);
			}
			Debug.Log ("Updated Kinect 2 floor normal " + kinect2FloorNormal + " and floor distance (" + kinect2DistanceFromFloor + ")");
		}
	}
			
	private void DisableUnneededMoveWands()
    {
        List<RUISPSMoveWand> childWands = new List<RUISPSMoveWand>(GetComponentsInChildren<RUISPSMoveWand>());

        foreach (RUISPSMoveWand moveController in FindObjectsOfType(typeof(RUISPSMoveWand)) as RUISPSMoveWand[])
        {
            if (!childWands.Contains(moveController) && (!enablePSMove || !psMoveWrapper.isConnected || moveController.controllerId >= amountOfPSMoveControllers))
            {
                Debug.LogWarning("Disabling PS Move wand: " + moveController.name, moveController);
                moveController.enabled = false;
                RUISWandSelector wandSelector = moveController.GetComponent<RUISWandSelector>();
                if (wandSelector)
                {
                    wandSelector.enabled = false;
                    LineRenderer lineRenderer = wandSelector.GetComponent<LineRenderer>();
                    if (lineRenderer)
                    {
                        lineRenderer.enabled = false;
                    }
                }
				moveController.gameObject.SetActive(false);
            }
        }
    }
	
	private void DisableUnneededRazerHydraWands()
    {
        foreach (RUISRazerWand hydraWand in FindObjectsOfType(typeof(RUISRazerWand)) as RUISRazerWand[])
        {
            if (!enableRazerHydra)
            {
                Debug.LogWarning("Disabling Razer Hydra wand: " + hydraWand.name);
				hydraWand.gameObject.SetActive(false);
            }
        }
    }

    public RUISPSMoveWand GetMoveWand(int i)
    {
		if(psMoveWrapper && psMoveWrapper.isConnected)
		{
	        if (i < 0 || i >= amountOfPSMoveControllers || i >= moveControllers.Length)
	        {
				Debug.LogError("RUISPSMoveWand with ID " + i + " was not found in RUISInputManager");
	            return null;
	        }

        	return moveControllers[i];
		}
		else
			return null;
    }
}
