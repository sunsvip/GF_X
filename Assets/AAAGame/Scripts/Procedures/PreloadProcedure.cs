
using UnityEngine;
using GameFramework.Event;
using GameFramework.Procedure;
using UnityGameFramework.Runtime;
using GameFramework.Fsm;
using System.Collections.Generic;
using GameFramework;
using System;
using GameFramework.Resource;

public class PreloadProcedure : ProcedureBase
{
    private int totalProgress;
    private int loadedProgress;
    private float smoothProgress;
    private bool preloadAllCompleted;
    private float progressSmoothSpeed = 10f;
    private int m_DataTablesCount;
    protected override void OnEnter(IFsm<IProcedureManager> procedureOwner)
    {
        base.OnEnter(procedureOwner);
        GF.Event.Subscribe(LoadConfigSuccessEventArgs.EventId, OnLoadConfigSuccess);
        GF.Event.Subscribe(LoadConfigFailureEventArgs.EventId, OnLoadConfigFailure);
        GF.Event.Subscribe(LoadDataTableSuccessEventArgs.EventId, OnLoadDataTableSuccess);
        GF.Event.Subscribe(LoadDataTableFailureEventArgs.EventId, OnLoadDataTableFailure);
        GF.Event.Subscribe(LoadDictionarySuccessEventArgs.EventId, OnLoadDicSuccess);
        GF.Event.Subscribe(LoadDictionaryFailureEventArgs.EventId, OnLoadDicFailure);
        GF.BuiltinView.ShowLoadingProgress();
        GF.LogInfo("进入HybridCLR热更流程! 预加载游戏数据...");

        InitAppSettings();
        PreloadAndInitData();
    }


    protected override void OnLeave(IFsm<IProcedureManager> procedureOwner, bool isShutdown)
    {
        GF.Event.Unsubscribe(LoadConfigSuccessEventArgs.EventId, OnLoadConfigSuccess);
        GF.Event.Unsubscribe(LoadConfigFailureEventArgs.EventId, OnLoadConfigFailure);
        GF.Event.Unsubscribe(LoadDataTableSuccessEventArgs.EventId, OnLoadDataTableSuccess);
        GF.Event.Unsubscribe(LoadDataTableFailureEventArgs.EventId, OnLoadDataTableFailure);
        GF.Event.Unsubscribe(LoadDictionarySuccessEventArgs.EventId, OnLoadDicSuccess);
        GF.Event.Unsubscribe(LoadDictionaryFailureEventArgs.EventId, OnLoadDicFailure);
        base.OnLeave(procedureOwner, isShutdown);
    }


