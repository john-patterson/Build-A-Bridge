/*****************************************************************************

Content    :   Comprehensive virtual reality camera class
Authors    :   Mikael Matveinen, Tuukka Takala, Heikki Heiskanen
Copyright  :   Copyright 2015 Tuukka Takala, Mikael Matveinen. All Rights reserved.
Licensing  :   RUIS is distributed under the LGPL Version 3 license.

******************************************************************************/

using UnityEngine;
using System.Collections;
using System.Xml;

public class RUISCamera : MonoBehaviour {

    [HideInInspector]
    public bool isKeystoneCorrected;

    public Camera centerCamera; //the camera used for mono rendering
    public Camera leftCamera;
    public Camera rightCamera;
	public Camera keystoningCamera;

    [HideInInspector]
    public RUISDisplay associatedDisplay;

    private Rect normalizedScreenRect;
    private float aspectRatio;

    public float horizontalFOV = 60;
    public float verticalFOV = 40;

    public LayerMask cullingMask = 0xFFFFFF;

    public bool isStereo { get { return associatedDisplay.isStereo; } }

    private bool oldStereoValue;
    private RUISDisplay.StereoType oldStereoTypeValue;

    RUISKeystoningConfiguration keystoningConfiguration;
	
    public float near = 0.3f;
    public float far = 1000;
	
	private float frustumWidth = 1;
	private float frustumHeight = 1;
//	private float frustumTop = 1;
//	private float frustumBottom = 0;
//	private float topPlusBottom = 1;
	
	public Vector3 KeystoningHeadTrackerPosition {
		get
		{
			if(associatedDisplay && associatedDisplay.headTracker){
				return associatedDisplay.headTracker.defaultPosition;
			}
			
			return associatedDisplay.displayCenterPosition + associatedDisplay.DisplayNormal;
		}
	}

	public void Awake()
	{
        keystoningConfiguration = GetComponent<RUISKeystoningConfiguration>();
		if(!keystoningConfiguration)
			Debug.LogError( "GameObject " + name + " has " + typeof(RUISCamera) + " script, "
			               + "but is missing " + typeof(RUISKeystoningConfiguration) + " component!");
		
        centerCamera = GetComponent<Camera>();
		try
		{
			
			leftCamera = transform.FindChild("CameraLeft").GetComponent<Camera>();
			rightCamera = transform.FindChild("CameraRight").GetComponent<Camera>();
		}
		catch (System.Exception e)
		{
			Debug.LogError(  e.TargetSite + ": GameObject " + name + " has " + typeof(RUISCamera) + " script, "
			               + "but it is missing either CameraLeft or CameraRight child object.");
			gameObject.SetActive(false);
		}

		if(centerCamera == null)
		{
			Debug.LogError(  "GameObject " + name + " has " + typeof(RUISCamera) + " script, "
			               + "but is missing camera component!");
			
			gameObject.SetActive(false);
			return;
		}

		bool isDifferentCenterMask = false;
		bool isDifferentLeftMask = false;
		bool isDifferentRightMask = false;
		
		if (associatedDisplay != null && !associatedDisplay.enableOculusRift)
		{
			// TODO: Recreate RUISCamera for SDK 0.4.4
			try 
			{
				GetComponent<OVRCameraRig>().enabled = false;
			}
			catch (System.Exception e)
			{
				Debug.LogError(e.TargetSite + ": GameObject '" + name + "': " + typeof(OVRCameraRig) + " was not found.", this);
			}
		}
		else
		{
			//				Debug.LogWarning(  "GameObject " + name + " has " + typeof(RUISCamera) + " script, but no " + typeof(RUISDisplay) 
			//				                 + " was associated with it upon Awake(). Leaving OVR scripts on.");
			foreach (RUISKeystoningBorderDrawer drawer in GetComponentsInChildren<RUISKeystoningBorderDrawer>())
			{
				drawer.enabled = false;
			}
		}
		


		if(cullingMask != centerCamera.cullingMask)
			isDifferentCenterMask = true;

		try
		{
			
			if(cullingMask != leftCamera.cullingMask)
				isDifferentLeftMask = true;
			if(cullingMask != rightCamera.cullingMask)
				isDifferentRightMask = true;
		}
		catch (System.NullReferenceException e)
		{
			Debug.LogError(e.ToString(), this);
			Debug.LogError(  "GameObject " + name + " has " + typeof(RUISCamera) + " script, "
			               + "but it is missing either CameraLeft or CameraRight child object or their Camera components.");
			gameObject.SetActive(false);
			return;
		}

		if(isDifferentCenterMask || isDifferentLeftMask || isDifferentRightMask)
		{
			string differingMasksList = isDifferentCenterMask?"Camera":"";
			if(differingMasksList.Length == 0)
				differingMasksList += isDifferentLeftMask?"CameraLeft":"";
			else
				differingMasksList += isDifferentLeftMask?", CameraLeft":"";
			if(differingMasksList.Length == 0)
				differingMasksList += isDifferentRightMask?"CameraRight":"";
			else
				differingMasksList += isDifferentRightMask?", CameraRight":"";

			Debug.LogWarning(  "GameObject " + name + " has " + typeof(RUISCamera) + " script, whose "
			                 + "Culling Mask property has overwritten Culling Masks of its [" + differingMasksList + "]." );

		}
		
		centerCamera.cullingMask = cullingMask;
		leftCamera.cullingMask = cullingMask;
		rightCamera.cullingMask = cullingMask;
	}
	
