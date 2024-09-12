using GameFramework;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityGameFramework.Runtime;
public class PlayerDataModel : DataModelBase
{
    public int Coins
    {
        get
        {
            return GF.Setting.GetInt(Const.UserData.MONEY, GF.Config.GetInt("DEFAULT_COINS"));
        }
        set
        {
            int oldNum = Coins;
            int fixedNum = Mathf.Max(0, value);
            GF.Setting.SetInt(Const.UserData.MONEY, fixedNum);
            FireUserDataChanged(UserDataType.MONEY, oldNum, fixedNum);
        }
    }
    /// <summary>
    /// 关卡
    /// </summary>
    public int GAME_LEVEL
    {
        get { return GF.Setting.GetInt(Const.UserData.GAME_LEVEL, 1); }
        set
        {
            var lvTb = GF.DataTable.GetDataTable<LevelTable>();
            int preLvId = GAME_LEVEL;

            int nextLvId = Const.RepeatLevel ? value : Mathf.Clamp(value, lvTb.MinIdDataRow.Id, lvTb.MaxIdDataRow.Id);
            GF.Setting.SetInt(Const.UserData.GAME_LEVEL, nextLvId);
            FireUserDataChanged(UserDataType.GAME_LEVEL, preLvId, nextLvId);
        }
    }

    public int GetCurrentLevelId()
    {

        var lvTb = GF.DataTable.GetDataTable<LevelTable>();
        if (lvTb == null) Log.Fatal("Get LevelTable failed");
        return (GAME_LEVEL - 1) % lvTb.MaxIdDataRow.Id + 1;
    }
    internal void ClaimMoney(int bonus, bool showFx, Vector3 fxSpawnPos)
    {
        int initMoney = this.Coins;
        this.Coins += bonus;
        GF.Event.Fire(this, ReferencePool.Acquire<PlayerEventArgs>().Fill(PlayerEventType.ClaimMoney, new Dictionary<string, object>
        {
            ["ShowFX"] = showFx,
            ["SpawnPoint"] = fxSpawnPos,
            ["StartNum"] = initMoney
        }));
    }
    internal Vector2Int GetOfflineBonus()
    {
        int offlineFactor = GF.Config.GetInt("OfflineFactor");
        int bonus = GF.Config.GetInt("OfflineBonus");
        int bonusMulti = GF.Config.GetInt("OfflineAdBonusMulti");
        int maxBonus = GF.Config.GetInt("MaxOfflineBonus");
        var offlineMinutes = GetOfflineTime().TotalMinutes;
        Vector2Int result = Vector2Int.zero;

        result.x = Mathf.Clamp(bonus * Mathf.FloorToInt((float)offlineMinutes / offlineFactor), 0, maxBonus);
        result.y = Mathf.CeilToInt(result.x * bonusMulti);
        return result;
    }
    internal TimeSpan GetOfflineTime()
    {
        string dTimeStr = GF.Setting.GetString(ConstBuiltin.Setting.QuitAppTime, string.Empty);
        if (string.IsNullOrWhiteSpace(dTimeStr) || !DateTime.TryParse(dTimeStr, out DateTime exitTime))
        {
            return TimeSpan.Zero;
        }
        return System.DateTime.UtcNow.Subtract(exitTime);
    }
    internal bool IsNewDay()
    {
        string dTimeStr = GF.Setting.GetString(ConstBuiltin.Setting.QuitAppTime, string.Empty);
        if (string.IsNullOrWhiteSpace(dTimeStr) || !DateTime.TryParse(dTimeStr, out DateTime dTime))
        {
            return true;
        }

        var today = DateTime.Today;
        return !(today.Year == dTime.Year && today.Month == dTime.Month && today.Day == dTime.Day);
    }
    internal void CheckAndShowRating(float ratio)
    {
        if (GF.UI.HasUIForm(UIViews.RatingDialog) || GF.Setting.GetBool("RATED_FIVE", false))
        {
            return;
        }

        int show_count = GF.Setting.GetInt(Const.UserData.SHOW_RATING_COUNT, 0);
        if (show_count > 3 || UnityEngine.Random.value > ratio)
        {
            return;
        }

        GF.Setting.SetInt(Const.UserData.SHOW_RATING_COUNT, ++show_count);
        GF.UI.OpenUIForm(UIViews.RatingDialog);
    }
    /// <summary>
    /// 触发用户数据改变事件
    /// </summary>
    /// <param name="tp"></param>
    /// <param name="udt"></param>
    private void FireUserDataChanged(UserDataType tp, object oldValue, object value)
    {
        GF.Event.Fire(this, ReferencePool.Acquire<UserDataChangedEventArgs>().Fill(tp, oldValue, value));
    }

    internal int GetMultiReward(int rewardNum, int multi)
    {
        return rewardNum * multi;
    }
}
