/* Copyright 2017 Positron code by Brad Nelson */

using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace Positron
{
	/* This class is used to initialize the Voyager Interface
	 * and execute the commands from the Voyager Interface*/
	public class VoyagerManager : MonoBehaviour
	{
		[ Header("XR Space") ]
		public UnityEngine.XR.TrackingSpaceType XRSpaceType = UnityEngine.XR.TrackingSpaceType.Stationary;

		[ Header("Content Path") ]

		// Path for this application's executable
		[ SerializeField ]
		private string path = "C:/ExecutableName.exe";

		[ Header("Input") ]

		public float pitch = 0f;
		public float pitchAccel = 10f;
		public float pitchDecel = 5f;
		public float yaw = 0f;
		public float yawAccel = 10f;
		public float yawDecel = 5f;
		public Text inputTypeLabel;
		public Text pitchLabel;
		public Text yawLabel;

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

		public void PitchUp5()
		{
			PitchTo(pitch + 5);
		}

		public void PitchDown5()
		{
			PitchTo( pitch - 5 );
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
			YawTo( yaw - 10 );
		}

		public void YawTo( float yawDegrees )
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

		public void CycleInputType()
		{
			if( VoyagerDevice.PlayState != VoyagerDevicePlayState.Stop )
			{
				VoyagerDevice.SetInputType((VoyagerDeviceInputType)(((int)VoyagerDevice.InputType + 1) % 3));
			}

			if( inputTypeLabel )
			{
				inputTypeLabel.text = VoyagerDevice.InputType.ToString().ToUpper();
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

		void Awake()
		{
			DontDestroyOnLoad( this );

			if( timelineControl == null )
			{
				timelineControl = GetComponent<TimelineControl>();
			}

			// Init HMD
			if( UnityEngine.XR.XRDevice.isPresent && UnityEngine.XR.XRSettings.enabled )
			{
				UnityEngine.XR.XRDevice.SetTrackingSpaceType(XRSpaceType);
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
			if( UnityEngine.XR.XRDevice.isPresent )
			{
				if( UnityEngine.XR.XRSettings.enabled )
				{
					UnityEngine.XR.XRDevice.SetTrackingSpaceType( XRSpaceType );
					UnityEngine.XR.InputTracking.Recenter();
				}
				else
				{
					Debug.LogWarning("Recieved OnVoyagerRecenterHMD() 'Recenter' callback when UnityEngine.XR is disabled.");
				}
			}
		}

		void Update()
		{
			if( VoyagerDevice.Instance == null || !VoyagerDevice.IsInitialized )
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

			if( Input.GetKey(KeyCode.Comma))	// Pitch or Yaw
			{
				if( Input.GetKey(KeyCode.LeftShift))// Pitch
				{
					PitchTo( pitch + (8f * Time.deltaTime));
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
					PitchTo( pitch - (8f * Time.deltaTime));
				}
				else
				{
					YawTo(yaw + (24f * Time.deltaTime));
				}
			}

			// ~===============================================
			// Send Experience Time to PSM.

			int tickNum = Time.frameCount;
			if( !optimizeSendTime || (tickNum % voyagerSendTimeInterval) == 0 )
			{
				switch( VoyagerDevice.PlayState )
				{
					case VoyagerDevicePlayState.Play:
					{
						if( timelineControl == null )
						{
							experienceTime += Time.deltaTime;
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
