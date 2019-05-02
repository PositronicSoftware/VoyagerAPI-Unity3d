# Voyager API Unity3d Tester & Source

Please use the latest release when intergating in to your project > [Releases](https://github.com/PositronicSoftware/VoyagerAPI-Unity3d/releases).

---------------------------------------------------------------------------

## Key Classes

_Class_ **VoyagerDevice**

Implements the network-connection and interface methods to the Voyager chair. Singleton.

_Class_ **VoyagerDeviceConfig**

Defines the network-connection settings for interfacing with a Voyager chair. Use this to initialize the _VoyagerDevice_ instance.

_Class_ **VoyagerDeviceUtils**

Implements loading a _VoyagerDeviceConfig_ object from a JSON config-file. For details, read the 'Device Settings Config' section below.

---------------------------------------------------------------------------

## Using the API - Components

### 01 | Setup VoyagerManager and TimelineControl Components

Create a GameObject in your scene and call it **VoyagerManager**.

Now add the `VoyagerManager` and `TimelineControl` Components to this GameObject.

### 02 | VoyagerManager Properties

![VoyagerManager Properties](Docs/VoyagerManagerProperties.png)

1. `path` should be set to your build executable path `"C:/ExecutableName.exe"` in the inspector.
2. `timelineControl` can be left null. It will be auto-set on Play if a TimelineControl component is detected.
3. You can optimize the VoyagerManager SendTime() frequency on memory-constrained platforms by setting `optimizeSendTime = true` in the inspector.
4. VoyagerManager will load your PSM connection settings from a JSON Config file: See [Device Settings Config](#device-settings-config).

VoyagerManager implements the following Keyboard commands for us to easily create Motion data and test your experience.

Key 				| Command 						
 :------			|  :------						
`Spacebar`			| Toggle Play-Pause			
`Left-Right Arrows`	| Skip Forward-Backward 10 Seconds
`Up-Down Arrows`	| Skip Forward-Backward 30 Seconds
`R`					| Recenter HMD Position + Orientation

</br>

### 03 | TimelineControl Properties

![TimelineControl Properties](Docs/TimelineControlProperties.png)

This component is required if your project is using 1 or more `PlayableDirector` to run the experience.

1. Set the Timeline UI references to handle Scrubbing, Pause, Play, etc..
2. Bind your Timeline UI buttons to VoyagerManager methods like `Play(), Pause(), PlayPause(), Mute(), NextTrack(), PrevTrack()` etc..
3. Use the `TrackSetups` Array to add your **PlayableDirector** reference(s) and their associated **MotionProfile** ( name of the Motion data file used by the Chair ).

</br>

---------------------------------------------------------------------------

## Using the API - CSharp

### 01 | Construct and Init() a VoyagerDevice

```csharp
var voyagerDevice = VoyagerDevice.Instance; // Init Singleton
if( voyagerDevice == null )
{
	Debug.LogError("Failed to create VoyagerDevice Singleton.");
	yield break;
}
```

Once created, the instance must be initialized with the desired network-connection settings.

```csharp
// Load config from file
VoyagerDeviceConfig config = VoyagerDeviceUtils.LoadDeviceConfigFile("Config", "InterfaceConfig.json");

// Initialize interface
VoyagerDevice.Init(config);

// Error: Not initialized
if( !VoyagerDevice.IsInitialized )
{
	Debug.LogError("VoyagerDevice not initialized.");
	yield break;
}
```

### 02 | Connect VoyagerDevice to PSM

The following sequence of calls ensures that the newly created `UVoyagerDevice` instance is linked with the **Positronic Show Manager** ( PSM ) that controls the chair.

```csharp
// Set the Content Params.
VoyagerDevice.SetContent("Application", "Windows", "Voyager VR Demo", "1.0");

// Experience should start in Paused state.
VoyagerDevice.Pause();

// Set the Content ID.
VoyagerDevice.LoadContent("C:/ExecutableName.exe");

// Notify PSM that loading is complete.
VoyagerDevice.Loaded(true);

// Set the initial Motion Profile track name.
VoyagerDevice.SetMotionProfile( "TestProfile" );
```

### 03 | Listen To UVoyagerDevice Events

The `VoyagerDevice` has useful events that you can Bind to in order to control your experience. These events are triggered based on calls from Positronic Show Manager ( PSM ).

```csharp
VoyagerDevice.OnPlayStateChange += OnVoyagerPlayStateChange;
VoyagerDevice.OnPlay += OnVoyagerPlay;
VoyagerDevice.OnPaused += OnVoyagerPaused;
VoyagerDevice.OnStopped += OnVoyagerStopped;
VoyagerDevice.OnMuteToggle += OnVoyagerToggleMute;
VoyagerDevice.OnRecenter += OnVoyagerRecenterHMD;
VoyagerDevice.OnMotionProfileChange += OnVoyagerMotionProfileChange;
```

### 04 | Send Experience Time back to PSM

```csharp
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
}
```

For the Voyager Chair to accurately synchronize motion with the experience, you must frequently send the current _Experience Time_ back to PSM. Think of this as the current playback-time of your experience i.e. When the experience is paused time will not increase.

**Caveats**:

* You must continue to send _Experience Time_ back to PSM even if the Voyager state is Paused.
* If PSM stops receiving time data from the API for a certain duration it will cause errors.

### 05 | Key Commands for Motion Encoding and Testing

For us to easily create Motion data for your experience, and test it, we require projects to support the following Keyboard commands.

Key 				| Command 						
 :------			|  :------						
`Spacebar`			| Toggle Play-Pause			
`Right-Left Arrows`	| Skip Forward-Backward 10 Seconds
`Up-Down Arrows`	| Skip Forward-Backward 30 Seconds
`R`					| Recenter HMD Position + Orientation

</br>

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


