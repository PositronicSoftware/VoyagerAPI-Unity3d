/* Copyright Positron 2018 - Code by Brad Nelson */

using System;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.UI;

namespace Positron
{
	[ Serializable ]
	public class VoyagerTrackSetup
	{
		[ SerializeField ]
		public string motionProfile;

		[ SerializeField ]
		public PlayableDirector track;

		public bool IsValid()
		{
			return !string.IsNullOrEmpty( motionProfile ) && (track != null);
		}
	}

	public class TimelineControl : MonoBehaviour
	{
		[ Header("Timeline UI") ]

		public CanvasGroup menuCanvas;
		public RectTransform bottomUI2D;
		private Vector2 startBottomUI2DPos;
		private Vector2 pos;

		public Button fullScreenButton;
		public Sprite fullscreenSprite2D;
		public Sprite windowSprite2D;

		public Button playButton2D;
		public Sprite playSprite2D;
		public Sprite pauseSprite2D;

		public Button muteButton2D;
		public Sprite muteOffSprite;
		public Sprite muteOnSprite;

		public Slider videoSeekSlider2D;
		private float setVideoSeekSliderValue = 0;
		private bool wasPlayingOnScrub;
		public Text position2DText;
		public Text duration2DText;

		[ Header("Playable Tracks") ]

		public float seekTime = 5000f;
		public float seekTimeFast = 7000f;
		private PlayableDirector director;
		public VoyagerTrackSetup[] TrackSetups;
		private int currentTrackIndex = 0;
		private float lastTrackSwitchTime = 0f;
		private float lastTrackSwitchDelay = 1f;

		[ Header("Optimization") ]

		[ Tooltip("Enable optimizations to reduce memory allocation per frame.") ]
		public bool optimizeMemAlloc;

		[ Range(1, 30), Tooltip("Stagger SendTime() updates to Voyager to reduce JSON parse mem-alloc.\n'N' == SendTime() every Nth frame.\nRequires 'OptimizeMemAlloc' ON to work.") ]
		public int voyagerSendTimeInterval = 1;

		private double directorCachedTimeSec = 0;
		private double directorCachedDurationSec = 1;
		private StringBuilder timeStampSB = new StringBuilder("00:00:00", 8);

		public bool HasTrackSetups
		{
			get{ return TrackSetups.Length > 0; }
		}

		public int CurrentTrackIndex
		{
			get{ return currentTrackIndex; }
		}

		public VoyagerTrackSetup CurrentTrackDefinition
		{
			get{ return TrackSetups[ currentTrackIndex ]; }
		}

		public PlayableDirector CurrentTrack
		{
			get{ return director; }
		}

		public void TrackForward()
		{
			SwitchPlayableTrack( currentTrackIndex + 1 );
		}

		public void TrackBack()
		{
			SwitchPlayableTrack( currentTrackIndex - 1 );
		}

		// overrideTime = bool, override the Interface send time or not
		static private bool _overrideTime = false;
		static public bool OverrideTime
		{
			get{ return _overrideTime; }
		}

		public float GetTime()
		{
			return (float)directorCachedTimeSec * 1000f;
		}

		public float GetDuration()
		{
			return (float)directorCachedDurationSec * 1000f;
		}

		public void Seek( float timeMS )
		{
			if( director != null )
			{
				director.time = ((double)(Mathf.Max( 0f, timeMS ) / 1000f));
				director.Evaluate();
				VoyagerDevice.SendTime((int)(timeMS));
			}
			else
			{
				Debug.LogError("Need TrackSetups with a Playable Director");
			}
		}

		public void Seek()
		{
			Seek( GetTime() + seekTime );
		}

		public void Rewind()
		{
			Seek( GetTime() - seekTime );
		}

		void Play()
		{
			if( director != null )
			{
				if( director.state != PlayState.Playing )
				{
					director.Play();
				}
				var playGraph = director.playableGraph;
				if( playGraph.IsValid())
				{
					playGraph.GetRootPlayable(0).SetSpeed(1);
				}
				// Debug.Log("Play Director");

				SetTime();

				playButton2D.image.sprite = pauseSprite2D;
			}
			else
			{
				Debug.LogError("Need TrackSetups with a Playable Director");
			}
		}

