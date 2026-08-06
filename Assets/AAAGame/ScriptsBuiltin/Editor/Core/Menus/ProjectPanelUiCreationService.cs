using GameFramework;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace UGF.EditorTools
{
    internal static class ProjectPanelUiCreationService
    {
        public static void CreateUIPrefabWithRename(string srcAsset, string savePath, string fileName, bool createUIScriptFile = false)
        {
            if (string.IsNullOrEmpty(savePath) || !AssetDatabase.IsValidFolder(savePath) || AssetDatabase.LoadAssetAtPath<GameObject>(srcAsset) == null)
            {
                return;
            }

            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(
                0,
                createUIScriptFile ? ScriptableObject.CreateInstance<DoCreateUIPrefabAndScriptFile>() : ScriptableObject.CreateInstance<DoCreatePrefab>(),
                Utility.Text.Format("{0}.prefab", fileName),
                EditorGUIUtility.FindTexture("Prefab Icon"),
                srcAsset);
        }

        public static void CreateUIItemWithRename(string srcAsset, string savePath, string fileName, bool createUIScriptFile = false)
        {
            if (string.IsNullOrEmpty(savePath) || !AssetDatabase.IsValidFolder(savePath) || AssetDatabase.LoadAssetAtPath<GameObject>(srcAsset) == null)
            {
                return;
            }

            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(
                0,
                createUIScriptFile ? ScriptableObject.CreateInstance<DoCreateUIItemAndScriptFile>() : ScriptableObject.CreateInstance<DoCreatePrefab>(),
                Utility.Text.Format("{0}.prefab", fileName),
                EditorGUIUtility.FindTexture("Prefab Icon"),
                srcAsset);
        }

        internal static string GetProjectAbsolutePath(string assetPath)
        {
            return Path.GetFullPath(assetPath, ConstEditor.ProjectRootPath);
        }

        internal static bool TryReadTemplateText(string templateAssetPath, out string text)
        {
            var templateFullPath = GetProjectAbsolutePath(templateAssetPath);
            if (!File.Exists(templateFullPath))
            {
                text = null;
                return false;
            }

            text = File.ReadAllText(templateFullPath, UTF8Encoding.UTF8);
            return true;
        }

        internal static bool TryCreateUIScriptFile(string scriptAssetPath, string templateAssetPath, string className)
        {
            var scriptFullPath = GetProjectAbsolutePath(scriptAssetPath);
            if (File.Exists(scriptFullPath))
            {
                Debug.LogWarningFormat("创建UI脚本失败! 文件已存在:{0}", scriptAssetPath);
                return false;
            }

            if (!TryReadTemplateText(templateAssetPath, out var text))
            {
                Debug.LogErrorFormat("创建UI脚本失败! 文件模板不存在:{0}", templateAssetPath);
                return false;
            }

            text = text.Replace("_CLASS_NAME_", className);
            File.WriteAllText(scriptFullPath, text, new UTF8Encoding(false));
            AssetDatabase.ImportAsset(scriptAssetPath, ImportAssetOptions.ForceUpdate);
            return true;
        }
    }
}
