using GameFramework;

namespace UGF.EditorTools
{
    internal sealed class AtlasVariantSettings : AtlasSettings
    {
        public float variantScale = 0.5f;

        public override void Clear()
        {
            base.Clear();
            variantScale = 0.5f;
        }

        internal static AtlasVariantSettings CreateFrom(AtlasSettings atlasSettings, float scale = 1f)
        {
            var settings = ReferencePool.Acquire<AtlasVariantSettings>();
            settings.includeInBuild = atlasSettings.includeInBuild;
            settings.allowRotation = atlasSettings.allowRotation;
            settings.tightPacking = atlasSettings.tightPacking;
            settings.alphaDilation = atlasSettings.alphaDilation;
            settings.padding = atlasSettings.padding;
            settings.readWrite = atlasSettings.readWrite;
            settings.mipMaps = atlasSettings.mipMaps;
            settings.sRGB = atlasSettings.sRGB;
            settings.filterMode = atlasSettings.filterMode;
            settings.maxTexSize = atlasSettings.maxTexSize;
            settings.texFormat = atlasSettings.texFormat;
            settings.compressQuality = atlasSettings.compressQuality;
            settings.variantScale = scale;
            return settings;
        }
    }
}
