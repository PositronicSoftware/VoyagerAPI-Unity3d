using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Positron {

	public class PitchYaw : MonoBehaviour {
		public Vector3 startPitch = Vector3.zero;
		public Vector3 endPitch = Vector3.zero;

		public Vector3 startYaw = Vector3.zero;
		public Vector3 endYaw = Vector3.zero;

		[SerializeField]
		private Vector3 _pitch = Vector3.zero;
		public Vector3 pitch {
			get {
				return _pitch;
			}
			set {
				_pitch = value;

				if (Application.isPlaying) { 
					VoyagerDevice.Pitch(_pitch);
					VoyagerDevice.Play();
				}
			}
		}

		[SerializeField]
		private Vector3 _yaw = Vector3.zero;
		public Vector3 yaw {
			get {
				return _yaw;
			}
			set {
				_yaw = value;

				if (Application.isPlaying) { 
					VoyagerDevice.Yaw(_yaw);
					VoyagerDevice.Play();
				}
			}
		}

		void Update() {
			VoyagerDevice.SendTimeNow();
		}
	}
}
