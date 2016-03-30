/*****************************************************************************

Content    :   Functionality to combine skeleton input from Kinect with a Mecanim Animator controlled avatar
Authors    :   Mikael Matveinen, Tuukka Takala
Copyright  :   Copyright 2013 Tuukka Takala, Mikael Matveinen. All Rights reserved.
Licensing  :   RUIS is distributed under the LGPL Version 3 license.

******************************************************************************/


using UnityEngine;
using System.Collections.Generic;

public class RUISKinectAndMecanimCombiner : MonoBehaviour {
    private enum BodypartClassification
    {
        Root,
        Torso,
        Head,
        RightArm,
        LeftArm,
        RightLeg,
        LeftLeg
    }

    private class BoneTriplet
    {
        public Transform kinectTransform;
        public Transform mecanimTransform;
        public Transform blendedTransform;

        public BodypartClassification bodypartClassification;

        public List<BoneTriplet> children;

        public BoneTriplet(Transform kinectTransform, Transform mecanimTransform, Transform blendedTransform, BodypartClassification bodypartClassification)
        {
            this.kinectTransform = kinectTransform;
            this.mecanimTransform = mecanimTransform;
            this.blendedTransform = blendedTransform;

            children = new List<BoneTriplet>();

            this.bodypartClassification = bodypartClassification;
        }

        public override string ToString()
        {
            return bodypartClassification + " (" + kinectTransform.name + ", " + mecanimTransform.name + ", " + blendedTransform.name + ")";
        }
    }
    
    public Animator mecanimAnimator;
    public RUISSkeletonController skeletonController;

    public float rootBlendWeight;
    public float torsoBlendWeight;
    public float headBlendWeight;
    public float rightArmBlendWeight;
    public float leftArmBlendWeight;
    public float rightLegBlendWeight;
    public float leftLegBlendWeight;

    public bool applyTorsoCounteringRotations = true;


    public bool forceArmStartPosition = true;
    public bool forceLegStartPosition = true;

    BoneTriplet skeletonRoot;
    BoneTriplet torsoRoot;
    BoneTriplet headRoot;
    BoneTriplet neckRoot;
    BoneTriplet rightArmRoot;
    BoneTriplet leftArmRoot;
    BoneTriplet rightLegRoot;
    BoneTriplet leftLegRoot;

    GameObject kinectGameObject;
    GameObject mecanimGameObject;

    private bool childrenInstantiated = false;

	private RUISInputManager inputManager;
	private RUISSkeletonManager skeletonManager;

    private bool torsoIsRoot = false;

    void Awake()
    {
        inputManager = FindObjectOfType(typeof(RUISInputManager)) as RUISInputManager;
    }

