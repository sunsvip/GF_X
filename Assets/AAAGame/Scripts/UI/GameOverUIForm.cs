using DG.Tweening;
using GameFramework;
using GameFramework.Event;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityGameFramework.Runtime;

public class GameOverUIForm : UIFormBase
{
    public const string P_IsWin = "IsWin";
    [SerializeField] Text moneyText;
    [SerializeField] GameObject[] panels;
    [SerializeField] Text rewardNumText;
    [SerializeField] Transform diamondNode;
    private bool isClaimed;
    private bool animDoing;
    private bool isWin;
    private int rewardNum;
    protected override void OnInit(object userData)
    {
        base.OnInit(userData);

    }
    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);
        isClaimed = false;
        isWin = Params.Get<VarBoolean>(P_IsWin);
        rewardNum = 10;
        panels[0].SetActive(!isWin);
        panels[1].SetActive(isWin);
        topBar.gameObject.SetActive(isWin);
        this.SetMoneyText(GF.DataModel.GetOrCreate<PlayerDataModel>().Coins);
        rewardNumText.text = Utility.Text.Format("+{0}", rewardNum);
    }

    private void SetMoneyText(int money)
    {
        moneyText.text = UtilityBuiltin.Valuer.ToCoins(money);
    }
    protected override void OnButtonClick(object sender, string btId)
    {
        if (animDoing)
        {
            return;
        }
        base.OnButtonClick(sender, btId);
        switch (btId)
        {
            case "RETRY":
                var gameOverProc = GF.Procedure.CurrentProcedure as GameOverProcedure;
                gameOverProc.NextLevel();
                break;
        }
    }
}
