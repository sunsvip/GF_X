using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class TransformExtension
{
    public static void DoBlinkScale(this Transform node, float inScale, float outScale, float inDuration, float outDuration, TweenCallback onAnimComplete = null, object target = null)
    {
        var seq = DOTween.Sequence();
        seq.Append(node.DOScale(inScale, inDuration));
        seq.Append(node.DOScale(outScale, outDuration));
        seq.onComplete = onAnimComplete;
        seq.SetTarget(target);
    }
    public static Transform FindWithTag(this Transform node, string tag)
    {
        Transform result = null;
        FindNodeByTag(node, tag, ref result);
        return result;
    }
    public static List<Transform> FindChildrenWithTag(this Transform root, string tag)
    {
        List<Transform> result = new List<Transform>();
        root.GetComponentsInChildren<Transform>(result);

        result.RemoveAll(node => !node.CompareTag(tag));
        return result;
    }

    private static void FindNodeByTag(Transform root, string tag, ref Transform result)
    {
        if (root.CompareTag(tag))
        {
            result = root;
            return;
        }
        foreach (Transform child in root)
        {
            FindNodeByTag(child, tag, ref result);
        }
    }
}