    void Start()
	{
		Vector3 initialLocalScale = transform.localScale;
		skeletonManager = FindObjectOfType(typeof(RUISSkeletonManager)) as RUISSkeletonManager;
		
		skeletonController = GetComponent<RUISSkeletonController>();
		//		if (	!inputManager.enableKinect && !inputManager.enableKinect2 
		//		    &&  (skeletonController.bodyTrackingDevice != RUISSkeletonController.bodyTrackingDeviceType.GenericMotionTracker))
		if(skeletonController != null && (   skeletonController.followOculusController || inputManager == null
		                                  || (	   skeletonController.followMoveController 
		    									&& (   (skeletonController.bodyTrackingDeviceID == RUISSkeletonManager.kinect1SensorID && !inputManager.enableKinect )
		    										|| (skeletonController.bodyTrackingDeviceID == RUISSkeletonManager.kinect2SensorID && !inputManager.enableKinect2)))))
		{
			// Without the below if-clause the legs will twist with PS Move head tracker (when Move is enabled but Kinect is not)
			//if(!inputManager.enablePSMove)
			torsoBlendWeight = 1;
			headBlendWeight = 1;		
			rightArmBlendWeight = 1;
			leftArmBlendWeight = 1;
			rightLegBlendWeight = 1;
			leftLegBlendWeight = 1;
//			forceArmStartPosition = false;
//			forceLegStartPosition = false;
		}

        if (!childrenInstantiated)
        {
            childrenInstantiated = true;

            kinectGameObject = Instantiate(gameObject, transform.position, transform.rotation) as GameObject;
            kinectGameObject.name = name + "Kinect";
			kinectGameObject.transform.parent = transform.parent;
			kinectGameObject.transform.localScale = initialLocalScale;
            //kinectGameObject.GetComponent<RUISKinectAndMecanimCombiner>().childrenInstantiated = true;
            Destroy(kinectGameObject.GetComponent<RUISKinectAndMecanimCombiner>());
            Destroy(kinectGameObject.GetComponent<Animator>());
            Destroy(kinectGameObject.GetComponent<RUISCharacterAnimationController>());
            foreach (Collider collider in kinectGameObject.GetComponentsInChildren<Collider>())
            {
                Destroy(collider);
            }
            foreach (Renderer meshRenderer in kinectGameObject.GetComponentsInChildren<Renderer>())
            {
                Destroy(meshRenderer);
            }


            mecanimGameObject = Instantiate(gameObject, transform.position, transform.rotation) as GameObject;
            mecanimGameObject.name = name + "Mecanim";
            mecanimGameObject.transform.parent = transform.parent;
			mecanimGameObject.transform.localScale = initialLocalScale;
            Destroy(mecanimGameObject.GetComponent<RUISKinectAndMecanimCombiner>());
            Destroy(mecanimGameObject.GetComponent<RUISCharacterAnimationController>());

            Destroy(mecanimGameObject.GetComponent<RUISSkeletonController>());
            foreach (Collider collider in mecanimGameObject.GetComponentsInChildren<Collider>())
            {
                Destroy(collider);
            }
            foreach (Renderer meshRenderer in mecanimGameObject.GetComponentsInChildren<Renderer>())
            {
                Destroy(meshRenderer);
            }


            Destroy(GetComponent<RUISSkeletonController>());
            Destroy(GetComponent<Animator>());
            GetComponent<RUISCharacterAnimationController>().animator = mecanimGameObject.GetComponent<Animator>();

            skeletonController = kinectGameObject.GetComponent<RUISSkeletonController>();
            mecanimAnimator = mecanimGameObject.GetComponent<Animator>();

			if(!skeletonController)
				Debug.LogError("Script " + typeof(RUISSkeletonController) + " is not found by " + this.GetType().Name + "!");

            torsoIsRoot = skeletonController.root == skeletonController.torso;

            Transform kinectRootBone = skeletonController.root;
            Transform mecanimRootBone = FindBone(mecanimAnimator.transform, kinectRootBone.name);
            Transform blendedRootBone = FindBone(transform, kinectRootBone.name);
            skeletonRoot = new BoneTriplet(kinectRootBone, mecanimRootBone, blendedRootBone, BodypartClassification.Root);

            if (torsoIsRoot)
            {
                torsoRoot = skeletonRoot;
                AddChildren(ref torsoRoot, BodypartClassification.Torso);
            }
            else
            {
                AddChildren(ref skeletonRoot, BodypartClassification.Root);
            }
        }
    }

    void LateUpdate()
    {
		// For debug
		//rightLegBlendWeight = 1;
		//leftLegBlendWeight = 1;
		// end for debug

        if (torsoIsRoot)
        {
            UpdateScales(torsoRoot);
        }
        else
        {
            UpdateScales(skeletonRoot);
        }

        transform.position = kinectGameObject.transform.position;
        mecanimGameObject.transform.position = kinectGameObject.transform.position;
        
        torsoRoot.mecanimTransform.localPosition = torsoRoot.kinectTransform.localPosition;

        if (neckRoot != null)
        {
            //apply kinect neck height tweak to mecanim
            neckRoot.mecanimTransform.localPosition = neckRoot.mecanimTransform.localPosition - neckRoot.mecanimTransform.InverseTransformDirection(Vector3.up) * skeletonController.neckHeightTweaker;
        }

        if (torsoIsRoot)
        {
            Blend(torsoRoot);
        }
        else
        {
            Blend(skeletonRoot);
        }

        if (forceArmStartPosition)
        {
            rightArmRoot.blendedTransform.position = rightArmRoot.kinectTransform.position;
            //rightArmRoot.blendedTransform.rotation = rightArmRoot.kinectTransform.rotation;
            leftArmRoot.blendedTransform.position = leftArmRoot.kinectTransform.position;
            //leftArmRoot.blendedTransform.rotation = leftArmRoot.kinectTransform.rotation;
        }
        if (forceLegStartPosition)
        {
            rightLegRoot.blendedTransform.position = rightLegRoot.kinectTransform.position;
            //rightLegRoot.blendedTransform.rotation = rightLegRoot.kinectTransform.rotation;
            leftLegRoot.blendedTransform.position = leftLegRoot.kinectTransform.position;
            //leftLegRoot.blendedTransform.rotation = leftLegRoot.kinectTransform.rotation;
        }
    }

