/*****************************************************************************

Content    :   A Script to handle controlling a rigidbody character using Kinect and some traditional input method
Authors    :   Mikael Matveinen, Tuukka Takala
Copyright  :   Copyright 2015 Tuukka Takala, Mikael Matveinen. All Rights reserved.
Licensing  :   RUIS is distributed under the LGPL Version 3 license.

******************************************************************************/

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Ovr;

public class RUISCharacterController : MonoBehaviour
{
    public enum CharacterPivotType
    {
        KinectHead,
        KinectTorso,
        MoveController
    }

    public CharacterPivotType characterPivotType = CharacterPivotType.KinectTorso;

	public bool useOculusPositionalTracking = false;
	public bool headRotatesBody = true;
	public bool headPointsWalkingDirection = false;

    public int kinectPlayerId;
	public int bodyTrackingDeviceID;
    public int moveControllerId;
    public Transform characterPivot;
	public Vector3 psmoveOffset = Vector3.zero;

    public bool ignorePitchAndRoll = true;

    private RUISInputManager inputManager;
    private RUISSkeletonManager skeletonManager;

    public LayerMask groundLayers;
    public float groundedErrorTweaker = 0.05f;

    public bool grounded { get; private set; }
    public bool colliding { get; private set; }
	private bool wasColliding = false;
	
	Vector3 raycastPosition;
	float distanceToRaycast;
	
	RaycastHit hitInfo;
	private bool tempGrounded = false;
	private bool rayIntersected = false;
	
	private RUISCharacterStabilizingCollider stabilizingCollider;
	RUISCoordinateSystem coordinateSystem;
	
	public bool dynamicFriction = true;
	public PhysicMaterial dynamicMaterial;
	private PhysicMaterial originalMaterial;
	private Collider colliderComponent;
    public float lastJumpTime { get; set; }
	
	public bool feetAlsoAffectGrounding = true;
	List<Transform> bodyParts = new List<Transform>(2);
	RUISSkeletonController skeletonController;

	private bool kinectAndMecanimCombinerExists = false;
	private bool combinerChildrenInstantiated = false;
	
	Vector3 previousPosition;

	Ovr.HmdType ovrHmdVersion = Ovr.HmdType.None;
	
    void Awake()
    {
        inputManager = FindObjectOfType(typeof(RUISInputManager)) as RUISInputManager;
        skeletonManager = FindObjectOfType(typeof(RUISSkeletonManager)) as RUISSkeletonManager;
        stabilizingCollider = GetComponentInChildren<RUISCharacterStabilizingCollider>();
		lastJumpTime = 0;
		skeletonController = gameObject.GetComponentInChildren<RUISSkeletonController>();
    }
	
