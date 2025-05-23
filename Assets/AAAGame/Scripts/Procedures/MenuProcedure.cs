using GameFramework;
using GameFramework.Event;
using GameFramework.Fsm;
using GameFramework.Procedure;
using UnityEngine;
using UnityGameFramework.Runtime;
[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
public class MenuProcedure : ProcedureBase
{
    int menuUIFormId;
    LevelEntity lvEntity;

    IFsm<IProcedureManager> procedure;
    protected override void OnInit(IFsm<IProcedureManager> procedureOwner)
    {
        base.OnInit(procedureOwner);
    }
    protected override void OnEnter(IFsm<IProcedureManager> procedureOwner)
    {
        base.OnEnter(procedureOwner);
        procedure = procedureOwner;
        ShowLevel();//加载关卡
        //var res = await GF.WebRequest.AddWebRequestAsync("https://blog.csdn.net/final5788");
        //Log.Info(Utility.Converter.GetString(res.Bytes));
    }

    protected override void OnUpdate(IFsm<IProcedureManager> procedureOwner, float elapseSeconds, float realElapseSeconds)
    {
        base.OnUpdate(procedureOwner, elapseSeconds, realElapseSeconds);
        if (lvEntity == null || !lvEntity.IsAllReady)
        {
            return;
        }
        //点击屏幕开始游戏
        if (Input.GetMouseButtonDown(0) && !GF.UI.IsPointerOverUIObject(Input.mousePosition) && GF.UI.GetTopUIFormId() == menuUIFormId)
        {
            EnterGame();
        }
    }
    protected override void OnLeave(IFsm<IProcedureManager> procedureOwner, bool isShutdown)
    {
        if (!isShutdown)
        {
            GF.UI.CloseUIForm(menuUIFormId);
        }
        base.OnLeave(procedureOwner, isShutdown);
    }
    public void EnterGame()
    {
        procedure.SetData<VarUnityObject>("LevelEntity", lvEntity);
        ChangeState<GameProcedure>(procedure);
    }
    public async void ShowLevel()
    {
        lvEntity = null;
        if (GF.Base.IsGamePaused)
        {
            GF.Base.ResumeGame();
        }
        GF.UI.CloseAllLoadingUIForms();
        GF.UI.CloseAllLoadedUIForms();
        GF.Entity.HideAllLoadingEntities();
        GF.Entity.HideAllLoadedEntities();

        //异步打开主菜单UI
        menuUIFormId = GF.UI.OpenUIForm(UIViews.MenuUIForm);

        //动态创建关卡
        var lvTb = GF.DataTable.GetDataTable<LevelTable>();
        var playerMd = GF.DataModel.GetOrCreate<PlayerDataModel>();
        var lvRow = lvTb.GetDataRow(playerMd.LevelId);

        var lvParams = EntityParams.Create(Vector3.zero, Vector3.zero, Vector3.one);
        lvParams.Set(LevelEntity.P_LevelData, lvRow);
        lvEntity = await GF.Entity.ShowEntityAwait<LevelEntity>(lvRow.LvPfbName, Const.EntityGroup.Level, lvParams) as LevelEntity;
        GF.BuiltinView.HideLoadingProgress();
    }
}
