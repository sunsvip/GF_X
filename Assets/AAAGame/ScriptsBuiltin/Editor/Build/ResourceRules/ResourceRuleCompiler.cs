using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using GameFramework;
using UnityEditor;
using UnityEngine;
using UnityGameFramework.Editor.ResourceTools;
using GFResource = UnityGameFramework.Editor.ResourceTools.Resource;

namespace UGF.EditorTools.Build.ResourceRules
{
    internal sealed class ResourceRuleCompiler
    {
        internal const string DefaultConfigurationPath = "Assets/Plugins/UnityGameFramework/Configs/ResourceRuleEditor.asset";

        private const string SourceAssetExceptTypeFilter = "t:Script";
        private const string SourceAssetExceptLabelFilter = "l:ResourceExclusive";

        private readonly ResourceRuleEditorData _configuration;
        private readonly ResourceCollection _resourceCollection;
        private readonly HashSet<string> _sourceAssetExceptTypeFilterGuidSet;
        private readonly HashSet<string> _sourceAssetExceptLabelFilterGuidSet;

        private ResourceRuleCompiler(ResourceRuleEditorData configuration)
        {
            _configuration = configuration;
            _resourceCollection = new ResourceCollection();
            _resourceCollection.Load();
            _sourceAssetExceptTypeFilterGuidSet = new HashSet<string>(AssetDatabase.FindAssets(SourceAssetExceptTypeFilter));
            _sourceAssetExceptLabelFilterGuidSet = new HashSet<string>(AssetDatabase.FindAssets(SourceAssetExceptLabelFilter));
        }

        internal static bool RefreshResourceCollection()
        {
            var configuration = LoadConfiguration(string.Empty, out _);
            return configuration != null && RefreshResourceCollection(configuration);
        }

        internal static bool RefreshResourceCollection(string configPath)
        {
            var configuration = LoadConfiguration(configPath, out _);
            return configuration != null && RefreshResourceCollection(configuration);
        }

        internal static bool RefreshResourceCollection(ResourceRuleEditorData configuration)
        {
            if (configuration == null)
            {
                return false;
            }

            return new ResourceRuleCompiler(configuration).CompileAndSave();
        }

        private static ResourceRuleEditorData LoadConfiguration(string configPath, out string resolvedPath)
        {
            resolvedPath = configPath;
            if (!string.IsNullOrEmpty(configPath))
            {
                var config = AssetDatabase.LoadAssetAtPath<ResourceRuleEditorData>(configPath);
                if (config != null)
                {
                    return config;
                }
            }

            var allConfigPaths = AssetDatabase.FindAssets("t:ResourceRuleEditorData")
                .Select(AssetDatabase.GUIDToAssetPath)
                .ToList();
            if (allConfigPaths.Count > 0)
            {
                resolvedPath = allConfigPaths[0];
                return AssetDatabase.LoadAssetAtPath<ResourceRuleEditorData>(resolvedPath);
            }

            resolvedPath = DefaultConfigurationPath;
            return ScriptableObject.CreateInstance<ResourceRuleEditorData>();
        }

        private bool CompileAndSave()
        {
            RemoveNonSharedResources();

            var signedAssetBundleSet = new HashSet<string>(StringComparer.Ordinal);
            foreach (var resourceRule in _configuration.rules)
            {
                NormalizeRule(resourceRule);
                if (!resourceRule.valid)
                {
                    continue;
                }

                switch (resourceRule.filterType)
                {
                    case ResourceFilterType.Root:
                        ApplyRootRule(SearchOption.AllDirectories, signedAssetBundleSet, resourceRule);
                        break;
                    case ResourceFilterType.RootTopDirectoryOnly:
                        ApplyRootRule(SearchOption.TopDirectoryOnly, signedAssetBundleSet, resourceRule);
                        break;
                    case ResourceFilterType.Children:
                        ApplyChildrenRule(signedAssetBundleSet, resourceRule);
                        break;
                    case ResourceFilterType.ChildrenFoldersOnly:
                        ApplyChildrenFoldersRule(signedAssetBundleSet, resourceRule);
                        break;
                    case ResourceFilterType.ChildrenFilesOnly:
                        ApplyChildrenFilesOnlyRule(signedAssetBundleSet, resourceRule);
                        break;
                }
            }

            return _resourceCollection.Save();
        }

        private void RemoveNonSharedResources()
        {
            var resources = _resourceCollection.GetResources();
            for (var i = resources.Length - 1; i >= 0; i--)
            {
                var resource = resources[i];
                if (resource.FullName == ConstEditor.SharedAssetBundleName)
                {
                    continue;
                }

                _resourceCollection.RemoveResource(resource.Name, resource.Variant);
            }
        }

