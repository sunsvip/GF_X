using System.Collections.Generic;
using GameFramework;
using GameFramework.Procedure;
using GameFramework.Fsm;
using GameFramework.Event;
using UnityGameFramework.Runtime;
using System;
using GameFramework.Resource;
using ResourceUpdateStartEventArgs = UnityGameFramework.Runtime.ResourceUpdateStartEventArgs;
using ResourceUpdateChangedEventArgs = UnityGameFramework.Runtime.ResourceUpdateChangedEventArgs;
using ResourceUpdateSuccessEventArgs = UnityGameFramework.Runtime.ResourceUpdateSuccessEventArgs;
using ResourceUpdateFailureEventArgs = UnityGameFramework.Runtime.ResourceUpdateFailureEventArgs;
using UnityEngine;
using ResourceVerifyStartEventArgs = UnityGameFramework.Runtime.ResourceVerifyStartEventArgs;
using ResourceVerifySuccessEventArgs = UnityGameFramework.Runtime.ResourceVerifySuccessEventArgs;
using ResourceVerifyFailureEventArgs = UnityGameFramework.Runtime.ResourceVerifyFailureEventArgs;

[Serializable]
[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.All)]
public class VersionInfo
{
    public int InternalResourceVersion;//资源版本号
    public int VersionListLength;
    public int VersionListHashCode;
    public int VersionListCompressedLength;
    public int VersionListCompressedHashCode;
    public string ApplicableGameVersion;//资源适用的App版本
    public string UpdatePrefixUri;//热更资源地址

