using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Positron
{
	public class MotionProfile : MonoBehaviour
	{
		[ SerializeField ]
		private string _profileName = null;

		public string profileName
		{
			get
			{
				return _profileName;
			}
			set
			{
				_profileName = value;

				if( !string.IsNullOrEmpty(_profileName)
				&& Application.isPlaying
				&& VoyagerDevice.MotionProfile != _profileName
				&& VoyagerDevice.PlayState != VoyagerDevicePlayState.Stop ) {
					VoyagerDevice.SetMotionProfile(_profileName);
					// Interface.Play();
				}

				/*else if (string.IsNullOrEmpty(_profileName)
				 * && Application.isPlaying
				 * && Interface.currentMotionProfile != _profileName
				 * && Interface.state != Interface.State.Pause) {
				 *  Interface.Pause();
				 * }*/
			}
		}

        void Start() {
            if (!string.IsNullOrEmpty(_profileName)
			&& Application.isPlaying
			&& VoyagerDevice.MotionProfile != _profileName
			&& VoyagerDevice.PlayState != VoyagerDevicePlayState.Stop) {
                VoyagerDevice.SetMotionProfile(_profileName);
            }
        }


        /*void Update() {
			if (!VoyagerDevice.IsUpdated && VoyagerDevice.IsInitialized) {
				VoyagerDevice.SendData();
			}
		}*/
	}
}
