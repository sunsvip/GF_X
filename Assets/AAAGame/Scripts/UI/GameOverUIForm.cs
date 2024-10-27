using DG.Tweening;
using GameFramework;
using GameFramework.Event;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityGameFramework.Runtime;

public partial class GameOverUIForm : UIFormBase
{
    public const string P_IsWin = "IsWin";
    
    private bool isWin;
    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);
        
        isWin = Params.Get<VarBoolean>(P_IsWin);
        varTitleTxt.text = isWin ? GF.Localization.GetString("Victory") : GF.Localization.GetString("Failed");
    }
    protected override void OnButtonClick(object sender, Button btSelf)
    {
        base.OnButtonClick(sender, btSelf);
        if(btSelf == varBackBtn)
        {
            (GF.Procedure.CurrentProcedure as GameOverProcedure).BackHome();
        }
    }
}
