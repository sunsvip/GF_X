using UnityEditor;
using UnityEditorInternal;
using UnityGameFramework.Editor.ResourceTools;
using UnityEngine;

namespace UGF.EditorTools.Build.ResourceRules
{
    public partial class ResourceRuleEditor : EditorWindow
    {
        private void InitRuleListDrawer()
        {
            _ruleList = new ReorderableList(_configuration.rules, typeof(ResourceRule))
            {
                draggable = true,
                elementHeight = 22
            };
            _ruleList.drawElementCallback = OnListElementGUI;
            _ruleList.drawHeaderCallback = OnListHeaderGUI;
            _ruleList.onAddCallback = _ => Add();
        }

        private void OnListElementGUI(Rect rect, int index, bool isactive, bool isfocused)
        {
            if (index >= _configuration.rules.Count)
            {
                return;
            }

            const float gap = 5f;
            ResourceRule rule = _configuration.rules[index];
            rect.y++;

            Rect fieldRect = rect;
            fieldRect.width = 16;
            fieldRect.height = 18;
            rule.valid = EditorGUI.Toggle(fieldRect, rule.valid);

            fieldRect.xMin = fieldRect.xMax + gap;
            fieldRect.xMax += 225;
            float assetBundleNameLength = fieldRect.width;
            rule.name = EditorGUI.TextField(fieldRect, rule.name);

            fieldRect.xMin = fieldRect.xMax + gap;
            fieldRect.xMax = fieldRect.xMin + 100;
            rule.loadType = (LoadType)EditorGUI.EnumPopup(fieldRect, rule.loadType);

            fieldRect.xMin = fieldRect.xMax + gap + 15;
            fieldRect.xMax = fieldRect.xMin + 30;
            rule.packed = EditorGUI.Toggle(fieldRect, rule.packed);

            fieldRect.xMin = fieldRect.xMax + gap;
            fieldRect.xMax = fieldRect.xMin + 85;
            rule.fileSystem = EditorGUI.TextField(fieldRect, rule.fileSystem);

            fieldRect.xMin = fieldRect.xMax + gap;
            fieldRect.xMax = fieldRect.xMin + 85;
            rule.groups = EditorGUI.TextField(fieldRect, rule.groups);

            fieldRect.xMin = fieldRect.xMax + gap;
            fieldRect.xMax = fieldRect.xMin + 85;
            rule.variant = EditorGUI.TextField(fieldRect, rule.variant);
            if (!string.IsNullOrEmpty(rule.variant))
            {
                rule.variant = rule.variant.ToLowerInvariant();
            }

            fieldRect.xMin = fieldRect.xMax + gap;
            fieldRect.width = assetBundleNameLength - 15;
            GUI.enabled = false;
            rule.assetsDirectoryPath = EditorGUI.TextField(fieldRect, rule.assetsDirectoryPath);
            GUI.enabled = true;

            fieldRect.xMin = fieldRect.xMax + gap;
            fieldRect.width = 50;
            if (GUI.Button(fieldRect, "Select"))
            {
                var path = SelectFolder();
                if (path != null)
                {
                    rule.assetsDirectoryPath = path;
                }
            }

            fieldRect.xMin = fieldRect.xMax + gap;
            fieldRect.xMax = fieldRect.xMin + 85;
            rule.filterType = (ResourceFilterType)EditorGUI.EnumPopup(fieldRect, rule.filterType);

            fieldRect.xMin = fieldRect.xMax + gap;
            fieldRect.xMax = fieldRect.xMin + 100;
            rule.searchPatterns = EditorGUI.TextField(fieldRect, rule.searchPatterns);

            fieldRect.xMin = fieldRect.xMax + gap;
            fieldRect.xMax = rect.xMax;
            rule.excludeSearchPattern = EditorGUI.TextField(fieldRect, rule.excludeSearchPattern);
        }

        private void OnListHeaderGUI(Rect rect)
        {
            Rect rulesRect = new Rect(rect.x, rect.y, 100, rect.height);
            EditorGUI.LabelField(rulesRect, "Rules");
            Rect configLabelRect = new Rect(rect.x + rulesRect.width, rect.y, 90, rect.height);
            EditorGUI.LabelField(configLabelRect, "CurrentConfig:");
            Rect configPopupRect = new Rect(rect.x + rulesRect.width + configLabelRect.width, rect.y, 200, rect.height);
            _currentConfigIndex = EditorGUI.Popup(configPopupRect, _currentConfigIndex, _configNames);
            _currentConfigIndex = Mathf.Clamp(_currentConfigIndex, 0, _allConfigPaths.Count - 1);
            if (_allConfigPaths.Count > 0 && _currentConfigPath != _allConfigPaths[_currentConfigIndex])
            {
                _currentConfigPath = _allConfigPaths[_currentConfigIndex];
                _configuration = ResourceRuleEditorConfigRepository.LoadConfig(_currentConfigPath);
                _ruleList = null;
            }

            Rect reloadRect = new Rect(rect.width - 100, rect.y, 100, rect.height);
            if (GUI.Button(reloadRect, "Reload"))
            {
                Load();
            }
        }

        private void OnListElementLabelGUI()
        {
            Rect rect = new Rect();
            const float gap = 5f;
            GUI.enabled = false;

            Rect fieldRect = new Rect(0, 20, rect.width, rect.height)
            {
                width = 45,
                height = 18
            };
            EditorGUI.TextField(fieldRect, "Active");

            fieldRect.xMin = fieldRect.xMax + gap;
            fieldRect.xMax += 215;
            float assetBundleNameLength = fieldRect.width;
            EditorGUI.TextField(fieldRect, "Name");

            fieldRect.xMin = fieldRect.xMax + gap;
            fieldRect.xMax = fieldRect.xMin + 100;
            EditorGUI.TextField(fieldRect, "Load Type");

            fieldRect.xMin = fieldRect.xMax + gap;
            fieldRect.xMax = fieldRect.xMin + 50;
            EditorGUI.TextField(fieldRect, "Packed");

            fieldRect.xMin = fieldRect.xMax + gap;
            fieldRect.xMax = fieldRect.xMin + 85;
            EditorGUI.TextField(fieldRect, "File System");

            fieldRect.xMin = fieldRect.xMax + gap;
            fieldRect.xMax = fieldRect.xMin + 85;
            EditorGUI.TextField(fieldRect, "Groups");

            fieldRect.xMin = fieldRect.xMax + gap;
            fieldRect.xMax = fieldRect.xMin + 85;
            EditorGUI.TextField(fieldRect, "Variant");

            fieldRect.xMin = fieldRect.xMax + gap;
            fieldRect.width = assetBundleNameLength + 50;
            EditorGUI.TextField(fieldRect, "AssetDirectory");

            fieldRect.xMin = fieldRect.xMax + gap;
            fieldRect.xMax = fieldRect.xMin + 85;
            EditorGUI.TextField(fieldRect, "Filter Type");

            fieldRect.xMin = fieldRect.xMax + gap;
            fieldRect.xMax = fieldRect.xMin + 100;
            EditorGUI.TextField(fieldRect, "Patterns");

            fieldRect.xMin = fieldRect.xMax + gap;
            fieldRect.xMax = fieldRect.xMin + 100;
            EditorGUI.TextField(fieldRect, "ExcludeRegexPatterns");
            GUI.enabled = true;
        }
    }
}