    void Start()
    {
		colliding = false;
		grounded = false;
	
		// Second substitution, because RUISKinectAndMecanimCombiner might have already erased the original one and re-created it
		skeletonController = gameObject.GetComponentInChildren<RUISSkeletonController>();
		if(skeletonController)
		{
			bodyParts.Add(skeletonController.leftFoot);
			bodyParts.Add(skeletonController.rightFoot);
			kinectPlayerId = skeletonController.playerId;
			bodyTrackingDeviceID = skeletonController.bodyTrackingDeviceID;
		}
		else
		{
			Debug.LogError(  "RUISCharacterController script in game object '" + gameObject.name 
			               + "' did not find RUISSkeletonController component from it's child objects!");
		}
		
		coordinateSystem = FindObjectOfType(typeof(RUISCoordinateSystem)) as RUISCoordinateSystem;

		if(stabilizingCollider)
		{	
			colliderComponent = stabilizingCollider.gameObject.GetComponent<Collider>();
			if(colliderComponent)
			{
				if(    characterPivotType == CharacterPivotType.KinectHead
				    || characterPivotType == CharacterPivotType.KinectTorso)
				{
					if(coordinateSystem && inputManager.enableKinect && !coordinateSystem.setKinectOriginToFloor)
						Debug.LogWarning("It is best to enable 'setKinectOriginToFloor' from RUISCoordinateSystem " +
						                 "when using Kinect and RUISCharacterController script.");
				}

				if(colliderComponent.material)
					originalMaterial = colliderComponent.material;
				else
				{
					colliderComponent.material = new PhysicMaterial();
					originalMaterial = colliderComponent.material;
				}
				
				if(dynamicMaterial == null)
				{
					dynamicMaterial = new PhysicMaterial();
					
					dynamicMaterial.dynamicFriction = 0;
					dynamicMaterial.staticFriction = 0;
					dynamicMaterial.frictionCombine = PhysicMaterialCombine.Minimum;
					
					if(colliderComponent.material)
					{
						dynamicMaterial.bounceCombine = originalMaterial.bounceCombine;
						dynamicMaterial.bounciness = originalMaterial.bounciness;
//						dynamicMaterial.staticFriction2 = originalMaterial.staticFriction2;
//						dynamicMaterial.dynamicFriction2 = originalMaterial.dynamicFriction2;
//						dynamicMaterial.frictionDirection2 = originalMaterial.frictionDirection2;
					}
				}
			}
		}
		if((   characterPivotType == CharacterPivotType.KinectHead
		    || characterPivotType == CharacterPivotType.KinectTorso)
		    && (skeletonController && skeletonController.playerId != kinectPlayerId))
			Debug.LogError(  "The 'Kinect Player Id' variable in RUISCharacterController script in gameObject '" + gameObject.name
			               + "is different from the Kinect Player Id of the RUISSkeletonController script (located in child "
			               + "object '" + skeletonController.gameObject.name + "). Make sure that these two values are "
			               + "the same.");

		//#if UNITY_EDITOR
		//if(UnityEditorInternal.InternalEditorUtility.HasPro())
		//#endif
		{
			try
			{
				bool isRiftConnected = false;
				if(OVRManager.display != null)
					isRiftConnected = OVRManager.display.isPresent;
				if(OVRManager.capiHmd != null)
					ovrHmdVersion = OVRManager.capiHmd.GetDesc().Type;

				if(useOculusPositionalTracking && ovrHmdVersion == Ovr.HmdType.DK1 || ovrHmdVersion == Ovr.HmdType.DKHD || ovrHmdVersion == Ovr.HmdType.None)
				{
					Debug.LogError("Can't use Oculus Rift's tracked position as a pivot with Oculus Rift " + ovrHmdVersion);
					useOculusPositionalTracking = false;
				}
				
				if(useOculusPositionalTracking && !isRiftConnected)
				{
					Debug.LogError("Can't use Oculus Rift's tracked position as a pivot because Oculus Rift is not connected.");
					useOculusPositionalTracking = false;
				}

			}
			catch(UnityException e)
			{
				useOculusPositionalTracking = false;
				Debug.LogError(e);
			}
		}
		
		if(GetComponentInChildren<RUISKinectAndMecanimCombiner>())
			kinectAndMecanimCombinerExists = true;

		previousPosition = transform.position;
	}
	
    void Update()
	{
		if(!combinerChildrenInstantiated)
			skeletonController = getSkeletonController();

		// Check whether character collider (aka stabilizingCollider) is grounded
		raycastPosition = (stabilizingCollider? stabilizingCollider.transform.position : transform.position );

        distanceToRaycast = (stabilizingCollider ? stabilizingCollider.colliderHeight / 2 : 1.5f);
        distanceToRaycast += groundedErrorTweaker;
		distanceToRaycast = Mathf.Max(distanceToRaycast * transform.lossyScale.y, float.Epsilon);

        tempGrounded = Physics.Raycast(raycastPosition, -transform.up, distanceToRaycast, groundLayers.value);
		
		// Check if feet are grounded
		if(!tempGrounded && feetAlsoAffectGrounding)
		{
			foreach(Transform bodyPart in bodyParts)
			{
	            if(bodyPart && bodyPart.GetComponent<Collider>())
	            {
					raycastPosition = bodyPart.GetComponent<Collider>().bounds.center;
					distanceToRaycast = (bodyPart.GetComponent<Collider>().bounds.extents.y + groundedErrorTweaker) * transform.lossyScale.y;
					rayIntersected = Physics.Raycast(raycastPosition, -transform.up, out hitInfo, 
													 distanceToRaycast, groundLayers.value		  );
					
					if(rayIntersected && hitInfo.rigidbody)
					{
						if(!hitInfo.rigidbody.isKinematic)
						{
							tempGrounded = true;
							break;
						}
					}
				}
			}
		}
		
        grounded = tempGrounded;

		// *** HACK: for fixing weird drift that is probably related to RUISKinectAndMecanimCombiner's transform.position update and possibly stabilizing collider & others
		//  TODO: Does such miniscular position update affect how physics collision is handled? N.B. MovePosition is not used in RUISKinectAndMecanimCombiner right now
		if((transform.position - previousPosition).magnitude * Mathf.Max(Mathf.Abs(transform.lossyScale.x), 
		                                                                 Mathf.Max( Mathf.Abs(transform.lossyScale.y), Mathf.Abs(transform.lossyScale.z))) < 0.0009f)
			transform.position = previousPosition;
		
		previousPosition = transform.position;
    }
	
