/*****************************************************************************

Content    :   A class to manage the Kinect depth map image to also the skeleton on top of the user
Authors    :   Mikael Matveinen
Copyright  :   Copyright 2015 Tuukka Takala, Mikael Matveinen. All Rights reserved.
Licensing  :   RUIS is distributed under the LGPL Version 3 license.

******************************************************************************/

using UnityEngine;
using System;
using System.Collections;
using System.Runtime.InteropServices;

public class RUISUserViewer : MonoBehaviour {
    OpenNISettingsManager settingsManager;
    OpenNI.DepthGenerator depthGenerator;
    public NIPlayerManagerCOMSelection playerSelection;

    Texture2D texture;
    int width, height;

    short[] depthMap;
    float[] depthHistogramMap;
    OpenNI.DepthMetaData metaData;

    Color[] mapPixels;

    int lastFrameId;

    public Color depthMapColor;
    public int factor = 4;

    public bool Kinect1Update = true;

	void Start () 
	{

		// Below is only for Kinect 1
        settingsManager = FindObjectOfType(typeof(OpenNISettingsManager)) as OpenNISettingsManager;
	
		if (settingsManager == null || !settingsManager.UserGenrator.Valid || settingsManager.CurrentContext.Depth == null)
        {
            Kinect1Update = false;
            return;
        }

        depthGenerator = new OpenNI.DepthGenerator(settingsManager.CurrentContext.BasicContext);        

        OpenNI.MapOutputMode mapOutputMode = settingsManager.CurrentContext.Depth.MapOutputMode;
        width = mapOutputMode.XRes / factor;
        height = mapOutputMode.YRes / factor;
        texture = new Texture2D(width, height);

        depthMap = new short[(int)(mapOutputMode.XRes * mapOutputMode.YRes)];
        depthHistogramMap = new float[settingsManager.CurrentContext.Depth.DeviceMaxDepth];

        NIOpenNICheckVersion.Instance.ValidatePrerequisite();
        metaData = new OpenNI.DepthMetaData();

        mapPixels = new Color[width * height];
	}
	
	void Update () {
        if (!Kinect1Update) return;

        settingsManager.CurrentContext.Depth.GetMetaData(metaData);

        if (metaData.FrameID >= lastFrameId)
        {
            lastFrameId = metaData.FrameID;
            Marshal.Copy(settingsManager.CurrentContext.Depth.DepthMapPtr, depthMap, 0, depthMap.Length);
            UpdateHistogram();
            UpdateDepthmapTexture();

            DrawLineBetweenJoints(OpenNI.SkeletonJoint.Head, OpenNI.SkeletonJoint.Neck);
            DrawLineBetweenJoints(OpenNI.SkeletonJoint.Neck, OpenNI.SkeletonJoint.Torso);
            //DrawLineBetweenJoints(OpenNI.SkeletonJoint.Torso, OpenNI.SkeletonJoint.Waist);
            DrawLineBetweenJoints(OpenNI.SkeletonJoint.Neck, OpenNI.SkeletonJoint.RightShoulder);
            DrawLineBetweenJoints(OpenNI.SkeletonJoint.RightShoulder, OpenNI.SkeletonJoint.RightElbow);
            DrawLineBetweenJoints(OpenNI.SkeletonJoint.RightElbow, OpenNI.SkeletonJoint.RightHand);
            //DrawLineBetweenJoints(OpenNI.SkeletonJoint.RightWrist, OpenNI.SkeletonJoint.RightHand);
            //DrawLineBetweenJoints(OpenNI.SkeletonJoint.RightHand, OpenNI.SkeletonJoint.RightFingertip);
            DrawLineBetweenJoints(OpenNI.SkeletonJoint.Neck, OpenNI.SkeletonJoint.LeftShoulder);
            DrawLineBetweenJoints(OpenNI.SkeletonJoint.LeftShoulder, OpenNI.SkeletonJoint.LeftElbow);
            DrawLineBetweenJoints(OpenNI.SkeletonJoint.LeftElbow, OpenNI.SkeletonJoint.LeftHand);
            //DrawLineBetweenJoints(OpenNI.SkeletonJoint.LeftWrist, OpenNI.SkeletonJoint.LeftHand);
            //DrawLineBetweenJoints(OpenNI.SkeletonJoint.LeftHand, OpenNI.SkeletonJoint.LeftFingertip);
            DrawLineBetweenJoints(OpenNI.SkeletonJoint.Torso, OpenNI.SkeletonJoint.RightHip);
            DrawLineBetweenJoints(OpenNI.SkeletonJoint.RightHip, OpenNI.SkeletonJoint.RightKnee);
            //DrawLineBetweenJoints(OpenNI.SkeletonJoint.RightKnee, OpenNI.SkeletonJoint.RightAnkle);
            DrawLineBetweenJoints(OpenNI.SkeletonJoint.RightKnee, OpenNI.SkeletonJoint.RightFoot);
            DrawLineBetweenJoints(OpenNI.SkeletonJoint.Torso, OpenNI.SkeletonJoint.LeftHip);
            DrawLineBetweenJoints(OpenNI.SkeletonJoint.LeftHip, OpenNI.SkeletonJoint.LeftKnee);
            //DrawLineBetweenJoints(OpenNI.SkeletonJoint.LeftKnee, OpenNI.SkeletonJoint.LeftAnkle);
            DrawLineBetweenJoints(OpenNI.SkeletonJoint.LeftKnee, OpenNI.SkeletonJoint.LeftFoot);

            texture.SetPixels(mapPixels);
            texture.Apply(false);
        }
	}

