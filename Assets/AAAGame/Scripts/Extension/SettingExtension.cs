using GameFramework;
using UnityGameFramework.Runtime;
public static class SettingExtension
{
    /// <summary>
    /// 设置A/B测试组
    /// </summary>
    /// <param name="com"></param>
    /// <param name="groupName"></param>
    public static void SetABTestGroup(this SettingComponent com, string groupName)
    {
        com.SetString(ConstBuiltin.Setting.ABTestGroup, groupName);
    }
    /// <summary>
    /// 获取A/B测试组
    /// </summary>
    /// <param name="com"></param>
    /// <returns></returns>
    public static string GetABTestGroup(this SettingComponent com)
    {
        return com.GetString(ConstBuiltin.Setting.ABTestGroup);
    }
    /// <summary>
    /// 设置语言
    /// </summary>
    /// <param name="com"></param>
    /// <param name="lan"></param>
    public static void SetLanguage(this SettingComponent com, GameFramework.Localization.Language lan, bool saveSetting = true)
    {
        GFBuiltin.Localization.Language = lan;
        GFBuiltin.Setting.SetString(ConstBuiltin.Setting.Language, lan.ToString());
    }

    /// <summary>
    /// 获取当前设置的语言
    /// </summary>
    /// <returns></returns>
    public static GameFramework.Localization.Language GetLanguage(this SettingComponent com)
    {
        string lan = GFBuiltin.Setting.GetString(ConstBuiltin.Setting.Language, string.Empty);
        if (string.IsNullOrEmpty(lan))
        {
            return GameFramework.Localization.Language.Unspecified;
        }

        if (!System.Enum.TryParse(lan, out GameFramework.Localization.Language language))
        {
            language = GameFramework.Localization.Language.English;
        }
        return language;
    }

    /// <summary>
    /// 开启或关闭音乐/音效/震动
    /// </summary>
    /// <param name="com"></param>
    /// <param name="group"></param>
    /// <param name="isMute"></param>
    public static void SetMediaMute(this SettingComponent com, Const.SoundGroup group, bool isMute)
    {
        string groupName = group.ToString();
        
        var mediaGp = GF.Sound.GetSoundGroup(groupName);
        if (null == mediaGp)
        {
            return;
        }
        mediaGp.Mute = isMute;
        GF.Setting.SetBool(groupName, isMute);
    }
    /// <summary>
    /// 获取音乐/音效/震动开启状态
    /// </summary>
    /// <param name="com"></param>
    /// <param name="group"></param>
    /// <returns></returns>
    public static bool GetMediaMute(this SettingComponent com, Const.SoundGroup group)
    {
        return GF.Setting.GetBool(group.ToString(), false);
    }
    /// <summary>
    /// 设置音乐/音效音量
    /// </summary>
    /// <param name="com"></param>
    /// <param name="group"></param>
    /// <param name="volume"></param>
    public static void SetMediaVolume(this SettingComponent com, Const.SoundGroup group, float volume)
    {
        string groupName = group.ToString();
        var soundGp = GF.Sound.GetSoundGroup(groupName);
        if (null == soundGp)
        {
            return;
        }
        soundGp.Volume = volume;
        GF.Setting.SetFloat(Utility.Text.Format("{0}.Volume",groupName), soundGp.Volume);
    }
    /// <summary>
    /// 获取音乐/音效音量
    /// </summary>
    /// <param name="com"></param>
    /// <param name="group"></param>
    /// <returns></returns>
    public static float GetMediaVolume(this SettingComponent com, Const.SoundGroup group)
    {
        return GF.Setting.GetFloat(Utility.Text.Format("{0}.Volume", group.ToString()), 1);
    }

}