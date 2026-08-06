using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace UGF.EditorTools
{
    internal static class PrefabBatchEditService
    {
        public static void EditPrefabs(IList<string> prefabPaths, string progressTitle, Func<GameObject, bool> editAction)
        {
            if (prefabPaths == null || prefabPaths.Count == 0 || editAction == null)
            {
                return;
            }

            try
            {
                var totalCount = prefabPaths.Count;
                for (var i = 0; i < totalCount; i++)
                {
                    var prefabPath = prefabPaths[i];
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                    if (prefab == null)
                    {
                        continue;
                    }

                    EditorUtility.DisplayProgressBar(
                        $"{progressTitle}({i + 1}/{totalCount})",
                        prefabPath,
                        (i + 1) / (float)totalCount);

                    if (editAction(prefab))
                    {
                        PrefabUtility.SavePrefabAsset(prefab);
                    }
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }
    }
}
