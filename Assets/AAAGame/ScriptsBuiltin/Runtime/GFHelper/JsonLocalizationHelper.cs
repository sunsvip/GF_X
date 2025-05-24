using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GameFramework;
using UnityGameFramework.Runtime;
using GameFramework.Localization;
[Obfuz.ObfuzIgnore]
public class JsonLocalizationHelper : DefaultLocalizationHelper
{
    public override bool ParseData(ILocalizationManager localizationManager, string dictionaryString, object userData)
    {
        var dic = Utility.Json.ToObject<Dictionary<string, string>>(dictionaryString);
        if (dic == null)
        {
            return false;
        }
        foreach (KeyValuePair<string, string> item in dic)
        {
            localizationManager.AddRawString(item.Key, System.Text.RegularExpressions.Regex.Unescape(item.Value));
        }
        return true;
    }
}
