using GameFramework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
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

        private Type[] mSupportAssetTypes = { typeof(Sprite), typeof(Texture2D) };
        protected override Type[] SupportAssetTypes => mSupportAssetTypes;

        int[] texFormatValues;
        string[] texFormatDisplayOptions;
        TextureImporterSettings compressSettings;
        TextureImporterPlatformSettings compressPlatformSettings;

        readonly int[] maxTextureSizeOptionValues = { 32, 64, 128, 256, 512, 1024, 2048, 4096, 8192, 16384 };
        readonly string[] maxTextureSizeDisplayOptions = { "32", "64", "128", "256", "512", "1024", "2048", "4096", "8192", "16384" };
        private bool overrideTextureType;
        private bool overrideSpriteMode;
        private bool overrideMeshType;
        private bool overrideAlphaIsTransparency;
        private bool overrideReadable;
        private bool overrideGenerateMipMaps;
        private bool overrideWrapMode;
        private bool overrideFilterMode;
        //private bool overrideForTarget;
        private bool overrideMaxSize;
        private bool overrideFormat;
        private bool overrideCompresserQuality;

#if UNITY_ANDROID || UNITY_IOS || UNITY_SWITCH
        private TextureImporterFormat fallbackTexFormat = TextureImporterFormat.ASTC_6x6;
        TextureImporterFormat noAlphaTexFormat = TextureImporterFormat.ETC_RGB4Crunched;
#else
        private TextureImporterFormat fallbackTexFormat = TextureImporterFormat.Automatic;
        TextureImporterFormat noAlphaTexFormat = TextureImporterFormat.Automatic;
