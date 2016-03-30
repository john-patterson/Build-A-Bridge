/*****************************************************************************

Content    :   A script to shoot projectiles with a move controller
Authors    :   Mikael Matveinen
Copyright  :   Copyright 2013 Tuukka Takala, Mikael Matveinen. All Rights reserved.
Licensing  :   RUIS is distributed under the LGPL Version 3 license.

******************************************************************************/

using UnityEngine;
using System.Collections;

public class MoveGun : MonoBehaviour {
    RUISPSMoveWand moveController;
    public GameObject bulletPrefab;
    public Transform bulletSpawnSpot;

    public float bulletSpeed = 1.0f;

    public float shootingInterval = 0.3f;
    float timeSinceLastShot = 0;

    void Awake()
    {
        moveController = GetComponent<RUISPSMoveWand>();
    }

	void Update () {
        timeSinceLastShot += Time.deltaTime;

        if (moveController.triggerValue > 0.85 && timeSinceLastShot >= shootingInterval)
        {
            timeSinceLastShot = 0;
            GameObject bullet = Instantiate(bulletPrefab, bulletSpawnSpot.position, bulletSpawnSpot.rotation) as GameObject;
            bullet.GetComponent<Rigidbody>().AddForce(bulletSpawnSpot.forward * bulletSpeed, ForceMode.VelocityChange);

            Destroy(bullet, 5);
        }
	}
}
