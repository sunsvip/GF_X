using System;
using System.Collections.Generic;
using System.IO;
using GameFramework;
using UnityEditor;
using UnityEngine;

namespace UGF.EditorTools
{
    [EditorToolMenu("创建图集", typeof(CompressToolEditor), 3)]
    public class CreateAtlasPanel : CompressToolSubPanel
    {
        public override string AssetSelectorTypeFilter => "t:folder";
        public override string DragAreaTips => "拖拽到此处添加文件夹";
        public override AssetSelectionScope SelectionScope => AssetSelectionScope.FoldersOnly;
        public override string ReadmeText => "批量创建图集";

        private readonly Type[] mSupportAssetTypes = { typeof(Sprite), typeof(Texture2D) };
        private readonly int[] paddingOptionValues = { 2, 4, 8 };
        private readonly string[] paddingDisplayOptions = { "2", "4", "8" };
        private readonly int[] maxTextureSizeOptionValues = { 32, 64, 128, 256, 512, 1024, 2048, 4096, 8192 };
        private readonly string[] maxTextureSizeDisplayOptions = { "32", "64", "128", "256", "512", "1024", "2048", "4096", "8192" };
        private readonly AtlasCreationPanelState state = new AtlasCreationPanelState();

        private int[] texFormatValues;
        private string[] texFormatDisplayOptions;

        protected override Type[] SupportAssetTypes => mSupportAssetTypes;

        public override void OnEnter()
        {
            base.OnEnter();
            state.Initialize();
            TextureCompressionEditorBridge.InitializeTextureFormatOptions(out texFormatValues, out texFormatDisplayOptions);
        }

        public override void OnExit()
        {
            base.OnExit();
            state.Release();
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
            AtlasSettingsView.DrawCreationSettings(
                state,
                texFormatValues,
                texFormatDisplayOptions,
                paddingOptionValues,
                paddingDisplayOptions,
                maxTextureSizeOptionValues,
                maxTextureSizeDisplayOptions);
        }

        private List<string> GetSelectedFolders()
        {
            return AtlasFolderSelectionService.GetSelectedFolders(SelectedObjects, state.IncludeChildrenFolders, GetSelectedItemType);
        }

        private AtlasVariantSettings GetUserAtlasSettins()
        {
            var result = state.Overrides.CreateSnapshot();
            result.variantScale = state.Overrides.Settings.variantScale;
            return result;
        }

        private void CreateAtlas()
        {
            var textureFolders = GetSelectedFolders();
            int totalCount = textureFolders.Count;
            for (int i = 0; i < totalCount; i++)
            {
                var folder = textureFolders[i];
                if (EditorUtility.DisplayCancelableProgressBar($"创建图集({i}/{totalCount})", folder, i / (float)totalCount))
                {
                    break;
                }

                if (!Directory.Exists(folder))
                {
                    continue;
                }

                var textureObjects = AtlasFolderSelectionService.LoadPackObjects(folder, IsSupportAsset, state.AtlasSpriteSizeLimit);
                if (textureObjects.Length <= 0)
                {
                    continue;
                }

                string atlasAssetName = UtilityBuiltin.AssetsPath.GetCombinePath(folder, $"{new DirectoryInfo(folder).Name}_Atlas{SpriteAtlasBuildService.GetAtlasExtension()}");
                var settings = GetUserAtlasSettins();
                try
                {
                    SpriteAtlasBuildService.CreateAtlas(
                        atlasAssetName,
                        settings,
                        textureObjects,
                        state.GenerateAtlasVariant,
                        state.Overrides.Settings.variantScale);
                }
                finally
                {
                    ReferencePool.Release(settings);
                }
            }

            EditorUtility.ClearProgressBar();
            AssetDatabase.Refresh();
        }
    }
}
