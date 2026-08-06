using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.U2D;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.U2D;

namespace UGF.EditorTools
{
    internal static class SpriteAtlasTextureExportService
    {
        private static readonly System.Reflection.MethodInfo s_GetPackedSpritesMethod =
            typeof(SpriteAtlasExtensions).GetMethod("GetPackedSprites", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        private static readonly System.Reflection.MethodInfo s_GetPreviewTexturesMethod =
            typeof(UnityEditor.U2D.SpriteAtlasExtensions).GetMethod("GetPreviewTextures", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        internal static void ExportGridSheet(SpriteAtlas atlas)
        {
            if (atlas == null || atlas.spriteCount == 0)
            {
                return;
            }

            var sprites = new Sprite[atlas.spriteCount];
            atlas.GetSprites(sprites);
            System.Array.Sort(sprites, (a, b) => a.name.CompareTo(b.name));
            string sourceFileName = AssetDatabase.GetAssetPath(atlas);
            string sourceFileDirectory = Path.GetDirectoryName(sourceFileName);
            string sourceFileNameWithoutExtension = Path.GetFileNameWithoutExtension(sourceFileName);
            string textureFileName = UtilityBuiltin.AssetsPath.GetCombinePath(sourceFileDirectory, sourceFileNameWithoutExtension + "_girdsheet.png");
            BuildTextureSheet(sprites, textureFileName);
        }

        internal static void BuildTextureSheet(Sprite[] sprites, string outputFileName, int row = 1)
        {
            if (sprites == null || sprites.Length == 0 || row < 1)
            {
                return;
            }

            int cellWidth = 0;
            int cellHeight = 0;
            for (int i = 0; i < sprites.Length; i++)
            {
                var sprite = sprites[i];
                int width = (int)sprite.rect.width;
                int height = (int)sprite.rect.height;
                if (width > cellWidth) cellWidth = width;
                if (height > cellHeight) cellHeight = height;
            }

            int cols = Mathf.CeilToInt(sprites.Length / (float)row);
            int atlasWidth = cols * cellWidth;
            int atlasHeight = row * cellHeight;

            var atlasTexture = new Texture2D(atlasWidth, atlasHeight, TextureFormat.ARGB32, false);
            ClearTexture(atlasTexture);
            var tempTextures = new List<Texture2D>();

            for (int i = 0; i < sprites.Length; i++)
            {
                var sprite = sprites[i];
                Texture2D sourceTexture = sprite.texture;
                var sourceRect = sprite.textureRect;
                if (!sourceTexture.isReadable)
                {
                    sourceTexture = CreateReadableTexture(sourceTexture);
                    tempTextures.Add(sourceTexture);
                }

                var pixels = sourceTexture.GetPixels((int)sourceRect.x, (int)sourceRect.y, (int)sourceRect.width, (int)sourceRect.height);
                int rowIndex = i / cols;
                int colIndex = i % cols;
                int destinationX = colIndex * cellWidth + (cellWidth - (int)sourceRect.width) / 2;
                int destinationY = (row - 1 - rowIndex) * cellHeight + (cellHeight - (int)sourceRect.height) / 2;
                atlasTexture.SetPixels(destinationX, destinationY, (int)sourceRect.width, (int)sourceRect.height, pixels);
            }

            atlasTexture.Apply();
            File.WriteAllBytes(outputFileName, atlasTexture.EncodeToPNG());

            for (int i = 0; i < tempTextures.Count; i++)
            {
                Object.DestroyImmediate(tempTextures[i]);
            }

            Object.DestroyImmediate(atlasTexture);
            AssetDatabase.Refresh();
        }

        internal static void ExportSpriteSheet(SpriteAtlas atlas)
        {
            string sourceFileName = AssetDatabase.GetAssetPath(atlas);
            string sourceFileDirectory = Path.GetDirectoryName(sourceFileName);
            string sourceFileNameWithoutExtension = Path.GetFileNameWithoutExtension(sourceFileName);
            string textureFileName = UtilityBuiltin.AssetsPath.GetCombinePath(sourceFileDirectory, sourceFileNameWithoutExtension + "_sheet.png");
            if (!ExportAtlasTexture(atlas, textureFileName, TextureImporterType.Sprite))
            {
                return;
            }

            var textureImporter = TextureImporter.GetAtPath(textureFileName) as TextureImporter;
            var factory = new SpriteDataProviderFactories();
            factory.Init();
            var dataProvider = factory.GetSpriteEditorDataProviderFromObject(textureImporter);
            if (dataProvider == null)
            {
                Debug.LogWarning("SpriteAtlas2SpriteSheet failed: sprite editor data provider is null.");
                return;
            }

            dataProvider.InitSpriteEditorDataProvider();
            var spriteRects = GetSpriteRects(atlas);
            if (spriteRects == null || spriteRects.Length == 0)
            {
                Debug.LogWarning("SpriteAtlas2SpriteSheet failed: sprite rect list is empty.");
                return;
            }

            dataProvider.SetSpriteRects(spriteRects);
            dataProvider.Apply();
            textureImporter.SaveAndReimport();
        }

        internal static void ExportMultipleSprites(Object[] selectedObjects)
        {
            int selectedAssetsCount = selectedObjects.Length;
            EditorUtility.DisplayProgressBar($"拆分图集(0/{selectedAssetsCount})", "Export sprite sheet to sprites...", 0);
            var slicedSpriteAssets = new List<string>();
#if UNITY_2022_3_OR_NEWER
            var textureFactory = new SpriteDataProviderFactories();
            textureFactory.Init();
#endif
            for (int i = 0; i < selectedAssetsCount; i++)
            {
                var selectedObject = selectedObjects[i];
                if (selectedObject == null)
                {
                    continue;
                }

                var objectType = selectedObject.GetType();
                if (objectType != typeof(Sprite) && objectType != typeof(Texture2D))
                {
                    Debug.LogWarning("导出碎图sprites失败! 你选择的资源不是Sprite或Texture2D类型");
                    continue;
                }

                var spriteFileName = AssetDatabase.GetAssetPath(selectedObject);
                var spriteTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(spriteFileName);
                if (spriteTexture == null)
                {
                    continue;
                }

                var textureImporter = AssetImporter.GetAtPath(spriteFileName) as TextureImporter;
                if (textureImporter.textureType != TextureImporterType.Sprite || textureImporter.spriteImportMode != SpriteImportMode.Multiple)
                {
                    Debug.LogWarning($"导出碎图sprites失败! 你选择的资源不是Sprite类型或SpriteMode不是Multiple类型:{spriteFileName}");
                    continue;
                }

                bool textureReadable = textureImporter.isReadable;
                if (!textureReadable)
                {
                    textureImporter.isReadable = true;
                    textureImporter.SaveAndReimport();
                }

                var outputDirectory = UtilityBuiltin.AssetsPath.GetCombinePath(Path.GetDirectoryName(spriteFileName), $"{Path.GetFileNameWithoutExtension(spriteFileName)}_sliced");
                if (!Directory.Exists(outputDirectory))
                {
                    Directory.CreateDirectory(outputDirectory);
                }

#if UNITY_2022_3_OR_NEWER
                var textureProvider = textureFactory.GetSpriteEditorDataProviderFromObject(spriteTexture);
                textureProvider.InitSpriteEditorDataProvider();
                var spriteRects = textureProvider.GetSpriteRects();
#else
                var spriteRects = textureImporter.spritesheet;
#endif
                int childrenSpriteCount = spriteRects.Length;
                for (int spriteIndex = 0; spriteIndex < childrenSpriteCount; spriteIndex++)
                {
                    var spriteData = spriteRects[spriteIndex];
                    var texture = new Texture2D((int)spriteData.rect.width, (int)spriteData.rect.height);
                    texture.SetPixels(spriteTexture.GetPixels((int)spriteData.rect.x, (int)spriteData.rect.y, texture.width, texture.height));
                    texture.Apply();
                    string fileName = UtilityBuiltin.AssetsPath.GetCombinePath(outputDirectory, $"{spriteData.name}.png");
                    if (File.Exists(fileName))
                    {
                        File.Delete(fileName);
                    }

                    EditorUtility.DisplayProgressBar($"拆分图集({i + 1}/{selectedAssetsCount})", $"导出进度({spriteIndex}/{childrenSpriteCount}){System.Environment.NewLine}正在导出碎图{spriteData}", (i + 1) / (float)selectedAssetsCount);
                    File.WriteAllBytes(fileName, texture.EncodeToPNG());
                    slicedSpriteAssets.Add(fileName);
                }

                textureImporter.isReadable = textureReadable;
                textureImporter.SaveAndReimport();
            }

            AssetDatabase.Refresh();
            for (int i = 0; i < slicedSpriteAssets.Count; i++)
            {
                var textureImporter = AssetImporter.GetAtPath(slicedSpriteAssets[i]) as TextureImporter;
                if (textureImporter == null)
                {
                    continue;
                }

                textureImporter.textureType = TextureImporterType.Sprite;
                textureImporter.spriteImportMode = SpriteImportMode.Single;
                textureImporter.alphaIsTransparency = true;
                textureImporter.alphaSource = TextureImporterAlphaSource.FromInput;
                textureImporter.mipmapEnabled = false;
                textureImporter.SaveAndReimport();
            }

            EditorUtility.ClearProgressBar();
        }

        internal static bool ExportAtlasTexture(SpriteAtlas atlas, string outputFile, TextureImporterType textureImporterType = TextureImporterType.Default)
        {
            if (atlas == null || atlas.spriteCount == 0)
            {
                return false;
            }

            if (s_GetPreviewTexturesMethod == null)
            {
                return false;
            }

            var previews = s_GetPreviewTexturesMethod.Invoke(null, new object[] { atlas }) as Texture2D[];
            if (previews == null || previews.Length == 0)
            {
                Debug.LogWarning("SpriteAtlas2Texture failed: preview texture list is empty.");
                return false;
            }

            if (previews.Length != 1)
            {
                GFBuiltin.LogError($"SpriteAtlas转换为TMP_Sprite失败: 图集存在{previews.Length}个子图集,请修改MaxTextureSize以确保为单图集");
                return false;
            }

            var atlasTexture = previews[0];
            var renderTexture = new RenderTexture(atlasTexture.width, atlasTexture.height, 0);
            var previousActive = RenderTexture.active;
            Texture2D readableAtlasTexture = null;
            try
            {
                Graphics.Blit(atlasTexture, renderTexture);
                RenderTexture.active = renderTexture;

                readableAtlasTexture = new Texture2D(renderTexture.width, renderTexture.height)
                {
                    alphaIsTransparency = true
                };
                readableAtlasTexture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
                readableAtlasTexture.Apply();
                File.WriteAllBytes(outputFile, readableAtlasTexture.EncodeToPNG());
            }
            catch (System.Exception exception)
            {
                Debug.LogException(exception);
                return false;
            }
            finally
            {
                RenderTexture.active = previousActive;
                renderTexture.Release();
                if (readableAtlasTexture != null)
                {
                    Object.DestroyImmediate(readableAtlasTexture);
                }
            }

            AssetDatabase.Refresh();
            var textureImporter = AssetImporter.GetAtPath(outputFile) as TextureImporter;
            if (textureImporter == null)
            {
                Debug.LogWarning($"SpriteAtlas2Texture failed: texture importer is null. Output: {outputFile}");
                return false;
            }

            textureImporter.textureType = textureImporterType;
            if (textureImporterType == TextureImporterType.Sprite)
            {
                textureImporter.spriteImportMode = SpriteImportMode.Multiple;
                textureImporter.isReadable = true;
            }

            textureImporter.textureShape = TextureImporterShape.Texture2D;
            textureImporter.alphaIsTransparency = true;
            textureImporter.SaveAndReimport();
            return true;
        }

        internal static Sprite[] GetPackedSprites(SpriteAtlas atlas)
        {
            if (atlas == null || s_GetPackedSpritesMethod == null)
            {
                return null;
            }

            return s_GetPackedSpritesMethod.Invoke(null, new object[] { atlas }) as Sprite[];
        }

        private static Texture2D CreateReadableTexture(Texture2D sourceTexture)
        {
            var renderTexture = RenderTexture.GetTemporary(sourceTexture.width, sourceTexture.height, 0, RenderTextureFormat.ARGB32);
            var previousActive = RenderTexture.active;
            try
            {
                Graphics.Blit(sourceTexture, renderTexture);

                var texture = new Texture2D(sourceTexture.width, sourceTexture.height, TextureFormat.ARGB32, false);
                RenderTexture.active = renderTexture;
                texture.ReadPixels(new Rect(0, 0, sourceTexture.width, sourceTexture.height), 0, 0);
                texture.Apply();
                return texture;
            }
            finally
            {
                RenderTexture.active = previousActive;
                RenderTexture.ReleaseTemporary(renderTexture);
            }
        }

        private static void ClearTexture(Texture2D texture)
        {
            var clearColors = new Color[texture.width * texture.height];
            for (int i = 0; i < clearColors.Length; i++)
            {
                clearColors[i] = Color.clear;
            }

            texture.SetPixels(clearColors);
        }

        private static SpriteRect[] GetSpriteRects(SpriteAtlas atlas)
        {
            var sprites = GetPackedSprites(atlas);
            if (sprites == null || sprites.Length == 0)
            {
                return null;
            }

            var spriteRects = new SpriteRect[sprites.Length];
            for (int i = 0; i < sprites.Length; i++)
            {
                var sprite = sprites[i];
                spriteRects[i] = new SpriteRect
                {
                    name = StripCloneSuffix(sprite.name),
                    rect = sprite.textureRect
                };
            }

            return spriteRects;
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
