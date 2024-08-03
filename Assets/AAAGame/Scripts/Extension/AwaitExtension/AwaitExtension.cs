using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using GameFramework;
using GameFramework.DataTable;
using GameFramework.Event;
using GameFramework.Resource;
using UnityEngine;
using UnityGameFramework.Runtime;

public static class AwaitExtension
{
    private static readonly Dictionary<int, UniTaskCompletionSource<UIFormLogic>> mUIFormTask = new Dictionary<int, UniTaskCompletionSource<UIFormLogic>>();
    private static readonly Dictionary<int, UniTaskCompletionSource<EntityLogic>> mEntityTask = new Dictionary<int, UniTaskCompletionSource<EntityLogic>>();
    private static readonly Dictionary<string, UniTaskCompletionSource<bool>> mDataTableTask = new Dictionary<string, UniTaskCompletionSource<bool>>();
    private static readonly Dictionary<string, UniTaskCompletionSource<bool>> mLoadSceneTask = new Dictionary<string, UniTaskCompletionSource<bool>>();
    private static readonly Dictionary<string, UniTaskCompletionSource<bool>> mUnLoadSceneTask = new Dictionary<string, UniTaskCompletionSource<bool>>();
    private static readonly HashSet<int> mWebSerialIds = new HashSet<int>();
    private static readonly List<WebRequestResult> mDelayReleaseWebResult = new List<WebRequestResult>();
    private static readonly HashSet<int> mDownloadSerialIds = new HashSet<int>();
    private static readonly List<DownloadResult> mDelayReleaseDownloadResult = new List<DownloadResult>();

#if UNITY_EDITOR
    private static bool isSubscribeEvent = false;
#endif

    public static void SubscribeEvent()
    {
        GFBuiltin.Event.Subscribe(OpenUIFormSuccessEventArgs.EventId, OnOpenUIFormSuccess);
        GFBuiltin.Event.Subscribe(OpenUIFormFailureEventArgs.EventId, OnOpenUIFormFailure);

        GFBuiltin.Event.Subscribe(ShowEntitySuccessEventArgs.EventId, OnShowEntitySuccess);
        GFBuiltin.Event.Subscribe(ShowEntityFailureEventArgs.EventId, OnShowEntityFailure);

        GFBuiltin.Event.Subscribe(LoadSceneSuccessEventArgs.EventId, OnLoadSceneSuccess);
        GFBuiltin.Event.Subscribe(LoadSceneFailureEventArgs.EventId, OnLoadSceneFailure);
        GFBuiltin.Event.Subscribe(UnloadSceneSuccessEventArgs.EventId, OnUnloadSceneSuccess);
        GFBuiltin.Event.Subscribe(UnloadSceneFailureEventArgs.EventId, OnUnloadSceneFailure);

        GFBuiltin.Event.Subscribe(LoadDataTableSuccessEventArgs.EventId, OnLoadDataTableSuccess);
        GFBuiltin.Event.Subscribe(LoadDataTableFailureEventArgs.EventId, OnLoadDataTableFailure);

        GFBuiltin.Event.Subscribe(WebRequestSuccessEventArgs.EventId, OnWebRequestSuccess);
        GFBuiltin.Event.Subscribe(WebRequestFailureEventArgs.EventId, OnWebRequestFailure);

        GFBuiltin.Event.Subscribe(DownloadSuccessEventArgs.EventId, OnDownloadSuccess);
        GFBuiltin.Event.Subscribe(DownloadFailureEventArgs.EventId, OnDownloadFailure);
#if UNITY_EDITOR
        isSubscribeEvent = true;
#endif
    }

#if UNITY_EDITOR
    private static void TipsSubscribeEvent()
    {
        if (!isSubscribeEvent)
        {
            throw new Exception("Use await/async extensions must to subscribe event!");
        }
    }
#endif
    /// <summary>
    /// 打开界面（可等待）
    /// </summary>
    public static UniTask<UIFormLogic> OpenUIFormAsync(this UIComponent uiCom, UIViews viewId, UIParams parms = null)
    {
#if UNITY_EDITOR
        TipsSubscribeEvent();
#endif
        int serialId = uiCom.OpenUIForm(viewId, parms);
        if (serialId < 0)
        {
            return UniTask.FromResult((UIFormLogic)null);
        }

        var tcs = new UniTaskCompletionSource<UIFormLogic>();
        mUIFormTask.Add(serialId, tcs);
        return tcs.Task;
    }

