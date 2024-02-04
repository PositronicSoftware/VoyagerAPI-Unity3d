/* Copyright 2017 Positron code by Brad Nelson */

using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.XR;

namespace Positron
{
	public enum VoyagerMangerStartMode { StartPaused, StartIdle }

	/* This class is used to initialize the Voyager Interface
	 * and execute the commands from the Voyager Interface*/
	public class VoyagerManager : MonoBehaviour
	{
		[ Header("Initialization") ]

		// State to initialize the Voyager API in.
		public VoyagerMangerStartMode startMode = VoyagerMangerStartMode.StartPaused;

		// Tracking space types, Stationary generally refers to the Device tracking origin mode, 
		// and RoomScale generally refers to Floor tracking origin mode, but it can depend somewhat on the intricacies of the SDK
		public TrackingOriginModeFlags XRSpace = TrackingOriginModeFlags.Device;

		[Header("Content Path")]
		public bool singleExperienceExecutable = false;
		
		[ SerializeField, Tooltip("Path for this application's executable, if it's a single experience executable")]
        private string path = "C:/ExecutableName.exe";

		[ Header("Timeline") ]

		public TimelineControl timelineControl = null;

		[ Header("Optimization") ]

		[ Tooltip("Enable optimizations to reduce memory allocation per frame.") ]
		public bool optimizeSendTime;

		[ Range(1, 30), Tooltip("Stagger SendTime() updates to Voyager to reduce JSON parse mem-alloc.\n'N' == SendTime() every Nth frame.\nRequires 'OptimizeMemAlloc' ON to work.") ]
		public int voyagerSendTimeInterval = 1;

		// Return timeScale to previous saved value since we are overriding it
		private float previousTimeScale = 1f;

		// Simulated video/experience play head time.
		private float experienceTime = 0.0f;

		// Screen fading script
		private ScreenFader screenFader;

		private float lastTrackSwitchTime = 0f;
		private float lastTrackSwitchDelay = 1f;

		static private List<XRInputSubsystem> xrInputSubsystems = new List<XRInputSubsystem>();
		static private bool recenteredHMD = false;

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
			recenteredHMD = TryRecenter();
			if (!recenteredHMD) {
				Debug.LogWarning("HMD not recentered, try again");
			}
			VoyagerDevice.Recenter();
		}

		public void PlayPause()
		{
			if( VoyagerDevice.PlayState != VoyagerDevicePlayState.Stop )
			{
				VoyagerDevice.PlayPause();
			}
		}

		public void Play()
		{
			if( VoyagerDevice.PlayState != VoyagerDevicePlayState.Stop )
			{
				VoyagerDevice.Play();
			}
		}

		public void Pause()
		{
			if( VoyagerDevice.PlayState != VoyagerDevicePlayState.Stop )
			{
				VoyagerDevice.Pause();
			}
		}

		public void Idle()
		{
			if( VoyagerDevice.PlayState != VoyagerDevicePlayState.Idle )
			{
				VoyagerDevice.Idle();
			}
		}

		public void SeekForward10()
		{
			if( VoyagerDevice.PlayState != VoyagerDevicePlayState.Stop )
			{
				experienceTime += 10;

				if( timelineControl )
				{
					experienceTime = timelineControl.SeekToSeconds( experienceTime );
				}
			}
		}

		public void SeekBack10()
		{
			if( VoyagerDevice.PlayState != VoyagerDevicePlayState.Stop )
			{
				experienceTime = Mathf.Max( 0f, experienceTime - 10f );

				if( timelineControl )
				{
					experienceTime = timelineControl.SeekToSeconds( experienceTime );
				}
			}
		}

		public void SeekForward30()
		{
			if( VoyagerDevice.PlayState != VoyagerDevicePlayState.Stop )
			{
				experienceTime += 30;

				if( timelineControl )
				{
					experienceTime = timelineControl.SeekToSeconds( experienceTime );
				}
			}
		}

		public void SeekBack30()
		{
			if( VoyagerDevice.PlayState != VoyagerDevicePlayState.Stop )
			{
				experienceTime = Mathf.Max( 0f, experienceTime - 30f );

				if( timelineControl )
				{
					experienceTime = timelineControl.SeekToSeconds( experienceTime );
				}
			}
		}

		public void NextTrack()
		{
			if( VoyagerDevice.PlayState != VoyagerDevicePlayState.Stop )
			{
				if( timelineControl )
				{
					lastTrackSwitchTime = Time.realtimeSinceStartup;
					timelineControl.NextTrack();
				}
				else
				{
					Debug.LogError("No TimelineControl component set.");
				}
			}
		}