    protected override void OnUpdate(IFsm<IProcedureManager> procedureOwner, float elapseSeconds, float realElapseSeconds)
    {
        base.OnUpdate(procedureOwner, elapseSeconds, realElapseSeconds);
        if (totalProgress <= 0 || preloadAllCompleted) return;

        smoothProgress = Mathf.Lerp(smoothProgress, loadedProgress / (float)totalProgress, elapseSeconds * progressSmoothSpeed);

        GF.BuiltinView.SetLoadingProgress(smoothProgress);
        //预加载完成 切换场景
        if (loadedProgress >= totalProgress && smoothProgress >= 0.99f)
        {
            preloadAllCompleted = true;
            InitGameFrameworkSettings();
            GF.LogInfo("预加载完成, 进入游戏场景.");
            procedureOwner.SetData<VarString>(ChangeSceneProcedure.P_SceneName, "Game");
            ChangeState<ChangeSceneProcedure>(procedureOwner);
        }
    }
    private void InitAppSettings()
    {
        //if (string.IsNullOrWhiteSpace(GF.Setting.GetABTestGroup()))
        //{
        //    GF.Setting.SetABTestGroup("B");//设置A/B测试组; 应由服务器分配该新用户所属测试组
        //}
    }
    /// <summary>
    /// 预加载完成之后需要处理的事情
    /// </summary>
    private void InitGameFrameworkSettings()
    {
        //初始化EntityGroup
        var entityGroupTb = GF.DataTable.GetDataTable<EntityGroupTable>();
        foreach (var tb in entityGroupTb.GetAllDataRows())
        {
            if (GF.Entity.HasEntityGroup(tb.Name))
            {
                var group = GF.Entity.GetEntityGroup(tb.Name);
                group.InstanceAutoReleaseInterval = tb.ReleaseInterval;
                group.InstanceCapacity = tb.Capacity;
                group.InstanceExpireTime = tb.ExpireTime;
                group.InstancePriority = tb.Priority;
                continue;
            }
            GF.Entity.AddEntityGroup(tb.Name, tb.ReleaseInterval, tb.Capacity, tb.ExpireTime, tb.Priority);
        }
        Dictionary<string, SoundGroupTable> defaultSoundGroupData = new Dictionary<string, SoundGroupTable>();
        //初始化SoundGroup
        var soundGroupTb = GF.DataTable.GetDataTable<SoundGroupTable>();
        foreach (var tb in soundGroupTb.GetAllDataRows())
        {
            if (!defaultSoundGroupData.ContainsKey(tb.Name))
            {
                defaultSoundGroupData.Add(tb.Name, tb);
            }
            if (GF.Sound.HasSoundGroup(tb.Name))
            {
                var group = GF.Sound.GetSoundGroup(tb.Name);
                group.AvoidBeingReplacedBySamePriority = tb.AvoidBeingReplacedBySamePriority;
                group.Mute = tb.Mute;
                group.Volume = tb.Volume;
                continue;
            }
            GF.Sound.AddSoundGroup(tb.Name, tb.AvoidBeingReplacedBySamePriority, tb.Mute, tb.Volume, tb.SoundAgentCount);
        }
        //初始化UIGroup
        var uiGroupTb = GF.DataTable.GetDataTable<UIGroupTable>();
        foreach (var tb in uiGroupTb.GetAllDataRows())
        {
            if (GF.UI.HasUIGroup(tb.Name))
            {
                var group = GF.UI.GetUIGroup(tb.Name);
                group.Depth = tb.Depth;
                continue;
            }
            GF.UI.AddUIGroup(tb.Name, tb.Depth);
        }


        //初始化音效
        GF.Setting.SetMediaMute(Const.SoundGroup.Music, GF.Setting.GetMediaMute(Const.SoundGroup.Music, defaultSoundGroupData[Const.SoundGroup.Music.ToString()].Mute));
        GF.Setting.SetMediaMute(Const.SoundGroup.Sound, GF.Setting.GetMediaMute(Const.SoundGroup.Sound, defaultSoundGroupData[Const.SoundGroup.Sound.ToString()].Mute));
        GF.Setting.SetMediaMute(Const.SoundGroup.Vibrate, GF.Setting.GetMediaMute(Const.SoundGroup.Vibrate, defaultSoundGroupData[Const.SoundGroup.Vibrate.ToString()].Mute));
        GF.Setting.SetMediaMute(Const.SoundGroup.Joystick, GF.Setting.GetMediaMute(Const.SoundGroup.Joystick, defaultSoundGroupData[Const.SoundGroup.Joystick.ToString()].Mute));

        GF.Setting.SetMediaVolume(Const.SoundGroup.Music, GF.Setting.GetMediaVolume(Const.SoundGroup.Music, defaultSoundGroupData[Const.SoundGroup.Music.ToString()].Volume));
        GF.Setting.SetMediaVolume(Const.SoundGroup.Sound, GF.Setting.GetMediaVolume(Const.SoundGroup.Sound, defaultSoundGroupData[Const.SoundGroup.Sound.ToString()].Volume));
    }
    /// <summary>
    /// 预加载数据表、游戏配置,以及初始化游戏数据
    /// </summary>
    private async void PreloadAndInitData()
    {
        preloadAllCompleted = false;
        smoothProgress = 0;
        totalProgress = 0;
        loadedProgress = 0;
        m_DataTablesCount = -1;
        var appConfig = await AppConfigs.GetInstanceSync();
        totalProgress = appConfig.DataTables.Length + appConfig.Configs.Length + 2;//2是加载多语言和创建框架扩展
        CreateGFExtension();
    }
    private async void LoadConfigsAndDataTables()
    {
        var appConfig = await AppConfigs.GetInstanceSync();
        m_DataTablesCount = appConfig.DataTables.Length;
        foreach (var item in appConfig.Configs)
        {
            GF.Config.LoadConfig(item, appConfig.LoadFromBytes, this);
        }
        foreach (var item in appConfig.DataTables)
        {
            GF.DataTable.LoadDataTable(item, appConfig.LoadFromBytes, this);
        }
    }
    private void CreateGFExtension()
    {
        GF.Resource.LoadAsset(UtilityBuiltin.AssetsPath.GetPrefab("Core/GFExtension"), typeof(GameObject), new GameFramework.Resource.LoadAssetCallbacks(OnLoadGFExtensionSuccess, OnLoadGFExtensionFailed));
    }

