#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using GameFramework;
using GameFramework.Procedure;
using UnityEditorInternal;
using System;

namespace UGF.EditorTools
{
    [Flags]
    public enum GameDataType
    {
        DataTable = 1 << 0,
        Config = 1 << 1,
        Language = 1 << 2
    }
    [CustomEditor(typeof(AppConfigs))]
    public class AppConfigsInspector : UnityEditor.Editor
    {
        private class ItemData
        {
            public bool isOn;
            public string excelName { get; private set; }

            public ItemData(bool isOn, string dllName)
            {
                this.isOn = isOn;
                this.excelName = dllName;
            }
        }
        private class GameDataScrollView
        {
            public bool foldout = true;
            public GameDataType CfgType { get; private set; }
            public Vector2 scrollPos;//记录滚动列表位置
            public string excelDir;
            public string excelOuputDir;
            private string newExcelName;
            private AppConfigs appConfig;
            public List<ItemData> ExcelItems { get; private set; }
            private GUIStyle normalStyle;
            private GUIStyle selectedStyle;
            GUIContent titleContent;
            public GameDataScrollView(AppConfigs cfg, GameDataType configTp)
            {
                normalStyle = new GUIStyle();
                normalStyle.normal.textColor = Color.white;
                selectedStyle = new GUIStyle();
                selectedStyle.normal.textColor = ColorUtility.TryParseHtmlString("#2BD988", out var textCol) ? textCol : Color.green;
                this.CfgType = configTp;
                this.excelDir = GameDataGenerator.GetGameDataExcelDir(configTp);
                this.excelOuputDir = GameDataGenerator.GetGameDataExcelOutputDir(configTp);
                this.appConfig = cfg;
                titleContent = new GUIContent(configTp.ToString());
                switch (configTp)
                {
                    case GameDataType.DataTable:
                        titleContent.tooltip = "选择项目需要用到的数据表";
                        break;
                    case GameDataType.Config:
                        titleContent.tooltip = "选择项目需要用到的常量配置表";
                        break;
                    case GameDataType.Language:
                        titleContent.tooltip = "选择项目需要用到的多语言表";
                        break;
                    default:
                        break;
                }
            }
            public void Reload()
            {
                if (!Directory.Exists(excelDir) || appConfig == null) return;

                var mainExcels = GameDataGenerator.GetAllGameDataExcels(this.CfgType, GameDataExcelFileType.MainFile);

                if (ExcelItems == null) ExcelItems = new List<ItemData>(); else ExcelItems.Clear();

                string[] desArr = GetGameDataList();
                if (desArr != null)
                {
                    foreach (var mainExcelFile in mainExcels)
                    {
                        var mainExcelRelativePath = GameDataGenerator.GetGameDataExcelRelativePath(this.CfgType, mainExcelFile);
                        var isOn = ArrayUtility.Contains(desArr, mainExcelRelativePath);
                        ExcelItems.Add(new ItemData(isOn, mainExcelRelativePath));
                    }
                }
            }
            public string[] GetSelectedItems()
            {
                var selectedList = ExcelItems.Where(dt => dt.isOn).ToArray();
                string[] resultArr = new string[selectedList.Length];
                for (int i = 0; i < selectedList.Length; i++)
                {
                    resultArr[i] = selectedList[i].excelName;
                }
                return resultArr;
            }

            internal void SetSelectAll(bool v)
            {
                foreach (var item in ExcelItems)
                {
                    item.isOn = v;
                }
            }

