using System.Collections;
using UnityEngine;
using UnityEngine.XR;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace Positron
{
	public static class VoyagerDefaults
	{
		public const string apiVersion = "2.0.1-alpha";

		// Connection defaults
		public const string localHostIP = "127.0.0.1";
		public const int tcpPort = 61557;
	}

	// Device Play State Id.
	public enum VoyagerDevicePlayState { Stop = 0, Play = 1, Pause = 2, Idle = 3 }

	public delegate void VoyagerEventDelegate();

	public delegate void VoyagerPlayStateEventDelegate( VoyagerDevicePlayState InState );

    public delegate void VoyagerContentChangeEventDelegate(string InUrl);

    public delegate void VoyagerMotionProfileEventDelegate( string InProfile );

	public delegate void VoyagerToggleEventDelegate( bool InValue );

	public class VoyagerDevice : MonoBehaviour
	{
		// instance = an instance of the Positron.Interface
		static private VoyagerDevice _instance;
		static public VoyagerDevice Instance
		{
			get
			{
				if( _instance == null )
				{
					_instance = GameObject.FindObjectOfType<VoyagerDevice>();

					if( _instance == null )
					{
						_instance = new GameObject("_VoyagerDeviceAPI").AddComponent<VoyagerDevice>();

						DontDestroyOnLoad(_instance.gameObject);
					}
				}
				return _instance;
			}
		}

		// Networking
		private VoyagerDeviceConfig _config;
		public static VoyagerDeviceConfig Config
		{
			get{ return Instance._config; }
		}

		static public Transform cameraMain;
		
		private Telepathy.Client client = new Telepathy.Client(5 * 1024);

        public static Action OnConnected;
        public static Action OnDisconnected;

        private int lastRecvPacketId = -1;
		private string lastSentJsonPktStr;							// Packet string is cached to prevent GC cleanup on each SendData() call.
		private byte[] lastSentJsonPktBytes = new byte[ 5 * 1024 ];	// Packet bytes fixed size & cached to prevent GC cleanup on each SendData() call.

		private VoyagerDevicePacket lastRecvDevicePacket = new VoyagerDevicePacket();
		public static VoyagerDevicePacket LastRecvDevicePacket
		{
			get{ return Instance.lastRecvDevicePacket; }
		}

		static public VoyagerDevicePacket deviceState = new VoyagerDevicePacket();

		private Coroutine reconnectCoroutine = null;

		public static bool IsConnected 
		{
			get { return Instance.client.Connected; }
		}

		/********************************************************
		 *  Device Interface
		 *
		 *******************************************************/

		// PlayerType = enum, the type of player we are running for this application. 0 = AVPro, 1 = Unity, 2 = Executable
		public enum PlayerType { AVPro, Unity, Executable }

		// playerType = PlayerType, sets the Interface player type - #todo currently unused
		static public PlayerType playerType = PlayerType.AVPro;

		static public event VoyagerPlayStateEventDelegate OnPlayStateChange;
		static public event VoyagerEventDelegate OnPlay;
		static public event VoyagerEventDelegate OnPaused;
		static public event VoyagerEventDelegate OnStopped;
		static public event VoyagerEventDelegate OnRecenter;
		static public event VoyagerToggleEventDelegate OnMuteToggle;
		static public event VoyagerEventDelegate OnFastForward;
		static public event VoyagerEventDelegate OnRewind;
		static public event VoyagerMotionProfileEventDelegate OnMotionProfileChange;
        static public event VoyagerContentChangeEventDelegate OnContentChange;
        static public event VoyagerToggleEventDelegate OnUserPresentToggle;
		static public event VoyagerToggleEventDelegate OnSixDofPresentToggle;

		// The current state used by the Interface.  State.Stop, State.Play, State.Pause
		static public VoyagerDevicePlayState _playState = VoyagerDevicePlayState.Stop;
		static public VoyagerDevicePlayState PlayState
		{
			get{ return _playState; }
		}

		// Content object the Interface is currently using
		static private Positron.VoyagerDevicePacketContent _content = new VoyagerDevicePacketContent();
		static public Positron.VoyagerDevicePacketContent Content
		{
			get{ return _content; }
		}

		// Is the DeviceInterface initialized. Call Init( FVoyagerDeviceConfig InConfig ) in-order to initialize it.
		static private bool _isInitialized = false;
		static public bool IsInitialized
		{
			get{ return _isInitialized; }
		}

		// startMotionTime = float, the time the motion profile started
		static private float _motionProfileStartTime = 0f;

		// Variables received from TCP //

		// paused = bool, is the Interface paused or not
		static private bool _isPaused = true;
		static public bool IsPaused
		{
			get{ return _isPaused; }
		}

		// mute = bool, is the Interface muted or not
		static private bool _isMute = false;
		static public bool IsMuted
		{
			get{ return _isMute; }
		}

		// forward = bool, is the Interface in forward seek mode
		static private bool _isFastForwarding = false;
		static public bool IsFastForwarding
		{
			get{ return _isFastForwarding; }
		}

		// rewind = bool, is the Interface in rewind seek mode
		static private bool _isRewinding = false;
		static public bool IsRewinding
		{
			get{ return _isRewinding; }
		}

		// library = bool, is the Interface in the library in Voyager Portal
		static private bool _isInLibrary = false;
		static public bool IsInLibrary
		{
			get{ return _isInLibrary; }
		}

		// recenter = bool, is the Interface recentering the headset
		static private bool _isRecentering = false;
		static public bool IsRecentering
		{
			get{ return _isRecentering; }
		}

		// updated = bool, has the Interface been updated
		static private bool _isUpdated = false;
		static public bool IsUpdated
		{
			get{ return _isUpdated; }
		}

		// loaded = bool, is the content loaded or not
		static private bool _isContentLoaded = false;
		static public bool IsContentLoaded
		{
			get{ return _isContentLoaded; }
		}

		// loaded = bool, is the content loaded or not
		static private bool _isUserPresent = false;
		static public bool IsUserPresent
		{
			get{ return _isUserPresent; }
		}

		// loaded = bool, is the content loaded or not
		static private bool _isSixDofPresent = false;
		static public bool IsSixDofPresent
		{
			get{ return _isSixDofPresent; }
		}

		// currentTime = int, the current time of the current motion profile
		static private int _deviceMotionProfileTime = 0;
		static public int DeviceMotionProfileTime
		{
			get{ return _deviceMotionProfileTime; }
		}

		// previousTime = int, the previous time of the current motion profile
		static private int _prevDeviceMotionProfileTime = 0;
		static public int PrevDeviceMotionProfileTime
		{
			get{ return _prevDeviceMotionProfileTime; }
		}

		// stereoscopy = int, the type of stereoscopy the interface is using, 0 = None, 1 = Top/Bottom, 2 = Left/Right
		static private int _stereoscopyMode = 0;
		static public int StereoscopyMode
		{
			get{ return _stereoscopyMode; }
		}

		// currentUrl = string, the current URL that the Interface has loaded (content url, i.e. application path)
		static private string _contentUrl;
		static public string ContentUrl
		{
			get{ return _contentUrl; }
		}

		// previousUrl = string, the previous URL that the Interface loaded (content url, i.e. application path)
		static private string _previousContentUrl;
		static public string PreviousContentUrl
		{
			get{ return _previousContentUrl; }
		}

		// currentMotionProfile = string, the current Motion Profile that is playing in the Interface
		static private string _motionProfile;
		static public string MotionProfile
		{
			get{ return _motionProfile; }
		}

		// xrDisplaySubsystems = List<XRDisplaySubsystem>, used to track user presence
		static private List<XRDisplaySubsystem> _xrDisplaySubsystems = new List<XRDisplaySubsystem>();
		static public List<XRDisplaySubsystem> xrDisplaySubsystems
		{
			get { 
				return _xrDisplaySubsystems; }
		}

		// sixDofFilter = InputDeviceCharacteristics, define characteristics of the device for checking 6dof preference
		static private InputDeviceCharacteristics sixDofFilter = InputDeviceCharacteristics.HeadMounted | InputDeviceCharacteristics.TrackedDevice;
		static private List<InputDevice> inputDevices = new List<InputDevice>();
		static private bool hasSixDof = false;
		static private InputTrackingState inputTrackingState;

		// Call in Awake to initialize the Device-Interface correctly. 
		public static void Init(VoyagerDeviceConfig config)
		{
			if( IsInitialized )
			{
				Debug.LogError("VoyagerDevice >> Already initialized. Reinitialization not yet supported.");
				return;
			}

            // Use Debug.Log functions for Telepathy so we can see it in the console
            Telepathy.Log.Info = Debug.Log;
            Telepathy.Log.Warning = Debug.LogWarning;
            Telepathy.Log.Error = Debug.LogError;

            // Hook up telepathy events
            Instance.client.OnConnected += Instance.OnClientConnected;
            Instance.client.OnData += Instance.ReceiveData;
			Instance.client.OnDisconnected += Instance.OnClientDisconnected;

            Instance._config = config;

            // Initialize sending and receiving packets with Voyager
            Debug.Log("VoyagerDevice >> Initializing...");
            IPAddress remoteIP;
            if (IPAddress.TryParse(Config.ipAddr, out remoteIP))
            {
                Debug.Log("VoyagerDevice >> Connecting to: " + Config.ipAddr + ":" + Config.portNum);

                // Telepathy also starts the receive thread here
                Instance.client.Connect(Config.ipAddr, Config.portNum);

                Debug.Log("VoyagerDevice >> Initialized Voyager API v" + VoyagerDefaults.apiVersion);
				_isInitialized = true;
            }
            else
            {
                Debug.LogError("VoyagerDevice >> Init Failed: IP address is not valid! " + Config.ipAddr);
            }
        }
		private void OnClientConnected()
		{
			Debug.Log("VoyagerDevice >> Telepathy Event: Client Connected");

			OnConnected?.Invoke();
        }
		private void OnClientDisconnected()
		{
			Debug.Log("VoyagerDevice >> Telepathy Event: Client Disconnected");

            // Reset last received packet since if PSM has restarted it will start at 0 again.
            lastRecvPacketId = -1;

            OnDisconnected?.Invoke(); 

			if(reconnectCoroutine == null)
			{
                reconnectCoroutine = StartCoroutine(RetryConnect());
			}
		}

		IEnumerator RetryConnect()
		{
            while (!client.Connected)
            {
				if(!client.Connecting && client.ReceivePipeCount == 0)
				{
					Debug.Log("VoyagerDevice >> Attempting to reconnect to: " + Config.ipAddr + " "+ Config.portNum);
					client.Connect(Config.ipAddr, Config.portNum);
				}
				yield return new WaitForSecondsRealtime(2.0f);
            }
			reconnectCoroutine = null;
        }

		// Send latest DeviceState data through client */
		public static void SendData()
		{
			try
			{
				if( !IsInitialized ) { throw new Exception("VoyagerDevice >> DeviceInterface is NOT initialized; Call Init( VoyagerDeviceConfig ) first!"); }
				if( !IsConnected ) 
				{ 
					Debug.LogWarning("VoyagerDevice >> Trying to SendData, but the connection is lost or trying to connect. Check for VoyagerDevice.IsConnected?");
					return;
				}

				VoyagerDeviceUtils.DevicePacketToJson(deviceState, out Instance.lastSentJsonPktStr);
				int numJSONChars = Instance.lastSentJsonPktStr.Length;
				int numBytes = Encoding.UTF8.GetBytes(Instance.lastSentJsonPktStr, 0, numJSONChars, Instance.lastSentJsonPktBytes, 0);
				
                Instance.client.Send(new ArraySegment<byte>(Instance.lastSentJsonPktBytes, 0, numBytes));
				// print(Instance.lastSentJsonPktStr);
			}
			catch( Exception err ) {
				print(err.ToString());
			}
		}

        // Receive data from Voyager
        private void ReceiveData(ArraySegment<byte> data)
		{
			string text = Encoding.UTF8.GetString(data);
			VoyagerDevicePacket receivedPacket;
			VoyagerDeviceUtils.JsonToDevicePacket(text, out receivedPacket);

			if(receivedPacket == null )
			{
				Debug.LogWarning("VoyagerDevice >> Json object not created.");
				return;
            }

			// Although we are using TCP, packets from PSM to utility are UDP and the utility is currently just a relay.
			// Check for out-of-order UDP packet Recv
			if (lastRecvPacketId > -1 && receivedPacket.ID <= lastRecvPacketId)
			{
				Debug.LogWarning("VoyagerDevice >> Rejecting out-of-order packet ID '" + receivedPacket.ID + "'");
			}
			else
			{
				// Track prev params
				_prevDeviceMotionProfileTime = _deviceMotionProfileTime;
				_previousContentUrl = _contentUrl;

				// Update params
				_deviceMotionProfileTime = receivedPacket.@event.timePosition;
				_isInLibrary = receivedPacket.@event.library;
				_stereoscopyMode = receivedPacket.@event.stereoscopy;
				_contentUrl = receivedPacket.@event.url;

				if (_previousContentUrl != _contentUrl)
				{
					OnContentChange?.Invoke(_contentUrl);
				}

				// Handle Motion profile change
				if (receivedPacket.@event.motionProfile != _motionProfile && !string.IsNullOrEmpty(receivedPacket.@event.motionProfile))
				{
					SetMotionProfile(receivedPacket.@event.motionProfile);
				}

				// Handle Recenter
				if (receivedPacket.@event.recenter)
				{
					Recenter();
				}
				else
				{
					_isRecentering = false;
				}

				// Handle state events
				if (receivedPacket.@event.stop)
				{
					Stop();
				}
				else
				{
					// In previous versions we used to ignore Play & Pause Commands if the Content URL was empty.
					// Now we no longer check this URL, which will make testing a lot easier.
					// if(	!string.IsNullOrEmpty(receivedPacket.@event.url) && IsContentLoaded )
					if (IsContentLoaded)
					{
						if (receivedPacket.@event.playPause == IsPaused)
						{
							PlayPause();
						}

						if (PlayState == VoyagerDevicePlayState.Stop)
						{
							Pause();
						}
					}

					if (receivedPacket.@event.mute != IsMuted)
					{
						ToggleMute();
					}

					if (receivedPacket.@event.forward)
					{
						FastForward();
					}
					else if (receivedPacket.@event.rewind)
					{
						Rewind();
					}
					else
					{
						_isFastForwarding = false;
						_isRewinding = false;
					}
				}

				Instance.lastRecvDevicePacket = receivedPacket;
				lastRecvPacketId = receivedPacket.ID;

				_isUpdated = true;
				// Debug.Log( "VoyagerDevice >> Received packet ID '%d' " +newPacket.ID );	
			}
		}

		// Play or Pause the motion profile depending on the Interface.paused variable
		static public void PlayPause()
		{
			if( PlayState == VoyagerDevicePlayState.Pause )
			{
				Play();
			}
			else if( PlayState == VoyagerDevicePlayState.Play )
			{
				Pause();
			}
		}

		// Play the motion profile, sets Interface.paused to false
		static public void Play()
		{
			if( IsInitialized )
			{
				_isPaused = false;

				var prevState = _playState;
				_playState = VoyagerDevicePlayState.Play;
				NotifyStateChange( prevState );

				deviceState.@event.status = (int)_playState;
				deviceState.@event.playPause = true;
				SendData();

				Debug.Log("VoyagerDevice >> | command | 'Play'");
			}
			else
			{
				Debug.LogError("DeviceInterface is NOT initialized; Call Init( VoyagerDeviceConfig ) first!");
			}
		}

		// Pause the motion profile, sets the Interface.paused to true
		static public void Pause()
		{
			if( IsInitialized )
			{
				_isPaused = true;

				var prevState = _playState;
				_playState = VoyagerDevicePlayState.Pause;
				NotifyStateChange( prevState );

				deviceState.@event.status = (int)_playState;
				deviceState.@event.playPause = false;
				SendData();

				Debug.Log("VoyagerDevice >> | command | 'Pause'");
			}
			else
			{
				Debug.LogError("DeviceInterface is NOT initialized; Call Init( VoyagerDeviceConfig ) first!");
			}
		}

		// Set the Voyager to Idle state.
		static public void Idle()
		{
			if( IsInitialized )
			{
				_isPaused = true;

				var prevState = _playState;
				_playState = VoyagerDevicePlayState.Idle;
				NotifyStateChange(prevState);

				deviceState.@event.status = (int)_playState;
				deviceState.@event.playPause = false;
				SendData();

				Debug.Log("VoyagerDevice >> | command | 'Idle'");
			}
			else
			{
				Debug.LogError("DeviceInterface is NOT initialized; Call Init( VoyagerDeviceConfig ) first!");
			}
		}

		// Stops the motion profile and sends data to Voyager telling it the motion is stopped, sets Interface.paused to true
		static public void Stop()
		{
			if( IsInitialized )
			{
				_isPaused = true;

				var prevState = _playState;
				_playState = VoyagerDevicePlayState.Stop;
				NotifyStateChange( prevState );

				deviceState.@event.status = (int)_playState;
				deviceState.@event.playPause = false;
				deviceState.@event.stop = true;
				deviceState.@event.loaded = false;

				LoadContent(null);
				SetMotionProfile(null);

				Debug.Log("VoyagerDevice >> | command | 'Stop'");
			}
			else
			{
				Debug.LogError("DeviceInterface is NOT initialized; Call Init( VoyagerDeviceConfig ) first!");
			}
		}

		// Sets Interface.rewind to true and Interface.forward to false
		static public void Rewind()
		{
			var prevState = _isRewinding;
			_isRewinding = true;
			_isFastForwarding = false;

			if( prevState != true )
			{
				if( OnRewind != null )
				{
					OnRewind();
				}
			}
			Debug.Log("VoyagerDevice >> | command | 'Rewind'");
		}

		// Sets Interface.forward to true and Interface.rewind to false
		static public void FastForward()
		{
			var prevState = _isFastForwarding;
			_isFastForwarding = true;
			_isRewinding = false;

			if( prevState != true )
			{
				if( OnFastForward != null )
				{
					OnFastForward();
				}
			}
			Debug.Log("VoyagerDevice >> | command | 'FastForward'");
		}

		// Sends the current time of the motion profile that is playing in milliseconds,
		// sends 0 if the Interface.state is Interface.State.Stop
		// also sends HMD rotation data in radians
		static public void SendTimeNow()
		{
			if( PlayState == VoyagerDevicePlayState.Stop )
			{
				SendTime(0);
			}
			else
			{
				SendTimeSeconds((Time.time - _motionProfileStartTime));
			}
		}

		// Sends a float time converted to milliseconds to the Voyager, also sends HMD rotation data in radians
		static public void SendTimeSeconds(float time)
		{
			SendTime((int)(time * 1000f));
		}

		// Sends time in milliseconds to Voyager, also sends HMD rotation data in radians
		static public void SendTime(int time)
		{
			if( IsInitialized )
			{
				deviceState.@event.timePosition = time;

				if( cameraMain == null || !cameraMain.gameObject.activeInHierarchy )
				{
					cameraMain = Camera.main.transform;
				}

				deviceState.@event.headsetPitch = cameraMain.localEulerAngles.x * Mathf.Deg2Rad;
				deviceState.@event.headsetYaw = cameraMain.localEulerAngles.y * Mathf.Deg2Rad;
				deviceState.@event.headsetRoll = cameraMain.localEulerAngles.z * Mathf.Deg2Rad;

				SendData();
				// Debug.Log(json);
				// Debug.Log("Time: " + time);
			}
			else
			{
				Debug.LogError("DeviceInterface is NOT initialized; Call Init( VoyagerDeviceConfig ) first!");
			}
		}

		// Sets the time from a float value and converts to milliseconds to send to the Voyager
		static public void SetTimeSeconds(float time)
		{
			SetTime((int)(time * 1000f));
		}

		// Sets the time in milliseconds to send to the Voyager
		static public void SetTime(int time)
		{
			if( IsInitialized )
			{
				deviceState.@event.timePosition = time;

				SendData();
			}
			else
			{
				Debug.LogError("DeviceInterface is NOT initialized; Call Init( VoyagerDeviceConfig ) first!");
			}
		}

		// Send pitch angle, velocity, acceleration to Voyager
		static public void Pitch(Vector3 pitch)
		{
			if( IsInitialized )
			{
				deviceState.body.pitch = pitch.x;	// --- pitch in radians
				deviceState.body.pitchVel = pitch.y;
				deviceState.body.pitchAccel = pitch.z;

				SendData();
			}
			else
			{
				Debug.LogError("DeviceInterface is NOT initialized; Call Init( VoyagerDeviceConfig ) first!");
			}
		}

		// Send yaw angle, velocity, acceleration to Voyager
		static public void Yaw(Vector3 yaw)
		{
			if( IsInitialized )
			{
				deviceState.body.yaw = yaw.x;	// --- yaw in radians
				deviceState.body.yawVel = yaw.y;
				deviceState.body.yawAccel = yaw.z;

				SendData();
			}
			else
			{
				Debug.LogError("DeviceInterface is NOT initialized; Call Init( VoyagerDeviceConfig ) first!");
			}
		}

		// Sends a command to the Voyager to jog left #NotImplementsd
		static public void JogLeft()
		{
			if( IsInitialized )
			{
				// sendData.body.jogLeft = true;
				Debug.LogWarning("VoyagerDevice >> JogLeft() API call not implemented");

				SendData();
			}
			else
			{
				Debug.LogError("DeviceInterface is NOT initialized; Call Init( VoyagerDeviceConfig ) first!");
			}
		}

		// Sends a command to the Voyager to jog right #NotImplementsd
		static public void JogRight()
		{
			if( IsInitialized )
			{
				// sendData.body.jogRight = true;
				Debug.LogWarning("VoyagerDevice >> JogRight() API call not implemented");
				SendData();
			}
			else
			{
				Debug.LogError("DeviceInterface is NOT initialized; Call Init( VoyagerDeviceConfig ) first!");
			}
		}

		// Sends a command to the Voyager to pitch up #NotImplementsd
		static public void PitchUp()
		{
			if( IsInitialized )
			{
				// sendData.body.pitchUp = true;
				Debug.LogWarning("VoyagerDevice >> PitchUp() API call not implemented");
				SendData();
			}
			else
			{
				Debug.LogError("DeviceInterface is NOT initialized; Call Init( VoyagerDeviceConfig ) first!");
			}
		}

		// Sends a command to the Voyager to pitch down #NotImplementsd
		static public void PitchDown()
		{
			if( IsInitialized )
			{
				// sendData.body.pitchDown = true;
				Debug.LogWarning("VoyagerDevice >> PitchDown() API call not implemented");
				SendData();
			}
			else
			{
				Debug.LogError("DeviceInterface is NOT initialized; Call Init( VoyagerDeviceConfig ) first!");
			}
		}

		// Recenter the HMD, and sends a packet to Voyager telling it has done so
		static public void Recenter()
		{
			if( IsInitialized )
			{
				_isRecentering = true;
				if( OnRecenter != null )
				{
					OnRecenter();
				}

				deviceState.@event.recenter = true;
				SendData();

				Debug.Log("VoyagerDevice >> | command | 'Recenter'");
			}
			else
			{
				Debug.LogError("DeviceInterface is NOT initialized; Call Init( VoyagerDeviceConfig ) first!");
			}
		}

		// Sets the the current content url to Voyager (path to application or video).
		// Sets the Interface.currentUrl and Interface.previousUrl variables.
		static public void LoadContent(string url)
		{
			if( IsInitialized )
			{
				_previousContentUrl = _contentUrl;
				_contentUrl = url;

				deviceState.@event.url = url;

				SendData();

				Debug.Log("VoyagerDevice >> | command | 'Load Content' " + _contentUrl );
			}
			else
			{
				Debug.LogError("DeviceInterface is NOT initialized; Call Init( VoyagerDeviceConfig ) first!");
			}
		}

		// Sets the Interface.loaded variable and sends to Voyager if the content is loaded
		static public void Loaded(bool isLoaded)
		{
			if( IsInitialized )
			{
				_isContentLoaded = isLoaded;
				deviceState.@event.stop = !isLoaded;
				deviceState.@event.loaded = _isContentLoaded;

				SendData();

				Debug.Log("Loaded: " + IsContentLoaded);
			}
			else
			{
				Debug.LogError("DeviceInterface is NOT initialized; Call Init( VoyagerDeviceConfig ) first!");
			}
		}

		// Sets the application's unique identifiers to the Voyager
		static public void SetContent(string type, string platform, string contentName, string version)
		{
			_content.type = type;
			_content.platform = platform;
			_content.name = contentName;
			_content.ver = version;

			deviceState.content.type = _content.type;
			deviceState.content.platform = _content.platform;
			deviceState.content.name = _content.name;
			deviceState.content.ver = _content.ver;

			Debug.Log("VoyagerDevice >> | command | 'Set Content' " + "|" + type + "|" + platform + "|" + contentName + "|" + version + "|");
		}

		// Sets the motion profile for Voyager and initializes motion profile time.
		// Sets the Interface.currentMotionProfile and Interface.previousMotionProfile variables.
		static public void SetMotionProfile( string InProfileName )
		{
			if( IsInitialized )
			{
				var prevProfile = _motionProfile;
				_motionProfile = InProfileName;
				if( prevProfile != _motionProfile )
				{
					_motionProfileStartTime = Time.time;

					if( OnMotionProfileChange != null )
					{
						OnMotionProfileChange( _motionProfile );
					}
				}

				deviceState.@event.motionProfile = _motionProfile;
				deviceState.@event.timePosition = 0;
				SendData();

				Debug.Log("VoyagerDevice >> | command | 'MotionProfile' " + _motionProfile);
			}
			else
			{
				Debug.LogError("DeviceInterface is NOT initialized; Call Init( VoyagerDeviceConfig ) first!");
			}
		}

		// Toggles the Interface.mute value, and sends value to Voyager
		static public void ToggleMute()
		{
			_isMute = !_isMute;

			if( OnMuteToggle != null )
			{
				OnMuteToggle( _isMute );
			}

			deviceState.@event.mute = _isMute;
			Debug.Log("VoyagerDevice >> | command | 'Toggle Mute' " + _isMute);
		}

		static private void NotifyStateChange( VoyagerDevicePlayState InPrevState )
		{
			if( InPrevState != PlayState )
			{
				if( OnPlayStateChange != null )
				{
					OnPlayStateChange( PlayState );
				}

				switch( PlayState )
				{
					case VoyagerDevicePlayState.Play:
					{
						if( OnPlay != null )
						{
							OnPlay();
						}
						break;
					}
					case VoyagerDevicePlayState.Pause:
					{
						if( OnPaused != null )
						{
							OnPaused();
						}
						break;
					}
					case VoyagerDevicePlayState.Stop:
					{
						if( OnStopped != null )
						{
							OnStopped();
						}
						break;
					}
				}
			}
		}

		static void SetUserPresent(bool value)
		{
			var prevVal = _isUserPresent;
			_isUserPresent = value;
			deviceState.@event.userPresent = _isUserPresent;

			if( value != prevVal && OnUserPresentToggle != null )
			{
				OnUserPresentToggle(value);
			}
		}

		static void SetSixDofPresent(bool value)
		{
			var prevVal = _isSixDofPresent;
			_isSixDofPresent = value;
			// #TODO deviceState.@event.sixDofPresent = _isSixDofPresent;

			if( value != prevVal && OnSixDofPresentToggle != null )
			{
				OnSixDofPresentToggle(value);
			}
		}

		public static bool IsPresent()
		{
			_xrDisplaySubsystems.Clear();
			SubsystemManager.GetInstances<XRDisplaySubsystem>(_xrDisplaySubsystems);
			foreach (var xrDisplay in _xrDisplaySubsystems)
			{
				if (xrDisplay.running)
				{
					return true;
				}
			}
			return false;
		}

		public static bool SixDofPresence() {
			inputDevices.Clear();
			InputDevices.GetDevicesWithCharacteristics(sixDofFilter, inputDevices);

			for (int i = 0; i < inputDevices.Count; i++)
			{
				hasSixDof = false;

				if (inputDevices[i] != null) {
					inputDevices[i].TryGetFeatureValue(CommonUsages.trackingState, out inputTrackingState);

					// Assuming the device has rotation and positional tracking, it is then 6Dof, 
					// may need to check velocity if this is not valid
					hasSixDof = (inputTrackingState & 
					(InputTrackingState.Position | InputTrackingState.Rotation)) == 
					((InputTrackingState.Position | InputTrackingState.Rotation));

					if (hasSixDof) {
						return true;
					}
				}
			}

			return false;
		}

		private void Update()
		{
            _isUpdated = false;
			client.Tick(100);

			// Auto-set certain parameters
			SetUserPresent(IsPresent() && XRSettings.enabled);

			SetSixDofPresent(SixDofPresence());
        }

        void OnApplicationQuit()
		{
			Cleanup();
		}

		void OnDestroy()
		{
			Cleanup();
		}

		void Cleanup()
		{
            client.OnConnected = null;
            client.OnData = null;
            client.OnDisconnected = null;

			if(reconnectCoroutine != null)
			{
				StopCoroutine(reconnectCoroutine);
				reconnectCoroutine = null;
            }

            client.Disconnect();

			if( _instance == this )
			{
				_instance = null;

				OnConnected = null;
				OnDisconnected = null;

				OnPlayStateChange = null;
				OnPlay = null;
				OnPaused = null;
				OnStopped = null;
				OnRecenter = null;
				OnMuteToggle = null;
				OnFastForward = null;
				OnRewind = null;
				OnMotionProfileChange = null;
				OnContentChange = null;

				_isInitialized = false;
			}

			lastRecvDevicePacket = null;
		}
	}
}