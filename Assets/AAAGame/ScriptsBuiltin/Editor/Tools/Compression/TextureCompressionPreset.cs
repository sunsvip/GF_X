using UnityEditor;
using UnityEngine;

namespace UGF.EditorTools
{
    internal sealed class TextureCompressionPreset
    {
        internal bool OverrideTextureType;
        internal bool OverrideSpriteMode;
        internal bool OverrideMeshType;
        internal bool OverrideAlphaIsTransparency;
        internal bool OverrideReadable;
        internal bool OverrideGenerateMipMaps;
        internal bool OverrideWrapMode;
        internal bool OverrideFilterMode;
        internal bool OverrideMaxSize;
        internal bool OverrideFormat;
        internal bool OverrideCompressorQuality;
        internal TextureImporterFormat FallbackFormat;
        internal TextureImporterFormat NoAlphaFormat;
        internal TextureImporterSettings ImporterSettings { get; } = new TextureImporterSettings();
        internal TextureImporterPlatformSettings PlatformSettings { get; } = new TextureImporterPlatformSettings();

        internal static TextureCompressionPreset CreateDefault(int[] textureFormatValues)
        {
            var preset = new TextureCompressionPreset();
            preset.ImporterSettings.spriteMode = (int)SpriteImportMode.Single;
            preset.ImporterSettings.spriteMeshType = SpriteMeshType.FullRect;
            preset.ImporterSettings.alphaIsTransparency = true;
            preset.ImporterSettings.sRGBTexture = true;
            preset.ImporterSettings.alphaSource = TextureImporterAlphaSource.FromInput;
            preset.PlatformSettings.maxTextureSize = 2048;
            preset.PlatformSettings.compressionQuality = 50;
#if UNITY_ANDROID || UNITY_IOS || UNITY_SWITCH
            preset.FallbackFormat = TextureImporterFormat.ASTC_6x6;
            preset.NoAlphaFormat = TextureImporterFormat.ETC_RGB4Crunched;
            preset.PlatformSettings.format = TextureImporterFormat.ETC2_RGBA8Crunched;
#else
            preset.FallbackFormat = TextureImporterFormat.Automatic;
            preset.NoAlphaFormat = TextureImporterFormat.Automatic;
            preset.PlatformSettings.format = textureFormatValues.Length > 0
                ? (TextureImporterFormat)textureFormatValues[0]
                : TextureImporterFormat.Automatic;
#endif
            return preset;
        }
    }
}
