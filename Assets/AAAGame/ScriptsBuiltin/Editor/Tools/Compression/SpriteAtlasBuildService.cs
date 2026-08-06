using System.IO;
using GameFramework;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;

namespace UGF.EditorTools
{
    internal static class SpriteAtlasBuildService
    {
        internal static SpriteAtlas CreateAtlas(string atlasName, AtlasSettings settings, Object[] objectsForPack, bool createAtlasVariant = false, float atlasVariantScale = 1f)
        {
            CreateEmptyAtlas(atlasName);
            SpriteAtlas result;
            if (EditorSettings.spritePackerMode == SpritePackerMode.SpriteAtlasV2)
            {
                var atlas = SpriteAtlasAsset.Load(atlasName);
#if UNITY_2022_1_OR_NEWER
                var atlasImporter = AssetImporter.GetAtPath(atlasName) as SpriteAtlasImporter;
                atlasImporter.includeInBuild = settings.includeInBuild ?? true;
#else
                atlas.SetIncludeInBuild(settings.includeInBuild ?? true);
#endif
                atlas.Add(objectsForPack);
#if UNITY_2022_1_OR_NEWER
                var packingSettings = atlasImporter.packingSettings;
                var textureSettings = atlasImporter.textureSettings;
                var platformSettings = atlasImporter.GetPlatformSettings(EditorUserBuildSettings.activeBuildTarget.ToString());
#else
                var packingSettings = atlas.GetPackingSettings();
                var textureSettings = atlas.GetTextureSettings();
                var platformSettings = atlas.GetPlatformSettings(EditorUserBuildSettings.activeBuildTarget.ToString());
#endif
                ApplyAtlasSettings(settings, ref packingSettings, ref textureSettings, ref platformSettings);
#if UNITY_2022_1_OR_NEWER
                atlasImporter.packingSettings = packingSettings;
                atlasImporter.textureSettings = textureSettings;
                atlasImporter.SetPlatformSettings(platformSettings);
                atlasImporter.SaveAndReimport();
#else
                atlas.SetPackingSettings(packingSettings);
                atlas.SetTextureSettings(textureSettings);
                atlas.SetPlatformSettings(platformSettings);
                EditorUtility.SetDirty(atlas);
#endif
                SpriteAtlasAsset.Save(atlas, atlasName);
                result = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(atlasName);
            }
            else
            {
                var atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(atlasName);
                atlas.SetIncludeInBuild(settings.includeInBuild ?? true);
                atlas.Add(objectsForPack);
                var packingSettings = atlas.GetPackingSettings();
                var textureSettings = atlas.GetTextureSettings();
                var platformSettings = atlas.GetPlatformSettings(EditorUserBuildSettings.activeBuildTarget.ToString());
                ApplyAtlasSettings(settings, ref packingSettings, ref textureSettings, ref platformSettings);
                atlas.SetPackingSettings(packingSettings);
                atlas.SetTextureSettings(textureSettings);
                atlas.SetPlatformSettings(platformSettings);
                EditorUtility.SetDirty(atlas);
                result = atlas;
            }

            if (createAtlasVariant)
            {
                var atlasVariantSettings = new AtlasVariantSettings
                {
                    variantScale = atlasVariantScale,
                    readWrite = settings.readWrite,
                    mipMaps = settings.mipMaps,
                    sRGB = settings.sRGB,
                    filterMode = settings.filterMode,
                    texFormat = settings.texFormat,
                    compressQuality = settings.compressQuality
                };
                CreateAtlasVariant(result, atlasVariantSettings);
            }

            return result;
        }

