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
        return com.GetString(ConstBuiltin.Setting.ABTestGroup, string.Empty);
    }
    /// <summary>
    /// 设置语言
    /// </summary>
    /// <param name="com"></param>
    /// <param name="lan"></param>
    public static void SetLanguage(this SettingComponent com, GameFramework.Localization.Language lan, bool saveSetting = true)
    {
        GFBuiltin.Localization.Language = lan;
        com.SetString(ConstBuiltin.Setting.Language, lan.ToString());
    }

    /// <summary>
    /// 获取当前设置的语言
    /// </summary>
    /// <returns></returns>
    public static GameFramework.Localization.Language GetLanguage(this SettingComponent com)
    {
        string lan = com.GetString(ConstBuiltin.Setting.Language, string.Empty);
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
    /// 获取音乐/音效/震动是否被静音
    /// </summary>
    /// <param name="com"></param>
    /// <param name="group"></param>
    /// <param name="isMute"></param>
    public static void SetMediaMute(this SettingComponent com, Const.SoundGroup group, bool isOn)
    {
        string groupName = group.ToString();
        string key = Utility.Text.Format("Sound.{0}.Mute", groupName);
        var mediaGp = GF.Sound.GetSoundGroup(groupName);
        if (null == mediaGp)
        {
            return;
        }
        mediaGp.Mute = isOn;
        com.SetBool(key, isOn);
    }
    /// <summary>
    /// 获取音乐/音效/震动是否静音
    /// </summary>
    /// <param name="com"></param>
    /// <param name="group"></param>
    /// <param name="defaultValue">默认值</param>
    /// <returns></returns>
    public static bool GetMediaMute(this SettingComponent com, Const.SoundGroup group, bool defaultValue = true)
    {
        string key = Utility.Text.Format("Sound.{0}.Mute", group);
        return com.GetBool(key, defaultValue);
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
        string key = Utility.Text.Format("Sound.{0}.Volume", groupName);
        var soundGp = GF.Sound.GetSoundGroup(groupName);
        if (null == soundGp)
        {
            return;
        }
        soundGp.Volume = volume;
        com.SetFloat(key, soundGp.Volume);
    }
    /// <summary>
    /// 获取音乐/音效音量
    /// </summary>
    /// <param name="com"></param>
    /// <param name="group"></param>
    /// <param name="defaultVolume">默认音量0-1</param>
    /// <returns></returns>
    public static float GetMediaVolume(this SettingComponent com, Const.SoundGroup group, float defaultVolume = 1f)
    {
        string key = Utility.Text.Format("Sound.{0}.Volume", group.ToString());

        return com.GetFloat(key, defaultVolume);
    }

}