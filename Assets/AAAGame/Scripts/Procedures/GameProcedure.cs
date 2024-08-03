using GameFramework.Fsm;
using GameFramework.Procedure;
using UnityEngine;
using GameFramework;
using UnityGameFramework.Runtime;
using GameFramework.Event;


public class GameProcedure : ProcedureBase
{
    public GameUIForm GameUI { get; private set; }
    public LevelEntity Level { get; private set; }
    private IFsm<IProcedureManager> procedure;

    protected override void OnEnter(IFsm<IProcedureManager> procedureOwner)
    {
        base.OnEnter(procedureOwner);
        this.procedure = procedureOwner;

        if (GF.Base.IsGamePaused)
        {
            GF.Base.ResumeGame();
        }
        GF.Event.Subscribe(ShowEntitySuccessEventArgs.EventId, OnShowEntitySuccess);
        GF.Event.Subscribe(OpenUIFormSuccessEventArgs.EventId, OnOpenUIFormSuccess);
        GF.Event.Subscribe(CloseUIFormCompleteEventArgs.EventId, OnCloseUIForm);
        Level = procedureOwner.GetData<VarUnityObject>("LevelEntity").Value as LevelEntity;
        procedureOwner.RemoveData("LevelEntity");
        Level.StartGame();

        var uiParms = UIParams.Acquire();
        uiParms.ButtonClickCallback = (sender, btId) =>
        {
            switch (btId)
            {
                case "ADD":
                    Level.AddEnemies(10);
                    break;
                case "SUB":
                    Level.RemoveEnemies(10);
                    break;
            }
        };
        GF.UI.OpenUIForm(UIViews.GameUIForm, uiParms);
    }
    protected override void OnUpdate(IFsm<IProcedureManager> procedureOwner, float elapseSeconds, float realElapseSeconds)
    {
        base.OnUpdate(procedureOwner, elapseSeconds, realElapseSeconds);

    }
    protected override void OnLeave(IFsm<IProcedureManager> procedureOwner, bool isShutdown)
    {
        if (GF.Base.IsGamePaused)
        {
            GF.Base.ResumeGame();
        }
        GF.Event.Unsubscribe(ShowEntitySuccessEventArgs.EventId, OnShowEntitySuccess);
        GF.Event.Unsubscribe(OpenUIFormSuccessEventArgs.EventId, OnOpenUIFormSuccess);
        GF.Event.Unsubscribe(CloseUIFormCompleteEventArgs.EventId, OnCloseUIForm);
        if (this.procedure.HasData(Const.UserData.GAME_LEVEL)) this.procedure.RemoveData(Const.UserData.GAME_LEVEL);
        base.OnLeave(procedureOwner, isShutdown);
    }


    public void Restart()
    {
        ChangeState<MenuProcedure>(procedure);
    }
    public void BackHome()
    {
        ChangeState<MenuProcedure>(procedure);
    }
    private void OnGameOver(bool isWin)
    {
        Log.Info("Game Over, isWin:{0}", isWin);
        procedure.SetData<VarBoolean>("IsWin", isWin);
        ChangeState<GameOverProcedure>(procedure);
    }
    private void CheckGamePause()
    {
        if (GameUI == null)
        {
            return;
        }
        if (GF.UI.GetTopUIFormId() != GameUI.UIForm.SerialId)
        {
            if (!GF.Base.IsGamePaused)
            {
                GF.Base.PauseGame();
            }
        }
        else
        {
            if (GF.Base.IsGamePaused)
            {
                GF.Base.ResumeGame();
            }
        }
    }
    private void OnCloseUIForm(object sender, GameEventArgs e)
    {
        CheckGamePause();
    }

    private void OnOpenUIFormSuccess(object sender, GameEventArgs e)
    {
        var args = e as OpenUIFormSuccessEventArgs;
        if (args.UIForm.Logic.GetType() == typeof(GameUIForm))
        {
            GameUI = args.UIForm.Logic as GameUIForm;
            Level?.StartGame();
        }
        CheckGamePause();
    }
    private void OnShowEntitySuccess(object sender, GameEventArgs e)
    {
        var args = e as ShowEntitySuccessEventArgs;

    }

    public void EnterNextLevel(bool isNext)
    {
        var playerDm = GF.DataModel.GetOrCreate<PlayerDataModel>();
        if (isNext)
            playerDm.GAME_LEVEL += 1;
        else
        {
            playerDm.GAME_LEVEL -= 1;
            playerDm.GAME_LEVEL = Mathf.Clamp(playerDm.GAME_LEVEL, 1, playerDm.GAME_LEVEL);
        }

        this.procedure.SetData<VarBoolean>("EnterNextLevel", true);
        ChangeState<MenuProcedure>(procedure);
    }

    #region - Debug Mode
    public void ChangeLevel(int level)
    {
        var playerDm = GF.DataModel.GetOrCreate<PlayerDataModel>();
        playerDm.GAME_LEVEL = level;
        this.procedure.SetData<VarBoolean>("EnterNextLevel", true);
        ChangeState<MenuProcedure>(procedure);
    }
    #endregion
}
