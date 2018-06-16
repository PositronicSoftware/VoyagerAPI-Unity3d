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

		// Simulated video/content playhead time, not using timescale since gazeui uses game time.
		private float playheadTime = 0.0f;

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

		public void Stop()
		{
			VoyagerDevice.Stop();
		}

		void Awake()
		{
			// Add the Interface to this object so it doesn't get destroyed on load
			if( GetComponent<VoyagerDevice>() == null )
			{
				gameObject.AddComponent<VoyagerDevice>();
			}

			if( XRDevice.isPresent
				&& XRSettings.enabled )
			{
				XRDevice.SetTrackingSpaceType(TrackingSpaceType.Stationary);

				InputTracking.Recenter();
			}
		}

		// Use this for initialization
		IEnumerator Start()
		{
			// Make sure we have an instance of the Positron interface before we do anything else
			while( VoyagerDevice.Instance == null )
			{
				yield return null;
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

			// Set the Positron Interface content here so it is only receiving data for this application
			VoyagerDevice.SetContent("Application", "Windows", "Voyager VR Demo", "1.0");

			VoyagerDevice.LoadContent("file:///C:/Test/Test.mp4");
			VoyagerDevice.Pause();
			VoyagerDevice.Loaded(true);

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

			if( VoyagerDevice.PlayState == VoyagerDevicePlayState.Play )
			{
				playheadTime += Time.deltaTime;
				VoyagerDevice.SendTimeSeconds(playheadTime);
			}
			else if( VoyagerDevice.PlayState == VoyagerDevicePlayState.Stop )
			{
				if( !Mathf.Approximately(playheadTime, 0.0f))
				{
					playheadTime = 0.0f;
					VoyagerDevice.SendTimeSeconds(0.0f);
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
