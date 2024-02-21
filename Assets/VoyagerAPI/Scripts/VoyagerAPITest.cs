/* Copyright 2017 Positron code by Brad Nelson */

using System.Collections;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.UI;

namespace Positron
{
	/* Tests sending and receiving commands with VoyagerAPI. */
	public class VoyagerAPITest : MonoBehaviour
	{
		public Text stateText;
		public Text configText;
		public Text deviceDataText;
		public Text lastRecvDataText;

		public XROrigin XROrigin;

		// Simulated video/experience play head time.
		private float experienceTime = 0.0f;

		public void ToggleMute()
		{
			VoyagerDevice.ToggleMute();
		}

		public void FastForward()
		{
			VoyagerDevice.FastForward();
		}

		public void Rewind()
		{
			VoyagerDevice.Rewind();
		}

		public void Recenter()
		{
			VoyagerDevice.Recenter();
		}

		public void PlayPause()
		{
			VoyagerDevice.PlayPause();
		}

		public void Idle()
		{
			VoyagerDevice.Idle();
		}

		public void Stop()
		{
			VoyagerDevice.Stop();
		}

		IEnumerator Start()
		{

            // Make sure we have an instance of the Positron interface before we do anything else
            var voyagerDevice = VoyagerDevice.Instance;
			if (voyagerDevice == null)
			{
				Debug.LogError("Failed to create VoyagerDevice Singleton.");
				yield break;
			}

			// Load config from file
			VoyagerDeviceConfig config = VoyagerDeviceUtils.LoadDeviceConfigFile("Config", "InterfaceConfig.json");

			// Initialize interface
			VoyagerDevice.Init(config);

			// Quick Exit: Not initialized
			if (!VoyagerDevice.IsInitialized)
			{
				Debug.LogError("VoyagerDevice not initialized.");
				yield break;
			}

			// Setup calls must be done when connected
			VoyagerDevice.OnConnected += OnVoyagerConnected;
			VoyagerDevice.OnDisconnected += OnVoyagerDisconnected;
			VoyagerDevice.OnContentChange += OnVoyagerContentChange;


			VoyagerDevice.OnRecenter += OnVoyagerRecenter;
			VoyagerDevice.OnUserPresentToggle += OnVoyagerUserPresentToggle;
		}

		private void OnVoyagerConnected()
		{
			// Set the Content Params.
			VoyagerDevice.SetContent("Application", "Quest 3", "Voyager API Test", "1.0");

			// Media players should start in Stopped state.
			VoyagerDevice.Stop();

			configText.text = VoyagerDevice.Config.ToString();
		}

		private void OnVoyagerContentChange(string url)
		{
			// Set the Content ID. Send back the same url as a confirmation to avoid errors
			VoyagerDevice.LoadContent(url);

			// Notify PSM that loading is complete.
			VoyagerDevice.Loaded(true);

			// Set the initial Motion Profile track name.
			VoyagerDevice.SetMotionProfile("TestProfile");
		}

		private void OnVoyagerDisconnected()
		{
			// This will log a warning since there is no connection.
			VoyagerDevice.Pause();

			// Update UI to show the paused state.
			UpdateText();
		}

		private void OnVoyagerRecenter()
		{
			// Quest 3 apps running on android (vs link) only allow recentering from a user. Any recenter api calls are no-op. 
			// (https://forum.unity.com/threads/xr-recenter-not-working-in-oculus-quest-2.1129019/#post-7268662)

			// Undo any y rotation 
			float currentYRotation = Camera.main.transform.eulerAngles.y;
			XROrigin.transform.Rotate(0, -currentYRotation, 0);
		}


		private void OnVoyagerUserPresentToggle(bool isUserPresent)
		{
			// Adjust height when user puts on headset
			if (isUserPresent) 
			{
				Vector3 cameraPosition = XROrigin.Camera.transform.localPosition;
				XROrigin.Origin.transform.position = new Vector3(0, -cameraPosition.y, 0);
			}
        }

        private void OnDestroy()
        {
			VoyagerDevice.OnUserPresentToggle -= OnVoyagerUserPresentToggle;
			VoyagerDevice.OnRecenter -= OnVoyagerRecenter;
			VoyagerDevice.OnConnected -= OnVoyagerConnected;
            VoyagerDevice.OnDisconnected -= OnVoyagerDisconnected;
			VoyagerDevice.OnContentChange -= OnVoyagerContentChange;

        }

        void Update()
		{
			if( VoyagerDevice.Instance == null || !VoyagerDevice.IsInitialized || !VoyagerDevice.IsConnected)
			{
				return;
			}

			// Recenter for Oculus Remote DPad
			if( Input.GetAxis("Vertical") >= 1.0f )
			{
				Recenter();
			}

			// ~===============================================
			// Key Commands

			if( Input.GetKeyDown( KeyCode.Space ))		// Play-Pause
			{
				VoyagerDevice.PlayPause();
			}

			if( Input.GetKeyDown( KeyCode.R ))			// Recenter HMD
			{
				Recenter();
			}

			if( Input.GetKeyDown( KeyCode.RightArrow ))	// Skip ahead 10sec
			{
				experienceTime += 10f;
			}

			if( Input.GetKeyDown( KeyCode.LeftArrow ))	// Skip back 10sec
			{
				experienceTime = Mathf.Max( 0f, experienceTime - 10f );
			}

			if( Input.GetKeyDown( KeyCode.UpArrow ))	// Skip ahead 30sec
			{
				experienceTime += 30f;
			}

			if( Input.GetKeyDown( KeyCode.DownArrow ))	// Skip back 30sec
			{
				experienceTime = Mathf.Max( 0f, experienceTime - 30f );
			}

			// ~===============================================
			// Send Experience Time to PSM.

			switch( VoyagerDevice.PlayState )
			{
				case VoyagerDevicePlayState.Play:
				{
					experienceTime += Time.deltaTime;
					VoyagerDevice.SendTimeSeconds(experienceTime);
					break;
				}

				case VoyagerDevicePlayState.Pause:
				{
					VoyagerDevice.SendTimeSeconds(experienceTime);
					break;
				}

				case VoyagerDevicePlayState.Stop:
				{
					experienceTime = 0.0f;
					VoyagerDevice.SendTimeSeconds(experienceTime);
					break;
				}
			}
			UpdateText();
		}
		void UpdateText()
		{
            string deviceStateStr;
            string lastRecvDataStr;
            VoyagerDeviceUtils.DevicePacketToJson(VoyagerDevice.deviceState, out deviceStateStr);
            VoyagerDeviceUtils.DevicePacketToJson(VoyagerDevice.LastRecvDevicePacket, out lastRecvDataStr);

            deviceDataText.text = deviceStateStr;
            lastRecvDataText.text = lastRecvDataStr;
            stateText.text = string.Format("Voyager state '{0}'", VoyagerDevice.PlayState.ToString());
        }
	}
}
