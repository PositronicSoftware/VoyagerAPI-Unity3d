using UnityEngine; 
using UnityEngine.UI; 
using System.Collections;
using UnityEngine.Playables;

namespace Positron {
	public class SwitchTrack : MonoBehaviour { 
		public PlayableDirector[] tracks;
		public PlayableDirector director;

		public void Switch(int track) {
			director = tracks[track];
			director.Play();
		}
	}
}