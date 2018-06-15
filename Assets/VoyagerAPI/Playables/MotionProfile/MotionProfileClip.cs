using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using Positron;

[Serializable]
public class MotionProfileClip : PlayableAsset, ITimelineClipAsset
{
    public MotionProfileBehaviour template = new MotionProfileBehaviour ();

    public ClipCaps clipCaps
    {
        get { return ClipCaps.Blending; }
    }

    public override Playable CreatePlayable (PlayableGraph graph, GameObject owner)
    {
        var playable = ScriptPlayable<MotionProfileBehaviour>.Create (graph, template);
        return playable;    }
}