            internal bool DrawPanel()
            {
                bool dataChanged = false;
                var dataTypeStr = this.CfgType.ToString();
                this.foldout = EditorGUILayout.Foldout(this.foldout, titleContent);
                if (foldout)
                {
                    EditorGUILayout.BeginVertical();
                    {
                        scrollPos = EditorGUILayout.BeginScrollView(scrollPos, EditorStyles.textArea, GUILayout.MaxHeight(200));
                        {
                            EditorGUI.BeginChangeCheck();
                            foreach (var item in ExcelItems)
                            {
                                item.isOn = EditorGUILayout.ToggleLeft(item.excelName, item.isOn, item.isOn ? selectedStyle : normalStyle);
                            }
                            if (EditorGUI.EndChangeCheck())
                            {
                                dataChanged = true;
                            }
                            EditorGUILayout.EndScrollView();
                        }
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
                                EditorUtility.RevealInFinder(excelDir);
                                GUIUtility.ExitGUI();
                            }
                            if (GUILayout.Button("Export", GUILayout.Width(70)))
                            {
                                Export();
                            }
                            EditorGUILayout.EndHorizontal();
                        }
                        if (this.CfgType == GameDataType.DataTable || this.CfgType == GameDataType.Config)
                        {
                            EditorGUILayout.BeginHorizontal("box");
                            {
                                newExcelName = EditorGUILayout.TextField(newExcelName);
                                if (GUILayout.Button($"New {dataTypeStr}", GUILayout.Width(100)))
                                {
                                    CreateExcel(newExcelName);
                                }
                                EditorGUILayout.EndHorizontal();
                            }
                        }
                        EditorGUILayout.EndVertical();
                    }
                }
                return dataChanged;
            }
            private void Export()
            {
                switch (this.CfgType)
                {
                    case GameDataType.DataTable:
                        GameDataGenerator.RefreshAllDataTable(GameDataGenerator.GameDataExcelRelative2FullPath(CfgType, GetGameDataList()));
                        break;
                    case GameDataType.Config:
                        GameDataGenerator.RefreshAllConfig(GameDataGenerator.GameDataExcelRelative2FullPath(CfgType, GetGameDataList()));
                        break;
                    case GameDataType.Language:
                        GameDataGenerator.RefreshAllLanguage(GameDataGenerator.GameDataExcelRelative2FullPath(CfgType, GetGameDataList()));
                        break;
                }
            }
            private string[] GetGameDataList()
            {
                switch (this.CfgType)
                {
                    case GameDataType.DataTable:
                        return appConfig.DataTables;
                    case GameDataType.Config:
                        return appConfig.Configs;
                    case GameDataType.Language:
                        return appConfig.Languages;
                    default:
                        return null;
                }
            }
            private void CreateExcel(string newExcelName)
            {
                switch (this.CfgType)
                {
                    case GameDataType.DataTable:
                        CreateDataTableExcel(newExcelName);
                        break;
                    case GameDataType.Config:
                        CreateConfigExcel(newExcelName);
                        break;
                }
            }
            private void CreateDataTableExcel(string v)
            {
                if (string.IsNullOrWhiteSpace(v))
                {
                    return;
                }
                var excelPath = UtilityBuiltin.ResPath.GetCombinePath(excelDir, v + ".xlsx");
                if (File.Exists(excelPath))
                {
                    Debug.LogWarning($"创建DataTable失败, 文件已存在:{excelPath}");
                    return;
                }
                if (GameDataGenerator.CreateDataTableExcel(excelPath))
                {
                    Reload();
                    EditorUtility.RevealInFinder(excelPath);
                    GUIUtility.ExitGUI();
                }
            }
            private void CreateConfigExcel(string v)
            {
                if (string.IsNullOrWhiteSpace(v))
                {
                    return;
                }
                var excelPath = UtilityBuiltin.ResPath.GetCombinePath(excelDir, v + ".xlsx");
                if (File.Exists(excelPath))
                {
                    Debug.LogWarning($"创建Config失败, 文件已存在:{excelPath}");
                    return;
                }
                if (GameDataGenerator.CreateGameConfigExcel(excelPath))
                {
                    Reload();
                    EditorUtility.RevealInFinder(excelPath);
                    GUIUtility.ExitGUI();
                }
            }
        }
        AppConfigs appConfig;
        GameDataScrollView[] svDataArr;
        bool procedureFoldout = true;
        Vector2 procedureScrollPos;
        ItemData[] procedures;
        private GUIStyle normalStyle;
        private GUIStyle selectedStyle;
        GUIContent procedureTitleContent;
        GUIContent editorConstSettingsContent;
        private void OnEnable()
        {
            appConfig = target as AppConfigs;
            normalStyle = new GUIStyle();
            normalStyle.normal.textColor = Color.white;
            selectedStyle = new GUIStyle();
            selectedStyle.normal.textColor = ColorUtility.TryParseHtmlString("#2BD988", out var textCol) ? textCol : Color.green;

            procedureTitleContent = new GUIContent("流程(Procedures)", "勾选的流程在有限状态机中有效");
            editorConstSettingsContent = EditorGUIUtility.TrTextContentWithIcon("Path Settings [设置DataTable/Config导入/导出路径]", "Settings");
            svDataArr = new GameDataScrollView[] { new GameDataScrollView(appConfig, GameDataType.DataTable), new GameDataScrollView(appConfig, GameDataType.Config), new GameDataScrollView(appConfig, GameDataType.Language) };
            ReloadScrollView(appConfig);
        }

        public override void OnInspectorGUI()
        {
            //base.OnInspectorGUI();
            serializedObject.Update();
            if (GUILayout.Button(editorConstSettingsContent))
            {
                InternalEditorUtility.OpenFileAtLineExternal(Path.Combine(Path.GetDirectoryName(ConstEditor.BuiltinAssembly), "../ForEditor/Editor/Common/ConstEditor.cs"), 0);
            }
            procedureFoldout = EditorGUILayout.Foldout(procedureFoldout, procedureTitleContent);
            if (procedureFoldout)
            {
                EditorGUILayout.BeginVertical();
                {
                    procedureScrollPos = EditorGUILayout.BeginScrollView(procedureScrollPos, EditorStyles.textField, GUILayout.Height(200));
                    {
                        EditorGUI.BeginChangeCheck();
                        foreach (var item in procedures)
                        {
                            item.isOn = EditorGUILayout.ToggleLeft(item.excelName, item.isOn, item.isOn ? selectedStyle : normalStyle);
                        }
                        if (EditorGUI.EndChangeCheck())
                        {
                            SaveConfig(appConfig);
                        }
                        EditorGUILayout.EndScrollView();
                    }
                    EditorGUILayout.EndVertical();
                }
            }
            foreach (var item in svDataArr)
            {
                if (item.DrawPanel())
                {
                    SaveConfig(appConfig);
                }
            }
            EditorGUILayout.Space(10);
            EditorGUILayout.BeginHorizontal("box");
            {
                if (GUILayout.Button("Reload", GUILayout.Height(30)))
                {
                    ReloadScrollView(appConfig);
                }
                if (GUILayout.Button("Save", GUILayout.Height(30)))
                {
                    SaveConfig(appConfig);
                }
                EditorGUILayout.EndHorizontal();
            }
            serializedObject.ApplyModifiedProperties();
        }

        private void SaveConfig(AppConfigs cfg)
        {
            foreach (var svData in svDataArr)
            {
                if (svData.CfgType == GameDataType.DataTable)
                {
                    cfg.GetType().GetField("mDataTables", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(cfg, svData.GetSelectedItems());
                }
                else if (svData.CfgType == GameDataType.Config)
                {
                    cfg.GetType().GetField("mConfigs", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(cfg, svData.GetSelectedItems());
                }
                else if (svData.CfgType == GameDataType.Language)
                {
                    cfg.GetType().GetField("mLanguages", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(cfg, svData.GetSelectedItems());
                }
            }
            string[] selectedProcedures = new string[0];
            foreach (var item in procedures)
            {
                if (item.isOn)
                {
                    ArrayUtility.Add(ref selectedProcedures, item.excelName);
                }
            }
            cfg.GetType().GetField("mProcedures", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(cfg, selectedProcedures);
            EditorUtility.SetDirty(cfg);
        }
        private void ReloadScrollView(AppConfigs cfg)
        {
            foreach (var item in svDataArr)
            {
                item.Reload();
            }

            ReloadProcedures(cfg);
        }
        private void ReloadProcedures(AppConfigs cfg)
        {
            procedures ??= new ItemData[0];
            ArrayUtility.Clear(ref procedures);
            //#if !DISABLE_HYBRIDCLR
            var hotfixDlls = Utility.Assembly.GetAssemblies().Where(dll => HybridCLR.Editor.SettingsUtil.HotUpdateAssemblyNamesIncludePreserved.Contains(dll.GetName().Name)).ToArray();

            foreach (var item in hotfixDlls)
            {
                var proceClassArr = item.GetTypes().Where(tp => tp.BaseType == typeof(ProcedureBase)).ToArray();
                foreach (var proceClass in proceClassArr)
                {
                    var proceName = proceClass.FullName;
                    ArrayUtility.Add(ref procedures, new ItemData(cfg.Procedures.Contains(proceName), proceName));
                }
            }
            //#endif
        }
    }

}
#endif