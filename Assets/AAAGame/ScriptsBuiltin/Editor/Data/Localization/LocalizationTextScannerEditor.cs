using GameFramework.Localization;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace UGF.EditorTools
{
    [EditorToolMenu("资源/语言国际化扫描工具", null, 3)]
    public class LocalizationTextScannerEditor : EditorToolBase
    {
        private List<LocalizationText> _localizationTexts = new List<LocalizationText>();
        private readonly LocalizationTextPagedListView _textListView = new LocalizationTextPagedListView();

        private GUIContent _scanButtonContent;
        private GUIContent _saveButtonContent;
        private GUIContent _translateButtonContent;
        private GUIContent _translateAllButtonContent;
        private GUIStyle _dropdownButtonStyle;
        private bool _settingFoldout = true;
        private Vector2 _languageListScrollPosition;
        private LocalizationSupportLanguageListView _supportLanguageListView;

        public override string ToolName => "语言国际化工具";

        public override Vector2Int WinSize => new Vector2Int(600, 800);

        protected override void OnEnable()
        {
            base.OnEnable();
            _translateAllButtonContent = new GUIContent("强制翻译全部(包括非空白行)");
            _scanButtonContent = new GUIContent("扫描多语言文本", "从资源/数据表/代码中扫描多语言文本");
            _translateButtonContent = new GUIContent("一键翻译", "翻译多语言(空白行),并把结果保存到多语言Excel文件");
            _saveButtonContent = new GUIContent("保存多语言", "把扫描结果保存到多语言Excel文件, 并导出多语言json");

            var dropdownToggleButton = EditorGUIUtility.GetBuiltinSkin(EditorSkin.Inspector).FindStyle("DropDownToggleButton");
            _dropdownButtonStyle = new GUIStyle(dropdownToggleButton)
            {
                alignment = TextAnchor.MiddleCenter
            };
            _dropdownButtonStyle.normal.textColor = Color.white;
            _dropdownButtonStyle.hover.textColor = Color.white;
            _dropdownButtonStyle.active.textColor = Color.white;

            _supportLanguageListView = new LocalizationSupportLanguageListView();
            _localizationTexts.Clear();
            _textListView.Reset();
            InitLanguageTextsFromMain();
        }

        private void OnDisable()
        {
            EditorToolSettings.Save();
        }

        private void OnInspectorUpdate()
        {
            if (LocalizationTextScanner.GetTranslationStatus().IsRunning)
            {
                Repaint();
            }
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginVertical();
            if (EditorToolSettings.Instance.LanguagesSupport == null || EditorToolSettings.Instance.LanguagesSupport.Count < 1)
            {
                EditorGUILayout.HelpBox("多语言列表为空, 请在下方设置中添加语言", MessageType.Error);
            }

            _textListView.Draw(_localizationTexts);
            if (_settingFoldout = EditorGUILayout.Foldout(_settingFoldout, "展开设置项:"))
            {
                DrawSettingsPanel();
            }

            DrawBottomButtons();
            EditorGUILayout.EndVertical();
        }

        private void DrawSettingsPanel()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUI.BeginChangeCheck();
            DrawTranslationModeSettings();
            EditorGUILayout.Space(5);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("翻译设置:");
            if (GetCurrentTranslationProvider() == LocalizationTranslationProvider.Baidu && EditorGUILayout.LinkButton("获取百度翻译API Key"))
            {
                Application.OpenURL("https://fanyi-api.baidu.com/api/trans/product/desktop");
            }

            EditorGUILayout.EndHorizontal();
            DrawProviderSettings();
            DrawTranslationStatusPanel();
            EditorGUILayout.Space(5);
            _supportLanguageListView.Draw(_languageListScrollPosition, out _languageListScrollPosition);
            if (EditorGUI.EndChangeCheck())
            {
                EditorToolSettings.Save();
            }
            EditorGUILayout.EndVertical();
        }

        private static void DrawTranslationModeSettings()
        {
            var provider = (LocalizationTranslationProvider)EditorToolSettings.Instance.LocalizationTranslationProvider;
            provider = (LocalizationTranslationProvider)EditorGUILayout.EnumPopup("翻译模式:", provider);
            EditorToolSettings.Instance.LocalizationTranslationProvider = (int)provider;
        }

        private static void DrawProviderSettings()
        {
            if (GetCurrentTranslationProvider() == LocalizationTranslationProvider.Baidu)
            {
                DrawBaiduTranslationSettings();
                return;
            }

            DrawAiTranslationSettings();
        }

        private static void DrawBaiduTranslationSettings()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.Space(4);
            var titleWidth = GUILayout.Width(70);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("APP ID:", titleWidth);
            EditorToolSettings.Instance.BaiduTransAppId = EditorGUILayout.PasswordField(EditorToolSettings.Instance.BaiduTransAppId);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("密钥:", titleWidth);
            EditorToolSettings.Instance.BaiduTransSecretKey = EditorGUILayout.PasswordField(EditorToolSettings.Instance.BaiduTransSecretKey);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("批次字节限长:", titleWidth);
            EditorToolSettings.Instance.BaiduTransMaxLength = EditorGUILayout.IntSlider(
                EditorToolSettings.Instance.BaiduTransMaxLength,
                LocalizationTextScanner.MinLength,
                LocalizationTextScanner.MaxLength);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4);
            EditorGUILayout.EndVertical();
        }

        private static void DrawAiTranslationSettings()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.Space(4);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Prompt模板:", GUILayout.Width(70));
            EditorGUILayout.SelectableLabel(EditorToolSettings.Instance.LocalizationAiPromptTemplatePath, EditorStyles.textField, GUILayout.Height(EditorGUIUtility.singleLineHeight));
            if (GUILayout.Button("选择", GUILayout.Width(50)))
            {
                string path = EditorDialogUtility.OpenRelativeFilePanel("选择 AI 翻译 Prompt 模板", EditorToolSettings.Instance.LocalizationAiPromptTemplatePath, "md");
                if (!string.IsNullOrWhiteSpace(path))
                {
                    EditorToolSettings.Instance.LocalizationAiPromptTemplatePath = path;
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorToolSettings.Instance.LocalizationAiShowDebugCommandWindow =
                EditorGUILayout.ToggleLeft("显示调试命令窗口", EditorToolSettings.Instance.LocalizationAiShowDebugCommandWindow);

            var status = LocalizationTextScanner.GetTranslationStatus();
            if (!string.IsNullOrWhiteSpace(status.WorkingDirectory))
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("打开AI工作目录", GUILayout.Width(120)))
                {
                    Directory.CreateDirectory(status.WorkingDirectory);
                    EditorUtility.RevealInFinder(status.WorkingDirectory);
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space(4);
            EditorGUILayout.EndVertical();
        }

        private static void DrawTranslationStatusPanel()
        {
            var status = LocalizationTextScanner.GetTranslationStatus();
            if (status == null)
            {
                return;
            }

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("翻译状态", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Provider", status.Provider.ToString());
            EditorGUILayout.LabelField("阶段", GetRunStateLabel(status.State));
            DrawTranslationProgressBar(status);
            if (status.IsRunning)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("取消翻译", GUILayout.Width(100f)))
                {
                    LocalizationAiTranslationService.CancelCurrentTask();
                }
                EditorGUILayout.EndHorizontal();
            }

            if (!string.IsNullOrWhiteSpace(status.Message))
            {
                EditorGUILayout.HelpBox(status.Message, status.State == LocalizationTranslationRunState.Failed ? MessageType.Error : MessageType.Info);
            }

            if (!string.IsNullOrWhiteSpace(status.Detail))
            {
                EditorGUILayout.LabelField("详情", status.Detail, EditorStyles.wordWrappedLabel);
            }
            EditorGUILayout.EndVertical();
        }

        private static void DrawTranslationProgressBar(LocalizationTranslationStatusSnapshot status)
        {
            float progress = status != null ? Mathf.Clamp01(status.Progress01) : 0f;
            string progressText = BuildProgressText(status);
            Rect rect = GUILayoutUtility.GetRect(18f, 18f, GUILayout.ExpandWidth(true));
            EditorGUI.ProgressBar(rect, progress, progressText);
            GUILayout.Space(2f);
        }

        private static string BuildProgressText(LocalizationTranslationStatusSnapshot status)
        {
            if (status == null)
            {
                return "待命";
            }

            if (status.TotalLanguages > 0)
            {
                return $"{status.CompletedLanguages}/{status.TotalLanguages} 语言";
            }

            return GetRunStateLabel(status.State);
        }

        private static string GetRunStateLabel(LocalizationTranslationRunState state)
        {
            switch (state)
            {
                case LocalizationTranslationRunState.Preparing:
                    return "准备中";
                case LocalizationTranslationRunState.Running:
                    return "执行中";
                case LocalizationTranslationRunState.Validating:
                    return "校验中";
                case LocalizationTranslationRunState.Syncing:
                    return "同步中";
                case LocalizationTranslationRunState.Completed:
                    return "已完成";
                case LocalizationTranslationRunState.Failed:
                    return "失败";
                default:
                    return "待命";
            }
        }

        private void DrawBottomButtons()
        {
            EditorGUILayout.BeginHorizontal("box");
            var buttonHeight = GUILayout.Height(30);
            if (GUILayout.Button(_scanButtonContent, buttonHeight))
            {
                ScanAllLocalizationText();
            }

            if (GUILayout.Button(_saveButtonContent, buttonHeight))
            {
                SaveAllLocalizationText();
            }

            DrawTranslateButton(buttonHeight);
            EditorGUILayout.EndHorizontal();
        }

        private void DrawTranslateButton(GUILayoutOption buttonHeight)
        {
            Rect buildRect = GUILayoutUtility.GetRect(_translateButtonContent, _dropdownButtonStyle, buttonHeight);
            Rect popupButtonRect = buildRect;
            popupButtonRect.x += buildRect.width - 35;
            popupButtonRect.width = 35;

            if (EditorGUI.DropdownButton(popupButtonRect, GUIContent.none, FocusType.Passive, GUIStyle.none))
            {
                GenericMenu menu = new GenericMenu();
                menu.AddItem(_translateAllButtonContent, false, () =>
                {
                    TranslateLocalizationTexts(true);
                });
                menu.DropDown(buildRect);
            }
            else if (GUI.Button(buildRect, _translateButtonContent, _dropdownButtonStyle))
            {
                if (EditorUtility.DisplayDialog("多语言翻译", "确认开始一键翻译?", "是", "否"))
                {
                    TranslateLocalizationTexts(false);
                }
            }
        }

        private void InitLanguageTextsFromMain()
        {
            if (EditorToolSettings.Instance.LanguagesSupport.Count <= 0)
            {
                return;
            }

            var mainLanguage = (Language)EditorToolSettings.Instance.LanguagesSupport[0];
            EditorUtility.DisplayProgressBar("加载中...", $"初始化本地化文本列表:{mainLanguage}", 0.5f);
            LocalizationTextScanner.LoadLanguageExcelTexts(mainLanguage, ref _localizationTexts);
            _textListView.Reset();
            EditorUtility.ClearProgressBar();
        }

        private void TranslateLocalizationTexts(bool forceAll)
        {
            bool started = LocalizationTextScanner.TranslateAllLanguages(
                forceAll,
                null,
                () =>
                {
                    Repaint();
                });
            if (!started)
            {
                Repaint();
            }
        }

        private void SaveAllLocalizationText()
        {
            if (_localizationTexts.Count < 1)
            {
                return;
            }

            try
            {
                LocalizationTextScanner.Save2LanguagesExcel(_localizationTexts, (languageName, total, current) =>
                {
                    EditorUtility.DisplayProgressBar($"进度({current}/{total})", $"保存语言Excel: {languageName}", current / (float)total);
                });
            }
            catch (Exception exception)
            {
                Debug.LogError($"保存多语言失败: {exception}");
                EditorUtility.DisplayDialog("保存多语言失败", exception.Message, "确定");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        private void ScanAllLocalizationText()
        {
            try
            {
                if (EditorToolSettings.Instance.LanguagesSupport != null && EditorToolSettings.Instance.LanguagesSupport.Count > 0)
                {
                    var mainLanguage = (Language)EditorToolSettings.Instance.LanguagesSupport[0];
                    if (mainLanguage != Language.Unspecified)
                    {
                        LocalizationTextScanner.LoadLanguageExcelTexts(mainLanguage, ref _localizationTexts);
                    }
                }

                var textList = LocalizationTextScanner.ScanAllLocalizationText((dealFileName, totalCount, dealIndex) =>
                {
                    EditorUtility.DisplayProgressBar($"扫描进度:({dealIndex}/{totalCount})", dealFileName, dealIndex / (float)totalCount);
                });
                LocalizationTextScanner.MergeTexts(textList, ref _localizationTexts);
                _textListView.Reset();
            }
            catch (Exception exception)
            {
                Debug.LogError($"扫描全部本地化本文报错:{exception.Message}");
            }

            EditorUtility.ClearProgressBar();
        }

        private static LocalizationTranslationProvider GetCurrentTranslationProvider()
        {
            return (LocalizationTranslationProvider)EditorToolSettings.Instance.LocalizationTranslationProvider;
        }
    }
}
