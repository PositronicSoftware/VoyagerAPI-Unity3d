/* Copyright 2017 Positron code by Brad Nelson */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;

namespace Positron
{
	/* Tests sending and receiving commands with VoyagerAPI. */
	public class VoyagerAPITest : MonoBehaviour
	{
		public Text stateText;
		public Text configText;
		public Text deviceDataText;
		public Text lastRecvDataText;

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

		void Awake()
		{
			DontDestroyOnLoad( this );

			// Init HMD
			if ( XRSettings.enabled )
			{
				#if UNITY_2019_3_OR_NEWER
				if ( VoyagerDevice.IsPresent())
				{
					List<XRInputSubsystem> xrInputSubsystems = new List<XRInputSubsystem>();
					SubsystemManager.GetInstances<XRInputSubsystem>(xrInputSubsystems);

					for (int i = 0; i < xrInputSubsystems.Count; i++)
					{
						if (xrInputSubsystems[i] != null) {
							xrInputSubsystems[i].TrySetTrackingOriginMode(TrackingOriginModeFlags.Device);
							xrInputSubsystems[i].TryRecenter();
						}
					}
				}
				#else
				if ( XRDevice.isPresent )
				{
					XRDevice.SetTrackingSpaceType(TrackingSpaceType.Stationary);
					InputTracking.Recenter();
				}
				#endif
			}
		}

		IEnumerator Start()
		{
			// Make sure we have an instance of the Positron interface before we do anything else
			var voyagerDevice = VoyagerDevice.Instance;
			if( voyagerDevice == null )
			{
				Debug.LogError("Failed to create VoyagerDevice Singleton.");
				yield break;
			}

			// Load config from file
			VoyagerDeviceConfig config = VoyagerDeviceUtils.LoadDeviceConfigFile("Config", "InterfaceConfig.json");

			// Initialize interface
			VoyagerDevice.Init(config);

            // Quick Exit: Not initialized
            if ( !VoyagerDevice.IsInitialized )
			{
				Debug.LogError("VoyagerDevice not initialized.");
				yield break;
			}

            // Critical setup calls must be done when connected
            VoyagerDevice.OnConnected += OnVoyagerConnected;
            VoyagerDevice.OnDisconnected += OnVoyagerDisconnected;
			VoyagerDevice.OnContentChange += OnVoyagerContentChange;
        }
        private void OnVoyagerConnected()
        {
            // Set the Content Params.
            VoyagerDevice.SetContent("Application", "Windows", "Voyager VR Demo", "1.0");

            // Experience should start in Paused state.
            VoyagerDevice.Pause();

            configText.text = VoyagerDevice.Config.ToString();
        }

		private void OnVoyagerContentChange(string inUrl)
		{
            // Set the Content ID.
            VoyagerDevice.LoadContent(inUrl);

            // Notify PSM that loading is complete.
            VoyagerDevice.Loaded(true);

            // Set the initial Motion Profile track name.
            VoyagerDevice.SetMotionProfile("TestProfile");
        }

		private void OnVoyagerDisconnected()
		{
            // Update the device state. This will log a warning since there is no connection.
            VoyagerDevice.Pause();

            // Since there is a guard for IsConnected in Update, we need to call UpdateText otherwise the text won't reflect the current paused state.
            UpdateText();
        }

        private void OnDestroy()
        {
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
					if( !Mathf.Approximately(experienceTime, 0.0f))
					{
						experienceTime = 0.0f;
						VoyagerDevice.SendTimeSeconds(0.0f);
					}
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
