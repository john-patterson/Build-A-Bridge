/*****************************************************************************

Content    :   A class to visualize the difference between move and kinect calibration results
Authors    :   Mikael Matveinen
Copyright  :   Copyright 2013 Tuukka Takala, Mikael Matveinen. All Rights reserved.
Licensing  :   RUIS is distributed under the LGPL Version 3 license.

******************************************************************************/

using UnityEngine;
using System.Collections;

public class RUISMoveCalibrationVisualizer : MonoBehaviour {
    public GameObject kinectCalibrationSphere;
    private LineRenderer lineRenderer;

	void Start () {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.SetVertexCount(2);
	}
	
	void Update () {
        lineRenderer.SetPosition(0, transform.position);
        lineRenderer.SetPosition(1, kinectCalibrationSphere.transform.position);
	}
}