    void UpdateScales(BoneTriplet root)
    {
        root.blendedTransform.localScale = root.mecanimTransform.localScale = root.kinectTransform.localScale;

        foreach (BoneTriplet childTriplet in root.children)
        {
            UpdateScales(childTriplet);
        }
    }
    
    void Blend(BoneTriplet boneTriplet)
    {
        float blendWeight = GetBlendWeight(boneTriplet.bodypartClassification);
        boneTriplet.blendedTransform.localPosition = Vector3.Lerp(boneTriplet.kinectTransform.localPosition, boneTriplet.mecanimTransform.localPosition, blendWeight);
        boneTriplet.blendedTransform.localRotation = Quaternion.Slerp(boneTriplet.kinectTransform.localRotation, boneTriplet.mecanimTransform.localRotation, blendWeight);
        //boneTriplet.blendedTransform.localScale = boneTriplet.kinectTransform.localScale;

        if(applyTorsoCounteringRotations && IsLimbRoot(boneTriplet))
        {
            ApplyCounteringRotationToLimbRoot(torsoRoot, boneTriplet);
        }

        foreach (BoneTriplet childTriplet in boneTriplet.children)
        {
            Blend(childTriplet);
        }
    }

    private void AddChildren(ref BoneTriplet triplet, BodypartClassification parentBodypartClassification){
        for (int i = 0; i < triplet.kinectTransform.childCount; i++)
        {
            BoneTriplet childTriplet = new BoneTriplet(triplet.kinectTransform.GetChild(i),
                                                       triplet.mecanimTransform.GetChild(i),
                                                       triplet.blendedTransform.GetChild(i),
                                                       GetBodypartClassification(triplet.kinectTransform.GetChild(i), triplet.bodypartClassification));

            if (parentBodypartClassification != childTriplet.bodypartClassification)
            {
                RegisterRootBone(childTriplet);
            }

            if (childTriplet.kinectTransform == skeletonController.neck)
            {
                neckRoot = childTriplet;
            }

            triplet.children.Add(childTriplet);
            AddChildren(ref childTriplet, childTriplet.bodypartClassification);
        }
    }

    private BodypartClassification GetBodypartClassification(Transform kinectTransform, BodypartClassification parentClassification)
    {
        BodypartClassification newBodypartClassification = parentClassification;
        if (kinectTransform == skeletonController.torso)
        {
            newBodypartClassification = BodypartClassification.Torso;
        }
        else if (kinectTransform == skeletonController.head)
        {
            newBodypartClassification = BodypartClassification.Head;
        }
        else if (kinectTransform == skeletonController.rightShoulder)
        {
            newBodypartClassification = BodypartClassification.RightArm;
        }
        else if (kinectTransform == skeletonController.leftShoulder)
        {
            newBodypartClassification = BodypartClassification.LeftArm;
        }
        else if (kinectTransform == skeletonController.rightHip)
        {
            newBodypartClassification = BodypartClassification.RightLeg;
        }
        else if (kinectTransform == skeletonController.leftHip)
        {
            newBodypartClassification = BodypartClassification.LeftLeg;
        }

        return newBodypartClassification;
    }

