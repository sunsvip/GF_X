using System.IO;
using UnityEditor;
using UnityEngine;

namespace UGF.EditorTools
{
    internal static class EditorDialogUtility
    {
        internal static string OpenRelativeFilePanel(string title, string relativeFilePath, string fileExt)
        {
            var rootPath = GetProjectRootPath();
            var currentFullPath = !string.IsNullOrWhiteSpace(relativeFilePath) ? Path.Combine(rootPath, relativeFilePath) : rootPath;
            var selectPath = EditorUtility.OpenFilePanel(title, Path.GetDirectoryName(currentFullPath), fileExt);
            return string.IsNullOrWhiteSpace(selectPath) ? selectPath : Path.GetRelativePath(rootPath, selectPath);
        }

        internal static string OpenRelativeFolderPanel(string title, string relativePath)
        {
            var rootPath = GetProjectRootPath();
            var currentFullPath = !string.IsNullOrWhiteSpace(relativePath) ? Path.Combine(rootPath, relativePath) : rootPath;
            var selectPath = EditorUtility.OpenFolderPanel(title, currentFullPath, null);
            return string.IsNullOrWhiteSpace(selectPath) ? selectPath : Path.GetRelativePath(rootPath, selectPath);
        }

        private static string GetProjectRootPath()
        {
            return Directory.GetParent(Application.dataPath).FullName;
        }
    }
}
