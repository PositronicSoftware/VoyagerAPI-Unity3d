using System;
using UnityEngine;
using UnityEngine.Playables;
using Positron;

public class PitchYawMixerBehaviour : PlayableBehaviour
{
    bool m_FirstFrameHappened;

    public override void ProcessFrame(Playable playable, FrameData info, object playerData)
    {
        PitchYaw trackBinding = playerData as PitchYaw;

        if(trackBinding == null)
            return;

        Vector3 defaultPitch = trackBinding.startPitch;
        Vector3 defaultYaw = trackBinding.startYaw;

        int inputCount = playable.GetInputCount ();

        float pitchTotalWeight = 0f;
        float yawTotalWeight = 0f;

        Vector3 blendedPitch = Vector3.zero;
        Vector3 blendedYaw = Vector3.zero;

        for (int i = 0; i < inputCount; i++)
        {
            ScriptPlayable<PitchYawBehaviour> playableInput = (ScriptPlayable<PitchYawBehaviour>)playable.GetInput (i);
            PitchYawBehaviour input = playableInput.GetBehaviour ();

            float inputWeight = playable.GetInputWeight(i);

            if (!m_FirstFrameHappened)
            {
                input.startPitch = defaultPitch;
                input.startYaw = defaultYaw;
                m_FirstFrameHappened = true;
            }

            float normalisedTime = (float)(playableInput.GetTime() * input.inverseDuration);
            float tweenProgress = input.currentCurve.Evaluate(normalisedTime);

            if (input.tweenPitch)
            {
                pitchTotalWeight += inputWeight;

                blendedPitch += Vector3.Lerp(input.startPitch, input.endPitch, tweenProgress) * inputWeight;
            }

            if (input.tweenYaw)
            {
                yawTotalWeight += inputWeight;

                blendedYaw += Vector3.Lerp(input.startYaw, input.endYaw, tweenProgress) * inputWeight;
            }
        }

        blendedPitch += defaultPitch * (1f - pitchTotalWeight);
        blendedYaw += defaultYaw * (1f - yawTotalWeight);

        trackBinding.pitch = blendedPitch;
        trackBinding.yaw = blendedYaw;
    }
}