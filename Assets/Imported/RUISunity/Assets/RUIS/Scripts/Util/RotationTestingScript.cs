/*****************************************************************************

Content    :   An internal class used to test out different quaternion rotation modifications
Authors    :   Mikael Matveinen
Copyright  :   Copyright 2013 Tuukka Takala, Mikael Matveinen. All Rights reserved.
Licensing  :   RUIS is distributed under the LGPL Version 3 license.

******************************************************************************/

using UnityEngine;
using System.Collections;

public class RotationTestingScript : MonoBehaviour {
    public int controllerId = 0;
    public int flipSignsId = 0;
    public PSMoveWrapper psMoveWrapper;
    public GameObject[] controllers;

    private int t = 1;
    private int u = 1;
    private int v = 1;
    private int w = 1;

	void Start () {
        controllers[0].transform.rotation = Quaternion.LookRotation(Vector3.up);

        psMoveWrapper.Connect();
	}

    void OnDestroy()
    {
        psMoveWrapper.Disconnect(false);
    }

    void Update()
    {
        if (psMoveWrapper.WasReleased(controllerId, PSMoveWrapper.CROSS))
        {
            flipSignsId++;
            if (flipSignsId >= 16)
            {
                flipSignsId = 0;
            }
        }

        if (flipSignsId > 7)
            t = -1;
        switch (flipSignsId % 8)
        {
            case 0:
                u = 1; v = 1; w = 1;
                break;
            case 1:
                u = -1; v = 1; w = 1;
                break;
            case 2:
                u = 1; v = -1; w = 1;
                break;
            case 3:
                u = 1; v = 1; w = -1;
                break;
            case 4:
                u = -1; v = -1; w = 1;
                break;
            case 5:
                u = 1; v = -1; w = -1;
                break;
            case 6:
                u = -1; v = 1; w = -1;
                break;
            case 7:
                u = -1; v = -1; w = -1;
                break;
        }

        float a = t * psMoveWrapper.qOrientation[controllerId].w;
        float b = u * psMoveWrapper.qOrientation[controllerId].x;
        float c = v * psMoveWrapper.qOrientation[controllerId].y;
        float d = w * psMoveWrapper.qOrientation[controllerId].z;

        // Generate all 24 quaternion element order combinations
        for (int i = 0; i < 24; ++i)
        {
            Quaternion quat = new Quaternion();

            switch (i)
            {
                case 0:
                    quat.w = a; quat.x = b; quat.y = c; quat.z = d; break;
                case 1:
                    quat.w = d; quat.x = a; quat.y = b; quat.z = c; break;
                case 2:
                    quat.w = c; quat.x = d; quat.y = a; quat.z = b; break;
                case 3:
                    quat.w = b; quat.x = c; quat.y = d; quat.z = a; break;

                case 4:
                    quat.w = d; quat.x = c; quat.y = b; quat.z = a; break;
                case 5:
                    quat.w = a; quat.x = d; quat.y = c; quat.z = b; break;
                case 6:
                    quat.w = b; quat.x = a; quat.y = d; quat.z = c; break;
                case 7:
                    quat.w = c; quat.x = b; quat.y = a; quat.z = d; break;

                case 17:
                    quat.w = d; quat.x = b; quat.y = a; quat.z = c; break;
                case 18:
                    quat.w = d; quat.x = a; quat.y = c; quat.z = b; break;
                case 19:
                    quat.w = d; quat.x = b; quat.y = c; quat.z = a; break;
                case 20:
                    quat.w = d; quat.x = c; quat.y = a; quat.z = b; break;

                case 8:
                    quat.w = a; quat.x = d; quat.y = b; quat.z = c; break;
                case 9:
                    quat.w = a; quat.x = c; quat.y = d; quat.z = b; break;
                case 10:
                    quat.w = a; quat.x = b; quat.y = d; quat.z = c; break;
                case 21:
                    quat.w = a; quat.x = c; quat.y = b; quat.z = d; break;

                case 11:
                    quat.w = b; quat.x = d; quat.y = a; quat.z = c; break;
                case 12:
                    quat.w = b; quat.x = a; quat.y = c; quat.z = d; break;
                case 13:
                    quat.w = b; quat.x = c; quat.y = a; quat.z = d; break;
                case 22:
                    quat.w = b; quat.x = d; quat.y = c; quat.z = a; break;

                case 14:
                    quat.w = c; quat.x = d; quat.y = b; quat.z = a; break;
                case 15:
                    quat.w = c; quat.x = a; quat.y = d; quat.z = b; break;
                case 16:
                    quat.w = c; quat.x = a; quat.y = b; quat.z = d; break;
                case 23:
                    quat.w = c; quat.x = b; quat.y = d; quat.z = a; break;
            }

            controllers[i].transform.rotation = quat;
        }
    }
}
