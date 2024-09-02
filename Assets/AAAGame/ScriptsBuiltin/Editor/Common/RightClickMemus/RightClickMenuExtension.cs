using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace UGF.EditorTools
{
    public partial class RightClickMenuExtension
    {
        [MenuItem("Assets/GF Tools/Log Asset Dependencies", priority = 1003)]
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

        [MenuItem("Assets/GF Tools/Copy Asset Path/Relative Path", priority = 1000)]
        static void CopyAssetRelativePath()
        {
            CopyAssetsPath2Clipboard(Selection.objects, false);
        }
        [MenuItem("Assets/GF Tools/Copy Asset Path/Full Path", priority = 1001)]
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

