/*****************************************************************************

Content    :   Visualize the difference between device1 and device2 calibration results
Authors    :   Mikael Matveinen, Tuukka Takala
Copyright  :   Copyright 2013 Tuukka Takala, Mikael Matveinen. All Rights reserved.
Licensing  :   RUIS is distributed under the LGPL Version 3 license.

******************************************************************************/

using UnityEngine;
using System.Collections;

public class RUISSampleDifferenceVisualizer : MonoBehaviour {
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
