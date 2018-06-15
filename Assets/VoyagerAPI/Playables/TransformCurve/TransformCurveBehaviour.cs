using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

[Serializable]
public class TransformCurveBehaviour : PlayableBehaviour
{
	public BezierSpline spline;

	public SplineWalkerMode mode = SplineWalkerMode.Once;

    public enum TweenType
    {
        Linear,
        Deceleration,
        Harmonic,
        Custom,
    }

    public bool tweenPosition = true;
    public bool tweenRotation = true;
    public TweenType tweenType;
    public float customStartingSpeed;
    public float customEndingSpeed;

    public float inverseDuration;
    public Vector3 startingPosition;
    public Quaternion startingRotation;
    public AnimationCurve currentCurve;

    AnimationCurve m_LinearCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
    AnimationCurve m_DecelerationCurve = new AnimationCurve
    (
        new Keyframe(0f, 0f, -k_RightAngleInRads, k_RightAngleInRads),
        new Keyframe(1f, 1f, 0f, 0f)
    );
    AnimationCurve m_HarmonicCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    AnimationCurve m_CustomCurve;

    const float k_RightAngleInRads = Mathf.PI * 0.5f;

    public override void OnGraphStart(Playable playable)
    {
        double duration = playable.GetDuration();
        if (Mathf.Approximately((float)duration, 0f))
            throw new UnityException("A TransformCurve cannot have a duration of zero.");

        inverseDuration = 1f / (float)duration;

        m_CustomCurve = new AnimationCurve
        (
            new Keyframe(0f, 0f, -customStartingSpeed * k_RightAngleInRads, customStartingSpeed * k_RightAngleInRads),
            new Keyframe(1f, 1f, customEndingSpeed * k_RightAngleInRads, customEndingSpeed * k_RightAngleInRads)
        );

        switch (tweenType)
        {
            case TweenType.Linear:
                currentCurve = m_LinearCurve;
                break;
            case TweenType.Deceleration:
                currentCurve = m_DecelerationCurve;
                break;
            case TweenType.Harmonic:
                currentCurve = m_HarmonicCurve;
                break;
            case TweenType.Custom:
                currentCurve = m_CustomCurve;
                break;
        }
    }
}