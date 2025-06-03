using System;
using DG.Tweening;
using UnityEngine;

public static class AnimationExtension
{
    public static void PlayBackward(this Animation animation, string name, Action onComplete = null)
    {
        var animState = animation[name];
        float duration = animState.length - 0.001f;
        animState.time = duration;
        animation.Play(name);
        var motionHandle = DOVirtual.Float(animState.length, 0, duration, v =>
        {
            animState.time = v;
        }).SetUpdate(true).SetEase(Ease.Linear).SetTarget(animation);
        if (onComplete != null) motionHandle.onComplete = () => { onComplete.Invoke(); };
    }

    public static void PlayForward(this Animation animation, string name, Action onComplete = null)
    {
        var animState = animation[name];
        float duration = animState.length - 0.001f;
        animState.time = 0;
        animation.Play(name);
        var motionHandle = DOVirtual.Float(0, animState.length, duration, v =>
        {
            animState.time = v;
        }).SetUpdate(true).SetEase(Ease.Linear).SetTarget(animation);
        if (onComplete != null) motionHandle.onComplete = () => { onComplete.Invoke(); };
    }
}