		public void PreviousTrack()
		{
			if( VoyagerDevice.PlayState != VoyagerDevicePlayState.Stop )
			{
				if( timelineControl )
				{
					lastTrackSwitchTime = Time.realtimeSinceStartup;
					timelineControl.PreviousTrack();
				}
				else
				{
					Debug.LogError("No TimelineControl component set.");
				}
			}
		}

		public void SetPlayableTrack( int trackIndex )
		{
			if( VoyagerDevice.PlayState != VoyagerDevicePlayState.Stop )
			{
				if( timelineControl )
				{
					lastTrackSwitchTime = Time.realtimeSinceStartup;
					timelineControl.SwitchPlayableTrack( trackIndex );
				}
				else
				{
					Debug.LogError("No TimelineControl component set.");
				}
			}
		}

		public bool TryRecenter() {
			if (!XRSettings.enabled) {
				return false;
			}

			if ( VoyagerDevice.IsPresent())
			{
				bool recentered = false;
				xrInputSubsystems.Clear();
				SubsystemManager.GetInstances<XRInputSubsystem>(xrInputSubsystems);

				for (int i = 0; i < xrInputSubsystems.Count; i++)
				{
					bool tryRecenter = false;
					if (xrInputSubsystems[i] != null) {
						xrInputSubsystems[i].TrySetTrackingOriginMode(XRSpace);
						tryRecenter = xrInputSubsystems[i].TryRecenter();
					}

					if (tryRecenter) {
						recentered = true;
					}
				}

				return recentered;
			}

			return false;
		}

		void Awake()
		{
			DontDestroyOnLoad( this );

			if( timelineControl == null )
			{
				timelineControl = GetComponent<TimelineControl>();
			}

			// Init HMD
			TryRecenter();
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
            if ( !VoyagerDevice.IsInitialized )
			{
				Debug.LogError("VoyagerDevice not initialized.");
				yield break;
			}

            // Wait until we are connected.
            yield return new WaitUntil(() => VoyagerDevice.IsConnected);

			// Calling this before events are hooked up to retain old behavior
			OnVoyagerConnected();

            // ~===============================================
            // Bind Events

            VoyagerDevice.OnPlayStateChange += OnVoyagerPlayStateChange;
			VoyagerDevice.OnPlay += OnVoyagerPlay;
			VoyagerDevice.OnPaused += OnVoyagerPaused;
			VoyagerDevice.OnStopped += OnVoyagerStopped;
			VoyagerDevice.OnMuteToggle += OnVoyagerToggleMute;
			VoyagerDevice.OnRecenter += OnVoyagerRecenterHMD;
			VoyagerDevice.OnUserPresentToggle += OnVoyagerUserPresentToggle;
			VoyagerDevice.OnSixDofPresentToggle += OnVoyagerSixDofTrackingToggle;

            // Setup calls must be done when connected/on content change
            VoyagerDevice.OnConnected += OnVoyagerConnected;
            VoyagerDevice.OnDisconnected += OnVoyagerDisconnected;
            VoyagerDevice.OnContentChange += OnVoyagerContentChange;
        }
        
		private void OnVoyagerConnected()
		{
            // Set the Content Params.
            VoyagerDevice.SetContent("Application", "Windows", "Voyager VR Demo", "1.0");

            // Set the Voyager start state.
            switch (startMode)
            {
                case VoyagerMangerStartMode.StartIdle:
                    {
                        VoyagerDevice.Idle();
                        break;
                    }
                case VoyagerMangerStartMode.StartPaused:
                    {
                        VoyagerDevice.Pause();
                        break;
                    }
                default:
                    {
                        Debug.LogError("Unhandled Start Mode " + startMode);
                        break;
                    }
            }

            if (singleExperienceExecutable)
			{
				// Set the Content ID.
				VoyagerDevice.LoadContent(path);

				// Notify PSM that loading is complete.
				VoyagerDevice.Loaded(true);

				// Set the initial Motion Profile track name.
				if(timelineControl != null && timelineControl.HasTrackSetups )
				{
					var currDef = timelineControl.CurrentTrackDefinition;
					VoyagerDevice.SetMotionProfile(currDef.motionProfile );
				}
				else
				{
					VoyagerDevice.SetMotionProfile( "TestProfile" );
				}
			}
		}

        private void OnVoyagerContentChange(string inUrl)
        {
			if (!singleExperienceExecutable)
			{
				// Set the Content ID.
				VoyagerDevice.LoadContent(inUrl);

				// Notify PSM that loading is complete.
				VoyagerDevice.Loaded(true);

				// Set the Motion Profile
                VoyagerDevice.SetMotionProfile("A");
            }
        }

