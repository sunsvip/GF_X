using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GameFramework;
using UnityEditor;
using UnityEngine;

namespace UGF.EditorTools
{
    internal static class AtlasFolderSelectionService
    {
        internal static List<string> GetSelectedFolders(
            IList<UnityEngine.Object> selectedObjects,
            bool includeChildrenFolders,
            Func<string, ItemType> getSelectedItemType)
        {
            var folders = new List<string>();
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            for (int i = 0; i < selectedObjects.Count; i++)
            {
                var item = selectedObjects[i];
                if (item == null)
                {
                    continue;
                }

                var assetPath = AssetDatabase.GetAssetPath(item);
                if (getSelectedItemType(assetPath) != ItemType.Folder)
                {
                    continue;
                }

                folders.Add(assetPath);
                if (!includeChildrenFolders)
                {
                    continue;
                }

                var directories = Directory.GetDirectories(assetPath, "*", SearchOption.AllDirectories);
                for (int directoryIndex = 0; directoryIndex < directories.Length; directoryIndex++)
                {
                    var directory = directories[directoryIndex];
                    string relativeDirectory = directory.StartsWith("Assets", StringComparison.Ordinal)
                        ? directory
                        : Path.GetRelativePath(projectRoot, directory);
                    folders.Add(Utility.Path.GetRegularPath(relativeDirectory));
                }
            }

            return folders.Distinct().ToList();
        }

        internal static UnityEngine.Object[] LoadPackObjects(string folder, Func<string, bool> isSupportAsset, int atlasSpriteSizeLimit)
        {
            var textureFiles = Directory.GetFiles(folder, "*.*", SearchOption.TopDirectoryOnly)
                .Select(Utility.Path.GetRegularPath)
                .Where(isSupportAsset);
            var textureObjects = new List<UnityEngine.Object>();
            foreach (var file in textureFiles)
            {
                var textureObject = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(file);
                if (textureObject is not Texture texture)
                {
                    continue;
                }

                if (Mathf.Max(texture.width, texture.height) > atlasSpriteSizeLimit)
                {
                    continue;
                }

                textureObjects.Add(textureObject);
            }

            return textureObjects.ToArray();
        }
    }
}
