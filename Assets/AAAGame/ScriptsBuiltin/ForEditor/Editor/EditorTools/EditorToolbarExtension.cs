using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using GameFramework;
using System.Linq;
using System.Reflection;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Unity.CodeEditor;

namespace UGF.EditorTools
{
    public class EditorToolbarExtension
    {
        private static GUIContent switchSceneBtContent;
        private static GUIContent buildBtContent;
        private static GUIContent appConfigBtContent;
        private static GUIContent toolsDropBtContent;
        private static GUIContent openCsProjectBtContent;

        //Toolbar栏工具箱下拉列表
        private static List<Type> editorToolList;
        private static List<string> sceneAssetList;
        [InitializeOnLoadMethod]
        static void Init()
        {
            editorToolList = new List<Type>();
            sceneAssetList = new List<string>();
            var curPlatformIcon = Utility.Assembly.GetType("UnityEditor.Networking.PlayerConnection.ConnectionUIHelper").GetMethod("GetIcon", BindingFlags.Static | BindingFlags.Public).Invoke(null, new object[] { EditorUserBuildSettings.activeBuildTarget.ToString() }) as GUIContent;
            var curOpenSceneName = EditorSceneManager.GetActiveScene().name;
            switchSceneBtContent = EditorGUIUtility.TrTextContentWithIcon(string.IsNullOrEmpty(curOpenSceneName) ? "Switch Scene" : curOpenSceneName, "切换场景", "UnityLogo");

            buildBtContent = EditorGUIUtility.TrTextContentWithIcon("Build App/Hotfix", "打新包/打热更", curPlatformIcon.image);
            appConfigBtContent = EditorGUIUtility.TrTextContentWithIcon("App Configs", "配置App运行时所需DataTable/Config/Procedure", "Settings");
            toolsDropBtContent = EditorGUIUtility.TrTextContentWithIcon("Tools", "工具箱", "CustomTool");
            openCsProjectBtContent = EditorGUIUtility.TrTextContentWithIcon("Open C# Project", "打开C#工程", "dll Script Icon");
            EditorSceneManager.sceneOpened += OnSceneOpened;
            ScanEditorToolClass();

            UnityEditorToolbar.RightToolbarGUI.Add(OnRightToolbarGUI);
            UnityEditorToolbar.LeftToolbarGUI.Add(OnLeftToolbarGUI);
        }

        private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
        {
            switchSceneBtContent.text = scene.name;
        }
        /// <summary>
        /// 获取所有EditorTool扩展工具类,用于显示到Toolbar的Tools菜单栏
        /// </summary>
        static void ScanEditorToolClass()
        {
            editorToolList.Clear();
            var editorDll = Utility.Assembly.GetAssemblies().First(dll => dll.GetName().Name.CompareTo("Assembly-CSharp-Editor") == 0);
            var allEditorTool = editorDll.GetTypes().Where(tp => (tp.IsClass && !tp.IsAbstract && tp.IsSubclassOf(typeof(EditorToolBase)) && tp.GetCustomAttribute(typeof(EditorToolMenuAttribute)) != null));

            editorToolList.AddRange(allEditorTool);
            editorToolList.Sort((x, y) =>
            {
                int xOrder = x.GetCustomAttribute<EditorToolMenuAttribute>().MenuOrder;
                int yOrder = y.GetCustomAttribute<EditorToolMenuAttribute>().MenuOrder;
                return xOrder.CompareTo(yOrder);
            });
        }
        private static void OnLeftToolbarGUI()
        {
            GUILayout.FlexibleSpace();
            if (EditorGUILayout.DropdownButton(switchSceneBtContent, FocusType.Passive, EditorStyles.toolbarPopup, GUILayout.MaxWidth(150)))
            {
                DrawSwithSceneDropdownMenus();
            }
            EditorGUILayout.Space(10);
            if (GUILayout.Button(buildBtContent, EditorStyles.toolbarButton, GUILayout.MaxWidth(125)))
            {
                AppBuildEidtor.Open();
            }
        }

