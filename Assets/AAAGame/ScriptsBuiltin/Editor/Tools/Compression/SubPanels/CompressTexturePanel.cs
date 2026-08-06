using GameFramework;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace UGF.EditorTools
{
    [EditorToolMenu("压缩贴图", typeof(CompressToolEditor), 2)]
    public class CompressTexturePanel : CompressToolSubPanel
    {
        private string TexWarningLogFile => Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Tools/CompressImageTools/TextureWarnings.txt");
        public override string AssetSelectorTypeFilter => "t:sprite t:texture2d t:folder";
        public override string ReadmeText => "批量修改当前目标平台的图片压缩格式";
        public override string DragAreaTips => "拖拽到此处添加文件夹或图片";

        private readonly Type[] mSupportAssetTypes = { typeof(Sprite), typeof(Texture2D) };
        protected override Type[] SupportAssetTypes => mSupportAssetTypes;

        private readonly int[] maxTextureSizeOptionValues = { 32, 64, 128, 256, 512, 1024, 2048, 4096, 8192, 16384 };
        private readonly string[] maxTextureSizeDisplayOptions = { "32", "64", "128", "256", "512", "1024", "2048", "4096", "8192", "16384" };

        private readonly Dictionary<BuildTarget, TextureImporterFormat[]> texFormatsForPlatforms = new Dictionary<BuildTarget, TextureImporterFormat[]>
        {
            [BuildTarget.Android] = new[] { TextureImporterFormat.ETC2_RGBA8Crunched, TextureImporterFormat.ASTC_6x6 },
            [BuildTarget.StandaloneWindows] = new[] { TextureImporterFormat.DXT5Crunched, TextureImporterFormat.DXT5 },
            [BuildTarget.StandaloneWindows64] = new[] { TextureImporterFormat.DXT5Crunched, TextureImporterFormat.DXT5 }
        };

        private readonly Dictionary<BuildTarget, TextureImporterFormat> texNoAlphaFormatPlatforms = new Dictionary<BuildTarget, TextureImporterFormat>
        {
            [BuildTarget.Android] = TextureImporterFormat.ETC_RGB4Crunched,
            [BuildTarget.StandaloneWindows] = TextureImporterFormat.DXT1Crunched,
            [BuildTarget.StandaloneWindows64] = TextureImporterFormat.DXT1Crunched,
        };

        private readonly Dictionary<BuildTarget, int> texMaxSizePlatforms = new Dictionary<BuildTarget, int>
        {
            [BuildTarget.Android] = 2048,
            [BuildTarget.StandaloneWindows] = 4096,
            [BuildTarget.StandaloneWindows64] = 4096
        };

        private int[] texFormatValues;
        private string[] texFormatDisplayOptions;
        private TextureCompressionPreset compressionPreset;

        public override void OnEnter()
        {
            TextureCompressionEditorBridge.InitializeTextureFormatOptions(out texFormatValues, out texFormatDisplayOptions);
            compressionPreset = TextureCompressionPreset.CreateDefault(texFormatValues);
        }

        public override void DrawBottomButtonsPanel()
        {
            EditorGUILayout.BeginHorizontal("box");
            {
                if (GUILayout.Button("开始压缩", GUILayout.Height(30)))
                {
                    StartCompressUnityAssetMode();
                }

                if (GUILayout.Button("压缩警告日志", GUILayout.Height(30), GUILayout.MaxWidth(100)) && File.Exists(TexWarningLogFile))
                {
                    EditorUtility.RevealInFinder(TexWarningLogFile);
                }

                if (GUILayout.Button("保存设置", GUILayout.Height(30), GUILayout.MaxWidth(100)))
                {
                    SaveSettings();
                }

                EditorGUILayout.EndHorizontal();
            }
        }

        public override void DrawSettingsPanel()
        {
            EditorGUILayout.BeginHorizontal();
            {
                EditorGUILayout.LabelField("Fallback Texture Format", GUILayout.Width(150));
                compressionPreset.FallbackFormat = (TextureImporterFormat)EditorGUILayout.IntPopup((int)compressionPreset.FallbackFormat, texFormatDisplayOptions, texFormatValues);
                EditorGUILayout.EndHorizontal();
            }

            DrawImporterOverrideRow("Texture Type", ref compressionPreset.OverrideTextureType, () =>
            {
                compressionPreset.ImporterSettings.textureType = (TextureImporterType)EditorGUILayout.EnumPopup(compressionPreset.ImporterSettings.textureType);
            });

            DrawImporterOverrideRow("Sprite Mode", ref compressionPreset.OverrideSpriteMode, () =>
            {
                compressionPreset.ImporterSettings.spriteMode = (int)(SpriteImportMode)EditorGUILayout.EnumPopup((SpriteImportMode)compressionPreset.ImporterSettings.spriteMode);
            });

            DrawImporterOverrideRow("Mesh Type", ref compressionPreset.OverrideMeshType, () =>
            {
                compressionPreset.ImporterSettings.spriteMeshType = (SpriteMeshType)EditorGUILayout.EnumPopup(compressionPreset.ImporterSettings.spriteMeshType);
            });

            DrawImporterOverrideRow("Alpha Is Transparency", ref compressionPreset.OverrideAlphaIsTransparency, () =>
            {
                compressionPreset.ImporterSettings.alphaIsTransparency = EditorGUILayout.ToggleLeft("Enable", compressionPreset.ImporterSettings.alphaIsTransparency);
            });

            DrawImporterOverrideRow("Read/Write", ref compressionPreset.OverrideReadable, () =>
            {
                compressionPreset.ImporterSettings.readable = EditorGUILayout.ToggleLeft("Enable", compressionPreset.ImporterSettings.readable);
            });

            DrawImporterOverrideRow("Generate Mip Maps", ref compressionPreset.OverrideGenerateMipMaps, () =>
            {
                compressionPreset.ImporterSettings.mipmapEnabled = EditorGUILayout.ToggleLeft("Enable", compressionPreset.ImporterSettings.mipmapEnabled);
            });

            DrawImporterOverrideRow("Wrap Mode", ref compressionPreset.OverrideWrapMode, () =>
            {
                compressionPreset.ImporterSettings.wrapMode = (TextureWrapMode)EditorGUILayout.EnumPopup(compressionPreset.ImporterSettings.wrapMode);
            });

            DrawImporterOverrideRow("Filter Mode", ref compressionPreset.OverrideFilterMode, () =>
            {
                compressionPreset.ImporterSettings.filterMode = (FilterMode)EditorGUILayout.EnumPopup(compressionPreset.ImporterSettings.filterMode);
            });

            DrawImporterOverrideRow("Max Size", ref compressionPreset.OverrideMaxSize, () =>
            {
                compressionPreset.PlatformSettings.maxTextureSize = EditorGUILayout.IntPopup(compressionPreset.PlatformSettings.maxTextureSize, maxTextureSizeDisplayOptions, maxTextureSizeOptionValues);
            });

            DrawImporterOverrideRow("Format for RGBA", ref compressionPreset.OverrideFormat, () =>
            {
                compressionPreset.PlatformSettings.format = (TextureImporterFormat)EditorGUILayout.IntPopup((int)compressionPreset.PlatformSettings.format, texFormatDisplayOptions, texFormatValues);
                EditorGUILayout.Space(10);
                EditorGUILayout.LabelField("Format for RGB", GUILayout.Width(100));
                compressionPreset.NoAlphaFormat = (TextureImporterFormat)EditorGUILayout.IntPopup((int)compressionPreset.NoAlphaFormat, texFormatDisplayOptions, texFormatValues);
            });

            DrawImporterOverrideRow("Compresser Quality", ref compressionPreset.OverrideCompressorQuality, () =>
            {
                compressionPreset.PlatformSettings.compressionQuality = EditorGUILayout.IntSlider(compressionPreset.PlatformSettings.compressionQuality, 0, 100);
            });
        }

        public static void InitTextureFormatOptions(out int[] formatValues, out string[] formatDisplayOptions)
        {
            TextureCompressionEditorBridge.InitializeTextureFormatOptions(out formatValues, out formatDisplayOptions);
        }

        private void StartCompressUnityAssetMode()
        {
            var imageList = GetSelectedAssets();
            TextureCompressionApplyService.Apply(imageList, compressionPreset, TexWarningLogFile);
        }

        /// <summary>
        /// [测试功能]自动选择压缩比最大的格式
        /// </summary>
        private void AutoCompressUnityAssetMode()
        {
            var activeBuildTarget = EditorUserBuildSettings.activeBuildTarget;
            if (!texMaxSizePlatforms.TryGetValue(activeBuildTarget, out int maxTextureSize)
                || !texFormatsForPlatforms.TryGetValue(activeBuildTarget, out var targetFormats)
                || !texNoAlphaFormatPlatforms.TryGetValue(activeBuildTarget, out var noAlphaFormat))
            {
                Debug.LogWarning($"当前平台未配置自动压缩参数: {activeBuildTarget}");
                return;
            }

            var fileList = GetSelectedAssets();
            int totalCount = fileList.Count;
            for (int i = 0; i < totalCount; i++)
            {
                var fileName = fileList[i];
                if (EditorUtility.DisplayCancelableProgressBar($"进度({i}/{totalCount})", fileName, i / (float)totalCount))
                {
                    break;
                }

                var texImporter = AssetImporter.GetAtPath(fileName) as TextureImporter;
                if (texImporter == null)
                {
                    continue;
                }

                var texSettings = new TextureImporterSettings();
                texImporter.ReadTextureSettings(texSettings);
                if (texImporter.textureType == TextureImporterType.NormalMap ||
                    !texSettings.alphaIsTransparency ||
                    string.Equals(Path.GetExtension(fileName), ".jpg", StringComparison.OrdinalIgnoreCase))
                {
                    var platformSettings = texImporter.GetPlatformTextureSettings(EditorUserBuildSettings.activeBuildTarget.ToString());
                    platformSettings.overridden = true;
                    platformSettings.format = noAlphaFormat;
                    if (platformSettings.maxTextureSize > maxTextureSize)
                    {
                        platformSettings.maxTextureSize = maxTextureSize;
                    }

                    texImporter.SetPlatformTextureSettings(platformSettings);
                    texImporter.SaveAndReimport();
                    continue;
                }

                long minTextureSize = -1L;
                TextureImporterFormat? minTextureFormat = null;
                for (int formatIndex = 0; formatIndex < targetFormats.Length; formatIndex++)
                {
                    var targetFormat = targetFormats[formatIndex];
                    var platformSettings = texImporter.GetPlatformTextureSettings(EditorUserBuildSettings.activeBuildTarget.ToString());
                    platformSettings.overridden = true;
                    platformSettings.format = targetFormat;
                    if (platformSettings.maxTextureSize > maxTextureSize)
                    {
                        platformSettings.maxTextureSize = maxTextureSize;
                    }

                    texImporter.SetPlatformTextureSettings(platformSettings);
                    texImporter.SaveAndReimport();

                    var texture = AssetDatabase.LoadAssetAtPath<Texture>(fileName);
                    if (texture == null)
                    {
                        continue;
                    }

                    if (!TextureCompressionEditorBridge.TryGetStorageMemorySize(texture, out var textureSize))
                    {
                        Debug.LogWarning("Auto texture compression failed: UnityEditor.TextureUtil.GetStorageMemorySizeLong not found.");
                        EditorUtility.ClearProgressBar();
                        return;
                    }

                    if (minTextureSize < 0 || textureSize < minTextureSize)
                    {
                        minTextureSize = textureSize;
                        minTextureFormat = targetFormat;
                    }
                }

                if (minTextureFormat == null)
                {
                    continue;
                }

                Debug.Log($"---------:贴图:{fileName}, 最小格式:{minTextureFormat.Value}");
                var bestPlatformSettings = texImporter.GetPlatformTextureSettings(EditorUserBuildSettings.activeBuildTarget.ToString());
                if (bestPlatformSettings.format == minTextureFormat.Value)
                {
                    continue;
                }

                bestPlatformSettings.format = minTextureFormat.Value;
                texImporter.SetPlatformTextureSettings(bestPlatformSettings);
                texImporter.SaveAndReimport();
            }

            EditorUtility.ClearProgressBar();
        }

        private static void DrawImporterOverrideRow(string label, ref bool toggle, Action drawContent)
        {
            EditorGUILayout.BeginHorizontal();
            {
                toggle = EditorGUILayout.ToggleLeft(label, toggle, GUILayout.Width(150));
                EditorGUI.BeginDisabledGroup(!toggle);
                {
                    drawContent();
                    EditorGUI.EndDisabledGroup();
                }
                EditorGUILayout.EndHorizontal();
            }
        }
    }
}
