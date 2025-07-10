using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UGF.EditorTools.ResourceTools;
using UnityEditor;
using UnityEngine;
using UnityGameFramework.Editor.ResourceTools;

namespace UGF.EditorTools
{
    public class AssetBuildHandler : IBuildEventHandler
    {
        public bool ContinueOnFailure
        {
            get
            {
                return false;
            }
        }

        public void OnPreprocessPlatform(Platform platform, string workingPath, bool outputPackageSelected, string outputPackagePath, bool outputFullSelected, string outputFullPath, bool outputPackedSelected, string outputPackedPath)
        {

        }

        public void OnBuildAssetBundlesComplete(Platform platform, string workingPath, bool outputPackageSelected, string outputPackagePath, bool outputFullSelected, string outputFullPath, bool outputPackedSelected, string outputPackedPath, AssetBundleManifest assetBundleManifest)
        {

        }


        public void OnPostprocessPlatform(Platform platform, string workingPath, bool outputPackageSelected, string outputPackagePath, bool outputFullSelected, string outputFullPath, bool outputPackedSelected, string outputPackedPath, bool isSuccess)
        {
            //打包完成后把文件复制到StreamingAssets目录
            string srcAbPath = string.Empty;
            bool copyToStreamingAssets = false;
            if (outputPackageSelected)
            {
                srcAbPath = outputPackagePath;
                copyToStreamingAssets = true;
            }
            else if (outputPackedSelected)
            {
                srcAbPath = outputPackedPath;
                copyToStreamingAssets = true;
            }
            else if (outputFullSelected)
            {
                srcAbPath = outputFullPath;
            }
            if (string.IsNullOrEmpty(srcAbPath))
            {
                Debug.LogErrorFormat("AB资源目录为空.");
                return;
            }
            if (copyToStreamingAssets)
            {
                string[] fileNames = Directory.GetFiles(srcAbPath, "*", SearchOption.AllDirectories);
                string streamingAssetsPath = Application.streamingAssetsPath;
                foreach (string fileName in fileNames)
                {
                    var abAssetName = fileName.Substring(srcAbPath.Length);
                    string destFileName = Path.Combine(streamingAssetsPath, abAssetName);
                    FileInfo destFileInfo = new FileInfo(destFileName);
                    if (!destFileInfo.Directory.Exists)
                    {
                        destFileInfo.Directory.Create();
                    }
                    File.Copy(fileName, destFileName, true);
                }
                AssetDatabase.Refresh();
            }
        }

        public void OnPostprocessAllPlatforms(string productName, string companyName, string gameIdentifier, string gameFrameworkVersion, string unityVersion, string applicableGameVersion, int internalResourceVersion, BuildAssetBundleOptions buildAssetBundleOptions, bool zip, string outputDirectory, string workingPath, bool outputPackageSelected, string outputPackagePath, bool outputFullSelected, string outputFullPath, bool outputPackedSelected, string outputPackedPath, string buildReportPath)
        {

        }

        public void OnPreprocessAllPlatforms(string productName, string companyName, string gameIdentifier, string gameFrameworkVersion, string unityVersion, string applicableGameVersion, int internalResourceVersion, Platform platforms, AssetBundleCompressionType assetBundleCompression, string compressionHelperTypeName, bool additionalCompressionSelected, bool forceRebuildAssetBundleSelected, string buildEventHandlerTypeName, string outputDirectory, BuildAssetBundleOptions buildAssetBundleOptions, string workingPath, bool outputPackageSelected, string outputPackagePath, bool outputFullSelected, string outputFullPath, bool outputPackedSelected, string outputPackedPath, string buildReportPath)
        {
            RemoveStreamingAssetsBundles();
        }

        public void OnPostprocessAllPlatforms(string productName, string companyName, string gameIdentifier, string gameFrameworkVersion, string unityVersion, string applicableGameVersion, int internalResourceVersion, Platform platforms, AssetBundleCompressionType assetBundleCompression, string compressionHelperTypeName, bool additionalCompressionSelected, bool forceRebuildAssetBundleSelected, string buildEventHandlerTypeName, string outputDirectory, BuildAssetBundleOptions buildAssetBundleOptions, string workingPath, bool outputPackageSelected, string outputPackagePath, bool outputFullSelected, string outputFullPath, bool outputPackedSelected, string outputPackedPath, string buildReportPath)
        {

        }

        public void OnOutputUpdatableVersionListData(Platform platform, string versionListPath, int versionListLength, int versionListHashCode, int versionListCompressedLength, int versionListCompressedHashCode)
        {
            //将版本信息写入对应平台的version.json
            var dir = Path.GetDirectoryName(versionListPath);
            var resourceVersionStr = new DirectoryInfo(dir).Parent.Name.Split('_').Last();
            int resourceVersion = string.IsNullOrWhiteSpace(resourceVersionStr) ? 0 : int.Parse(resourceVersionStr);
            var outputVersionFile = UtilityBuiltin.AssetsPath.GetCombinePath(dir, ConstBuiltin.VersionFile);

            var outputVersionInfo = new VersionInfo()
            {
                ApplicableGameVersion = AppBuildSettings.Instance.ApplicableGameVersion,
                ForceUpdateApp = AppBuildSettings.Instance.ForceUpdateApp,
                AppUpdateDesc = AppBuildSettings.Instance.AppUpdateDesc,
                AppUpdateUrl = AppBuildSettings.Instance.AppUpdateUrl,
                UpdatePrefixUri = UtilityBuiltin.AssetsPath.GetCombinePath(AppBuildSettings.Instance.UpdatePrefixUri, platform.ToString()),
                VersionListHashCode = versionListHashCode,
                VersionListLength = versionListLength,
                VersionListCompressedHashCode = versionListCompressedHashCode,
                VersionListCompressedLength = versionListCompressedLength,
                InternalResourceVersion = resourceVersion,
                LastAppVersion = Application.version
            };
            File.WriteAllText(outputVersionFile, UtilityBuiltin.Json.ToJson(outputVersionInfo));
        }


