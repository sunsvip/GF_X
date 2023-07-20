using GameFramework.Localization;
using Newtonsoft.Json.Linq;
using OfficeOpenXml;
using OfficeOpenXml.Drawing.Controls;
using OfficeOpenXml.Style;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using TMPro.EditorUtilities;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

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
#if UNITY_EDITOR_WIN
        const string scannerTool = "Tools/LocalizationStringScanner/LocalizationCodeScanner.exe";
#else
        const string scannerTool = "Tools/LocalizationStringScanner/LocalizationCodeScanner";
#endif
        const string LOCK_TG_NAME = "锁定";
        const string LOCK_TIPS = "勾选锁定此行,锁定后将强制保留";
        const int LOCK_COL = 1; //勾选框所在列
        const int KEY_COL = 2; //key所在列
        const int VALUE_COL = 3; //value所在列
        static readonly string[] LocalizationFuncNames = { "GF.Localization.GetText", "GF.Localization.GetString", "GFBuiltin.Localization.GetText", "GFBuiltin.Localization.GetString" };
        internal const int MinLength = 600;
        internal const int MaxLength = 4000;
        const string CELL_KEY_TIPS = "多语言Key";
        const string CELL_VALUE_TIPS = "多语言Value, 当值为空时, [一键翻译]会自动填充Value值";

        const string BAIDU_TRANS_URL = "https://fanyi-api.baidu.com/api/trans/vip/translate?";//百度在线翻译url
        const string TRANS_SPLIT_TAG = "↕";//通过此符号分割多个翻译
        const string EXCEL_I18N_TAG = "i18n";//把Excel表备注行标识为i18n的列扫描到多语言Excel
        public static void Save2LanguagesExcel(List<LocalizationText> mainLangList, Action<string, int, int> onSaveProgress = null)
        {
            if (mainLangList == null || EditorToolSettings.Instance.LanguagesSupport == null || EditorToolSettings.Instance.LanguagesSupport.Count < 1) return;

            var mainLang = (Language)EditorToolSettings.Instance.LanguagesSupport[0];
            onSaveProgress?.Invoke(mainLang.ToString(), EditorToolSettings.Instance.LanguagesSupport.Count, 0);
            SaveLanguage(mainLang, mainLangList);//保存主语言
            List<LocalizationText> tmpTextList = new List<LocalizationText>();
            for (int i = 1; i < EditorToolSettings.Instance.LanguagesSupport.Count; i++)
            {
                var lang = (Language)EditorToolSettings.Instance.LanguagesSupport[i];

                onSaveProgress?.Invoke(lang.ToString(), EditorToolSettings.Instance.LanguagesSupport.Count, i);
                LoadLanguageExcelTexts(lang, ref tmpTextList);
                MergeTexts(mainLangList, ref tmpTextList);
                SaveLanguage(lang, tmpTextList);
            }
        }
        /// <summary>
        /// 保存本地化数据到语言Excel
        /// </summary>
        /// <param name="mainLang"></param>
        /// <param name="mainLangList"></param>
        private static void SaveLanguage(Language mainLang, List<LocalizationText> mainLangList)
        {
            var excelName = GetLanguageExcelFileName(mainLang);
            try
            {
                var excelFileInfo = new FileInfo(excelName);
                using (var excel = new ExcelPackage(excelFileInfo))
                {
                    ExcelWorksheet sheet;
                    if (excel.Workbook.Worksheets.Count < 1)
                    {
                        sheet = excel.Workbook.Worksheets.Add("Sheet1");
                    }
                    else
                    {
                        sheet = excel.Workbook.Worksheets[0];
                    }
                    sheet.Cells.Clear();
                    sheet.Drawings.Clear();
                    for (int i = 0; i < mainLangList.Count; i++)
                    {
                        int fixedIdx = i + 1;
                        var lanText = mainLangList[i];
                        var cell = sheet.Cells[fixedIdx, LOCK_COL];
                        var checkBox = sheet.Drawings.AddCheckBoxControl(cell.Address);
                        checkBox.Checked = lanText.Locked ? eCheckState.Checked : eCheckState.Unchecked;
                        cell.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        cell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        checkBox.Text = LOCK_TG_NAME;
                        checkBox.LockedText = true;
                        checkBox.SetSize(30, 20);
                        checkBox.SetPosition(i, 0, 0, 0);
                        checkBox.AdjustPositionAndSize();
                        var cellKey = sheet.Cells[fixedIdx, KEY_COL];
                        cellKey.Value = lanText.Key;
                        var cellValue = sheet.Cells[fixedIdx, VALUE_COL];
                        cellValue.Value = lanText.Value;
                        if (i == 0)
                        {
                            var cellComment = cell.Comment ?? cell.AddComment("");
                            cellComment.Text = LOCK_TIPS;

                            var cellKeyComment = cellKey.Comment ?? cellKey.AddComment("");
                            cellKeyComment.Text = (CELL_KEY_TIPS);

                            var cellValueComment = cellValue.Comment ?? cellValue.AddComment("");
                            cellValueComment.Text = (CELL_VALUE_TIPS);
                        }
                    }
                    sheet.Column(LOCK_COL).AutoFit();
                    sheet.Column(KEY_COL).AutoFit(20, 50);
                    sheet.Column(VALUE_COL).AutoFit(20, 50);
                    excel.Save();
                }
            }
            catch (Exception excp)
            {
                Debug.LogError($"保存语言Excel失败:{excp.Message}\n请确保文件({excelName})未被其它程序占用, 若被占用请关闭文件重试!");
            }
        }

        /// <summary>
        /// 把扫描到的本地化文本合并到List<LocalizationText> list
        /// </summary>
        /// <param name="list"></param>
        /// <param name="texts"></param>
        public static void MergeTexts(List<string> texts, ref List<LocalizationText> list)
        {
            foreach (string text in texts)
            {
                LocalizationText existingItem = list.Find(x => x.Key == text);
                if (existingItem == null)
                {
                    list.Add(new LocalizationText
                    {
                        Key = text,
                        Value = "",
                        Locked = false
                    });
                }
            }
            list.RemoveAll(item => !texts.Contains(item.Key) && !item.Locked);
            list.Sort((a, b) => a.Key.CompareTo(b.Key));
        }
        public static void MergeTexts(List<LocalizationText> srcList, ref List<LocalizationText> destList)
        {
            foreach (var text in srcList)
            {
                LocalizationText existingItem = destList.Find(x => x.Key == text.Key);
                if (existingItem == null)
                {
                    destList.Add(new LocalizationText
                    {
                        Key = text.Key,
                        Value = "",
                        Locked = text.Locked
                    });
                }
            }
            destList.RemoveAll(item =>
            {
                var hasItm = srcList.Find(x => x.Key == item.Key) != null;
                return !hasItm && !item.Locked;
            });
            destList.Sort((a, b) => a.Key.CompareTo(b.Key));
        }
        /// <summary>
        /// 扫描prefab,datatable以及代码中所有本地化文本
        /// </summary>
        /// <returns></returns>
        public static List<string> ScanAllLocalizationText(Action<string, int, int> onScanProgress = null)
        {
            var textsFromPrefab = ScanLocalizationTextFromPrefab(onScanProgress);
            var textsFromDataTable = ScanLocalizationTextFromDataTables(onScanProgress);

            var tmpOutputTxtFile = UtilityBuiltin.ResPath.GetCombinePath(ConstEditor.ToolsPath, "LocalizationTextsScannerOutput.txt");
            var textsFromCode = ScanLocalizationTextFromCode(Path.GetDirectoryName(ConstEditor.HotfixAssembly), LocalizationFuncNames, tmpOutputTxtFile, onScanProgress, true);

            List<string> result = new List<string>();
            foreach (var item in textsFromPrefab)
            {
                if (string.IsNullOrWhiteSpace(item) || result.Contains(item)) continue;
                result.Add(item);
            }
            foreach (var item in textsFromDataTable)
            {
                if (string.IsNullOrWhiteSpace(item) || result.Contains(item)) continue;
                result.Add(item);
            }
            foreach (var item in textsFromCode)
            {
                if (string.IsNullOrWhiteSpace(item) || result.Contains(item)) continue;
                result.Add(item);
            }
            return result;
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
            List<string> result = new List<string>();
            if (!Directory.Exists(csFileDir)) return result;
            if (File.Exists(outputFile))
            {
                File.Delete(outputFile);
            }
            var projectDir = Directory.GetParent(Application.dataPath).FullName;
            bool allSuccess = true;

            if (!scanByDir)
            {
                var csFiles = Directory.GetFiles(csFileDir, "*.cs", SearchOption.AllDirectories);
                int totalCount = csFiles.Length;
                for (int i = 0; i < totalCount; i++)
                {
                    var csFile = csFiles[i];
                    onProgressUpdate?.Invoke(csFile, totalCount, i);
                    var success = ScanLocalizationTextFromScript(csFile, funcNames, outputFile);
                    allSuccess &= success;
                }
            }
            else
            {
                onProgressUpdate?.Invoke(csFileDir, 1, 1);
                var success = ScanLocalizationTextFromScript(csFileDir, funcNames, outputFile);
                allSuccess &= success;
            }

            if (File.Exists(outputFile))
            {
                var allLines = File.ReadAllLines(outputFile);
                foreach (var line in allLines)
                {
                    if (string.IsNullOrWhiteSpace(line) || result.Contains(line)) continue;
                    result.Add(line);
                }
            }

            return result;
        }

        /// <summary>
        /// 扫描代码中的国际化文本, 并将结果写入outputFile文件
        /// </summary>
        /// <param name="srcPath">cs代码文件夹或cs文件</param>
        /// <param name="functionName">要扫描的函数名</param>
        /// <param name="outputFile">结果输出到文件</param>
        /// <returns></returns>
        private static bool ScanLocalizationTextFromScript(string srcPath, string[] functionNames, string outputFile)
        {
            string scannerToolFile = UtilityBuiltin.ResPath.GetCombinePath(Directory.GetParent(Application.dataPath).FullName, scannerTool);

            StringBuilder strBuilder = new StringBuilder();
            strBuilder.Append($" {srcPath}");
            strBuilder.Append($" {outputFile}");
            foreach (var func in functionNames)
            {
                strBuilder.Append($" {func}");
            }
            var proceInfo = new System.Diagnostics.ProcessStartInfo(scannerToolFile, strBuilder.ToString());
            proceInfo.CreateNoWindow = true;
            proceInfo.UseShellExecute = false;
            proceInfo.Verb = "runas";
            proceInfo.WorkingDirectory = Directory.GetParent(Application.dataPath).FullName;
            bool success;
            using (var proce = System.Diagnostics.Process.Start(proceInfo))
            {
                proce.WaitForExit();
                success = proce.ExitCode == 0;
                if (!success)
                {
                    Debug.LogError($"扫描代码本地化文本失败! srcPath:{srcPath}, functions:{UtilityBuiltin.Json.ToJson(functionNames)}, outputFile:{outputFile}");
                }
                return success;
            }
        }


        /// <summary>
        /// 扫描Prefab中的国际化语言
        /// </summary>
        public static List<string> ScanLocalizationTextFromPrefab(Action<string, int, int> onProgressUpdate = null)
        {
            var assetGUIDs = AssetDatabase.FindAssets("t:Prefab", new string[] { ConstEditor.PrefabsPath });
            List<string> keyList = new List<string>();
            int totalCount = assetGUIDs.Length;
            for (int i = 0; i < totalCount; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(assetGUIDs[i]);
                var pfb = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                onProgressUpdate?.Invoke(path, totalCount, i);
                var keyArr = pfb.GetComponentsInChildren<UnityGameFramework.Runtime.UIStringKey>(true);
                foreach (var newKey in keyArr)
                {
                    if (string.IsNullOrWhiteSpace(newKey.Key) || keyList.Contains(newKey.Key)) continue;
                    keyList.Add(newKey.Key);
                }
            }
            return keyList;
        }
        /// <summary>
        /// 从DataTable Excel文件扫描本地化文本
        /// </summary>
        /// <param name="onProgressUpdate"></param>
        /// <returns></returns>
        public static List<string> ScanLocalizationTextFromDataTables(Action<string, int, int> onProgressUpdate = null)
        {
            List<string> keyList = new List<string>();
            var appConfig = AppConfigs.GetInstanceEditor();
            var mainTbFullFiles = GameDataGenerator.GameDataExcelRelative2FullPath(GameDataType.DataTable, appConfig.DataTables);
            var tbFullFiles = GameDataGenerator.GetGameDataExcelWithABFiles(GameDataType.DataTable, mainTbFullFiles);//同时扫描AB测试表
            for (int i = 0; i < tbFullFiles.Length; i++)
            {
                var excelFile = tbFullFiles[i];
                var fileInfo = new FileInfo(excelFile);
                if (!fileInfo.Exists) continue;

                onProgressUpdate?.Invoke(excelFile, tbFullFiles.Length, i);
                string tmpExcelFile = UtilityBuiltin.ResPath.GetCombinePath(fileInfo.Directory.FullName, GameFramework.Utility.Text.Format("{0}.temp", fileInfo.Name));
                try
                {
                    File.Copy(excelFile, tmpExcelFile, true);
                    using (var excelPackage = new ExcelPackage(tmpExcelFile))
                    {
                        var excelSheet = excelPackage.Workbook.Worksheets.FirstOrDefault();
                        if (excelSheet.Dimension.End.Row >= 1)
                        {
                            for (int colIndex = excelSheet.Dimension.Start.Column; colIndex <= excelSheet.Dimension.End.Column; colIndex++)
                            {
                                if (excelSheet.GetValue<string>(1, colIndex)?.ToLower() != EXCEL_I18N_TAG)
                                {
                                    continue;
                                }
                                for (int rowIndex = 5; rowIndex <= excelSheet.Dimension.End.Row; rowIndex++)
                                {
                                    string langKey = excelSheet.GetValue<string>(rowIndex, colIndex);
                                    if (string.IsNullOrWhiteSpace(langKey) || keyList.Contains(langKey)) continue;
                                    keyList.Add(langKey);
                                }
                            }

                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"扫描数据表本地化文本失败!文件:{excelFile}, Error:{e.Message}");
                }

                if (File.Exists(tmpExcelFile))
                {
                    File.Delete(tmpExcelFile);
                }
            }
            return keyList;
        }
        /// <summary>
        /// 加载语言的所有本地化数据
        /// </summary>
        /// <param name="language"></param>
        /// <param name="localizationTexts"></param>
        internal static void LoadLanguageExcelTexts(Language language, ref List<LocalizationText> localizationTexts)
        {
            if (language == Language.Unspecified)
            {
                return;
            }
            var lanExcelFile = GetLanguageExcelFileName(language);
            LoadLanguageExcelTexts(lanExcelFile, ref localizationTexts);
        }
        /// <summary>
        /// 加载语言的所有本地化数据
        /// </summary>
        /// <param name="language"></param>
        /// <param name="localizationTexts"></param>
        internal static void LoadLanguageExcelTexts(string lanExcelFile, ref List<LocalizationText> localizationTexts)
        {
            localizationTexts.Clear();
            if (!File.Exists(lanExcelFile)) return;
            try
            {
                var fileInfo = new FileInfo(lanExcelFile);
                string tmpExcelFile = UtilityBuiltin.ResPath.GetCombinePath(fileInfo.Directory.FullName, GameFramework.Utility.Text.Format("{0}.temp", fileInfo.Name));
                File.Copy(lanExcelFile, tmpExcelFile, true);

                using (var excelPackage = new ExcelPackage(tmpExcelFile))
                {
                    if (excelPackage.Workbook.Worksheets.Count > 0)
                    {
                        var excelSheet = excelPackage.Workbook.Worksheets.FirstOrDefault();
                        for (int rowIndex = excelSheet.Dimension.Start.Row; rowIndex <= excelSheet.Dimension.End.Row; rowIndex++)
                        {
                            var key = excelSheet.GetValue<string>(rowIndex, KEY_COL);
                            var value = excelSheet.GetValue<string>(rowIndex, VALUE_COL);
                            if (string.IsNullOrWhiteSpace(key))
                            {
                                continue;
                            }

                            var langTxt = new LocalizationText()
                            {
                                Key = key,
                                Value = value
                            };
                            var cell = excelSheet.Cells[rowIndex, LOCK_COL];
                            var checkBox = excelSheet.Drawings[cell.Address] as ExcelControlCheckBox;
                            langTxt.Locked = checkBox != null && checkBox.Checked == eCheckState.Checked;
                            localizationTexts.Add(langTxt);
                        }
                    }
                    excelPackage.Dispose();
                }
                if (File.Exists(tmpExcelFile))
                {
                    File.Delete(tmpExcelFile);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"读取语言本地化文本列表失败:{e.Message}");
            }
        }

        /// <summary>
        /// 获取语言对应的Excel文件名
        /// </summary>
        /// <param name="language"></param>
        /// <returns></returns>
        public static string GetLanguageExcelFileName(GameFramework.Localization.Language language)
        {
            var lanExcelFile = UtilityBuiltin.ResPath.GetCombinePath(ConstEditor.LanguageExcelPath, GameFramework.Utility.Text.Format("{0}.xlsx", language.ToString()));
            return lanExcelFile;
        }
        /// <summary>
        /// 翻译所有多语言Excel
        /// </summary>
        /// <param name="forceAll">是否强制翻译所有行, 默认只翻译空白行</param>
        public static void TranslateAllLanguages(bool forceAll = false, Action<string, int, int> onProgressUpdate = null)
        {
            if (EditorToolSettings.Instance.LanguagesSupport == null || EditorToolSettings.Instance.LanguagesSupport.Count < 2) return;

            var mainLanguage = (Language)EditorToolSettings.Instance.LanguagesSupport[0];//母语
            var mainLangTexts = new List<LocalizationText>();
            LoadLanguageExcelTexts(mainLanguage, ref mainLangTexts);
            mainLangTexts.RemoveAll(tmpItm => string.IsNullOrWhiteSpace(tmpItm.Value));//移除空白行
            int totalCount = EditorToolSettings.Instance.LanguagesSupport.Count;
            onProgressUpdate?.Invoke($"翻译多语言Excel", totalCount, 0);
            for (int i = 1; i < totalCount; i++)
            {
                var lang = (Language)EditorToolSettings.Instance.LanguagesSupport[i];
                var langTexts = new List<LocalizationText>();
                LoadLanguageExcelTexts(lang, ref langTexts);
                TranslateAndSave(mainLangTexts, mainLanguage, langTexts, lang, forceAll);
                onProgressUpdate?.Invoke($"翻译多语言:{lang}", totalCount, i);
            }
        }

        private static void TranslateAndSave(List<LocalizationText> mainLangTexts, Language srcLang, List<LocalizationText> langTexts, Language targetLang, bool forceAll)
        {
            int curTransIdx = 0;
            while (curTransIdx < langTexts.Count)
            {
                string totalText = "";
                List<int> totalTextIdx = new List<int>();
                for (; curTransIdx < langTexts.Count; curTransIdx++)
                {
                    var text = langTexts[curTransIdx];
                    string srcText = "";
                    if (forceAll)
                    {
                        var mainText = mainLangTexts.FirstOrDefault(tmpItm => tmpItm.Key.CompareTo(text.Key) == 0);
                        if (mainText != null && !string.IsNullOrWhiteSpace(mainText.Value))
                        {
                            srcText = mainText.Value;
                        }
                    }
                    else
                    {
                        if (string.IsNullOrWhiteSpace(text.Value))
                        {
                            var mainText = mainLangTexts.FirstOrDefault(tmpItm => tmpItm.Key.CompareTo(text.Key) == 0);
                            if (mainText != null && !string.IsNullOrWhiteSpace(mainText.Value))
                            {
                                srcText = mainText.Value;
                            }
                        }
                    }
                    if (!string.IsNullOrWhiteSpace(srcText))
                    {
                        if ((totalText.Length + srcText.Length) > EditorToolSettings.Instance.BaiduTransMaxLength)
                        {
                            curTransIdx -= 1; //如果长度超了下个请求接着这行
                            break;
                        }
                        totalText += srcText + TRANS_SPLIT_TAG;
                        totalTextIdx.Add(curTransIdx);
                    }
                }
                if (string.IsNullOrWhiteSpace(totalText))
                {
                    curTransIdx++;//如果一行字数就超过上限则跳过翻译这行
                    continue;
                }
                totalText = totalText.Substring(0, totalText.Length - TRANS_SPLIT_TAG.Length);//去掉结分隔符
                TMP_EditorCoroutine.StartCoroutine(TranslateCoroutine(totalText, srcLang, targetLang, (success, trans, userDt) =>
                {
                    if (success)
                    {
                        ParseAndSaveTransResults(langTexts, targetLang, trans, userDt as int[]);
                    }
                }, totalTextIdx.ToArray()));
            }
        }
        /// <summary>
        /// 解析翻译结果并保存到语言Excel
        /// </summary>
        /// <param name="targetTexts"></param>
        /// <param name="targetLang"></param>
        /// <param name="resultStr"></param>
        /// <param name="resultTextIdxArr"></param>
        private static void ParseAndSaveTransResults(List<LocalizationText> targetTexts, Language targetLang, TranslationResult trans, int[] resultTextIdxArr)
        {
            if (string.IsNullOrWhiteSpace(trans.dst) || resultTextIdxArr == null) return;
            var srcTexts = trans.src.Split(TRANS_SPLIT_TAG);
            var resultTexts = trans.dst.Split(TRANS_SPLIT_TAG);
            if (resultTexts.Length != resultTextIdxArr.Length || resultTexts.Length != srcTexts.Length)
            {
                Debug.LogError($"翻译失败, 翻译结果数量和索引数不一致.result count:{resultTexts.Length}, but index count:{resultTextIdxArr.Length}\n 翻译结果:{trans.dst}");
                return;
            }
            for (int i = 0; i < resultTextIdxArr.Length; i++)
            {
                var idx = resultTextIdxArr[i];
                var srcStr = srcTexts[i];
                var dstStr = resultTexts[i].Trim();
                int leadingSpaces = srcStr.Length - srcStr.TrimStart().Length;
                int trailingSpaces = srcStr.Length - srcStr.TrimEnd().Length;

                dstStr = dstStr.PadLeft(dstStr.Length + leadingSpaces);
                dstStr = dstStr.PadRight(dstStr.Length + trailingSpaces);
                targetTexts[idx].Value = dstStr;
            }

            SaveLanguage(targetLang, targetTexts);
        }
        private static IEnumerator TranslateCoroutine(string srcText, Language srcLang, Language targetLang, Action<bool, TranslationResult, object> onComplete, object userData)
        {
            var randomCode = System.DateTime.Now.Ticks.ToString();

            var strBuilder = new StringBuilder();
            strBuilder.Append(BAIDU_TRANS_URL);
            strBuilder.AppendFormat("q={0}", UnityWebRequest.EscapeURL(srcText));
            strBuilder.AppendFormat("&from={0}", GetBaiduLanguage(srcLang) ?? "auto"); //自动识别源文字语言
            strBuilder.AppendFormat("&to={0}", GetBaiduLanguage(targetLang));//翻译到目标语言
            strBuilder.AppendFormat("&appid={0}", EditorToolSettings.Instance.BaiduTransAppId);
            strBuilder.AppendFormat("&salt={0}", randomCode);
            strBuilder.AppendFormat("&sign={0}", GenerateBaiduSign(srcText, randomCode));

            //Debug.Log($"发送:{strBuilder}");
            // 发送请求
            using (var webRequest = UnityEngine.Networking.UnityWebRequest.Get(strBuilder.ToString()))
            {
                webRequest.SetRequestHeader("Content-Type", "text/html;charset=UTF-8");
                webRequest.certificateHandler = new WebRequestCertNoValidate();
                webRequest.SendWebRequest();
                while (!webRequest.isDone) yield return null;

                if (webRequest.result != UnityEngine.Networking.UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"---------翻译{targetLang}请求失败:{webRequest.error}---------");
                    onComplete?.Invoke(false, null, userData);
                }
                else
                {
                    var json = webRequest.downloadHandler.text;
                    //Debug.Log($"接收:{json}");
                    try
                    {
                        var responseJson = UtilityBuiltin.Json.ToObject<JObject>(json);
                        if (responseJson.ContainsKey("trans_result"))
                        {
                            var resultArray = responseJson["trans_result"].ToObject<TranslationResult[]>();
                            if (resultArray != null && resultArray.Length > 0)
                            {
                                var resultTrans = resultArray[0];
                                onComplete?.Invoke(true, resultTrans, userData);
                            }
                            else
                            {
                                Debug.LogError($"---------翻译{targetLang}失败:{responseJson}---------");
                                onComplete?.Invoke(false, null, userData);
                            }
                        }
                        else
                        {
                            Debug.LogError($"---------翻译{targetLang}失败:{responseJson}---------");
                            onComplete?.Invoke(false, null, userData);
                        }
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError($"---------翻译{targetLang}返回数据解析失败:{e.Message}---------");
                        onComplete?.Invoke(false, null, userData);
                    }
                }
            }

        }

        /// <summary>
        /// 生成百度翻译请求签名
        /// </summary>
        /// <param name="srcText"></param>
        /// <returns></returns>
        private static string GenerateBaiduSign(string srcText, string randomCode)
        {
            MD5 md5 = MD5.Create();
            var fullStr = GameFramework.Utility.Text.Format("{0}{1}{2}{3}", EditorToolSettings.Instance.BaiduTransAppId, srcText, randomCode, EditorToolSettings.Instance.BaiduTransSecretKey);
            byte[] byteOld = Encoding.UTF8.GetBytes(fullStr);
            byte[] byteNew = md5.ComputeHash(byteOld);
            StringBuilder sb = new StringBuilder();
            foreach (byte b in byteNew)
            {
                sb.Append(b.ToString("x2"));
            }
            return sb.ToString();
        }
        /// <summary>
        /// 根据语言类型返回对应的百度语言缩写
        /// </summary>
        /// <param name="lang"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public static string GetBaiduLanguage(Language lang)
        {
            switch (lang)
            {
                case Language.Afrikaans:
                    return "afr";
                case Language.Albanian:
                    return "alb";
                case Language.Arabic:
                    return "ara";
                case Language.Basque:
                    return "baq";
                case Language.Belarusian:
                    return "bel";
                case Language.Bulgarian:
                    return "bul";
                case Language.Catalan:
                    return "cat";
                case Language.ChineseSimplified:
                    return "zh";
                case Language.ChineseTraditional:
                    return "cht";
                case Language.Croatian:
                    return "hrv";
                case Language.Czech:
                    return "cs";
                case Language.Danish:
                    return "dan";
                case Language.Dutch:
                    return "nl";
                case Language.English:
                    return "en";
                case Language.Estonian:
                    return "est";
                case Language.Faroese:
                    return "fao";
                case Language.Finnish:
                    return "fin";
                case Language.French:
                    return "fra";
                case Language.Georgian:
                    return "geo";
                case Language.German:
                    return "de";
                case Language.Greek:
                    return "el";
                case Language.Hebrew:
                    return "heb";
                case Language.Hungarian:
                    return "hu";
                case Language.Icelandic:
                    return "ice";
                case Language.Indonesian:
                    return "id";
                case Language.Italian:
                    return "it";
                case Language.Japanese:
                    return "jp";
                case Language.Korean:
                    return "kor";
                case Language.Latvian:
                    return "lav";
                case Language.Lithuanian:
                    return "lit";
                case Language.Macedonian:
                    return "mac";
                case Language.Malayalam:
                    return "may";
                case Language.Norwegian:
                    return "nor";
                case Language.Persian:
                    return "per";
                case Language.Polish:
                    return "pl";
                case Language.PortugueseBrazil:
                    return "pt";
                case Language.PortuguesePortugal:
                    return "pt";
                case Language.Romanian:
                    return "rom";
                case Language.Russian:
                    return "ru";
                case Language.SerboCroatian:
                    return "sec";
                case Language.SerbianCyrillic:
                    return "src";
                case Language.SerbianLatin:
                    return "srp";
                case Language.Slovak:
                    return "sk";
                case Language.Slovenian:
                    return "slo";
                case Language.Spanish:
                    return "spa";
                case Language.Swedish:
                    return "swe";
                case Language.Thai:
                    return "th";
                case Language.Turkish:
                    return "tr";
                case Language.Ukrainian:
                    return "ukr";
                case Language.Vietnamese:
                    return "vie";
                default:
                    throw new NotSupportedException($"暂不支持该语言:{lang}");
            }
        }
    }
    internal class TranslationResult
    {
        public string src;
        public string dst;
    }
}
