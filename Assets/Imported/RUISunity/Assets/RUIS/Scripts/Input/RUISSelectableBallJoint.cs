/*****************************************************************************

Content    :   Implements selection behavior for RUISWands
Authors    :   Heikki Heiskanen, Tuukka Takala
Copyright  :   Copyright 2013 Tuukka Takala, Mikael Matveinen, Heikki Heiskanen. All Rights reserved.
Licensing  :   RUIS is distributed under the LGPL Version 3 license.

******************************************************************************/

using UnityEngine;
using System.Collections;

public class RUISSelectableBallJoint : RUISSelectable {
	
	public float springForce = 10;
	public bool resetTargetOnRelease;
	private ConfigurableJoint configurableJoint;
	private Vector3 jointAxisInGlobalCoordinates;
	private Quaternion initialRotation; 

	private JointDrive originalJointDriveX, originalJointDriveYZ;
	
	
	
	void Start() 
	{
		
		this.configurableJoint = this.gameObject.GetComponent(typeof(ConfigurableJoint)) as ConfigurableJoint;
		this.jointAxisInGlobalCoordinates = transform.TransformDirection(Vector3.Cross(this.configurableJoint.axis, this.configurableJoint.secondaryAxis)).normalized;
		this.initialRotation = this.configurableJoint.transform.localRotation;
		
//		Vector3 objectCenterProjectedOnPlane = MathUtil.ProjectPointOnPlane(jointAxisInGlobalCoordinates, this.configurableJoint.connectedAnchor, transform.position);
		
	}
	
	public override void OnSelection(RUISWandSelector selector)
	{
		this.selector = selector;
		// Transform information
		positionAtSelection = selector.selectionRayEnd;
		rotationAtSelection = transform.rotation;
		// Selector information
		selectorPositionAtSelection = selector.transform.position;
		selectorRotationAtSelection = selector.transform.rotation;
		distanceFromSelectionRayOrigin = (selector.selectionRayEnd - selector.selectionRay.origin).magnitude; // Dont remove this, needed in the inherited class
		
		
		this.originalJointDriveX = this.configurableJoint.angularXDrive;
		this.originalJointDriveYZ = this.configurableJoint.angularYZDrive;
		
		JointDrive jd = new JointDrive();
		jd.mode = JointDriveMode.Position;
		jd.positionSpring = springForce;
		jd.maximumForce = 999;
		
		this.configurableJoint.angularXDrive = jd;
		this.configurableJoint.angularYZDrive = jd;

		if (selectionMaterial != null)
			AddMaterialToEverything(selectionMaterial);
	}
	
	public override void OnSelectionEnd()
	{
		if (GetComponent<Rigidbody>())
		{
			GetComponent<Rigidbody>().isKinematic = rigidbodyWasKinematic;
			if(continuousCollisionDetectionWhenSelected)
			{
				switchToOldCollisionMode = true;
			}
		}
		if(selectionMaterial != null)
			RemoveMaterialFromEverything();
		
		
		this.selector = null;
		
		if(!physicalSelection) {
			transform.GetComponent<Rigidbody>().angularVelocity = Vector3.zero;
			transform.GetComponent<Rigidbody>().velocity = Vector3.zero;
		}
		
		if(!resetTargetOnRelease) 
		{
			this.configurableJoint.SetTargetRotationLocal (Quaternion.LookRotation(Vector3.down), this.initialRotation);
		}
		
		this.configurableJoint.angularXDrive = this.originalJointDriveX;
		this.configurableJoint.angularYZDrive = this.originalJointDriveYZ;
	}
	
	public new void FixedUpdate()
	{
		this.UpdateTransform(true);
	}
	
	public override Vector3 getManipulationPoint()
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
			if (clampToCertainDistance) clampDistance = distanceToClampTo;
			return selector.selectionRay.origin + clampDistance * selector.selectionRay.direction;
		case RUISWandSelector.SelectionGrabType.DoNotGrab:
			return transform.position;
		}
		return transform.position;
	}
	
	protected override void UpdateTransform(bool safePhysics)
	{
		Vector3 newManipulationPoint = getManipulationPoint();
		Vector3 projectedPoint = MathUtil.ProjectPointOnPlane(this.jointAxisInGlobalCoordinates, this.configurableJoint.connectedAnchor, newManipulationPoint);
//		Vector3 fromHingeToProjectedPoint = this.configurableJoint.connectedAnchor - projectedPoint;
		
		// https://gist.github.com/mstevenson/4958837	
		Quaternion targetRotation = Quaternion.FromToRotation(-this.jointAxisInGlobalCoordinates, (newManipulationPoint - this.configurableJoint.connectedAnchor).normalized);
		this.configurableJoint.SetTargetRotationLocal (targetRotation * Quaternion.LookRotation(Vector3.down), this.initialRotation);
		
		Debug.DrawLine(this.configurableJoint.connectedAnchor, projectedPoint, Color.blue);
		Debug.DrawLine(this.configurableJoint.connectedAnchor, newManipulationPoint, Color.red);
		DrawPlane(this.configurableJoint.connectedAnchor, jointAxisInGlobalCoordinates);
	}
	
	// For debug : http://answers.unity3d.com/questions/467458/how-to-debug-drawing-plane.html
	public void DrawPlane(Vector3 position , Vector3 normal) {
		Vector3 v3;
		if (normal.normalized != Vector3.forward)
			v3 = Vector3.Cross(normal, Vector3.forward).normalized * normal.magnitude;
		else
			v3 = Vector3.Cross(normal, Vector3.up).normalized * normal.magnitude;;
		Vector3 corner0 = position + v3;
		Vector3 corner2 = position - v3;
		
		Quaternion q = Quaternion.AngleAxis(90.0f, normal);
		v3 = q * v3;
		Vector3 corner1 = position + v3;
		Vector3 corner3 = position - v3;
		Debug.DrawLine(corner0, corner2, Color.green);
		Debug.DrawLine(corner1, corner3, Color.green);
		Debug.DrawLine(corner0, corner1, Color.green);
		Debug.DrawLine(corner1, corner2, Color.green);
		Debug.DrawLine(corner2, corner3, Color.green);
		Debug.DrawLine(corner3, corner0, Color.green);
		//Debug.DrawRay(position, normal, Color.red);
	}
	
}
