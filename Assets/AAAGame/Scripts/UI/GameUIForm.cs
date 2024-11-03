using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using GameFramework;
using GameFramework.Event;
using UnityEngine.EventSystems;
using DG.Tweening;
using System;
using UnityGameFramework.Runtime;
using TMPro;
using UnityEngine.U2D;

public partial class GameUIForm : UIFormBase
{
    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);
        RefreshCoinsText();

        var uiparms = UIParams.Create();
        uiparms.Set<VarBoolean>(UITopbar.P_EnableBG, false);
        uiparms.Set<VarBoolean>(UITopbar.P_EnableSettingBtn, true);
        this.OpenSubUIForm(UIViews.Topbar, 1, uiparms);
    }
    private void RefreshCoinsText()
    {
        var playerDm = GF.DataModel.GetOrCreate<PlayerDataModel>();
        coinNumText.text = playerDm.Coins.ToString();
    }
}
