using UnityEngine;

namespace Positron
{
	public class PitchYaw : MonoBehaviour
	{
		public float startPitch = 0f;
		public float endPitch = 0f;
		public float startYaw = 0f;
		public float endYaw = 0f;

		[SerializeField]
		private float _pitch = 0;
		public float pitch
		{
			get {return _pitch;}
			set
			{
				_pitch = value;

				if (Application.isPlaying)
				{ 
					VoyagerDevice.SetPitch(_pitch);
					VoyagerDevice.Play();
				}
			}
		}

		[SerializeField]
		private float _yaw = 0;
		public float yaw
		{
			get {return _yaw;}
			set
			{
				_yaw = value;

				if (Application.isPlaying)
				{ 
					VoyagerDevice.SetYaw(_yaw);
					VoyagerDevice.Play();
				}
			}
		}

		void Update()
		{
			VoyagerDevice.SendTimeNow();
		}
	}
}