    /// <summary>
    /// 显示实体（可等待）
    /// </summary>
    public static UniTask<EntityLogic> ShowEntityAsync<T>(this EntityComponent eCom, string pfbName, Const.EntityGroup eGroup, EntityParams parms = null) where T : EntityLogic
    {
#if UNITY_EDITOR
        TipsSubscribeEvent();
#endif
        var tcs = new UniTaskCompletionSource<EntityLogic>();
        int eId = eCom.ShowEntity<T>(pfbName, eGroup, parms);
        mEntityTask.Add(eId, tcs);
        return tcs.Task;
    }

    /// <summary>
    /// 加载数据表（可等待）
    /// </summary>
    public static async UniTask<IDataTable<T>> LoadDataTableAsync<T>(this DataTableComponent dataTableComponent, string dataTableName, object userData = null) where T : IDataRow
    {
#if UNITY_EDITOR
        TipsSubscribeEvent();
#endif
        IDataTable<T> dataTable = dataTableComponent.GetDataTable<T>();
        if (dataTable != null)
        {
            return await UniTask.FromResult(dataTable);
        }

        var loadTcs = new UniTaskCompletionSource<bool>();
        dataTableComponent.LoadDataTable(dataTableName, userData);
        var dataTableAssetName = UtilityBuiltin.AssetsPath.GetDataTablePath(dataTableName);
        mDataTableTask.Add(dataTableAssetName, loadTcs);
        bool isLoaded = await loadTcs.Task;
        dataTable = isLoaded ? dataTableComponent.GetDataTable<T>() : null;
        return await UniTask.FromResult(dataTable);
    }


    private static void OnLoadDataTableSuccess(object sender, GameEventArgs e)
    {
        var ne = (LoadDataTableSuccessEventArgs)e;
        mDataTableTask.TryGetValue(ne.DataTableAssetName, out UniTaskCompletionSource<bool> tcs);
        if (tcs != null)
        {
            Log.Info("Load data table '{0}' OK.", ne.DataTableAssetName);
            tcs.TrySetResult(true);
            mDataTableTask.Remove(ne.DataTableAssetName);
        }
    }

    private static void OnLoadDataTableFailure(object sender, GameEventArgs e)
    {
        var ne = (LoadDataTableFailureEventArgs)e;
        mDataTableTask.TryGetValue(ne.DataTableAssetName, out UniTaskCompletionSource<bool> tcs);
        if (tcs != null)
        {
            Log.Error("Can not load data table '{0}' from '{1}' with error message '{2}'.", ne.DataTableAssetName,
                ne.DataTableAssetName, ne.ErrorMessage);
            tcs.TrySetResult(false);
            mDataTableTask.Remove(ne.DataTableAssetName);
        }
    }
    /// <summary>
    /// 打开界面（可等待）
    /// </summary>
    public static UniTask<UIFormLogic> OpenUIFormAsync(this UIComponent uiComponent, string uiFormAssetName, string uiGroupName, int priority, bool pauseCoveredUIForm, object userData)
    {
#if UNITY_EDITOR
        TipsSubscribeEvent();
#endif
        int serialId = uiComponent.OpenUIForm(uiFormAssetName, uiGroupName, priority, pauseCoveredUIForm, userData);
        var tcs = new UniTaskCompletionSource<UIFormLogic>();
        mUIFormTask.Add(serialId, tcs);
        return tcs.Task;
    }

    private static void OnOpenUIFormSuccess(object sender, GameEventArgs e)
    {
        OpenUIFormSuccessEventArgs ne = (OpenUIFormSuccessEventArgs)e;
        mUIFormTask.TryGetValue(ne.UIForm.SerialId, out UniTaskCompletionSource<UIFormLogic> tcs);
        if (tcs != null)
        {
            tcs.TrySetResult(ne.UIForm.Logic);
            mUIFormTask.Remove(ne.UIForm.SerialId);
        }
    }

    private static void OnOpenUIFormFailure(object sender, GameEventArgs e)
    {
        OpenUIFormFailureEventArgs ne = (OpenUIFormFailureEventArgs)e;
        mUIFormTask.TryGetValue(ne.SerialId, out UniTaskCompletionSource<UIFormLogic> tcs);
        if (tcs != null)
        {
            Debug.LogError(ne.ErrorMessage);
            tcs.TrySetException(new GameFrameworkException(ne.ErrorMessage));
            mUIFormTask.Remove(ne.SerialId);
        }
    }

