using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace UGF.EditorTools
{
    internal static class AnimationClipOptimizeService
    {
        internal static void Optimize(List<string> assetPaths, int precision = 3, float posAllowErr = 0.02f, float rotAllowErr = 0.01f, float scaleAllowErr = 0.05f, bool accurateEndPoint = false)
        {
            int totalCount = assetPaths.Count;
            int finishCount = 0;
            var projectRoot = Directory.GetParent(Application.dataPath).FullName;
            var compressor = new AnimationCompressor.Core();
            var compressOption = new AnimationCompressor.Option
            {
                PositionAllowError = posAllowErr,
                RotationAllowError = rotAllowErr,
                ScaleAllowError = scaleAllowErr,
                EnableAccurateEndPointNodes = accurateEndPoint
            };

            try
            {
                for (var i = 0; i < assetPaths.Count; i++)
                {
                    var assetPath = assetPaths[i];
                    var filePath = Path.GetFullPath(assetPath, projectRoot);
                    if (!File.Exists(filePath))
                    {
                        continue;
                    }

                    var fileInfo = new FileInfo(filePath);
                    if (fileInfo.IsReadOnly || !string.Equals(Path.GetExtension(assetPath), ".anim", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    finishCount++;
                    if (EditorUtility.DisplayCancelableProgressBar($"压缩动画({finishCount}/{totalCount})", assetPath, finishCount / (float)totalCount))
                    {
                        break;
                    }

                    var animationClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(assetPath);
                    if (animationClip == null)
                    {
                        continue;
                    }

                    compressor.Compress(animationClip, compressOption);
                    OptimizePrecision(assetPath, precision);
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                AssetDatabase.Refresh();
            }
        }

        private static void OptimizePrecision(string animationClipAsset, int precision)
        {
            if (EditorSettings.serializationMode != SerializationMode.ForceText)
            {
                return;
            }

            string pattern = $"(?<=:\\s)-?\\d+\\.\\d{{{precision},}}";
            var projectRoot = Directory.GetParent(Application.dataPath).FullName;
            var animationClipPath = Path.GetFullPath(animationClipAsset, projectRoot);
            string backupPath = animationClipPath + ".bak";
            string tempPath = animationClipPath + ".tmp";
            File.Copy(animationClipPath, backupPath, true);
            try
            {
                var allText = File.ReadAllText(animationClipPath);
                string output = Regex.Replace(
                    allText,
                    pattern,
                    match => float.Parse(match.Value, CultureInfo.InvariantCulture).ToString($"F{precision}", CultureInfo.InvariantCulture));
                File.WriteAllText(tempPath, output, new System.Text.UTF8Encoding(false));
                File.Replace(tempPath, animationClipPath, null);
            }
            catch
            {
                if (File.Exists(backupPath))
                {
                    File.Copy(backupPath, animationClipPath, true);
                }

                throw;
            }
            finally
            {
                if (File.Exists(tempPath))
                {
                    File.Delete(tempPath);
                }

                if (File.Exists(backupPath))
                {
                    File.Delete(backupPath);
                }
            }
        }
    }
}