	public void Start () {
		if (!associatedDisplay)
		{
			Debug.LogError(  "GameObject " + name + " has " + typeof(RUISCamera) + " script, "
			               + "but it is not associated to any display in DisplayManager, disabling " + name, this);
			gameObject.SetActive(false);
			return;
		}
		else
		{
			if(associatedDisplay.isObliqueFrustum)
			{
				centerCamera.fieldOfView = 170;
				if(leftCamera)
					leftCamera.fieldOfView = 170;
				if(rightCamera)
					rightCamera.fieldOfView = 170;
			}
		}

		UpdateStereo();
		UpdateStereoType();
		
		if (!leftCamera || !rightCamera)
		{
			Debug.LogError("Cameras not set properly in RUISCamera: " + name, this);
		}
		
		if(associatedDisplay)
		{
            if (associatedDisplay.enableOculusRift)
            {
                if (!associatedDisplay.isStereo)
                {
                    Debug.LogWarning("Oculus Rift enabled in RUISCamera, forcing stereo to display: " + associatedDisplay.name, associatedDisplay);
                    associatedDisplay.isStereo = true;
					associatedDisplay.stereoType = RUISDisplay.StereoType.SideBySide;
                }
                
				if (associatedDisplay.stereoType != RUISDisplay.StereoType.SideBySide)
				{
					Debug.LogWarning(  "Oculus Rift enabled in RUISCamera, switching to side-by-side stereo mode to display: " 
									 + associatedDisplay.name, associatedDisplay);
					associatedDisplay.stereoType = RUISDisplay.StereoType.SideBySide;
				}

                associatedDisplay.isObliqueFrustum = false;
                associatedDisplay.isKeystoneCorrected = false;
            }
            else
            {
                SetupCameraTransforms();
            }
            
            if(associatedDisplay.isObliqueFrustum)
            {
                if (associatedDisplay.headTracker)
                {
                    Vector3[] eyePositions = associatedDisplay.headTracker.GetEyePositions(associatedDisplay.eyeSeparation);
				    Vector3 camToDisplay = associatedDisplay.displayCenterPosition - eyePositions[0];
        		    float distanceFromPlane = Vector3.Dot(camToDisplay, associatedDisplay.DisplayNormal);
	                if(distanceFromPlane == 0)
						Debug.LogError(  "In " + associatedDisplay.headTracker.gameObject.name + " GameObject's "
						               + "RUISTracker script, you have set defaultPosition to " 
						               + "lie on the display plane of " 
						               + associatedDisplay.gameObject.name + ". The defaultPosition "
						               + "needs to be apart from the display!", associatedDisplay);
				}
				else
				{
					Debug.LogError("RUISTracker is none, you need to set it from the inspector!", associatedDisplay);
				}
			}
		}
	}
	
