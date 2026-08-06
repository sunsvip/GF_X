using GameFramework.Localization;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace UGF.EditorTools
{
    internal static class BaiduTranslationService
    {
        private const string BaiduTranslationUrl = "https://fanyi-api.baidu.com/api/trans/vip/translate";

        internal static IEnumerator TranslateCoroutine(string sourceText, Language sourceLanguage, Language targetLanguage, Action<bool, TranslationResult, object> onComplete, object userData)
        {
            var randomCode = DateTime.Now.Ticks.ToString();

            var builder = new StringBuilder();
            builder.AppendFormat("q={0}", UnityWebRequest.EscapeURL(sourceText));
            builder.AppendFormat("&from={0}", GetBaiduLanguage(sourceLanguage) ?? "auto");
            builder.AppendFormat("&to={0}", GetBaiduLanguage(targetLanguage));
            builder.AppendFormat("&appid={0}", EditorToolSettings.Instance.BaiduTransAppId);
            builder.AppendFormat("&salt={0}", randomCode);
            builder.AppendFormat("&sign={0}", GenerateBaiduSign(sourceText, randomCode));

            using var webRequest = new UnityWebRequest(BaiduTranslationUrl, UnityWebRequest.kHttpVerbPOST)
            {
                uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(builder.ToString())),
                downloadHandler = new DownloadHandlerBuffer()
            };
            webRequest.SetRequestHeader("Content-Type", "application/x-www-form-urlencoded;charset=UTF-8");
            webRequest.certificateHandler = new WebRequestCertNoValidate();
            webRequest.timeout = 30;
            webRequest.SendWebRequest();
            while (!webRequest.isDone)
            {
                yield return null;
            }

            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"---------翻译{targetLanguage}请求失败:{webRequest.error}---------");
                onComplete?.Invoke(false, null, userData);
                yield break;
            }

            var json = webRequest.downloadHandler.text;
            try
            {
                var responseJson = UtilityBuiltin.Json.ToObject<JObject>(json);
                if (responseJson.ContainsKey("trans_result"))
                {
                    var resultArray = responseJson["trans_result"].ToObject<TranslationResult[]>();
                    if (resultArray != null && resultArray.Length > 0)
                    {
                        onComplete?.Invoke(true, resultArray[0], userData);
                        yield break;
                    }
                }

                Debug.LogError($"---------翻译{targetLanguage}失败:{responseJson}---------");
                onComplete?.Invoke(false, null, userData);
            }
            catch (Exception exception)
            {
                Debug.LogError($"---------翻译{targetLanguage}返回数据解析失败:{exception.Message}---------");
                onComplete?.Invoke(false, null, userData);
            }
        }

        internal static string GetBaiduLanguage(Language language)
        {
            switch (language)
            {
                case Language.Afrikaans: return "afr";
                case Language.Albanian: return "alb";
                case Language.Arabic: return "ara";
                case Language.Basque: return "baq";
                case Language.Belarusian: return "bel";
                case Language.Bulgarian: return "bul";
                case Language.Catalan: return "cat";
                case Language.ChineseSimplified: return "zh";
                case Language.ChineseTraditional: return "cht";
                case Language.Croatian: return "hrv";
                case Language.Czech: return "cs";
                case Language.Danish: return "dan";
                case Language.Dutch: return "nl";
                case Language.English: return "en";
                case Language.Estonian: return "est";
                case Language.Faroese: return "fao";
                case Language.Finnish: return "fin";
                case Language.French: return "fra";
                case Language.Georgian: return "geo";
                case Language.German: return "de";
                case Language.Greek: return "el";
                case Language.Hebrew: return "heb";
                case Language.Hungarian: return "hu";
                case Language.Icelandic: return "ice";
                case Language.Indonesian: return "id";
                case Language.Italian: return "it";
                case Language.Japanese: return "jp";
                case Language.Korean: return "kor";
                case Language.Latvian: return "lav";
                case Language.Lithuanian: return "lit";
                case Language.Macedonian: return "mac";
                case Language.Malayalam: return "may";
                case Language.Norwegian: return "nor";
                case Language.Persian: return "per";
                case Language.Polish: return "pl";
                case Language.PortugueseBrazil: return "pt";
                case Language.PortuguesePortugal: return "pt";
                case Language.Romanian: return "rom";
                case Language.Russian: return "ru";
                case Language.SerboCroatian: return "sec";
                case Language.SerbianCyrillic: return "src";
                case Language.SerbianLatin: return "srp";
                case Language.Slovak: return "sk";
                case Language.Slovenian: return "slo";
                case Language.Spanish: return "spa";
                case Language.Swedish: return "swe";
                case Language.Thai: return "th";
                case Language.Turkish: return "tr";
                case Language.Ukrainian: return "ukr";
                case Language.Vietnamese: return "vie";
                default:
                    return null;
            }
        }

        private static string GenerateBaiduSign(string sourceText, string randomCode)
        {
            using MD5 md5 = MD5.Create();
            var fullString = GameFramework.Utility.Text.Format(
                "{0}{1}{2}{3}",
                EditorToolSettings.Instance.BaiduTransAppId,
                sourceText,
                randomCode,
                EditorToolSettings.Instance.BaiduTransSecretKey);
            var sourceBytes = Encoding.UTF8.GetBytes(fullString);
            var hashBytes = md5.ComputeHash(sourceBytes);
            var builder = new StringBuilder();
            for (var i = 0; i < hashBytes.Length; i++)
            {
                builder.Append(hashBytes[i].ToString("x2"));
            }

            return builder.ToString();
        }
    }
}
