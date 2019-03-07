/* Copyright Positron 2017 - 2018 - Code by Brad Nelson */

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace Positron
{
	public class SceneManager : MonoBehaviour
	{
		public GameObject bullet;
		public GameObject gazePoint;
		public float shootForce;
		public UnityEngine.UI.Text debugText;
		public VRStandardAssets.Flyer.FlyerLaserController laserController;

		// Use this for initialization
		IEnumerator Start()
		{
			while( VoyagerDevice.Instance == null && !VoyagerDevice.IsInitialized )
			{
				yield return null;
			}

			// Start game after Voyager has been initialized
			VRStandardAssets.Flyer.FlyerMovementController flyerController = FindObjectOfType<VRStandardAssets.Flyer.FlyerMovementController>();
			if( flyerController != null )
			{
				flyerController.StartGame();
			}
		}

		void Update()
		{
			if( Input.GetButtonUp("Submit"))
			{
				if( bullet != null && gazePoint != null )
				{
					GameObject shot = GameObject.Instantiate(bullet, gazePoint.transform.position, gazePoint.transform.rotation);
					// Add force to the cloned object in the object's forward direction
					shot.GetComponent<Rigidbody>().AddForce(shot.transform.forward * shootForce);
				}
				else if( laserController != null )
				{
					laserController.ShootLasers();
				}
			}

			// Show Debug Text
			if( Input.GetKeyUp(KeyCode.D))
			{
				debugText.transform.parent.gameObject.SetActive(!debugText.transform.parent.gameObject.activeSelf);
			}
		}
	}
}