        internal static SpriteAtlas CreateAtlasVariant(SpriteAtlas atlasMaster, AtlasVariantSettings settings)
        {
            if (atlasMaster == null || atlasMaster.isVariant)
            {
                return null;
            }

            var atlasFileName = AssetDatabase.GetAssetPath(atlasMaster);
            if (string.IsNullOrEmpty(atlasFileName))
            {
                Debug.LogError($"atlas '{atlasMaster.name}' is not a asset file.");
                return null;
            }

            var atlasVariantName = UtilityBuiltin.AssetsPath.GetCombinePath(
                Path.GetDirectoryName(atlasFileName),
                $"{Path.GetFileNameWithoutExtension(atlasFileName)}_Variant{GetAtlasExtension()}");
            CreateEmptyAtlas(atlasVariantName);

            SpriteAtlas variantAtlas;
            if (EditorSettings.spritePackerMode == SpritePackerMode.SpriteAtlasV2)
            {
                var temporaryVariantAtlas = SpriteAtlasAsset.Load(atlasVariantName);
#if UNITY_2022_1_OR_NEWER
                var variantImporter = AssetImporter.GetAtPath(atlasVariantName) as SpriteAtlasImporter;
                variantImporter.includeInBuild = settings.includeInBuild ?? true;
                var packingSettings = variantImporter.packingSettings;
                var textureSettings = variantImporter.textureSettings;
                var platformSettings = variantImporter.GetPlatformSettings(EditorUserBuildSettings.activeBuildTarget.ToString());
#else
                temporaryVariantAtlas.SetIncludeInBuild(true);
                var packingSettings = temporaryVariantAtlas.GetPackingSettings();
                var textureSettings = temporaryVariantAtlas.GetTextureSettings();
                var platformSettings = temporaryVariantAtlas.GetPlatformSettings(EditorUserBuildSettings.activeBuildTarget.ToString());
#endif
                temporaryVariantAtlas.SetIsVariant(true);
                temporaryVariantAtlas.SetMasterAtlas(atlasMaster);
                ApplyAtlasSettings(settings, ref packingSettings, ref textureSettings, ref platformSettings);
#if UNITY_2022_1_OR_NEWER
                variantImporter.packingSettings = packingSettings;
                variantImporter.textureSettings = textureSettings;
                variantImporter.variantScale = settings.variantScale;
                variantImporter.SetPlatformSettings(platformSettings);
                variantImporter.SaveAndReimport();
#else
                temporaryVariantAtlas.SetPackingSettings(packingSettings);
                temporaryVariantAtlas.SetTextureSettings(textureSettings);
                temporaryVariantAtlas.SetVariantScale(settings.variantScale);
                temporaryVariantAtlas.SetPlatformSettings(platformSettings);
#endif
                SpriteAtlasAsset.Save(temporaryVariantAtlas, atlasVariantName);
                variantAtlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(atlasVariantName);
            }
            else
            {
                var temporaryVariantAtlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(atlasVariantName);
                temporaryVariantAtlas.SetIncludeInBuild(true);
                temporaryVariantAtlas.SetIsVariant(true);
                var packingSettings = temporaryVariantAtlas.GetPackingSettings();
                var textureSettings = temporaryVariantAtlas.GetTextureSettings();
                var platformSettings = temporaryVariantAtlas.GetPlatformSettings(EditorUserBuildSettings.activeBuildTarget.ToString());
                ApplyAtlasSettings(settings, ref packingSettings, ref textureSettings, ref platformSettings);
                temporaryVariantAtlas.SetPackingSettings(packingSettings);
                temporaryVariantAtlas.SetTextureSettings(textureSettings);
                temporaryVariantAtlas.SetPlatformSettings(platformSettings);
                temporaryVariantAtlas.SetMasterAtlas(atlasMaster);
                temporaryVariantAtlas.SetVariantScale(settings.variantScale);
                EditorUtility.SetDirty(temporaryVariantAtlas);
                variantAtlas = temporaryVariantAtlas;
            }

            return variantAtlas;
        }

        internal static SpriteAtlas CreateAtlasVariant(string atlasFile, AtlasVariantSettings settings)
        {
            var atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(atlasFile);
            return CreateAtlasVariant(atlas, settings);
        }

        internal static void PackAtlases(SpriteAtlas[] spriteAtlases)
        {
            SpriteAtlasUtility.PackAtlases(spriteAtlases, EditorUserBuildSettings.activeBuildTarget);
        }

        internal static string GetAtlasExtension()
        {
            return EditorSettings.spritePackerMode == SpritePackerMode.SpriteAtlasV2 ? ".spriteatlasv2" : ".spriteatlas";
        }

        private static void CreateEmptyAtlas(string atlasAssetName)
        {
            if (EditorSettings.spritePackerMode == SpritePackerMode.SpriteAtlasV2)
            {
                SpriteAtlasAsset.Save(new SpriteAtlasAsset(), atlasAssetName);
            }
            else
            {
                AssetDatabase.CreateAsset(new SpriteAtlas(), atlasAssetName);
            }

            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
        }

        private static void ApplyAtlasSettings(
            AtlasSettings input,
            ref SpriteAtlasPackingSettings packingSettings,
            ref SpriteAtlasTextureSettings textureSettings,
            ref TextureImporterPlatformSettings platformSettings)
        {
            packingSettings.enableRotation = input.allowRotation ?? packingSettings.enableRotation;
            packingSettings.enableTightPacking = input.tightPacking ?? packingSettings.enableTightPacking;
            packingSettings.enableAlphaDilation = input.alphaDilation ?? packingSettings.enableAlphaDilation;
            packingSettings.padding = input.padding ?? packingSettings.padding;
            textureSettings.readable = input.readWrite ?? textureSettings.readable;
            textureSettings.generateMipMaps = input.mipMaps ?? textureSettings.generateMipMaps;
            textureSettings.sRGB = input.sRGB ?? textureSettings.sRGB;
            textureSettings.filterMode = input.filterMode ?? textureSettings.filterMode;
            platformSettings.overridden = input.maxTexSize != null || input.texFormat != null || input.compressQuality != null;
            platformSettings.maxTextureSize = input.maxTexSize ?? platformSettings.maxTextureSize;
            platformSettings.format = input.texFormat ?? platformSettings.format;
            platformSettings.compressionQuality = input.compressQuality ?? platformSettings.compressionQuality;
        }
    }
}