        private void NormalizeRule(ResourceRule resourceRule)
        {
            if (resourceRule.variant == string.Empty)
            {
                resourceRule.variant = null;
            }

            if (string.IsNullOrEmpty(resourceRule.fileSystem))
            {
                resourceRule.fileSystem = null;
            }
        }

        private void ApplyRootRule(SearchOption searchOption, HashSet<string> signedResourceSet, ResourceRule resourceRule)
        {
            var resourceName = string.IsNullOrEmpty(resourceRule.name)
                ? Utility.Path.GetRegularPath(resourceRule.assetsDirectoryPath.Replace("Assets/", string.Empty))
                : resourceRule.name;
            ApplyResourceFilter(searchOption, signedResourceSet, resourceRule, resourceName);
        }

        private void ApplyChildrenRule(HashSet<string> signedResourceSet, ResourceRule resourceRule)
        {
            foreach (var filePath in EnumerateAssetFiles(resourceRule.assetsDirectoryPath, resourceRule.searchPatterns, SearchOption.AllDirectories))
            {
                var assetPath = ToAssetPath(filePath);
                if (string.IsNullOrEmpty(assetPath))
                {
                    continue;
                }

                var assetGuid = AssetDatabase.AssetPathToGUID(assetPath);
                if (IsExcludedAsset(assetGuid))
                {
                    continue;
                }

                var resourceName = GetResourceNameWithoutExtension(assetPath);
                ApplyResourceFilter(SearchOption.AllDirectories, signedResourceSet, resourceRule, resourceName, assetGuid);
            }
        }

        private void ApplyChildrenFoldersRule(HashSet<string> signedResourceSet, ResourceRule resourceRule)
        {
            if (!Directory.Exists(resourceRule.assetsDirectoryPath))
            {
                return;
            }

            var assetDirectories = Directory.GetDirectories(resourceRule.assetsDirectoryPath, "*", SearchOption.TopDirectoryOnly);
            for (var i = 0; i < assetDirectories.Length; i++)
            {
                var assetDirectoryPath = ToAssetPath(assetDirectories[i]);
                if (string.IsNullOrEmpty(assetDirectoryPath))
                {
                    continue;
                }

                var resourceName = Utility.Path.GetRegularPath(assetDirectoryPath.Replace("Assets/", string.Empty));
                ApplyResourceFilter(SearchOption.AllDirectories, signedResourceSet, resourceRule, resourceName, string.Empty, assetDirectories[i]);
            }
        }

        private void ApplyChildrenFilesOnlyRule(HashSet<string> signedResourceSet, ResourceRule resourceRule)
        {
            if (!Directory.Exists(resourceRule.assetsDirectoryPath))
            {
                return;
            }

            var assetDirectories = Directory.GetDirectories(resourceRule.assetsDirectoryPath, "*", SearchOption.TopDirectoryOnly);
            for (var i = 0; i < assetDirectories.Length; i++)
            {
                foreach (var filePath in EnumerateAssetFiles(assetDirectories[i], resourceRule.searchPatterns, SearchOption.AllDirectories))
                {
                    var assetPath = ToAssetPath(filePath);
                    if (string.IsNullOrEmpty(assetPath))
                    {
                        continue;
                    }

                    var assetGuid = AssetDatabase.AssetPathToGUID(assetPath);
                    if (IsExcludedAsset(assetGuid))
                    {
                        continue;
                    }

                    var resourceName = GetResourceNameWithoutExtension(assetPath);
                    ApplyResourceFilter(SearchOption.AllDirectories, signedResourceSet, resourceRule, resourceName, assetGuid);
                }
            }
        }

        private void ApplyResourceFilter(
            SearchOption searchOption,
            HashSet<string> signedResourceSet,
            ResourceRule resourceRule,
            string resourceName,
            string singleAssetGuid = "",
            string childDirectoryPath = "")
        {
            var signedKey = Path.Combine(resourceRule.assetsDirectoryPath, resourceName);
            if (!signedResourceSet.Add(signedKey))
            {
                return;
            }

            RenameExistingResource(resourceName, resourceRule.variant);
            EnsureResource(resourceName, resourceRule);

            switch (resourceRule.filterType)
            {
                case ResourceFilterType.Root:
                case ResourceFilterType.RootTopDirectoryOnly:
                case ResourceFilterType.ChildrenFoldersOnly:
                    if (string.IsNullOrEmpty(childDirectoryPath))
                    {
                        childDirectoryPath = resourceRule.assetsDirectoryPath;
                    }

                    foreach (var filePath in EnumerateAssetFiles(childDirectoryPath, resourceRule.searchPatterns, searchOption))
                    {
                        var assetPath = ToAssetPath(filePath);
                        if (string.IsNullOrEmpty(assetPath))
                        {
                            continue;
                        }

                        var assetGuid = AssetDatabase.AssetPathToGUID(assetPath);
                        if (!IsExcludedAsset(assetGuid))
                        {
                            AssignAsset(assetGuid, resourceName, resourceRule.variant, resourceRule.excludeSearchPattern);
                        }
                    }

                    break;

                case ResourceFilterType.Children:
                case ResourceFilterType.ChildrenFilesOnly:
                    AssignAsset(singleAssetGuid, resourceName, resourceRule.variant, resourceRule.excludeSearchPattern);
                    break;
            }
        }

