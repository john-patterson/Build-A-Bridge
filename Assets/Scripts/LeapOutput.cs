using System;
using UnityEngine;
using System.Collections;
using System.Linq;

public class LeapOutput : MonoBehaviour
{
    public Transform ThumbTip;
    public Transform IndexTip;
    public Transform IndexKnuckle;
    public Transform MiddleTip;
    public Transform RingTip;
    public Transform PinkyTip;
    public Transform Palm;
    public float ThumbIndexKnuckleThreshold = 0.03f;
    public float FingersToPalmThreshold = 0.05f;
    
    public bool PlacePointGesture()
    {
        var distance = ThumbTip.position - IndexKnuckle.position;
        return distance.magnitude <= ThumbIndexKnuckleThreshold;
    }

    public bool BridgeGesture()
    {
        var tips = new [] {IndexTip, MiddleTip, RingTip, PinkyTip};
        var averageDistanceToPalm = tips.Average(tip => (tip.position - Palm.position).magnitude);
        var triggered = averageDistanceToPalm <= FingersToPalmThreshold;
        return triggered;
    }
}

