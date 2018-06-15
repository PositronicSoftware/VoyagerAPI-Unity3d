using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

[Serializable]
public class TransformCurveClip : PlayableAsset, ITimelineClipAsset
{
    public TransformCurveBehaviour template = new TransformCurveBehaviour ();
    public ExposedReference<BezierSpline> spline;
    
    public ClipCaps clipCaps
    {
        get { return ClipCaps.Blending; }
    }

    public override Playable CreatePlayable (PlayableGraph graph, GameObject owner)
    {
        var playable = ScriptPlayable<TransformCurveBehaviour>.Create (graph, template);
        TransformCurveBehaviour clone = playable.GetBehaviour ();
        clone.spline = spline.Resolve (graph.GetResolver ());
        return playable;
    }
}