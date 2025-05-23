using GameFramework;
using GameFramework.Procedure;
using UnityGameFramework.Runtime;
using GameFramework.Fsm;
using GameFramework.Event;
[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
public class ChangeSceneProcedure : ProcedureBase
{
    /// <summary>
    /// 要加载的场景资源名,相对于场景目录
    /// </summary>
    internal const string P_SceneName = "SceneName";
    private bool loadSceneOver = false;
    private string nextScene = string.Empty;
    protected override void OnEnter(IFsm<IProcedureManager> procedureOwner)
    {
        base.OnEnter(procedureOwner);
        loadSceneOver = false;

        GF.Event.Subscribe(LoadSceneSuccessEventArgs.EventId, OnLoadSceneSuccess);
        GF.Event.Subscribe(LoadSceneFailureEventArgs.EventId, OnLoadSceneFailure);
        GF.Event.Subscribe(LoadSceneUpdateEventArgs.EventId, OnLoadSceneUpdate);
        // 停止所有声音
        GF.Sound.StopAllLoadingSounds();
        GF.Sound.StopAllLoadedSounds();

        // 隐藏所有实体
        GF.Entity.HideAllLoadingEntities();
        GF.Entity.HideAllLoadedEntities();

        // 卸载所有场景
        string[] loadedSceneAssetNames = GF.Scene.GetLoadedSceneAssetNames();
        for (int i = 0; i < loadedSceneAssetNames.Length; i++)
        {
            GF.Scene.UnloadScene(loadedSceneAssetNames[i]);
        }

        // 还原游戏速度
        GF.Base.ResetNormalGameSpeed();

        if (!procedureOwner.HasData(P_SceneName))
        {
            throw new GameFrameworkException("未设置要加载的场景资源名!");
        }
        nextScene = procedureOwner.GetData<VarString>(P_SceneName);
        procedureOwner.RemoveData(P_SceneName);
        GF.Scene.LoadScene(UtilityBuiltin.AssetsPath.GetScenePath(nextScene), this);
    }

    protected override void OnUpdate(IFsm<IProcedureManager> procedureOwner, float elapseSeconds, float realElapseSeconds)
    {
        base.OnUpdate(procedureOwner, elapseSeconds, realElapseSeconds);
        if (!loadSceneOver)
        {
            return;
        }

        //场景加载完成,根据不同场景切换对应Procedure
        switch (nextScene)
        {
            case "Game":
                ChangeState<MenuProcedure>(procedureOwner);
                //GF.Sound.PlayBGM("BillieEilishMusic.wav");
                break;
        }
    }

    protected override void OnLeave(IFsm<IProcedureManager> procedureOwner, bool isShutdown)
    {
        GF.Event.Unsubscribe(LoadSceneSuccessEventArgs.EventId, OnLoadSceneSuccess);
        GF.Event.Unsubscribe(LoadSceneFailureEventArgs.EventId, OnLoadSceneFailure);
        GF.Event.Unsubscribe(LoadSceneUpdateEventArgs.EventId, OnLoadSceneUpdate);
        base.OnLeave(procedureOwner, isShutdown);
    }
    private void OnLoadSceneUpdate(object sender, GameEventArgs e)
    {
        var arg = (LoadSceneUpdateEventArgs)e;
        if (arg.UserData != this)
        {
            return;
        }
        //Log.Info("场景加载进度:{0}, {1}", arg.Progress, arg.SceneAssetName);
        //TODO 显示场景加载进度
    }

    private void OnLoadSceneSuccess(object sender, GameEventArgs e)
    {
        var arg = (LoadSceneSuccessEventArgs)e;
        if (arg.UserData != this)
        {
            return;
        }
        //Log.Info("场景加载成功:{0}", arg.SceneAssetName);
        loadSceneOver = true;
    }
    //加载场景资源失败 重启游戏框架
    private void OnLoadSceneFailure(object sender, GameEventArgs e)
    {
        var arg = (LoadSceneFailureEventArgs)e;
        if (arg.UserData != this)
        {
            return;
        }

        Log.Error("加载场景失败,自动重启框架！", arg.SceneAssetName);
        GameEntry.Shutdown(ShutdownType.Restart);
    }
}
