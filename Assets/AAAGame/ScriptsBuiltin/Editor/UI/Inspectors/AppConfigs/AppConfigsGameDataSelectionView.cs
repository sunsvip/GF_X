using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace UGF.EditorTools
{
    internal sealed class AppConfigsGameDataSelectionView
    {
        private readonly AppConfigs _appConfig;
        private readonly GUIStyle _normalStyle;
        private readonly GUIStyle _selectedStyle;
        private readonly GUIContent _titleContent;
        private string _newExcelName;

        internal bool Foldout = true;
        internal Vector2 ScrollPos;
        internal GameDataType ConfigType { get; }
        internal string ExcelDir { get; }
        internal string ExcelOutputDir { get; }
        internal List<AppConfigsSelectableItem> ExcelItems { get; } = new List<AppConfigsSelectableItem>();

        internal AppConfigsGameDataSelectionView(AppConfigs config, GameDataType configType)
        {
            _appConfig = config;
            ConfigType = configType;
            ExcelDir = GameDataGenerator.GetGameDataExcelDir(configType);
            ExcelOutputDir = GameDataGenerator.GetGameDataExcelOutputDir(configType);

            _normalStyle = new GUIStyle
            {
                normal = { textColor = Color.white }
            };
            _selectedStyle = new GUIStyle
            {
                normal = { textColor = ColorUtility.TryParseHtmlString("#2BD988", out var textColor) ? textColor : Color.green }
            };

            _titleContent = new GUIContent(configType.ToString())
            {
                tooltip = configType switch
                {
                    GameDataType.DataTable => "选择项目需要用到的数据表",
                    GameDataType.Config => "选择项目需要用到的常量配置表",
                    GameDataType.Language => "选择项目需要用到的多语言表",
                    _ => string.Empty,
                }
            };
        }

        internal void Reload()
        {
            ExcelItems.Clear();
            if (!Directory.Exists(ExcelDir) || _appConfig == null)
            {
                return;
            }

            string[] selectedItems = GetGameDataList();
            if (selectedItems == null)
            {
                return;
            }

            IList<string> mainExcels = GameDataGenerator.GetAllGameDataExcels(ConfigType, GameDataExcelFileType.MainFile);
            foreach (string mainExcelFile in mainExcels)
            {
                string mainExcelRelativePath = GameDataGenerator.GetGameDataExcelRelativePath(ConfigType, mainExcelFile);
                if (ConfigType == GameDataType.DataTable && ArrayUtility.Contains(ConstEditor.FrameworkRequiredDataTables, mainExcelRelativePath))
                {
                    continue;
                }

                bool isOn = ArrayUtility.Contains(selectedItems, mainExcelRelativePath);
                ExcelItems.Add(new AppConfigsSelectableItem(isOn, mainExcelRelativePath));
            }
        }

        internal string[] GetSelectedItems()
        {
            AppConfigsSelectableItem[] selectedList = ExcelItems.Where(item => item.IsOn).ToArray();
            string[] result = new string[selectedList.Length];
            for (int i = 0; i < selectedList.Length; i++)
            {
                result[i] = selectedList[i].Name;
            }

            return result;
        }

        internal bool DrawPanel(GUILayoutOption perItemWidth)
        {
            bool dataChanged = false;
            string dataTypeName = ConfigType.ToString();
            Foldout = EditorGUILayout.Foldout(Foldout, _titleContent);
            if (!Foldout)
            {
                return false;
            }

            EditorGUILayout.BeginVertical();
            {
                ScrollPos = EditorGUILayout.BeginScrollView(ScrollPos, "box", GUILayout.MaxHeight(200));
                {
                    EditorGUI.BeginChangeCheck();
                    for (int i = 0; i < ExcelItems.Count; i++)
                    {
                        if (i % AppConfigsInspector.ONE_LINE_SHOW_COUNT == 0)
                        {
                            EditorGUILayout.BeginHorizontal();
                        }

                        AppConfigsSelectableItem item = ExcelItems[i];
                        item.IsOn = EditorGUILayout.ToggleLeft(item.Name, item.IsOn, item.IsOn ? _selectedStyle : _normalStyle, perItemWidth);
                        if (i % AppConfigsInspector.ONE_LINE_SHOW_COUNT == AppConfigsInspector.ONE_LINE_SHOW_COUNT - 1)
                        {
                            EditorGUILayout.EndHorizontal();
                        }
                    }

                    if (EditorGUI.EndChangeCheck())
                    {
                        dataChanged = true;
                    }

                    if (ExcelItems.Count % AppConfigsInspector.ONE_LINE_SHOW_COUNT != 0)
                    {
                        EditorGUILayout.EndHorizontal();
                    }
                }
                EditorGUILayout.EndScrollView();

                EditorGUILayout.BeginHorizontal("box");
                {
                    if (GUILayout.Button("All", GUILayout.Width(50)))
                    {
                        SetSelectAll(true);
                        dataChanged = true;
                    }

                    if (GUILayout.Button("None", GUILayout.Width(50)))
                    {
                        SetSelectAll(false);
                        dataChanged = true;
                    }

                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("Reveal", GUILayout.Width(70)))
                    {
                        EditorUtility.RevealInFinder(ExcelDir);
                        GUIUtility.ExitGUI();
                    }

                    if (GUILayout.Button("Export", GUILayout.Width(70)))
                    {
                        AppConfigsGameDataActions.ExportSelection(ConfigType, GetSelectedItems());
                    }
                }
                EditorGUILayout.EndHorizontal();

                if (ConfigType == GameDataType.DataTable || ConfigType == GameDataType.Config)
                {
                    EditorGUILayout.BeginHorizontal("box");
                    {
                        _newExcelName = EditorGUILayout.TextField(_newExcelName);
                        if (GUILayout.Button($"New {dataTypeName}", GUILayout.Width(100)))
                        {
                            CreateExcel(_newExcelName);
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }
            EditorGUILayout.EndVertical();
            return dataChanged;
        }

        private void SetSelectAll(bool value)
        {
            foreach (AppConfigsSelectableItem item in ExcelItems)
            {
                item.IsOn = value;
            }
        }

        private string[] GetGameDataList()
        {
            return ConfigType switch
            {
                GameDataType.DataTable => _appConfig.DataTables,
                GameDataType.Config => _appConfig.Configs,
                GameDataType.Language => _appConfig.Languages,
                _ => null,
            };
        }

        private void CreateExcel(string newExcelName)
        {
            if (!AppConfigsGameDataActions.TryCreateExcel(ConfigType, ExcelDir, newExcelName, out string excelPath))
            {
                return;
            }

            Reload();
            EditorUtility.RevealInFinder(excelPath);
            GUIUtility.ExitGUI();
        }
    }
}
