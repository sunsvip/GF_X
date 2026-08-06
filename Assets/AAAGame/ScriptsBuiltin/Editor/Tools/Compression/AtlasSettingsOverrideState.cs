using GameFramework;

namespace UGF.EditorTools
{
    internal sealed class AtlasSettingsOverrideState
    {
        internal AtlasVariantSettings Settings;
        internal bool OverrideIncludeInBuild;
        internal bool OverrideAllowRotation;
        internal bool OverrideTightPacking;
        internal bool OverrideAlphaDilation;
        internal bool OverridePadding;
        internal bool OverrideReadWrite;
        internal bool OverrideMipMaps;
        internal bool OverrideSRGB;
        internal bool OverrideFilterMode;
        internal bool OverrideMaxTextureSize;
        internal bool OverrideTextureFormat;
        internal bool OverrideCompressQuality;

        internal void Initialize()
        {
            Settings ??= ReferencePool.Acquire<AtlasVariantSettings>();
        }

        internal void Release()
        {
            if (Settings == null)
            {
                return;
            }

            ReferencePool.Release(Settings);
            Settings = null;
        }

        internal AtlasVariantSettings CreateSnapshot()
        {
            var result = AtlasVariantSettings.CreateFrom(Settings);
            if (!OverrideAllowRotation) result.allowRotation = null;
            if (!OverrideAlphaDilation) result.alphaDilation = null;
            if (!OverrideCompressQuality) result.compressQuality = null;
            if (!OverrideFilterMode) result.filterMode = null;
            if (!OverrideIncludeInBuild) result.includeInBuild = null;
            if (!OverrideMaxTextureSize) result.maxTexSize = null;
            if (!OverrideMipMaps) result.mipMaps = null;
            if (!OverridePadding) result.padding = null;
            if (!OverrideReadWrite) result.readWrite = null;
            if (!OverrideSRGB) result.sRGB = null;
            if (!OverrideTextureFormat) result.texFormat = null;
            if (!OverrideTightPacking) result.tightPacking = null;
            return result;
        }
    }
}
