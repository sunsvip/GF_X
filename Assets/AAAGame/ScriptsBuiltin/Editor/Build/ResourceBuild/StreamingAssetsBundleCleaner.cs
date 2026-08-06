using GameFramework;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace UGF.EditorTools
{
    internal static class StreamingAssetsBundleCleaner
    {
        public static void RemoveStreamingAssetsBundles()
        {
            var streamingAssetsPath = Application.streamingAssetsPath;
            if (!Directory.Exists(streamingAssetsPath))
            {
                return;
            }

            var oldAbFiles = Directory.GetFiles(streamingAssetsPath, "*.dat", SearchOption.AllDirectories);
            var projectRoot = ConstEditor.ProjectRootPath;
            foreach (var abFile in oldAbFiles)
            {
                Debug.Log($"删除文件:{abFile}");
                var relativePath = Path.GetRelativePath(projectRoot, abFile);
                AssetDatabase.DeleteAsset(Utility.Path.GetRegularPath(relativePath));
            }

            var dirInfo = new DirectoryInfo(streamingAssetsPath);
            var subDirs = dirInfo.GetDirectories("*", SearchOption.AllDirectories).OrderByDescending(item => item.FullName.Length);
            foreach (var item in subDirs)
            {
                if (!item.Exists || item.GetFiles("*", SearchOption.AllDirectories).Length > 0)
                {
                    continue;
                }

                Debug.Log($"删除文件夹:{item}");
                var relativePath = Path.GetRelativePath(projectRoot, item.FullName);
                AssetDatabase.DeleteAsset(Utility.Path.GetRegularPath(relativePath));
            }
        }
    }
}
