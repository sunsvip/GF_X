using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using GameFramework.Event;

public enum PlayerEventType
{
    TriggerShopPanel,
    BuyFollower,
    UpgradeItem,
    TriggerUpgradePanel,
    BuyAIPlayer, 
    OnUnlocked,
    ExitGame,   // 退出游戏时通知
    ClaimMoney,
    NotifyFreshUpgradeView, //通知刷新升级界面信息
    InterstitialAdClose,
    UpgradeByAd
}
public class PlayerEventArgs : GameEventArgs
{
    public static readonly int EventId = typeof(PlayerEventArgs).GetHashCode();
    public override int Id { get { return EventId; } }
    public PlayerEventType EventType { get; private set; }
    public object EventData { get; private set; }
    public override void Clear()
    {
        this.EventData = null;
    }
    public PlayerEventArgs Fill(PlayerEventType eventType, object eventData = null)
    {
        this.EventType = eventType;
        this.EventData = eventData;
        return this;
    }
}
