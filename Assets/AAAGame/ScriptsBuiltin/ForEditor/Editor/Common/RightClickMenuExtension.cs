using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace UGF.EditorTools
{
    public class RightClickMenuExtension
    {
        [MenuItem("Assets/GF Editor Tool/Log Asset Dependencies", priority = 1003)]
        static void LogAssetDependencies()
        {
            if (Selection.activeObject == null) return;

            var path = AssetDatabase.GetAssetPath(Selection.activeObject);
            if (string.IsNullOrWhiteSpace(path)) return;

            var dependencies = AssetDatabase.GetDependencies(path);
            Debug.Log($"----------------{path} Dependencies---------------");
            foreach (var dependency in dependencies)
            {
                Debug.Log(dependency);
            }
            Debug.Log($"--------------------------------------------------");
        }

        /// <summary>
        /// 导出Multiple类型的Sprite为碎图
        /// </summary>
        [MenuItem("Assets/GF Editor Tool/2D/SpriteSheet to sprites", priority = 1002)]
        static void ExportSpriteMultiple()
        {
            int selectAssetsCount = Selection.objects.Length;
            EditorUtility.DisplayProgressBar($"拆分图集(0/{selectAssetsCount})", "Export sprite sheet to sprites...", 0);
            List<string> slicedSpritesAssets = new List<string>();
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
                var outputDir = UtilityBuiltin.ResPath.GetCombinePath(Path.GetDirectoryName(spFileName), $"{Path.GetFileNameWithoutExtension(spFileName)}_sliced");

                if (!Directory.Exists(outputDir))
                {
                    Directory.CreateDirectory(outputDir);
                }
                int childrenSpCount = texImporter.spritesheet.Length;
                for (int spIndex = 0; spIndex < childrenSpCount; spIndex++)
                {
                    var spDt = texImporter.spritesheet[spIndex];
                    var tex = new Texture2D((int)spDt.rect.width, (int)spDt.rect.height);
                    tex.SetPixels(spTex.GetPixels((int)spDt.rect.x, (int)spDt.rect.y, tex.width, tex.height));
                    tex.Apply();
                    string fileName = UtilityBuiltin.ResPath.GetCombinePath(outputDir, $"{spDt.name}.png");
                    if (File.Exists(fileName))
                    {
                        File.Delete(fileName);
                    }
                    EditorUtility.DisplayProgressBar($"拆分图集({i + 1}/{selectAssetsCount})", $"导出进度({spIndex}/{childrenSpCount}){Environment.NewLine}正在导出碎图{spDt}", (i + 1) / (float)selectAssetsCount);
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
        [MenuItem("Assets/GF Editor Tool/Copy Asset Path/Relative Path", priority = 1000)]
        static void CopyAssetRelativePath()
        {
            CopyAssetsPath2Clipboard(Selection.objects, false);
        }
        [MenuItem("Assets/GF Editor Tool/Copy Asset Path/Full Path", priority = 1001)]
        static void CopyAssetFullPath()
        {
            CopyAssetsPath2Clipboard(Selection.objects, true);
        }
        /// <summary>
        /// 复制资源路径到剪贴板
        /// </summary>
        /// <param name="assets"></param>
        /// <param name="copyFullPath"></param>
        private static void CopyAssetsPath2Clipboard(UnityEngine.Object[] assets, bool copyFullPath = false)
        {
            if (assets == null || assets.Length < 1)
            {
                return;
            }
            StringBuilder strBuilder = new StringBuilder();
            if (copyFullPath)
            {
                var projectRoot = Directory.GetParent(Application.dataPath).FullName;
                foreach (var item in assets)
                {
                    var itemPath = Path.GetFullPath(AssetDatabase.GetAssetPath(item), projectRoot);
                    strBuilder.AppendLine(itemPath);
                }
            }
            else
            {
                foreach (var item in assets)
                {
                    var itemPath = AssetDatabase.GetAssetPath(item);
                    strBuilder.AppendLine(itemPath);
                }
            }

            var result = strBuilder.ToString().TrimEnd(Environment.NewLine.ToCharArray());
            EditorGUIUtility.systemCopyBuffer = result;
        }
    }
}

