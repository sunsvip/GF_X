using GameFramework;
using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace UGF.EditorTools
{
    [EditorToolMenu("艺术字工具", typeof(BatchOperateToolEditor), 1)]
    public class ImageFontCreator : UtilitySubToolBase
    {
        private enum ImageFontType
        {
            Font,
            TextMeshProFont
        }

        private const string DefaultCharsFile = "";
        private const string CharsFileKey = "ImageFontCreator.CharsFilePath";
        private const string AiProviderKey = "ImageFontCreator.AiProvider";
        private const string AiStyleRequirementKey = "ImageFontCreator.AiStyleRequirement";
        private const string AiTargetDirectoryKey = "ImageFontCreator.AiTargetDirectory";
        private const string AiShowDebugCommandWindowKey = "ImageFontCreator.AiShowDebugCommandWindow";
        private const string FontSaveDirectoryKey = "ImageFontCreator.FontSaveDirectory";

        private readonly List<int> _cachedUnicodes = new List<int>();
        private readonly ImageFontSpritePreviewState _previewState = new ImageFontSpritePreviewState();

        private string _charsString;
        private string _charsFilePath;
        private string _aiStyleRequirement;
        private string _aiTargetDirectory;
        private int _fontSize;
        private ImageFontType _fontType = ImageFontType.TextMeshProFont;
        private AiCliProvider _aiProvider = AiCliProvider.CodexCli;
        private bool _normalizeHeight = true;
        private bool _aiSettingsFoldout = true;
        private bool _aiShowDebugCommandWindow;
        private Font _tmpBaseFont;

        public override string AssetSelectorTypeFilter => "t:Texture2D";

        public override string DragAreaTips => "拖拽到此添加艺术字Sprite图集\nSpriteMode必须为Multiple";
        public override AssetSelectionScope SelectionScope => AssetSelectionScope.FilesOnly;
        public override int MaxSelectedObjectCount => 1;

        protected override Type[] SupportAssetTypes => new[] { typeof(Texture2D) };

        public override void OnEnter()
        {
            base.OnEnter();
            _charsFilePath = EditorPrefs.GetString(CharsFileKey, DefaultCharsFile);
            _aiProvider = (AiCliProvider)Mathf.Clamp(EditorPrefs.GetInt(AiProviderKey, (int)AiCliProvider.CodexCli), (int)AiCliProvider.CodexCli, (int)AiCliProvider.OpenCodeCli);
            _aiStyleRequirement = EditorPrefs.GetString(AiStyleRequirementKey, string.Empty);
            _aiTargetDirectory = EditorPrefs.GetString(AiTargetDirectoryKey, ImageFontAiGenerateService.DefaultTargetDirectory);
            _aiShowDebugCommandWindow = EditorPrefs.GetBool(AiShowDebugCommandWindowKey, false);
            _fontSize = 24;
            _tmpBaseFont ??= GetDefaultTmpBaseFont();
            RefreshCharsUnicodes();
        }

        public override void OnExit()
        {
            base.OnExit();
            SaveAiSettings();
            _previewState.TextureInstanceId = 0;
            _previewState.SpriteRects = null;
        }

        public override void DrawBottomButtonsPanel()
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("一键切图", GUILayout.Height(30)))
            {
                SliceSelectedSpriteAtlas();
            }

            if (GUILayout.Button("生成字体", GUILayout.Height(30)))
            {
                GenerateCustomFont();
            }
            EditorGUILayout.EndHorizontal();
        }

        public override void DrawSettingsPanel()
        {
            DrawCharacterFileSelector();

            _normalizeHeight = EditorGUILayout.Toggle("统一字符高度:", _normalizeHeight);
            _fontType = (ImageFontType)EditorGUILayout.EnumPopup("字体类型:", _fontType);
            if (_fontType == ImageFontType.TextMeshProFont)
            {
                _tmpBaseFont = EditorGUILayout.ObjectField("Base Font:", _tmpBaseFont, typeof(Font), false) as Font;
            }
            else
            {
                _fontSize = EditorGUILayout.IntSlider("字体大小:", _fontSize, 1, 512);
            }

            EditorGUILayout.LabelField("追加字符:");
            EditorGUI.BeginChangeCheck();
            _charsString = EditorGUILayout.TextArea(_charsString, GUILayout.Height(50));
            if (EditorGUI.EndChangeCheck())
            {
                RefreshCharsUnicodes();
            }

            DrawAiGenerateSettings();
        }

        public override void DrawBeforeSettingsPanel()
        {
            var selectedTexture = GetSelectedTexture();
            if (selectedTexture != null)
            {
                ImageFontSpritePreviewDrawer.Draw(selectedTexture, _cachedUnicodes, _previewState);
            }
        }

        private void DrawCharacterFileSelector()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("字符文件(相对工程路径):", _charsFilePath, EditorStyles.selectionRect);
            if (GUILayout.Button("选择文件", GUILayout.Width(100)))
            {
                _charsFilePath = EditorDialogUtility.OpenRelativeFilePanel("选择字符文件", _charsFilePath, "txt");
                if (!string.IsNullOrWhiteSpace(_charsFilePath))
                {
                    EditorPrefs.SetString(CharsFileKey, _charsFilePath);
                    RefreshCharsUnicodes();
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawAiGenerateSettings()
        {
            EditorGUILayout.Space(8);
            _aiSettingsFoldout = EditorGUILayout.Foldout(_aiSettingsFoldout, "AI生成艺术字Sprite图集");
            if (!_aiSettingsFoldout)
            {
                return;
            }

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUI.BeginChangeCheck();
            _aiProvider = (AiCliProvider)EditorGUILayout.EnumPopup("CLI模式:", _aiProvider);
            EditorGUILayout.LabelField("艺术字样式需求:");
            _aiStyleRequirement = EditorGUILayout.TextArea(_aiStyleRequirement, GUILayout.Height(64));

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("图片生成目录:", GUILayout.Width(90));
            EditorGUILayout.SelectableLabel(_aiTargetDirectory, EditorStyles.textField, GUILayout.Height(EditorGUIUtility.singleLineHeight));
            if (GUILayout.Button("选择", GUILayout.Width(50)))
            {
                string selectedDirectory = EditorDialogUtility.OpenRelativeFolderPanel("选择艺术字图片生成目录", _aiTargetDirectory);
                if (!string.IsNullOrWhiteSpace(selectedDirectory))
                {
                    _aiTargetDirectory = selectedDirectory;
                }
            }
            EditorGUILayout.EndHorizontal();

            _aiShowDebugCommandWindow = EditorGUILayout.ToggleLeft("显示调试命令窗口", _aiShowDebugCommandWindow);
            if (EditorGUI.EndChangeCheck())
            {
                SaveAiSettings();
            }

            DrawAiGenerateStatus();
            DrawAiGenerateButtons();
            EditorGUILayout.EndVertical();
        }

        private void DrawAiGenerateStatus()
        {
            var status = ImageFontAiGenerateService.GetStatusSnapshot();
            if (status == null)
            {
                return;
            }

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("AI生成状态", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Provider", status.Provider.ToString());
            EditorGUILayout.LabelField("阶段", GetAiRunStateLabel(status.State));
            Rect progressRect = GUILayoutUtility.GetRect(18f, 18f, GUILayout.ExpandWidth(true));
            EditorGUI.ProgressBar(progressRect, Mathf.Clamp01(status.Progress01), BuildAiProgressText(status));
            if (!string.IsNullOrWhiteSpace(status.Message))
            {
                EditorGUILayout.HelpBox(status.Message, status.State == AiCliTaskState.Failed ? MessageType.Error : MessageType.Info);
            }

            if (!string.IsNullOrWhiteSpace(status.Detail))
            {
                EditorGUILayout.LabelField("详情", status.Detail, EditorStyles.wordWrappedLabel);
            }

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

            if (status.IsRunning)
            {
                OwnerEditor?.Repaint();
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawAiGenerateButtons()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginDisabledGroup(ImageFontAiGenerateService.IsRunning);
            if (GUILayout.Button("开始AI生成图片", GUILayout.Height(26)))
            {
                StartAiGenerateAtlas();
            }
            EditorGUI.EndDisabledGroup();

            EditorGUI.BeginDisabledGroup(!ImageFontAiGenerateService.IsRunning);
            if (GUILayout.Button("取消生成", GUILayout.Height(26), GUILayout.Width(90)))
            {
                ImageFontAiGenerateService.CancelCurrentTask();
                OwnerEditor?.Repaint();
            }
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();
        }

        private void StartAiGenerateAtlas()
        {
            RefreshCharsUnicodes();
            if (_cachedUnicodes.Count < 1)
            {
                EditorUtility.DisplayDialog("AI生成艺术字失败", "请先指定字符或字符文件。", "OK");
                return;
            }

            if (string.IsNullOrWhiteSpace(_aiStyleRequirement))
            {
                EditorUtility.DisplayDialog("AI生成艺术字失败", "请填写艺术字样式需求。", "OK");
                return;
            }

            SaveAiSettings();
            bool started = ImageFontAiGenerateService.GenerateAtlas(
                _aiProvider,
                new List<int>(_cachedUnicodes),
                _aiStyleRequirement,
                _aiTargetDirectory,
                _aiShowDebugCommandWindow,
                SelectedObjects,
                () =>
                {
                    _previewState.TextureInstanceId = 0;
                    _previewState.SpriteRects = null;
                    OwnerEditor?.Repaint();
                });
            if (!started)
            {
                OwnerEditor?.Repaint();
            }
        }

        private void SaveAiSettings()
        {
            EditorPrefs.SetInt(AiProviderKey, (int)_aiProvider);
            EditorPrefs.SetString(AiStyleRequirementKey, _aiStyleRequirement ?? string.Empty);
            EditorPrefs.SetString(AiTargetDirectoryKey, string.IsNullOrWhiteSpace(_aiTargetDirectory) ? ImageFontAiGenerateService.DefaultTargetDirectory : _aiTargetDirectory);
            EditorPrefs.SetBool(AiShowDebugCommandWindowKey, _aiShowDebugCommandWindow);
        }

        private static string BuildAiProgressText(AiCliTaskStatusSnapshot status)
        {
            if (status == null)
            {
                return "待命";
            }

            if (status.TotalUnits > 0)
            {
                return $"{status.CompletedUnits}/{status.TotalUnits}";
            }

            return GetAiRunStateLabel(status.State);
        }

        private static string GetAiRunStateLabel(AiCliTaskState state)
        {
            switch (state)
            {
                case AiCliTaskState.Preparing:
                    return "准备中";
                case AiCliTaskState.Running:
                    return "执行中";
                case AiCliTaskState.Validating:
                    return "校验中";
                case AiCliTaskState.Applying:
                    return "应用中";
                case AiCliTaskState.Completed:
                    return "已完成";
                case AiCliTaskState.Failed:
                    return "失败";
                default:
                    return "待命";
            }
        }

        private void RefreshCharsUnicodes()
        {
            _cachedUnicodes.Clear();
            var chars = string.Empty;
            if (File.Exists(_charsFilePath))
            {
                chars = File.ReadAllText(_charsFilePath, System.Text.Encoding.UTF8);
            }

            if (!string.IsNullOrEmpty(_charsString))
            {
                chars = Utility.Text.Format("{0}{1}", chars, _charsString);
            }

            for (var i = 0; i < chars.Length; i++)
            {
                if (char.IsHighSurrogate(chars, i) && i + 1 < chars.Length && char.IsLowSurrogate(chars, i + 1))
                {
                    _cachedUnicodes.Add(char.ConvertToUtf32(chars[i], chars[i + 1]));
                    i++;
                }
                else
                {
                    _cachedUnicodes.Add(chars[i]);
                }
            }
        }

        private void GenerateCustomFont()
        {
            var selectedTexture = GetSelectedTexture();
            if (selectedTexture == null)
            {
                return;
            }

            RefreshCharsUnicodes();
            if (_cachedUnicodes.Count < 1)
            {
                Debug.LogWarning("生成艺术字失败: 请先指定字符或字符文件");
                return;
            }

            selectedTexture = ImageFontBuildService.PrepareTextureForImageFont(selectedTexture);
            if (selectedTexture == null)
            {
                return;
            }

            _previewState.TextureInstanceId = 0;
            _previewState.SpriteRects = null;
            ImageFontSpritePreviewDrawer.EnsureSpriteRects(selectedTexture, _previewState);
            if (!ImageFontBuildService.TryCreateCharacterInfo(
                    _cachedUnicodes,
                    selectedTexture,
                    _previewState.SpriteRects,
                    _normalizeHeight,
                    _fontSize,
                    _fontType == ImageFontType.TextMeshProFont,
                    out var characterInfos,
                    out var maxFontHeight))
            {
                return;
            }

            if (_fontType == ImageFontType.TextMeshProFont && _tmpBaseFont == null)
            {
                _tmpBaseFont = GetDefaultTmpBaseFont();
                if (_tmpBaseFont == null)
                {
                    Debug.LogWarning("生成艺术字失败: 未找到 Unity 默认 Base Font");
                    return;
                }
            }

            string outputFontPath = SaveFontPath(selectedTexture);
            if (string.IsNullOrWhiteSpace(outputFontPath))
            {
                return;
            }

            switch (_fontType)
            {
                case ImageFontType.Font:
                    ImageFontBuildService.SelectAsset(ImageFontBuildService.BuildLegacyFont(characterInfos, selectedTexture, outputFontPath));
                    break;
                case ImageFontType.TextMeshProFont:
                    ImageFontBuildService.SelectAsset(ImageFontBuildService.BuildTextMeshProFont(_tmpBaseFont, characterInfos, selectedTexture, outputFontPath, maxFontHeight));
                    break;
            }
        }

        private string SaveFontPath(Texture2D texture)
        {
            string extension = _fontType == ImageFontType.TextMeshProFont ? "asset" : "fontsettings";
            string outputFontPath = EditorUtility.SaveFilePanelInProject(
                "保存艺术字字体",
                $"{texture.name}_{_fontSize}",
                extension,
                "选择艺术字字体保存位置。",
                GetLastFontSaveDirectory());
            if (string.IsNullOrWhiteSpace(outputFontPath))
            {
                return null;
            }

            string directory = Path.GetDirectoryName(outputFontPath);
            EditorPrefs.SetString(FontSaveDirectoryKey, directory.Replace('\\', '/'));
            return outputFontPath;
        }

        private static string GetLastFontSaveDirectory()
        {
            string directory = EditorPrefs.GetString(FontSaveDirectoryKey, "Assets");
            return AssetDatabase.IsValidFolder(directory) ? directory : "Assets";
        }

        private void SliceSelectedSpriteAtlas()
        {
            var selectedTexture = GetSelectedTexture();
            if (selectedTexture == null)
            {
                Debug.LogWarning("艺术字 Sprite Slice 失败: 请先拖入或添加 Sprite 图集");
                return;
            }

            RefreshCharsUnicodes();
            if (_cachedUnicodes.Count < 1)
            {
                Debug.LogWarning("艺术字 Sprite Slice 失败: 请先指定字符或字符文件");
                return;
            }

            if (ImageFontAiGenerateService.SliceAtlas(selectedTexture, _cachedUnicodes))
            {
                _previewState.TextureInstanceId = 0;
                _previewState.SpriteRects = null;
                OwnerEditor?.Repaint();
            }
        }

        private Texture2D GetSelectedTexture()
        {
            if (SelectedObjects.Count == 0)
            {
                return null;
            }

            return SelectedObjects[0] as Texture2D;
        }

        private static Font GetDefaultTmpBaseFont()
        {
            return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }
    }
}
