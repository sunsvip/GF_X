using System;
using UnityEditor;
using UnityEditor.SceneManagement;
#if UNITY_6000_3_OR_NEWER
using System.Reflection;
using UnityEditor.Toolbars;
#endif
using UnityEngine;
using UnityEngine.SceneManagement;
namespace UGF.EditorTools
{
    public class EditorToolbarExtension
    {
        private static GUIContent switchSceneBtContent;
        private static GUIContent buildBtContent;
        private static GUIContent appConfigBtContent;
        private static GUIContent toolsDropBtContent;
        private static GUIContent openCsProjectBtContent;

        [InitializeOnLoadMethod]
        static void Init()
        {
            var platformIconImage = ToolbarInternalApiBridge.GetBuildTargetIcon(EditorUserBuildSettings.activeBuildTarget);
            var curOpenSceneName = EditorSceneManager.GetActiveScene().name;
            switchSceneBtContent = EditorGUIUtility.TrTextContentWithIcon(string.IsNullOrEmpty(curOpenSceneName) ? "Switch Scene" : curOpenSceneName, "切换场景", "UnityLogo");

            buildBtContent = EditorGUIUtility.TrTextContentWithIcon("Build App/Hotfix", "打新包/打热更", platformIconImage);
            appConfigBtContent = EditorGUIUtility.TrTextContentWithIcon("App Configs", "配置App运行时所需DataTable/Config/Procedure", "Settings");
            toolsDropBtContent = EditorGUIUtility.TrTextContentWithIcon("Tools", "工具箱", "CustomTool");
            openCsProjectBtContent = EditorGUIUtility.TrTextContentWithIcon("Open C# Project", "打开C#工程", "dll Script Icon");
            EditorSceneManager.sceneOpened += OnSceneOpened;
            EditorToolRegistry.Rebuild();
#if !UNITY_6000_3_OR_NEWER
            UnityEditorToolbar.RightToolbarGUI.Add(OnRightToolbarGUI);
            UnityEditorToolbar.LeftToolbarGUI.Add(OnLeftToolbarGUI);
#endif
        }
#if UNITY_6000_3_OR_NEWER
        [MenuItem("Game Framework/Show Toolbars")]
        static void ShowToolbars()
        {
            var toolbarShowAll = typeof(MainToolbar).GetMethod("ShowAll", BindingFlags.NonPublic | BindingFlags.Static);
            toolbarShowAll?.Invoke(null, new object[] { "GF_X" });
            MainToolbar.Refresh("GF_X");
        }

        [MainToolbarElement("GF_X/Scene", defaultDockIndex = 1, defaultDockPosition = MainToolbarDockPosition.Middle)]
        public static MainToolbarElement SceneSelectionButton()
        {
            var content = new MainToolbarContent(switchSceneBtContent.text, switchSceneBtContent.image as Texture2D, switchSceneBtContent.tooltip);
            return new MainToolbarDropdown(content, (rect) => { DrawSwitchSceneDropdownMenus(); });
        }
        [MainToolbarElement("GF_X/App Builder", defaultDockIndex = 2, defaultDockPosition = MainToolbarDockPosition.Middle)]
        public static MainToolbarElement AppBuilderButton()
        {
            var content = new MainToolbarContent(buildBtContent.text, buildBtContent.image as Texture2D, buildBtContent.tooltip);
            return new MainToolbarButton(content, () => { EditorToolbarCommandService.OpenAppBuilder(); });
        }
        [MainToolbarElement("GF_X/App Configs", defaultDockIndex = 3, defaultDockPosition = MainToolbarDockPosition.Middle)]
        public static MainToolbarElement AppConfigsButton()
        {
            var content = new MainToolbarContent(appConfigBtContent.text, appConfigBtContent.image as Texture2D, appConfigBtContent.tooltip);
            return new MainToolbarButton(content, () => { EditorToolbarCommandService.SelectAppConfigs(); });
        }
        [MainToolbarElement("GF_X/Tools", defaultDockIndex = 4, defaultDockPosition = MainToolbarDockPosition.Middle)]
        public static MainToolbarElement ToolsButton()
        {
            var content = new MainToolbarContent(toolsDropBtContent.text, toolsDropBtContent.image as Texture2D, toolsDropBtContent.tooltip);
            return new MainToolbarDropdown(content, (rect) => { EditorToolbarMenuService.ShowEditorToolMenu(); });
        }
        [MainToolbarElement("GF_X/Open Code Editor", defaultDockIndex = 5, defaultDockPosition = MainToolbarDockPosition.Middle)]
        public static MainToolbarElement OpenScriptsButton()
        {
            var content = new MainToolbarContent(openCsProjectBtContent.text, openCsProjectBtContent.image as Texture2D, openCsProjectBtContent.tooltip);
            return new MainToolbarButton(content, () => { EditorToolbarCommandService.OpenCSharpProject(); });
        }
#endif
        private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
        {
            switchSceneBtContent.text = scene.name;
#if UNITY_6000_3_OR_NEWER
            MainToolbar.Refresh("GF_X/Scene");
#endif
        }

        private static void OnLeftToolbarGUI()
        {
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(buildBtContent, EditorStyles.toolbarButton, GUILayout.MaxWidth(125)))
            {
                EditorToolbarCommandService.OpenAppBuilder();
            }
            EditorGUILayout.Space(10);
            if (EditorGUILayout.DropdownButton(switchSceneBtContent, FocusType.Passive, EditorStyles.toolbarPopup, GUILayout.MaxWidth(150)))
            {
                EditorToolbarMenuService.ShowSceneMenu();
            }
            EditorGUILayout.Space(10);
        }

        private static void OnRightToolbarGUI()
        {
            EditorGUILayout.Space(10);

            if (GUILayout.Button(appConfigBtContent, EditorStyles.toolbarButton, GUILayout.MaxWidth(100)))
            {
                EditorToolbarCommandService.SelectAppConfigs();
            }
            EditorGUILayout.Space(10);
            if (EditorGUILayout.DropdownButton(toolsDropBtContent, FocusType.Passive, EditorStyles.toolbarPopup, GUILayout.MaxWidth(90)))
            {
                EditorToolbarMenuService.ShowEditorToolMenu();
            }
            EditorGUILayout.Space(10);
            if (GUILayout.Button(openCsProjectBtContent, EditorStyles.toolbarButton, GUILayout.MaxWidth(120)))
            {
                EditorToolbarCommandService.OpenCSharpProject();
            }
            GUILayout.FlexibleSpace();
        }
    }

}
