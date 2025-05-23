using DG.Tweening;
using GameFramework.Fsm;
using GameFramework.Procedure;
using UnityGameFramework.Runtime;
[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
public class GameOverProcedure : ProcedureBase
{
    IFsm<IProcedureManager> procedure;
    private bool isWin;

    protected override void OnEnter(IFsm<IProcedureManager> procedureOwner)
    {
        base.OnEnter(procedureOwner);
        this.procedure = procedureOwner;
        isWin = this.procedure.GetData<VarBoolean>("IsWin");

        ShowGameOverUIForm(2);
    }
    protected override void OnLeave(IFsm<IProcedureManager> procedureOwner, bool isShutdown)
    {
        if (!isShutdown)
        {
            GF.UI.CloseAllLoadingUIForms();
            GF.UI.CloseAllLoadedUIForms();
            GF.Entity.HideAllLoadingEntities();
            GF.Entity.HideAllLoadedEntities();
        }
        base.OnLeave(procedureOwner, isShutdown);
    }

    private void ShowGameOverUIForm(float delay)
    {
        DOVirtual.DelayedCall(delay, () =>
        {
            var gameoverParms = UIParams.Create();
            gameoverParms.Set<VarBoolean>(GameOverUIForm.P_IsWin, isWin);
            GF.UI.OpenUIForm(UIViews.GameOverUIForm, gameoverParms);
        });
    }

    internal void BackHome()
    {
        ChangeState<MenuProcedure>(procedure);
    }
}