        private void OnVoyagerDisconnected()
        {
            // Update the device state. This will log a warning since there is no connection.
            VoyagerDevice.Pause();
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
			if( timelineControl )
			{
				timelineControl.PlayTrack();
			}
		}

		void OnVoyagerPaused()
		{
			if( timelineControl )
			{
				timelineControl.PauseTrack();
			}
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
			if ( VoyagerDevice.IsPresent() )
			{
				if ( XRSettings.enabled )
				{
					recenteredHMD = TryRecenter();

					if (!recenteredHMD) {
						Debug.LogWarning("HMD not recentered, try again");
					}
				}
				else
				{
					Debug.LogWarning("Recieved OnVoyagerRecenterHMD() 'Recenter' callback when UnityEngine.XR is disabled.");
				}
			}
		}

		void OnVoyagerUserPresentToggle(bool InValue)
		{
			if( InValue )
			{
				Debug.Log("Recieved OnVoyagerUserPresentToggle() with 'UserPresent' TRUE.");
			}
			else
			{
				Debug.LogWarning("Recieved OnVoyagerUserPresentToggle() with 'UserPresent' FALSE.");
			}
		}

		void OnVoyagerSixDofTrackingToggle(bool InValue)
		{
			if( InValue )
			{
				Debug.Log("Recieved OnVoyagerSixDofTrackingToggle() with 'SixDofPresent' TRUE.");
			}
			else
			{
				Debug.LogWarning("Recieved OnVoyagerSixDofTrackingToggle() with 'SixDofPresent' FALSE.");
			}
		}

		void Update()
		{
			if( VoyagerDevice.Instance == null || !VoyagerDevice.IsInitialized || !VoyagerDevice.IsConnected)
			{
				return;
			}

			// ~===============================================
			// Oculus Commands

			// Recenter for Oculus Remote DPad
			if( Input.GetAxis("Vertical") >= 1.0f )
			{
				Recenter();
			}

			// Switch tracks with the Oculus Remote DPad
			if( Time.realtimeSinceStartup - lastTrackSwitchTime > lastTrackSwitchDelay )
			{
				if( Input.GetAxis("Horizontal") >= 1.0f )
				{
					NextTrack();
				}
				else if( Input.GetAxis("Horizontal") <= -1.0f )
				{
					PreviousTrack();
				}
			}

			// ~===============================================
			// Key Commands

			if( timelineControl != null && Input.GetKeyDown(KeyCode.H))
			{
				timelineControl.ToggleMenu();
			}

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
				SeekForward10();
			}

			if( Input.GetKeyDown( KeyCode.LeftArrow ))	// Skip back 10sec
			{
				SeekBack10();
			}

			if( Input.GetKeyDown( KeyCode.UpArrow ))	// Skip ahead 30sec
			{
				SeekForward30();
			}

			if( Input.GetKeyDown( KeyCode.DownArrow ))	// Skip back 30sec
			{
				SeekBack30();
			}

			// ~===============================================
			// Update and Send Experience Time to PSM.

			// Handle special case that could cause drift if moved into optimization
			if (VoyagerDevice.PlayState == VoyagerDevicePlayState.Play && timelineControl == null)
			{
				experienceTime += Time.deltaTime;
			}

			int tickNum = Time.frameCount;
			if( !optimizeSendTime || (tickNum % voyagerSendTimeInterval) == 0 )
			{
				switch( VoyagerDevice.PlayState )
				{
					case VoyagerDevicePlayState.Play:
					{
						if( timelineControl == null )
						{
							VoyagerDevice.SendTimeSeconds(experienceTime);
						}
						else
						{
							experienceTime = timelineControl.SendTimeSeconds();
						}
						break;
					}

					case VoyagerDevicePlayState.Pause:
					{
						if( timelineControl == null )
						{
							VoyagerDevice.SendTimeSeconds(experienceTime);
						}
						else
						{
							experienceTime = timelineControl.SendTimeSeconds();
						}
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

			// ~===============================================
			// Handle Fast-Forward and Rewind

			if( VoyagerDevice.IsContentLoaded )
			{
				if( VoyagerDevice.IsFastForwarding )
				{
					SeekForward10();
				}
				else if( VoyagerDevice.IsRewinding )
				{
					SeekBack10();
				}
			}
		}

		public void ExitApp()
		{
			VoyagerDevice.Stop();

			ScreenFadeOut();

			StartCoroutine(DoQuitApp());
		}

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
