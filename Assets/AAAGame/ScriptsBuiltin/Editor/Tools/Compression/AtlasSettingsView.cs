using UnityEditor;
using UnityEngine;

namespace UGF.EditorTools
{
    internal static class AtlasSettingsView
    {
        internal static void DrawCreationSettings(
            AtlasCreationPanelState state,
            int[] textureFormatValues,
            string[] textureFormatDisplayOptions,
            int[] paddingOptionValues,
            string[] paddingDisplayOptions,
            int[] maxTextureSizeOptionValues,
            string[] maxTextureSizeDisplayOptions)
        {
            EditorGUILayout.BeginVertical("box");
            {
                EditorGUILayout.BeginHorizontal();
                {
                    state.IncludeChildrenFolders = EditorGUILayout.ToggleLeft("包括每个子文件夹", state.IncludeChildrenFolders, GUILayout.Width(170));
                    state.AtlasSpriteSizeLimit = EditorGUILayout.IntPopup("过滤图片像素大于:", state.AtlasSpriteSizeLimit, maxTextureSizeDisplayOptions, maxTextureSizeOptionValues);
                    EditorGUILayout.EndHorizontal();
                }

                DrawVariantScaleRow(ref state.GenerateAtlasVariant, state.Overrides.Settings, "创建AtlasVariant");
                DrawIncludeInBuildRow(state.Overrides);
                DrawAllowRotationRow(state.Overrides);
                DrawTightPackingRow(state.Overrides);
                DrawAlphaDilationRow(state.Overrides);
                DrawPaddingRow(state.Overrides, paddingOptionValues, paddingDisplayOptions);
                DrawReadWriteRow(state.Overrides);
                DrawMipMapsRow(state.Overrides);
                DrawSrgbRow(state.Overrides);
                DrawFilterModeRow(state.Overrides);
                DrawMaxTextureSizeRow(state.Overrides, maxTextureSizeOptionValues, maxTextureSizeDisplayOptions);
                DrawTextureFormatRow(state.Overrides, textureFormatValues, textureFormatDisplayOptions);
                DrawCompressQualityRow(state.Overrides, false);
                EditorGUILayout.EndVertical();
            }
        }

        internal static void DrawVariantSettings(
            AtlasVariantPanelState state,
            int[] textureFormatValues,
            string[] textureFormatDisplayOptions)
        {
            EditorGUILayout.BeginVertical("box");
            {
                DrawIncludeInBuildRow(state.Overrides);
                DrawVariantScaleRow(ref state.EnableVariantScale, state.Overrides.Settings, "Scale");
                DrawReadWriteRow(state.Overrides);
                DrawMipMapsRow(state.Overrides);
                DrawSrgbRow(state.Overrides);
                DrawFilterModeRow(state.Overrides);
                DrawTextureFormatRow(state.Overrides, textureFormatValues, textureFormatDisplayOptions);
                DrawCompressQualityRow(state.Overrides, true);
                EditorGUILayout.EndVertical();
            }
        }

        private static void DrawVariantScaleRow(ref bool enabled, AtlasVariantSettings settings, string label)
        {
            EditorGUILayout.BeginHorizontal();
            {
                enabled = EditorGUILayout.ToggleLeft(label, enabled, GUILayout.Width(170));
                EditorGUI.BeginDisabledGroup(!enabled);
                {
                    EditorGUILayout.LabelField("Variant Scale:", GUILayout.Width(100));
                    settings.variantScale = EditorGUILayout.Slider(settings.variantScale, 0f, 1f);
                    EditorGUI.EndDisabledGroup();
                }
                EditorGUILayout.EndHorizontal();
            }
        }

        private static void DrawIncludeInBuildRow(AtlasSettingsOverrideState state)
        {
            DrawToggleRow("Include In Build", ref state.OverrideIncludeInBuild, () =>
            {
                state.Settings.includeInBuild = EditorGUILayout.Toggle(state.Settings.includeInBuild ?? true);
            });
        }

        private static void DrawAllowRotationRow(AtlasSettingsOverrideState state)
        {
            DrawToggleRow("Allow Rotation", ref state.OverrideAllowRotation, () =>
            {
                state.Settings.allowRotation = EditorGUILayout.Toggle(state.Settings.allowRotation ?? true);
            });
        }

