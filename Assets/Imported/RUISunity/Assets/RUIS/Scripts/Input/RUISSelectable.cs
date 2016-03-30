/*****************************************************************************

Content    :   The functionality for selectable objects, just add this to an object along with a collider to make it selectable
Authors    :   Tuukka Takala, Mikael Matveinen
Copyright  :   Copyright 2015 Tuukka Takala, Mikael Matveinen. All Rights reserved.
Licensing  :   RUIS is distributed under the LGPL Version 3 license.

******************************************************************************/

using UnityEngine;
using System.Collections.Generic;

[AddComponentMenu("RUIS/Input/RUISSelectable")]
public class RUISSelectable : MonoBehaviour {

	protected bool rigidbodyWasKinematic;
	public RUISWandSelector selector { get; protected set; }
	public bool isSelected { get { return selector != null; } }

	// TODO: return to public when implementation is done
	[Tooltip(  "This object's motion will be affected by physical forces during selection, instead of merely overwriting the Transform values. "
	         + "Results in more natural animation and interaction with other dynamic RigidBodies. Currently this option only has effect for "
	         + "RUISSelectableHingeJoint and RUISSelectableBallJoint.")]
	protected bool physicalSelection = false;

	protected Vector3 positionAtSelection;
	protected Quaternion rotationAtSelection;
	protected Vector3 selectorPositionAtSelection;
	protected Quaternion selectorRotationAtSelection;
	protected Vector3 rayEndToPositionAtSelection;
	protected float distanceFromSelectionRayOrigin;
    public float DistanceFromSelectionRayOrigin
    {
        get
        {
            return distanceFromSelectionRayOrigin;
        }
    }
	
	[Tooltip(  "Force this object to a specific distance from the selecting Wand. "
	         + "Only has effect when the selecting Wand's 'Position Grab' is set to 'Along Selection Ray'.")]
	public bool clampToCertainDistance = false;
	[Tooltip(  "The desired distance for the above 'Clamp To Certain Distance' option.")]
	public float distanceToClampTo = 1.0f;
	[Tooltip(  "Force this object to selection ray center upon selection, causing a jump in position. "
	         + "Only has effect when the selecting Wand's 'Position Grab' is NOT set to 'Snap To Wand'.")]
	public bool snapToRay = false;
    
	//for highlights
	[Tooltip(  "This material will be temporarily blended with this object's original material when the object is highlighted by a Wand.")]
	public Material highlightMaterial;
	[Tooltip(  "This material will be temporarily blended with this object's original material when the object is selected by a Wand.")]
    public Material selectionMaterial;
	
	[Tooltip(  "Inherit linear and angular momentum from the selecting Wand upon releasing selection. Enable if you want to be able to throw this gameObject.")]
    public bool maintainMomentumAfterRelease = true;
	
	[Tooltip(  "The CollisionDetectionMode of this object's RigidBody will be temporarily set to Continuous when the object is selected by a Wand. "
	         + "Then you can properly use this gameObject as a racket to intercept fast moving dynamic RigidBodies, if all involved Colliders are of "
	         + "primitive type")]
	public bool continuousCollisionDetectionWhenSelected = true;

	protected CollisionDetectionMode oldCollisionMode;
	protected bool switchToOldCollisionMode = false;
	protected bool switchToContinuousCollisionMode = false;

	protected Vector3 latestVelocity = Vector3.zero;
	protected Vector3 lastPosition = Vector3.zero;

//    private List<Vector3> velocityBuffer;
	
	protected KalmanFilter positionKalman;
	protected double[] measuredPos = {0, 0, 0};
	protected double[] pos = {0, 0, 0};
	protected float positionNoiseCovariance = 1000;
	Vector3 filteredVelocity = Vector3.zero;

    protected bool transformHasBeenUpdated = false;

    public void Awake()
    {
//        velocityBuffer = new List<Vector3>();
		if(GetComponent<Rigidbody>())
			oldCollisionMode = GetComponent<Rigidbody>().collisionDetectionMode;
			
		positionKalman = new KalmanFilter();
		positionKalman.initialize(3,3);
		positionKalman.skipIdenticalMeasurements = true;
    }

	// TODO: Ideally there would not be any calls to Update() and FixedUpdate(), so that CPU resources are spared
    public void Update()
    {
        if (transformHasBeenUpdated)
        {
            latestVelocity = (transform.position - lastPosition) 
								/ Mathf.Max(Time.deltaTime, Time.fixedDeltaTime);
            lastPosition = transform.position;

			if(isSelected)
			{
				updateVelocity(positionNoiseCovariance, Time.deltaTime);
			}
//            velocityBuffer.Add(latestVelocity);
//            LimitBufferSize(velocityBuffer, 2);

            transformHasBeenUpdated = false;
        }
    }

