/* Copyright 2017 Positron code by Brad Nelson */

using System.Collections;
using System.IO;
using UnityEngine;

namespace Positron
{
	/* This class is used to initialize the Voyager Interface
	 * and execute the commands from the Voyager Interface*/

	public class VoyagerManager : MonoBehaviour
	{
		// Return timeScale to previous saved value since we are overriding it
		private float previousTimeScale = 1f;

		// Path for this application's executable
		private string path;

		// Bool to check to see if audio is muted
		private bool _mute = false;

		// Screen fading script
		private ScreenFader screenFader;

		// Use this to check to see if the Interface has been initialized
		private bool initializedInterface = false;

		// Fade out the screen
		public void ScreenFadeOut()
		{
			if( screenFader != null )
			{
				screenFader.fadeIn = false;
			}
		}

		// Fade in the screen
		public void ScreenFadeIn()
		{
			if( screenFader != null )
			{
				screenFader.fadeIn = true;
			}
		}

		// Mute the audio
		public void Mute(bool mute)
		{
			_mute = mute;
			if( _mute )
			{
				AudioListener.volume = 0f;
			}
			else
			{
				AudioListener.volume = 1f;
			}
			// Debug.Log("VoyagerManager Mute " + _mute);
		}

		// Recenter the HMD
		public void Recenter()
		{
			if( UnityEngine.XR.XRDevice.isPresent )
			{
				UnityEngine.XR.InputTracking.Recenter();
				// Debug.Log("HMD Recentered.");
			}
		}

		// Toggle between play and pause
		public void PlayPause()
		{
			VoyagerDevice.PlayPause();
			if( VoyagerDevice.IsPaused )
			{
				if( Time.timeScale != 0f )
				{
					previousTimeScale = Time.timeScale;
				}

				Time.timeScale = 0f;
			}
			else
			{
				Time.timeScale = previousTimeScale;
			}
		}

		// Screen fade out if it is available and quit the application
		IEnumerator QuitApp()
		{
			Time.timeScale = 1f;
			if( screenFader != null )
			{
				yield return new WaitForSeconds(screenFader.fadeTime);
			}

			VoyagerDevice.Stop();
			Application.Quit();
		}

		// Exit the application
		public void ExitApp()
		{
			ScreenFadeOut();

			StartCoroutine(QuitApp());
		}

		string LoadTextFile(string fileName)
		{
			string t = "";
			string line = "-";
			try
			{
				StreamReader sr = new StreamReader(Application.streamingAssetsPath + "/" + fileName + ".txt");
				line = sr.ReadLine();
				while( line != null )
				{
					t += line;
					line = sr.ReadLine();
					if( line != null )
					{
						t += "\n";
					}
				}
				sr.Close();
				// Debug.Log(t);
			}
			catch( System.Exception e )
			{
				print("Error: " + Application.streamingAssetsPath + "/" + fileName);
			}
			return t;
		}

		void Awake()
		{
			// Add the Interface to this object so it doesn't get destroyed on load
			if( GetComponent<VoyagerDevice>() == null )
			{
				gameObject.AddComponent<VoyagerDevice>();
			}

			VoyagerDeviceConfig deviceConfig = VoyagerDeviceUtils.LoadDeviceConfigFile("Config", "InterfaceConfig.json", VoyagerDeviceConfigId.VDC_Default);

			VoyagerDevice.Init(deviceConfig);

			if( UnityEngine.XR.XRDevice.isPresent
				&& UnityEngine.XR.XRSettings.enabled )
			{
				UnityEngine.XR.XRDevice.SetTrackingSpaceType(UnityEngine.XR.TrackingSpaceType.Stationary);

				UnityEngine.XR.InputTracking.Recenter();
			}
		}

		// Use this for initialization
		IEnumerator Start()
		{
			// Make sure we have an instance of the Positron interface before we do anything else
			while( VoyagerDevice.Instance == null || !VoyagerDevice.IsInitialized )
			{
				yield return null;
			}

			// Set the Positron Interface content here so it is only receiving data for this application
			VoyagerDevice.SetContent("Application", "Windows", "Voyager VR Demo", "1.0");

			// Set the path to this executable so Voyager knows what application is playing
			path = System.IO.Directory.GetParent(Application.dataPath).FullName;
			string[] executables = System.IO.Directory.GetFiles(path, "*.exe");

			if( executables != null && executables.Length > 0 )
			{
				// Load the content if there is an executable
				VoyagerDevice.LoadContent(executables[ 0 ]);
				PlayPause();
				VoyagerDevice.Loaded(true);
			}
			else
			{
				Debug.Log("No executable found at " + path);
			}

			screenFader = FindObjectOfType<ScreenFader>();

			// Start playing motion in Voyager - Positron Interface call to start motion //
			// Interface.MotionProfile("Voyager Demo Start Motion Profile");
			VoyagerDevice.Play();

			initializedInterface = true;

			// Any other game related code to start the game can go after Voyager is initialized
		}

		// Update is called once per frame
		void Update()
		{
			// Make sure we have an instance of the Positron interface before we do anything else
			if( VoyagerDevice.Instance == null )
			{
				return;
			}

			// Recenter for Oculus Remote DPad
			if( Input.GetAxis("Vertical") >= 1.0f )
			{
				Recenter();
			}

			// If we have the Interface and it has been updated and initialized then run commands from it
			if( VoyagerDevice.Instance != null && VoyagerDevice.IsUpdated && VoyagerDevice.IsInitialized )
			{
				// Play the content here on Interface Play state
				if( VoyagerDevice.PlayState == VoyagerDevicePlayState.Play )
				{
					if( Time.timeScale == 0f )
					{
						Time.timeScale = previousTimeScale;
					}
				}
				// Pause the content here on Interface Pause state
				else if( VoyagerDevice.PlayState == VoyagerDevicePlayState.Pause )
				{
					if( Time.timeScale != 0f )
					{
						previousTimeScale = Time.timeScale;
					}

					Time.timeScale = 0f;

					if( !VoyagerDevice.IsMuted )
					{
						VoyagerDevice.ToggleMute();
					}

					Mute(true);
				}
				// Quit the content on Interface Stop state
				else if( VoyagerDevice.PlayState == VoyagerDevicePlayState.Stop )
				{
					ExitApp();
				}

				// Mute the sound if the Interface is set to Mute
				Mute(VoyagerDevice.IsMuted);
			}

			/* On use this next part if not using Motion Profiles with Timeline
			 * // If the Interface is initialized then start sending time
			 * // to go with the motion profiles
			 * if (initializedInterface) {
			 *  Interface.SendTime();
			 *  // Debug.Log("Sending time");
			 * }
			 * // Send the data to the Interface otherwise to set it up
			 * else {
			 *  Interface.SendData();
			 *  // Debug.Log("Sending Data");
			 * }*/
		}
	}
}