	public void Update () {
		
		if (oldStereoValue != associatedDisplay.isStereo)
		{
			UpdateStereo();
		}
		
		if (oldStereoTypeValue != associatedDisplay.stereoType)
		{
			UpdateStereoType();
		}
	}
	
	public void LateUpdate()
	{
		if(associatedDisplay.enableOculusRift){
			return;
		}
		
		centerCamera.ResetProjectionMatrix();
		leftCamera.ResetProjectionMatrix();
		rightCamera.ResetProjectionMatrix();
		
		SetupCameraTransforms();
		
		Matrix4x4[] projectionMatrices = GetProjectionMatricesWithoutKeystoning();
		
//		setCameraAspectAndFOV(centerCamera, projectionMatrices[0]);
//		setCameraAspectAndFOV(leftCamera,   projectionMatrices[1]);
//		setCameraAspectAndFOV(rightCamera,  projectionMatrices[2]);
		
		centerCamera.projectionMatrix = projectionMatrices[0];
		leftCamera.projectionMatrix = projectionMatrices[1];
		rightCamera.projectionMatrix = projectionMatrices[2];
		
		//		if(gameObject.name == "Left")
		//			print(centerCamera.projectionMatrix);
		
		if (associatedDisplay.isObliqueFrustum)
		{
			centerCamera.worldToCameraMatrix = Matrix4x4.TRS(new Vector3(-transform.position.x, 
			                                                             -transform.position.y, 
			                                                             transform.position.z), 
			                                                 Quaternion.identity, 
			                                                 new Vector3( transform.lossyScale.x, 
			            transform.lossyScale.y, 
			            -transform.lossyScale.z ));
			centerCamera.worldToCameraMatrix = Matrix4x4.TRS(Vector3.zero, transform.rotation, Vector3.one) 
				* centerCamera.worldToCameraMatrix;
			
			leftCamera.worldToCameraMatrix = centerCamera.worldToCameraMatrix;
			rightCamera.worldToCameraMatrix = centerCamera.worldToCameraMatrix;
		}
		
		//		if(gameObject.name == "Front")
		//			print(centerCamera.worldToCameraMatrix);
		
		ApplyKeystoneCorrection();
	}
	
	public Matrix4x4[] GetProjectionMatricesWithoutKeystoning()
	{
		if (associatedDisplay.isObliqueFrustum && associatedDisplay.headTracker)
		{
			Vector3[] eyePositions = associatedDisplay.headTracker.GetEyePositions(associatedDisplay.eyeSeparation);

			// FIXME: dangerous programming style: CreateProjectionMatrix() stores global variables frustumWidth, frustumHeight, and
			//  topPlusBottom which are then consumed by setCameraAspectAndFOV(). The following 6 statements must be in this 
			//  order
			Matrix4x4 centerProjectionMatrix = CreateProjectionMatrix(eyePositions[0]);
			setCameraAspectAndFOV(centerCamera);
			Matrix4x4 leftProjectionMatrix = CreateProjectionMatrix(eyePositions[1]);
			setCameraAspectAndFOV(leftCamera);
			Matrix4x4 rightProjectionMatrix = CreateProjectionMatrix(eyePositions[2]);
			setCameraAspectAndFOV(rightCamera);

			return new Matrix4x4[] { centerProjectionMatrix, leftProjectionMatrix, rightProjectionMatrix };

//			return new Matrix4x4[] { CreateProjectionMatrix(eyePositions[0]), 
//				CreateProjectionMatrix(eyePositions[1]),
//				CreateProjectionMatrix(eyePositions[2]) };
		}
		else
		{
			Matrix4x4 defaultMatrix = CreateDefaultFrustum();
			return new Matrix4x4[] { defaultMatrix, defaultMatrix, defaultMatrix };
		}
	}
	