    private float GetBlendWeight(BodypartClassification bodypartClassification)
    {
        switch (bodypartClassification)
        {
            case BodypartClassification.Root:
                return rootBlendWeight;
            case BodypartClassification.Torso:
                return torsoBlendWeight;
            case BodypartClassification.Head:
                return headBlendWeight;
            case BodypartClassification.RightArm:
                return rightArmBlendWeight;
            case BodypartClassification.LeftArm:
                return leftArmBlendWeight;
            case BodypartClassification.RightLeg:
                return rightLegBlendWeight;
            case BodypartClassification.LeftLeg:
                return leftLegBlendWeight;
            default:
                return 0;
        }
    }

    private void RegisterRootBone(BoneTriplet triplet)
    {
        switch (triplet.bodypartClassification)
        {
            case BodypartClassification.Torso:
                torsoRoot = triplet;
                return;
            case BodypartClassification.Head:
                headRoot = triplet;
                return;
            case BodypartClassification.RightArm:
                rightArmRoot = triplet;
                return;
            case BodypartClassification.LeftArm:
                leftArmRoot = triplet;
                return;
            case BodypartClassification.RightLeg:
                rightLegRoot = triplet;
                return;
            case BodypartClassification.LeftLeg:
                leftLegRoot = triplet;
                return;
        }
    }

    private void ApplyCounteringRotationToLimbRoot(BoneTriplet torsoBone, BoneTriplet limbRootBone)
    {
        //boneTriplet.blendedTransform.localRotation = Quaternion.Slerp(boneTriplet.kinectTransform.localRotation, boneTriplet.mecanimTransform.localRotation, blendWeight);

		if(skeletonController == null)
			return;

        float limbRootBlendWeight = GetBlendWeight(limbRootBone.bodypartClassification);
        
        //first set the global rotation so that it matches
        limbRootBone.blendedTransform.rotation = Quaternion.Slerp(limbRootBone.kinectTransform.rotation, limbRootBone.mecanimTransform.rotation, limbRootBlendWeight);

		bool headRotates = false;
		bool headRotatesBody = false;
		bool headRotatesWalking = false;
		bool headRotatesBodyAndWalking = false;

		if(   skeletonController.characterController 
		   && (skeletonController.followMoveController || skeletonController.followOculusController))
		{
			headRotatesBodyAndWalking =  skeletonController.characterController.headRotatesBody &&  skeletonController.characterController.headPointsWalkingDirection;
			headRotatesWalking        = !skeletonController.characterController.headRotatesBody &&  skeletonController.characterController.headPointsWalkingDirection;
			headRotatesBody           =  skeletonController.characterController.headRotatesBody && !skeletonController.characterController.headPointsWalkingDirection;
			headRotates               = !skeletonController.characterController.headRotatesBody && !skeletonController.characterController.headPointsWalkingDirection;
		}

        //then apply the yaw to turn the limb to the same general direction as the torso
		Quaternion kinectToMecanimYaw = CalculateKinectToMecanimYaw(   skeletonController.characterController 
		                                                            && (skeletonController.followMoveController || skeletonController.followOculusController)
		                                                            && (    headRotatesBodyAndWalking
		                                                                || (headRotatesWalking && limbRootBone.bodypartClassification != BodypartClassification.LeftArm 
		    																				   && limbRootBone.bodypartClassification != BodypartClassification.RightArm)
			                                                            || (headRotatesBody && limbRootBone.bodypartClassification != BodypartClassification.LeftLeg 
			                        														&& limbRootBone.bodypartClassification != BodypartClassification.RightLeg)
		    															|| (headRotates && limbRootBone.bodypartClassification == BodypartClassification.Head))
		                                                            );

        kinectToMecanimYaw = Quaternion.Slerp(Quaternion.identity, kinectToMecanimYaw, limbRootBlendWeight);   
		
		Vector3 rotatedForward = kinectToMecanimYaw * Vector3.forward;
        float angles = -Mathf.Atan2(rotatedForward.y, rotatedForward.z) * Mathf.Rad2Deg;
		//float angles = Vector3.Angle(Vector3.up, kinectToMecanimYaw * Vector3.up);
		// That doesn't work right, Vector3.Angle seems to return only values between [0, 180], and the leg animation came up wrong when facing left
		
		//print(angles); 
	
		// TODO fix arm twisting when skeletonController.followMoveController == true || skeletonController.followMoveController == true

		if(   (!skeletonController.followOculusController && !skeletonController.followMoveController)
		   || (    skeletonController.characterController 
		    	|| (    headRotatesBodyAndWalking
		    		&& (limbRootBone.bodypartClassification == BodypartClassification.LeftLeg || limbRootBone.bodypartClassification == BodypartClassification.RightLeg))
		    	|| (    headRotatesWalking
			    	&& (   limbRootBone.bodypartClassification == BodypartClassification.LeftLeg || limbRootBone.bodypartClassification == BodypartClassification.RightLeg))
		    	||      headRotatesBody
		    	||      headRotates ))
			limbRootBone.blendedTransform.rotation = limbRootBone.blendedTransform.rotation * Quaternion.AngleAxis(angles, limbRootBone.blendedTransform.InverseTransformDirection(transform.up));


        
    }

