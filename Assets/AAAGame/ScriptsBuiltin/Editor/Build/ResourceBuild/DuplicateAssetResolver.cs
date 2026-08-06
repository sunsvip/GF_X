using System;
using System.Collections.Generic;
using UGF.EditorTools.Build.ResourceRules;
using UnityEditor;
using UnityEngine;
using UnityGameFramework.Editor.ResourceTools;

namespace UGF.EditorTools
{
    internal static class DuplicateAssetResolver
    {
        public static void RefreshResourceRule()
        {
            if (AppBuilderEditorSettings.Instance.EnableResourceRuleEditor)
            {
                ResourceRuleEditorUtility.RefreshResourceCollection();
            }
        }

        public static bool AutoResolveAbDuplicateAssets(bool forceExecute = false, bool packed = false)
        {
            if (!forceExecute && !ConstEditor.ResolveDuplicateAssets)
            {
                return false;
            }

            var resEditor = new ResourceEditorController();
            if (!resEditor.Load())
            {
                return false;
            }

            var duplicateAssetNames = FindDuplicateAssetNames(resEditor);
            return duplicateAssetNames != null && ResolveDuplicateAssets(resEditor, duplicateAssetNames, packed);
        }

        private static bool ResolveDuplicateAssets(ResourceEditorController resEditor, HashSet<string> duplicateAssetNames, bool packed)
        {
            if (!resEditor.HasResource(ConstEditor.SharedAssetBundleName, null))
            {
                var addSuccess = resEditor.AddResource(ConstEditor.SharedAssetBundleName, null, null, LoadType.LoadFromMemoryAndQuickDecrypt, packed);
                if (!addSuccess)
                {
                    Debug.LogWarningFormat("ResourceEditor Add Resource:{0} Failed!", ConstEditor.SharedAssetBundleName);
                    return false;
                }
            }

            var hasChanged = false;
            var items = resEditor.GetResource(ConstEditor.SharedAssetBundleName, null).GetAssets();
            foreach (var item in items)
            {
                var assetName = item.Name;
                if (duplicateAssetNames.Contains(assetName))
                {
                    duplicateAssetNames.Remove(assetName);
                }
                else
                {
                    resEditor.UnassignAsset(AssetDatabase.AssetPathToGUID(assetName));
                    hasChanged = true;
                }
            }

            hasChanged |= duplicateAssetNames.Count > 0;
            foreach (var assetName in duplicateAssetNames)
            {
                if (!resEditor.AssignAsset(AssetDatabase.AssetPathToGUID(assetName), ConstEditor.SharedAssetBundleName, null))
                {
                    Debug.LogWarning($"添加资源:{assetName}到{ConstEditor.SharedAssetBundleName}失败!");
                }
            }

            var sharedRes = resEditor.GetResource(ConstEditor.SharedAssetBundleName, null);
            if (sharedRes.Packed != packed)
            {
                sharedRes.Packed = packed;
                hasChanged = true;
            }

            if (!hasChanged)
            {
                Debug.Log("-------------处理冗余资源结束,无重复引用资源------------");
            }

            resEditor.RemoveUnknownAssets();
            resEditor.RemoveUnusedResources();
            return resEditor.Save();
        }

        private static HashSet<string> FindDuplicateAssetNames(ResourceEditorController resEditor)
        {
            var result = new HashSet<string>();
            var assetReferenceDic = new Dictionary<string, int>();
            var srcAssetRoot = resEditor.SourceAssetRootPath;
            var resources = resEditor.GetResources();
            for (var i = 0; i < resources.Length; i++)
            {
                var resource = resources[i];
                if (resource.FullName == ConstEditor.SharedAssetBundleName)
                {
                    continue;
                }

                var assets = resource.GetAssets();
                foreach (var asset in assets)
                {
                    var files = AssetDatabase.GetDependencies(asset.Name, true);
                    foreach (var file in files)
                    {
                        if (!file.StartsWith(srcAssetRoot, StringComparison.Ordinal))
                        {
                            continue;
                        }

                        if (assetReferenceDic.TryGetValue(file, out var resIdx) && i != resIdx)
                        {
                            result.Add(file);
                            continue;
                        }

                        assetReferenceDic[file] = i;
                    }
                }
            }

            return result;
        }
    }
}