	protected void updateVelocity(float noiseCovariance, float deltaTime)
	{
		measuredPos[0] = latestVelocity.x;
		measuredPos[1] = latestVelocity.y;
		measuredPos[2] = latestVelocity.z;
		positionKalman.setR(deltaTime * noiseCovariance);
		positionKalman.predict();
		positionKalman.update(measuredPos);
		pos = positionKalman.getState();
		filteredVelocity.x = (float) pos[0];
		filteredVelocity.y = (float) pos[1];
		filteredVelocity.z = (float) pos[2];
	}

    public void FixedUpdate()
    {
		if(switchToContinuousCollisionMode)
		{
			oldCollisionMode = GetComponent<Rigidbody>().collisionDetectionMode;
			GetComponent<Rigidbody>().collisionDetectionMode = CollisionDetectionMode.Continuous;
			switchToContinuousCollisionMode = false;
		}
		if(switchToOldCollisionMode)
		{
			GetComponent<Rigidbody>().collisionDetectionMode = oldCollisionMode;
			switchToOldCollisionMode = false;
		}
        UpdateTransform(true);
        transformHasBeenUpdated = true;
    }

    public virtual void OnSelection(RUISWandSelector selector)
    {
        this.selector = selector;

		// "Reset" filtered velocity by temporarily using position noise covariance of 10
		updateVelocity(10, Time.deltaTime);

        positionAtSelection = transform.position;
        rotationAtSelection = transform.rotation;
        selectorPositionAtSelection = selector.transform.position;
        selectorRotationAtSelection = selector.transform.rotation;
		if(snapToRay)
			rayEndToPositionAtSelection = Vector3.zero;
		else
			rayEndToPositionAtSelection = transform.position - selector.selectionRayEnd;
		distanceFromSelectionRayOrigin = (selector.selectionRayEnd - selector.selectionRay.origin).magnitude;

        lastPosition = transform.position;

        if (GetComponent<Rigidbody>())
        {
			if(continuousCollisionDetectionWhenSelected)
			{
				switchToContinuousCollisionMode = true;
			}
            rigidbodyWasKinematic = GetComponent<Rigidbody>().isKinematic;
            GetComponent<Rigidbody>().isKinematic = true;
        }

        if (selectionMaterial != null)
            AddMaterialToEverything(selectionMaterial);

        UpdateTransform(false);
    }


    public virtual void OnSelectionEnd()
    {
        if (GetComponent<Rigidbody>())
        {
            GetComponent<Rigidbody>().isKinematic = rigidbodyWasKinematic;
			if(continuousCollisionDetectionWhenSelected)
			{
				switchToOldCollisionMode = true;
			}
        }

        if (maintainMomentumAfterRelease && GetComponent<Rigidbody>() && !GetComponent<Rigidbody>().isKinematic)
        {
//            rigidbody.AddForce(AverageBufferContent(velocityBuffer), ForceMode.VelocityChange);

			GetComponent<Rigidbody>().AddForce(filteredVelocity, ForceMode.VelocityChange);
			if(selector) // Put this if-clause here just in case because once received NullReferenceException
			{
				if(selector.transform.parent)
				{
					GetComponent<Rigidbody>().AddTorque(selector.transform.parent.TransformDirection(
										Mathf.Deg2Rad * selector.angularVelocity), ForceMode.VelocityChange);
				}
	            else
					GetComponent<Rigidbody>().AddTorque(Mathf.Deg2Rad * selector.angularVelocity, ForceMode.VelocityChange);
			}
        }

        if(selectionMaterial != null)
            RemoveMaterialFromEverything();

        this.selector = null;
    }

    public virtual void OnSelectionHighlight()
    {
        if(highlightMaterial != null)
            AddMaterialToEverything(highlightMaterial);
    }

    public virtual void OnSelectionHighlightEnd()
    {
        if(highlightMaterial != null)
            RemoveMaterialFromEverything();
    }

    protected virtual void UpdateTransform(bool safePhysics)
    {	
        if (!isSelected) return;

		Vector3 newManipulationPoint = getManipulationPoint();
		Quaternion newManipulationRotation = getManipulationRotation();

        if (GetComponent<Rigidbody>() && safePhysics)
        {
			if(selector.positionSelectionGrabType != RUISWandSelector.SelectionGrabType.DoNotGrab)
				GetComponent<Rigidbody>().MovePosition(newManipulationPoint);
			if(selector.rotationSelectionGrabType != RUISWandSelector.SelectionGrabType.DoNotGrab)
            	GetComponent<Rigidbody>().MoveRotation(newManipulationRotation);
        }
        else
		{
			if(selector.positionSelectionGrabType != RUISWandSelector.SelectionGrabType.DoNotGrab)
				transform.position = newManipulationPoint;
			if(selector.rotationSelectionGrabType != RUISWandSelector.SelectionGrabType.DoNotGrab)
            	transform.rotation = newManipulationRotation;
        }
    }


