using System;
using System.Text;
using UnityEditor;

namespace UGF.EditorTools
{
    internal static class AssetMenuClipboardUtility
    {
        public static void CopyAssetsPathToClipboard(UnityEngine.Object[] assets, int pathMode)
        {
            if (assets == null || assets.Length < 1)
            {
                return;
            }

            var strBuilder = new StringBuilder();
            switch (pathMode)
            {
                case 1:
                    foreach (var item in assets)
                    {
                        var itemPath = AssetDatabase.GetAssetPath(item);
                        strBuilder.AppendLine(itemPath);
                    }
                    break;

                case 2:
                    foreach (var item in assets)
                    {
                        var itemPath = AssetDatabase.GetAssetPath(item);
                        if (string.IsNullOrWhiteSpace(itemPath) || !System.IO.Path.HasExtension(itemPath))
                        {
                            continue;
                        }

                        itemPath = System.IO.Path.GetFileName(itemPath);
                        strBuilder.AppendLine(itemPath);
                    }
                    break;

                default:
                    foreach (var item in assets)
                    {
                        var itemPath = System.IO.Path.GetFullPath(AssetDatabase.GetAssetPath(item), ConstEditor.ProjectRootPath);
                        strBuilder.AppendLine(itemPath);
                    }
                    break;
            }

            var result = strBuilder.ToString().TrimEnd(Environment.NewLine.ToCharArray());
            EditorGUIUtility.systemCopyBuffer = result;
        }
    }
}
