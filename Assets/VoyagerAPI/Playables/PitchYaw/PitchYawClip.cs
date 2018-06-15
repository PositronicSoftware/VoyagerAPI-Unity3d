using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using Positron;

[Serializable]
public class PitchYawClip : PlayableAsset, ITimelineClipAsset
{
    public PitchYawBehaviour template = new PitchYawBehaviour ();

    public ClipCaps clipCaps
    {
        get { return ClipCaps.Blending; }
    }

    public override Playable CreatePlayable (PlayableGraph graph, GameObject owner)
    {
        var playable = ScriptPlayable<PitchYawBehaviour>.Create (graph, template);
        return playable;    }
}
