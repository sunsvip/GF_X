#if UNITY_2022_3_14
using UnityEngine;
using UnityEngine.Rendering.Universal;

public static class URPExtension
{
    public static UniversalAdditionalCameraData GetUniversalAdditionalCameraData(this Camera cam)
    {
        return cam.GetComponent<UniversalAdditionalCameraData>();
    }
}
#endif