# Voyager API Unity3d Tester & Source

For API usage and info on how to initialize the Voyager chair through API calls refer to the [Voyager API Docs](https://docs.google.com/document/d/1lZc5NaYeIUBKR2u4wBalcRn69S3lgsBh_itUxj5fI5w/).

---------------------------------------------------------------------------

## Key Classes & Structs

_Class_ **VoyagerDevice**

Implements the network-connection and interface methods to the Voyager chair. Singleton.

_Class_ **VoyagerDeviceConfig**

Defines the network-connection settings for interfacing with a Voyager chair. Use this to initialize the _VoyagerDevice_ instance.

_Class_ **VoyagerDeviceUtils**

Implements loading a _VoyagerDeviceConfig_ object from a JSON config-file. For details, read the 'Device Settings Config' section below.

---------------------------------------------------------------------------

## Using the API

### 01 Initialize the VoyagerDevice

VoyagerDevice must be initialized with the desired network-connection settings. Create a `VoyagerDeviceConfig` object, and call `VoyagerDevice::Init()`.

```c#
VoyagerDeviceConfig config = new VoyagerDeviceConfig(...); // Create configuration settings for the API.

VoyagerDevice.Init( config ); // Initialize the interface w/ the config.
```

**Caveats**

* Calling `VoyagerDevice::Init()` results in listener & receiver socket-thread creation. 
* `Init()` will fail ( and possibly crash the application ) if the provided config port numbers collide with OS reserved port, or other applications.

### 02 Access to VoyagerDevice API

VoyagerDevice is setup as a singleton with access to Instance fields and functions through properties and static methods.

**Caveats**: Calling API methods on _VoyagerDevice_ before it’s initialized will cause errors! You can check `VoyagerDevice::IsInitialized` prior to starting calls.

</br>

> As in the provided example project, we recommend initializing _VoyagerDevice_ in `Awake()`.

---------------------------------------------------------------------------


## Device Settings Config

If you wish to load _VoyagerDeviceConfig_ settings from a config-file, do the following:

1. Setup a folder in '<ProjectName>/Assets/StreamingAssets/' directory, that will contain your config file(s). 

2. Your config-file must contain valid JSON with the following structure:
```json
{
	"use": 1,
	"comment": "Change the 'use' property above to select settings: 1-local, 2-development, 3-production",
	"local":
	{
		"ipAddr": "127.0.0.1", "sendPortNum": 61557, "recvPortNum": 7755, "onScreenLogs": true
	},
	"development":
	{
		"ipAddr": "192.166.0.82", "sendPortNum": 61557, "recvPortNum": 7755, "onScreenLogs": true
	},
	"production":
	{
		"ipAddr": "192.168.13.100", "sendPortNum": 61557, "recvPortNum": 7755, "onScreenLogs": true
	}
}
```
3. Use the `VoyagerDeviceUtils::LoadDeviceConfigFile()` method to load settings from your config.

```c#
// Name of directory that contains config file(s)
string configDir = "Config";

// Name of Device Config file to load
string fileName = "ExampleConfig.json";

// Load config from file
VoyagerDeviceConfig deviceConfig = VoyagerDeviceUtils.LoadDeviceConfigFile(configDir, fileName);

VoyagerDevice.Init(deviceConfig); // Initialize the interface w/ the config.
```


`LoadDeviceConfigFile()` will look for config files in the following locations.

| **Platform** | **Config Location** |
| :-------- | :-------- |
| In-Editor | `<ProjectDir>\Assets\StreamingAssets\<ConfigDir>\<FileName>` |
| Windows Build | `<BuildFolder>\<ProjectName>_Data\StreamingAssets\<ConfigDir>\<FileName>` |
| OculusGo Build | `/storage/emulated/0/Android/data/<packagename>/files/<ConfigDir>/<FileName>` |

**In-Editor and Windows Builds**

You can edit your config-file(s) without having to rebuild ( assuming you did step 2 ). This allows you to quickly test different network settings.

**Oculus-Go Builds**

For Android, the config loading system will use the  `'/storage/emulated/0/Android/data/<packagename>/files/<ConfigDir>/...'` path for your configs.

You can use the provided script `BatchScripts/Push_DeviceConfig.bat` to push your config file(s) to the _Go_ through ADB. This allows you to test different network settings without having to re-deploy the app.

</br>

> See the Example Project for a working implementation of this in 'VoyagerAPITest.cs', in the `Awake()` method.

---------------------------------------------------------------------------

## Project Info

This project implements a simple demo scene that shows you how to use VoyagerDevice.

**VoyagerAPITest.cs**

Povides a working example of how to initialize a VoyagerDevice

**Voyager API Test.unity**

A simple test scene for the API. UI buttons make API calls to the VoyagerDevice Instance. UI Tested with with mouse, Rift remote click, and gaze in Oculus Go.

---------------------------------------------------------------------------

## Debugging

**Windows**

Runtime logging from the API is written to the log file.

**Oculus GO**

You can use our batch script Push_DeviceConfig.bat in `BatchScripts/` to push a new JSON config file(s)

**Log File Locations**

See https://docs.unity3d.com/Manual/LogFiles.html

---------------------------------------------------------------------------

## License

_Copyright 2018 Positron Voyager, Inc. All Rights Reserved_


