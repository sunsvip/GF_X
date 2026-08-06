using System;
using GameFramework;
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

        private readonly Type[] mSupportAssetTypes = { typeof(SpriteAtlas) };
        private readonly AtlasVariantPanelState state = new AtlasVariantPanelState();

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
            AtlasSettingsView.DrawVariantSettings(state, texFormatValues, texFormatDisplayOptions);
        }

        private void CreateAtlasVariant()
        {
            var atlasFiles = GetSelectedAssets();
            int totalCount = atlasFiles.Count;
            for (int i = 0; i < totalCount; i++)
            {
                var atlasPath = atlasFiles[i];
                if (EditorUtility.DisplayCancelableProgressBar($"创建图集变体({i}/{totalCount})", atlasPath, i / (float)totalCount))
                {
                    break;
                }

                var atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(atlasPath);
                if (atlas == null)
                {
                    continue;
                }

                var settings = GetUserAtlasSettins();
                try
                {
                    SpriteAtlasBuildService.CreateAtlasVariant(atlas, settings);
                }
                finally
                {
                    ReferencePool.Release(settings);
                }
            }

            EditorUtility.ClearProgressBar();
        }

        private AtlasVariantSettings GetUserAtlasSettins()
        {
            var result = state.Overrides.CreateSnapshot();
            if (state.EnableVariantScale)
            {
                result.variantScale = state.Overrides.Settings.variantScale;
            }

            return result;
        }
    }
}
