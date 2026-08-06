using GameFramework;
using UnityEditor;
using UnityEngine;

namespace UGF.EditorTools
{
    internal class AtlasSettings : IReference
    {
        public bool? includeInBuild;
        public bool? allowRotation;
        public bool? tightPacking;
        public bool? alphaDilation;
        public int? padding;
        public bool? readWrite;
        public bool? mipMaps;
        public bool? sRGB;
        public FilterMode? filterMode;
        public int? maxTexSize;
        public TextureImporterFormat? texFormat;
        public int? compressQuality;

        public virtual void Clear()
        {
            includeInBuild = null;
            allowRotation = null;
            tightPacking = null;
            alphaDilation = null;
            padding = null;
            readWrite = null;
            mipMaps = null;
            sRGB = null;
            filterMode = null;
            maxTexSize = null;
            texFormat = null;
            compressQuality = null;
        }
    }
}