	void FixedUpdate()
	{
		if(wasColliding)
			colliding = true;
		else
			colliding = false;
		wasColliding = false;
		
		if(stabilizingCollider)
		{
			if(dynamicFriction)
			{
				colliderComponent = stabilizingCollider.gameObject.GetComponent<Collider>();
				if(colliderComponent)
				{
					if(colliderComponent.material)
					{
						
						if(grounded && (Time.fixedTime - lastJumpTime) > 1)
						{
							colliderComponent.material = originalMaterial;
						}
						else
						{
							colliderComponent.material = dynamicMaterial; 
						}
					}
				}
			}
		}
	}

    public void RotateAroundCharacterPivot(Vector3 eulerRotation)
    {
        Vector3 pivotPosition = GetPivotPositionInTrackerCoordinates();
        if (pivotPosition == Vector3.zero)
        {
            GetComponent<Rigidbody>().MoveRotation(Quaternion.Euler(eulerRotation) * transform.rotation);
            return;
        }

        pivotPosition = transform.TransformPoint(pivotPosition);
        //Debug.Log("pivotPosition: " + pivotPosition);
        //Debug.DrawLine(pivotPosition, transform.position, Color.blue);

        Vector3 positionDiff = pivotPosition - transform.position;
        //Debug.Log("old: " + positionDiff);
        //positionDiff.y = 0;
        //Debug.DrawLine(pivotPosition - positionDiff, pivotPosition, Color.red);

        positionDiff = Quaternion.Euler(eulerRotation) * positionDiff;
        //Debug.DrawLine(transform.position, pivotPosition, Color.red);
        //Debug.DrawLine(pivotPosition - positionDiff, pivotPosition, Color.green);

        //Debug.Log("new: " + positionDiff);
        Vector3 newPosition = pivotPosition - positionDiff;
        //Debug.DrawLine(transform.position, newPosition, Color.yellow);
        GetComponent<Rigidbody>().MovePosition(newPosition);
        GetComponent<Rigidbody>().MoveRotation(Quaternion.Euler(eulerRotation) * transform.rotation);
    }

    public Vector3 TransformDirection(Vector3 directionInCharacterCoordinates)
    {
        Vector3 characterForward = Vector3.forward;
		if(skeletonController)
			characterForward = skeletonController.transform.localRotation * Vector3.forward;

		switch (characterPivotType)
		{
			case CharacterPivotType.KinectHead:
				if(   (bodyTrackingDeviceID == RUISSkeletonManager.kinect2SensorID && !inputManager.enableKinect2)
				   || (bodyTrackingDeviceID == RUISSkeletonManager.kinect1SensorID && !inputManager.enableKinect ))
					break;
				if(skeletonManager != null && skeletonManager.skeletons[bodyTrackingDeviceID, kinectPlayerId] != null)
					characterForward = skeletonManager.skeletons[bodyTrackingDeviceID, kinectPlayerId].head.rotation * Vector3.forward;
					else 
						characterForward = Vector3.forward;
	                break;
			case CharacterPivotType.KinectTorso:
				if(   (bodyTrackingDeviceID == RUISSkeletonManager.kinect2SensorID && !inputManager.enableKinect2)
				   || (bodyTrackingDeviceID == RUISSkeletonManager.kinect1SensorID && !inputManager.enableKinect ))
					break;
				if(skeletonManager != null && skeletonManager.skeletons[bodyTrackingDeviceID, kinectPlayerId] != null)
					characterForward = skeletonManager.skeletons[bodyTrackingDeviceID, kinectPlayerId].torso.rotation * Vector3.forward;
					else 
						characterForward = Vector3.forward;
	                break;
            case CharacterPivotType.MoveController:
			{
				if(!inputManager.enablePSMove || !headPointsWalkingDirection)
					break;
				RUISPSMoveWand psmove = inputManager.GetMoveWand(moveControllerId);
				if(psmove != null)
	                characterForward = psmove.localRotation * Vector3.forward;
			}
                break;
        }

		if(skeletonManager != null && (skeletonController.followOculusController || skeletonController.followMoveController) && headPointsWalkingDirection)
			characterForward = skeletonController.trackedDeviceYawRotation * Vector3.forward;

        if (ignorePitchAndRoll)
        {
            characterForward.y = 0;
            characterForward.Normalize();
        }

        characterForward = transform.TransformDirection(characterForward);


        return Quaternion.LookRotation(characterForward, transform.up) * directionInCharacterCoordinates;
    }

