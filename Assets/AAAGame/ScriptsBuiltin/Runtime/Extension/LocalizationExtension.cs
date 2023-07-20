using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GameFramework;
using GameFramework.Localization;
using UnityGameFramework.Runtime;
public static class LocalizationExtension
{
    /// <summary>
    /// 获取本地化字符串,若不存在则直接返回key
    /// </summary>
    /// <param name="com"></param>
    /// <param name="key"></param>
    /// <returns></returns>
    public static string GetText(this LocalizationComponent com, string key)
    {
        if (!com.HasRawString(key)) return key;
        return com.GetString(key);
    }
    /// <summary>
    /// 获取本地化文本, 并格式化字符串
    /// </summary>
    /// <param name="com"></param>
    /// <param name="key"></param>
    /// <param name="parms"></param>
    /// <returns></returns>
    public static string GetText(this LocalizationComponent com, string key, params object[] parms)
    {
        return com.GetString(key, parms);
    }
    /// <summary>
    /// 获取本地化文本,并转换大小写
    /// </summary>
    /// <param name="com"></param>
    /// <param name="key"></param>
    /// <param name="toUpperOrLower">true:转为大写字母, false:转为小写字母</param>
    /// <returns></returns>
    public static string GetText(this LocalizationComponent com, string key, bool toUpperOrLower)
    {
        string result = com.GetString(key);
        if (com.Language == Language.English)
            return toUpperOrLower ? result.ToUpper() : result.ToLower();

        return result;
    }
    /// <summary>
    /// 获取本地化文本,并转换大小写
    /// </summary>
    /// <param name="com"></param>
    /// <param name="key"></param>
    /// <param name="toUpperOrLower"></param>
    /// <param name="parms"></param>
    /// <returns></returns>
    public static string GetText(this LocalizationComponent com, string key, bool toUpperOrLower, params object[] parms)
    {
        string result = com.GetString(key, parms);
        if (com.Language == Language.English)
            return toUpperOrLower ? result.ToUpper() : result.ToLower();

        return result;
    }
}
