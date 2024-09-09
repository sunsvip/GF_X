
using System.Collections.Generic;

/// <summary>
/// 热更Const
/// </summary>
public static partial class Const
{
    internal const long DefaultVibrateDuration = 50;//安卓手机震动强度
    internal static readonly float SHOW_CLOSE_INTERVAL = 1f;//出现关闭按钮的延迟

    public static readonly string HORIZONTAL = "Horizontal";
    public static readonly bool RepeatLevel = true;//是否循环关卡

    internal static class UserData
    {
        internal static readonly string MONEY = "UserData.MONEY";
        internal static readonly string GUIDE_ON = "UserData.GUIDE_ON";

        internal static readonly string SHOW_RATING_COUNT = "UserData.SHOW_RATING_COUNT";
        internal static readonly string GAME_LEVEL = "UserData.GAME_LEVEL";
        internal static readonly string CAR_SKIN_ID = "UserData.CAR_SKIN_ID";

        internal static readonly string USER_SPAWN_POINT_TYPE = "UserData.USER_SPAWN_POINT_TYPE";
    }
}