	//http://csc.lsu.edu/~kooima/pdfs/gen-perspective.pdf
	//Generalized Perspective Projection
	//Robert Kooima
	public Matrix4x4 CreateProjectionMatrix(Vector3 trackerCoordinates)
	{
		Vector3 va = associatedDisplay.BottomLeftPosition - trackerCoordinates;
		Vector3 vb = associatedDisplay.BottomRightPosition - trackerCoordinates;
		Vector3 vc = associatedDisplay.TopLeftPosition - trackerCoordinates;
		Vector3 vr = associatedDisplay.DisplayRight;
		Vector3 vu = associatedDisplay.DisplayUp;
		Vector3 vn = associatedDisplay.DisplayNormal;
		
		float eyedistance = -(Vector3.Dot(va, vn));
		
		float left = (Vector3.Dot(vr, va) * near) / eyedistance;
		float right = (Vector3.Dot(vr, vb) * near) / eyedistance;
		float bottom = (Vector3.Dot(vu, va) * near) / eyedistance;
		float top = (Vector3.Dot(vu, vc) * near) / eyedistance;
		Matrix4x4 projectionMatrix = CreateFrustum(left, right, bottom, top, near, far);
		
		this.frustumWidth  = projectionMatrix[0, 0];
		this.frustumHeight = projectionMatrix[1, 1];
//		this.topPlusBottom = projectionMatrix[1, 2]*this.frustumHeight;
		
		//			if(gameObject.name == "Front")
		//				print(projectionMatrix);
		
		Matrix4x4 rotation = Matrix4x4.identity;
		rotation.SetRow(0,  vr);
		rotation.SetRow(1,  vu);
		rotation.SetRow(2,  vn);
		rotation[0, 2] = -rotation[0, 2];
		rotation[1, 2] = -rotation[1, 2];
		rotation[2, 2] = -rotation[2, 2];
		//rotation = rotation.inverse;
		
		Matrix4x4 translation = Matrix4x4.identity;
		//translation.SetColumn(3, -trackerCoordinates);
		translation[0, 3] = -trackerCoordinates.x;
		translation[1, 3] = -trackerCoordinates.y;
		translation[2, 3] =  trackerCoordinates.z;
		translation[3, 3] = 1;
		
		return projectionMatrix * rotation * translation;
	
	}
	
	public Matrix4x4 CreateDefaultFrustum()
	{
		float right = -Mathf.Tan(horizontalFOV / 2 * Mathf.Deg2Rad) * near;
		float left = -right;
		float top = -Mathf.Tan(verticalFOV / 2 * Mathf.Deg2Rad) * near;
		float bottom = -top;
		
		return CreateFrustum(right, left, top, bottom, near, far);
	}
	
	public Matrix4x4 CreateKeystoningObliqueFrustum(){
		return CreateProjectionMatrix (KeystoningHeadTrackerPosition);
	}
	
	private static Matrix4x4 CreateFrustum(float left, float right, float bottom, float top, float near, float far)
	{
		Matrix4x4 frustum = new Matrix4x4();
		
		frustum[0, 0] = 2 * near / (right - left);
		frustum[0, 2] = (right + left) / (right - left);
		frustum[1, 1] = 2 * near / (top - bottom);
		frustum[1, 2] = (top + bottom) / (top - bottom);
		frustum[2, 2] = -(far + near) / (far - near);
		frustum[2, 3] = -2 * far * near / (far - near);
		frustum[3, 2] = -1;

		return frustum;
	}
	
