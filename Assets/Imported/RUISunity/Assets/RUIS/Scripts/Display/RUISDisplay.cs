/*****************************************************************************

Content    :   A display used by RUIS in the display configurations, allows e.g. multiple displays & stereoscopy
Authors    :   Mikael Matveinen, Tuukka Takala
Copyright  :   Copyright 2015 Tuukka Takala, Mikael Matveinen. All Rights reserved.
Licensing  :   RUIS is distributed under the LGPL Version 3 license.

******************************************************************************/

using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Schema;

public class RUISDisplay : MonoBehaviour 
{
    public TextAsset displaySchema;
    public string xmlFilename = "defaultDisplay.xml";
    public bool loadFromFileInEditor = false;

    public enum StereoType
    {
        SideBySide,
        TopAndBottom
    }

    public int resolutionX;
    public int resolutionY;

    public bool useDoubleTheSpace = false;

    public int rawResolutionX
    {
        get
        {
            if (isStereo && stereoType == StereoType.SideBySide && useDoubleTheSpace)
            {
                return 2 * resolutionX;
            }
            else
            {
                return resolutionX;
            }
        }
    }

    public int rawResolutionY
    {
        get
        {
            if (isStereo && stereoType == StereoType.TopAndBottom && useDoubleTheSpace)
            {
                return 2 * resolutionY;
            }
            else
            {
                return resolutionY;
            }
        }
    }

	public bool enableOculusRift = false;
	public bool oculusLowPersistence = true;
	public bool oculusMirrorMode = true;

    public bool isStereo = false;
    public float eyeSeparation = 0.06f;
    public RUISCamera linkedCamera
    {
        get { return _linkedCamera; }
        set
        {
            _linkedCamera = value;
            _linkedCamera.associatedDisplay = this;
        }
    }
    public RUISCamera _linkedCamera;
    
    public StereoType stereoType;
    public bool isObliqueFrustum = false;
    public bool isKeystoneCorrected = false;

    public Vector3 displayCenterPosition = Vector3.zero;
    public Vector3 displayNormalInternal = Vector3.back;
    public Vector3 displayUpInternal = Vector3.up;
    public float width = 2;
    public float height = 1.5f;

    public RUISTracker headTracker;

    public Vector3 DisplayNormal
    {
        get
        {
            return displayNormalInternal.normalized;
        }
    }

    public Vector3 DisplayUp
    {
        get
        {
            return displayUpInternal.normalized;
        }
    }

    public Vector3 DisplayRight
    {
        get
        {
            return Vector3.Cross(DisplayNormal, DisplayUp).normalized;
        }
    }

    
    public Vector3 TopLeftPosition
    {
        get
        {
            return displayCenterPosition + DisplayUp * height / 2 - DisplayRight * width / 2;
        }
    }

    public Vector3 TopRightPosition
    {
        get
        {
            return displayCenterPosition + DisplayUp * height / 2 + DisplayRight * width / 2;
        }
    }

    public Vector3 BottomLeftPosition
    {
        get
        {
            return displayCenterPosition - DisplayUp * height / 2 - DisplayRight * width / 2;
        }
    }

    public Vector3 BottomRightPosition
    {
        get
        {
            return displayCenterPosition - DisplayUp * height / 2 + DisplayRight * width / 2;
        }
    }

    private float aspectRatio;

    public void Awake()
    {
        aspectRatio = resolutionX / resolutionY;
        
		if (!linkedCamera)
		{
			Debug.LogError("No camera attached to display: " + name, this);
		}
		else
		{
			linkedCamera.isKeystoneCorrected = isKeystoneCorrected;
			linkedCamera.associatedDisplay = this;
		}
 	}
	
	public void Start()
	{
		if(enableOculusRift && OVRManager.display != null)
			OVRManager.display.mirrorMode = oculusMirrorMode;
	}
	
	public void SetupViewports(int xCoordinate, Vector2 totalRawResolution)
    {
        float relativeWidth = rawResolutionX / totalRawResolution.x;
        float relativeHeight = rawResolutionY / totalRawResolution.y;
        
        float relativeLeft = xCoordinate / totalRawResolution.x;
        float relativeBottom = 1.0f - relativeHeight;

        if (linkedCamera)
        {
            linkedCamera.associatedDisplay = this;
//            if(enableOculusRift)
//				return;
			linkedCamera.SetupCameraViewports(relativeLeft, relativeBottom, relativeWidth, relativeHeight, aspectRatio);
        }
        else
        {
            Debug.LogWarning("Please set up a RUISCamera for display: " + name);
        }
    }

