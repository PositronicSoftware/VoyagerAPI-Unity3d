/* Copyright Positron 2018 - Code by Brad Nelson */

using System;
using System.Text;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.UI;

namespace Positron
{
	public class TimelineControl : MonoBehaviour
	{
		public VoyagerManager voyagerManager;
		public PlayableDirector director;

		// 2D UI
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

		public float seekTime = 5000f;
		public float seekTimeFast = 7000f;

		[ Header("Optimization") ]

		[ Tooltip("Enable optimizations to reduce memory allocation per frame.") ]
		public bool optimizeMemAlloc;

		[ Range(1, 30), Tooltip("Stagger SendTime() updates to Voyager to reduce JSON parse mem-alloc.\n'N' == SendTime() every Nth frame.\nRequires 'OptimizeMemAlloc' ON to work.") ]
		public int voyagerSendTimeInterval = 1;

		private int currentTrack = 0;
		private float lastSwitchTime = 0f;
		private float lastSwitchDelay = 1f;
		private double directorCachedTime = 0;
		private double directorCachedDuration = 1;
		private StringBuilder timeStampSB = new StringBuilder("00:00:00", 8);
		private MotionProfile motionProfile;

		private bool _trackForward = false;
		public bool trackForward
		{
			get
			{
				return _trackForward;
			}
			set
			{
				_trackForward = value;
			}
		}

		private bool _trackBack = false;
		public bool trackBack
		{
			get
			{
				return _trackBack;
			}
			set
			{
				_trackBack = value;
			}
		}

		public void TrackBack()
		{
			_trackBack = true;
			_trackForward = false;
		}

		public void TrackForward()
		{
			_trackBack = false;
			_trackForward = true;
		}

		// overrideTime = bool, override the Interface send time or not
		static private bool _overrideTime = false;
		static public bool overrideTime
		{
			get
			{
				return _overrideTime;
			}
		}

		public void Rewind()
		{
			if( director != null )
			{
				videoSeekSlider2D.value = (GetTime() - seekTime) / GetDuration();

				VoyagerDevice.SendTime((int)(GetTime() - seekTime));
			}
			else
			{
				Debug.LogError("Need a SwitchTrack component with a Playable Director");
			}
		}

		public float GetTime()
		{
			return (float)directorCachedTime * 1000f;
		}

		public float GetDuration()
		{
			return (float)directorCachedDuration * 1000f;
		}

		public void Seek(float time)
		{
			if( director != null )
			{
				director.time = ((double)(time / 1000f));
				director.Evaluate();
				VoyagerDevice.SendTime((int)(time));
			}
			else
			{
				Debug.LogError("Need a SwitchTrack component with a Playable Director");
			}
		}

		public void Seek()
		{
			if( director != null )
			{
				videoSeekSlider2D.value = (GetTime() + seekTime) / GetDuration();

				VoyagerDevice.SendTime((int)(GetTime() + seekTime));
			}
			else
			{
				Debug.LogError("Need a SwitchTrack component with a Playable Director");
			}
		}

		public bool IsPlaying()
		{
			return (Time.timeScale == 1f && director != null && director.state == PlayState.Playing);
		}

		public void Play()
		{
			if( director != null )
			{
				// director.Resume();
				if( director.state == PlayState.Playing )
				{
					director.playableGraph.GetRootPlayable(0).SetSpeed(1);
				}
				else
				{
					director.Play();
					director.playableGraph.GetRootPlayable(0).SetSpeed(1);
					Debug.Log("Playing director");
				}

				VoyagerDevice.SendTime((int)GetTime());
				// Interface.Play();

				playButton2D.image.sprite = pauseSprite2D;
			}
			else
			{
				Debug.LogError("Need a SwitchTrack component with a Playable Director");
			}
		}

		public void Pause()
		{
			if( director != null )
			{
				// director.Pause();
				if( director.state == PlayState.Playing )
				{
					director.playableGraph.GetRootPlayable(0).SetSpeed(0);
					director.Pause();
				}

				VoyagerDevice.SendTime((int)GetTime());
				// Interface.Pause();

				playButton2D.image.sprite = playSprite2D;
			}
			else
			{
				Debug.LogError("Need a SwitchTrack component with a Playable Director");
			}
		}