    public string LastAppVersion; //最新的App版本号
    public bool ForceUpdateApp;//是否强制更新App
    public string AppUpdateUrl;//强制更新App地址
    public string AppUpdateDesc;//强制更新说明文字,显示在对话框中
}
/// <summary>
/// 初始化资源流程
/// 1.如果是单机模式直接初始化资源
/// 2.如果是热更新模式先检测更新再初始化资源
/// </summary>
[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
public class UpdateResourcesProcedure : ProcedureBase
{
    private bool initComplete = false;
    private long mDownloadTotalZipLength = 0L;
    private List<DownloadProgressData> mDownloadProgressData;
    protected override void OnEnter(IFsm<IProcedureManager> procedureOwner)
    {
        base.OnEnter(procedureOwner);
        initComplete = false;
        mDownloadProgressData = new List<DownloadProgressData>();


        GFBuiltin.Event.Subscribe(WebRequestSuccessEventArgs.EventId, OnWebRequestSuccess);
        GFBuiltin.Event.Subscribe(UnityGameFramework.Runtime.ResourceUpdateStartEventArgs.EventId, OnResourceUpdateStart);
        GFBuiltin.Event.Subscribe(UnityGameFramework.Runtime.ResourceUpdateChangedEventArgs.EventId, OnResourceUpdateChanged);
        GFBuiltin.Event.Subscribe(UnityGameFramework.Runtime.ResourceUpdateSuccessEventArgs.EventId, OnResourceUpdateSuccess);
        GFBuiltin.Event.Subscribe(UnityGameFramework.Runtime.ResourceUpdateAllCompleteEventArgs.EventId, OnResourceUpdateAllComplete);
        GFBuiltin.Event.Subscribe(UnityGameFramework.Runtime.ResourceUpdateFailureEventArgs.EventId, OnResourceUpdateFailure);
        GFBuiltin.Event.Subscribe(UnityGameFramework.Runtime.ResourceVerifyStartEventArgs.EventId, OnResourceVerifyStart);
        GFBuiltin.Event.Subscribe(UnityGameFramework.Runtime.ResourceVerifySuccessEventArgs.EventId, OnResourceVerifySuccess);
        GFBuiltin.Event.Subscribe(UnityGameFramework.Runtime.ResourceVerifyFailureEventArgs.EventId, OnResourceVerifyFailure);
        CheckVersion();

    }


    protected override void OnLeave(IFsm<IProcedureManager> procedureOwner, bool isShutdown)
    {
        GFBuiltin.Event.Unsubscribe(WebRequestSuccessEventArgs.EventId, OnWebRequestSuccess);
        GFBuiltin.Event.Unsubscribe(UnityGameFramework.Runtime.ResourceUpdateStartEventArgs.EventId, OnResourceUpdateStart);
        GFBuiltin.Event.Unsubscribe(UnityGameFramework.Runtime.ResourceUpdateChangedEventArgs.EventId, OnResourceUpdateChanged);
        GFBuiltin.Event.Unsubscribe(UnityGameFramework.Runtime.ResourceUpdateSuccessEventArgs.EventId, OnResourceUpdateSuccess);
        GFBuiltin.Event.Unsubscribe(UnityGameFramework.Runtime.ResourceUpdateAllCompleteEventArgs.EventId, OnResourceUpdateAllComplete);
        GFBuiltin.Event.Unsubscribe(UnityGameFramework.Runtime.ResourceUpdateFailureEventArgs.EventId, OnResourceUpdateFailure);
        GFBuiltin.Event.Unsubscribe(UnityGameFramework.Runtime.ResourceVerifyStartEventArgs.EventId, OnResourceVerifyStart);
        GFBuiltin.Event.Unsubscribe(UnityGameFramework.Runtime.ResourceVerifySuccessEventArgs.EventId, OnResourceVerifySuccess);
        GFBuiltin.Event.Unsubscribe(UnityGameFramework.Runtime.ResourceVerifyFailureEventArgs.EventId, OnResourceVerifyFailure);
        base.OnLeave(procedureOwner, isShutdown);
    }
    protected override void OnUpdate(IFsm<IProcedureManager> procedureOwner, float elapseSeconds, float realElapseSeconds)
    {
        base.OnUpdate(procedureOwner, elapseSeconds, realElapseSeconds);
        if (initComplete)
        {
            ChangeState<LoadHotfixDllProcedure>(procedureOwner);
        }
    }

    private string GetPlatformPath()
    {
#if UNITY_ANDROID
        return "Android";
#elif UNITY_IOS
            return "IOS";
#elif UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
#if UNITY_64
            return "Windows64";
#else
        return "Windows";
#endif
#elif UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
            return "MacOS";
#elif UNITY_WEBGL
            return "WebGL";
#else
            throw new System.NotSupportedException(Utility.Text.Format("Platform '{0}' is not supported.", Application.platform));
#endif
    }
    //向服务器发送请求获取版本信息 进行版本更新检测
    void CheckVersion()
    {
        if (GFBuiltin.Resource.ResourceMode == GameFramework.Resource.ResourceMode.Updatable || GFBuiltin.Resource.ResourceMode == GameFramework.Resource.ResourceMode.UpdatableWhilePlaying)
        {
            Log.Info("当前为热更新模式, Web请求最新版本号...");
            string verFileUrl = UtilityBuiltin.AssetsPath.GetCombinePath(AppSettings.Instance.CheckVersionUrl, GetPlatformPath(), ConstBuiltin.VersionFile);
            Log.Info("请求版本信息地址:{0}", verFileUrl);
            GFBuiltin.WebRequest.AddWebRequest(verFileUrl, this);
            GFBuiltin.BuiltinView.ShowLoadingProgress(0);
        }
        else
        {
            GFBuiltin.Resource.InitResources(OnResInitComplete);
        }
    }
    private void OnWebRequestSuccess(object sender, GameEventArgs e)
    {
        var arg = e as WebRequestSuccessEventArgs;
        if (arg.UserData != this)
        {
            return;
        }
        var webText = Utility.Converter.GetString(arg.GetWebResponseBytes());
        GFBuiltin.Log($"最新资源版本信息:{webText}");
        var vinfo = Utility.Json.ToObject<VersionInfo>(webText);
        CheckVersionList(vinfo);
    }
    private void CheckVersionList(VersionInfo vinfo)
    {
        if (vinfo == null)
        {
            GFBuiltin.LogError("热更失败: 解析version.json信息失败!");
            return;
        }
        //Log.Info("{0},{1},{2},{3}", vinfo.VersionListLength, vinfo.VersionListHashCode, vinfo.VersionListCompressedLength, vinfo.VersionListCompressedHashCode);
        var curAppVersion = System.Version.Parse(GameFramework.Version.GameVersion);
        var lastAppVersion = System.Version.Parse(vinfo.LastAppVersion);
        if (lastAppVersion > curAppVersion)
        {
            GFBuiltin.BuiltinView.ShowDialog(GFBuiltin.Localization.GetString("New Version!"),
                vinfo.AppUpdateDesc, GFBuiltin.Localization.GetString("UPDATE"), GFBuiltin.Localization.GetString("LATER"),
                () =>
                {
                    Application.OpenURL(vinfo.AppUpdateUrl);
                    GFBuiltin.Shutdown(ShutdownType.Quit);
                },
                () =>
                {
                    if (vinfo.ForceUpdateApp)//强制更新时点不更新则退出游戏
                        GFBuiltin.Shutdown(ShutdownType.Quit);
                    else
                        CheckVersionAndUpdate(vinfo);
                });
            return;
        }

        CheckVersionAndUpdate(vinfo);
    }
    private void CheckVersionAndUpdate(VersionInfo vinfo)
    {
        GFBuiltin.Resource.UpdatePrefixUri = UtilityBuiltin.AssetsPath.GetCombinePath(vinfo.UpdatePrefixUri);
        GFBuiltin.Log($"资源服务器地址:{GFBuiltin.Resource.UpdatePrefixUri}");
        CheckVersionListResult checkResult;
        if (CheckResourceApplicable(vinfo.ApplicableGameVersion))
        {
            checkResult = GFBuiltin.Resource.CheckVersionList(vinfo.InternalResourceVersion);
            GFBuiltin.Log($"是否存需要更新资源:{checkResult}");
        }
        else
        {
            GFBuiltin.Log("资源不适用当前客户端版本, 已跳过更新");
            checkResult = GFBuiltin.Resource.CheckVersionList(GFBuiltin.Resource.InternalResourceVersion);
        }
        if (checkResult == GameFramework.Resource.CheckVersionListResult.NeedUpdate)
        {
            GFBuiltin.Log("更新资源列表文件...");
            var updateVersionCall = new UpdateVersionListCallbacks(OnUpdateVersionListSuccess, OnUpdateVersionListFailed);
            GFBuiltin.Resource.UpdateVersionList(vinfo.VersionListLength, vinfo.VersionListHashCode, vinfo.VersionListCompressedLength, vinfo.VersionListCompressedHashCode, updateVersionCall);
        }
        else
        {
            GFBuiltin.Resource.VerifyResources(OnVerifyResourcesComplete);
            //GFBuiltin.Resource.CheckResources(OnCheckResurcesComplete);
        }
    }
    /// <summary>
    /// 检测最新资源是否适用于当前客户端版本
    /// </summary>
    /// <param name="applicableGameVersion"></param>
    /// <returns></returns>
    private bool CheckResourceApplicable(string applicableGameVersion)
    {
        string[] versionArr = applicableGameVersion.Split('|');
        foreach (var version in versionArr)
        {
            var fixVer = version.Trim();
            if (GameFramework.Version.GameVersion.CompareTo(fixVer) == 0)
            {
                return true;
            }
        }
        return false;
    }

    private void OnVerifyResourcesComplete(bool result)
    {
        Log.Info<bool>("资源验证完成, 验证结果:{0}", result);
        GFBuiltin.Resource.CheckResources(OnCheckResurcesComplete);
    }

    private void OnCheckResurcesComplete(int movedCount, int removedCount, int updateCount, long updateTotalLength, long updateTotalZipLength)
    {
        mDownloadTotalZipLength = updateTotalZipLength;
        if (updateCount <= 0)
        {
            Log.Info("资源已是最新,无需更新.");
            OnResInitComplete();
        }
        else
        {
            Log.Info<int, long, string>("需要更新资源个数:{0},资源大小:{1},下载地址:{2}", updateCount, updateTotalZipLength, GFBuiltin.Resource.UpdatePrefixUri);
            GFBuiltin.Resource.UpdateResources(OnUpdateResourceComplete);
        }
    }
    private void RefreshDownloadProgress()
    {
        long currentTotalUpdateLength = 0L;
        for (int i = 0; i < mDownloadProgressData.Count; i++)
        {
            currentTotalUpdateLength += mDownloadProgressData[i].Length;
        }

        float progressTotal = (float)currentTotalUpdateLength / mDownloadTotalZipLength;
        GFBuiltin.BuiltinView.SetLoadingProgress(progressTotal);
    }
    private void OnResourceUpdateStart(object sender, GameEventArgs e)
    {
        ResourceUpdateStartEventArgs ne = (ResourceUpdateStartEventArgs)e;

        for (int i = 0; i < mDownloadProgressData.Count; i++)
        {
            if (mDownloadProgressData[i].Name == ne.Name)
            {
                //Log.Warning("Update resource '{0}' is invalid.", ne.Name);
                mDownloadProgressData[i].Length = 0;
                RefreshDownloadProgress();
                return;
            }
        }

        mDownloadProgressData.Add(new DownloadProgressData(ne.Name));
    }
    private void OnResourceUpdateFailure(object sender, GameEventArgs e)
    {
        ResourceUpdateFailureEventArgs ne = (ResourceUpdateFailureEventArgs)e;
        if (ne.RetryCount >= ne.TotalRetryCount)
        {
            Log.Error<string, string, string, int>("Download '{0}' failure from '{1}' with error message '{2}', retry count '{3}'.", ne.Name, ne.DownloadUri, ne.ErrorMessage, ne.RetryCount);
            return;
        }
        else
        {
            Log.Warning("Download '{0}' failure from '{1}' with error message '{2}', retry count '{3}'.", ne.Name, ne.DownloadUri, ne.ErrorMessage, ne.RetryCount);
        }

        for (int i = 0; i < mDownloadProgressData.Count; i++)
        {
            if (mDownloadProgressData[i].Name == ne.Name)
            {
                mDownloadProgressData.Remove(mDownloadProgressData[i]);
                RefreshDownloadProgress();
                return;
            }
        }
    }
    private void OnResourceUpdateSuccess(object sender, GameEventArgs e)
    {
        ResourceUpdateSuccessEventArgs ne = (ResourceUpdateSuccessEventArgs)e;
        Log.Info("Download '{0}' success.", ne.Name);

        for (int i = 0; i < mDownloadProgressData.Count; i++)
        {
            if (mDownloadProgressData[i].Name == ne.Name)
            {
                mDownloadProgressData[i].Length = ne.CompressedLength;
                RefreshDownloadProgress();
                return;
            }
        }
    }
    private void OnResourceUpdateChanged(object sender, GameEventArgs e)
    {
        ResourceUpdateChangedEventArgs ne = (ResourceUpdateChangedEventArgs)e;

        for (int i = 0; i < mDownloadProgressData.Count; i++)
        {
            if (mDownloadProgressData[i].Name == ne.Name)
            {
                mDownloadProgressData[i].Length = ne.CurrentLength;
                RefreshDownloadProgress();
                return;
            }
        }
    }
    private void OnUpdateResourceComplete(IResourceGroup resourceGroup, bool result)
    {
        if (result)
        {
            Log.Info("Update resources complete!");
            OnResInitComplete();
        }
        else
        {
            Log.Error("Update resources complete with errors.");
        }
    }

    private void OnResourceUpdateAllComplete(object sender, GameEventArgs e)
    {

    }

    private void OnUpdateVersionListSuccess(string downloadPath, string downloadUri)
    {
        GFBuiltin.Resource.CheckResources(OnCheckResurcesComplete);
    }

    private void OnUpdateVersionListFailed(string downloadUri, string errorMessage)
    {
        Log.Fatal("UpdateVersionListFailed, downloadUri:{0}, errorMessage:{1}", downloadUri, errorMessage);
    }
    private void OnResourceVerifyStart(object sender, GameEventArgs e)
    {
        ResourceVerifyStartEventArgs ne = (ResourceVerifyStartEventArgs)e;
        Log.Info("Start verify resources, verify resource count '{0}', verify resource total length '{1}'.", ne.Count, ne.TotalLength);
    }

    private void OnResourceVerifySuccess(object sender, GameEventArgs e)
    {
        ResourceVerifySuccessEventArgs ne = (ResourceVerifySuccessEventArgs)e;
        Log.Info("Verify resource '{0}' success.", ne.Name);
    }

    private void OnResourceVerifyFailure(object sender, GameEventArgs e)
    {
        ResourceVerifyFailureEventArgs ne = (ResourceVerifyFailureEventArgs)e;
        Log.Warning("Verify resource '{0}' failure.", ne.Name);
    }
    void OnResInitComplete()
    {
        initComplete = true;

        GFBuiltin.Log("All Resource Completed!");
    }
    private class DownloadProgressData
    {
        private readonly string m_Name;

        public DownloadProgressData(string name)
        {
            m_Name = name;
        }

        public string Name
        {
            get
            {
                return m_Name;
            }
        }

        public int Length
        {
            get;
            set;
        }
    }
}
