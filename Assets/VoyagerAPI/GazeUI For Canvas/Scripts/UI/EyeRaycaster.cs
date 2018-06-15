/* Copyright 2017 Positron code by Brad Nelson code derived from original Eyecaster script */

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.XR;

namespace Positron {
	public class EyeRaycaster : MonoBehaviour {

		[SerializeField]
		private float loadingTime;
		[SerializeField]
		private float sliderIncrement;
		[SerializeField]
		private Color activeColor;
		[SerializeField]
		private AnimationCurve curve;
		[SerializeField]
		private bool forceActive;

		private float endFocusTime;
		private float progress;

		private RectTransform indicatorFillRT;
		private RawImage indicatorFillRawImage;
		private RawImage centerRawImage;

		private GameObject lastActivatedTarget;
		private GameObject target;

		private Slider slider;

		static public bool hasTarget = false;

		void Start() {
			indicatorFillRT = transform.Find("IndicatorFill").GetComponent<RectTransform>();
			indicatorFillRawImage = transform.Find("IndicatorFill").GetComponent<RawImage>();
			centerRawImage = transform.Find("Center").GetComponent<RawImage>();

			gameObject.SetActive(UnityEngine.XR.XRSettings.enabled || forceActive);

			endFocusTime = Time.time + loadingTime;
		}

		void Update() {
			// Centre of the screen
			PointerEventData pointer = new PointerEventData(EventSystem.current);
			if (XRSettings.enabled) {
				pointer.position = new Vector2(XRSettings.eyeTextureWidth / 2, XRSettings.eyeTextureHeight / 2);
			}
			else {
				pointer.position = new Vector2(Screen.width / 2, Screen.height / 2);
			}
			pointer.button = PointerEventData.InputButton.Left;

			List<RaycastResult> raycastResults = new List<RaycastResult>();
			EventSystem.current.RaycastAll(pointer, raycastResults);

			if (raycastResults.Count > 0) {
				// Target is being activating -> fade in anim
				if (target == raycastResults[0].gameObject && target != lastActivatedTarget) {
					progress = Mathf.Lerp(1, 0, (endFocusTime - Time.time) / loadingTime);

					indicatorFillRT.localScale = Vector3.Lerp(Vector3.zero, Vector3.one, curve.Evaluate(progress));
					indicatorFillRawImage.color = Color.Lerp(Color.clear, activeColor, curve.Evaluate(progress));
					centerRawImage.color = Color.Lerp(Color.white, Color.white, curve.Evaluate(progress));

					if (target.GetComponent<ISubmitHandler>() != null)
						EventSystem.current.SetSelectedGameObject(target);
					else if (target.GetComponentInParent<ISubmitHandler>() != null)
						EventSystem.current.SetSelectedGameObject(target.transform.parent.gameObject);

					if (target.GetComponent<Selectable>())
						target.GetComponent<Selectable>().OnPointerEnter(pointer);

					if (Time.time >= endFocusTime && target != lastActivatedTarget 
					|| Input.GetButtonUp("Submit") && target != lastActivatedTarget) {
						lastActivatedTarget = target;

						if (!Input.GetButtonUp("Submit")) {
							if (target.GetComponent<ISubmitHandler>() != null)
								target.GetComponent<ISubmitHandler>().OnSubmit(pointer);
							else if (target.GetComponentInParent<ISubmitHandler>() != null)
								target.GetComponentInParent<ISubmitHandler>().OnSubmit(pointer);
						}

						slider = target.GetComponentInParent<Slider>();

						if (slider != null) {
							// Debug.Log(target.GetComponentInParent<Slider>().name);
							lastActivatedTarget = null;
							endFocusTime = Time.time + loadingTime;

							Vector3 handlePos = Camera.main.WorldToScreenPoint(slider.handleRect.position);

							if (slider.direction == Slider.Direction.BottomToTop && pointer.position.y >= handlePos.y
							|| slider.direction == Slider.Direction.LeftToRight && pointer.position.x >= handlePos.x) {
								slider.normalizedValue += sliderIncrement;
							}
							else if (slider.direction == Slider.Direction.BottomToTop && pointer.position.y < handlePos.y
							|| slider.direction == Slider.Direction.LeftToRight && pointer.position.x < handlePos.x) {
								slider.normalizedValue -= sliderIncrement;
							}
						}
					}
				}

				// Target activated -> fade out anim
				else {
					if (target && target.GetComponent<Selectable>()) 
						target.GetComponent<Selectable>().OnPointerExit(pointer);

					if(target != raycastResults[0].gameObject) {
						target = raycastResults[0].gameObject;
						endFocusTime = Time.time + loadingTime;
					}

					progress = Mathf.Lerp(0, 1, (Time.time - endFocusTime) / loadingTime * 2);

					indicatorFillRawImage.color = Color.Lerp(Color.white, Color.clear, curve.Evaluate(progress));
					centerRawImage.color = Color.Lerp(activeColor, Color.white, curve.Evaluate(progress));
				}

				hasTarget = true;
			}

			// No target -> reset
			else {
				lastActivatedTarget = null;

				if (target && target.GetComponent<Selectable>())
					target.GetComponent<Selectable>().OnPointerExit(pointer);

				target = null;

				indicatorFillRT.localScale = Vector3.zero;
				endFocusTime = Time.time + loadingTime;

				hasTarget = false;
			}
		}
	}
}
