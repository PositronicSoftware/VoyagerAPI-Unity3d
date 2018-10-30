using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using System.Collections.Generic;

[TrackColor(0.532526f, 0.7186393f, 0.8941177f)]
[TrackClipType(typeof(EnableComponentClip))]
[TrackBindingType(typeof(MonoBehaviour))]
public class EnableComponentTrack : TrackAsset
{
    public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
    {
        return ScriptPlayable<EnableComponentMixerBehaviour>.Create (graph, inputCount);
    }

    public override void GatherProperties (PlayableDirector director, IPropertyCollector driver)
    {
#if UNITY_EDITOR
        MonoBehaviour trackBinding = director.GetGenericBinding(this) as MonoBehaviour;
        if (trackBinding == null)
            return;

        var serializedObject = new UnityEditor.SerializedObject (trackBinding);
        var iterator = serializedObject.GetIterator();
        while (iterator.NextVisible(true))
        {
            if (iterator.hasVisibleChildren)
                continue;

            driver.AddFromName<MonoBehaviour>(trackBinding.gameObject, iterator.propertyPath);
        }
#endif
        base.GatherProperties (director, driver);
    }
}
