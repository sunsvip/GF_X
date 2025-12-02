using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.U2D;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.U2D;

public partial class ProjectPanelRightClickExtension
{
    [MenuItem("Assets/GF Tools/2D/SpriteAtlas -> TMP_SpriteAsset", priority = 100)]
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
    [MenuItem("Assets/GF Tools/2D/SpriteAtlas -> SpriteSheet", priority = 101)]
    static void SpriteAtlas2SpriteSheetMenu()
    {
        var objs = Selection.objects;
        foreach (var item in objs)
        {
            if (item is SpriteAtlas spAtlas)
            {
                SpriteAtlas2SpriteSheet(spAtlas);
            }
        }
    }
    [MenuItem("Assets/GF Tools/2D/SpriteAtlas -> SpriteGirdSheet", priority = 102)]
    static void SpriteAtlas2SpriteGirdSheetMenu()
    {
        var objs = Selection.objects;
        if (objs.Length == 0) return;
        foreach (var item in objs)
        {
            if (item is SpriteAtlas spAtlas)
            {
                if (spAtlas.spriteCount == 0) continue;
                var sprites = new Sprite[spAtlas.spriteCount];
                spAtlas.GetSprites(sprites);
                System.Array.Sort<Sprite>(sprites, (a, b) => a.name.CompareTo(b.name));
                string srcFileName = AssetDatabase.GetAssetPath(spAtlas);
                string srcFileDir = Path.GetDirectoryName(srcFileName);
                string srcFileNameWithoutExtension = Path.GetFileNameWithoutExtension(srcFileName);
                string textureFileName = UtilityBuiltin.AssetsPath.GetCombinePath(srcFileDir, srcFileNameWithoutExtension + "_girdsheet.png");
                Sprites2SpriteAtlas(sprites, textureFileName);
            }
        }
    }
    public static void Sprites2SpriteAtlas(Sprite[] sprites, string outpathFileName, int row = 1)
    {
        if (sprites == null || sprites.Length == 0 || row < 1) return;
        // 计算最大单元格尺寸
        int cellWidth = 0;
        int cellHeight = 0;
        foreach (Sprite sprite in sprites)
        {
            int width = (int)sprite.rect.width;
            int height = (int)sprite.rect.height;
            if (width > cellWidth) cellWidth = width;
            if (height > cellHeight) cellHeight = height;
        }

        // 计算行列布局
        int cols = Mathf.CeilToInt(sprites.Length / (float)row);
        int atlasWidth = cols * cellWidth;
        int atlasHeight = row * cellHeight;

        // 创建目标纹理
        Texture2D atlasTex = new Texture2D(atlasWidth, atlasHeight, TextureFormat.ARGB32, false);
        ClearTexture(atlasTex);

        List<Texture2D> tempTextures = new List<Texture2D>(); // 存储临时纹理

        // 遍历所有精灵进行排列
        for (int i = 0; i < sprites.Length; i++)
        {
            Sprite sprite = sprites[i];
            Texture2D srcTex = sprite.texture;
            Rect srcRect = sprite.textureRect;
            // 处理不可读纹理
            if (!srcTex.isReadable)
            {
                // 创建临时可读副本
                RenderTexture rt = RenderTexture.GetTemporary(
                    srcTex.width,
                    srcTex.height,
                    0, RenderTextureFormat.ARGB32
                );
                Graphics.Blit(srcTex, rt);

                Texture2D tempTex = new Texture2D(
                    (int)srcTex.width,
                    (int)srcTex.height,
                    TextureFormat.ARGB32,
                    false
                );

                RenderTexture.active = rt;
                tempTex.ReadPixels(new Rect(0, 0, srcTex.width, srcTex.height), 0, 0);
                tempTex.Apply();
                RenderTexture.active = null;
                RenderTexture.ReleaseTemporary(rt);

                srcTex = tempTex;
                tempTextures.Add(tempTex); // 记录以便后续清理
            }

            // 获取源像素数据
            Color[] pixels = srcTex.GetPixels(
                (int)srcRect.x,
                (int)srcRect.y,
                (int)srcRect.width,
                (int)srcRect.height
            );

            // 计算目标位置（居中显示）
            int rowIndex = i / cols;
            int colIndex = i % cols;
            int destX = colIndex * cellWidth + (cellWidth - (int)srcRect.width) / 2;
            int destY = (row - 1 - rowIndex) * cellHeight + (cellHeight - (int)srcRect.height) / 2;

            // 写入像素数据
            atlasTex.SetPixels(destX, destY, (int)srcRect.width, (int)srcRect.height, pixels);
        }

        atlasTex.Apply();

        // 保存文件
        byte[] pngData = atlasTex.EncodeToPNG();
        File.WriteAllBytes(outpathFileName, pngData);

        // 清理资源
        foreach (Texture2D tempTex in tempTextures)
        {
            Object.DestroyImmediate(tempTex);
        }
        Object.DestroyImmediate(atlasTex);
        AssetDatabase.Refresh();
    }
    private static void ClearTexture(Texture2D tex)
    {
        Color[] clearColors = new Color[tex.width * tex.height];
        for (int i = 0; i < clearColors.Length; i++)
        {
            clearColors[i] = Color.clear;
        }
        tex.SetPixels(clearColors);
    }
    public static void SpriteAtlas2SpriteSheet(SpriteAtlas atlas)
    {
        string srcFileName = AssetDatabase.GetAssetPath(atlas);
        string srcFileDir = Path.GetDirectoryName(srcFileName);
        string srcFileNameWithoutExtension = Path.GetFileNameWithoutExtension(srcFileName);
        string textureFileName = UtilityBuiltin.AssetsPath.GetCombinePath(srcFileDir, srcFileNameWithoutExtension + "_sheet.png");
        if (!SpriteAtlas2Texture(atlas, textureFileName, TextureImporterType.Sprite))
        {
            return;
        }
        TextureImporter texImporter = TextureImporter.GetAtPath(textureFileName) as TextureImporter;
        var factory = new SpriteDataProviderFactories();
        factory.Init();
        var dataProvider = factory.GetSpriteEditorDataProviderFromObject(texImporter);
        dataProvider.InitSpriteEditorDataProvider();
        dataProvider.SetSpriteRects(GetSpriteRects(atlas));
        dataProvider.Apply();
        texImporter.SaveAndReimport();
    }
    public static void SpriteAtlas2TmpSprite(SpriteAtlas atlas)
    {
        string srcFileName = AssetDatabase.GetAssetPath(atlas);
        string srcFileDir = Path.GetDirectoryName(srcFileName);
        string srcFileNameWithoutExtension = Path.GetFileNameWithoutExtension(srcFileName);
        string tmpSpriteAssetName = UtilityBuiltin.AssetsPath.GetCombinePath(srcFileDir, srcFileNameWithoutExtension + ".asset");
        string textureFileName = UtilityBuiltin.AssetsPath.GetCombinePath(srcFileDir, srcFileNameWithoutExtension + ".png");
        if (!SpriteAtlas2Texture(atlas, textureFileName, TextureImporterType.Default))
        {
            return;
        }
        var getSpritesFunc = typeof(SpriteAtlasExtensions).GetMethod("GetPackedSprites", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        Sprite[] sprites = getSpritesFunc.Invoke(null, new object[] { atlas }) as Sprite[];
        System.Array.Sort<Sprite>(sprites, (a, b) => a.name.CompareTo(b.name));
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
            Material material = new Material(Shader.Find("TextMeshPro/Sprite"));
            material.mainTexture = spriteAsset.spriteSheet;
            AssetDatabase.AddObjectToAsset(material, spriteAsset);
            AssetDatabase.SaveAssetIfDirty(spriteAsset);
            spriteAsset.material = material;
        }
        var spNameTrim = "(Clone)".Length;
        for (int i = 0; i < sprites.Length; i++)
        {
            var sp = sprites[i];
            var spUVRect = sp.textureRect;
            var glyph = new TMP_SpriteGlyph((uint)i, new UnityEngine.TextCore.GlyphMetrics(spUVRect.width, spUVRect.height, 0, spUVRect.height, spUVRect.width),
            new UnityEngine.TextCore.GlyphRect(spUVRect), 1, 0);
            spriteAsset.spriteGlyphTable.Add(glyph);
            var spChar = new TMP_SpriteCharacter(ToUnicode(i.ToString()), glyph);
            spChar.name = sp.name[..^spNameTrim];
            spriteAsset.spriteCharacterTable.Add(spChar);
        }
        AssetDatabase.SaveAssetIfDirty(spriteAsset);
    }
    static uint ToUnicode(string chars)
    {
        if (char.IsHighSurrogate(chars, 0) && 1 < chars.Length && char.IsLowSurrogate(chars, 1))
        {
            return (uint)char.ConvertToUtf32(chars[0], chars[1]);
        }
        else
        {
            return chars[0];
        }
    }
    /// <summary>
    /// 导出Multiple类型的Sprite为碎图
    /// </summary>
    [MenuItem("Assets/GF Tools/2D/SpriteSheet -> sprites", priority = 101)]
    static void ExportSpriteMultiple()
    {
        int selectAssetsCount = Selection.objects.Length;
        EditorUtility.DisplayProgressBar($"拆分图集(0/{selectAssetsCount})", "Export sprite sheet to sprites...", 0);
        List<string> slicedSpritesAssets = new List<string>();
#if UNITY_2022_3_OR_NEWER
        var texFact = new SpriteDataProviderFactories();
        texFact.Init();
#endif
        for (int i = 0; i < selectAssetsCount; i++)
        {
            var selectObj = Selection.objects[i];
            if (selectObj == null) continue;
            var objType = selectObj.GetType();
            if (objType != typeof(Sprite) && objType != typeof(Texture2D))
            {
                Debug.LogWarning($"导出碎图sprites失败! 你选择的资源不是Sprite或Texture2D类型");
                continue;
            }

            var spFileName = AssetDatabase.GetAssetPath(Selection.objects[i]);
            var spTex = AssetDatabase.LoadAssetAtPath<Texture2D>(spFileName);
            if (spTex == null) continue;

            var texImporter = AssetImporter.GetAtPath(spFileName) as TextureImporter;
            if (texImporter.textureType != TextureImporterType.Sprite || texImporter.spriteImportMode != SpriteImportMode.Multiple)
            {
                Debug.LogWarning($"导出碎图sprites失败! 你选择的资源不是Sprite类型或SpriteMode不是Multiple类型:{spFileName}");
                continue;
            }
            var texReadable = texImporter.isReadable;
            if (!texReadable)
            {
                texImporter.isReadable = true;
                texImporter.SaveAndReimport();
            }
            var outputDir = UtilityBuiltin.AssetsPath.GetCombinePath(Path.GetDirectoryName(spFileName), $"{Path.GetFileNameWithoutExtension(spFileName)}_sliced");

            if (!Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }
#if UNITY_2022_3_OR_NEWER
            var texProvider = texFact.GetSpriteEditorDataProviderFromObject(spTex);
            texProvider.InitSpriteEditorDataProvider();
            var spRects = texProvider.GetSpriteRects();
#else
                var spRects = texImporter.spritesheet;
#endif
            int childrenSpCount = spRects.Length;
            for (int spIndex = 0; spIndex < childrenSpCount; spIndex++)
            {
                var spDt = spRects[spIndex];
                var tex = new Texture2D((int)spDt.rect.width, (int)spDt.rect.height);
                tex.SetPixels(spTex.GetPixels((int)spDt.rect.x, (int)spDt.rect.y, tex.width, tex.height));
                tex.Apply();
                string fileName = UtilityBuiltin.AssetsPath.GetCombinePath(outputDir, $"{spDt.name}.png");
                if (File.Exists(fileName))
                {
                    File.Delete(fileName);
                }
                EditorUtility.DisplayProgressBar($"拆分图集({i + 1}/{selectAssetsCount})", $"导出进度({spIndex}/{childrenSpCount}){System.Environment.NewLine}正在导出碎图{spDt}", (i + 1) / (float)selectAssetsCount);
                File.WriteAllBytes(fileName, tex.EncodeToPNG());
                slicedSpritesAssets.Add(fileName);
            }
            texImporter.isReadable = texReadable;
            texImporter.SaveAndReimport();
        }
        AssetDatabase.Refresh();

        foreach (var item in slicedSpritesAssets)
        {
            var texImporter = AssetImporter.GetAtPath(item) as TextureImporter;
            if (texImporter == null) continue;
            texImporter.textureType = TextureImporterType.Sprite;
            texImporter.spriteImportMode = SpriteImportMode.Single;
            texImporter.alphaIsTransparency = true;
            texImporter.alphaSource = TextureImporterAlphaSource.FromInput;
            texImporter.mipmapEnabled = false;
            texImporter.SaveAndReimport();
        }
        EditorUtility.ClearProgressBar();
    }

