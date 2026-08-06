using UnityEditor;
using UnityEngine;

namespace UGF.EditorTools
{
    public static class ProjectPanelMenuCommands
    {
        [MenuItem("Assets/GF Tools/Clear Prefabs Missing Scripts", priority = 2)]
        static void ClearMissingScripts()
        {
            PrefabMissingComponentCleaner.ClearMissingScriptsFromSelection(Selection.objects);
        }

        public static void ClearPrefabMissingComponents(string prefabPath)
        {
            PrefabMissingComponentCleaner.ClearPrefabMissingComponents(prefabPath);
        }

        [MenuItem("Assets/GF Tools/Log Asset Dependencies", priority = 19)]
        static void LogAssetDependencies()
        {
            AssetDependencyLogger.LogDependencies(Selection.activeObject);
        }

        [MenuItem("Assets/GF Tools/Copy Asset Path/Relative Path", priority = 1000)]
        static void CopyAssetRelativePath()
        {
            AssetMenuClipboardUtility.CopyAssetsPathToClipboard(Selection.objects, 1);
        }

        [MenuItem("Assets/GF Tools/Copy Asset Path/Full Path", priority = 1001)]
        static void CopyAssetFullPath()
        {
            AssetMenuClipboardUtility.CopyAssetsPathToClipboard(Selection.objects, 0);
        }

        [MenuItem("Assets/GF Tools/Copy Asset Path/Assets Name", priority = 1002)]
        static void CopyAssetNameWithoutPath()
        {
            AssetMenuClipboardUtility.CopyAssetsPathToClipboard(Selection.objects, 2);
        }

        [MenuItem("Assets/GF Tools/Create/UIForm Prefab", priority = 1)]
        static void CreateUIFormMenu()
        {
            var savePath = AssetDatabase.GetAssetPath(Selection.activeObject);
            ProjectPanelUiCreationService.CreateUIPrefabWithRename(ConstEditor.UIFormTemplate, savePath, "NewUIForm");
        }

        [MenuItem("Assets/GF Tools/Create/UIDialog Prefab", priority = 2)]
        static void CreateUIDialogMenu()
        {
            var savePath = AssetDatabase.GetAssetPath(Selection.activeObject);
            ProjectPanelUiCreationService.CreateUIPrefabWithRename(ConstEditor.UIDialogTemplate, savePath, "NewUIDialog");
        }

        [MenuItem("Assets/GF Tools/Create/UIForm Prefab And Script", priority = 3)]
        static void CreateUIFormAndScriptMenu()
        {
            var savePath = AssetDatabase.GetAssetPath(Selection.activeObject);
            ProjectPanelUiCreationService.CreateUIPrefabWithRename(ConstEditor.UIFormTemplate, savePath, "NewUIForm", true);
        }

        [MenuItem("Assets/GF Tools/Create/UIDialog Prefab And Script", priority = 4)]
        static void CreateUIDialogAndScriptMenu()
        {
            var savePath = AssetDatabase.GetAssetPath(Selection.activeObject);
            ProjectPanelUiCreationService.CreateUIPrefabWithRename(ConstEditor.UIDialogTemplate, savePath, "NewUIDialog", true);
        }

        [MenuItem("Assets/GF Tools/Create/UIItem And Script", priority = 5)]
        static void CreateUIItemAndScriptMenu()
        {
            var savePath = AssetDatabase.GetAssetPath(Selection.activeObject);
            ProjectPanelUiCreationService.CreateUIItemWithRename(ConstEditor.UIItemTemplate, savePath, "NewUIItem", true);
        }
    }
}
