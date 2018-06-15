﻿using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace Positron
{
	/**
	 * Configuration type Id.
	 */
	[ Serializable ]
	public enum VoyagerDeviceConfigId
	{
		/** Default specified in config file */
		VDC_Default,

		/** Localhost config */
		VDC_Local,

		/** Development config */
		VDC_Development,

		/** Production config */
		VDC_Production,
	};


	/**
	 * Configuration Data for the VoyagerDevice interface.
	 */
	[ Serializable ]
	public class VoyagerDeviceConfig
	{
		/** IP Address to connect to */
		public string ipAddr;

		/** UDP Port number to send from */
		public int sendPortNum;

		/** UDP Port number to receive from */
		public int recvPortNum;

		/** Display logs on the screen */
		public bool onScreenLogs;

		public VoyagerDeviceConfig()
		{
			ipAddr = VoyagerDefaults.localHostIP;
			sendPortNum = VoyagerDefaults.udpSendPort;
			recvPortNum = VoyagerDefaults.udpRecvPort;
			onScreenLogs = true;
		}

		public VoyagerDeviceConfig(int inSendPortNum)
		{
			ipAddr = VoyagerDefaults.localHostIP;
			sendPortNum = inSendPortNum;
			recvPortNum = VoyagerDefaults.udpRecvPort;
			onScreenLogs = true;
		}

		public VoyagerDeviceConfig(string inIpAddr, int inSendPortNum, int inRecvPortNum, bool inOnScreenLogs)
		{
			ipAddr = inIpAddr;
			sendPortNum = inSendPortNum;
			recvPortNum = inRecvPortNum;
			onScreenLogs = inOnScreenLogs;
		}

		/** Converts to readable String */
		public override string ToString()
		{
			return string.Format("[ IP: '{0}', SendPort: {1}, RecvPort: {2}, ScreenLogs: {3} ]", ipAddr, sendPortNum, recvPortNum, onScreenLogs);
		}
	}

	/**
	 * Set of Configuration Data for the VoyagerDevice interface.
	 */
	[ Serializable ]
	public struct FVoyagerDeviceConfigSet
	{
		/** Which config setting to use. If VDC_Default it will used Localhost  */
		public VoyagerDeviceConfigId use;

		/** LocalHost Config */
		public VoyagerDeviceConfig local;

		/** Development Config */
		public VoyagerDeviceConfig development;

		/** Production Config */
		public VoyagerDeviceConfig production;
	};

	/**
	 * Common util functions for Voyager Device Interface API.
	 */
	public class VoyagerDeviceUtils : MonoBehaviour
	{
		/**
		 * Load VoyagerDevice interface configuration from config JSON file.
		 * @param dirName	- Name of Content Directory that contains the config file. Example: "Config"
		 * @param fileName	- Filename of the config to load. Example: "VoyagerDevice.json"
		 * @param settingId	- Settings to use from within the specified config file.
		 *
		 * Per platform config file location:
		 * Android build uses Application.persistentDataPath - Ex: /storage/emulated/0/Android/data/com.gopositron.voyagerdemo/files/Config/VoyagerDevice.json
		 * Windows build uses Application.streamingAssetsPath - Ex: VoyagerDemo\VoyagerDemo_Data\StreamingAssets\Config\VoyagerDevice.json
		 *
		 * Editor uses Application.streamingAssetsPath - Ex: VoyagerDemo\Assets\StreamingAssets\Config\VoyagerDevice.json
		 */
		public static VoyagerDeviceConfig LoadDeviceConfigFile(string dirName, string fileName, VoyagerDeviceConfigId settingId)
		{
			VoyagerDeviceConfig result = new VoyagerDeviceConfig();
			string configPath = Path.Combine(dirName, fileName);

#if UNITY_ANDROID && !UNITY_EDITOR
			// Check if saved config exists otherwise use localhost
			string filePath = Path.Combine( Application.persistentDataPath, configPath );
			if( !File.Exists( filePath ))
			{
				Debug.LogError("VoyagerDeviceUtils >> Failed to load config file '" + filePath);
				return result;
			}
#else
			string filePath = Path.Combine(Application.streamingAssetsPath, configPath);
#endif
			// Load config file
			string configJson = "";

			try
			{
				configJson = File.ReadAllText(filePath);
			}
			catch( System.Exception e )
			{
				Debug.LogError("VoyagerDeviceUtils >> Failed to load config file '" + filePath + "': " + e.Message);
				return result;
			}


			// Convert JSON string to connection info
			FVoyagerDeviceConfigSet configSet = JsonUtility.FromJson<FVoyagerDeviceConfigSet>(configJson);

			// For default we will use the setting specified in the config
			if( settingId == VoyagerDeviceConfigId.VDC_Default )
			{
				settingId = configSet.use;
			}

			switch( settingId )
			{
				case VoyagerDeviceConfigId.VDC_Default:
				case VoyagerDeviceConfigId.VDC_Local:
				{
					result = configSet.local;
					break;
				}

				case VoyagerDeviceConfigId.VDC_Development:
				{
					result = configSet.development;
					break;
				}

				case VoyagerDeviceConfigId.VDC_Production:
				{
					result = configSet.production;
					break;
				}

				default:
				{
					Debug.LogAssertion("VoyagerDeviceUtils >> Unhandled DeviceConfigId: " + settingId.ToString());
					break;
				}
			}

			Debug.Log(String.Format("VoyagerDeviceUtils >> Loaded config '{0}' {1}", settingId.ToString(), result.ToString()));
			Debug.Log(String.Format("VoyagerDeviceUtils >> ...from path '{0}'", filePath));


			return result;
		}

		/** Parse Json string to a DevicePacket. */
		public static bool JsonToDevicePacket( string jsonStr, out VoyagerDevicePacket devicePacket )
		{
			devicePacket = JsonUtility.FromJson<VoyagerDevicePacket>(jsonStr);
			return true;
		}

		/** Serialize DevicePacket data to a JSON FString */
		public static bool DevicePacketToJson( VoyagerDevicePacket devicePacket, out string jsonStr )
		{
			jsonStr = JsonUtility.ToJson(devicePacket);
			return true;
		}
	}
}