        private static void DrawTightPackingRow(AtlasSettingsOverrideState state)
        {
            DrawToggleRow("Tight Packing", ref state.OverrideTightPacking, () =>
            {
                state.Settings.tightPacking = EditorGUILayout.Toggle(state.Settings.tightPacking ?? true);
            });
        }

        private static void DrawAlphaDilationRow(AtlasSettingsOverrideState state)
        {
            DrawToggleRow("Alpha Dilation", ref state.OverrideAlphaDilation, () =>
            {
                state.Settings.alphaDilation = EditorGUILayout.Toggle(state.Settings.alphaDilation ?? false);
            });
        }

        private static void DrawPaddingRow(AtlasSettingsOverrideState state, int[] values, string[] displays)
        {
            DrawToggleRow("Padding", ref state.OverridePadding, () =>
            {
                state.Settings.padding = EditorGUILayout.IntPopup(state.Settings.padding ?? values[0], displays, values);
            });
        }

        private static void DrawReadWriteRow(AtlasSettingsOverrideState state)
        {
            DrawToggleRow("Read/Write", ref state.OverrideReadWrite, () =>
            {
                state.Settings.readWrite = EditorGUILayout.Toggle(state.Settings.readWrite ?? false);
            });
        }

        private static void DrawMipMapsRow(AtlasSettingsOverrideState state)
        {
            DrawToggleRow("Generate Mip Maps", ref state.OverrideMipMaps, () =>
            {
                state.Settings.mipMaps = EditorGUILayout.Toggle(state.Settings.mipMaps ?? false);
            });
        }

        private static void DrawSrgbRow(AtlasSettingsOverrideState state)
        {
            DrawToggleRow("sRGB", ref state.OverrideSRGB, () =>
            {
                state.Settings.sRGB = EditorGUILayout.Toggle(state.Settings.sRGB ?? true);
            });
        }

        private static void DrawFilterModeRow(AtlasSettingsOverrideState state)
        {
            DrawToggleRow("Filter Mode", ref state.OverrideFilterMode, () =>
            {
                state.Settings.filterMode = (FilterMode)EditorGUILayout.EnumPopup(state.Settings.filterMode ?? FilterMode.Bilinear);
            });
        }

        private static void DrawMaxTextureSizeRow(AtlasSettingsOverrideState state, int[] values, string[] displays)
        {
            DrawToggleRow("Max Texture Size", ref state.OverrideMaxTextureSize, () =>
            {
                state.Settings.maxTexSize = EditorGUILayout.IntPopup(state.Settings.maxTexSize ?? 2048, displays, values);
            });
        }

        private static void DrawTextureFormatRow(AtlasSettingsOverrideState state, int[] values, string[] displays)
        {
            DrawToggleRow("Texture Format", ref state.OverrideTextureFormat, () =>
            {
                state.Settings.texFormat = (TextureImporterFormat)EditorGUILayout.IntPopup((int)(state.Settings.texFormat ?? (TextureImporterFormat)values[0]), displays, values);
            });
        }

        private static void DrawCompressQualityRow(AtlasSettingsOverrideState state, bool useEnumPopup)
        {
            DrawToggleRow("Compress Quality", ref state.OverrideCompressQuality, () =>
            {
                if (useEnumPopup)
                {
                    state.Settings.compressQuality = (int)(TextureCompressionQuality)EditorGUILayout.EnumPopup((TextureCompressionQuality)(state.Settings.compressQuality ?? (int)TextureCompressionQuality.Normal));
                }
                else
                {
                    state.Settings.compressQuality = EditorGUILayout.IntSlider(state.Settings.compressQuality ?? 50, 0, 100);
                }
            });
        }

        private static void DrawToggleRow(string label, ref bool toggle, System.Action drawValue)
        {
            EditorGUILayout.BeginHorizontal();
            {
                toggle = EditorGUILayout.ToggleLeft(label, toggle, GUILayout.Width(170));
                EditorGUI.BeginDisabledGroup(!toggle);
                {
                    drawValue();
                    EditorGUI.EndDisabledGroup();
                }
                EditorGUILayout.EndHorizontal();
            }
        }
    }
}