    public Camera GetCameraForScreenPoint(Vector2 screenPoint)
    {
        if (linkedCamera == null) return null;

        if (isStereo)
        {
            if (linkedCamera.leftCamera.pixelRect.Contains(screenPoint))
            {
                return linkedCamera.leftCamera;
            }
            else if (linkedCamera.rightCamera.pixelRect.Contains(screenPoint))
            {
                return linkedCamera.rightCamera;
            }
            else return null;
        }
        else
        {
            if(linkedCamera.centerCamera.pixelRect.Contains(screenPoint)){
                return linkedCamera.centerCamera;
            } 
            else return null;
        }
    }

    public void WorldPointToScreenPoints(Vector3 worldPoint, ref List<RUISDisplayManager.ScreenPoint> screenPoints)
    {
        if (isStereo)
        {
            RUISDisplayManager.ScreenPoint leftCameraPoint = new RUISDisplayManager.ScreenPoint();
            leftCameraPoint.camera = linkedCamera.leftCamera;
            leftCameraPoint.coordinates = leftCameraPoint.camera.WorldToScreenPoint(worldPoint);
            screenPoints.Add(leftCameraPoint);

            RUISDisplayManager.ScreenPoint rightCameraPoint = new RUISDisplayManager.ScreenPoint();
            rightCameraPoint.camera = linkedCamera.rightCamera;
            rightCameraPoint.coordinates = rightCameraPoint.camera.WorldToScreenPoint(worldPoint);
            screenPoints.Add(rightCameraPoint);
        }
        else
        {
            RUISDisplayManager.ScreenPoint screenPoint = new RUISDisplayManager.ScreenPoint();
            screenPoint.camera = linkedCamera.centerCamera;
            screenPoint.coordinates = screenPoint.camera.WorldToScreenPoint(worldPoint);
            screenPoints.Add(screenPoint);
        }
    }

    public bool LoadFromXML()
    {
        return XmlImportExport.ImportDisplay(this, xmlFilename, displaySchema, loadFromFileInEditor);
    }

    public bool SaveToXML()
    {
        return XmlImportExport.ExportDisplay(this, xmlFilename);
    }

    void OnDrawGizmos()
    {
		if(isObliqueFrustum)
		{
	        Color color = Gizmos.color;
	        Gizmos.color = new Color(128, 128, 128);
	        Gizmos.DrawLine(TopLeftPosition, TopRightPosition);
	        Gizmos.DrawLine(TopRightPosition, BottomRightPosition);
	        Gizmos.DrawLine(BottomRightPosition, BottomLeftPosition);
	        Gizmos.DrawLine(BottomLeftPosition, TopLeftPosition);
	        Gizmos.color = color;
		}
    }

    void OnDrawGizmosSelected()
	{
		if(isObliqueFrustum)
		{
	        Color color = Gizmos.color;
	        Gizmos.color = Color.green;
	        Gizmos.DrawLine(TopLeftPosition, TopRightPosition);
	        Gizmos.DrawLine(TopRightPosition, BottomRightPosition);
	        Gizmos.DrawLine(BottomRightPosition, BottomLeftPosition);
	        Gizmos.DrawLine(BottomLeftPosition, TopLeftPosition);

	        Vector3 horizontalScale = 0.1f * (TopRightPosition - TopLeftPosition);
	        Vector3 verticalScale = 0.1f * (BottomRightPosition - TopRightPosition);
	        Gizmos.color = Color.yellow;
	        Gizmos.DrawLine(TopLeftPosition + horizontalScale + verticalScale, TopRightPosition - horizontalScale + verticalScale);
	        Gizmos.DrawLine(TopRightPosition - horizontalScale + verticalScale, BottomRightPosition - horizontalScale - verticalScale);
	        Gizmos.DrawLine(BottomRightPosition - horizontalScale - verticalScale, BottomLeftPosition + horizontalScale - verticalScale);
	        Gizmos.DrawLine(BottomLeftPosition + horizontalScale - verticalScale, TopLeftPosition + horizontalScale + verticalScale);

	        Gizmos.color = Color.blue;
	        Gizmos.DrawLine(displayCenterPosition, displayCenterPosition + DisplayNormal/2);
	        Gizmos.color = Color.green;
	        Gizmos.DrawLine(displayCenterPosition, displayCenterPosition + DisplayUp/2);
	        Gizmos.color = Color.red;
	        Gizmos.DrawLine(displayCenterPosition, displayCenterPosition + DisplayRight/2);

			Gizmos.color = color;
		}
    }
    
	public Vector2 ConvertOculusScreenPoint(Vector2 screenPoint) {
		Vector2 newScreenpoint = Vector2.zero;
		newScreenpoint.x = 1.25f*(1920f/Mathf.Max((float)this.rawResolutionX, 1)) * (screenPoint.x );//% (0.5f*((float)this.rawResolutionX))));
		newScreenpoint.y = 1.25f*(1080f/Mathf.Max((float)this.rawResolutionY, 1)) * screenPoint.y;
		return newScreenpoint;
	}
	
}
