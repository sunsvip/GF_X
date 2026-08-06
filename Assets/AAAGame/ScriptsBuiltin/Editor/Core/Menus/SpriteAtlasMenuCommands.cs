using UnityEditor;
using UnityEngine;
using UnityEngine.U2D;

namespace UGF.EditorTools
{
    public static class SpriteAtlasMenuCommands
    {
        [MenuItem("Assets/GF Tools/2D/SpriteAtlas -> TMP_SpriteAsset", priority = 100)]
        private static void SpriteAtlasToTmpSpriteMenu()
        {
            var selectedObjects = Selection.objects;
            for (int i = 0; i < selectedObjects.Length; i++)
            {
                if (selectedObjects[i] is SpriteAtlas spriteAtlas)
                {
                    TmpSpriteAssetBuildService.BuildFromAtlas(spriteAtlas);
                }
            }
        }

        [MenuItem("Assets/GF Tools/2D/SpriteAtlas -> Sprite(Multiple)", priority = 101)]
        private static void SpriteAtlasToSpriteSheetMenu()
        {
            var selectedObjects = Selection.objects;
            for (int i = 0; i < selectedObjects.Length; i++)
            {
                if (selectedObjects[i] is SpriteAtlas spriteAtlas)
                {
                    SpriteAtlasTextureExportService.ExportSpriteSheet(spriteAtlas);
                }
            }
        }

        [MenuItem("Assets/GF Tools/2D/SpriteAtlas -> TextureSheet", priority = 102)]
        private static void SpriteAtlasToGridSheetMenu()
        {
            var selectedObjects = Selection.objects;
            for (int i = 0; i < selectedObjects.Length; i++)
            {
                if (selectedObjects[i] is SpriteAtlas spriteAtlas)
                {
                    SpriteAtlasTextureExportService.ExportGridSheet(spriteAtlas);
                }
            }
        }

        [MenuItem("Assets/GF Tools/2D/Sprite(Multiple) -> Sprites", priority = 104)]
        private static void ExportSpriteMultiple()
        {
            SpriteAtlasTextureExportService.ExportMultipleSprites(Selection.objects);
        }

        internal static void SpritesToTextureSheet(Sprite[] sprites, string outputFileName, int row = 1)
        {
            SpriteAtlasTextureExportService.BuildTextureSheet(sprites, outputFileName, row);
        }
    }
}