		public void PlayPause()
		{
			if( voyagerManager != null )
			{
				voyagerManager.PlayPause();
			}
			else
			{
				Debug.LogError("Need a VoyagerManager");
			}

			if( VoyagerDevice.IsPaused )
			{
				Pause();
			}
			else
			{
				Play();
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
			CheckFullscreen();
		}

		public void CheckFullscreen()
		{
			if( Screen.fullScreen )
			{
				fullScreenButton.image.sprite = windowSprite2D;
			}
			else
			{
				fullScreenButton.image.sprite = fullscreenSprite2D;
			}
		}

		public void ToggleMute(bool mute)
		{
			if( mute )
			{
				// muteButton.image.sprite = muteOnSprite;
				muteButton2D.image.sprite = muteOnSprite;
			}
			else
			{
				// muteButton.image.sprite = muteOffSprite;
				muteButton2D.image.sprite = muteOffSprite;
			}
		}

		public void ToggleMute()
		{
			VoyagerDevice.ToggleMute();

			voyagerManager.Mute(VoyagerDevice.IsMuted);

			ToggleMute(VoyagerDevice.IsMuted);
		}

		private string ConvertTime(int ms)
		{
			var timeSpan = TimeSpan.FromMilliseconds(ms);
			timeStampSB.Remove(0, 8);
			timeStampSB.AppendFormat("{0:D2}:{1:D2}:{2:D2}", timeSpan.Hours, timeSpan.Minutes, timeSpan.Seconds);
			return timeStampSB.ToString();
		}

		public void SetTime()
		{
			VoyagerDevice.SendTime((int)GetTime());
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

		// Toggle the menu
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

		// Use this for initialization
		void Start()
		{
			startBottomUI2DPos = bottomUI2D.anchoredPosition;
			pos = startBottomUI2DPos;
			_overrideTime = true;
		}

		void Update()
		{
			if( Input.GetKeyDown(KeyCode.H))
			{
				ToggleMenu();
			}

			if( VoyagerDevice.Instance == null )
			{
				return;
			}

			if( director == null )
			{
				Debug.LogError("Need a PlayableDirector");
				return;
			}

			// These PlayableDirector getters() cause mem allocations and are therefore called once & cached here.
			directorCachedTime = director.time;
			directorCachedDuration = director.duration;

			int tickNum = Time.frameCount;
			if( VoyagerDevice.IsInitialized )
			{
				if( VoyagerDevice.IsUpdated )
				{
					if( VoyagerDevice.DeviceMotionProfileTime != VoyagerDevice.PrevDeviceMotionProfileTime
						&& director != null && director.time != ((double)(VoyagerDevice.DeviceMotionProfileTime / 1000)))
					{
						Seek((float)VoyagerDevice.DeviceMotionProfileTime);
					}

					if( VoyagerDevice.IsRecentering )
					{
						voyagerManager.Recenter();
					}

					if( VoyagerDevice.IsContentLoaded )
					{
						if( !VoyagerDevice.IsPaused && !IsPlaying())
						{
							Play();
						}

						if( VoyagerDevice.IsPaused && IsPlaying()
							|| VoyagerDevice.IsPaused && Time.timeScale == 0f && director != null && director.state == PlayState.Playing )
						{
							Pause();
						}

						if( VoyagerDevice.IsFastForwarding )
						{
							Seek();
						}
						else if( VoyagerDevice.IsRewinding )
						{
							Rewind();
						}

						ToggleMute(VoyagerDevice.IsMuted);
					}
				}

				if( !optimizeMemAlloc || (tickNum % voyagerSendTimeInterval) == 0 )
				{
					SetTime();
				}
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

			CheckFullscreen();

			// Set Bottom UI to zero position
			pos = Vector2.zero;
			bottomUI2D.anchoredPosition = Vector3.Lerp(bottomUI2D.anchoredPosition, pos, 0.16f);
		}
	}
}