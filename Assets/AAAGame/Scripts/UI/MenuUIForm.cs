using UnityEngine;
using GameFramework.Event;

public partial class MenuUIForm : UIFormBase
{
    [SerializeField] bool showLvSwitch = false;

    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);
        GF.Event.Subscribe(UserDataChangedEventArgs.EventId, OnUserDataChanged);
        GF.Event.Subscribe(PlayerEventArgs.EventId, OnPlayerEvent);
        RefreshMoneyText();
    }
    protected override void OnUpdate(float elapseSeconds, float realElapseSeconds)
    {
        base.OnUpdate(elapseSeconds, realElapseSeconds);

    }

    protected override void OnClose(bool isShutdown, object userData)
    {
        GF.Event.Unsubscribe(UserDataChangedEventArgs.EventId, OnUserDataChanged);
        GF.Event.Unsubscribe(PlayerEventArgs.EventId, OnPlayerEvent);
        base.OnClose(isShutdown, userData);
    }

    private void OnPlayerEvent(object sender, GameEventArgs e)
    {
        var args = e as PlayerEventArgs;
        
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
        var args = e as UserDataChangedEventArgs;
        switch (args.Type)
        {
            case UserDataType.MONEY:
                RefreshMoneyText();
                break;
            case UserDataType.GAME_LEVEL:

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
        GF.DataModel.GetOrCreate<PlayerDataModel>().GAME_LEVEL += dir;
        var menuProcedure = GF.Procedure.CurrentProcedure as MenuProcedure;
        if (null != menuProcedure)
        {
            menuProcedure.ShowLevel();
        }
    }
}
