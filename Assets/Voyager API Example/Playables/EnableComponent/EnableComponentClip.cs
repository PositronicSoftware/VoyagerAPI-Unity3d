using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

[Serializable]
public class EnableComponentClip : PlayableAsset, ITimelineClipAsset
{
    public EnableComponentBehaviour template = new EnableComponentBehaviour ();

    public ClipCaps clipCaps
    {
        get { return ClipCaps.Blending; }
    }

    public override Playable CreatePlayable (PlayableGraph graph, GameObject owner)
    {
        var playable = ScriptPlayable<EnableComponentBehaviour>.Create (graph, template);
        return playable;
    }
}
