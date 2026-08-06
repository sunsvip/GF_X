using UnityEditor;
using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace UGF.EditorTools
{

    public class CreateNewScriptListener : UnityEditor.AssetModificationProcessor
    {
        public static void OnWillCreateAsset(string assetPath)
        {
            if (ConstEditor.AutoScriptUTF8 && string.Equals(Path.GetExtension(assetPath), ".meta", StringComparison.OrdinalIgnoreCase))
            {
                var assetName = Path.GetFileNameWithoutExtension(assetPath);
                if (assetName.EndsWith(".cs", StringComparison.OrdinalIgnoreCase) || assetName.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
                {
                    var projectRelativeAssetPath = UtilityBuiltin.AssetsPath.GetCombinePath(Path.GetDirectoryName(assetPath), assetName);
                    ConvertScriptToUTF8(projectRelativeAssetPath);
                }
            }
        }
        /// <summary>
        /// 把.cs或.txt文件转为utf-8
        /// </summary>
        /// <param name="projectRelativeAssetPath"></param>
        static void ConvertScriptToUTF8(string projectRelativeAssetPath)
        {
            var fullPath = Path.GetFullPath(projectRelativeAssetPath, ConstEditor.ProjectRootPath);
            if (!File.Exists(fullPath)) return;
            var rawBytes = File.ReadAllBytes(fullPath);
            string fileTxt;
            try
            {
                // 先按严格 UTF-8 解码(遇非法字节抛异常), 成功则说明已是 UTF-8, 无需破坏性重写.
                fileTxt = new UTF8Encoding(false, throwOnInvalidBytes: true).GetString(rawBytes);
            }
            catch (DecoderFallbackException)
            {
                // 非 UTF-8(常见 ANSI/GB2312 模板), 回退 GB2312 解码后再统一写为 UTF-8 无 BOM.
                fileTxt = Encoding.GetEncoding(936).GetString(rawBytes);
            }
            // 去掉可能的 UTF-8 BOM(U+FEFF), 避免以无 BOM 写回时把 BOM 固化成正文字节.
            if (fileTxt.Length > 0 && fileTxt[0] == (char)0xFEFF)
            {
                fileTxt = fileTxt.Substring(1);
            }
            File.WriteAllText(fullPath, fileTxt, new UTF8Encoding(false));
            AssetDatabase.ImportAsset(projectRelativeAssetPath, ImportAssetOptions.ForceUpdate);
        }
    }
}
