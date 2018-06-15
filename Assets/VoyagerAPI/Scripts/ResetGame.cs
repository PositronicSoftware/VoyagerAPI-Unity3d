using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ResetGame : MonoBehaviour {

	public Transform start;
	private Vector3 startPos;
	private Quaternion startRot;

	void Start() {
		startPos = start.position;
		startRot = start.rotation;
	}

	void OnEnable() {
		if (start != null) {
			Camera.main.transform.parent.position = startPos;
			Camera.main.transform.parent.rotation = startRot;
		}
	}
}
