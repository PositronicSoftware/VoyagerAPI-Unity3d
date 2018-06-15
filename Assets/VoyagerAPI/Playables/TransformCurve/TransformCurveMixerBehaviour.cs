using System;
using UnityEngine;
using UnityEngine.Playables;

public class TransformCurveMixerBehaviour : PlayableBehaviour
{
    bool m_FirstFrameHappened;

	Transform trackBinding = null;

    public override void ProcessFrame(Playable playable, FrameData info, object playerData)
    {
        trackBinding = playerData as Transform;

        if(trackBinding == null)
            return;

        Vector3 defaultPosition = trackBinding.position;
        Vector3 defaultRotation = Vector3.zero;

        int inputCount = playable.GetInputCount ();

        float positionTotalWeight = 0f;
        float rotationTotalWeight = 0f;

        Vector3 blendedPosition = Vector3.zero;
        Vector3 blendedRotation = Vector3.zero;
		bool goingForward = true;
		bool lookForward = false;

        for (int i = 0; i < inputCount; i++)
        {
            ScriptPlayable<TransformCurveBehaviour> playableInput = (ScriptPlayable<TransformCurveBehaviour>)playable.GetInput (i);
            TransformCurveBehaviour input = playableInput.GetBehaviour ();

			if (input.spline == null) {
				continue;
			}

            float inputWeight = playable.GetInputWeight(i);

			if (inputWeight <= 0f) {
				return;
			}

            if (!m_FirstFrameHappened)
            {
                m_FirstFrameHappened = true;
            }

            float normalisedTime = (float)(playableInput.GetTime() * input.inverseDuration);
            float tweenProgress = input.currentCurve.Evaluate(normalisedTime);
			float progress = 0f;
			if (goingForward) {
				progress += normalisedTime;
				if (progress > 1f) {
					if (input.mode == SplineWalkerMode.Once) {
						progress = 1f;
					}
					else if (input.mode == SplineWalkerMode.Loop) {
						progress -= 1f;
					}
					else {
						progress = 2f - progress;
						goingForward = false;
					}
				}
			}
			else {
				progress -= normalisedTime;
				if (progress < 0f) {
					progress = -progress;
					goingForward = true;
				}
			}

			if (progress > 0.99f) {
				progress = 1f;
			}

            if (input.tweenPosition)
            {
                positionTotalWeight += inputWeight;

                blendedPosition += input.spline.GetPoint(progress) * inputWeight;
            }

            if (input.tweenRotation)
            {
                rotationTotalWeight += inputWeight;

                blendedRotation += (blendedPosition + input.spline.GetDirection(progress)) * inputWeight;
				lookForward = true;
            }

			/*Debug.Log (trackBinding.name + " " + progress);
			Debug.Log (trackBinding.name + " " + normalisedTime);
			Debug.Log (trackBinding.name + " " + tweenProgress);
			Debug.Log (trackBinding.name + " " + inputWeight);
			Debug.Log (trackBinding.name + " " + input.spline.GetPoint(progress));
			Debug.Log (trackBinding.name + " " + input.spline.GetDirection(progress));
			Debug.Log (trackBinding.name + " " + blendedPosition);
			Debug.Log (trackBinding.name + " " + blendedRotation);*/
        }

		// Debug.Log (trackBinding.name + " " + positionTotalWeight);
		// Debug.Log (trackBinding.name + " " + blendedPosition);

        blendedPosition += defaultPosition * (1f - positionTotalWeight);
        blendedRotation += defaultRotation * (1f - rotationTotalWeight);

		// Debug.Log (trackBinding.name + " " + blendedPosition);

        trackBinding.position = blendedPosition;

		if (lookForward) {
			trackBinding.LookAt(blendedRotation);
		}
    }
}