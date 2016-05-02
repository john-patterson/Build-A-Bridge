using System;
using UnityEngine;
using System.Collections;
using System.Linq;
using Leap.Unity;

public class LeapOutput : MonoBehaviour
{
    public Transform RightThumbTip;
    public Transform RightIndexTip;
    public Transform RightIndexKnuckle;
    public Transform RightMiddleTip;
    public Transform RightRingTip;
    public Transform RightPinkyTip;
    public Transform RightPalm;

    public Transform LeftThumbTip;
    public Transform LeftIndexTip;
    public Transform LeftIndexKnuckle;
    public Transform LeftMiddleTip;
    public Transform LeftRingTip;
    public Transform LeftPinkyTip;
    public Transform LeftPalm;

    public float ThumbIndexKnuckleThreshold = 0.04f;
    public float FingersToPalmThreshold = 0.05f;
    
    public bool PlacePointGesture()
    {
        var distance = RightThumbTip.position - RightIndexKnuckle.position;
        var palmUp = Vector3.Dot(RightPalm.up, Vector3.up) >= -FingersToPalmThreshold;
        return distance.magnitude <= ThumbIndexKnuckleThreshold && palmUp;
    }

    public bool BridgeGesture()
    {
        var tips = new [] {RightIndexTip, RightMiddleTip, RightRingTip, RightPinkyTip};
        var averageDistanceToPalm = tips.Average(tip => (tip.position - RightPalm.position).magnitude);
        var triggered = averageDistanceToPalm <= FingersToPalmThreshold;
        var palmUp = Vector3.Dot(RightPalm.up, Vector3.up) < -FingersToPalmThreshold;
        
        return triggered && palmUp;
    }

    public bool LockGesture()
    {
        var tips = new[] { LeftIndexTip, LeftMiddleTip, LeftRingTip, LeftPinkyTip };
        var averageDistanceToPalm = tips.Average(tip => (tip.position - LeftPalm.position).magnitude);
        var triggered = averageDistanceToPalm <= FingersToPalmThreshold;
        var palmUp = Vector3.Dot(LeftPalm.up, Vector3.up) < -FingersToPalmThreshold;

        return triggered && palmUp;
    }
}

