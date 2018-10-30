using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

public class EnableComponentMixerBehaviour : PlayableBehaviour
{
    bool m_DefaultEnabled;

    MonoBehaviour m_TrackBinding;
    bool m_FirstFrameHappened;

    public override void ProcessFrame(Playable playable, FrameData info, object playerData)
    {
        m_TrackBinding = playerData as MonoBehaviour;

        if (m_TrackBinding == null)
            return;

        if (!m_FirstFrameHappened)
        {
            m_DefaultEnabled = m_TrackBinding.enabled;
            m_FirstFrameHappened = true;
        }

        int inputCount = playable.GetInputCount ();

        float totalWeight = 0f;
        float greatestWeight = 0f;
        int currentInputs = 0;

        for (int i = 0; i < inputCount; i++)
        {
            float inputWeight = playable.GetInputWeight(i);
            ScriptPlayable<EnableComponentBehaviour> inputPlayable = (ScriptPlayable<EnableComponentBehaviour>)playable.GetInput(i);
            EnableComponentBehaviour input = inputPlayable.GetBehaviour ();
            
            totalWeight += inputWeight;

            if (inputWeight > greatestWeight)
            {
                m_TrackBinding.enabled = input.enabled;
                greatestWeight = inputWeight;
            }

            if (!Mathf.Approximately (inputWeight, 0f))
                currentInputs++;
        }

        if (currentInputs != 1 && 1f - totalWeight > greatestWeight)
        {
            m_TrackBinding.enabled = m_DefaultEnabled;
        }
    }

    public override void OnGraphStop (Playable playable)
    {
        m_FirstFrameHappened = false;

        if (m_TrackBinding == null)
            return;

        m_TrackBinding.enabled = m_DefaultEnabled;
    }
}