    /// <summary>
    /// 显示实体（可等待）
    /// </summary>
    public static UniTask<EntityLogic> ShowEntityAsync(this EntityComponent entityComponent, int entityId,
        Type entityLogicType, string entityAssetName, string entityGroupName, int priority, object userData)
    {
#if UNITY_EDITOR
        TipsSubscribeEvent();
#endif
        var tcs = new UniTaskCompletionSource<EntityLogic>();
        mEntityTask.Add(entityId, tcs);
        entityComponent.ShowEntity(entityId, entityLogicType, entityAssetName, entityGroupName, priority, userData);
        return tcs.Task;
    }


    private static void OnShowEntitySuccess(object sender, GameEventArgs e)
    {
        ShowEntitySuccessEventArgs ne = (ShowEntitySuccessEventArgs)e;
        mEntityTask.TryGetValue(ne.Entity.Id, out var tcs);
        if (tcs != null)
        {
            tcs.TrySetResult(ne.Entity.Logic);
            mEntityTask.Remove(ne.Entity.Id);
        }
    }

    private static void OnShowEntityFailure(object sender, GameEventArgs e)
    {
        ShowEntityFailureEventArgs ne = (ShowEntityFailureEventArgs)e;
        mEntityTask.TryGetValue(ne.EntityId, out var tcs);
        if (tcs != null)
        {
            Debug.LogError(ne.ErrorMessage);
            tcs.TrySetException(new GameFrameworkException(ne.ErrorMessage));
            mEntityTask.Remove(ne.EntityId);
        }
    }


    /// <summary>
    /// 加载场景（可等待）
    /// </summary>
    public static async UniTask<bool> LoadSceneAsync(this SceneComponent sceneComponent, string sceneAssetName)
    {
#if UNITY_EDITOR
        TipsSubscribeEvent();
#endif
        var tcs = new UniTaskCompletionSource<bool>();
        var isUnLoadScene = mUnLoadSceneTask.TryGetValue(sceneAssetName, out var unloadSceneTcs);
        if (isUnLoadScene)
        {
            await unloadSceneTcs.Task;
        }
        mLoadSceneTask.Add(sceneAssetName, tcs);

        try
        {
            sceneComponent.LoadScene(sceneAssetName);
        }
        catch (Exception e)
        {
            Debug.LogError(e.ToString());
            tcs.TrySetException(e);
            mLoadSceneTask.Remove(sceneAssetName);
        }
        return await tcs.Task;
    }

    private static void OnLoadSceneSuccess(object sender, GameEventArgs e)
    {
        LoadSceneSuccessEventArgs ne = (LoadSceneSuccessEventArgs)e;
        mLoadSceneTask.TryGetValue(ne.SceneAssetName, out var tcs);
        if (tcs != null)
        {
            tcs.TrySetResult(true);
            mLoadSceneTask.Remove(ne.SceneAssetName);
        }
    }

    private static void OnLoadSceneFailure(object sender, GameEventArgs e)
    {
        LoadSceneFailureEventArgs ne = (LoadSceneFailureEventArgs)e;
        mLoadSceneTask.TryGetValue(ne.SceneAssetName, out var tcs);
        if (tcs != null)
        {
            Debug.LogError(ne.ErrorMessage);
            tcs.TrySetException(new GameFrameworkException(ne.ErrorMessage));
            mLoadSceneTask.Remove(ne.SceneAssetName);
        }
    }

    /// <summary>
    /// 卸载场景（可等待）
    /// </summary>
    public static async UniTask<bool> UnLoadSceneAsync(this SceneComponent sceneComponent, string sceneAssetName)
    {
#if UNITY_EDITOR
        TipsSubscribeEvent();
#endif
        var tcs = new UniTaskCompletionSource<bool>();
        var isLoadSceneTcs = mLoadSceneTask.TryGetValue(sceneAssetName, out var loadSceneTcs);
        if (isLoadSceneTcs)
        {
            Debug.Log("Unload  loading scene");
            await loadSceneTcs.Task;
        }
        mUnLoadSceneTask.Add(sceneAssetName, tcs);
        try
        {
            sceneComponent.UnloadScene(sceneAssetName);
        }
        catch (Exception e)
        {
            Debug.LogError(e.ToString());
            tcs.TrySetException(e);
            mUnLoadSceneTask.Remove(sceneAssetName);
        }
        return await tcs.Task;
    }
    private static void OnUnloadSceneSuccess(object sender, GameEventArgs e)
    {
        UnloadSceneSuccessEventArgs ne = (UnloadSceneSuccessEventArgs)e;
        mUnLoadSceneTask.TryGetValue(ne.SceneAssetName, out var tcs);
        if (tcs != null)
        {
            tcs.TrySetResult(true);
            mUnLoadSceneTask.Remove(ne.SceneAssetName);
        }
    }

