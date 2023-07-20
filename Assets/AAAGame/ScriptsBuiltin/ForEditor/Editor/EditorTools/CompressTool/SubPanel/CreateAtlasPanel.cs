using GameFramework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace UGF.EditorTools
{
    [EditorToolMenu("创建图集", typeof(CompressToolEditor), 3)]
    public class CreateAtlasPanel : CompressToolSubPanel
    {
        public override string AssetSelectorTypeFilter => "t:folder";

        public override string DragAreaTips => "拖拽到此处添加文件夹";
        public override string ReadmeText => "批量创建图集";
        private Type[] mSupportAssetTypes = { typeof(Sprite), typeof(Texture2D) };
        protected override Type[] SupportAssetTypes => mSupportAssetTypes;

        //图集相关
        AtlasVariantSettings atlasSettings;
        bool generateAtlasVariant = false;
        bool overrideAtlasIncludeInBuild;
        bool overrideAtlasAllowRotation;
        bool overrideAtlasTightPacking;
        bool overrideAtlasAlphaDilation;
        bool overrideAtlasPadding;
        bool overrideAtlasReadWrite;
        bool overrideAtlasMipMaps;
        bool overrideAtlasSRGB;
        bool overrideAtlasFilterMode;
        bool overrideAtlasMaxTexSize;
        bool overrideAtlasTexFormat;
        bool overrideAtlasCompressQuality;
        private bool includeChildrenFoler;
        private int atlasSpriteSizeLimit;//像素在多少之内的图片打进图集
        readonly int[] paddingOptionValues = { 2, 4, 8 };
        readonly string[] paddingDisplayOptions = { "2", "4", "8" };
        readonly int[] maxTextureSizeOptionValues = { 32, 64, 128, 256, 512, 1024, 2048, 4096, 8192 };
        readonly string[] maxTextureSizeDisplayOptions = { "32", "64", "128", "256", "512", "1024", "2048", "4096", "8192" };

        int[] texFormatValues;
        string[] texFormatDisplayOptions;

        public override void OnEnter()
        {
            base.OnEnter();
            if (null == atlasSettings)
            {
                atlasSettings = ReferencePool.Acquire<AtlasVariantSettings>();
            }
            includeChildrenFoler = true;
            atlasSpriteSizeLimit = 512;
            CompressTexturePanel.InitTextureFormatOptions(out texFormatValues, out texFormatDisplayOptions);
        }
        public override void OnExit()
        {
            base.OnExit();
            if (atlasSettings != null)
            {
                ReferencePool.Release(atlasSettings);
            }
        }
        public override void DrawBottomButtonsPanel()
        {
            if (EditorSettings.spritePackerMode == SpritePackerMode.Disabled)
            {
                EditorGUILayout.HelpBox("SpritePackerMode已禁用, 在ProjectSettings中启用后才能使用此功能", MessageType.Error);
            }

            EditorGUI.BeginDisabledGroup(EditorSettings.spritePackerMode == SpritePackerMode.Disabled);
            {
                EditorGUILayout.BeginHorizontal("box");
                {
                    if (GUILayout.Button("创建图集", GUILayout.Height(30)))
                    {
                        CreateAtlas();
                    }

                    if (GUILayout.Button("保存设置", GUILayout.Height(30), GUILayout.MaxWidth(100)))
                    {
                        SaveSettings();
                    }
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUI.EndDisabledGroup();
            }
        }

        public override void DrawSettingsPanel()
        {
            EditorGUILayout.BeginVertical("box");
            {
                EditorGUILayout.BeginHorizontal();
                {
                    includeChildrenFoler = EditorGUILayout.ToggleLeft("包括每个子文件夹", includeChildrenFoler, GUILayout.Width(170));
                    atlasSpriteSizeLimit = EditorGUILayout.IntPopup("过滤图片像素大于:", atlasSpriteSizeLimit, maxTextureSizeDisplayOptions, maxTextureSizeOptionValues);
                    EditorGUILayout.EndHorizontal();
                }

                EditorGUILayout.BeginHorizontal();
                {
                    generateAtlasVariant = EditorGUILayout.ToggleLeft("创建AtlasVariant", generateAtlasVariant, GUILayout.Width(170));
                    EditorGUI.BeginDisabledGroup(!generateAtlasVariant);
                    {
                        EditorGUILayout.LabelField("Variant Scale:", GUILayout.Width(100));
                        atlasSettings.variantScale = EditorGUILayout.Slider(atlasSettings.variantScale, 0, 1f);
                        EditorGUI.EndDisabledGroup();
                    }
                    EditorGUILayout.EndHorizontal();
                }
                //Include In Build
                EditorGUILayout.BeginHorizontal();
                {
                    overrideAtlasIncludeInBuild = EditorGUILayout.ToggleLeft("Include In Build", overrideAtlasIncludeInBuild, GUILayout.Width(170));
                    EditorGUI.BeginDisabledGroup(!overrideAtlasIncludeInBuild);
                    {
                        atlasSettings.includeInBuild = EditorGUILayout.Toggle(atlasSettings.includeInBuild ?? true);
                        EditorGUI.EndDisabledGroup();
                    }
                    EditorGUILayout.EndHorizontal();
                }
                //Allow Rotation
                EditorGUILayout.BeginHorizontal();
                {
                    overrideAtlasAllowRotation = EditorGUILayout.ToggleLeft("Allow Rotation", overrideAtlasAllowRotation, GUILayout.Width(170));
                    EditorGUI.BeginDisabledGroup(!overrideAtlasAllowRotation);
                    {
                        atlasSettings.allowRotation = EditorGUILayout.Toggle(atlasSettings.allowRotation ?? true);
                        EditorGUI.EndDisabledGroup();
                    }
                    EditorGUILayout.EndHorizontal();
                }
                //Tight Packing
                EditorGUILayout.BeginHorizontal();
                {
                    overrideAtlasTightPacking = EditorGUILayout.ToggleLeft("Tight Packing", overrideAtlasTightPacking, GUILayout.Width(170));
                    EditorGUI.BeginDisabledGroup(!overrideAtlasTightPacking);
                    {
                        atlasSettings.tightPacking = EditorGUILayout.Toggle(atlasSettings.tightPacking ?? true);
                        EditorGUI.EndDisabledGroup();
                    }
                    EditorGUILayout.EndHorizontal();
                }
                //Alpha Dilation
                EditorGUILayout.BeginHorizontal();
                {
                    overrideAtlasAlphaDilation = EditorGUILayout.ToggleLeft("Alpha Dilation", overrideAtlasAlphaDilation, GUILayout.Width(170));
                    EditorGUI.BeginDisabledGroup(!overrideAtlasAlphaDilation);
                    {
                        atlasSettings.alphaDilation = EditorGUILayout.Toggle(atlasSettings.alphaDilation ?? false);
                        EditorGUI.EndDisabledGroup();
                    }
                    EditorGUILayout.EndHorizontal();
                }
                //Padding
                EditorGUILayout.BeginHorizontal();
                {
                    overrideAtlasPadding = EditorGUILayout.ToggleLeft("Padding", overrideAtlasPadding, GUILayout.Width(170));
                    EditorGUI.BeginDisabledGroup(!overrideAtlasPadding);
                    {
                        atlasSettings.padding = EditorGUILayout.IntPopup(atlasSettings.padding ?? paddingOptionValues[0], paddingDisplayOptions, paddingOptionValues);
                        EditorGUI.EndDisabledGroup();
                    }
                    EditorGUILayout.EndHorizontal();
                }
                //ReadWrite
                EditorGUILayout.BeginHorizontal();
                {
                    overrideAtlasReadWrite = EditorGUILayout.ToggleLeft("Read/Write", overrideAtlasReadWrite, GUILayout.Width(170));
                    EditorGUI.BeginDisabledGroup(!overrideAtlasReadWrite);
                    {
                        atlasSettings.readWrite = EditorGUILayout.Toggle(atlasSettings.readWrite ?? false);
                        EditorGUI.EndDisabledGroup();
                    }
                    EditorGUILayout.EndHorizontal();
                }
                //mipMaps
                EditorGUILayout.BeginHorizontal();
                {
                    overrideAtlasMipMaps = EditorGUILayout.ToggleLeft("Generate Mip Maps", overrideAtlasMipMaps, GUILayout.Width(170));
                    EditorGUI.BeginDisabledGroup(!overrideAtlasMipMaps);
                    {
                        atlasSettings.mipMaps = EditorGUILayout.Toggle(atlasSettings.mipMaps ?? false);
                        EditorGUI.EndDisabledGroup();
                    }
                    EditorGUILayout.EndHorizontal();
                }
                //sRGB
                EditorGUILayout.BeginHorizontal();
                {
                    overrideAtlasSRGB = EditorGUILayout.ToggleLeft("sRGB", overrideAtlasSRGB, GUILayout.Width(170));
                    EditorGUI.BeginDisabledGroup(!overrideAtlasSRGB);
                    {
                        atlasSettings.sRGB = EditorGUILayout.Toggle(atlasSettings.sRGB ?? true);
                        EditorGUI.EndDisabledGroup();
                    }
                    EditorGUILayout.EndHorizontal();
                }
                //filterMode
                EditorGUILayout.BeginHorizontal();
                {
                    overrideAtlasFilterMode = EditorGUILayout.ToggleLeft("Filter Mode", overrideAtlasFilterMode, GUILayout.Width(170));
                    EditorGUI.BeginDisabledGroup(!overrideAtlasFilterMode);
                    {
                        atlasSettings.filterMode = (FilterMode)EditorGUILayout.EnumPopup(atlasSettings.filterMode ?? FilterMode.Bilinear);
                        EditorGUI.EndDisabledGroup();
                    }
                    EditorGUILayout.EndHorizontal();
                }
                //MaxTextureSize
                EditorGUILayout.BeginHorizontal();
                {
                    overrideAtlasMaxTexSize = EditorGUILayout.ToggleLeft("Max Texture Size", overrideAtlasMaxTexSize, GUILayout.Width(170));
                    EditorGUI.BeginDisabledGroup(!overrideAtlasMaxTexSize);
                    {
                        atlasSettings.maxTexSize = EditorGUILayout.IntPopup(atlasSettings.maxTexSize ?? 2048, maxTextureSizeDisplayOptions, maxTextureSizeOptionValues);
                        EditorGUI.EndDisabledGroup();
                    }
                    EditorGUILayout.EndHorizontal();
                }
                //TextureFormat
                EditorGUILayout.BeginHorizontal();
                {
                    overrideAtlasTexFormat = EditorGUILayout.ToggleLeft("Texture Format", overrideAtlasTexFormat, GUILayout.Width(170));
                    EditorGUI.BeginDisabledGroup(!overrideAtlasTexFormat);
                    {
                        atlasSettings.texFormat = (TextureImporterFormat)EditorGUILayout.IntPopup((int)(atlasSettings.texFormat ?? (TextureImporterFormat)texFormatValues[0]), texFormatDisplayOptions, texFormatValues);
                        EditorGUI.EndDisabledGroup();
                    }
                    EditorGUILayout.EndHorizontal();
                }
                //CompressQuality
                EditorGUILayout.BeginHorizontal();
                {
                    overrideAtlasCompressQuality = EditorGUILayout.ToggleLeft("Compress Quality", overrideAtlasCompressQuality, GUILayout.Width(170));
                    EditorGUI.BeginDisabledGroup(!overrideAtlasCompressQuality);
                    {
                        atlasSettings.compressQuality = EditorGUILayout.IntSlider(atlasSettings.compressQuality ?? 50, 0, 100);
                        EditorGUI.EndDisabledGroup();
                    }
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUILayout.EndVertical();
            }

        }
        /// <summary>
        /// 获取选择的文件夹
        /// </summary>
        /// <returns></returns>
        private List<string> GetSelectedFolders()
        {
            List<string> folders = new List<string>();
            var projectRoot = Directory.GetParent(Application.dataPath).FullName;
            foreach (var item in EditorToolSettings.Instance.CompressImgToolItemList)
            {
                if (item == null) continue;

                var assetPath = AssetDatabase.GetAssetPath(item);
                var itmTp = GetSelectedItemType(assetPath);
                if (itmTp == ItemType.Folder)
                {
                    folders.Add(assetPath);
                    if (includeChildrenFoler)
                    {
                        var dirs = Directory.GetDirectories(assetPath, "*", SearchOption.AllDirectories);
                        foreach (var dir in dirs)
                        {
                            var relativeDir = dir;
                            if (!dir.StartsWith("Assets"))
                            {
                                relativeDir = Path.GetRelativePath(dir, projectRoot);
                            }
                            folders.Add(relativeDir);
                        }
                    }
                }
            }

            return folders.Distinct().ToList();
        }
        private AtlasVariantSettings GetUserAtlasSettins()
        {
            var result = AtlasVariantSettings.CreateFrom(atlasSettings);
            result.variantScale = atlasSettings.variantScale;

            if (!overrideAtlasAllowRotation) result.allowRotation = null;
            if (!overrideAtlasAlphaDilation) result.alphaDilation = null;
            if (!overrideAtlasCompressQuality) result.compressQuality = null;
            if (!overrideAtlasFilterMode) result.filterMode = null;
            if (!overrideAtlasIncludeInBuild) result.includeInBuild = null;
            if (!overrideAtlasMaxTexSize) result.maxTexSize = null;
            if (!overrideAtlasMipMaps) result.mipMaps = null;
            if (!overrideAtlasPadding) result.padding = null;
            if (!overrideAtlasReadWrite) result.readWrite = null;
            if (!overrideAtlasSRGB) result.sRGB = null;
            if (!overrideAtlasTexFormat) result.texFormat = null;
            if (!overrideAtlasTightPacking) result.tightPacking = null;
            return result;
        }
        private void CreateAtlas()
        {
            var getSizeFunc = Utility.Assembly.GetType("UnityEditor.TextureUtil").GetMethod("GetGPUWidth", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

            //创建图集
            var texFolders = GetSelectedFolders();
            int totalCount = texFolders.Count;

            AssetDatabase.StartAssetEditing();
            for (int i = 0; i < totalCount; i++)
            {
                var folder = texFolders[i];
                if(EditorUtility.DisplayCancelableProgressBar($"创建图集({i}/{totalCount})", folder, i / (float)totalCount))
                {
                    break;
                }
                if (!Directory.Exists(folder)) continue;

                var texFiles = Directory.GetFiles(folder, "*.*", SearchOption.TopDirectoryOnly).Where(fName => IsSupportAsset(fName));
                List<UnityEngine.Object> texObjs = new List<UnityEngine.Object>();
                foreach (var file in texFiles)
                {
                    var texObj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(file);
                    if (texObj == null) continue;
                    var tmpTex = texObj as Texture;
                    if (Mathf.Max(tmpTex.width, tmpTex.height) > atlasSpriteSizeLimit)
                    {
                        //宽/高超过限制的贴图不打进图集
                        continue;
                    }
                    texObjs.Add(texObj);
                }
                if (texObjs.Count > 0)
                {
                    string atlasAssetName = Path.Combine(folder, $"{new DirectoryInfo(folder).Name}_Atlas{CompressTool.GetAtlasExtensionV1V2()}");
                    CompressTool.CreateAtlas(atlasAssetName, GetUserAtlasSettins(), texObjs.ToArray(), generateAtlasVariant, atlasSettings.variantScale);
                }
            }
            EditorUtility.ClearProgressBar();
            AssetDatabase.StopAssetEditing();
            AssetDatabase.Refresh();
        }
    }
}

