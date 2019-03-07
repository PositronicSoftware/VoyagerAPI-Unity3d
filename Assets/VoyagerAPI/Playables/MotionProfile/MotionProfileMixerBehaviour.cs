using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using Positron;

public class MotionProfileMixerBehaviour : PlayableBehaviour
{
    string m_ProfileName;

    MotionProfile m_TrackBinding;
    bool m_FirstFrameHappened;

    public override void ProcessFrame(Playable playable, FrameData info, object playerData)
    {
        m_TrackBinding = playerData as MotionProfile;

        if (m_TrackBinding == null)
		{
            return;
		}

        if (!m_FirstFrameHappened)
        {
            m_ProfileName = m_TrackBinding.ProfileName;
            m_FirstFrameHappened = true;
        }

        int inputCount = playable.GetInputCount();
        float totalWeight = 0f;
        float greatestWeight = 0f;
        int currentInputs = 0;

        for (int i = 0; i < inputCount; i++)
        {
            float inputWeight = playable.GetInputWeight(i);
            ScriptPlayable<MotionProfileBehaviour> inputPlayable = (ScriptPlayable<MotionProfileBehaviour>)playable.GetInput(i);
            MotionProfileBehaviour input = inputPlayable.GetBehaviour ();

            totalWeight += inputWeight;

            if (inputWeight > greatestWeight)
            {
                m_TrackBinding.ProfileName = input.profileName;
                greatestWeight = inputWeight;
            }

            if (!Mathf.Approximately (inputWeight, 0f))
                currentInputs++;
        }

        if (currentInputs != 1 && 1f - totalWeight > greatestWeight)
        {
            m_TrackBinding.ProfileName = m_ProfileName;
        }

		if (Application.isPlaying)
		{
			if (!VoyagerDevice.IsUpdated && VoyagerDevice.IsInitialized && !TimelineControl.OverrideTime)
			{
				VoyagerDevice.SetTimeSeconds((float)playable.GetTime());
			}
		}
    }

    public override void OnGraphStop( Playable playable )
    {
		if( m_TrackBinding  )
		{
			m_TrackBinding.ProfileName = m_ProfileName;
		}
    }
}
