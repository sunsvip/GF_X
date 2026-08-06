using System;
using System.Collections.Generic;
using System.IO;
using GameFramework;
using UnityEditor;

namespace UGF.EditorTools
{
    internal static class ImageFileCompressionRunner
    {
        internal static List<string> Compress(List<string> assetPaths, string inputRoot, string outputRoot)
        {
            var failedAssets = new List<string>(assetPaths);
            if (failedAssets.Count == 0)
            {
                return failedAssets;
            }

            try
            {
                failedAssets.Reverse();
                int totalCount = failedAssets.Count;
                for (int i = totalCount - 1; i >= 0; i--)
                {
                    var assetPath = failedAssets[i];
                    var inputFile = Utility.Path.GetRegularPath(Path.GetFullPath(assetPath, inputRoot));
                    var outputFile = Utility.Path.GetRegularPath(Path.GetFullPath(assetPath, outputRoot));
                    var outputDirectory = Path.GetDirectoryName(outputFile);
                    if (!string.IsNullOrWhiteSpace(outputDirectory) && !Directory.Exists(outputDirectory))
                    {
                        Directory.CreateDirectory(outputDirectory);
                    }

                    if (EditorUtility.DisplayCancelableProgressBar(
                        Utility.Text.Format("压缩进度({0}/{1})", totalCount - failedAssets.Count, totalCount),
                        Utility.Text.Format("正在压缩:{0}", assetPath),
                        (totalCount - i) / (float)totalCount))
                    {
                        break;
                    }

                    bool success = ImageCompressionService.CompressOffline(inputFile, outputFile);
                    if (success)
                    {
                        failedAssets.RemoveAt(i);
                    }
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                AssetDatabase.Refresh();
            }

            return failedAssets;
        }
    }
}
