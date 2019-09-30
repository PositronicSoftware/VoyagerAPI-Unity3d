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
		[ Header("Info") ]

		public Text stateText;
		public Text configText;
		public Text deviceDataText;
		public Text lastRecvDataText;

		[ Header("Input") ]

		public float pitch = 0f;
		public float pitchSpeed = 10f;
		public float pitchAccel = 10f;
		public float pitchDecel = 5f;
		public float yaw = 0f;
		public float yawSpeed = 10f;
		public float yawAccel = 10f;
		public float yawDecel = 5f;
		public Text inputTypeLabel;
		public Text pitchLabel;
		public Text yawLabel;

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

		public void Stop()
		{
			VoyagerDevice.Stop();
		}

		public void CycleInputType()
		{
			if( VoyagerDevice.PlayState != VoyagerDevicePlayState.Stop )
			{
				VoyagerDeviceInputType nextMode = (VoyagerDeviceInputType)(((int)VoyagerDevice.InputType + 1) % 3);
				VoyagerDevice.SetInputType(nextMode);

				if( nextMode != VoyagerDeviceInputType.Lite )
				{
					VoyagerDevice.SetPitchParams(pitch, pitchSpeed, pitchAccel, pitchDecel);
					VoyagerDevice.SetYawParams(yaw, yawSpeed, yawAccel, yawDecel);
				}
			}

			if( inputTypeLabel )
			{
				inputTypeLabel.text = VoyagerDevice.InputType.ToString().ToUpper();
			}
		}

		public void PitchUp5()
		{
			PitchTo(pitch + 5);
		}

		public void PitchDown5()
		{
			PitchTo(pitch - 5);
		}

		public void PitchTo(float pitchDegrees)
		{
			if( VoyagerDevice.PlayState != VoyagerDevicePlayState.Stop )
			{
				pitch = pitchDegrees;

				VoyagerDevice.SetPitch(pitch);
			}

			if( pitchLabel )
			{
				pitchLabel.text = pitch.ToString("F1");
			}
		}

		public void YawRight10()
		{
			YawTo(yaw + 10);
		}

		public void YawLeft10()
		{
			YawTo(yaw - 10);
		}

		public void YawTo(float yawDegrees)
		{
			if( VoyagerDevice.PlayState != VoyagerDevicePlayState.Stop )
			{
				yaw = yawDegrees;

				VoyagerDevice.SetYaw(yaw);
			}

			if( yawLabel )
			{
				yawLabel.text = yaw.ToString("F1");
			}
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

			// ~===============================================
			// Update UI

			if( inputTypeLabel )
			{
				inputTypeLabel.text = VoyagerDevice.InputType.ToString().ToUpper();
			}
			if( pitchLabel )
			{
				pitchLabel.text = pitch.ToString("F1");
			}
			if( yawLabel )
			{
				yawLabel.text = yaw.ToString("F1");
			}
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

			if( Input.GetKey(KeyCode.Comma))	// Pitch or Yaw
			{
				if( Input.GetKey(KeyCode.LeftShift))// Pitch
				{
					PitchTo(pitch + (8f * Time.deltaTime));
				}
				else
				{
					YawTo(yaw - (24f * Time.deltaTime));
				}
			}

			if( Input.GetKey(KeyCode.Period))	// Pitch or Yaw
			{
				if( Input.GetKey(KeyCode.LeftShift))// Pitch
				{
					PitchTo(pitch - (8f * Time.deltaTime));
				}
				else
				{
					YawTo(yaw + (24f * Time.deltaTime));
				}
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

			if( deviceDataText )
			{
				deviceDataText.text = deviceStateStr;
			}
			if( lastRecvDataText )
			{
				lastRecvDataText.text = lastRecvDataStr;
			}
			if( stateText )
			{
				stateText.text = string.Format("Voyager state '{0}'", VoyagerDevice.PlayState.ToString());
			}
		}
	}
}