    private static void OnUnloadSceneFailure(object sender, GameEventArgs e)
    {
        UnloadSceneFailureEventArgs ne = (UnloadSceneFailureEventArgs)e;
        mUnLoadSceneTask.TryGetValue(ne.SceneAssetName, out var tcs);
        if (tcs != null)
        {
            Debug.LogError($"Unload scene {ne.SceneAssetName} failure.");
            tcs.TrySetException(new GameFrameworkException($"Unload scene {ne.SceneAssetName} failure."));
            mUnLoadSceneTask.Remove(ne.SceneAssetName);
        }
    }

    /// <summary>
    /// 加载资源（可等待）
    /// </summary>
    public static UniTask<T> LoadAssetAsync<T>(this ResourceComponent resourceComponent, string assetName)
        where T : UnityEngine.Object
    {
#if UNITY_EDITOR
        TipsSubscribeEvent();
#endif
        UniTaskCompletionSource<T> loadAssetTcs = new UniTaskCompletionSource<T>();
        resourceComponent.LoadAsset(assetName, typeof(T), new LoadAssetCallbacks(
            (tempAssetName, asset, duration, userdata) =>
            {
                var source = loadAssetTcs;
                loadAssetTcs = null;
                T tAsset = asset as T;
                if (tAsset != null)
                {
                    source.TrySetResult(tAsset);
                }
                else
                {
                    Debug.LogError($"Load asset failure load type is {asset.GetType()} but asset type is {typeof(T)}.");
                    source.TrySetException(new GameFrameworkException(
                        $"Load asset failure load type is {asset.GetType()} but asset type is {typeof(T)}."));
                }
            },
            (tempAssetName, status, errorMessage, userdata) =>
            {
                Debug.LogError(errorMessage);
                loadAssetTcs.TrySetException(new GameFrameworkException(errorMessage));
            }
        ));

        return loadAssetTcs.Task;
    }

    /// <summary>
    /// 加载多个资源（可等待）
    /// </summary>
    public static async UniTask<T[]> LoadAssetsAsync<T>(this ResourceComponent resourceComponent, string[] assetName) where T : UnityEngine.Object
    {
#if UNITY_EDITOR
        TipsSubscribeEvent();
#endif
        if (assetName == null)
        {
            return null;
        }
        T[] assets = new T[assetName.Length];
        var tasks = new UniTask<T>[assets.Length];
        for (int i = 0; i < tasks.Length; i++)
        {
            tasks[i] = resourceComponent.LoadAssetAsync<T>(assetName[i]);
        }

        await UniTask.WhenAll(tasks);
        for (int i = 0; i < assets.Length; i++)
        {
            assets[i] = tasks[i].GetAwaiter().GetResult();
        }

        return assets;
    }


    /// <summary>
    /// 增加Web请求任务（可等待）
    /// </summary>
    public static UniTask<WebRequestResult> AddWebRequestAsync(this WebRequestComponent webRequestComponent,
        string webRequestUri, WWWForm wwwForm = null, object userdata = null)
    {
#if UNITY_EDITOR
        TipsSubscribeEvent();
#endif
        var tsc = new UniTaskCompletionSource<WebRequestResult>();
        int serialId = webRequestComponent.AddWebRequest(webRequestUri, wwwForm,
            AwaitParams<WebRequestResult>.Create(userdata, tsc));
        mWebSerialIds.Add(serialId);
        return tsc.Task;
    }

    /// <summary>
    /// 增加Web请求任务（可等待）
    /// </summary>
    public static UniTask<WebRequestResult> AddWebRequestAsync(this WebRequestComponent webRequestComponent,
        string webRequestUri, byte[] postData, object userdata = null)
    {
#if UNITY_EDITOR
        TipsSubscribeEvent();
#endif
        var tsc = new UniTaskCompletionSource<WebRequestResult>();
        int serialId = webRequestComponent.AddWebRequest(webRequestUri, postData,
            AwaitParams<WebRequestResult>.Create(userdata, tsc));
        mWebSerialIds.Add(serialId);
        return tsc.Task;
    }