	// TUUKKA
	private void setCameraAspectAndFOV(Camera camera) //, Matrix4x4 projectionMatrix)
	{
//		if(projectionMatrix[0, 0] == 0)
//			frustumWidth = float.MaxValue;
//		else
//			frustumWidth = 2*camera.nearClipPlane/projectionMatrix[0, 0];
//		
//		if(projectionMatrix[1, 1] == 0)
//			frustumHeight = float.MaxValue;
//		else
//			frustumHeight = 2*camera.nearClipPlane/projectionMatrix[1, 1];
//		
//		topPlusBottom = projectionMatrix[1, 2]*frustumHeight;


		camera.aspect = frustumWidth/frustumHeight;
		
//		frustumTop = 0.5f*(topPlusBottom + frustumHeight);
//		frustumBottom = 0.5f*(topPlusBottom - frustumHeight);
//		camera.fieldOfView = 57.2957795f * Mathf.Max(90,  (Mathf.Atan(frustumTop/camera.nearClipPlane) 
//		                                                 - Mathf.Atan(frustumBottom/camera.nearClipPlane))*fovFactor);
		
	}

    private void ApplyKeystoneCorrection()
    {
		if(keystoningConfiguration)
		{
        //Debug.Log(keystoningConfiguration.centerCameraKeystoningSpec.GetMatrix());
	        //Debug.Log(centerCamera.projectionMatrix * keystoningConfiguration.centerCameraKeystoningSpec.GetMatrix());
	        centerCamera.projectionMatrix = keystoningConfiguration.centerCameraKeystoningSpec.GetMatrix() * centerCamera.projectionMatrix;
	        leftCamera.projectionMatrix = keystoningConfiguration.leftCameraKeystoningSpec.GetMatrix() * leftCamera.projectionMatrix;
	        rightCamera.projectionMatrix = keystoningConfiguration.rightCameraKeystoningSpec.GetMatrix() * rightCamera.projectionMatrix;
	        //Debug.Log(keystoningConfiguration.centerCameraKeystoningSpec.GetMatrix());
	        //leftCamera.projectionMatrix *= keystoningConfiguration.leftCameraKeystoningSpec.GetMatrix();
	        //rightCamera.projectionMatrix *= keystoningConfiguration.rightCameraKeystoningSpec.GetMatrix();
		}
    }

    virtual public void SetupCameraViewports(float relativeLeft, float relativeBottom, float relativeWidth, float relativeHeight, float aspectRatio)
    {
		
		if (associatedDisplay == null)
		{
			Debug.LogError("Associated Display was null", this);
		}
		else
		{
			if(associatedDisplay.enableOculusRift)
			{
			}
			else
			{
		        normalizedScreenRect = new Rect(relativeLeft, relativeBottom, relativeWidth, relativeHeight);
		        this.aspectRatio = aspectRatio;
		
				if(centerCamera)
				{
			        centerCamera.rect = normalizedScreenRect;
			        centerCamera.aspect = aspectRatio;
		        }
		
			
		        if (associatedDisplay.stereoType == RUISDisplay.StereoType.SideBySide)
		        {
		            leftCamera.rect = new Rect(relativeLeft, relativeBottom, relativeWidth / 2, relativeHeight);
		            rightCamera.rect = new Rect(relativeLeft + relativeWidth / 2, relativeBottom, relativeWidth / 2, relativeHeight);
		        }
		        else if (associatedDisplay.stereoType == RUISDisplay.StereoType.TopAndBottom)
		        {
		            leftCamera.rect = new Rect(relativeLeft, relativeBottom + relativeHeight / 2, relativeWidth, relativeHeight / 2);
		            rightCamera.rect = new Rect(relativeLeft, relativeBottom, relativeWidth, relativeHeight / 2);
		        }
		        else
		        {
		            leftCamera.rect = new Rect(relativeLeft, relativeBottom, relativeWidth, relativeHeight);
		            rightCamera.rect = new Rect(leftCamera.rect);
				}

				leftCamera.aspect = aspectRatio;
				rightCamera.aspect = aspectRatio;
			}
		}
		
	}
	
