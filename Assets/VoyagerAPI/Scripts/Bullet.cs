using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Positron {
	public class Bullet : MonoBehaviour {
		public GameObject bulletHit;

		// Use this for initialization
		void Start () {
			Destroy(gameObject, 2f);
		}

		// Update is called once per frame
		void OnCollisionEnter(Collision hit) {
			if(bulletHit != null) {
				ContactPoint contact = hit.contacts[0];
				var hitRotation = Quaternion.FromToRotation(Vector3.up, contact.normal);
				GameObject.Instantiate(bulletHit, contact.point, hitRotation);
				Destroy(gameObject);
			}
		}
	}
}
