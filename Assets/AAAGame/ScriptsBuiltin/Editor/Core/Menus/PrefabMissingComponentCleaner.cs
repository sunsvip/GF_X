using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace UGF.EditorTools
{
    internal static class PrefabMissingComponentCleaner
    {
        public static void ClearMissingScriptsFromSelection(UnityEngine.Object[] selectedObjects)
        {
            var totalCount = selectedObjects.Length;
            try
            {
                for (var i = 0; i < totalCount; i++)
                {
                    var item = selectedObjects[i];
                    if (item == null)
                    {
                        continue;
                    }

                    EditorUtility.DisplayProgressBar($"Clear missing scripts: [{i}/{totalCount}]", $"清理{item.name}丢失脚本:", i / (float)totalCount);
                    var path = AssetDatabase.GetAssetPath(item);
                    if (AssetDatabase.IsValidFolder(path))
                    {
                        var prefabs = AssetDatabase.FindAssets("t:Prefab", new[] { path });
                        foreach (var guid in prefabs)
                        {
                            ClearPrefabMissingComponents(AssetDatabase.GUIDToAssetPath(guid));
                        }
                    }
                    else if (string.Equals(Path.GetExtension(path), ".prefab", StringComparison.OrdinalIgnoreCase) && AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
                    {
                        ClearPrefabMissingComponents(path);
                    }
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        public static void ClearPrefabMissingComponents(string prefabPath)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                return;
            }

            var type = PrefabUtility.GetPrefabAssetType(prefab);
            if (type == PrefabAssetType.Model || type == PrefabAssetType.NotAPrefab || type == PrefabAssetType.Variant)
            {
                return;
            }

            var prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                var nodes = prefabRoot.GetComponentsInChildren<Transform>(true);
                var isDirty = false;
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
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }
    }
}
