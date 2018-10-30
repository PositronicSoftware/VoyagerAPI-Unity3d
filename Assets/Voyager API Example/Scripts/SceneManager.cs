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
		public TimelineControl timelineControl;
		public GameObject bullet;
		public GameObject gazePoint;
		public float shootForce;
		public UnityEngine.UI.Text debugText;
		public VRStandardAssets.Flyer.FlyerLaserController laserController;
		public SwitchTrack switchTrack;

		private int currentTrack = 0;

		private float lastSwitchTime = 0f;
		private float lastSwitchDelay = 1f;

		private MotionProfile motionProfile;

		void Awake()
		{
			motionProfile = FindObjectOfType<MotionProfile>();
		}

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

		// Update is called once per frame
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

			if( VoyagerDevice.Instance == null )
			{
				return;
			}

			if( switchTrack != null )
			{
				// Switch timeline tracks with the Oculus Remote Dpad left and right
				if( Time.time - lastSwitchTime > lastSwitchDelay )
				{
					if( Input.GetAxis("Horizontal") >= 1.0f
						|| timelineControl != null && timelineControl.trackForward )
					{
						currentTrack++;
						if( currentTrack >= switchTrack.tracks.Length )
						{
							currentTrack = 0;
						}

						switchTrack.director.Stop();
						switchTrack.Switch(currentTrack);

						lastSwitchTime = Time.time;

						if( timelineControl != null )
						{
							timelineControl.trackForward = false;
						}

						switch( currentTrack )
						{
							case (0): {
								VoyagerDevice.SetMotionProfile("A");
								break;
							}
							case (1): {
								VoyagerDevice.SetMotionProfile("B");
								break;
							}
							case (2): {
								VoyagerDevice.SetMotionProfile("C");
								break;
							}
						}
					}
					else if( Input.GetAxis("Horizontal") <= -1.0f
							 || timelineControl && timelineControl.trackBack )
					{
						currentTrack--;
						if( currentTrack < 0 )
						{
							currentTrack = switchTrack.tracks.Length - 1;
						}

						switchTrack.director.Stop();
						switchTrack.Switch(currentTrack);

						lastSwitchTime = Time.time;

						if( timelineControl != null )
						{
							timelineControl.trackBack = false;
						}

						switch( currentTrack )
						{
							case (0): {
								VoyagerDevice.SetMotionProfile("A");
								break;
							}
							case (1): {
								VoyagerDevice.SetMotionProfile("B");
								break;
							}
							case (2): {
								VoyagerDevice.SetMotionProfile("C");
								break;
							}
						}
					}
				}

				// Seek time given from the Interface
				if( VoyagerDevice.IsUpdated && VoyagerDevice.IsInitialized )
				{
					if( VoyagerDevice.MotionProfile == "A" )
					{
						if( currentTrack != 0 )
						{
							currentTrack = 0;
							switchTrack.director.Stop();
							switchTrack.Switch(0);

							VoyagerDevice.SetMotionProfile("A");

							timelineControl.director = switchTrack.director;
							timelineControl.Seek(VoyagerDevice.DeviceMotionProfileTime);
						}
					}
					else if( VoyagerDevice.MotionProfile == "B" )
					{
						if( currentTrack != 1 )
						{
							currentTrack = 1;
							switchTrack.director.Stop();
							switchTrack.Switch(1);

							VoyagerDevice.SetMotionProfile("B");

							timelineControl.director = switchTrack.director;
							timelineControl.Seek(VoyagerDevice.DeviceMotionProfileTime);
						}
					}
					else if( VoyagerDevice.MotionProfile == "C" )
					{
						if( currentTrack != 2 )
						{
							currentTrack = 2;
							switchTrack.director.Stop();
							switchTrack.Switch(2);

							VoyagerDevice.SetMotionProfile("C");

							timelineControl.director = switchTrack.director;
							timelineControl.Seek(VoyagerDevice.DeviceMotionProfileTime);
						}
					}
				}

				timelineControl.director = switchTrack.director;
			}
			else
			{
				Debug.LogError("Need a SwitchTrack component with a Playable Director");
			}

			// debugText.text = "Current Path: " + path + " Interface URL: " + Interface.currentUrl + " Interface Paused: " + Interface.paused + " Interface Recenter: " + Interface.recenter;

			// debugText.text = "Current State: " + System.Enum.GetName(typeof(Interface.State), Interface.state) + " " + Interface.GetLatestUDPPacket();

			// debugText.text = "Current State: " + System.Enum.GetName(typeof(Interface.State), Interface.state) + " Mute: " + Interface.mute;

			if( motionProfile != null )
			{
				debugText.text = "Received Time: " + VoyagerDevice.DeviceMotionProfileTime + " Current Motion Profile: " + motionProfile.profileName + " Current Playable Time: " + ((int)(switchTrack.director.time * 1000));
			}
		}
	}
}
