/* UltimateJoystickScreenSizeUpdater.cs */
/* Written by Kaz Crowe */
using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems;

public class UltimateJoystickScreenSizeUpdater : UIBehaviour
{
    protected override void OnRectTransformDimensionsChange()
    {
        if (gameObject == null || !gameObject.activeInHierarchy)
            return;

        StartCoroutine("YieldPositioning");
    }

    IEnumerator YieldPositioning()
    {
        yield return new WaitForEndOfFrame();
#if UNITY_6000_0_OR_NEWER
        UltimateJoystick[] allJoysticks = FindObjectsByType<UltimateJoystick>(FindObjectsSortMode.None);
#else
		UltimateJoystick[] allJoysticks = FindObjectsOfType( typeof( UltimateJoystick ) ) as UltimateJoystick[];
#endif
        for (int i = 0; i < allJoysticks.Length; i++)
            allJoysticks[i].UpdatePositioning();
    }
}