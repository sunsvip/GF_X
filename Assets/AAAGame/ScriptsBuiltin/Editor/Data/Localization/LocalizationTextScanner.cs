using GameFramework.Localization;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using TMPro.EditorUtilities;

namespace UGF.EditorTools
{
    public class LocalizationText
    {
        public string Key;
        public string Value;
        public bool Locked;//true表示禁止移除和修改Key值
    }
    public class LocalizationTextScanner
    {
        static readonly string[] LocalizationFuncNames = { "GF.Localization.GetText", "GF.Localization.GetString", "GFBuiltin.Localization.GetText", "GFBuiltin.Localization.GetString" };
        internal const int MinLength = 600;
        internal const int MaxLength = 4000;
        public const string EXCEL_I18N_TAG = "i18n";//把Excel表备注行标识为i18n的列扫描到多语言Excel
        public static void Save2LanguagesExcel(List<LocalizationText> mainLangList, Action<string, int, int> onSaveProgress = null)
        {
            if (mainLangList == null || EditorToolSettings.Instance.LanguagesSupport == null || EditorToolSettings.Instance.LanguagesSupport.Count < 1) return;

            var mainLang = (Language)EditorToolSettings.Instance.LanguagesSupport[0];
            onSaveProgress?.Invoke(mainLang.ToString(), EditorToolSettings.Instance.LanguagesSupport.Count, 0);
            LocalizationLanguageExcelRepository.SaveLanguage(mainLang, mainLangList);
            List<LocalizationText> tmpTextList = new List<LocalizationText>();
            for (int i = 1; i < EditorToolSettings.Instance.LanguagesSupport.Count; i++)
            {
                var lang = (Language)EditorToolSettings.Instance.LanguagesSupport[i];

                onSaveProgress?.Invoke(lang.ToString(), EditorToolSettings.Instance.LanguagesSupport.Count, i);
                LocalizationLanguageExcelRepository.LoadLanguageExcelTexts(lang, ref tmpTextList);
                LocalizationMergeService.MergeTexts(mainLangList, ref tmpTextList);
                LocalizationLanguageExcelRepository.SaveLanguage(lang, tmpTextList);
            }
        }

        /// <summary>
        /// 把扫描到的本地化文本合并到List<LocalizationText> list
        /// </summary>
        /// <param name="list"></param>
        /// <param name="texts"></param>
        public static void MergeTexts(List<string> texts, ref List<LocalizationText> list)
        {
            LocalizationMergeService.MergeTexts(texts, ref list);
        }
        public static void MergeTexts(List<LocalizationText> srcList, ref List<LocalizationText> destList)
        {
            LocalizationMergeService.MergeTexts(srcList, ref destList);
        }
        /// <summary>
        /// 扫描prefab,datatable以及代码中所有本地化文本
        /// </summary>
        /// <returns></returns>
        public static List<string> ScanAllLocalizationText(Action<string, int, int> onScanProgress = null)
        {
            return LocalizationScanService.ScanAllLocalizationText(LocalizationFuncNames, onScanProgress);
        }
        /// <summary>
        /// 扫描全部代码中的国际化文本
        /// </summary>
        /// <param name="csFiles">注意:路径要求是完整路径</param>
        /// <param name="funcName"></param>
        /// <param name="outputFile"></param>
        /// <param name="onProgressUpdate"></param>
        /// <param name="scanByDir">true:按文件夹扫描; false:逐个cs文件扫描</param>
        /// <returns></returns>
        public static List<string> ScanLocalizationTextFromCode(string csFileDir, string[] funcNames, string outputFile, Action<string, int, int> onProgressUpdate = null, bool scanByDir = false)
        {
            return LocalizationScanService.ScanLocalizationTextFromCode(csFileDir, funcNames, outputFile, onProgressUpdate, scanByDir);
        }


        /// <summary>
        /// 扫描Prefab中的国际化语言
        /// </summary>
        public static List<string> ScanLocalizationTextFromPrefab(Action<string, int, int> onProgressUpdate = null)
        {
            return LocalizationScanService.ScanLocalizationTextFromPrefab(onProgressUpdate);
        }
        /// <summary>
        /// 从DataTable Excel文件扫描本地化文本
        /// </summary>
        /// <param name="onProgressUpdate"></param>
        /// <returns></returns>
        public static List<string> ScanLocalizationTextFromDataTables(Action<string, int, int> onProgressUpdate = null)
        {
            return LocalizationScanService.ScanLocalizationTextFromDataTables(onProgressUpdate);
        }
        /// <summary>
        /// 加载语言的所有本地化数据
        /// </summary>
        /// <param name="language"></param>
        /// <param name="localizationTexts"></param>
        public static void LoadLanguageExcelTexts(Language language, ref List<LocalizationText> localizationTexts)
        {
            LocalizationLanguageExcelRepository.LoadLanguageExcelTexts(language, ref localizationTexts);
        }

        public static void LoadLanguageExcelTexts(string languageExcelFile, ref List<LocalizationText> localizationTexts)
        {
            LocalizationLanguageExcelRepository.LoadLanguageExcelTexts(languageExcelFile, ref localizationTexts);
        }

        /// <summary>
        /// 翻译所有多语言Excel
        /// </summary>
        /// <param name="forceAll">是否强制翻译所有行, 默认只翻译空白行</param>
        public static bool TranslateAllLanguages(bool forceAll = false, Action<string, int, int> onProgressUpdate = null, Action onComplete = null)
        {
            return LocalizationTranslationService.TranslateAllLanguages(forceAll, onProgressUpdate, onComplete);
        }

        internal static LocalizationTranslationStatusSnapshot GetTranslationStatus()
        {
            return LocalizationTranslationService.GetStatusSnapshot();
        }

    }
    internal class TranslationResult
    {
        public string src;
        public string dst;
    }
}