		void Pause()
		{
			if( director != null )
			{
				var playGraph = director.playableGraph;
				if( playGraph.IsValid())
				{
					playGraph.GetRootPlayable(0).SetSpeed(0);
				}
				if( director.state != PlayState.Paused )
				{
					director.Pause();
				}
				// Debug.Log("Pause Director");

				SetTime();

				playButton2D.image.sprite = playSprite2D;
			}
			else
			{
				Debug.LogError("Need TrackSetups with a Playable Director");
			}
		}

		public void SwitchPlayableTrack( int trackIndex )
		{
			if( trackIndex == currentTrackIndex ) { return; }

			int numTracks = TrackSetups.Length;
			if( numTracks > 0 )
			{
				if( trackIndex > -1 )
				{
					currentTrackIndex = trackIndex % numTracks;
				}
				else
				{
					currentTrackIndex = Mathf.Max( 0, numTracks + trackIndex );
				}

				var trackDef = TrackSetups[ currentTrackIndex ];
				if( trackDef.IsValid())
				{
					lastTrackSwitchTime = Time.time;

					director.Stop();
					director = trackDef.track;

					VoyagerDevice.SetMotionProfile( trackDef.motionProfile );
					Seek( 0 );

					switch( VoyagerDevice.PlayState )
					{
						case VoyagerDevicePlayState.Play:
						{
							OnVoyagerPlay();
							break;
						}

						case VoyagerDevicePlayState.Pause:
						{
							OnVoyagerPaused();
							break;
						}
					}
				}
				else
				{
					Debug.LogError( "TrackDefinition is NOT valid! Make sure both 'Direction' and 'MotionProfile' name are set." );
				}
			}
			else
			{
				Debug.LogError( "No TrackDefinition(s) setup" );
			}
		}

		public void OnVideoSeekSlider2D()
		{
			if( videoSeekSlider2D.value != setVideoSeekSliderValue )
			{
				Seek(videoSeekSlider2D.value * GetDuration());
			}
		}

		public void OnVideoSliderDown2D()
		{
			wasPlayingOnScrub = (director.state == PlayState.Playing);

			Seek(videoSeekSlider2D.value * GetDuration());

			if( wasPlayingOnScrub )
			{
				Pause();
			}
		}

		public void OnVideoSliderUp()
		{
			Seek(videoSeekSlider2D.value * GetDuration());

			if( wasPlayingOnScrub )
			{
				Play();

				wasPlayingOnScrub = false;
			}
		}

		public void ToggleMenu()
		{
			if( menuCanvas != null )
			{
				if( menuCanvas.alpha == 1f )
				{
					menuCanvas.alpha = 0;
					menuCanvas.interactable = false;
					menuCanvas.blocksRaycasts = false;
				}
				else
				{
					menuCanvas.alpha = 1f;
					menuCanvas.interactable = true;
					menuCanvas.blocksRaycasts = true;
				}
			}
		}

		string ConvertTime(int timeMS)
		{
			var timeSpan = TimeSpan.FromMilliseconds(timeMS);
			timeStampSB.Remove(0, 8);
			timeStampSB.AppendFormat("{0:D2}:{1:D2}:{2:D2}", timeSpan.Hours, timeSpan.Minutes, timeSpan.Seconds);
			return timeStampSB.ToString();
		}

		void SetTime()
		{
			VoyagerDevice.SendTime((int)GetTime());
		}

		void Start()
		{
			startBottomUI2DPos = bottomUI2D.anchoredPosition;
			pos = startBottomUI2DPos;
			_overrideTime = true;

			currentTrackIndex = 0;
			if( TrackSetups.Length > 0 && TrackSetups[ currentTrackIndex ].IsValid())
			{
				var firstDef = TrackSetups[ currentTrackIndex ];
				director = firstDef.track;
			}
			if( director == null )
			{
				Debug.LogError( "PlayableDirector must be set in the 'TracksDefinitions' Array!" );
			}

			// ~===============================================
			// Bind Events

			VoyagerDevice.OnPlay += OnVoyagerPlay;
			VoyagerDevice.OnPaused += OnVoyagerPaused;
			VoyagerDevice.OnStopped += OnVoyagerStopped;
			VoyagerDevice.OnMuteToggle += OnVoyagerToggleMute;
			VoyagerDevice.OnMotionProfileChange += OnVoyagerMotionProfileChange;

			// ~===============================================
			// Initialize
			switch( VoyagerDevice.PlayState )
			{
				case VoyagerDevicePlayState.Play:
				{
					OnVoyagerPlay();
					break;
				}

				case VoyagerDevicePlayState.Pause:
				{
					OnVoyagerPaused();
					break;
				}
			}
			OnVoyagerToggleMute( VoyagerDevice.IsMuted );
		}

