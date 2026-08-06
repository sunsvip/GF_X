using System;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UGF.EditorTools
{
    /// <summary>
    /// 批量替换 UI 字体资源。源资源留空时，替换范围内的对应组件都会被命中。
    /// </summary>
    [EditorToolMenu("替换字体", typeof(BatchOperateToolEditor), 0)]
    public class FontReplaceTool : UtilitySubToolBase
    {
        private struct ReplaceStats
        {
            public int LegacyFontCount;
            public int TmpFontCount;
            public int TmpMaterialCount;
            public int TmpSpriteAssetCount;
            public int TmpStyleSheetCount;
            public int ChangedAssetCount;

            public bool HasChanges => LegacyFontCount + TmpFontCount + TmpMaterialCount + TmpSpriteAssetCount + TmpStyleSheetCount > 0;

            public void Add(in ReplaceStats other)
            {
                LegacyFontCount += other.LegacyFontCount;
                TmpFontCount += other.TmpFontCount;
                TmpMaterialCount += other.TmpMaterialCount;
                TmpSpriteAssetCount += other.TmpSpriteAssetCount;
                TmpStyleSheetCount += other.TmpStyleSheetCount;
                ChangedAssetCount += other.ChangedAssetCount;
            }

            public string ToSummary(string title)
            {
                return $"{title}：资源/场景 {ChangedAssetCount} 个，UGUI Text 字体 {LegacyFontCount} 处，TMP 字体 {TmpFontCount} 处，TMP 默认材质 {TmpMaterialCount} 处，Sprite Asset {TmpSpriteAssetCount} 处，Style Sheet {TmpStyleSheetCount} 处。";
            }
        }

        public override string AssetSelectorTypeFilter => "t:prefab t:scene t:folder";

        public override string DragAreaTips => "拖拽添加 Prefab、场景或文件夹";

        public override string ReadmeText => "资源列表支持 Prefab、场景和文件夹。关闭强行替换时按“源 → 目标”精确替换。";

        protected override Type[] SupportAssetTypes => new[] { typeof(GameObject), typeof(SceneAsset) };

        private Font _sourceTextFont;
        private Font _targetTextFont;
        private TMP_FontAsset _sourceTmpFont;
        private TMP_FontAsset _targetTmpFont;
        private TMP_SpriteAsset _sourceTmpSpriteAsset;
        private TMP_SpriteAsset _targetTmpSpriteAsset;
        private TMP_StyleSheet _sourceTmpStyleSheet;
        private TMP_StyleSheet _targetTmpStyleSheet;
        private bool _forceReplaceAll;
        private bool _includeWorldSpaceTmp;
        private bool _replaceTmpDefaultMaterial = true;
        private string _lastResult;

        public override bool IsSupportAsset(string assetPath)
        {
            return assetPath.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase) ||
                   assetPath.EndsWith(".unity", StringComparison.OrdinalIgnoreCase);
        }

        public override List<string> FilterSelectedAssets(List<UnityEngine.Object> selectedObjects)
        {
            var paths = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < selectedObjects.Count; i++)
            {
                var selectedObject = selectedObjects[i];
                if (selectedObject == null)
                {
                    continue;
                }

                string assetPath = AssetDatabase.GetAssetPath(selectedObject);
                if (AssetDatabase.IsValidFolder(assetPath))
                {
                    var guids = AssetDatabase.FindAssets("t:prefab t:scene", new[] { assetPath });
                    for (int guidIndex = 0; guidIndex < guids.Length; guidIndex++)
                    {
                        paths.Add(AssetDatabase.GUIDToAssetPath(guids[guidIndex]));
                    }
                }
                else if (IsSupportAsset(assetPath))
                {
                    paths.Add(assetPath);
                }
            }

            var result = new List<string>(paths);
            result.Sort(StringComparer.Ordinal);
            return result;
        }

        public override void DrawSettingsPanel()
        {
            _forceReplaceAll = EditorGUILayout.ToggleLeft("全量替换（不考虑源资源）", _forceReplaceAll);
            EditorGUILayout.HelpBox(
                _forceReplaceAll
                    ? "强行全量替换已开启：会忽略左侧源资源，将每个已填写的目标资源覆盖到范围内所有对应组件。"
                    : "精确替换：仅替换与左侧源资源匹配的组件；源资源留空时匹配全部。目标资源留空时不处理该项。",
                MessageType.Info);

            EditorGUILayout.LabelField("UGUI Text", EditorStyles.boldLabel);
            DrawObjectPair("Font Asset", ref _sourceTextFont, ref _targetTextFont, !_forceReplaceAll);

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("TextMeshPro", EditorStyles.boldLabel);
            DrawObjectPair("Font Asset", ref _sourceTmpFont, ref _targetTmpFont, !_forceReplaceAll);
            DrawObjectPair("Sprite Asset", ref _sourceTmpSpriteAsset, ref _targetTmpSpriteAsset, !_forceReplaceAll);
            DrawObjectPair("Style Sheet", ref _sourceTmpStyleSheet, ref _targetTmpStyleSheet, !_forceReplaceAll);
            _replaceTmpDefaultMaterial = EditorGUILayout.ToggleLeft("更换 TMP 字体时同步目标字体默认材质", _replaceTmpDefaultMaterial);
            _includeWorldSpaceTmp = EditorGUILayout.ToggleLeft("包含 3D TextMeshPro（默认仅 TextMeshProUGUI）", _includeWorldSpaceTmp);

            if (!string.IsNullOrEmpty(_lastResult))
            {
                EditorGUILayout.Space(6f);
                EditorGUILayout.HelpBox(_lastResult, MessageType.None);
            }
        }

        public override void DrawBottomButtonsPanel()
        {
            if (GUILayout.Button("替换资源列表", GUILayout.Height(30f)))
            {
                ExecuteAssetList();
            }
        }

        private static void DrawObjectPair<T>(string label, ref T source, ref T target, bool sourceEnabled) where T : UnityEngine.Object
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel(label);
            EditorGUI.BeginDisabledGroup(!sourceEnabled);
            GUILayout.Label("源", GUILayout.Width(15f));
            source = EditorGUILayout.ObjectField(source, typeof(T), false) as T;
            EditorGUI.EndDisabledGroup();
            GUILayout.Label("→", GUILayout.Width(25f));
            GUILayout.Label("目标", GUILayout.Width(30f));
            target = EditorGUILayout.ObjectField(target, typeof(T), false) as T;
            EditorGUILayout.EndHorizontal();
        }

        private void ExecuteAssetList()
        {
            if (!HasReplacementTarget())
            {
                EditorUtility.DisplayDialog("替换字体", "请至少指定一个目标资源。", "确定");
                return;
            }

            var assetPaths = GetSelectedAssets();
            if (assetPaths.Count == 0)
            {
                EditorUtility.DisplayDialog("替换字体", "请在资源列表中添加 Prefab、场景或文件夹。", "确定");
                return;
            }

            if (!EditorUtility.DisplayDialog("确认批量替换", $"将修改并保存 {assetPaths.Count} 个资源/场景。", "替换并保存", "取消"))
            {
                return;
            }

            var stats = new ReplaceStats();
            try
            {
                for (int i = 0; i < assetPaths.Count; i++)
                {
                    string assetPath = assetPaths[i];
                    EditorUtility.DisplayProgressBar(
                        "替换字体",
                        assetPath,
                        (i + 1f) / assetPaths.Count);

                    ReplaceStats assetStats;
                    if (assetPath.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
                    {
                        assetStats = ProcessPrefab(assetPath);
                    }
                    else
                    {
                        assetStats = ProcessScene(assetPath);
                    }

                    if (assetStats.HasChanges)
                    {
                        assetStats.ChangedAssetCount = 1;
                    }

                    stats.Add(assetStats);
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            _lastResult = stats.ToSummary("已替换并保存");
            Debug.Log($"[FontReplaceTool] {_lastResult}");
        }

        private ReplaceStats ProcessPrefab(string assetPath)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (prefab == null)
            {
                return default;
            }

            var stats = ProcessRoot(prefab);
            if (stats.HasChanges)
            {
                PrefabUtility.SavePrefabAsset(prefab);
            }

            return stats;
        }

        private ReplaceStats ProcessScene(string assetPath)
        {
            Scene scene = SceneManager.GetSceneByPath(assetPath);
            bool wasLoaded = scene.IsValid() && scene.isLoaded;
            if (!wasLoaded)
            {
                scene = EditorSceneManager.OpenScene(assetPath, OpenSceneMode.Additive);
            }

            try
            {
                var stats = new ReplaceStats();
                var roots = scene.GetRootGameObjects();
                for (int i = 0; i < roots.Length; i++)
                {
                    stats.Add(ProcessRoot(roots[i]));
                }

                if (stats.HasChanges)
                {
                    EditorSceneManager.SaveScene(scene);
                }

                return stats;
            }
            finally
            {
                if (!wasLoaded)
                {
                    EditorSceneManager.CloseScene(scene, true);
                }
            }
        }

        private ReplaceStats ProcessRoot(GameObject root)
        {
            var stats = new ReplaceStats();
            var texts = root.GetComponentsInChildren<UnityEngine.UI.Text>(true);
            for (int i = 0; i < texts.Length; i++)
            {
                var text = texts[i];
                ProcessLegacyText(text, ref stats);
            }

            var tmpTexts = root.GetComponentsInChildren<TMP_Text>(true);
            for (int i = 0; i < tmpTexts.Length; i++)
            {
                var text = tmpTexts[i];
                if (!_includeWorldSpaceTmp && !(text is TextMeshProUGUI))
                {
                    continue;
                }

                ProcessTmpText(text, ref stats);
            }

            return stats;
        }

        private void ProcessLegacyText(UnityEngine.UI.Text text, ref ReplaceStats stats)
        {
            if (_targetTextFont == null || text.font == _targetTextFont || (!_forceReplaceAll && _sourceTextFont != null && text.font != _sourceTextFont))
            {
                return;
            }

            stats.LegacyFontCount++;
            EditorUtility.SetDirty(text);
            text.font = _targetTextFont;
        }

        private void ProcessTmpText(TMP_Text text, ref ReplaceStats stats)
        {
            bool replaceFont = _targetTmpFont != null && text.font != _targetTmpFont && (_forceReplaceAll || _sourceTmpFont == null || text.font == _sourceTmpFont);
            bool replaceSpriteAsset = _targetTmpSpriteAsset != null && text.spriteAsset != _targetTmpSpriteAsset && (_forceReplaceAll || _sourceTmpSpriteAsset == null || text.spriteAsset == _sourceTmpSpriteAsset);
            bool replaceStyleSheet = _targetTmpStyleSheet != null && text.styleSheet != _targetTmpStyleSheet && (_forceReplaceAll || _sourceTmpStyleSheet == null || text.styleSheet == _sourceTmpStyleSheet);
            bool replaceMaterial = replaceFont && _replaceTmpDefaultMaterial && _targetTmpFont.material != null && text.fontSharedMaterial != _targetTmpFont.material;
            if (!replaceFont && !replaceSpriteAsset && !replaceStyleSheet && !replaceMaterial)
            {
                return;
            }

            if (replaceFont)
            {
                stats.TmpFontCount++;
            }

            if (replaceMaterial)
            {
                stats.TmpMaterialCount++;
            }

            if (replaceSpriteAsset)
            {
                stats.TmpSpriteAssetCount++;
            }

            if (replaceStyleSheet)
            {
                stats.TmpStyleSheetCount++;
            }

            EditorUtility.SetDirty(text);
            if (replaceFont)
            {
                text.font = _targetTmpFont;
            }

            if (replaceMaterial)
            {
                text.fontSharedMaterial = _targetTmpFont.material;
            }

            if (replaceSpriteAsset)
            {
                text.spriteAsset = _targetTmpSpriteAsset;
            }

            if (replaceStyleSheet)
            {
                text.styleSheet = _targetTmpStyleSheet;
            }
        }

        private bool HasReplacementTarget()
        {
            return _targetTextFont != null || _targetTmpFont != null || _targetTmpSpriteAsset != null || _targetTmpStyleSheet != null;
        }
    }
}