    private Quaternion CalculateKinectToMecanimYaw(bool useTrackedDeviceYaw)
    {
		Vector3 kinectTorsoForward = torsoRoot.kinectTransform.forward; // The old and occasionally buggy way (kinectTorsoForward is set again below)
		Vector3 kinectTorsoUp = Vector3.up; //torsoRoot.kinectTransform.up;

		// If Move controller or Oculus Rift is character pivot
		if(useTrackedDeviceYaw)
		{
//			if(skeletonController.characterController.headPointsWalkingDirection)
			kinectTorsoForward = skeletonController.trackedDeviceYawRotation * Vector3.forward;
		}
		else // If Kinect torso/head is character pivot
			if(skeletonManager != null && skeletonManager.skeletons[skeletonController.bodyTrackingDeviceID, skeletonController.playerId] != null)
				kinectTorsoForward = skeletonManager.skeletons[skeletonController.bodyTrackingDeviceID, skeletonController.playerId].torso.rotation*Vector3.forward;

		//kinectTorsoUp.y = 0;
		//kinectTorsoUp.Normalize();

        kinectTorsoForward.y = 0;
		kinectTorsoForward.Normalize();
        Quaternion kinectTorsoRotation = Quaternion.LookRotation(kinectTorsoForward, kinectTorsoUp);

		Vector3 mecanimTorsoForward = transform.forward; //-transform.right;
        mecanimTorsoForward.y = 0;
        mecanimTorsoForward.Normalize();
		//Vector3 mecanimTorsoUp = Vector3.up; //transform.forward;
        //mecanimTorsoUp.y = 0;
        //mecanimTorsoUp.Normalize();
//        Quaternion mecanimTorsoRotation = Quaternion.LookRotation(mecanimTorsoForward, mecanimTorsoUp);

		// Is the custom Euler rotation rig hierarchy dependent..?
		Quaternion torsoYawRotation = Quaternion.Euler(new Vector3(0,0,-90)) * kinectTorsoRotation; // Quaternion.Inverse(kinectTorsoRotation) * mecanimTorsoRotation;

        return torsoYawRotation;
    }

    private bool IsLimbRoot(BoneTriplet bone)
    {
        return bone == headRoot || bone == rightArmRoot || bone == leftArmRoot || bone == rightLegRoot || bone == leftLegRoot;
    }

    private bool IsPartOfLimb(BoneTriplet bone)
    {
        return bone.bodypartClassification == BodypartClassification.RightLeg ||
               bone.bodypartClassification == BodypartClassification.LeftLeg;
    }
	
	public bool isChildrenInstantiated()
	{
		return childrenInstantiated;
	}

    public Transform FindBone(Transform root, string name)
    {
        for (int i = 0; i < root.childCount; i++)
        {
            if (root.name == name)
            {
                return root;
            }

            Transform result = FindBone(root.GetChild(i), name);
            if (result != null)
            {
                return result;
            }
        }

        return null;
    }
}
