using UnityEditor;
using UnityEngine;

namespace UGF.EditorTools
{
    internal static class AssetDependencyLogger
    {
        public static void LogDependencies(UnityEngine.Object asset)
        {
            if (asset == null)
            {
                return;
            }

            var path = AssetDatabase.GetAssetPath(asset);
            if (string.IsNullOrWhiteSpace(path))
            {
                return;
            }

            var dependencies = AssetDatabase.GetDependencies(path);
            Debug.Log($"----------------{path} Dependencies---------------");
            foreach (var dependency in dependencies)
            {
                Debug.Log(dependency);
            }
            Debug.Log("--------------------------------------------------");
        }
    }
}
