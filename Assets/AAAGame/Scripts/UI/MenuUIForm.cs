using UnityEngine;
using GameFramework.Event;
using UnityGameFramework.Runtime;

public partial class MenuUIForm : UIFormBase
{
    [SerializeField] bool showLvSwitch = false;

    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);
        GF.Event.Subscribe(PlayerDataChangedEventArgs.EventId, OnUserDataChanged);
        RefreshMoneyText();
        var uiparms = UIParams.Create();
        uiparms.Set<VarBoolean>(UITopbar.P_EnableBG, true);
        uiparms.Set<VarBoolean>(UITopbar.P_EnableSettingBtn, true);
        this.OpenSubUIForm(UIViews.Topbar, 1, uiparms);
    }
    protected override void OnUpdate(float elapseSeconds, float realElapseSeconds)
    {
        base.OnUpdate(elapseSeconds, realElapseSeconds);

    }

    protected override void OnClose(bool isShutdown, object userData)
    {
        GF.Event.Unsubscribe(PlayerDataChangedEventArgs.EventId, OnUserDataChanged);
        base.OnClose(isShutdown, userData);
    }


    protected override void OnButtonClick(object sender, string btId)
    {
        base.OnButtonClick(sender, btId);
        switch (btId)
        {
            case "SETTING":
                GF.UI.OpenUIForm(UIViews.SettingDialog);
                break;
        }
    }

    private void OnUserDataChanged(object sender, GameEventArgs e)
    {
        var args = e as PlayerDataChangedEventArgs;
        switch (args.DataType)
        {
            case PlayerDataType.Coins:
                RefreshMoneyText();
                break;
            case PlayerDataType.LevelId:

                break;
        }
    }


    private void RefreshMoneyText()
    {
        var playerDm = GF.DataModel.GetOrCreate<PlayerDataModel>();
        SetMoneyText(playerDm.Coins);
    }
    private void SetMoneyText(int money)
    {
        moneyText.text = UtilityBuiltin.Valuer.ToCoins(money);
    }
    public void SwitchLevel(int dir)
    {
        GF.DataModel.GetOrCreate<PlayerDataModel>().LevelId += dir;
        var menuProcedure = GF.Procedure.CurrentProcedure as MenuProcedure;
        if (null != menuProcedure)
        {
            menuProcedure.ShowLevel();
        }
    }
}
