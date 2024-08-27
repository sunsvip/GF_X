using UnityEngine;
using UnityEditor;
using GameFramework.Localization;
using System;
using UnityEditorInternal;
using System.Linq;
using System.Collections.Generic;

namespace UGF.EditorTools
{
    [EditorToolMenu("资源/语言国际化扫描工具", null, 3)]
    public class LocalizationTextScannerEditor : EditorToolBase
    {
        Vector2 scrollViewPos;
        public override string ToolName => "语言国际化工具";
        public override Vector2Int WinSize => new Vector2Int(600, 800);

        private Language[] languageAllOptions;
        private bool settingFoldout = true;
        ReorderableList langScrollView;
        Vector2 langScrollViewPos;
        private Texture mainLanguageIcon;
        private const string langScrollViewTitle = "多语言列表:";

        List<LocalizationText> localizationTexts = new List<LocalizationText>();

        private GUIContent lockedContent;
        private GUIContent scanBtContent;
        private GUIContent transBtContent;
        private GUIContent saveBtContent;
        private GUIStyle dropDownBtStyle;
        private GUIContent transAllBtContent;

        private void OnEnable()
        {
            transAllBtContent = new GUIContent("强制翻译全部(包括非空白行)");
            var dropDownToggleButton = EditorGUIUtility.GetBuiltinSkin(EditorSkin.Inspector).FindStyle("DropDownToggleButton");
            dropDownBtStyle = new GUIStyle(dropDownToggleButton);
            dropDownBtStyle.normal.textColor = Color.white;
            dropDownBtStyle.alignment = TextAnchor.MiddleCenter;
            dropDownBtStyle.hover.textColor = Color.white;
            dropDownBtStyle.active.textColor = Color.white;
            scanBtContent = new GUIContent("扫描多语言文本", "从资源/数据表/代码中扫描多语言文本");
            transBtContent = new GUIContent("一键翻译", "翻译多语言(空白行),并把结果保存到多语言Excel文件");
            saveBtContent = new GUIContent("保存多语言", "把扫描结果保存到多语言Excel文件, 并导出多语言json");
            lockedContent = EditorGUIUtility.TrIconContent("LockIcon-On", "勾选锁住,将强制保留此行");
            languageAllOptions = Enum.GetValues(typeof(GameFramework.Localization.Language)) as Language[];
            ArrayUtility.RemoveAt(ref languageAllOptions, 0);

            mainLanguageIcon = EditorGUIUtility.TrIconContent("Favorite@2x").image;
            langScrollView = new ReorderableList(EditorToolSettings.Instance.LanguagesSupport, typeof(int), true, true, true, true);
            langScrollView.drawHeaderCallback = DrawLanguageScrollViewHeader;
            langScrollView.onAddCallback = OnLangScrollViewAddBtClick;
            langScrollView.onRemoveCallback = OnLangScrollViewRemoveBtClick;
            langScrollView.drawElementCallback = DrawSupportLanguages;
            langScrollView.multiSelect = true;

            if (localizationTexts == null) localizationTexts = new List<LocalizationText>();
            else
                localizationTexts.Clear();

            InitLanguageTextsFromMain();
        }


