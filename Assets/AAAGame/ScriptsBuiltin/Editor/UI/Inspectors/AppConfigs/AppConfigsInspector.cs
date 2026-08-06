#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using System.IO;
using System;

namespace UGF.EditorTools
{
    [Flags]
    public enum GameDataType
    {
        DataTable = 1,
        Config = 2,
        Language = 4
    }

    [CustomEditor(typeof(AppConfigs))]
    public class AppConfigsInspector : UnityEditor.Editor
    {
        public const int ONE_LINE_SHOW_COUNT = 3;
        private AppConfigs appConfig;
        private AppConfigsGameDataSelectionView[] svDataArr;
        private AppConfigsProcedureSelectionView procedureView;
        private GUIStyle normalStyle;
        private GUIStyle selectedStyle;
        private GUIContent editorConstSettingsContent;
        private GUIContent loadFromBytesContent;

        private void OnEnable()
        {
            appConfig = target as AppConfigs;
            normalStyle = new GUIStyle();
            normalStyle.normal.textColor = Color.white;
            selectedStyle = new GUIStyle();
            selectedStyle.normal.textColor = ColorUtility.TryParseHtmlString("#2BD988", out var textCol) ? textCol : Color.green;

            editorConstSettingsContent = EditorGUIUtility.TrTextContentWithIcon("Path Settings [设置DataTable/Config导入/导出路径]", "Settings");
            loadFromBytesContent = new GUIContent("Load from bytes(勾选:二进制模式; 不勾选:文本模式)", "数据表/配置表/多语言表使用二进制模式");
            svDataArr = new[]
            {
                new AppConfigsGameDataSelectionView(appConfig, GameDataType.DataTable),
                new AppConfigsGameDataSelectionView(appConfig, GameDataType.Config),
                new AppConfigsGameDataSelectionView(appConfig, GameDataType.Language),
            };
            procedureView = new AppConfigsProcedureSelectionView(normalStyle, selectedStyle);
            ReloadScrollView(appConfig);
        }

        private void OnDisable()
        {
            SaveConfig();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            if (GUILayout.Button(editorConstSettingsContent))
            {
                AppConfigsEditorNavigationService.OpenConstEditorScript();
            }

            EditorGUILayout.Space(10);
            var loadFromBytesProperty = serializedObject.FindProperty("m_LoadFromBytes");
            loadFromBytesProperty.boolValue = EditorGUILayout.ToggleLeft(loadFromBytesContent, loadFromBytesProperty.boolValue);
            DrawFrameworkRequiredTables();
            var perItemWidth = GUILayout.Width(Mathf.Max(EditorGUIUtility.currentViewWidth / ONE_LINE_SHOW_COUNT - 20, 100));
            if (procedureView.DrawPanel(perItemWidth))
            {
                SaveConfig();
            }

            foreach (var item in svDataArr)
            {
                if (item.DrawPanel(perItemWidth))
                {
                    SaveConfig();
                }
            }

            EditorGUILayout.Space(10);
            EditorGUILayout.BeginHorizontal("box");
            {
                if (GUILayout.Button("Validate Paths", GUILayout.Height(30)))
                {
                    AppConfigsGameDataActions.ValidatePaths();
                }
                if (GUILayout.Button("Export All", GUILayout.Height(30)))
                {
                    SaveConfig();
                    AppConfigsGameDataActions.ExportAll();
                }
                if (GUILayout.Button("Clean", GUILayout.Height(30)))
                {
                    AppConfigsGameDataActions.CleanGeneratedData();
                }
                if (GUILayout.Button("Reload", GUILayout.Height(30)))
                {
                    ReloadScrollView(appConfig);
                }
                if (GUILayout.Button("Save", GUILayout.Height(30)))
                {
                    SaveConfig();
                }
                EditorGUILayout.EndHorizontal();
            }
            serializedObject.ApplyModifiedProperties();
        }

        private void DrawFrameworkRequiredTables()
        {
            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Framework Required Tables", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical("box");
            foreach (string tableName in ConstEditor.FrameworkRequiredDataTables)
            {
                string excelPath = GameDataGenerator.GameDataExcelRelative2FullPath(GameDataType.DataTable, tableName);
                bool exists = File.Exists(excelPath);
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(tableName, exists ? selectedStyle : normalStyle);
                GUI.enabled = exists;
                if (GUILayout.Button("Open", GUILayout.Width(60)))
                {
                    InternalEditorUtility.OpenFileAtLineExternal(excelPath, 0);
                }
                if (GUILayout.Button("Export", GUILayout.Width(70)))
                {
                    AppConfigsGameDataActions.ExportFrameworkRequiredTable(tableName);
                }
                GUI.enabled = true;
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndVertical();
        }

        private void SaveConfig()
        {
            string[] dataTables = null;
            string[] configs = null;
            string[] languages = null;
            foreach (var svData in svDataArr)
            {
                switch (svData.ConfigType)
                {
                    case GameDataType.DataTable:
                        dataTables = svData.GetSelectedItems();
                        break;
                    case GameDataType.Config:
                        configs = svData.GetSelectedItems();
                        break;
                    case GameDataType.Language:
                        languages = svData.GetSelectedItems();
                        break;
                }
            }

            AppConfigsSaveService.Save(serializedObject, dataTables, configs, languages, procedureView.GetSelectedItems());
        }

        private void ReloadScrollView(AppConfigs cfg)
        {
            foreach (var item in svDataArr)
            {
                item.Reload();
            }

            procedureView.Reload(cfg);
        }
    }

}
#endif
