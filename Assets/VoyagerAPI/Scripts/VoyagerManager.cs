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
		public TimelineControl timelineControl;

		// Path for this application's executable
		[ SerializeField ]
		private string path;

		// Return timeScale to previous saved value since we are overriding it
		private float previousTimeScale = 1f;

		// Simulated video/experience play head time.
		private float experienceTime = 0.0f;

		// Screen fading script
		private ScreenFader screenFader;

		// Fade in the screen
		public void ScreenFadeIn()
		{
			if( screenFader != null )
			{
				screenFader.fadeIn = true;
			}
		}

		// Fade out the screen
		public void ScreenFadeOut()
		{
			if( screenFader != null )
			{
				screenFader.fadeIn = false;
			}
		}

		public void SetFullScreen()
		{
			if( !Screen.fullScreen )
			{
				Screen.SetResolution(Screen.currentResolution.width, Screen.currentResolution.height, true);
			}
			else
			{
				Screen.SetResolution(1024, 768, false);
			}
		}

		// Mute audio
		public void Mute(bool muteOn)
		{
			if( VoyagerDevice.IsMuted != muteOn )
			{
				VoyagerDevice.ToggleMute();
			}
		}

		public void ToggleMute()
		{
			VoyagerDevice.ToggleMute();
		}

		// Recenter the HMD
		public void Recenter()
		{
			VoyagerDevice.Recenter();
		}

		public void PlayPause()
		{
			VoyagerDevice.PlayPause();
		}

		public void Play()
		{
			if( VoyagerDevice.PlayState == VoyagerDevicePlayState.Pause )
			{
				VoyagerDevice.PlayPause();
			}
		}

		public void Pause()
		{
			if( VoyagerDevice.PlayState == VoyagerDevicePlayState.Play )
			{
				VoyagerDevice.PlayPause();
			}
		}

		public void SeekForward10()
		{
			experienceTime += 10;
		}

		public void SeekBack10()
		{
			experienceTime = Mathf.Max( 0f, experienceTime - 10f );
		}

		public void SeekForward30()
		{
			experienceTime += 30;

			if( timelineControl )
			{
			}
		}

		public void SeekBack30()
		{
			experienceTime = Mathf.Max( 0f, experienceTime - 30f );

			if( timelineControl )
			{
			}
		}

		void Awake()
		{
			DontDestroyOnLoad( this );

			timelineControl = GetComponent<TimelineControl>();

			// Init HMD
			if( UnityEngine.XR.XRDevice.isPresent && UnityEngine.XR.XRSettings.enabled )
			{
				UnityEngine.XR.XRDevice.SetTrackingSpaceType(UnityEngine.XR.TrackingSpaceType.Stationary);

				UnityEngine.XR.InputTracking.Recenter();
			}
		}

		IEnumerator Start()
		{
			screenFader = FindObjectOfType<ScreenFader>();

			// ~===============================================
			// Initialize Voyager API

			// Make sure we have an instance of the Positron interface before we do anything else.
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

#if !UNITY_EDITOR
			// Set the path to this executable so Voyager knows what application is playing
			path = System.IO.Directory.GetParent(Application.dataPath).FullName;
			string[] executables = System.IO.Directory.GetFiles(path, "*.exe");

			// Load the content if there is an executable
			if( executables != null && executables.Length > 0 )
			{
				VoyagerDevice.LoadContent(executables[ 0 ]);
			}
			else
			{
				Debug.LogError("No executable found at " + path);
				yield break;
			}
#else
			// Set the Content ID.
			VoyagerDevice.LoadContent(path);
#endif

			// Notify PSM that loading is complete.
			VoyagerDevice.Loaded(true);

			// Set the initial Motion Profile track name.
			if( timelineControl != null && timelineControl.HasTrackSetups )
			{
				var currDef = timelineControl.CurrentTrackDefinition;
				VoyagerDevice.SetMotionProfile( currDef.motionProfile );
			}
			else
			{
				VoyagerDevice.SetMotionProfile( "TestProfile" );
			}

			// ~===============================================
			// Bind Events

			VoyagerDevice.OnPlayStateChange += OnVoyagerPlayStateChange;
			VoyagerDevice.OnPlay += OnVoyagerPlay;
			VoyagerDevice.OnPaused += OnVoyagerPaused;
			VoyagerDevice.OnStopped += OnVoyagerStopped;
			VoyagerDevice.OnMuteToggle += OnVoyagerToggleMute;
			VoyagerDevice.OnRecenter += OnVoyagerRecenterHMD;
		}

		void OnVoyagerPlayStateChange( VoyagerDevicePlayState InState )
		{
			switch( InState )
			{
				case VoyagerDevicePlayState.Play:
				{
					Time.timeScale = previousTimeScale;

					AudioListener.volume = 1f;

					break;
				}

				case VoyagerDevicePlayState.Pause:
				{
					if( Time.timeScale != 0f )
					{
						previousTimeScale = Time.timeScale;
					}

					Time.timeScale = 0f;

					AudioListener.volume = 0f;

					break;
				}

				case VoyagerDevicePlayState.Stop:
				{
					Time.timeScale = previousTimeScale;

					ExitApp();

					break;
				}
			}
		}

		void OnVoyagerPlay()
		{
			// React to Play state event.
		}

		void OnVoyagerPaused()
		{
			// React to Paused state event.
		}

		void OnVoyagerStopped()
		{
			// React to Stop state event.
		}

		void OnVoyagerToggleMute( bool InValue )
		{
			AudioListener.volume = InValue ? 0f : 1f;
		}

		void OnVoyagerRecenterHMD()
		{
			VoyagerDevice.Recenter();
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

			// If not using Motion Profiles with Timeline, we must send Time to PSM.
			if( timelineControl == null )
			{
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
			}
			else
			{
				experienceTime = timelineControl.GetTime();
				VoyagerDevice.SendTimeSeconds(experienceTime);
			}
		}

		// Exit the application
		public void ExitApp()
		{
			VoyagerDevice.Stop();

			ScreenFadeOut();

			StartCoroutine(DoQuitApp());
		}

		// Screen fade out if it is available and quit the application
		IEnumerator DoQuitApp()
		{
			Time.timeScale = 1f;
			if( screenFader != null )
			{
				yield return new WaitForSeconds(screenFader.fadeTime);
			}

			Debug.Log( "~ APP QUIT ~" );
			Application.Quit();
		}
	}
}