    private Vector3 GetPivotPositionInTrackerCoordinates()
    {
		if(   useOculusPositionalTracking 
		   && OVRManager.tracker != null && OVRManager.tracker.isPositionTracked)
		{
			if(coordinateSystem && coordinateSystem.applyToRootCoordinates)
			{
				return coordinateSystem.ConvertLocation(coordinateSystem.GetOculusRiftLocation(), RUISDevice.Oculus_DK2);
			}
			else if(OVRManager.display != null)
			{
				return OVRManager.display.GetHeadPose().position;
			}
		}
		else
		{
	        switch (characterPivotType)
	        {
	            case CharacterPivotType.KinectHead:
				{
					if(    (bodyTrackingDeviceID == RUISSkeletonManager.kinect2SensorID && !inputManager.enableKinect2)
				   		|| (bodyTrackingDeviceID == RUISSkeletonManager.kinect1SensorID && !inputManager.enableKinect ))
						break;

					if(skeletonManager && skeletonManager.skeletons[bodyTrackingDeviceID, kinectPlayerId] != null)
					{
						if(skeletonController) // Add root speed scaling
							return Vector3.Scale(skeletonManager.skeletons[bodyTrackingDeviceID, kinectPlayerId].head.position, skeletonController.rootSpeedScaling);
						else
							return skeletonManager.skeletons[bodyTrackingDeviceID, kinectPlayerId].head.position;
					}
					break;
				}
	            case CharacterPivotType.KinectTorso:
				{
					if(   (bodyTrackingDeviceID == RUISSkeletonManager.kinect2SensorID && !inputManager.enableKinect2)
					   || (bodyTrackingDeviceID == RUISSkeletonManager.kinect1SensorID && !inputManager.enableKinect ))
						break;

					if(skeletonManager && skeletonManager.skeletons[bodyTrackingDeviceID, kinectPlayerId] != null)
					{
						if(skeletonController) // Add root speed scaling
							return Vector3.Scale(skeletonManager.skeletons[bodyTrackingDeviceID, kinectPlayerId].torso.position, skeletonController.rootSpeedScaling);
						else
							return skeletonManager.skeletons[bodyTrackingDeviceID, kinectPlayerId].torso.position;
					}
					break;
				}
	            case CharacterPivotType.MoveController:
				{
					if(!inputManager.enablePSMove)
						break;

					if(inputManager.GetMoveWand(moveControllerId))
		                return inputManager.GetMoveWand(moveControllerId).handlePosition;
					break;
				}
	        }
		}

		if(skeletonController != null)
			return skeletonController.transform.localPosition;
		else
	        return Vector3.zero;
    }

    public void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying)
            return;
        Vector3 pivotPosition = GetPivotPositionInTrackerCoordinates();
        pivotPosition = transform.TransformPoint(pivotPosition);

        Gizmos.DrawLine(transform.position, pivotPosition);
    }
	
	
	void OnCollisionStay(Collision other)
	{
		// Check if collider belongs to groundLayers
		//if((groundLayers.value & (1 << other.gameObject.layer)) > 0)
		//{
		wasColliding = true;
		//Debug.LogError(other.gameObject.name);
		//}
	}

	RUISSkeletonController getSkeletonController()
	{
		// Original skeletonController has been destroyed because the GameObject which had
		// it has been split in three parts: Kinect, Mecanim, Blended. Lets fetch the new one.
		if (!combinerChildrenInstantiated && kinectAndMecanimCombinerExists)
		{
			RUISKinectAndMecanimCombiner combiner = GetComponentInChildren<RUISKinectAndMecanimCombiner>();
			if (combiner && combiner.isChildrenInstantiated())
			{
				skeletonController = combiner.skeletonController;
				
				if(skeletonController == null)
					Debug.LogError(  "Could not find Component " + typeof(RUISSkeletonController) + " from "
					               + "children of " + gameObject.name
					               + ", something is very wrong with this character setup!");
				combinerChildrenInstantiated = true;
			}
		}
		return skeletonController;
	}
}
