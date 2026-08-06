using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;

namespace UGF.EditorTools
{
    internal static class ImageFileBackupService
    {
        internal static int Backup(IReadOnlyList<string> assetPaths, string projectRoot, string backupPath)
        {
            int totalCount = assetPaths.Count;
            int successCount = 0;
            for (int i = 0; i < totalCount; i++)
            {
                var imagePath = assetPaths[i];
                var sourceImage = Path.GetFullPath(imagePath, projectRoot);
                var destinationImage = Path.GetFullPath(imagePath, backupPath);
                try
                {
                    if (EditorUtility.DisplayCancelableProgressBar($"备份进度({i}/{totalCount})", $"正在备份:{Environment.NewLine}{imagePath}", i / (float)totalCount))
                    {
                        break;
                    }

                    string destinationDirectory = Path.GetDirectoryName(destinationImage);
                    if (!Directory.Exists(destinationDirectory))
                    {
                        Directory.CreateDirectory(destinationDirectory);
                    }

                    File.Copy(sourceImage, destinationImage, true);
                    successCount++;
                }
                catch (Exception exception)
                {
                    UnityEngine.Debug.LogWarningFormat("---------备份图片{0}失败:{1}", imagePath, exception.Message);
                }
            }

            EditorUtility.ClearProgressBar();
            return successCount;
        }

        internal static int Restore(IReadOnlyList<string> relativePaths, string sourceRoot, string destinationRoot)
        {
            int totalCount = relativePaths.Count;
            int successCount = 0;
            for (int i = 0; i < totalCount; i++)
            {
                var relativePath = relativePaths[i];
                var destinationFile = UtilityBuiltin.AssetsPath.GetCombinePath(destinationRoot, relativePath);
                var destinationDirectory = Path.GetDirectoryName(destinationFile);
                if (!Directory.Exists(destinationDirectory))
                {
                    Directory.CreateDirectory(destinationDirectory);
                }

                var sourceFile = UtilityBuiltin.AssetsPath.GetCombinePath(sourceRoot, relativePath);
                if (EditorUtility.DisplayCancelableProgressBar("还原进度", $"还原文件:{relativePath}", i / (float)totalCount))
                {
                    break;
                }

                try
                {
                    File.Copy(sourceFile, destinationFile, true);
                    successCount++;
                }
                catch (Exception exception)
                {
                    UnityEngine.Debug.LogWarningFormat("--------还原文件{0}失败:{1}", sourceFile, exception.Message);
                }
            }

            EditorUtility.ClearProgressBar();
            return successCount;
        }
    }
}
