using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityGameFramework.Runtime;
using UnityEngine.UI;
public class CustomUIGroupHelper : UIGroupHelperBase
{
    public const int DepthFactor = 100;

    private int m_Depth = 0;
    private Canvas m_CachedCanvas = null;

    /// <summary>
    /// 设置界面组深度。
    /// </summary>
    /// <param name="depth">界面组深度。</param>
    public override void SetDepth(int depth)
    {
        m_Depth = depth;
        m_CachedCanvas.overrideSorting = true;
        m_CachedCanvas.sortingOrder = DepthFactor * depth;
    }

    private void Awake()
    {
        m_CachedCanvas = gameObject.GetOrAddComponent<Canvas>();
        gameObject.GetOrAddComponent<GraphicRaycaster>();
    }

    private void Start()
    {
        m_CachedCanvas.overrideSorting = true;
        m_CachedCanvas.sortingOrder = DepthFactor * m_Depth;

        RectTransform transform = GetComponent<RectTransform>();
        transform.anchorMin = Vector2.zero;
        transform.anchorMax = Vector2.one;
        transform.anchoredPosition = Vector2.zero;
        transform.sizeDelta = Vector2.zero;
        transform.localPosition = Vector3.zero;
    }
}
