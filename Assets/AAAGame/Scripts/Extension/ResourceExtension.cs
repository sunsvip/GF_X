using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GameFramework;
using UnityGameFramework.Runtime;
using GameFramework.Resource;

public static class ResourceExtension
{
    public static void LoadAsset(this ResourceComponent com, string assetName, LoadAssetSuccessCallback loadAssetSuccessCallback, LoadAssetFailureCallback loadAssetFailureCallback=null, LoadAssetUpdateCallback loadAssetUpdateCallback = null, LoadAssetDependencyAssetCallback loadAssetDependencyAssetCallback = null)
    {
        LoadAssetCallbacks callbacks = new LoadAssetCallbacks(loadAssetSuccessCallback, loadAssetFailureCallback, loadAssetUpdateCallback, loadAssetDependencyAssetCallback);
        com.LoadAsset(assetName, callbacks);
    }
}
