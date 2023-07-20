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
        isWin = Params.Get<VarBoolean>("IsWin");
        rewardNum = 10;
        panels[0].SetActive(!isWin);
        panels[1].SetActive(isWin);
        topBar.gameObject.SetActive(isWin);
        this.SetMoneyText(GF.DataModel.GetOrCreate<PlayerDataModel>().Coins);
        rewardNumText.text = Utility.Text.Format("+{0}", rewardNum);
    }


    private void ClaimReward(int multi, GameFrameworkAction onClaimComplete)
    {
        if (isClaimed)
        {
            onClaimComplete?.Invoke();
            return;
        }
        var playerDm = GF.DataModel.GetOrCreate<PlayerDataModel>();
        animDoing = true;
        int diamondNum = playerDm.GetMultiReward(rewardNum, multi);
        int curMoney = playerDm.Coins;
        isClaimed = true;
        playerDm.Coins += rewardNum;
        float doMoneyNumDuration = Mathf.Clamp(rewardNum * 0.01f, 0.5f, 1f);
        GF.UI.ShowRewardEffect(rewardNumText.transform.position, diamondNode.position,0.5f, () =>
        {
            onClaimComplete?.Invoke();
            animDoing = false;
        }, 30);
        var doMoneyNum = DOTween.To(() => curMoney, (x) => curMoney = x, playerDm.Coins, doMoneyNumDuration).SetEase(Ease.Linear).SetDelay(1f);
        doMoneyNum.onUpdate = () =>
        {
            SetMoneyText(curMoney);
        };
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
            case "CLAIM":
                if (!isClaimed)
                {
                    this.ClaimReward(1, () =>
                    {
                        var gameOverProc = GF.Procedure.CurrentProcedure as GameOverProcedure;
                        gameOverProc.NextLevel();
                    });
                }
                break;
        }
    }
}
