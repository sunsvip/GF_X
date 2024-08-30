using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using TMPro.EditorUtilities;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.U2D;

public partial class RightClickMenuExtension
{
    [MenuItem("Assets/GF Editor Tool/2D/SpriteAtlas to TMP_Sprite", priority = 1001)]
    static void SpriteAtlas2TmpSpriteMenu()
    {
        var objs = Selection.objects;
        foreach (var item in objs)
        {
            if (item is SpriteAtlas spAtlas)
            {
                SpriteAtlas2TmpSprite(spAtlas);
            }
        }
    }

    public static void SpriteAtlas2TmpSprite(SpriteAtlas atlas)
    {
        if (atlas == null || atlas.spriteCount == 0) return;

        var getPreviewFunc = typeof(UnityEditor.U2D.SpriteAtlasExtensions).GetMethod("GetPreviewTextures", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        if (null == getPreviewFunc) return;

        Texture2D[] previews = getPreviewFunc.Invoke(null, new object[] { atlas }) as Texture2D[];
        if (previews.Length != 1)
        {
            GFBuiltin.LogError($"SpriteAtlas转换为TMP_Sprite失败: 图集存在{previews.Length}个子图集,请修改MaxTextureSize以确保为单图集");
            return;
        }
        string srcFileName = AssetDatabase.GetAssetPath(atlas);
        string srcFileDir = Path.GetDirectoryName(srcFileName);
        string srcFileNameWithoutExtension = Path.GetFileNameWithoutExtension(srcFileName);
        string tmpSpriteAssetName = UtilityBuiltin.AssetsPath.GetCombinePath(srcFileDir, srcFileNameWithoutExtension + ".asset");

        var atlasTex2d = previews[0];
        RenderTexture rt = new RenderTexture(atlasTex2d.width, atlasTex2d.height, 0);
        Graphics.Blit(atlasTex2d, rt);
        RenderTexture.active = rt;

        Texture2D readableAtlasTex = new Texture2D(rt.width, rt.height);
        readableAtlasTex.alphaIsTransparency = true;
        readableAtlasTex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        readableAtlasTex.Apply();
        RenderTexture.active = null;
        rt.Release();

        string textureFileName = UtilityBuiltin.AssetsPath.GetCombinePath(srcFileDir, srcFileNameWithoutExtension + ".png");
        File.WriteAllBytes(textureFileName, readableAtlasTex.EncodeToPNG());

        AssetDatabase.Refresh();
        TextureImporter texImporter = AssetImporter.GetAtPath(textureFileName) as TextureImporter;
        texImporter.textureType = TextureImporterType.Default;
        texImporter.textureShape = TextureImporterShape.Texture2D;
        texImporter.alphaIsTransparency = true;
        texImporter.SaveAndReimport();

        Sprite[] sprites = new Sprite[atlas.spriteCount];
        atlas.GetSprites(sprites);
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
        if (spriteAsset.material == null)
        {
            Material material = new Material(Shader.Find("TextMeshPro/Sprite"));
            material.mainTexture = spriteAsset.spriteSheet;
            AssetDatabase.AddObjectToAsset(material, spriteAsset);
            AssetDatabase.SaveAssetIfDirty(spriteAsset);
            spriteAsset.material = material;
        }
        spriteAsset.spriteCharacterTable.Clear();
        spriteAsset.spriteGlyphTable.Clear();
        for (int i = 0; i < sprites.Length; i++)
        {
            var sp = sprites[i];
            var spUVRect = sp.textureRect;
            var glyph = new TMP_SpriteGlyph((uint)i, new UnityEngine.TextCore.GlyphMetrics(spUVRect.width, spUVRect.height, 0, spUVRect.height, spUVRect.width),
            new UnityEngine.TextCore.GlyphRect(spUVRect), 1, 0);
            spriteAsset.spriteGlyphTable.Add(glyph);

            var spChar = new TMP_SpriteCharacter(0, glyph);
            spriteAsset.spriteCharacterTable.Add(spChar);
        }
        AssetDatabase.SaveAssetIfDirty(spriteAsset);
    }
}
