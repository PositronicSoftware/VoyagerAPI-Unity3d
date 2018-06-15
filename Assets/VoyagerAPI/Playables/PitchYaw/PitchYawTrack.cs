using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using Positron;

[TrackColor(0.855f,0.8623f,0.870f)]
[TrackClipType(typeof(PitchYawClip))]
[TrackBindingType(typeof(PitchYaw))]
public class PitchYawTrack : TrackAsset
{
	public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
	{
	    return ScriptPlayable<PitchYawMixerBehaviour>.Create (graph, inputCount);
	}

    public override void GatherProperties(PlayableDirector director, IPropertyCollector driver)
    {
#if UNITY_EDITOR
        var comp = director.GetGenericBinding(this) as PitchYaw;
        if (comp == null)
            return;
        var so = new UnityEditor.SerializedObject(comp);
        var iter = so.GetIterator();
        while (iter.NextVisible(true))
        {
            if (iter.hasVisibleChildren)
                continue;
            driver.AddFromName<PitchYaw>(comp.gameObject, iter.propertyPath);
        }
#endif
        base.GatherProperties(director, driver);
    }
}
