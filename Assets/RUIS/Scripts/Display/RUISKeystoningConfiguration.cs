/*****************************************************************************

Content    :   A class to manage keystoning configurations and calculate new ones when necessary
Authors    :   Mikael Matveinen
Copyright  :   Copyright 2013 Tuukka Takala, Mikael Matveinen. All Rights reserved.
Licensing  :   RUIS is distributed under the LGPL Version 3 license.

******************************************************************************/

using UnityEngine;
using System.Collections;
using System.Xml;

public class RUISKeystoningConfiguration : MonoBehaviour {
    RUISCamera ruisCamera;

    public RUISKeystoning.KeystoningCorners centerCameraCorners { get; set; }
    public RUISKeystoning.KeystoningCorners leftCameraCorners { get; set; }
    public RUISKeystoning.KeystoningCorners rightCameraCorners { get; set; }

    RUISKeystoning.KeystoningCorners currentlyDragging = null;
    Camera cameraUnderModification = null;
    int draggedCornerIndex = -1;

    [HideInInspector]
    public bool drawKeystoningGrid = false;

    public RUISKeystoning.KeystoningSpecification centerCameraKeystoningSpec { get { return centerSpec; } }
    private RUISKeystoning.KeystoningSpecification centerSpec;

    public RUISKeystoning.KeystoningSpecification leftCameraKeystoningSpec { get { return leftSpec; } }
    private RUISKeystoning.KeystoningSpecification leftSpec;

    public RUISKeystoning.KeystoningSpecification rightCameraKeystoningSpec { get { return rightSpec; } }
    private RUISKeystoning.KeystoningSpecification rightSpec;

    [HideInInspector]
    public bool isEditing = false;

	void Awake () {
        ruisCamera = GetComponent<RUISCamera>();

        centerCameraCorners = new RUISKeystoning.KeystoningCorners();
        leftCameraCorners = new RUISKeystoning.KeystoningCorners();
        rightCameraCorners = new RUISKeystoning.KeystoningCorners();

        centerSpec = new RUISKeystoning.KeystoningSpecification();
        leftSpec = new RUISKeystoning.KeystoningSpecification();
        rightSpec = new RUISKeystoning.KeystoningSpecification();
	}
	
	void Update () {
        if (!isEditing) return;

	    if(Input.GetMouseButtonDown(0)){
            //figure out if we should start dragging some corner
            Camera camUnderClick = ruisCamera.associatedDisplay.GetCameraForScreenPoint(Input.mousePosition);
            if (camUnderClick == ruisCamera.centerCamera)
            {
                currentlyDragging = centerCameraCorners;
                cameraUnderModification = camUnderClick;
                
            }
            else if (camUnderClick == ruisCamera.leftCamera)
            {
                currentlyDragging = leftCameraCorners;
                cameraUnderModification = camUnderClick;
            }
            else if (camUnderClick == ruisCamera.rightCamera)
            {
                currentlyDragging = rightCameraCorners;
                cameraUnderModification = camUnderClick;
            }

            if (currentlyDragging != null)
            {
                draggedCornerIndex = currentlyDragging.GetClosestCornerIndex(cameraUnderModification.ScreenToViewportPoint(Input.mousePosition));
            }

            if (draggedCornerIndex == -1)
            {
                ResetDrag();
            }
        }
        else if (Input.GetMouseButtonUp(0))
        {
            ResetDrag();

            //Optimize();
        }

        if (currentlyDragging != null)
        {
            Vector2 newPos = cameraUnderModification.ScreenToViewportPoint(Input.mousePosition);
            newPos.x = Mathf.Clamp01(newPos.x);
            newPos.y = Mathf.Clamp01(newPos.y);
            currentlyDragging[draggedCornerIndex] = newPos;
        }
		
		Optimize ();
	}

    public bool LoadFromXML(XmlDocument xmlDoc)
    {
        XmlImportExport.ImportKeystoningConfiguration(this, xmlDoc);
		
        Optimize();

        return true;
    }

    public bool SaveToXML(XmlElement displayXmlElement)
    {
        return XmlImportExport.ExportKeystoningConfiguration(this, displayXmlElement);
    }

    private void ResetDrag()
    {
        currentlyDragging = null;
        cameraUnderModification = null;
        draggedCornerIndex = -1;
    }

    private float Optimize()
    {
		
		/*ruisCamera.keystoningCamera.gameObject.SetActive(true);
        
		ruisCamera.keystoningCamera.transform.position = ruisCamera.KeystoningHeadTrackerPosition;*/
		float totalError = 0;
		totalError += RUISKeystoning.Optimize(ruisCamera.keystoningCamera, ruisCamera.CreateKeystoningObliqueFrustum(), ruisCamera.associatedDisplay, centerCameraCorners, ref centerSpec);
        totalError += RUISKeystoning.Optimize(ruisCamera.keystoningCamera, ruisCamera.CreateKeystoningObliqueFrustum(), ruisCamera.associatedDisplay, leftCameraCorners, ref leftSpec);
        totalError += RUISKeystoning.Optimize(ruisCamera.keystoningCamera, ruisCamera.CreateKeystoningObliqueFrustum(), ruisCamera.associatedDisplay, rightCameraCorners, ref rightSpec);
		
		//ruisCamera.keystoningCamera.gameObject.SetActive(false);

        //Debug.Log(totalError);

		return totalError;
    }

    public void StartEditing()
    {
        isEditing = true;
    }

    public void EndEditing()
    {
        Optimize();
        isEditing = false;
        drawKeystoningGrid = false;
        ResetDrag();
    }

    public void ResetConfiguration()
    {
        centerCameraCorners = new RUISKeystoning.KeystoningCorners();
        leftCameraCorners = new RUISKeystoning.KeystoningCorners();
        rightCameraCorners = new RUISKeystoning.KeystoningCorners();

        centerSpec = new RUISKeystoning.KeystoningSpecification();
        leftSpec = new RUISKeystoning.KeystoningSpecification();
        rightSpec = new RUISKeystoning.KeystoningSpecification();
    }
}