	private void SetupCameraTransforms()
	{
		float halfEyeSeparation = associatedDisplay.eyeSeparation / 2;
		leftCamera.transform.localPosition = new Vector3(-halfEyeSeparation, 0, 0);
		rightCamera.transform.localPosition = new Vector3(halfEyeSeparation, 0, 0);
		
		/*if (zeroParallaxDistance > 0)
        {
            float angle = Mathf.Acos(halfEyeSeparation / Mathf.Sqrt(Mathf.Pow(halfEyeSeparation, 2) + Mathf.Pow(zeroParallaxDistance, 2)));
            Vector3 rotation = new Vector3(0, angle, 0);
            rightCamera.transform.localRotation = Quaternion.Euler(-rotation);
            leftCamera.transform.localRotation = Quaternion.Euler(rotation);
        }*/
    }

    private void UpdateStereo()
    {
        if (associatedDisplay.isStereo)
        {
			/*if(!associatedDisplay.enableOculusRift)*/ centerCamera.enabled = false;
            leftCamera.enabled = true;
            rightCamera.enabled = true;
        }
        else
        {
			centerCamera.enabled = true;
            leftCamera.enabled = false;
            rightCamera.enabled = false;
        }

        oldStereoValue = associatedDisplay.isStereo;
    }

    private void UpdateStereoType()
    {
        SetupCameraViewports(normalizedScreenRect.xMin, normalizedScreenRect.yMin, normalizedScreenRect.width, normalizedScreenRect.height, aspectRatio);
        oldStereoTypeValue = associatedDisplay.stereoType;
    }

    public void LoadKeystoningFromXML(XmlDocument xmlDoc)
    {
        keystoningConfiguration.LoadFromXML(xmlDoc);
    }

    public void SaveKeystoningToXML(XmlElement displayXmlElement)
    {
        keystoningConfiguration.SaveToXML(displayXmlElement);
    }

	/*
    public void OnDrawGizmos()
    {
        if (!associatedDisplay) return;

        Color color = Gizmos.color;
        Gizmos.color = new Color(50, 50, 50);
        Gizmos.DrawLine(headTracker.transform.position, associatedDisplay.TopRightPosition);
        Gizmos.DrawLine(headTracker.transform.position, associatedDisplay.BottomRightPosition);
        Gizmos.DrawLine(headTracker.transform.position, associatedDisplay.BottomLeftPosition);
        Gizmos.DrawLine(headTracker.transform.position, associatedDisplay.TopLeftPosition);


        Matrix4x4 originalMatrix = Gizmos.matrix;
        Matrix4x4 rotationMatrix = new Matrix4x4();
        rotationMatrix.SetTRS(Vector3.zero, Quaternion.LookRotation(associatedDisplay.DisplayNormal, associatedDisplay.DisplayUp), Vector3.one);
        //Gizmos.matrix = rotationMatrix;

        Gizmos.DrawCube((associatedDisplay.TopRightPosition + associatedDisplay.BottomRightPosition) / 2, new Vector3(0.1f, associatedDisplay.TopRightPosition.y - associatedDisplay.BottomRightPosition.y, 0.1f));
        Gizmos.DrawCube((associatedDisplay.TopLeftPosition + associatedDisplay.BottomLeftPosition) / 2, new Vector3(0.1f, associatedDisplay.TopLeftPosition.y - associatedDisplay.BottomLeftPosition.y, 0.1f));
        Gizmos.DrawCube((associatedDisplay.TopRightPosition + associatedDisplay.TopLeftPosition) / 2, new Vector3(associatedDisplay.TopRightPosition.x - associatedDisplay.TopLeftPosition.x, 0.1f, 0.1f));
        Gizmos.DrawCube((associatedDisplay.BottomRightPosition + associatedDisplay.BottomLeftPosition) / 2, new Vector3(associatedDisplay.BottomRightPosition.x - associatedDisplay.BottomLeftPosition.x, 0.1f, 0.1f));
        
        Gizmos.color = color;
        Gizmos.matrix = originalMatrix;
    }
	 */

	/*
	 * Sets a new Culling Mask for center camera, Camera Left, and Camera Right.
	 */
	public void setCullingMask(LayerMask newCullingMask)
	{
		cullingMask = newCullingMask;
		centerCamera.cullingMask = newCullingMask;
		leftCamera.cullingMask = newCullingMask;
		rightCamera.cullingMask = newCullingMask;
	}
}