    private async void InitAndLoadLanguage()
    {
        //初始化语言
        GameFramework.Localization.Language language = GF.Setting.GetLanguage();
        if (language == GameFramework.Localization.Language.Unspecified)
        {
#if UNITY_EDITOR
            language = GF.Base.EditorLanguage;
#else
            language = GFBuiltin.Localization.SystemLanguage;//默认语言跟随用户操作系统语言
#endif
        }
        var languageName = language.ToString();
        var langTb = GF.DataTable.GetDataTable<LanguagesTable>();
        var langRow = langTb.GetDataRow(row => row.LanguageKey == languageName);
        if (langRow == null)
        {
            langRow = langTb.MinIdDataRow;
            language = Enum.Parse<GameFramework.Localization.Language>(langRow.LanguageKey);//不支持的语言默认用英文
        }
        GF.Setting.SetLanguage(language, false);
        GF.LogInfo(Utility.Text.Format("初始化游戏设置. 游戏语言:{0},系统语言:{1}", language, GFBuiltin.Localization.SystemLanguage));
        var appConfig = await AppConfigs.GetInstanceSync();
        GF.Localization.LoadLanguage(langRow.AssetName, appConfig.LoadFromBytes, this);
    }

    private void OnLoadGFExtensionSuccess(string assetName, object asset, float duration, object userData)
    {
        var gfExtPfb = asset as GameObject;
        if (null != GameObject.Instantiate(gfExtPfb, Vector3.zero, Quaternion.identity, GF.Base.transform))
        {
            GF.LogInfo("GF框架扩展成功!");
            loadedProgress++;
            LoadConfigsAndDataTables();
        }
    }
    private void OnLoadGFExtensionFailed(string assetName, LoadResourceStatus status, string errorMessage, object userData)
    {
        GF.LogError(Utility.Text.Format("GF框架扩展加载失败:{0}, Error:{1}", assetName, errorMessage));
    }
    private void OnLoadDicSuccess(object sender, GameEventArgs e)
    {
        LoadDictionarySuccessEventArgs args = e as LoadDictionarySuccessEventArgs;
        if (args.UserData != this) return;
        loadedProgress++;
        Log.Info("Load Language Success:{0}", args.DictionaryAssetName);

    }
    /// <summary>
    /// 加载配置成功回调
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void OnLoadConfigSuccess(object sender, GameEventArgs e)
    {
        var args = e as LoadConfigSuccessEventArgs;
        if (args.UserData != this) return;
        loadedProgress++;
        Log.Info("Load Config Success:{0}", args.ConfigAssetName);
    }

    private void OnLoadDataTableSuccess(object sender, GameEventArgs e)
    {
        var args = e as LoadDataTableSuccessEventArgs;
        if (args.UserData != this) return;
        loadedProgress++;
        m_DataTablesCount--;
        Log.Info("Load DataTable Success:{0}", args.DataTableAssetName);
        if (m_DataTablesCount == 0)
        {
            InitAndLoadLanguage();
        }
    }

    private void OnLoadDicFailure(object sender, GameEventArgs e)
    {
        var args = e as LoadDictionaryFailureEventArgs;
        if (args.UserData != this) return;

        GF.LogError($"Load Dictionary Failed:{args.ErrorMessage}");
    }

    private void OnLoadDataTableFailure(object sender, GameEventArgs e)
    {
        var args = e as LoadDataTableFailureEventArgs;
        if (args.UserData != this) return;

        GF.LogError($"Load DataTable Failed:{args.ErrorMessage}");
    }

    private void OnLoadConfigFailure(object sender, GameEventArgs e)
    {
        var args = e as LoadConfigFailureEventArgs;
        if (args.UserData != this) return;

        GF.LogError($"Load Config Failed:{args.ErrorMessage}");
    }
}