        private void OnGUI()
        {
            EditorGUILayout.BeginVertical();
            if (EditorToolSettings.Instance.LanguagesSupport == null || EditorToolSettings.Instance.LanguagesSupport.Count < 1)
            {
                EditorGUILayout.HelpBox("多语言列表为空, 请在下方设置中添加语言", MessageType.Error);
            }

            DrawLocalizationTexts();
            if (settingFoldout = EditorGUILayout.Foldout(settingFoldout, "展开设置项:"))
            {
                DrawSettingsPanel();
            }
            EditorGUILayout.BeginHorizontal("box");
            {
                var btHeight = GUILayout.Height(30);
                if (GUILayout.Button(scanBtContent, btHeight))
                {
                    ScanAllLocalizationText();
                }
                if (GUILayout.Button(saveBtContent, btHeight))
                {
                    SaveAllLocalizationText();
                }
                DrawTranslateButton(btHeight);
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawSettingsPanel()
        {
            EditorGUILayout.BeginVertical("box");
            {
                EditorGUILayout.BeginHorizontal();
                {
                    EditorGUILayout.LabelField("百度翻译设置:");
                    if (EditorGUILayout.LinkButton("获取百度翻译API Key"))
                    {
                        Application.OpenURL("https://fanyi-api.baidu.com/api/trans/product/desktop");
                    }
                    EditorGUILayout.EndHorizontal();
                }

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                {
                    EditorGUILayout.Space(4);
                    var titleWidth = GUILayout.Width(70);
                    EditorGUILayout.BeginHorizontal();
                    {
                        EditorGUILayout.LabelField("APP ID:", titleWidth);
                        EditorToolSettings.Instance.BaiduTransAppId = EditorGUILayout.PasswordField(EditorToolSettings.Instance.BaiduTransAppId);
                        EditorGUILayout.EndHorizontal();
                    }
                    EditorGUILayout.BeginHorizontal();
                    {
                        EditorGUILayout.LabelField("密钥:", titleWidth);
                        EditorToolSettings.Instance.BaiduTransSecretKey = EditorGUILayout.PasswordField(EditorToolSettings.Instance.BaiduTransSecretKey);
                        EditorGUILayout.EndHorizontal();
                    }
                    EditorGUILayout.BeginHorizontal();
                    {
                        EditorGUILayout.LabelField("字节限长:", titleWidth);
                        EditorToolSettings.Instance.BaiduTransMaxLength = EditorGUILayout.IntSlider(EditorToolSettings.Instance.BaiduTransMaxLength, LocalizationTextScanner.MinLength, LocalizationTextScanner.MaxLength);
                        EditorGUILayout.EndHorizontal();
                    }
                    EditorGUILayout.Space(4);
                    EditorGUILayout.EndVertical();
                }
                EditorGUILayout.Space(5);
                langScrollViewPos = EditorGUILayout.BeginScrollView(langScrollViewPos, GUILayout.MaxHeight(200));
                {
                    langScrollView.DoLayoutList();
                    EditorGUILayout.EndScrollView();
                }
                EditorGUILayout.EndVertical();
            }
        }

        private void OnDisable()
        {
            EditorToolSettings.Save();
        }
        private void DrawTranslateButton(GUILayoutOption btHeight)
        {
            Rect buildRect = GUILayoutUtility.GetRect(transBtContent, dropDownBtStyle,
                        btHeight);
            Rect buildRectPopupButton = buildRect;
            buildRectPopupButton.x += buildRect.width - 35;
            buildRectPopupButton.width = 35;

            if (EditorGUI.DropdownButton(buildRectPopupButton, GUIContent.none, FocusType.Passive,
                GUIStyle.none))
            {
                GenericMenu menu = new GenericMenu();
                menu.AddItem(transAllBtContent, false,
                    () =>
                    {
                        TranslateLocalizationTexts(true);
                    });
                menu.DropDown(buildRect);
            }
            else if (GUI.Button(buildRect, transBtContent, dropDownBtStyle))
            {
                if (EditorUtility.DisplayDialog("多语言翻译", "确认开始一键翻译?", "是", "否"))
                {
                    TranslateLocalizationTexts(false);
                }
            }
        }
        private void InitLanguageTextsFromMain()
        {
            if (EditorToolSettings.Instance.LanguagesSupport.Count > 0)
            {
                var mainLang = (Language)EditorToolSettings.Instance.LanguagesSupport[0];
                EditorUtility.DisplayProgressBar("加载中...", $"初始化本地化文本列表:{mainLang}", 0.5f);
                LocalizationTextScanner.LoadLanguageExcelTexts(mainLang, ref localizationTexts);
                EditorUtility.ClearProgressBar();
            }
        }
        /// <summary>
        /// 展示本地化文本列表
        /// </summary>
        private void DrawLocalizationTexts()
        {
            EditorGUILayout.BeginVertical("box");
            {
                scrollViewPos = EditorGUILayout.BeginScrollView(scrollViewPos);
                {
                    for (int i = 0; i < localizationTexts.Count; i++)
                    {
                        var lanText = localizationTexts[i];
                        EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                        {
                            EditorGUILayout.LabelField(i.ToString(), GUILayout.Width(50));
                            lanText.Locked = EditorGUILayout.ToggleLeft(lockedContent, lanText.Locked, GUILayout.Width(40));
                            lanText.Key = EditorGUILayout.TextField(lanText.Key);
                            GUILayout.Space(5);
                            lanText.Value = EditorGUILayout.TextField(lanText.Value);
                            EditorGUILayout.EndHorizontal();
                        }
                    }
                    EditorGUILayout.EndScrollView();
                }
                EditorGUILayout.EndVertical();
            }
        }


        private void OnLangScrollViewRemoveBtClick(ReorderableList list)
        {
            for (int i = list.selectedIndices.Count - 1; i >= 0; i--)
            {
                RemoveLanguage(list.selectedIndices[i]);
            }
        }

        private void DrawSupportLanguages(Rect rect, int index, bool isActive, bool isFocused)
        {
            if (index == 0)
            {
                var mainLangRect = rect;
                mainLangRect.x += 5;
                mainLangRect.width = mainLangRect.height = EditorGUIUtility.singleLineHeight;
                GUI.DrawTexture(mainLangRect, mainLanguageIcon);
            }
            float mainLangIconWidth = EditorGUIUtility.singleLineHeight + 10;
            rect.x += mainLangIconWidth;
            rect.width -= mainLangIconWidth;
            var item = EditorToolSettings.Instance.LanguagesSupport[index];

            EditorGUI.LabelField(rect, ((Language)item).ToString());
        }

        private void OnLangScrollViewAddBtClick(ReorderableList list)
        {
            var unselectLanguagess = languageAllOptions.Where(lan => !EditorToolSettings.Instance.LanguagesSupport.Contains((int)lan));
            var popLanguages = new GenericMenu();
            foreach (var item in unselectLanguagess)
            {
                int curLang = (int)item;
                popLanguages.AddItem(new GUIContent(item.ToString()), false, () =>
                {
                    AddLanguage(curLang);
                });
            }
            popLanguages.ShowAsContext();
        }

        private void DrawLanguageScrollViewHeader(Rect rect)
        {
            EditorGUI.LabelField(rect, langScrollViewTitle);
        }

        /// <summary>
        /// 翻译
        /// </summary>
        private void TranslateLocalizationTexts(bool forceAll)
        {
            LocalizationTextScanner.TranslateAllLanguages(forceAll, (msg, total, curIdx) =>
            {
                EditorUtility.DisplayProgressBar($"翻译进度:{curIdx}/{total}", msg, curIdx / (float)total);
            });
            EditorUtility.ClearProgressBar();
        }
        /// <summary>
        /// 保存Excel并导出json
        /// </summary>
        private void SaveAllLocalizationText()
        {
            if (localizationTexts.Count < 1)
            {
                return;
            }
            LocalizationTextScanner.Save2LanguagesExcel(localizationTexts, (str, total, cur) =>
            {
                EditorUtility.DisplayProgressBar($"进度({cur}/{total})", $"保存语言Excel: {str}", cur / (float)total);
            });
            EditorUtility.ClearProgressBar();
        }

        /// <summary>
        /// 扫描全部国际化语言Key
        /// </summary>
        void ScanAllLocalizationText()
        {
            try
            {
                if (EditorToolSettings.Instance.LanguagesSupport != null && EditorToolSettings.Instance.LanguagesSupport.Count > 0)
                {
                    var mainLanguage = (Language)EditorToolSettings.Instance.LanguagesSupport[0];
                    if (mainLanguage != Language.Unspecified)
                        LocalizationTextScanner.LoadLanguageExcelTexts(mainLanguage, ref localizationTexts);
                }
                var textList = LocalizationTextScanner.ScanAllLocalizationText((dealFileName, totalCount, dealIdx) =>
                {
                    EditorUtility.DisplayProgressBar($"扫描进度:({dealIdx}/{totalCount})", dealFileName, dealIdx / (float)totalCount);
                });
                LocalizationTextScanner.MergeTexts(textList, ref localizationTexts);
            }
            catch (Exception exp)
            {
                Debug.LogError($"扫描全部本地化本文报错:{exp.Message}");
            }

            EditorUtility.ClearProgressBar();
        }
        void AddLanguage(int languageEnum)
        {
            if (!EditorToolSettings.Instance.LanguagesSupport.Contains(languageEnum))
            {
                EditorToolSettings.Instance.LanguagesSupport.Add(languageEnum);
            }
        }
        void RemoveLanguage(int idx)
        {
            var count = EditorToolSettings.Instance.LanguagesSupport.Count;
            if (idx >= 0 && idx < count)
            {
                EditorToolSettings.Instance.LanguagesSupport.RemoveAt(idx);
            }
        }
    }
}

