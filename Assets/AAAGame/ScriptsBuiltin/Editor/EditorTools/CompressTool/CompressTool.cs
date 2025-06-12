using UnityEditor.U2D;
using UnityEditor;
using UnityEngine;
using UnityEngine.U2D;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using TinifyAPI;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp;
using GameFramework;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace UGF.EditorTools
{
    public class AtlasSettings : IReference
    {
        public bool? includeInBuild = null;
        public bool? allowRotation = null;
        public bool? tightPacking = null;
        public bool? alphaDilation = null;
        public int? padding = null;
        public bool? readWrite = null;
        public bool? mipMaps = null;
        public bool? sRGB = null;
        public FilterMode? filterMode = null;
        public int? maxTexSize = null;
        public TextureImporterFormat? texFormat = null;
        public int? compressQuality = null;
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
    public class AtlasVariantSettings : AtlasSettings
    {
        public float variantScale = 0.5f;
        public override void Clear()
        {
            base.Clear();
            variantScale = 0.5f;
        }
        public static AtlasVariantSettings CreateFrom(AtlasSettings atlasSettings, float scale = 1f)
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
    public class CompressTool
    {
#if UNITY_EDITOR_WIN
        const string pngquantTool = "Tools/CompressImageTools/pngquant_win/pngquant.exe";
#elif UNITY_EDITOR_OSX
        const string pngquantTool = "Tools/CompressImageTools/pngquant_mac/pngquant";
#endif
        /// <summary>
        /// 使用TinyPng在线压缩,支持png,jpg,webp
        /// </summary>
        public static async Task<bool> CompressOnlineAsync(string imgFileName, string outputFileName, string tinypngKey)
        {
            if (string.IsNullOrWhiteSpace(tinypngKey))
            {
                return false;
            }
            Tinify.Key = tinypngKey;
            var srcImg = TinifyAPI.Tinify.FromFile(imgFileName);
            await srcImg.ToFile(outputFileName);
            return srcImg.IsCompletedSuccessfully;
        }

        /// <summary>
        /// 使用pngquant离线压缩,只支持png
        /// </summary>
        public static bool CompressImageOffline(string imgFileName, string outputFileName)
        {
            var fileExt = Path.GetExtension(imgFileName).ToLower();
            switch (fileExt)
            {
                case ".png":
                    return CompressPngOffline(imgFileName, outputFileName);
                case ".jpg":
                    return CompressJpgOffline(imgFileName, outputFileName);
            }
            return false;
        }
        /// <summary>
        /// 按比例缩放图片尺寸
        /// </summary>
        /// <param name="imgFileName"></param>
        /// <param name="outputFileName"></param>
        /// <param name="scale"></param>
        /// <returns></returns>
        public static bool ResizeImage(string imgFileName, string outputFileName, float scale)
        {
            using (var img = SixLabors.ImageSharp.Image.Load(imgFileName))
            {
                int scaleWidth = (int)(img.Width * scale);
                int scaleHeight = (int)(img.Height * scale);
                img.Mutate(x => x.Resize(scaleWidth, scaleHeight));
                img.Save(outputFileName);
            }
            return true;
        }
        /// <summary>
        /// 设置图片尺寸
        /// </summary>
        /// <param name="imgFileName"></param>
        /// <param name="outputFileName"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        public static bool ResizeImage(string imgFileName, string outputFileName, int width, int height)
        {
            using (var img = SixLabors.ImageSharp.Image.Load(imgFileName))
            {
                img.Mutate(x => x.Resize(width, height));
                img.Save(outputFileName);
            }
            return true;
        }
        /// <summary>
        /// 使用ImageSharp压缩jpg图片
        /// </summary>
        /// <param name="imgFileName"></param>
        /// <param name="outputFileName"></param>
        /// <returns></returns>
        private static bool CompressJpgOffline(string imgFileName, string outputFileName)
        {
            using (var img = SixLabors.ImageSharp.Image.Load(imgFileName))
            {
                var encoder = new SixLabors.ImageSharp.Formats.Jpeg.JpegEncoder()
                {
                    Quality = (int)EditorToolSettings.Instance.CompressImgToolQualityLv
                };
                using (var outputStream = new FileStream(outputFileName, FileMode.Create))
                {
                    img.Save(outputStream, encoder);
                }

            }

            return true;
        }
        /// <summary>
        /// 使用pngquant压缩png图片
        /// </summary>
        /// <param name="imgFileName"></param>
        /// <param name="outputFileName"></param>
        /// <returns></returns>
        private static bool CompressPngOffline(string imgFileName, string outputFileName)
        {
            string pngquant = Path.Combine(Directory.GetParent(Application.dataPath).FullName, pngquantTool);

            StringBuilder strBuilder = new StringBuilder();
            strBuilder.AppendFormat(" --force --quality {0}-{1}", (int)EditorToolSettings.Instance.CompressImgToolQualityMinLv, (int)EditorToolSettings.Instance.CompressImgToolQualityLv);
            strBuilder.AppendFormat(" --speed {0}", EditorToolSettings.Instance.CompressImgToolFastLv);
            strBuilder.AppendFormat(" --output \"{0}\"", outputFileName);
            strBuilder.AppendFormat(" -- \"{0}\"", imgFileName);

            var proceInfo = new System.Diagnostics.ProcessStartInfo(pngquant, strBuilder.ToString());
            proceInfo.CreateNoWindow = true;
            proceInfo.UseShellExecute = false;
            bool success;
            using (var proce = System.Diagnostics.Process.Start(proceInfo))
            {
                proce.WaitForExit();
                success = proce.ExitCode == 0;
                if (!success)
                {
                    Debug.LogWarningFormat("离线压缩图片:{0}失败,ExitCode:{1}", imgFileName, proce.ExitCode);
                }
            }
            return success;
        }

        /// <summary>
        /// 创建图集
        /// </summary>
        /// <param name="atlasFilePath"></param>
        /// <param name="settings"></param>
        /// <param name="objectsForPack"></param>
        /// <param name="createAtlasVariant"></param>
        /// <param name="atlasVariantScale"></param>
        /// <returns></returns>
        public static SpriteAtlas CreateAtlas(string atlasName, AtlasSettings settings, UnityEngine.Object[] objectsForPack, bool createAtlasVariant = false, float atlasVariantScale = 1f)
        {
            CreateEmptySpriteAtlas(atlasName);
            SpriteAtlas result;
            if (EditorSettings.spritePackerMode == SpritePackerMode.SpriteAtlasV2)
            {
                var atlas = SpriteAtlasAsset.Load(atlasName);
#if UNITY_2022_1_OR_NEWER
                var atlasImpt = AssetImporter.GetAtPath(atlasName) as SpriteAtlasImporter;
                atlasImpt.includeInBuild = settings.includeInBuild ?? true;
#else 
                atlas.SetIncludeInBuild(settings.includeInBuild ?? true);
#endif
                atlas.Add(objectsForPack);
#if UNITY_2022_1_OR_NEWER
                var packSettings = atlasImpt.packingSettings;
                var texSettings = atlasImpt.textureSettings;
                var platformSettings = atlasImpt.GetPlatformSettings(EditorUserBuildSettings.activeBuildTarget.ToString());
#else
                var packSettings = atlas.GetPackingSettings();
                var texSettings = atlas.GetTextureSettings();
                var platformSettings = atlas.GetPlatformSettings(EditorUserBuildSettings.activeBuildTarget.ToString());
#endif
                ModifySpriteAtlasSettings(settings, ref packSettings, ref texSettings, ref platformSettings);
#if UNITY_2022_1_OR_NEWER
                atlasImpt.packingSettings = packSettings;
                atlasImpt.textureSettings = texSettings;
                atlasImpt.SetPlatformSettings(platformSettings);
                atlasImpt.SaveAndReimport();
#else
                atlas.SetPackingSettings(packSettings);
                atlas.SetTextureSettings(texSettings);
                atlas.SetPlatformSettings(platformSettings);
                EditorUtility.SetDirty(atlas);
#endif
                result = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(atlasName);
            }
            else
            {
                var atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(atlasName);
                atlas.SetIncludeInBuild(settings.includeInBuild ?? true);
                atlas.Add(objectsForPack);
                var packSettings = atlas.GetPackingSettings();
                var texSettings = atlas.GetTextureSettings();
                var platformSettings = atlas.GetPlatformSettings(EditorUserBuildSettings.activeBuildTarget.ToString());
                ModifySpriteAtlasSettings(settings, ref packSettings, ref texSettings, ref platformSettings);
                atlas.SetPackingSettings(packSettings);
                atlas.SetTextureSettings(texSettings);
                atlas.SetPlatformSettings(platformSettings);
                EditorUtility.SetDirty(atlas);
                result = atlas;
            }

            if (createAtlasVariant)
            {
                var atlasVarSets = new AtlasVariantSettings()
                {
                    variantScale = atlasVariantScale,
                    readWrite = settings.readWrite,
                    mipMaps = settings.mipMaps,
                    sRGB = settings.sRGB,
                    filterMode = settings.filterMode,
                    texFormat = settings.texFormat,
                    compressQuality = settings.compressQuality
                };
                CreateAtlasVariant(result, atlasVarSets);
            }
            return result;
        }
        private static void ModifySpriteAtlasSettings(AtlasSettings input, ref SpriteAtlasPackingSettings packSets, ref SpriteAtlasTextureSettings texSets, ref TextureImporterPlatformSettings platSets)
        {
            packSets.enableRotation = input.allowRotation ?? packSets.enableRotation;
            packSets.enableTightPacking = input.tightPacking ?? packSets.enableTightPacking;
            packSets.enableAlphaDilation = input.alphaDilation ?? packSets.enableAlphaDilation;
            packSets.padding = input.padding ?? packSets.padding;
            texSets.readable = input.readWrite ?? texSets.readable;
            texSets.generateMipMaps = input.mipMaps ?? texSets.generateMipMaps;
            texSets.sRGB = input.sRGB ?? texSets.sRGB;
            texSets.filterMode = input.filterMode ?? texSets.filterMode;
            platSets.overridden = null != input.maxTexSize || null != input.texFormat || null != input.compressQuality;
            platSets.maxTextureSize = input.maxTexSize ?? platSets.maxTextureSize;
            platSets.format = input.texFormat ?? platSets.format;
            platSets.compressionQuality = input.compressQuality ?? platSets.compressionQuality;
        }
        /// <summary>
        /// 根据文件夹名字返回一个图集名
        /// </summary>
        /// <param name="folder"></param>
        /// <returns></returns>
        public static string GetAtlasExtensionV1V2()
        {
            return EditorSettings.spritePackerMode == SpritePackerMode.SpriteAtlasV2 ? ".spriteatlasv2" : ".spriteatlas";
        }
        public static void CreateEmptySpriteAtlas(string atlasAssetName)
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
        /// <summary>
        /// 根据图集对象生成图集变体
        /// </summary>
        /// <param name="atlas"></param>
        /// <param name="settings"></param>
        /// <returns></returns>
        public static SpriteAtlas CreateAtlasVariant(SpriteAtlas atlasMaster, AtlasVariantSettings settings)
        {
            if (atlasMaster == null || atlasMaster.isVariant) return null;
            var atlasFileName = AssetDatabase.GetAssetPath(atlasMaster);
            if (string.IsNullOrEmpty(atlasFileName))
            {
                Debug.LogError($"atlas '{atlasMaster.name}' is not a asset file.");
                return null;
            }

            var atlasVariantName = UtilityBuiltin.AssetsPath.GetCombinePath(Path.GetDirectoryName(atlasFileName), $"{Path.GetFileNameWithoutExtension(atlasFileName)}_Variant{GetAtlasExtensionV1V2()}");
            CreateEmptySpriteAtlas(atlasVariantName);

            SpriteAtlas varAtlas;
            if (EditorSettings.spritePackerMode == SpritePackerMode.SpriteAtlasV2)
            {
                var tmpVarAtlas = SpriteAtlasAsset.Load(atlasVariantName);
#if UNITY_2022_1_OR_NEWER
                var tmpVarAtlasImpt = AssetImporter.GetAtPath(atlasVariantName) as SpriteAtlasImporter;
                tmpVarAtlasImpt.includeInBuild = settings.includeInBuild ?? true;
                var packSettings = tmpVarAtlasImpt.packingSettings;
                var texSettings = tmpVarAtlasImpt.textureSettings;
                var platformSettings = tmpVarAtlasImpt.GetPlatformSettings(EditorUserBuildSettings.activeBuildTarget.ToString());
#else
                tmpVarAtlas.SetIncludeInBuild(true);
                var packSettings = tmpVarAtlas.GetPackingSettings();
                var texSettings = tmpVarAtlas.GetTextureSettings();
                var platformSettings = tmpVarAtlas.GetPlatformSettings(EditorUserBuildSettings.activeBuildTarget.ToString());

#endif
                tmpVarAtlas.SetIsVariant(true);
                tmpVarAtlas.SetMasterAtlas(atlasMaster);

                ModifySpriteAtlasSettings(settings, ref packSettings, ref texSettings, ref platformSettings);
#if UNITY_2022_1_OR_NEWER
                tmpVarAtlasImpt.packingSettings = packSettings;
                tmpVarAtlasImpt.textureSettings = texSettings;
                tmpVarAtlasImpt.variantScale = settings.variantScale;
                tmpVarAtlasImpt.SetPlatformSettings(platformSettings);
                tmpVarAtlasImpt.SaveAndReimport();
#else
                tmpVarAtlas.SetPackingSettings(packSettings);
                tmpVarAtlas.SetTextureSettings(texSettings);
                tmpVarAtlas.SetVariantScale(settings.variantScale);
                tmpVarAtlas.SetPlatformSettings(platformSettings);
#endif
                SpriteAtlasAsset.Save(tmpVarAtlas, atlasVariantName);
                varAtlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(atlasVariantName);
            }
            else
            {
                var tmpVarAtlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(atlasVariantName);
                tmpVarAtlas.SetIncludeInBuild(true);
                tmpVarAtlas.SetIsVariant(true);
                var packSettings = tmpVarAtlas.GetPackingSettings();
                var texSettings = tmpVarAtlas.GetTextureSettings();
                var platformSettings = tmpVarAtlas.GetPlatformSettings(EditorUserBuildSettings.activeBuildTarget.ToString());
                ModifySpriteAtlasSettings(settings, ref packSettings, ref texSettings, ref platformSettings);
                tmpVarAtlas.SetPackingSettings(packSettings);
                tmpVarAtlas.SetTextureSettings(texSettings);
                tmpVarAtlas.SetPlatformSettings(platformSettings);
                tmpVarAtlas.SetMasterAtlas(atlasMaster);
                tmpVarAtlas.SetVariantScale(settings.variantScale);
                EditorUtility.SetDirty(tmpVarAtlas);
                varAtlas = tmpVarAtlas;
            }

            return varAtlas;
        }
        /// <summary>
        /// 根据Atlas文件名为Atlas生成Atlas变体(Atlas Variant)
        /// </summary>
        /// <param name="atlasFile"></param>
        /// <param name="settings"></param>
        /// <returns></returns>
        public static SpriteAtlas CreateAtlasVariant(string atlasFile, AtlasVariantSettings settings)
        {
            var atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(atlasFile);

            return CreateAtlasVariant(atlas, settings);
        }

        /// <summary>
        /// 批量重新打包图集
        /// </summary>
        /// <param name="spriteAtlas"></param>
        public static void PackAtlases(SpriteAtlas[] spriteAtlas)
        {
            SpriteAtlasUtility.PackAtlases(spriteAtlas, EditorUserBuildSettings.activeBuildTarget);
        }

        public static void OptimizeAnimationClips(List<string> list, int precision)
        {
            string pattern = $"(\\d+\\.[\\d]{{{precision},}})";

            int totalCount = list.Count;
            int finishCount = 0;
            foreach (var itmName in list)
            {
                if (File.GetAttributes(itmName) != FileAttributes.ReadOnly)
                {
                    if (Path.GetExtension(itmName).ToLower().CompareTo(".anim") == 0)
                    {
                        finishCount++;
                        if (EditorUtility.DisplayCancelableProgressBar(string.Format("压缩浮点精度({0}/{1})", finishCount, totalCount), itmName, finishCount / (float)totalCount))
                        {
                            break;
                        }
                        var allTxt = File.ReadAllText(itmName);
                        // 将匹配到的浮点型数字替换为精确到3位小数的浮点型数字
                        string outputString = Regex.Replace(allTxt, pattern, match =>
                        float.Parse(match.Value).ToString($"F{precision}"));
                        File.WriteAllText(itmName, outputString);
                        Debug.LogFormat("----->压缩动画浮点精度:{0}", itmName);
                    }
                }
            }
            EditorUtility.ClearProgressBar();
            AssetDatabase.Refresh();
        }
    }
}

