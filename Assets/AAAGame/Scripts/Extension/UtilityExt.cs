using GameFramework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public partial class UtilityExt
{
    public static bool CheckNetwork()
    {
        return Application.internetReachability != NetworkReachability.NotReachable;
    }
    /// <summary>
    /// 用于移动平台检测是否点击到UI元素上
    /// </summary>
    /// <param name="screenPosition"></param>
    /// <returns></returns>
    public static bool IsPointerOverUIObject(Vector2 screenPosition)
    {
#if UNITY_IOS || UNITY_ANDROID
        PointerEventData eventDataCurrentPosition = new PointerEventData(EventSystem.current);
        eventDataCurrentPosition.position = new Vector2(screenPosition.x, screenPosition.y);

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventDataCurrentPosition, results);

        return results.Count > 0;
#else
        return EventSystem.current.IsPointerOverGameObject();
#endif

    }
}
