
using UnityEngine;
using GameFramework;
using GameFramework.Event;
using GameFramework.Procedure;
using UnityGameFramework.Runtime;
using GameFramework.Fsm;

public class PreloadProcedure : ProcedureBase
{
    private int totalProgress;
    private int loadedProgress;
    private float smoothProgress;
    private bool preloadAllCompleted;
    private float progressSmoothSpeed = 10f;
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

        smoothProgress = Mathf.Lerp(smoothProgress, loadedProgress / totalProgress, elapseSeconds * progressSmoothSpeed);

        GF.BuiltinView.SetLoadingProgress(smoothProgress);
        //预加载完成 切换场景
        if (loadedProgress >= totalProgress && smoothProgress >= 0.99f)
        {
            preloadAllCompleted = true;
            InitGameFrameworkSettings();
            GF.LogInfo("预加载完成, 进入游戏场景.");
            procedureOwner.SetData<VarString>("NextScene", "Game");
            ChangeState<ChangeSceneProcedure>(procedureOwner);
        }
    }
    private void InitAppSettings()
    {
        if (string.IsNullOrWhiteSpace(GF.Setting.GetABTestGroup()))
        {
            GF.Setting.SetABTestGroup("B");//设置A/B测试组; 应由服务器分配该新用户所属测试组
        }
        //初始化语言
        GameFramework.Localization.Language language;
#if UNITY_EDITOR
        language = GF.Base.EditorLanguage;
#else
        language = GF.Setting.GetLanguage();
#endif

        if (language == GameFramework.Localization.Language.Unspecified)
        {
            language = GFBuiltin.Localization.SystemLanguage;//默认语言跟随用户操作系统语言
        }
        var languageJson = UtilityBuiltin.ResPath.GetLanguagePath(language.ToString());
        if (GF.Resource.HasAsset(languageJson) == GameFramework.Resource.HasAssetResult.NotExist)
        {
            language = GameFramework.Localization.Language.English;//不支持的语言默认用英文
        }
        GF.Setting.SetLanguage(language, false);
        GF.LogInfo($"初始化游戏设置. 语言:{language}");
    }
    /// <summary>
    /// 预加载完成之后需要处理的事情
    /// </summary>
    private void InitGameFrameworkSettings()
    {
        GF.StaticUI.JoystickEnable = false;
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
        //初始化SoundGroup
        var soundGroupTb = GF.DataTable.GetDataTable<SoundGroupTable>();
        foreach (var tb in soundGroupTb.GetAllDataRows())
        {
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
        GF.Setting.SetMediaMute(Const.SoundGroup.Music, GF.Setting.GetMediaMute(Const.SoundGroup.Music));
        GF.Setting.SetMediaMute(Const.SoundGroup.Sound, GF.Setting.GetMediaMute(Const.SoundGroup.Sound));
        GF.Setting.SetMediaMute(Const.SoundGroup.Vibrate, GF.Setting.GetMediaMute(Const.SoundGroup.Vibrate));
        GF.Setting.SetMediaMute(Const.SoundGroup.Joystick, GF.Setting.GetMediaMute(Const.SoundGroup.Joystick));

        GF.Setting.SetMediaVolume(Const.SoundGroup.Music, GF.Setting.GetMediaVolume(Const.SoundGroup.Music));
        GF.Setting.SetMediaVolume(Const.SoundGroup.Sound, GF.Setting.GetMediaVolume(Const.SoundGroup.Sound));
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

        var appConfig = await AppConfigs.GetInstanceSync();
        totalProgress = appConfig.DataTables.Length + appConfig.Configs.Length + 2;//2是加载多语言和创建框架扩展
        CreateGFExtension();
    }
    private async void LoadOthers()
    {
        LoadLanguage();
        var appConfig = await AppConfigs.GetInstanceSync();
        foreach (var item in appConfig.Configs)
        {
            LoadConfig(item);
        }
        foreach (var item in appConfig.DataTables)
        {
            LoadDataTable(item);
        }
    }
    private void CreateGFExtension()
    {
        GF.Resource.LoadAsset(UtilityBuiltin.ResPath.GetPrefab("GFExtension"), typeof(GameObject), new GameFramework.Resource.LoadAssetCallbacks(OnLoadGFExtensionSuccess));
    }
    /// <summary>
    /// 加载配置
    /// </summary>
    /// <param name="name"></param>
    private void LoadConfig(string name)
    {
        GF.Config.LoadConfig(name, this);
    }
    /// <summary>
    /// 加载数据表
    /// </summary>
    /// <param name="name"></param>
    private void LoadDataTable(string name)
    {
        GF.DataTable.LoadDataTable(name, this);
    }

    private void LoadLanguage()
    {
        GF.Localization.LoadLanguage(GF.Localization.Language.ToString(), this);
    }

    private void OnLoadGFExtensionSuccess(string assetName, object asset, float duration, object userData)
    {
        var gfExtPfb = asset as GameObject;
        if (null != GameObject.Instantiate(gfExtPfb, Vector3.zero, Quaternion.identity, GF.Base.transform))
        {
            GF.LogInfo("GF框架扩展成功!");
            loadedProgress++;
            LoadOthers();
        }
    }
    private void OnLoadDicSuccess(object sender, GameEventArgs e)
    {
        LoadDictionarySuccessEventArgs args = e as LoadDictionarySuccessEventArgs;
        if (args.UserData != this) return;
        loadedProgress++;
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
        Log.Info("Load DataTable Success:{0}", args.DataTableAssetName);
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
