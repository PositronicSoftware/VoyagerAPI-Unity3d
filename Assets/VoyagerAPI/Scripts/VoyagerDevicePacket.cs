/* Copyright 2017 Positron code by Brad Nelson */

using UnityEngine;

namespace Positron
{
	[ System.Serializable ]
	public class VoyagerDevicePacket
	{
		public int ID = 0;
		public VoyagerDevicePacketContent content = new VoyagerDevicePacketContent();
		public VoyagerDevicePacketBody body = new VoyagerDevicePacketBody();
		public VoyagerDevicePacketEvent @event = new VoyagerDevicePacketEvent();
		public VoyagerDevicePacketParam parameter = new VoyagerDevicePacketParam();
	}

	[ System.Serializable ]
	public class VoyagerDevicePacketContent
	{
		public string type;
		public string platform;
		public string name;
		public string ver;
	}

	[ System.Serializable ]
	public class VoyagerDevicePacketBody
	{
		public float pitch;	// --- pitch in radians
		public float pitchVel;
		public float pitchAccel;
		public float yaw;	// --- yaw in radians
		public float yawVel;
		public float yawAccel;
	}

	[ System.Serializable ]
	public class VoyagerDevicePacketEvent
	{
		public string language = "English";
		public bool library = false;
		public string inputType = "None";
		public int status = 0;				// --- playback state, integer possible values are : 0 (StoppedState), 1 (PlayingState), 2 (PausedState)
		public bool playPause = false;
		public bool stop = false;
		public bool loaded = false;
		public string scene = "Scene00";
		public float fov = 0.0f;			// --- field of view in degrees
		public int timePosition = 0;		// --- current video playback position in milliseconds
		public string url = null;	// --- current video url. For local files, ensure to respect this scheme according to OS :
		// Windows : "url":"file:///C:/Users/kolor/Videos/FullHD_25fps.mp4"
		// Mac : "url":"file:///Users/loic/Movies/FullHD_25fps.mp4"
		public string motionProfile = null;
		public int stereoscopy = 0;
		public int audioFormat = 0;
		public bool forward = false;
		public bool rewind = false;
		public bool recenter = false;
		public float headsetYaw = 0.0f;
		public float headsetPitch = 0.0f;
		public float headsetRoll = 0.0f;
		public int fanID = 0;
		public int fan = 0;
		public int lightID = 0;
		public int lightState = 0;
		public int lightRed = 0;
		public int lightGreen = 0;
		public int lightBlue = 0;
		public int scentPort = 0;
		public float scentDuration = 0.0f;
		public bool scentBooster = false;
		public bool mute = false;
		public bool home = false;
		public bool restartSoftware = false;
		public bool restartComputer = false;
		public bool shutdown = false;
		public bool userPresent = false;
	}

	[ System.Serializable ]
	public class VoyagerDevicePacketParam
	{
		public string name;
		public string value;
	}
}