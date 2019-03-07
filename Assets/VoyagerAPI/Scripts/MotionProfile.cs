using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Positron
{
	public class MotionProfile : MonoBehaviour
	{
		[ SerializeField ]
		private string _profileName = null;

		public string ProfileName
		{
			get{ return _profileName; }
			set
			{
				_profileName = value;

				if( !string.IsNullOrEmpty(_profileName)
					&& Application.isPlaying
					&& VoyagerDevice.MotionProfile != _profileName
					&& VoyagerDevice.PlayState != VoyagerDevicePlayState.Stop )
				{
					// This has been made redundant by TimelineControl Track setup.
					// VoyagerDevice.SetMotionProfile( _profileName );
				}
			}
		}

		void Start()
		{
			if( !string.IsNullOrEmpty(_profileName)
				&& Application.isPlaying
				&& VoyagerDevice.MotionProfile != _profileName
				&& VoyagerDevice.PlayState != VoyagerDevicePlayState.Stop )
			{
				// This has been made redundant by TimelineControl Track setup.
				// VoyagerDevice.SetMotionProfile( _profileName );
			}
		}
	}
}