    private static void OnWebRequestSuccess(object sender, GameEventArgs e)
    {
        WebRequestSuccessEventArgs ne = (WebRequestSuccessEventArgs)e;
        if (mWebSerialIds.Contains(ne.SerialId))
        {
            if (ne.UserData is AwaitParams<WebRequestResult> webRequestUserdata)
            {
                WebRequestResult result = WebRequestResult.Create(ne.GetWebResponseBytes(), false, string.Empty,
                    webRequestUserdata.UserData);
                mDelayReleaseWebResult.Add(result);
                webRequestUserdata.Source.TrySetResult(result);
                ReferencePool.Release(webRequestUserdata);
            }

            mWebSerialIds.Remove(ne.SerialId);
            if (mWebSerialIds.Count == 0)
            {
                for (int i = 0; i < mDelayReleaseWebResult.Count; i++)
                {
                    ReferencePool.Release(mDelayReleaseWebResult[i]);
                }

                mDelayReleaseWebResult.Clear();
            }
        }
    }

    private static void OnWebRequestFailure(object sender, GameEventArgs e)
    {
        WebRequestFailureEventArgs ne = (WebRequestFailureEventArgs)e;
        if (mWebSerialIds.Contains(ne.SerialId))
        {
            if (ne.UserData is AwaitParams<WebRequestResult> webRequestUserdata)
            {
                WebRequestResult result = WebRequestResult.Create(null, true, ne.ErrorMessage, webRequestUserdata.UserData);
                webRequestUserdata.Source.TrySetResult(result);
                mDelayReleaseWebResult.Add(result);
                ReferencePool.Release(webRequestUserdata);
            }

            mWebSerialIds.Remove(ne.SerialId);
            if (mWebSerialIds.Count == 0)
            {
                for (int i = 0; i < mDelayReleaseWebResult.Count; i++)
                {
                    ReferencePool.Release(mDelayReleaseWebResult[i]);
                }

                mDelayReleaseWebResult.Clear();
            }
        }
    }

    /// <summary>
    /// 增加下载任务（可等待)
    /// </summary>
    public static UniTask<DownloadResult> AddDownloadAsync(this DownloadComponent downloadComponent,
        string downloadPath,
        string downloadUri,
        object userdata = null)
    {
#if UNITY_EDITOR
        TipsSubscribeEvent();
#endif
        var tcs = new UniTaskCompletionSource<DownloadResult>();
        int serialId = downloadComponent.AddDownload(downloadPath, downloadUri,
            AwaitParams<DownloadResult>.Create(userdata, tcs));
        mDownloadSerialIds.Add(serialId);
        return tcs.Task;
    }

    private static void OnDownloadSuccess(object sender, GameEventArgs e)
    {
        DownloadSuccessEventArgs ne = (DownloadSuccessEventArgs)e;
        if (mDownloadSerialIds.Contains(ne.SerialId))
        {
            if (ne.UserData is AwaitParams<DownloadResult> awaitDataWrap)
            {
                DownloadResult result = DownloadResult.Create(false, string.Empty, awaitDataWrap.UserData);
                mDelayReleaseDownloadResult.Add(result);
                awaitDataWrap.Source.TrySetResult(result);
                ReferencePool.Release(awaitDataWrap);
            }

            mDownloadSerialIds.Remove(ne.SerialId);
            if (mDownloadSerialIds.Count == 0)
            {
                for (int i = 0; i < mDelayReleaseDownloadResult.Count; i++)
                {
                    ReferencePool.Release(mDelayReleaseDownloadResult[i]);
                }

                mDelayReleaseDownloadResult.Clear();
            }
        }
    }

    private static void OnDownloadFailure(object sender, GameEventArgs e)
    {
        DownloadFailureEventArgs ne = (DownloadFailureEventArgs)e;
        if (mDownloadSerialIds.Contains(ne.SerialId))
        {
            if (ne.UserData is AwaitParams<DownloadResult> awaitDataWrap)
            {
                DownloadResult result = DownloadResult.Create(true, ne.ErrorMessage, awaitDataWrap.UserData);
                mDelayReleaseDownloadResult.Add(result);
                awaitDataWrap.Source.TrySetResult(result);
                ReferencePool.Release(awaitDataWrap);
            }

            mDownloadSerialIds.Remove(ne.SerialId);
            if (mDownloadSerialIds.Count == 0)
            {
                for (int i = 0; i < mDelayReleaseDownloadResult.Count; i++)
                {
                    ReferencePool.Release(mDelayReleaseDownloadResult[i]);
                }

                mDelayReleaseDownloadResult.Clear();
            }
        }
    }
}