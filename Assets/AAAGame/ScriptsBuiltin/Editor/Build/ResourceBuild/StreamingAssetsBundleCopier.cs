using System.IO;
using UnityEditor;
using UnityEngine;

namespace UGF.EditorTools
{
    internal static class StreamingAssetsBundleCopier
    {
        public static void CopyBuiltBundlesToStreamingAssets(bool outputPackageSelected, string outputPackagePath, bool outputFullSelected, string outputFullPath, bool outputPackedSelected, string outputPackedPath)
        {
            var srcAbPath = string.Empty;
            var copyToStreamingAssets = false;
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

            if (!copyToStreamingAssets)
            {
                return;
            }

            var fileNames = Directory.GetFiles(srcAbPath, "*", SearchOption.AllDirectories);
            var streamingAssetsPath = Application.streamingAssetsPath;
            foreach (var fileName in fileNames)
            {
                var abAssetName = Path.GetRelativePath(srcAbPath, fileName);
                var destFileName = Path.Combine(streamingAssetsPath, abAssetName);
                var destFileInfo = new FileInfo(destFileName);
                if (!destFileInfo.Directory.Exists)
                {
                    destFileInfo.Directory.Create();
                }

                File.Copy(fileName, destFileName, true);
            }

            AssetDatabase.Refresh();
        }
    }
}