        [MenuItem("Game Framework/Resource Tools/Resolve Duplicate Assets【解决AB资源重复依赖冗余】", false, 100)]
        static void RefreshSharedAssets()
        {
            AutoResolveAbDuplicateAssets(true);
        }
        public static bool AutoResolveAbDuplicateAssets(bool forceExecute = false)
        {
            if (AppBuildSettings.Instance.UseResourceRule)
            {
                ResourceRuleEditorUtility.RefreshResourceCollection();
            }
            if (forceExecute || ConstEditor.ResolveDuplicateAssets)
            {
                ResourceEditorController resEditor = new ResourceEditorController();
                if (resEditor.Load())
                {
                    var duplicateAssetNames = FindDuplicateAssetNames(resEditor);
                    if (duplicateAssetNames == null) return false;
                    bool resolved = ResolveDuplicateAssets(resEditor, duplicateAssetNames);
                    return resolved;
                }
            }
            return false;
        }
        private static bool ResolveDuplicateAssets(ResourceEditorController resEditor, HashSet<string> duplicateAssetNames)
        {
            if (!resEditor.HasResource(ConstEditor.SharedAssetBundleName, null))
            {
                bool addSuccess = resEditor.AddResource(ConstEditor.SharedAssetBundleName, null, null, LoadType.LoadFromMemoryAndQuickDecrypt, false);

                if (!addSuccess)
                {
                    Debug.LogWarningFormat("ResourceEditor Add Resource:{0} Failed!", ConstEditor.SharedAssetBundleName);
                    return false;
                }
            }
            bool hasChanged = false;
            if (duplicateAssetNames.Count > 0)
            {
                Debug.Log($"-------------添加下列冗余资源到{ConstEditor.SharedAssetBundleName}------------");
                var items = resEditor.GetResource(ConstEditor.SharedAssetBundleName, null).GetAssets();
                foreach (var item in items)
                {
                    var aseetName = item.Name;
                    if (duplicateAssetNames.Contains(aseetName))
                    {
                        duplicateAssetNames.Remove(aseetName);
                    }
                    else
                    {
                        resEditor.UnassignAsset(AssetDatabase.AssetPathToGUID(aseetName));
                        hasChanged = true;
                    }
                }
                hasChanged = duplicateAssetNames.Count > 0;
                foreach (var assetName in duplicateAssetNames)
                {
                    if (!resEditor.AssignAsset(AssetDatabase.AssetPathToGUID(assetName), ConstEditor.SharedAssetBundleName, null))
                    {
                        Debug.LogWarning($"添加资源:{assetName}到{ConstEditor.SharedAssetBundleName}失败!");
                    }
                }
                if (hasChanged)
                {
                    Debug.Log($"-------------处理冗余资源结束,新增处理{duplicateAssetNames.Count}个重复引用资源------------");
                }
                else
                {
                    Debug.Log("-------------处理冗余资源结束,无重复引用资源------------");
                }
                
            }

            resEditor.RemoveUnknownAssets();
            resEditor.RemoveUnusedResources();
            return resEditor.Save();
        }
        private static HashSet<string> FindDuplicateAssetNames(ResourceEditorController resEditor)
        {
            HashSet<string> result = new HashSet<string>();
            Dictionary<string, int> assetReferenceDic = new Dictionary<string, int>();
            var srcAssetRoot = resEditor.SourceAssetRootPath;
            var resources = resEditor.GetResources();
            for (int i = 0; i < resources.Length; i++)
            {
                var resource = resources[i];
                if (resource.FullName == ConstEditor.SharedAssetBundleName) continue;
                var assets = resource.GetAssets();
                foreach (var asset in assets)
                {
                    var files = AssetDatabase.GetDependencies(asset.Name, true);
                    foreach (var file in files)
                    {
                        if (!file.StartsWith(srcAssetRoot)) continue;
                        if (assetReferenceDic.TryGetValue(file, out int resIdx) && (i != resIdx))
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

        /// <summary>
        /// 移除StreamingAssets目录的AB包
        /// </summary>
        internal static void RemoveStreamingAssetsBundles()
        {
            string streamingAssetsPath = Application.streamingAssetsPath;
            if (Directory.Exists(streamingAssetsPath))
            {
                var oldAbFiles = Directory.GetFiles(streamingAssetsPath, "*.dat", SearchOption.AllDirectories);
                var projectRoot = Directory.GetParent(Application.dataPath).FullName;
                foreach (var abFile in oldAbFiles)
                {
                    Debug.Log($"删除文件:{abFile}");
                    var relativePath = Path.GetRelativePath(projectRoot, abFile);
                    AssetDatabase.DeleteAsset(relativePath);
                }
                var dirInfo = new DirectoryInfo(streamingAssetsPath);
                var subDirs = dirInfo.GetDirectories("*", SearchOption.AllDirectories);
                foreach (var item in subDirs)
                {
                    if (!item.Exists) continue;
                    if (item.GetFiles("*", SearchOption.AllDirectories).Length <= 0)
                    {
                        Debug.Log($"删除文件夹:{item}");

                        var relativePath = Path.GetRelativePath(projectRoot, item.FullName);
                        AssetDatabase.DeleteAsset(relativePath);
                    }
                }
            }
        }
    }
}
