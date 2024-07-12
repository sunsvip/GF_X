using DG.Tweening;
using GameFramework;
using GameFramework.Fsm;
using GameFramework.Procedure;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityGameFramework.Runtime;

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
        DOTween.Sequence().AppendInterval(delay).onComplete = () =>
        {
            var gameoverParms = UIParams.Acquire();
            gameoverParms.Set<VarBoolean>(GameOverUIForm.P_IsWin, isWin);
            GF.UI.OpenUIForm(UIViews.GameOverUIForm, gameoverParms);
        };
    }

    internal void NextLevel()
    {
        ChangeState<MenuProcedure>(procedure);
    }
}
