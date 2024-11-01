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
            AutoResolveAbDuplicateAssets();
        }
        public static bool AutoResolveAbDuplicateAssets()
        {
            if (AppBuildSettings.Instance.UseResourceRule)
            {
                ResourceRuleEditorUtility.RefreshResourceCollection();
            }

            ResourceEditorController resEditor = new ResourceEditorController();
            if (resEditor.Load())
            {
                if (resEditor.HasResource(ConstEditor.SharedAssetBundleName, null))
                {
                    foreach (var item in resEditor.GetResource(ConstEditor.SharedAssetBundleName, null).GetAssets())
                    {
                        resEditor.UnassignAsset(item.Guid);
                    }
                    resEditor.Save();
                }

                var duplicateAssetNames = FindDuplicateAssetNames();
                if (duplicateAssetNames == null) return true;
                bool resolved = ResolveDuplicateAssets(resEditor, duplicateAssetNames);
                return resolved;
            }

            return false;
        }
        private static bool ResolveDuplicateAssets(ResourceEditorController resEditor, List<string> duplicateAssetNames)
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

            var sharedRes = resEditor.GetResource(ConstEditor.SharedAssetBundleName, null);
            bool hasChanged = false;
            List<string> sharedResFiles = new List<string>();
            foreach (var item in sharedRes.GetAssets())
            {
                sharedResFiles.Add(item.Name);
            }
            Debug.Log($"-------------添加下列冗余资源到{ConstEditor.SharedAssetBundleName}------------");
            foreach (var assetName in duplicateAssetNames)
            {
                Debug.Log($"冗余资源:{assetName}");
                if (sharedResFiles.Contains(assetName))
                {
                    continue;
                }
                if (!resEditor.AssignAsset(AssetDatabase.AssetPathToGUID(assetName), ConstEditor.SharedAssetBundleName, null))
                {
                    Debug.LogWarning($"添加资源:{assetName}到{ConstEditor.SharedAssetBundleName}失败!");
                }
                hasChanged = true;
            }
            Debug.Log($"-------------处理冗余资源结束------------");
            var sharedAseets = sharedRes.GetAssets();
            for (int i = sharedAseets.Length - 1; i >= 0; i--)
            {
                var asset = sharedAseets[i];
                if (!duplicateAssetNames.Contains(asset.Name))
                {
                    if (!resEditor.UnassignAsset(asset.Guid))
                    {
                        Debug.LogWarning($"移除{ConstEditor.SharedAssetBundleName}中的资源:{asset.Name}失败!");
                    }
                    hasChanged = true;
                }
            }
            if (hasChanged)
            {
                resEditor.RemoveUnknownAssets();
                resEditor.RemoveUnusedResources();
                return resEditor.Save();
            }
            return true;
        }
        private static List<string> FindDuplicateAssetNames()
        {
            ResourceAnalyzerController resAnalyzer = new ResourceAnalyzerController();
            if (resAnalyzer.Prepare())
            {
                resAnalyzer.Analyze();
                List<string> duplicateAssets = new List<string>();
                var scatteredAssetNames = resAnalyzer.GetScatteredAssetNames();
                foreach (var scatteredAsset in scatteredAssetNames)
                {
                    var hostAssets = resAnalyzer.GetHostAssets(scatteredAsset);
                    if (hostAssets == null || hostAssets.Length < 1) continue;
                    var defaultHostAsset = hostAssets.FirstOrDefault(res => res.Resource.FullName != ConstEditor.SharedAssetBundleName);
                    if (defaultHostAsset != null)
                    {
                        var hostResourceName = defaultHostAsset.Resource.FullName;
                        foreach (var hostAsset in hostAssets)
                        {
                            if (hostAsset.Resource.FullName == ConstEditor.SharedAssetBundleName) continue;
                            if (hostResourceName != hostAsset.Resource.Name)
                            {
                                duplicateAssets.Add(scatteredAsset);
                                break;
                            }
                        }
                    }
                }
                return duplicateAssets;
            }
            return null;
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