        private static void OnRightToolbarGUI()
        {
            //if (EditorGUILayout.DropdownButton(switchSceneBtContent, FocusType.Passive, EditorStyles.toolbarPopup, GUILayout.MaxWidth(150)))
            //{
            //    DrawSwithSceneDropdownMenus();
            //}
            //EditorGUILayout.Space(10);
            //if (GUILayout.Button(buildBtContent, EditorStyles.toolbarButton, GUILayout.MaxWidth(125)))
            //{
            //    AppBuildEidtor.Open();
            //}
            //EditorGUILayout.Space(10);
            if (GUILayout.Button(appConfigBtContent, EditorStyles.toolbarButton, GUILayout.MaxWidth(100)))
            {
                var config = AppConfigs.GetInstanceEditor();
                Selection.activeObject = config;
                //EditorUtility.OpenPropertyEditor(config);
            }
            EditorGUILayout.Space(10);
            if (EditorGUILayout.DropdownButton(toolsDropBtContent, FocusType.Passive, EditorStyles.toolbarPopup, GUILayout.MaxWidth(90)))
            {
                DrawEditorToolDropdownMenus();
            }
            EditorGUILayout.Space(10);
            if (GUILayout.Button(openCsProjectBtContent, EditorStyles.toolbarButton, GUILayout.MaxWidth(120)))
            {
                OpenCSharpProject();
            }
            GUILayout.FlexibleSpace();
        }
        static void OpenCSharpProject()
        {
            // Ensure that the mono islands are up-to-date
            AssetDatabase.Refresh();
            CodeEditor.Editor.CurrentCodeEditor.SyncAll();

            CodeEditor.Editor.CurrentCodeEditor.OpenProject();
        }
        static void DrawSwithSceneDropdownMenus()
        {
            GenericMenu popMenu = new GenericMenu();
            popMenu.allowDuplicateNames = true;
            var sceneGuids = AssetDatabase.FindAssets("t:Scene", new string[] { ConstEditor.ScenePath });
            sceneAssetList.Clear();
            for (int i = 0; i < sceneGuids.Length; i++)
            {
                var scenePath = AssetDatabase.GUIDToAssetPath(sceneGuids[i]);
                sceneAssetList.Add(scenePath);
                string fileDir = System.IO.Path.GetDirectoryName(scenePath);
                bool isInRootDir = Utility.Path.GetRegularPath(ConstEditor.ScenePath).TrimEnd('/') == Utility.Path.GetRegularPath(fileDir).TrimEnd('/');
                var sceneName = System.IO.Path.GetFileNameWithoutExtension(scenePath);
                string displayName = sceneName;
                if (!isInRootDir)
                {
                    var sceneDir = System.IO.Path.GetRelativePath(ConstEditor.ScenePath, fileDir);
                    displayName = $"{sceneDir}/{sceneName}";
                }

                popMenu.AddItem(new GUIContent(displayName), false, menuIdx => { SwitchScene((int)menuIdx); }, i);
            }
            popMenu.ShowAsContext();
        }

        private static void SwitchScene(int menuIdx)
        {
            if (menuIdx >= 0 && menuIdx < sceneAssetList.Count)
            {
                var scenePath = sceneAssetList[menuIdx];
                var curScene = EditorSceneManager.GetActiveScene();
                if (curScene != null && curScene.isDirty)
                {
                    int opIndex = EditorUtility.DisplayDialogComplex("警告", $"当前场景{curScene.name}未保存,是否保存?", "保存", "取消", "不保存");
                    switch (opIndex)
                    {
                        case 0:
                            if (!EditorSceneManager.SaveOpenScenes())
                            {
                                return;
                            }
                            break;
                        case 1:
                            return;
                    }
                }
                EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            }
        }

        static void DrawEditorToolDropdownMenus()
        {
            GenericMenu popMenu = new GenericMenu();
            for (int i = 0; i < editorToolList.Count; i++)
            {
                var toolAttr = editorToolList[i].GetCustomAttribute<EditorToolMenuAttribute>();
                popMenu.AddItem(new GUIContent(toolAttr.ToolMenuPath), false, menuIdx => { ClickToolsSubmenu((int)menuIdx, toolAttr.IsUtility); }, i);
            }
            popMenu.ShowAsContext();
        }
        static void ClickToolsSubmenu(int menuIdx, bool showAsUtility = false)
        {
            var editorTp = editorToolList[menuIdx];
            var win = EditorWindow.GetWindow(editorTp);
            if (showAsUtility)
            {
                win.ShowUtility();
            }
            else
            {
                win.Show();
            }
        }
    }

}