	protected void LimitBufferSize(List<Vector3> buffer, int maxSize)
    {
        while (buffer.Count >= maxSize)
        {
            buffer.RemoveAt(0);
        }
    }
    
    public virtual Vector3 getManipulationPoint()
    {
		switch (selector.positionSelectionGrabType)
		{
		case RUISWandSelector.SelectionGrabType.SnapToWand:
			return selector.transform.position;
		case RUISWandSelector.SelectionGrabType.RelativeToWand:
			Vector3 selectorPositionChange = selector.transform.position - selectorPositionAtSelection;
			return positionAtSelection + selectorPositionChange;
		case RUISWandSelector.SelectionGrabType.AlongSelectionRay:
			float clampDistance = distanceFromSelectionRayOrigin;
			if (clampToCertainDistance) 
				clampDistance = distanceToClampTo;
			Vector3 rayEndPosition = selector.selectionRay.origin + clampDistance * selector.selectionRay.direction;
			// Selected object is now attached to the end of a "pole" that is the selection ray. Lets add a small translation
			// that affects the rotation pivot and depends on the rotationSelectionGrabType (this is the reason for the below if-clauses)
			if(selector.rotationSelectionGrabType == RUISWandSelector.SelectionGrabType.SnapToWand)
				return rayEndPosition; // Object center jumps to the end of the ray
			else
			{
				if(selector.rotationSelectionGrabType == RUISWandSelector.SelectionGrabType.RelativeToWand)
				{	// Below ensures that there is no "jump" in position upon selection, and that the rotation pivot is the ray hit point
					return rayEndPosition + selector.transform.rotation * Quaternion.Inverse(selectorRotationAtSelection) * rayEndToPositionAtSelection;
				}
				else // rotationSelectionGrabType == AlongSelectionRay || rotationSelectionGrabType == DoNotGrab
					return rayEndPosition + rayEndToPositionAtSelection; // The last term ensures that there is no "jump" in position upon selection
			}	
		case RUISWandSelector.SelectionGrabType.DoNotGrab:
			return transform.position;
		}
		return transform.position;
    }
    
	
	public virtual Quaternion getManipulationRotation()
	{
		switch (selector.rotationSelectionGrabType)
		{
		case RUISWandSelector.SelectionGrabType.SnapToWand:
			return selector.transform.rotation;
		case RUISWandSelector.SelectionGrabType.RelativeToWand:
			Quaternion selectorRotationChange = Quaternion.Inverse(selectorRotationAtSelection) * rotationAtSelection;
			return selector.transform.rotation * selectorRotationChange;
		case RUISWandSelector.SelectionGrabType.AlongSelectionRay:
			return Quaternion.LookRotation(selector.selectionRay.direction);
		case RUISWandSelector.SelectionGrabType.DoNotGrab:
			return transform.rotation;
		}
		return transform.rotation;
	}

//    private Vector3 AverageBufferContent(List<Vector3> buffer)
//    {
//        if (buffer.Count == 0) return Vector3.zero;
//
//        Vector3 averagedContent = new Vector3();
//        foreach (Vector3 v in buffer)
//        {
//            averagedContent += v;
//        }
//
//        averagedContent = averagedContent / buffer.Count;
//
//        return averagedContent;
//    }

	protected void AddMaterial(Material m, Renderer r)
    {
        if (	m == null || r == null || r.GetType() == typeof(ParticleRenderer) 
			||  r.GetType() == typeof(ParticleSystemRenderer))
			return;

        Material[] newMaterials = new Material[r.materials.Length + 1];
        for (int i = 0; i < r.materials.Length; i++)
        {
            newMaterials[i] = r.materials[i];
        }

        newMaterials[newMaterials.Length - 1] = m;
        r.materials = newMaterials;
    }

	protected void RemoveMaterial(Renderer r)
    {
        if (	r == null || r.GetType() == typeof(ParticleRenderer) 
			||  r.GetType() == typeof(ParticleSystemRenderer) || r.materials.Length == 0)
			return;

        Material[] newMaterials = new Material[r.materials.Length - 1];
        for (int i = 0; i < newMaterials.Length; i++)
        {
            newMaterials[i] = r.materials[i];
        }
        r.materials = newMaterials;
    }

	protected void AddMaterialToEverything(Material m)
    {
        AddMaterial(m, GetComponent<Renderer>());
		
		foreach (Renderer childRenderer in GetComponentsInChildren<Renderer>())
        {
		  	AddMaterial(m, childRenderer);
			//childRenderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
			//childRenderer.receiveShadows = false;
        }
    }

	protected void RemoveMaterialFromEverything()
    {
        RemoveMaterial(GetComponent<Renderer>());

        foreach (Renderer childRenderer in GetComponentsInChildren<Renderer>())
        {
            RemoveMaterial(childRenderer);
        }
    }
}
