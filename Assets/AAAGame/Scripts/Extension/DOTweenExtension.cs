using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins;
using DG.Tweening.Plugins.Core.PathCore;
using DG.Tweening.Plugins.Options;
using UnityEngine;

public static class DOTweenExtension
{
    public static TweenerCore<Vector3, Path, PathOptions> DOPath(this Rigidbody2D target, Vector3[] path, float duration, PathType pathType = PathType.Linear, PathMode pathMode = PathMode.Full3D, int resolution = 10, Color? gizmoColor = null)
    {
        if (resolution < 1)
        {
            resolution = 1;
        }
        TweenerCore<Vector3, Path, PathOptions> tweenerCore = DOTween.To<Vector3, Path, PathOptions>(PathPlugin.Get(), () => target.position, delegate (Vector3 x)
        {
            target.MovePosition(x);
        }, new Path(pathType, path, resolution, gizmoColor), duration).SetTarget(target);
        tweenerCore.plugOptions.mode = pathMode;
        return tweenerCore;
    }
    public static TweenerCore<float, float, FloatOptions> DOFade(this CanvasGroup canvasGroup, float targetValue, float duration)
    {
        var tweenerCore = DOTween.To(() => canvasGroup.alpha, x => canvasGroup.alpha = x, targetValue, duration);

        return tweenerCore;
    }
    public static TweenerCore<float, float, FloatOptions> DOFade(this TMPro.TextMeshPro textMeshPro, float targetValue, float duration)
    {
        var tweenerCore = DOTween.To(() => textMeshPro.alpha, x => textMeshPro.alpha = x, targetValue, duration);

        return tweenerCore;
    }
}