        private void RenameExistingResource(string resourceName, string resourceVariant)
        {
            foreach (var oldResource in _resourceCollection.GetResources())
            {
                if (oldResource.Name == resourceName && string.IsNullOrEmpty(oldResource.Variant))
                {
                    _resourceCollection.RenameResource(oldResource.Name, oldResource.Variant, resourceName, resourceVariant);
                    break;
                }
            }
        }

        private void EnsureResource(string resourceName, ResourceRule resourceRule)
        {
            if (_resourceCollection.HasResource(resourceName, resourceRule.variant))
            {
                return;
            }

            _resourceCollection.AddResource(
                resourceName,
                resourceRule.variant,
                resourceRule.fileSystem,
                resourceRule.loadType,
                resourceRule.packed,
                resourceRule.groups.Split(';', ',', '|'));
        }

        private bool AssignAsset(string assetGuid, string resourceName, string resourceVariant, string excludeRegexPattern)
        {
            if (string.IsNullOrEmpty(assetGuid))
            {
                return false;
            }

            if (!string.IsNullOrEmpty(excludeRegexPattern))
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(assetGuid);
                try
                {
                    if (Regex.IsMatch(Path.GetFileName(assetPath), excludeRegexPattern))
                    {
                        return false;
                    }
                }
                catch (ArgumentException exception)
                {
                    Debug.LogWarning($"忽略非法排除正则: {excludeRegexPattern}, Error: {exception.Message}");
                }
            }

            return _resourceCollection.AssignAsset(assetGuid, resourceName, resourceVariant);
        }

        private bool IsExcludedAsset(string assetGuid)
        {
            return _sourceAssetExceptTypeFilterGuidSet.Contains(assetGuid)
                || _sourceAssetExceptLabelFilterGuidSet.Contains(assetGuid);
        }

        private static IEnumerable<string> EnumerateAssetFiles(string rootPath, string searchPatterns, SearchOption searchOption)
        {
            if (string.IsNullOrWhiteSpace(rootPath) || !Directory.Exists(rootPath))
            {
                yield break;
            }

            var patterns = searchPatterns.Split(new[] { ';', ',', '|' }, StringSplitOptions.RemoveEmptyEntries);
            for (var i = 0; i < patterns.Length; i++)
            {
                var files = Directory.GetFiles(rootPath, patterns[i], searchOption);
                for (var j = 0; j < files.Length; j++)
                {
                    var filePath = files[j];
                    if (string.Equals(Path.GetExtension(filePath), ".meta", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    yield return filePath;
                }
            }
        }

        private static string ToAssetPath(string fullPath)
        {
            if (string.IsNullOrWhiteSpace(fullPath))
            {
                return null;
            }

            var assetsRoot = Path.GetFullPath(Application.dataPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var normalizedFullPath = Path.GetFullPath(fullPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var isInsideAssetsPath = string.Equals(normalizedFullPath, assetsRoot, StringComparison.OrdinalIgnoreCase)
                || normalizedFullPath.StartsWith(assetsRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
                || normalizedFullPath.StartsWith(assetsRoot + Path.AltDirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
            if (!isInsideAssetsPath)
            {
                return null;
            }

            if (string.Equals(normalizedFullPath, assetsRoot, StringComparison.OrdinalIgnoreCase))
            {
                return "Assets";
            }

            var relativePath = Path.GetRelativePath(assetsRoot, normalizedFullPath);
            return Utility.Path.GetRegularPath($"Assets/{relativePath}");
        }

        private static string GetResourceNameWithoutExtension(string assetPath)
        {
            if (string.IsNullOrWhiteSpace(assetPath))
            {
                return string.Empty;
            }

            string directory = Path.GetDirectoryName(assetPath);
            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(assetPath);
            return string.IsNullOrWhiteSpace(directory)
                ? Utility.Path.GetRegularPath(fileNameWithoutExtension)
                : Utility.Path.GetRegularPath(Path.Combine(directory, fileNameWithoutExtension));
        }
    }
}