#endif
        #region 自动选择压缩比最高的压缩格式
        Dictionary<BuildTarget, TextureImporterFormat[]> texFormatsForPlatforms = new Dictionary<BuildTarget, TextureImporterFormat[]>
        {
            [BuildTarget.Android] = new[] { TextureImporterFormat.ETC2_RGBA8Crunched, TextureImporterFormat.ASTC_6x6 },
            [BuildTarget.StandaloneWindows] = new[] { TextureImporterFormat.DXT5Crunched, TextureImporterFormat.DXT5 },
            [BuildTarget.StandaloneWindows64] = new[] { TextureImporterFormat.DXT5Crunched, TextureImporterFormat.DXT5 }
        };
        //无透明通道的贴图压缩格式
        Dictionary<BuildTarget, TextureImporterFormat> texNoAlphaFormatPlatforms = new Dictionary<BuildTarget, TextureImporterFormat>
        {
            [BuildTarget.Android] = TextureImporterFormat.ETC_RGB4Crunched,
            [BuildTarget.StandaloneWindows] = TextureImporterFormat.DXT1Crunched,
            [BuildTarget.StandaloneWindows64] = TextureImporterFormat.DXT1Crunched,
        };
        Dictionary<BuildTarget, int> texMaxSizePlatforms = new Dictionary<BuildTarget, int>
        {
            [BuildTarget.Android] = 2048,
            [BuildTarget.StandaloneWindows] = 4096,
            [BuildTarget.StandaloneWindows64] = 4096
        };

        #endregion
        public override void OnEnter()
        {
            InitTextureFormatOptions(out texFormatValues, out texFormatDisplayOptions);
            compressSettings = new TextureImporterSettings()
            {
                spriteMode = (int)SpriteImportMode.Single,
                spriteMeshType = SpriteMeshType.FullRect,
                alphaIsTransparency = true,
                sRGBTexture = true,
                alphaSource = TextureImporterAlphaSource.FromInput
            };
            compressPlatformSettings = new TextureImporterPlatformSettings()
            {
#if UNITY_ANDROID || UNITY_IOS || UNITY_SWITCH
                format = TextureImporterFormat.ETC2_RGBA8Crunched
#else
                format = (TextureImporterFormat)texFormatValues[0]
#endif
            };
        }

        public override void DrawBottomButtonsPanel()
        {
            EditorGUILayout.BeginHorizontal("box");
            {
                if (GUILayout.Button("开始压缩", GUILayout.Height(30)))
                {
                    StartCompressUnityAssetMode();
                }
                if (GUILayout.Button("压缩警告日志", GUILayout.Height(30), GUILayout.MaxWidth(100)))
                {
                    //StartCompressUnityAssetMode();
                    if (File.Exists(TexWarningLogFile))
                    {
                        EditorUtility.RevealInFinder(TexWarningLogFile);
                    }
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
            //所选压缩格式不支持当前贴图时,默认使用defaultMobileTexFormat
            EditorGUILayout.BeginHorizontal();
            {
                EditorGUILayout.LabelField("Fallback Texture Format", GUILayout.Width(150));
                fallbackTexFormat = (TextureImporterFormat)EditorGUILayout.IntPopup((int)fallbackTexFormat, texFormatDisplayOptions, texFormatValues);

                EditorGUILayout.EndHorizontal();
            }
            //Texture Type
            EditorGUILayout.BeginHorizontal();
            {
                overrideTextureType = EditorGUILayout.ToggleLeft("Texture Type", overrideTextureType, GUILayout.Width(150));
                EditorGUI.BeginDisabledGroup(!overrideTextureType);
                {
                    compressSettings.textureType = (TextureImporterType)EditorGUILayout.EnumPopup(compressSettings.textureType);
                    EditorGUI.EndDisabledGroup();
                }
                EditorGUILayout.EndHorizontal();
            }
            //SpriteMode
            EditorGUILayout.BeginHorizontal();
            {
                overrideSpriteMode = EditorGUILayout.ToggleLeft("Sprite Mode", overrideSpriteMode, GUILayout.Width(150));
                EditorGUI.BeginDisabledGroup(!overrideSpriteMode);
                {
                    compressSettings.spriteMode = (int)(SpriteImportMode)EditorGUILayout.EnumPopup((SpriteImportMode)compressSettings.spriteMode);
                    EditorGUI.EndDisabledGroup();
                }
                EditorGUILayout.EndHorizontal();
            }
            //Sprite Mesh Type
            EditorGUILayout.BeginHorizontal();
            {
                overrideMeshType = EditorGUILayout.ToggleLeft("Mesh Type", overrideMeshType, GUILayout.Width(150));
                EditorGUI.BeginDisabledGroup(!overrideMeshType);
                {
                    compressSettings.spriteMeshType = (SpriteMeshType)EditorGUILayout.EnumPopup(compressSettings.spriteMeshType);
                    EditorGUI.EndDisabledGroup();
                }
                EditorGUILayout.EndHorizontal();
            }
            //Alpha Is Transparency
            EditorGUILayout.BeginHorizontal();
            {
                overrideAlphaIsTransparency = EditorGUILayout.ToggleLeft("Alpha Is Transparency", overrideAlphaIsTransparency, GUILayout.Width(150));
                EditorGUI.BeginDisabledGroup(!overrideAlphaIsTransparency);
                {
                    compressSettings.alphaIsTransparency = EditorGUILayout.ToggleLeft("Enable", compressSettings.alphaIsTransparency);
                    EditorGUI.EndDisabledGroup();
                }
                EditorGUILayout.EndHorizontal();
            }
            //Read/Write
            EditorGUILayout.BeginHorizontal();
            {
                overrideReadable = EditorGUILayout.ToggleLeft("Read/Write", overrideReadable, GUILayout.Width(150));
                EditorGUI.BeginDisabledGroup(!overrideReadable);
                {
                    compressSettings.readable = EditorGUILayout.ToggleLeft("Enable", compressSettings.readable);
                    EditorGUI.EndDisabledGroup();
                }
                EditorGUILayout.EndHorizontal();
            }
            //Generate Mip Maps
            EditorGUILayout.BeginHorizontal();
            {
                overrideGenerateMipMaps = EditorGUILayout.ToggleLeft("Generate Mip Maps", overrideGenerateMipMaps, GUILayout.Width(150));
                EditorGUI.BeginDisabledGroup(!overrideGenerateMipMaps);
                {
                    compressSettings.mipmapEnabled = EditorGUILayout.ToggleLeft("Enable", compressSettings.mipmapEnabled);
                    EditorGUI.EndDisabledGroup();
                }
                EditorGUILayout.EndHorizontal();
            }
            //Wrap Mode
            EditorGUILayout.BeginHorizontal();
            {
                overrideWrapMode = EditorGUILayout.ToggleLeft("Wrap Mode", overrideWrapMode, GUILayout.Width(150));
                EditorGUI.BeginDisabledGroup(!overrideWrapMode);
                {
                    compressSettings.wrapMode = (TextureWrapMode)EditorGUILayout.EnumPopup(compressSettings.wrapMode);
                    EditorGUI.EndDisabledGroup();
                }
                EditorGUILayout.EndHorizontal();
            }
            //Filter Mode
            EditorGUILayout.BeginHorizontal();
            {
                overrideFilterMode = EditorGUILayout.ToggleLeft("Filter Mode", overrideFilterMode, GUILayout.Width(150));
                EditorGUI.BeginDisabledGroup(!overrideFilterMode);
                {
                    compressSettings.filterMode = (UnityEngine.FilterMode)EditorGUILayout.EnumPopup(compressSettings.filterMode);
                    EditorGUI.EndDisabledGroup();
                }
                EditorGUILayout.EndHorizontal();
            }

            //override for current platform
            //EditorGUILayout.BeginHorizontal();
            //{
            //    overrideForTarget = EditorGUILayout.ToggleLeft($"Override For {EditorUserBuildSettings.activeBuildTarget}", overrideForTarget, GUILayout.Width(150));
            //    EditorGUI.BeginDisabledGroup(!overrideForTarget);
            //    {
            //        compressPlatformSettings.overridden = EditorGUILayout.ToggleLeft("Enable", compressPlatformSettings.overridden);
            //        EditorGUI.EndDisabledGroup();
            //    }
            //    EditorGUILayout.EndHorizontal();
            //}
            //Max Size
            EditorGUILayout.BeginHorizontal();
            {
                overrideMaxSize = EditorGUILayout.ToggleLeft("Max Size", overrideMaxSize, GUILayout.Width(150));
                EditorGUI.BeginDisabledGroup(!overrideMaxSize);
                {
                    compressPlatformSettings.maxTextureSize = EditorGUILayout.IntPopup(compressPlatformSettings.maxTextureSize, maxTextureSizeDisplayOptions, maxTextureSizeOptionValues);
                    EditorGUI.EndDisabledGroup();
                }
                EditorGUILayout.EndHorizontal();
            }
            //Format
            EditorGUILayout.BeginHorizontal();
            {
                overrideFormat = EditorGUILayout.ToggleLeft("Format for RGBA", overrideFormat, GUILayout.Width(150));
                EditorGUI.BeginDisabledGroup(!overrideFormat);
                {
                    compressPlatformSettings.format = (TextureImporterFormat)EditorGUILayout.IntPopup((int)compressPlatformSettings.format, texFormatDisplayOptions, texFormatValues);

                    EditorGUILayout.Space(10);
                    EditorGUILayout.LabelField("Format for RGB", GUILayout.Width(100));
                    noAlphaTexFormat = (TextureImporterFormat)EditorGUILayout.IntPopup((int)noAlphaTexFormat, texFormatDisplayOptions, texFormatValues);
                    EditorGUI.EndDisabledGroup();
                }
                EditorGUILayout.EndHorizontal();
            }
            //Compresser Quality
            EditorGUILayout.BeginHorizontal();
            {
                overrideCompresserQuality = EditorGUILayout.ToggleLeft("Compresser Quality", overrideCompresserQuality, GUILayout.Width(150));
                EditorGUI.BeginDisabledGroup(!overrideCompresserQuality);
                {
                    compressPlatformSettings.compressionQuality = EditorGUILayout.IntSlider(compressPlatformSettings.compressionQuality, 0, 100);
                    EditorGUI.EndDisabledGroup();
                }
                EditorGUILayout.EndHorizontal();
            }
        }
        public static void InitTextureFormatOptions(out int[] formatValues, out string[] formatDisplayOptions)
        {
            var getOptionsFunc = Utility.Assembly.GetType("UnityEditor.TextureImportValidFormats").GetMethod("GetPlatformTextureFormatValuesAndStrings", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
            var paramsObjs = new object[] { TextureImporterType.Sprite, EditorUserBuildSettings.activeBuildTarget, null, null };
            getOptionsFunc.Invoke(null, paramsObjs);
            formatValues = paramsObjs[2] as int[];
            formatDisplayOptions = paramsObjs[3] as string[];
        }
        /// <summary>
        /// 将压缩失败的贴图格式设为回滚格式Fallback format
        /// </summary>
        private void FallbackTextureFormat(List<string> imgList)
        {
            if (imgList == null || imgList.Count < 1)
            {
                return;
            }
            AssetDatabase.StartAssetEditing();
            int totalCount = imgList.Count;

            StringBuilder strBuilder = new StringBuilder();
            for (int i = 0; i < totalCount; i++)
            {
                var assetName = imgList[i];
                var texImporter = AssetImporter.GetAtPath(assetName) as TextureImporter;
                if (EditorUtility.DisplayCancelableProgressBar($"压缩失败Fallback({i}/{totalCount})", assetName, i / (float)totalCount))
                {
                    break;
                }
                if (texImporter == null || CheckTexFormatValid(texImporter, out var texWarning)) continue;
                strBuilder.AppendLine($"{assetName}--->{texWarning}");
                var texPlatformSetting = texImporter.GetPlatformTextureSettings(EditorUserBuildSettings.activeBuildTarget.ToString());
                texPlatformSetting.overridden = true;
                texPlatformSetting.format = fallbackTexFormat;
                texImporter.SetPlatformTextureSettings(texPlatformSetting);
                texImporter.SaveAndReimport();
            }
            var warningInfo = strBuilder.ToString();
            if (!string.IsNullOrEmpty(warningInfo))
            {
                File.WriteAllText(TexWarningLogFile, warningInfo);
            }
            EditorUtility.ClearProgressBar();
            AssetDatabase.StopAssetEditing();
            AssetDatabase.Refresh();
        }
        private void StartCompressUnityAssetMode()
        {
            var imgList = GetSelectedAssets();
            if (imgList == null || imgList.Count < 1)
            {
                return;
            }
            AssetDatabase.StartAssetEditing();
            int totalCount = imgList.Count;
            for (int i = 0; i < totalCount; i++)
            {
                var assetName = imgList[i];
                var texImporter = AssetImporter.GetAtPath(assetName) as TextureImporter;
                if (EditorUtility.DisplayCancelableProgressBar($"压缩进度({i}/{totalCount})", assetName, i / (float)totalCount))
                {
                    break;
                }
                if (texImporter == null) continue;
                var texSetting = new TextureImporterSettings();
                texImporter.ReadTextureSettings(texSetting);

                var texPlatformSetting = texImporter.GetPlatformTextureSettings(EditorUserBuildSettings.activeBuildTarget.ToString());

                bool hasChange = false;
                if (overrideTextureType && texSetting.textureType != compressSettings.textureType)
                {
                    texSetting.textureType = compressSettings.textureType;
                    hasChange = true;
                }
                if (overrideSpriteMode && texSetting.spriteMode != compressSettings.spriteMode)
                {
                    texSetting.spriteMode = compressSettings.spriteMode;
                    hasChange = true;
                }
                if (overrideMeshType && texSetting.spriteMeshType != compressSettings.spriteMeshType)
                {
                    texSetting.spriteMeshType = compressSettings.spriteMeshType;
                    hasChange = true;
                }
                if (overrideAlphaIsTransparency && texSetting.alphaIsTransparency != compressSettings.alphaIsTransparency)
                {
                    texSetting.alphaIsTransparency = compressSettings.alphaIsTransparency;
                    hasChange = true;
                }
                if (overrideReadable && texSetting.readable != compressSettings.readable)
                {
                    texSetting.readable = compressSettings.readable;
                    hasChange = true;
                }
                if (overrideGenerateMipMaps && texSetting.mipmapEnabled != compressSettings.mipmapEnabled)
                {
                    texSetting.mipmapEnabled = compressSettings.mipmapEnabled;
                    hasChange = true;
                }
                if (overrideWrapMode && texSetting.wrapMode != compressSettings.wrapMode)
                {
                    texSetting.wrapMode = compressSettings.wrapMode;
                    hasChange = true;
                }
                if (overrideFilterMode && texSetting.filterMode != compressSettings.filterMode)
                {
                    texSetting.filterMode = compressSettings.filterMode;
                    hasChange = true;
                }
                //if (overrideForTarget && texPlatformSetting.overridden != compressPlatformSettings.overridden)
                //{
                //    texPlatformSetting.overridden = compressPlatformSettings.overridden;
                //    hasChange = true;
                //}
                if (overrideMaxSize && texPlatformSetting.maxTextureSize != compressPlatformSettings.maxTextureSize)
                {
                    texPlatformSetting.maxTextureSize = compressPlatformSettings.maxTextureSize;
                    texPlatformSetting.overridden = true;
                    hasChange = true;
                }
                if (overrideFormat)
                {
                    var destFormat = texImporter.DoesSourceTextureHaveAlpha() ? compressPlatformSettings.format : noAlphaTexFormat;
                    hasChange = texPlatformSetting.format != destFormat;
                    if (hasChange)
                    {
                        texPlatformSetting.overridden = true;
                        texPlatformSetting.format = destFormat;
                    }

                }
                if (overrideCompresserQuality && texPlatformSetting.compressionQuality != compressPlatformSettings.compressionQuality)
                {
                    texPlatformSetting.compressionQuality = compressPlatformSettings.compressionQuality;
                    texPlatformSetting.overridden = true;
                    hasChange = true;
                }
                if (hasChange)
                {
                    texImporter.SetTextureSettings(texSetting);
                    texImporter.SetPlatformTextureSettings(texPlatformSetting);
                    texImporter.SaveAndReimport();
                }
            }
            EditorUtility.ClearProgressBar();
            AssetDatabase.StopAssetEditing();
            AssetDatabase.Refresh();

            FallbackTextureFormat(imgList);
        }
        /// <summary>
        /// 检测贴图是否适用压缩格式
        /// </summary>
        /// <param name="texImporter"></param>
        /// <param name="warning"></param>
        /// <returns></returns>
        bool CheckTexFormatValid(TextureImporter texImporter, out string warning)
        {
            var impWarningFunc = texImporter.GetType().GetMethod("GetImportWarnings", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            warning = impWarningFunc.Invoke(texImporter, null) as string;
            return string.IsNullOrWhiteSpace(warning);
        }
        #region [测试功能]自动选择压缩比最大的格式
        /// <summary>
        /// [测试功能]自动选择压缩比最大的格式
        /// </summary>
        private void AutoCompressUnityAssetMode()
        {
            int maxTexSize = texMaxSizePlatforms[EditorUserBuildSettings.activeBuildTarget];
            var targetFormats = texFormatsForPlatforms[EditorUserBuildSettings.activeBuildTarget];
            var noAlphaFormat = texNoAlphaFormatPlatforms[EditorUserBuildSettings.activeBuildTarget];

            var getSizeFunc = Utility.Assembly.GetType("UnityEditor.TextureUtil").GetMethod("GetStorageMemorySizeLong", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

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
                TextureImporterSettings texSettings = new TextureImporterSettings();
                texImporter.ReadTextureSettings(texSettings);
                if (texImporter.textureType == TextureImporterType.NormalMap || !texSettings.alphaIsTransparency || Path.GetExtension(fileName).ToLower().CompareTo(".jpg") == 0)
                {
                    var platformSettings = texImporter.GetPlatformTextureSettings(EditorUserBuildSettings.activeBuildTarget.ToString());
                    platformSettings.overridden = true;
                    platformSettings.format = noAlphaFormat;
                    if (platformSettings.maxTextureSize > maxTexSize) platformSettings.maxTextureSize = maxTexSize;
                    texImporter.SetPlatformTextureSettings(platformSettings);
                    texImporter.SaveAndReimport();
                    continue;
                }
                long minTexSize = -1;
                TextureImporterFormat? minTexFormat = null;
                foreach (var tFormat in targetFormats)
                {
                    var platformSettings = texImporter.GetPlatformTextureSettings(EditorUserBuildSettings.activeBuildTarget.ToString());
                    platformSettings.overridden = true;
                    platformSettings.format = tFormat;
                    if (platformSettings.maxTextureSize > maxTexSize) platformSettings.maxTextureSize = maxTexSize;
                    texImporter.SetPlatformTextureSettings(platformSettings);
                    texImporter.SaveAndReimport();

                    var tex = AssetDatabase.LoadAssetAtPath<Texture>(fileName);
                    var texSize = (long)getSizeFunc.Invoke(null, new object[] { tex });
                    if (minTexSize < 0)
                    {
                        minTexSize = texSize;
                        minTexFormat = tFormat;
                    }

                    if (texSize < minTexSize)
                    {
                        minTexSize = texSize;
                        minTexFormat = tFormat;
                    }
                }
                if (minTexFormat != null)
                {
                    Debug.Log($"---------:贴图:{fileName}, 最小格式:{minTexFormat.Value}");
                    var platformSettings = texImporter.GetPlatformTextureSettings(EditorUserBuildSettings.activeBuildTarget.ToString());
                    if (platformSettings.format != minTexFormat.Value)
                    {
                        platformSettings.format = minTexFormat.Value;
                        texImporter.SetPlatformTextureSettings(platformSettings);
                        texImporter.SaveAndReimport();
                    }
                }
            }
            EditorUtility.ClearProgressBar();
        }
        #endregion
    }
}