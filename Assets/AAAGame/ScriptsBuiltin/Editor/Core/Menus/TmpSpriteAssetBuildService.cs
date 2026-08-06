using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.U2D;

namespace UGF.EditorTools
{
    internal static class TmpSpriteAssetBuildService
    {
        internal static void BuildFromAtlas(SpriteAtlas atlas)
        {
            string sourceFileName = AssetDatabase.GetAssetPath(atlas);
            string sourceFileDirectory = Path.GetDirectoryName(sourceFileName);
            string sourceFileNameWithoutExtension = Path.GetFileNameWithoutExtension(sourceFileName);
            string tmpSpriteAssetName = UtilityBuiltin.AssetsPath.GetCombinePath(sourceFileDirectory, sourceFileNameWithoutExtension + ".asset");
            string textureFileName = UtilityBuiltin.AssetsPath.GetCombinePath(sourceFileDirectory, sourceFileNameWithoutExtension + ".png");
            if (!SpriteAtlasTextureExportService.ExportAtlasTexture(atlas, textureFileName, TextureImporterType.Default))
            {
                return;
            }

            var sprites = SpriteAtlasTextureExportService.GetPackedSprites(atlas);
            if (sprites == null || sprites.Length == 0)
            {
                Debug.LogWarning("SpriteAtlas2TmpSprite failed: packed sprite list is empty.");
                return;
            }

            System.Array.Sort(sprites, (a, b) => a.name.CompareTo(b.name));
            TMP_SpriteAsset spriteAsset;
            if (File.Exists(tmpSpriteAssetName))
            {
                spriteAsset = AssetDatabase.LoadAssetAtPath<TMP_SpriteAsset>(tmpSpriteAssetName);
            }
            else
            {
                spriteAsset = ScriptableObject.CreateInstance<TMP_SpriteAsset>();
                AssetDatabase.CreateAsset(spriteAsset, tmpSpriteAssetName);
            }

            spriteAsset.spriteSheet = AssetDatabase.LoadAssetAtPath<Texture2D>(textureFileName);
            spriteAsset.spriteCharacterTable.Clear();
            spriteAsset.spriteGlyphTable.Clear();
            if (spriteAsset.material == null)
            {
                var material = new Material(Shader.Find("TextMeshPro/Sprite"))
                {
                    mainTexture = spriteAsset.spriteSheet
                };
                AssetDatabase.AddObjectToAsset(material, spriteAsset);
                AssetDatabase.SaveAssetIfDirty(spriteAsset);
                spriteAsset.material = material;
            }

            for (int i = 0; i < sprites.Length; i++)
            {
                var sprite = sprites[i];
                var spriteUvRect = sprite.textureRect;
                var glyph = new TMP_SpriteGlyph(
                    (uint)i,
                    new UnityEngine.TextCore.GlyphMetrics(spriteUvRect.width, spriteUvRect.height, 0, spriteUvRect.height, spriteUvRect.width),
                    new UnityEngine.TextCore.GlyphRect(spriteUvRect),
                    1,
                    0);
                spriteAsset.spriteGlyphTable.Add(glyph);
                // 按索引分配 Unicode 私用区 (U+E000 起) 码点, 保证每个 sprite 码点唯一.
                var spriteCharacter = new TMP_SpriteCharacter((uint)(0xE000 + i), glyph)
                {
                    name = StripCloneSuffix(sprite.name)
                };
                spriteAsset.spriteCharacterTable.Add(spriteCharacter);
            }

            AssetDatabase.SaveAssetIfDirty(spriteAsset);
        }

        private static string StripCloneSuffix(string name)
        {
            const string cloneSuffix = "(Clone)";
            return name.EndsWith(cloneSuffix, System.StringComparison.Ordinal)
                ? name.Substring(0, name.Length - cloneSuffix.Length)
                : name;
        }
    }
}
