using System;
using System.Collections.Generic;
using System.IO;
using GameFramework;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace UGF.EditorTools
{
    internal static class ImageFontAiGenerateService
    {
        internal const string DefaultTargetDirectory = "Assets/AAAGame/Sprites/ImageFonts";
        private const byte GlyphAlphaThreshold = 8;
        private const int GlyphCellGapPixels = 2;

        internal static bool IsRunning => AiCliTaskExecutor.IsRunning;

        internal static AiCliTaskStatusSnapshot GetStatusSnapshot()
        {
            return AiCliTaskExecutor.GetStatusSnapshot();
        }

        internal static bool CancelCurrentTask()
        {
            return AiCliTaskExecutor.Cancel("用户取消艺术字 AI 图集生成任务。");
        }

        internal static bool SliceAtlas(Texture2D texture, IReadOnlyList<int> unicodes)
        {
            if (texture == null)
            {
                Debug.LogWarning("艺术字 Sprite Slice 失败: 请先选择 Sprite 图集。");
                return false;
            }

            if (unicodes == null || unicodes.Count == 0)
            {
                Debug.LogWarning("艺术字 Sprite Slice 失败: 字符列表为空。");
                return false;
            }

            string assetPath = AssetDatabase.GetAssetPath(texture);
            if (string.IsNullOrWhiteSpace(assetPath))
            {
                Debug.LogError("艺术字 Sprite Slice 失败: 当前 Texture2D 不是项目资源。");
                return false;
            }

            var importer = TextureImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
            {
                Debug.LogError($"艺术字 Sprite Slice 失败, TextureImporter 为空: {assetPath}");
                return false;
            }

            ConfigureTextureImporterForImageFont(importer);
            importer.SaveAndReimport();

            texture = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
            if (texture == null)
            {
                Debug.LogError($"艺术字 Sprite Slice 失败, Texture2D 加载为空: {assetPath}");
                return false;
            }

            if (!TryBuildVariableWidthUniformHeightSpriteRects(texture, unicodes, out var spriteRects, out var errorMessage))
            {
                Debug.LogError($"艺术字 Sprite Slice 失败: {errorMessage}");
                return false;
            }

            ApplySpriteRects(importer, spriteRects);
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
            Debug.Log($"艺术字 Sprite Slice 完成: {assetPath}, Sprite 数量: {spriteRects.Length}");
            return true;
        }

        internal static bool GenerateAtlas(
            AiCliProvider provider,
            IReadOnlyList<int> unicodes,
            string styleRequirement,
            string targetDirectory,
            bool showDebugCommandWindow,
            IList<UnityEngine.Object> selectedObjects,
            Action onComplete)
        {
            if (AiCliTaskExecutor.IsRunning)
            {
                Debug.LogWarning("已有 AI CLI 任务在运行中。");
                return false;
            }

            if (unicodes == null || unicodes.Count == 0)
            {
                Debug.LogWarning("艺术字 AI 生成失败: 字符列表为空。");
                return false;
            }

            if (string.IsNullOrWhiteSpace(styleRequirement))
            {
                Debug.LogWarning("艺术字 AI 生成失败: 请先填写艺术字样式需求。");
                return false;
            }

            if (!TryResolveTargetDirectory(targetDirectory, out string targetAssetDirectory, out string targetFullDirectory, out string errorMessage))
            {
                Debug.LogError(errorMessage);
                return false;
            }

            string outputFileName = $"ImageFontAtlas_{DateTime.Now:yyyyMMdd_HHmmss}.png";
            var taskDefinition = new ImageFontAiGenerateTaskDefinition(provider, unicodes, styleRequirement, outputFileName);
            return AiCliTaskExecutor.Start(
                taskDefinition,
                showDebugCommandWindow,
                null,
                () =>
                {
                    TryImportGeneratedAtlas(taskDefinition, targetAssetDirectory, targetFullDirectory, unicodes, selectedObjects);
                    onComplete?.Invoke();
                });
        }

        private static void TryImportGeneratedAtlas(
            ImageFontAiGenerateTaskDefinition taskDefinition,
            string targetAssetDirectory,
            string targetFullDirectory,
            IReadOnlyList<int> unicodes,
            IList<UnityEngine.Object> selectedObjects)
        {
            var status = AiCliTaskExecutor.GetStatusSnapshot();
            if (status.State != AiCliTaskState.Completed)
            {
                return;
            }

            string sourcePath = taskDefinition.GetExpectedOutputPath();
            if (!File.Exists(sourcePath))
            {
                Debug.LogError($"艺术字 AI 输出文件不存在: {sourcePath}");
                return;
            }

            if (!TryBuildCompactAtlas(sourcePath, unicodes, out var compactAtlasPng, out var spriteRects, out var validationError))
            {
                Debug.LogError($"艺术字 AI 图集校验失败, 未复制到项目目录: {validationError}");
                return;
            }

            Directory.CreateDirectory(targetFullDirectory);
            string targetAssetPath = UtilityBuiltin.AssetsPath.GetCombinePath(targetAssetDirectory, taskDefinition.OutputFileName);
            string targetFullPath = Path.Combine(ConstEditor.ProjectRootPath, targetAssetPath);
            File.WriteAllBytes(targetFullPath, compactAtlasPng);

            AssetDatabase.ImportAsset(targetAssetPath, ImportAssetOptions.ForceUpdate);
            var importer = TextureImporter.GetAtPath(targetAssetPath) as TextureImporter;
            if (importer == null)
            {
                Debug.LogError($"艺术字 AI 图集导入失败, TextureImporter 为空: {targetAssetPath}");
                return;
            }

            ConfigureTextureImporterForImageFont(importer);
            importer.SaveAndReimport();

            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(targetAssetPath);
            if (texture == null)
            {
                Debug.LogError($"艺术字 AI 图集导入失败, Texture2D 加载为空: {targetAssetPath}");
                return;
            }

            ApplySpriteRects(importer, spriteRects);
            texture = AssetDatabase.LoadAssetAtPath<Texture2D>(targetAssetPath);
            if (texture == null)
            {
                Debug.LogError($"艺术字 AI 图集切片后加载失败: {targetAssetPath}");
                return;
            }

            if (selectedObjects != null)
            {
                if (selectedObjects.Count == 0)
                {
                    selectedObjects.Add(texture);
                }
                else
                {
                    selectedObjects[0] = texture;
                }
            }

            Selection.activeObject = texture;
            Debug.Log($"艺术字 AI 图集已生成并加入列表: {targetAssetPath}");
        }

        internal static void ConfigureTextureImporterForImageFont(TextureImporter importer)
        {
            importer.GetSourceTextureWidthAndHeight(out int sourceWidth, out int sourceHeight);
            int maxTextureSize = Mathf.NextPowerOfTwo(Mathf.Max(sourceWidth, sourceHeight));
            maxTextureSize = Mathf.Clamp(maxTextureSize, 32, 16384);

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.isReadable = true;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.crunchedCompression = false;
            importer.maxTextureSize = maxTextureSize;

            var defaultSettings = importer.GetDefaultPlatformTextureSettings();
            defaultSettings.maxTextureSize = maxTextureSize;
            defaultSettings.textureCompression = TextureImporterCompression.Uncompressed;
            defaultSettings.crunchedCompression = false;
            importer.SetPlatformTextureSettings(defaultSettings);
        }

        private static bool TryBuildCompactAtlas(
            string sourcePath,
            IReadOnlyList<int> unicodes,
            out byte[] compactAtlasPng,
            out SpriteRect[] spriteRects,
            out string errorMessage)
        {
            compactAtlasPng = null;
            spriteRects = null;
            errorMessage = null;
            if (unicodes == null || unicodes.Count == 0)
            {
                errorMessage = "字符列表为空。";
                return false;
            }

            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                if (!texture.LoadImage(File.ReadAllBytes(sourcePath), false))
                {
                    errorMessage = $"PNG 解码失败: {sourcePath}";
                    return false;
                }

                return TryBuildCompactAtlas(texture, unicodes, out compactAtlasPng, out spriteRects, out errorMessage);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        private static bool TryBuildCompactAtlas(
            Texture2D texture,
            IReadOnlyList<int> unicodes,
            out byte[] compactAtlasPng,
            out SpriteRect[] spriteRects,
            out string errorMessage)
        {
            compactAtlasPng = null;
            spriteRects = null;
            if (!TryCollectGlyphRuns(texture, unicodes, out var sourcePixels, out var runs, out errorMessage))
            {
                return false;
            }

            int compactWidth = GlyphCellGapPixels * (runs.Count - 1);
            for (int i = 0; i < runs.Count; i++)
            {
                compactWidth += runs[i].End - runs[i].Start + 1;
            }

            var compactPixels = new Color32[compactWidth * texture.height];
            var result = new SpriteRect[unicodes.Count];
            int destinationX = 0;
            for (int i = 0; i < runs.Count; i++)
            {
                var run = runs[i];
                int glyphWidth = run.End - run.Start + 1;
                for (int y = 0; y < texture.height; y++)
                {
                    Array.Copy(sourcePixels, y * texture.width + run.Start, compactPixels, y * compactWidth + destinationX, glyphWidth);
                }

                result[i] = new SpriteRect
                {
                    name = $"{i:D4}_u{unicodes[i]:X}",
                    rect = new Rect(destinationX, 0f, glyphWidth, texture.height),
                    pivot = new Vector2(0.5f, 0.5f),
                    alignment = SpriteAlignment.Center
                };
                destinationX += glyphWidth + GlyphCellGapPixels;
            }

            var compactTexture = new Texture2D(compactWidth, texture.height, TextureFormat.RGBA32, false);
            try
            {
                compactTexture.SetPixels32(compactPixels);
                compactTexture.Apply(false, false);
                compactAtlasPng = compactTexture.EncodeToPNG();
                spriteRects = result;
                return true;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(compactTexture);
            }
        }

        private static bool TryBuildVariableWidthUniformHeightSpriteRects(Texture2D texture, IReadOnlyList<int> unicodes, out SpriteRect[] spriteRects, out string errorMessage)
        {
            spriteRects = null;
            if (!TryCollectGlyphRuns(texture, unicodes, out _, out var runs, out errorMessage))
            {
                return false;
            }

            int padding = Mathf.Max(1, Mathf.RoundToInt(texture.height * 0.015f));
            var result = new SpriteRect[unicodes.Count];
            for (int i = 0; i < unicodes.Count; i++)
            {
                var run = runs[i];
                int leftLimit = i == 0 ? 0 : (runs[i - 1].End + run.Start) / 2 + 1;
                int rightLimit = i == runs.Count - 1 ? texture.width - 1 : (run.End + runs[i + 1].Start) / 2;
                int xMin = Mathf.Clamp(run.Start - padding, leftLimit, texture.width - 1);
                int xMax = Mathf.Clamp(run.End + padding, 0, rightLimit);
                result[i] = new SpriteRect
                {
                    name = $"{i:D4}_u{unicodes[i]:X}",
                    rect = new Rect(xMin, 0f, xMax - xMin + 1, texture.height),
                    pivot = new Vector2(0.5f, 0.5f),
                    alignment = SpriteAlignment.Center
                };
            }

            spriteRects = result;
            return true;
        }

        private static bool TryCollectGlyphRuns(
            Texture2D texture,
            IReadOnlyList<int> unicodes,
            out Color32[] pixels,
            out List<IntRange> runs,
            out string errorMessage)
        {
            pixels = null;
            runs = null;
            errorMessage = null;
            if (texture == null || unicodes == null || unicodes.Count == 0)
            {
                errorMessage = "Texture2D 或字符列表为空。";
                return false;
            }

            if (texture.width < unicodes.Count)
            {
                errorMessage = $"图集宽度({texture.width})小于字符数量({unicodes.Count})。";
                return false;
            }

            pixels = texture.GetPixels32();
            var columnPixelCounts = new int[texture.width];
            for (int y = 0; y < texture.height; y++)
            {
                int rowOffset = y * texture.width;
                for (int x = 0; x < texture.width; x++)
                {
                    if (pixels[rowOffset + x].a > GlyphAlphaThreshold)
                    {
                        columnPixelCounts[x]++;
                    }
                }
            }

            runs = CollectColumnRuns(columnPixelCounts);
            if (runs.Count == 0)
            {
                errorMessage = "未检测到有效字符像素。";
                return false;
            }

            MergeNearbyRuns(runs, Mathf.Max(2, texture.height / 64));
            while (runs.Count > unicodes.Count)
            {
                MergeClosestRuns(runs);
            }

            if (runs.Count != unicodes.Count)
            {
                errorMessage = $"检测到的字符区间数({runs.Count})与字符数({unicodes.Count})不一致。";
                return false;
            }

            return true;
        }

        private static void ApplySpriteRects(TextureImporter importer, SpriteRect[] spriteRects)
        {
            var factory = new SpriteDataProviderFactories();
            factory.Init();
            var dataProvider = factory.GetSpriteEditorDataProviderFromObject(importer);
            if (dataProvider == null)
            {
                Debug.LogWarning("艺术字 AI 图集切片失败: sprite editor data provider is null.");
                return;
            }

            dataProvider.InitSpriteEditorDataProvider();
            dataProvider.SetSpriteRects(spriteRects);
            dataProvider.Apply();
            importer.SaveAndReimport();
        }

        private static List<IntRange> CollectColumnRuns(int[] columnPixelCounts)
        {
            var runs = new List<IntRange>();
            bool inRun = false;
            int start = 0;
            for (int x = 0; x < columnPixelCounts.Length; x++)
            {
                if (columnPixelCounts[x] > 0)
                {
                    if (!inRun)
                    {
                        start = x;
                        inRun = true;
                    }
                }
                else if (inRun)
                {
                    runs.Add(new IntRange(start, x - 1));
                    inRun = false;
                }
            }

            if (inRun)
            {
                runs.Add(new IntRange(start, columnPixelCounts.Length - 1));
            }

            return runs;
        }

        private static void MergeNearbyRuns(List<IntRange> runs, int maxGap)
        {
            for (int i = 0; i < runs.Count - 1;)
            {
                if (runs[i + 1].Start - runs[i].End - 1 <= maxGap)
                {
                    runs[i] = new IntRange(runs[i].Start, runs[i + 1].End);
                    runs.RemoveAt(i + 1);
                    continue;
                }

                i++;
            }
        }

        private static void MergeClosestRuns(List<IntRange> runs)
        {
            int mergeIndex = 0;
            int minGap = int.MaxValue;
            for (int i = 0; i < runs.Count - 1; i++)
            {
                int gap = runs[i + 1].Start - runs[i].End - 1;
                if (gap < minGap)
                {
                    minGap = gap;
                    mergeIndex = i;
                }
            }

            runs[mergeIndex] = new IntRange(runs[mergeIndex].Start, runs[mergeIndex + 1].End);
            runs.RemoveAt(mergeIndex + 1);
        }

        private readonly struct IntRange
        {
            internal readonly int Start;
            internal readonly int End;

            internal IntRange(int start, int end)
            {
                Start = start;
                End = end;
            }
        }

        private static bool TryResolveTargetDirectory(string targetDirectory, out string targetAssetDirectory, out string targetFullDirectory, out string errorMessage)
        {
            targetAssetDirectory = null;
            targetFullDirectory = null;
            errorMessage = null;

            string directory = string.IsNullOrWhiteSpace(targetDirectory) ? DefaultTargetDirectory : targetDirectory.Trim();
            string projectRoot = Path.GetFullPath(ConstEditor.ProjectRootPath);
            string fullDirectory = Path.IsPathRooted(directory)
                ? Path.GetFullPath(directory)
                : Path.GetFullPath(Path.Combine(projectRoot, directory));
            string assetsRoot = Path.GetFullPath(Application.dataPath);

            if (!IsChildOrSame(fullDirectory, assetsRoot))
            {
                errorMessage = $"艺术字 AI 图片生成目录必须位于 Assets 下: {directory}";
                return false;
            }

            string relativeToAssets = Path.GetRelativePath(assetsRoot, fullDirectory);
            targetAssetDirectory = relativeToAssets == "."
                ? "Assets"
                : Utility.Path.GetRegularPath(Path.Combine("Assets", relativeToAssets));
            targetFullDirectory = fullDirectory;
            return true;
        }

        private static bool IsChildOrSame(string path, string parent)
        {
            string normalizedPath = Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            string normalizedParent = Path.GetFullPath(parent).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            return string.Equals(normalizedPath, normalizedParent, StringComparison.OrdinalIgnoreCase)
                || normalizedPath.StartsWith(normalizedParent + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
                || normalizedPath.StartsWith(normalizedParent + Path.AltDirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
        }
    }
}
