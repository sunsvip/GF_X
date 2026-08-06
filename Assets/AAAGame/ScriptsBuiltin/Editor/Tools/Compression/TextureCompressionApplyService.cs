using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;

namespace UGF.EditorTools
{
    internal static class TextureCompressionApplyService
    {
        internal static void Apply(List<string> assetPaths, TextureCompressionPreset preset, string warningLogFile)
        {
            if (assetPaths == null || assetPaths.Count < 1)
            {
                return;
            }

            AssetDatabase.StartAssetEditing();
            try
            {
                int totalCount = assetPaths.Count;
                for (int i = 0; i < totalCount; i++)
                {
                    var assetPath = assetPaths[i];
                    var textureImporter = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                    if (EditorUtility.DisplayCancelableProgressBar($"压缩进度({i}/{totalCount})", assetPath, i / (float)totalCount))
                    {
                        break;
                    }

                    if (textureImporter == null)
                    {
                        continue;
                    }

                    var importerSettings = new TextureImporterSettings();
                    textureImporter.ReadTextureSettings(importerSettings);
                    var platformSettings = textureImporter.GetPlatformTextureSettings(EditorUserBuildSettings.activeBuildTarget.ToString());
                    if (!ApplyOverrides(textureImporter, importerSettings, platformSettings, preset))
                    {
                        continue;
                    }

                    textureImporter.SetTextureSettings(importerSettings);
                    textureImporter.SetPlatformTextureSettings(platformSettings);
                    textureImporter.SaveAndReimport();
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                AssetDatabase.StopAssetEditing();
                AssetDatabase.Refresh();
            }

            FallbackTextureFormat(assetPaths, preset.FallbackFormat, warningLogFile);
        }

        internal static void FallbackTextureFormat(List<string> assetPaths, TextureImporterFormat fallbackFormat, string warningLogFile)
        {
            if (assetPaths == null || assetPaths.Count < 1)
            {
                return;
            }

            AssetDatabase.StartAssetEditing();
            try
            {
                int totalCount = assetPaths.Count;
                var warnings = new StringBuilder();
                for (int i = 0; i < totalCount; i++)
                {
                    var assetPath = assetPaths[i];
                    var textureImporter = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                    if (EditorUtility.DisplayCancelableProgressBar($"压缩失败Fallback({i}/{totalCount})", assetPath, i / (float)totalCount))
                    {
                        break;
                    }

                    if (textureImporter == null || !TextureCompressionEditorBridge.HasImportWarnings(textureImporter, out var warning))
                    {
                        continue;
                    }

                    warnings.AppendLine($"{assetPath}--->{warning}");
                    var platformSettings = textureImporter.GetPlatformTextureSettings(EditorUserBuildSettings.activeBuildTarget.ToString());
                    platformSettings.overridden = true;
                    platformSettings.format = fallbackFormat;
                    textureImporter.SetPlatformTextureSettings(platformSettings);
                    textureImporter.SaveAndReimport();
                }

                if (warnings.Length > 0)
                {
                    try
                    {
                        File.WriteAllText(warningLogFile, warnings.ToString(), new System.Text.UTF8Encoding(false));
                    }
                    catch (System.Exception exception)
                    {
                        UnityEngine.Debug.LogWarning($"写入贴图压缩警告日志失败: {warningLogFile}, Error:{exception.Message}");
                    }
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                AssetDatabase.StopAssetEditing();
                AssetDatabase.Refresh();
            }
        }

        private static bool ApplyOverrides(
            TextureImporter textureImporter,
            TextureImporterSettings importerSettings,
            TextureImporterPlatformSettings platformSettings,
            TextureCompressionPreset preset)
        {
            bool hasChange = false;

            if (preset.OverrideTextureType && importerSettings.textureType != preset.ImporterSettings.textureType)
            {
                importerSettings.textureType = preset.ImporterSettings.textureType;
                hasChange = true;
            }

            if (preset.OverrideSpriteMode && importerSettings.spriteMode != preset.ImporterSettings.spriteMode)
            {
                importerSettings.spriteMode = preset.ImporterSettings.spriteMode;
                hasChange = true;
            }

            if (preset.OverrideMeshType && importerSettings.spriteMeshType != preset.ImporterSettings.spriteMeshType)
            {
                importerSettings.spriteMeshType = preset.ImporterSettings.spriteMeshType;
                hasChange = true;
            }

            if (preset.OverrideAlphaIsTransparency && importerSettings.alphaIsTransparency != preset.ImporterSettings.alphaIsTransparency)
            {
                importerSettings.alphaIsTransparency = preset.ImporterSettings.alphaIsTransparency;
                hasChange = true;
            }

            if (preset.OverrideReadable && importerSettings.readable != preset.ImporterSettings.readable)
            {
                importerSettings.readable = preset.ImporterSettings.readable;
                hasChange = true;
            }

            if (preset.OverrideGenerateMipMaps && importerSettings.mipmapEnabled != preset.ImporterSettings.mipmapEnabled)
            {
                importerSettings.mipmapEnabled = preset.ImporterSettings.mipmapEnabled;
                hasChange = true;
            }

            if (preset.OverrideWrapMode && importerSettings.wrapMode != preset.ImporterSettings.wrapMode)
            {
                importerSettings.wrapMode = preset.ImporterSettings.wrapMode;
                hasChange = true;
            }

            if (preset.OverrideFilterMode && importerSettings.filterMode != preset.ImporterSettings.filterMode)
            {
                importerSettings.filterMode = preset.ImporterSettings.filterMode;
                hasChange = true;
            }

            if (preset.OverrideMaxSize && platformSettings.maxTextureSize != preset.PlatformSettings.maxTextureSize)
            {
                platformSettings.maxTextureSize = preset.PlatformSettings.maxTextureSize;
                platformSettings.overridden = true;
                hasChange = true;
            }

            if (preset.OverrideFormat)
            {
                var destinationFormat = textureImporter.DoesSourceTextureHaveAlpha()
                    ? preset.PlatformSettings.format
                    : preset.NoAlphaFormat;
                if (platformSettings.format != destinationFormat)
                {
                    platformSettings.overridden = true;
                    platformSettings.format = destinationFormat;
                    hasChange = true;
                }
            }

            if (preset.OverrideCompressorQuality && platformSettings.compressionQuality != preset.PlatformSettings.compressionQuality)
            {
                platformSettings.compressionQuality = preset.PlatformSettings.compressionQuality;
                platformSettings.overridden = true;
                hasChange = true;
            }

            return hasChange;
        }
    }
}
