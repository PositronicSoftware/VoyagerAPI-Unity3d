/* Copyright 2017 Positron code by Brad Nelson */

using System.Collections;
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
			if( XRDevice.isPresent && XRSettings.enabled )
			{
				XRDevice.SetTrackingSpaceType(TrackingSpaceType.Stationary);

				InputTracking.Recenter();
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
			if( !VoyagerDevice.IsInitialized )
			{
				Debug.LogError("VoyagerDevice not initialized.");
				yield break;
			}

			// Set the Content Params.
			VoyagerDevice.SetContent("Application", "Windows", "Voyager VR Demo", "1.0");

			// Experience should start in Paused state.
			VoyagerDevice.Pause();

			// Set the Content ID.
			VoyagerDevice.LoadContent("file:///C:/Test/Test.mp4");

			// Notify PSM that loading is complete.
			VoyagerDevice.Loaded(true);

			// Set the initial Motion Profile track name.
			VoyagerDevice.SetMotionProfile( "TestProfile" );

			configText.text = VoyagerDevice.Config.ToString();
		}

		void Update()
		{
			if( VoyagerDevice.Instance == null || !VoyagerDevice.IsInitialized )
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
