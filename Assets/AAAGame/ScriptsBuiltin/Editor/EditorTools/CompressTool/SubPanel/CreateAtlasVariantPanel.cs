using GameFramework;
using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.U2D;

namespace UGF.EditorTools
{
    [EditorToolMenu("创建图集变体", typeof(CompressToolEditor), 4)]
    public class CreateAtlasVariantPanel : CompressToolSubPanel
    {
        public override string AssetSelectorTypeFilter => "t:spriteatlas t:folder";

        public override string DragAreaTips => "拖拽到此处添加文件夹或SpriteAtlas";

        private Type[] mSupportAssetTypes = { typeof(SpriteAtlas) };
        protected override Type[] SupportAssetTypes => mSupportAssetTypes;
        //图集相关
        AtlasVariantSettings atlasSettings;
        bool generateAtlasVariant = false;
        bool overrideAtlasIncludeInBuild;
        bool overrideAtlasReadWrite;
        bool overrideAtlasMipMaps;
        bool overrideAtlasSRGB;
        bool overrideAtlasFilterMode;
        bool overrideAtlasTexFormat;
        bool overrideAtlasCompressQuality;

        int[] texFormatValues;
        string[] texFormatDisplayOptions;
        public override void OnEnter()
        {
            base.OnEnter();
            if (null == atlasSettings)
            {
                atlasSettings = ReferencePool.Acquire<AtlasVariantSettings>();
            }
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
            EditorGUILayout.BeginHorizontal("box");
            {
                if (GUILayout.Button("创建图集变体", GUILayout.Height(30)))
                {
                    CreateAtlasVariant();
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
            EditorGUILayout.BeginVertical("box");
            {
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
                //Variant Scale
                EditorGUILayout.BeginHorizontal();
                {
                    generateAtlasVariant = EditorGUILayout.ToggleLeft("Scale", generateAtlasVariant, GUILayout.Width(170));
                    EditorGUI.BeginDisabledGroup(!generateAtlasVariant);
                    {
                        atlasSettings.variantScale = EditorGUILayout.Slider(atlasSettings.variantScale, 0, 1f);
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
                        atlasSettings.compressQuality = (int)(TextureCompressionQuality)EditorGUILayout.EnumPopup((TextureCompressionQuality)(atlasSettings.compressQuality ?? (int)TextureCompressionQuality.Normal));
                        EditorGUI.EndDisabledGroup();
                    }
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUILayout.EndVertical();
            }
        }
        private void CreateAtlasVariant()
        {
            var atlasFiles = GetSelectedAssets();
            int totalCount = atlasFiles.Count;
            for (int i = 0; i < totalCount; i++)
            {
                var atlasPath = atlasFiles[i];
                if(EditorUtility.DisplayCancelableProgressBar($"创建图集变体({i}/{totalCount})", atlasPath, i / (float)totalCount))
                {
                    break;
                }
                var atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(atlasPath);
                if (atlas == null) continue;

                CompressTool.CreateAtlasVariant(atlas, GetUserAtlasSettins());
            }
            EditorUtility.ClearProgressBar();
        }
        private AtlasVariantSettings GetUserAtlasSettins()
        {
            if (!overrideAtlasCompressQuality) atlasSettings.compressQuality = null;
            if (!overrideAtlasFilterMode) atlasSettings.filterMode = null;
            if (!overrideAtlasIncludeInBuild) atlasSettings.includeInBuild = null;
            if (!overrideAtlasMipMaps) atlasSettings.mipMaps = null;
            if (!overrideAtlasReadWrite) atlasSettings.readWrite = null;
            if (!overrideAtlasSRGB) atlasSettings.sRGB = null;
            if (!overrideAtlasTexFormat) atlasSettings.texFormat = null;
            return atlasSettings;
        }
    }
}

