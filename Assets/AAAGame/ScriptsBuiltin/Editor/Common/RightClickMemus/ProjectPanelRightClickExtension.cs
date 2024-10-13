using GameFramework;
using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace UGF.EditorTools
{
    public partial class ProjectPanelRightClickExtension
    {
        [MenuItem("Assets/GF Tools/Clear Prefabs Missing Scripts", priority = 2)]
        static void ClearMissingScripts()
        {
            var selectObjs = Selection.objects;
            int totalCount = selectObjs.Length;
            for (int i = 0; i < totalCount; i++)
            {
                var item = selectObjs[i];
                EditorUtility.DisplayProgressBar($"Clear missing scripts: [{i}/{totalCount}]", $"清理{item.name}丢失脚本:", i / (float)totalCount);
                var path = AssetDatabase.GetAssetPath(item);
                if (Directory.Exists(path))
                {
                    var prefabs = AssetDatabase.FindAssets("t:Prefab", new string[] { path });
                    foreach (var guid in prefabs)
                    {
                        var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                        ClearPrefabMissingComponents(assetPath);
                    }
                }
                else if (File.Exists(path) && Path.GetExtension(path).ToLower().CompareTo(".prefab") == 0)
                {
                    ClearPrefabMissingComponents(path);
                }
            }
            EditorUtility.ClearProgressBar();
        }

        public static void ClearPrefabMissingComponents(string prefabPath)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            var type = PrefabUtility.GetPrefabAssetType(prefab);
            if (type == PrefabAssetType.Model || type == PrefabAssetType.NotAPrefab || type == PrefabAssetType.Variant)
            {
                return;
            }

            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
            var nodes = prefabRoot.GetComponentsInChildren<Transform>(true);
            bool isDirty = false;
            foreach (var node in nodes)
            {
                if (GameObjectUtility.RemoveMonoBehavioursWithMissingScript(node.gameObject) > 0)
                {
                    isDirty = true;
                }
            }
            if (isDirty)
            {
                PrefabUtility.SaveAsPrefabAssetAndConnect(prefabRoot, prefabPath, InteractionMode.AutomatedAction);
            }
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }
        [MenuItem("Assets/GF Tools/Log Asset Dependencies", priority = 19)]
        static void LogAssetDependencies()
        {
            if (Selection.activeObject == null) return;

            var path = AssetDatabase.GetAssetPath(Selection.activeObject);
            if (string.IsNullOrWhiteSpace(path)) return;

            var dependencies = AssetDatabase.GetDependencies(path);
            Debug.Log($"----------------{path} Dependencies---------------");
            foreach (var dependency in dependencies)
            {
                Debug.Log(dependency);
            }
            Debug.Log($"--------------------------------------------------");
        }
#if PSD2UGUI
        [MenuItem("Assets/GF Tools/2D/Auto Sprite Border", priority = 1004)]
        static void AutoSpriteSliceBorder()
        {
            foreach (var guid in Selection.assetGUIDs)
            {
                var assetName = AssetDatabase.GUIDToAssetPath(guid);
                var texImpt = AssetImporter.GetAtPath(assetName) as TextureImporter;
                if (texImpt == null || texImpt.textureType != TextureImporterType.Sprite) continue;
                var rawReadable = texImpt.isReadable;
                if (!rawReadable)
                {
                    texImpt.isReadable = true;
                    texImpt.SaveAndReimport();
                }
                var tex = AssetDatabase.LoadAssetAtPath<Sprite>(assetName);
                texImpt.spriteBorder = Psd2UGUI.UGUIParser.CalculateTexture9SliceBorder(tex.texture);
                texImpt.isReadable = rawReadable;
                texImpt.SaveAndReimport();
            }
        }
#endif

        [MenuItem("Assets/GF Tools/Copy Asset Path/Relative Path", priority = 1000)]
        static void CopyAssetRelativePath()
        {
            CopyAssetsPath2Clipboard(Selection.objects, 1);
        }
        [MenuItem("Assets/GF Tools/Copy Asset Path/Full Path", priority = 1001)]
        static void CopyAssetFullPath()
        {
            CopyAssetsPath2Clipboard(Selection.objects, 0);
        }
        [MenuItem("Assets/GF Tools/Copy Asset Path/Assets Name", priority = 1002)]
        static void CopyAssetNameWithoutPath()
        {
            CopyAssetsPath2Clipboard(Selection.objects, 2);
        }
        [MenuItem("Assets/GF Tools/Create/UIForm Prefab", priority = 1)]
        static void CreateUIFormMenu()
        {
            string savePath = AssetDatabase.GetAssetPath(Selection.activeObject);
            CreatePrefabWithRename(ConstEditor.UIFormTemplate, savePath, "NewUIFormPrefab");
        }
        [MenuItem("Assets/GF Tools/Create/UIDialog Prefab", priority = 2)]
        static void CreateUIDialogMenu()
        {
            string savePath = AssetDatabase.GetAssetPath(Selection.activeObject);
            CreatePrefabWithRename(ConstEditor.UIDialogTemplate, savePath, "NewUIDialogPrefab");
        }
        static void CreatePrefabWithRename(string srcAsset, string savePath, string fileName)
        {
            if (string.IsNullOrEmpty(savePath) || !AssetDatabase.IsValidFolder(savePath) || !File.Exists(srcAsset)) return;
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(
                0,
                ScriptableObject.CreateInstance<DoCreatePrefab>(),
                Utility.Text.Format("{0}.prefab", fileName),
                EditorGUIUtility.FindTexture("Prefab Icon"),
                srcAsset);
        }
        /// <summary>
        /// 复制资源路径到剪贴板
        /// </summary>
        /// <param name="assets"></param>
        /// <param name="copyFullPath"></param>
        private static void CopyAssetsPath2Clipboard(UnityEngine.Object[] assets, int pathMode)
        {
            if (assets == null || assets.Length < 1)
            {
                return;
            }
            StringBuilder strBuilder = new StringBuilder();
            switch (pathMode)
            {
                case 1: //Relative Path
                    foreach (var item in assets)
                    {
                        var itemPath = AssetDatabase.GetAssetPath(item);
                        strBuilder.AppendLine(itemPath);
                    }
                    break;
                case 2:
                    foreach (var item in assets)
                    {
                        var itemPath = AssetDatabase.GetAssetPath(item);
                        if (string.IsNullOrWhiteSpace(itemPath) || !Path.HasExtension(itemPath))
                        {
                            continue;
                        }
                        itemPath = Path.GetFileName(itemPath);
                        strBuilder.AppendLine(itemPath);
                    }
                    break;
                default: //Full Path
                    var projectRoot = Directory.GetParent(Application.dataPath).FullName;
                    foreach (var item in assets)
                    {
                        var itemPath = Path.GetFullPath(AssetDatabase.GetAssetPath(item), projectRoot);
                        strBuilder.AppendLine(itemPath);
                    }
                    break;
            }
            var result = strBuilder.ToString().TrimEnd(Environment.NewLine.ToCharArray());
            EditorGUIUtility.systemCopyBuffer = result;
        }
    }
    class DoCreatePrefab : UnityEditor.ProjectWindowCallback.EndNameEditAction
    {
        public override void Action(int instanceId, string pathName, string resourceFile)
        {
            try
            {
                if (AssetDatabase.CopyAsset(resourceFile, pathName))
                {
                    var newPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(pathName);
                    ProjectWindowUtil.ShowCreatedAsset(newPrefab);
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
    }
}