		void OnVoyagerPlay()
		{
			Play();
		}

		void OnVoyagerPaused()
		{
			Pause();
		}

		void OnVoyagerStopped()
		{
			// React to Stop state event.
		}

		void OnVoyagerToggleMute( bool InValue )
		{
			if( muteButton2D )
			{
				muteButton2D.image.sprite = InValue ? muteOnSprite : muteOffSprite;
			}
		}

		void OnVoyagerMotionProfileChange( string InProfile )
		{
			if( HasTrackSetups )
			{
				// If new profile does not match the current track.
				if( !string.IsNullOrEmpty( InProfile ) && CurrentTrackDefinition.motionProfile != InProfile )
				{
					var newTrack = TrackSetups.FirstOrDefault( e => (e.motionProfile.ToLower() == InProfile.ToLower()));
					if( newTrack != null )
					{
						SwitchPlayableTrack( System.Array.IndexOf( TrackSetups, newTrack ));
					}
					else
					{
						Debug.LogError("Voyager Transitioned to MotionProfile '" + InProfile + "' not supported by TimelineControl" );
					}
				}
			}
		}

		void Update()
		{
			if( director == null )
			{
				Debug.LogError("Need a PlayableDirector");
				return;
			}

			if( VoyagerDevice.Instance == null || !VoyagerDevice.IsInitialized )
			{
				return;
			}

			if( Input.GetKeyDown(KeyCode.H))
			{
				ToggleMenu();
			}

			// Switch tracks with the Oculus Remote DPad left and right
			if( Time.time - lastTrackSwitchTime > lastTrackSwitchDelay )
			{
				if( Input.GetAxis("Horizontal") >= 1.0f )
				{
					lastTrackSwitchTime = Time.time;
					TrackForward();
				}
				else if( Input.GetAxis("Horizontal") <= -1.0f )
				{
					lastTrackSwitchTime = Time.time;
					TrackBack();
				}
			}

			// These PlayableDirector getters() cause mem allocations and are therefore called once & cached here.
			directorCachedTimeSec = director.time;
			directorCachedDurationSec = director.duration;

			// Do Seek correction if Profile has changed.
			if( VoyagerDevice.DeviceMotionProfileTime != VoyagerDevice.PrevDeviceMotionProfileTime
				&& !Mathf.Approximately((float)directorCachedTimeSec, VoyagerDevice.DeviceMotionProfileTime / 1000f ))
			{
				Seek((float)VoyagerDevice.DeviceMotionProfileTime);
			}

			if( VoyagerDevice.IsContentLoaded )
			{
				if( VoyagerDevice.IsFastForwarding )
				{
					Seek();
				}
				else if( VoyagerDevice.IsRewinding )
				{
					Rewind();
				}
			}

			int tickNum = Time.frameCount;
			if( !optimizeMemAlloc || (tickNum % voyagerSendTimeInterval) == 0 )
			{
				SetTime();
			}

			float durationMS = GetDuration();
			int textTickCycle = tickNum % 30;
			if( !optimizeMemAlloc || textTickCycle == 0 )
			{
				duration2DText.text = ConvertTime((int)durationMS);
			}

			if( durationMS > 0f )
			{
				float timeMS = GetTime();
				float d = timeMS / durationMS;
				setVideoSeekSliderValue = d;
				videoSeekSlider2D.value = d;

				if( !optimizeMemAlloc || textTickCycle == 7 || textTickCycle == 22 )
				{
					position2DText.text = ConvertTime((int)timeMS);
				}
			}

			playButton2D.image.sprite = (director.state == PlayState.Playing ? pauseSprite2D : playSprite2D);

			// Check full-screen icon.
			if( Screen.fullScreen )
			{
				fullScreenButton.image.sprite = windowSprite2D;
			}
			else
			{
				fullScreenButton.image.sprite = fullscreenSprite2D;
			}

			// Set Bottom UI to zero position
			pos = Vector2.zero;
			bottomUI2D.anchoredPosition = Vector3.Lerp(bottomUI2D.anchoredPosition, pos, 0.16f);
		}
	}
}