    void OnGUI()
    {
        if (!Kinect1Update) return;

        GUI.DrawTexture(new Rect(0, 0, Screen.width/2, Screen.height/2), texture, ScaleMode.StretchToFill, false);
    }

    protected void UpdateHistogram()
    {
        int i, numOfPoints = 0;

        Array.Clear(depthHistogramMap, 0, depthHistogramMap.Length);

        int depthIndex = 0;
        for (int y = 0; y < height; ++y)
        {
            for (int x = 0; x < width; ++x, depthIndex += factor)
            {
                if (depthMap[depthIndex] != 0)
                {
                    depthHistogramMap[depthMap[depthIndex]]++;
                    numOfPoints++;
                }
            }
            depthIndex += (factor - 1) * width;
        }
        if (numOfPoints > 0)
        {
            for (i = 1; i < depthHistogramMap.Length; i++)
            {
                depthHistogramMap[i] += depthHistogramMap[i - 1];
            }
            for (i = 0; i < depthHistogramMap.Length; i++)
            {
                depthHistogramMap[i] = 1.0f - (depthHistogramMap[i] / numOfPoints);
            }
        }
    }


    void UpdateDepthmapTexture()
    {
        // flip the depthmap as we create the texture
        int i = width * height - 1;
        int depthIndex = 0;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++, i--, depthIndex += factor)
            {
                short pixel = depthMap[depthIndex];
                if (pixel == 0)
                {
                    mapPixels[i] = Color.black;
                }
                else
                {
                    Color c = new Color(depthHistogramMap[pixel], depthHistogramMap[pixel], depthHistogramMap[pixel]);
                    mapPixels[i] = depthMapColor * c;
                }
            }
            depthIndex += (factor - 1) * width * factor;
        }
    }

    void DrawLineBetweenJoints(OpenNI.SkeletonJoint first, OpenNI.SkeletonJoint second)
    {
        NISelectedPlayer player = playerSelection.GetPlayer(0);
        OpenNI.SkeletonJointPosition firstJointPosition;
        player.GetSkeletonJointPosition(first, out firstJointPosition);
        OpenNI.SkeletonJointPosition secondJointPosition;
        player.GetSkeletonJointPosition(second, out secondJointPosition);

        if (firstJointPosition.Confidence <= 0.5 || secondJointPosition.Confidence <= 0.5) return;

        OpenNI.Point3D firstJointScreenPosition = depthGenerator.ConvertRealWorldToProjective(firstJointPosition.Position);
        OpenNI.Point3D secondJointScreenPosition = depthGenerator.ConvertRealWorldToProjective(secondJointPosition.Position);
        DrawLine.DrawSimpleLine(ref mapPixels,
           (int)(width - firstJointScreenPosition.X / factor), (int)(height - firstJointScreenPosition.Y / factor), 
           (int)(width - secondJointScreenPosition.X / factor), (int)(height - secondJointScreenPosition.Y / factor), 
            width, height,
            Color.white);
    }
}
