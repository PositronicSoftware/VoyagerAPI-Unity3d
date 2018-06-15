using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class LookAtTarget : MonoBehaviour {
	public Transform target;

	void LateUpdate() {
		if (target != null) {
			transform.LookAt(target);
		}
	}
}