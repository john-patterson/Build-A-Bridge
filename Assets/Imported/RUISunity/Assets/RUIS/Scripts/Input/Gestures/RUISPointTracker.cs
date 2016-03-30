/*****************************************************************************

Content    :   A class used to keep track of a transform to get info about its movements
Authors    :   Mikael Matveinen
Copyright  :   Copyright 2013 Tuukka Takala, Mikael Matveinen. All Rights reserved.
Licensing  :   RUIS is distributed under the LGPL Version 3 license.

******************************************************************************/

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class RUISPointTracker : MonoBehaviour
{
    public class PointData
    {
        public Vector3 position;
        public Quaternion rotation;
        public float deltaTime;
        public Vector3 velocity;
        public float startTime;

        public PointData(Vector3 position, Quaternion rotation, float deltaTime, float startTime, PointData previous)
        {
            this.position = position;
            this.rotation = rotation;
            this.deltaTime = deltaTime;
            this.startTime = startTime;

            if (previous != null)
            {
                velocity = (position - previous.position) / deltaTime;
            }
        }
    }

    List<PointData> points = new List<PointData>();
    PointData previousPoint = null;

    public float bufferLength = 0.1f;

    float timeSinceLastUpdate = 0;

    void Awake()
    {
        cachedAverageSpeed = new CachedAverageSpeed(ref points);
        cachedMaxVelocity = new CachedMaxVelocity(ref points);
        cachedAverageVelocity = new CachedAverageVelocity(ref points);
    }

    void Update()
    {
        timeSinceLastUpdate += Time.deltaTime;

        PointData newPoint = new PointData(transform.localPosition, transform.localRotation, timeSinceLastUpdate, Time.timeSinceLevelLoad, previousPoint);

        //remove zero velocities just in case, in order for the speeds not to get polluted by nonexisting data
        //if (newPoint.velocity == Vector3.zero) return;

        points.Add(newPoint);
        previousPoint = newPoint;

        while (points[points.Count-1].startTime - points[0].startTime >= bufferLength)
        {
            points.RemoveAt(0);
        }

        //if (points.Count > bufferSize) points.RemoveAt(0);

        InvalidateCaches();

        //Debug.Log(averageSpeed);

        timeSinceLastUpdate = 0;
    }

    private void InvalidateCaches()
    {
        cachedAverageSpeed.Invalidate();
        cachedMaxVelocity.Invalidate();
        cachedAverageVelocity.Invalidate();
    }

    private CachedAverageSpeed cachedAverageSpeed;
    public float averageSpeed
    {
        get
        {
            return cachedAverageSpeed.GetValue();
        }
    }

    private CachedMaxVelocity cachedMaxVelocity;
    public Vector3 maxVelocity
    {
        get
        {
            return cachedMaxVelocity.GetValue();
        }
    }

    private CachedAverageVelocity cachedAverageVelocity;
    public Vector3 averageVelocity
    {
        get
        {
            return cachedAverageVelocity.GetValue();
        }
    }



    public class CachedAverageSpeed : CachedValue<float>
    {
        List<PointData> valueList;

        public CachedAverageSpeed(ref List<PointData> valueList)
        {
            this.valueList = valueList;
        }

        protected override float CalculateValue()
        {
            float speed = 0;
            foreach (PointData data in valueList)
            {
                speed += data.velocity.magnitude;
            }
            return speed / valueList.Count;
        }
    }

    public class CachedMaxVelocity : CachedValue<Vector3>
    {
        List<PointData> valueList;

        public CachedMaxVelocity(ref List<PointData> valueList)
        {
            this.valueList = valueList;
        }

        protected override Vector3 CalculateValue()
        {
            Vector3 maxVelocity = Vector3.zero;
            foreach (PointData data in valueList)
            {
                maxVelocity = maxVelocity.sqrMagnitude > data.velocity.sqrMagnitude ? maxVelocity : data.velocity;
            }

            return maxVelocity;
        }
    }

    public class CachedAverageVelocity : CachedValue<Vector3>
    {
        List<PointData> valueList;

        public CachedAverageVelocity(ref List<PointData> valueList)
        {
            this.valueList = valueList;
        }

        protected override Vector3 CalculateValue()
        {
            Vector3 velocity = Vector3.zero;
            foreach (PointData data in valueList)
            {
                velocity += data.velocity;
            }
            return velocity / valueList.Count;
        }
    }


}
