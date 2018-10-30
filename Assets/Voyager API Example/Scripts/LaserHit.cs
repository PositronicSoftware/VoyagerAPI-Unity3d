using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Positron {
	public class LaserHit : MonoBehaviour {
		public GameObject laserHit;

		// Use this for initialization
		void Start () {
			Destroy(gameObject, 2f);
		}

		void OnTriggerEnter(Collider hit) {
			if (laserHit != null && hit.CompareTag("Asteroid")) {
				GameObject explosion = GameObject.Instantiate(laserHit, hit.ClosestPointOnBounds(transform.position), Quaternion.identity);

				Destroy(hit.gameObject);

				Destroy(explosion, explosion.GetComponent<ParticleSystem>().duration);

				Destroy(gameObject);
			}
			else {
				Destroy(gameObject);
			}
		}
	}
}