    private static bool SpriteAtlas2Texture(SpriteAtlas atlas, string outputFile, TextureImporterType texType = TextureImporterType.Default)
    {
        if (atlas == null || atlas.spriteCount == 0) return false;

        var getPreviewFunc = typeof(UnityEditor.U2D.SpriteAtlasExtensions).GetMethod("GetPreviewTextures", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        if (null == getPreviewFunc) return false;

        Texture2D[] previews = getPreviewFunc.Invoke(null, new object[] { atlas }) as Texture2D[];
        if (previews.Length != 1)
        {
            GFBuiltin.LogError($"SpriteAtlas转换为TMP_Sprite失败: 图集存在{previews.Length}个子图集,请修改MaxTextureSize以确保为单图集");
            return false;
        }

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

        try
        {
            File.WriteAllBytes(outputFile, readableAtlasTex.EncodeToPNG());
        }
        catch (System.Exception e)
        {
            Debug.LogException(e);
            return false;
        }

        AssetDatabase.Refresh();
        TextureImporter texImporter = AssetImporter.GetAtPath(outputFile) as TextureImporter;
        texImporter.textureType = texType;
        if (texType == TextureImporterType.Sprite)
        {
            texImporter.spriteImportMode = SpriteImportMode.Multiple;
            texImporter.isReadable = true;
        }
        texImporter.textureShape = TextureImporterShape.Texture2D;
        texImporter.alphaIsTransparency = true;
        texImporter.SaveAndReimport();
        return true;
    }

    static SpriteRect[] GetSpriteRects(SpriteAtlas atlas)
    {
        if (atlas == null || atlas.spriteCount == 0) return null;
        var getSpritesFunc = typeof(SpriteAtlasExtensions).GetMethod("GetPackedSprites", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        Sprite[] sprites = getSpritesFunc.Invoke(null, new object[] { atlas }) as Sprite[];
        SpriteRect[] spriteRects = new SpriteRect[sprites.Length];
        var spNameTrim = "(Clone)".Length;
        for (int i = 0; i < sprites.Length; i++)
        {
            var sp = sprites[i];
            spriteRects[i] = new SpriteRect()
            {
                name = sp.name[..^spNameTrim],
                rect = sp.textureRect
            };
        }
        return spriteRects;
    }
}
