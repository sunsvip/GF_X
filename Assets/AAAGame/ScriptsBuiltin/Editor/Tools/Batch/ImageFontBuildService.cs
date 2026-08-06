using GameFramework;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace UGF.EditorTools
{
    internal static class ImageFontBuildService
    {
        public static bool TryCreateCharacterInfo(
            IList<int> unicodes,
            Texture2D texture,
            UnityEditor.SpriteRect[] spriteRects,
            bool normalizeHeight,
            int fontSize,
            bool useTextMeshProMetrics,
            out CharacterInfo[] characterInfos,
            out int maxFontHeight)
        {
            characterInfos = null;
            maxFontHeight = 0;
            if (unicodes == null || unicodes.Count == 0 || texture == null || spriteRects == null || spriteRects.Length == 0)
            {
                return false;
            }

            var textureSize = new Vector2Int(texture.width, texture.height);
            var sourceToAtlasScale = GetSourceToAtlasScale(texture);
            var count = Mathf.Min(unicodes.Count, spriteRects.Length);
            if (unicodes.Count != spriteRects.Length)
            {
                Debug.LogWarning($"字符数({unicodes.Count})与精灵数({spriteRects.Length})不匹配, 将按较小值{count}截断");
            }

            characterInfos = new CharacterInfo[count];
            for (var i = 0; i < count; i++)
            {
                var spriteRect = ToAtlasRect(spriteRects[i].rect, sourceToAtlasScale);
                if (spriteRect.height > maxFontHeight)
                {
                    maxFontHeight = Mathf.RoundToInt(spriteRect.height);
                }
            }

            for (var i = 0; i < count; i++)
            {
                var spriteRect = ToAtlasRect(spriteRects[i].rect, sourceToAtlasScale);
                var spriteHeight = normalizeHeight ? maxFontHeight : spriteRect.height;
                var spriteRectMax = spriteRect.max;
                if (normalizeHeight)
                {
                    spriteRectMax.y = spriteRect.min.y + spriteHeight;
                }

                var uvMin = spriteRect.min / textureSize;
                var uvMax = spriteRectMax / textureSize;
                var actualFontHeight = useTextMeshProMetrics ? spriteHeight : fontSize;
                var fontScale = useTextMeshProMetrics ? 1f : (spriteHeight > 0 ? fontSize / spriteHeight : 0f);
                var charBearing = useTextMeshProMetrics ? Mathf.RoundToInt(spriteHeight * 1.5f) : 0;

                characterInfos[i] = new CharacterInfo
                {
                    index = unicodes[i],
                    uvBottomLeft = uvMin,
                    uvBottomRight = new Vector2(uvMax.x, uvMin.y),
                    uvTopLeft = new Vector2(uvMin.x, uvMax.y),
                    uvTopRight = uvMax,
                    minX = 0,
                    minY = -Mathf.RoundToInt(actualFontHeight * 0.5f),
                    advance = Mathf.RoundToInt(spriteRect.width * fontScale),
                    glyphWidth = Mathf.RoundToInt(spriteRect.width * fontScale),
                    glyphHeight = Mathf.RoundToInt(actualFontHeight),
                    bearing = charBearing,
                };
            }

            return true;
        }

        public static Texture2D PrepareTextureForImageFont(Texture2D texture)
        {
            if (texture == null)
            {
                return null;
            }

            string assetPath = AssetDatabase.GetAssetPath(texture);
            var importer = TextureImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
            {
                return texture;
            }

            ImageFontAiGenerateService.ConfigureTextureImporterForImageFont(importer);
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
        }

        public static Font BuildLegacyFont(CharacterInfo[] characterInfos, Texture2D texture, string outputFontPath)
        {
            var font = GetOrCreateAsset(outputFontPath, () => new Font(texture.name), "普通艺术字 Font");
            if (font == null)
            {
                return null;
            }

            string outputDirectory = Path.GetDirectoryName(outputFontPath);
            var outputMaterialPath = UtilityBuiltin.AssetsPath.GetCombinePath(outputDirectory, $"{texture.name}.mat");
            var material = GetOrCreateAsset(outputMaterialPath, () => new Material(Shader.Find("UI/Default Font")), "普通艺术字材质");
            if (material == null)
            {
                return null;
            }

            material.shader = Shader.Find("UI/Default Font");
            material.SetTexture("_MainTex", texture);
            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssetIfDirty(material);

            font.material = material;
            font.characterInfo = characterInfos;
            EditorUtility.SetDirty(font);
            AssetDatabase.SaveAssetIfDirty(font);
            return font;
        }

        public static TMP_FontAsset BuildTextMeshProFont(Font baseFont, CharacterInfo[] characterInfos, Texture2D texture, string outputPath, int maxFontHeight)
        {
            var fileName = Path.GetFileNameWithoutExtension(outputPath);
            var fontAsset = GetOrCreateAsset(
                outputPath,
                () => CreateTmpFontAsset(
                    baseFont,
                    maxFontHeight,
                    texture.width,
                    texture.height),
                "TextMeshPro 艺术字 Font");
            if (fontAsset == null)
            {
                return null;
            }

            var atlasTexture = GetOrCreateAtlasTexture(outputPath, fontAsset, texture, Utility.Text.Format("{0}_tex", fileName));
            var material = GetOrCreateFontMaterial(outputPath, fontAsset, atlasTexture, Utility.Text.Format("{0}_mat", fileName));
            fontAsset.atlasPopulationMode = AtlasPopulationMode.Static;
            fontAsset.isMultiAtlasTexturesEnabled = false;
            SetTmpSourceFont(fontAsset, baseFont);

            fontAsset.atlas = atlasTexture;
            fontAsset.material = material;
            fontAsset.atlasTextures = new[] { atlasTexture };
            fontAsset.characterTable.Clear();
            fontAsset.glyphTable.Clear();
            for (var i = 0; i < characterInfos.Length; i++)
            {
                var glyph = CreateGlyph(i, characterInfos[i], atlasTexture.width, atlasTexture.height);
                fontAsset.characterTable.Add(new TMP_Character((uint)characterInfos[i].index, fontAsset, glyph));
                fontAsset.glyphTable.Add(glyph);
            }

            var faceInfo = fontAsset.faceInfo;
            faceInfo.familyName = fileName;
            faceInfo.pointSize = maxFontHeight;
            faceInfo.scale = 1f;
            faceInfo.lineHeight = maxFontHeight;
            faceInfo.ascentLine = maxFontHeight;
            faceInfo.capLine = maxFontHeight;
            faceInfo.meanLine = maxFontHeight * 0.5f;
            faceInfo.baseline = 0;
            faceInfo.descentLine = 0;
            fontAsset.faceInfo = faceInfo;
            SetFontAtlasMetadata(fontAsset, atlasTexture.width, atlasTexture.height);

            var creationSettings = fontAsset.creationSettings;
            creationSettings.referencedFontAssetGUID = string.Empty;
            creationSettings.sourceFontFileGUID = GetAssetGuid(baseFont);
            creationSettings.sourceFontFileName = baseFont == null ? string.Empty : baseFont.name;
            creationSettings.pointSize = maxFontHeight;
            creationSettings.padding = 0;
            creationSettings.atlasWidth = atlasTexture.width;
            creationSettings.atlasHeight = atlasTexture.height;
            creationSettings.renderMode = (int)UnityEngine.TextCore.LowLevel.GlyphRenderMode.SMOOTH;
            fontAsset.creationSettings = creationSettings;
            fontAsset.ReadFontAssetDefinition();

            EditorUtility.SetDirty(atlasTexture);
            EditorUtility.SetDirty(material);
            EditorUtility.SetDirty(fontAsset);
            AssetDatabase.SaveAssetIfDirty(atlasTexture);
            AssetDatabase.SaveAssetIfDirty(material);
            AssetDatabase.SaveAssetIfDirty(fontAsset);
            return fontAsset;
        }

        public static void SelectAsset(Object asset)
        {
            if (asset == null)
            {
                return;
            }

#if UNITY_6000_3_OR_NEWER
            Selection.activeEntityId = asset.GetEntityId();
#else
            Selection.activeInstanceID = asset.GetInstanceID();
#endif
        }

        private static UnityEngine.TextCore.Glyph CreateGlyph(int index, CharacterInfo characterInfo, int atlasWidth, int atlasHeight)
        {
            int glyphRectX = Mathf.RoundToInt(characterInfo.uvBottomLeft.x * atlasWidth);
            int glyphRectY = Mathf.RoundToInt(characterInfo.uvBottomLeft.y * atlasHeight);
            int glyphRectWidth = Mathf.RoundToInt((characterInfo.uvTopRight.x - characterInfo.uvBottomLeft.x) * atlasWidth);
            int glyphRectHeight = Mathf.RoundToInt((characterInfo.uvTopRight.y - characterInfo.uvBottomLeft.y) * atlasHeight);
            return new UnityEngine.TextCore.Glyph(
                (uint)index,
                new UnityEngine.TextCore.GlyphMetrics(
                    characterInfo.glyphWidth,
                    characterInfo.glyphHeight,
                    0,
                    characterInfo.glyphHeight,
                    characterInfo.glyphWidth),
                new UnityEngine.TextCore.GlyphRect(
                    glyphRectX,
                    glyphRectY,
                    glyphRectWidth,
                    glyphRectHeight));
        }

        private static Vector2 GetSourceToAtlasScale(Texture2D texture)
        {
            var importer = TextureImporter.GetAtPath(AssetDatabase.GetAssetPath(texture)) as TextureImporter;
            if (importer == null)
            {
                return Vector2.one;
            }

            importer.GetSourceTextureWidthAndHeight(out int sourceWidth, out int sourceHeight);
            return sourceWidth > 0 && sourceHeight > 0
                ? new Vector2((float)texture.width / sourceWidth, (float)texture.height / sourceHeight)
                : Vector2.one;
        }

        private static Rect ToAtlasRect(Rect sourceRect, Vector2 sourceToAtlasScale)
        {
            return Rect.MinMaxRect(
                Mathf.RoundToInt(sourceRect.xMin * sourceToAtlasScale.x),
                Mathf.RoundToInt(sourceRect.yMin * sourceToAtlasScale.y),
                Mathf.RoundToInt(sourceRect.xMax * sourceToAtlasScale.x),
                Mathf.RoundToInt(sourceRect.yMax * sourceToAtlasScale.y));
        }

        private static TMP_FontAsset CreateTmpFontAsset(Font baseFont, int pointSize, int atlasWidth, int atlasHeight)
        {
            var fontAsset = baseFont == null
                ? null
                : TMP_FontAsset.CreateFontAsset(
                    baseFont,
                    pointSize,
                    0,
                    UnityEngine.TextCore.LowLevel.GlyphRenderMode.SMOOTH,
                    atlasWidth,
                    atlasHeight,
                    AtlasPopulationMode.Static,
                    false);
            if (fontAsset != null)
            {
                return fontAsset;
            }

            fontAsset = ScriptableObject.CreateInstance<TMP_FontAsset>();
            fontAsset.name = baseFont == null ? "ImageFont" : baseFont.name;
            fontAsset.atlasPopulationMode = AtlasPopulationMode.Static;
            fontAsset.isMultiAtlasTexturesEnabled = false;
            return fontAsset;
        }

        private static Texture2D GetOrCreateAtlasTexture(string assetPath, TMP_FontAsset fontAsset, Texture2D sourceTexture, string atlasName)
        {
            var atlasTexture = LoadSubAsset<Texture2D>(assetPath, atlasName);
            if (atlasTexture == null)
            {
                atlasTexture = Object.Instantiate(sourceTexture);
                atlasTexture.name = atlasName;
                AssetDatabase.AddObjectToAsset(atlasTexture, fontAsset);
            }
            else
            {
                atlasTexture.Reinitialize(sourceTexture.width, sourceTexture.height, TextureFormat.RGBA32, false);
                atlasTexture.SetPixels32(sourceTexture.GetPixels32());
                atlasTexture.Apply(false, false);
            }

            atlasTexture.alphaIsTransparency = true;
            return atlasTexture;
        }

        private static Material GetOrCreateFontMaterial(string assetPath, TMP_FontAsset fontAsset, Texture2D atlasTexture, string materialName)
        {
            var material = LoadSubAsset<Material>(assetPath, materialName);
            if (material == null)
            {
                material = new Material(Shader.Find("TextMeshPro/Bitmap Custom Atlas"))
                {
                    name = materialName
                };
                AssetDatabase.AddObjectToAsset(material, fontAsset);
            }

            material.shader = Shader.Find("TextMeshPro/Bitmap Custom Atlas");
            material.mainTexture = atlasTexture;
            material.SetFloat(ShaderUtilities.ID_TextureWidth, atlasTexture.width);
            material.SetFloat(ShaderUtilities.ID_TextureHeight, atlasTexture.height);
            return material;
        }

        private static T LoadSubAsset<T>(string assetPath, string assetName) where T : Object
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            for (int i = 0; i < assets.Length; i++)
            {
                if (assets[i] is T asset && string.Equals(asset.name, assetName, System.StringComparison.Ordinal))
                {
                    return asset;
                }
            }

            return null;
        }

        private static T GetOrCreateAsset<T>(string assetPath, System.Func<T> createAsset, string assetLabel) where T : Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
            if (asset != null)
            {
                return asset;
            }

            var existingAsset = AssetDatabase.LoadMainAssetAtPath(assetPath);
            if (existingAsset != null || File.Exists(assetPath))
            {
                Debug.LogError($"{assetLabel} 生成失败: 目标路径已存在但不是 {typeof(T).Name}, 为避免引用丢失不会覆盖该文件: {assetPath}");
                return null;
            }

            asset = createAsset();
            if (asset == null)
            {
                Debug.LogError($"{assetLabel} 生成失败: 资源创建返回空值: {assetPath}");
                return null;
            }

            AssetDatabase.CreateAsset(asset, assetPath);
            return asset;
        }

        private static void SetFontAtlasMetadata(TMP_FontAsset fontAsset, int atlasWidth, int atlasHeight)
        {
            var serializedObject = new SerializedObject(fontAsset);
            serializedObject.FindProperty("m_FaceInfo").FindPropertyRelative("m_UnitsPerEM").intValue = 1000;
            serializedObject.FindProperty("m_AtlasWidth").intValue = atlasWidth;
            serializedObject.FindProperty("m_AtlasHeight").intValue = atlasHeight;
            serializedObject.FindProperty("m_AtlasPadding").intValue = 0;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetTmpSourceFont(TMP_FontAsset fontAsset, Font baseFont)
        {
            var serializedObject = new SerializedObject(fontAsset);
            serializedObject.FindProperty("m_SourceFontFileGUID").stringValue = GetAssetGuid(baseFont);
            serializedObject.FindProperty("m_SourceFontFile_EditorRef").objectReferenceValue = baseFont;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static string GetAssetGuid(Object asset)
        {
            if (asset == null)
            {
                return string.Empty;
            }

            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(asset, out string guid, out long _);
            return guid;
        }
    }
}
