using Obfuz.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Obfuz.Unity
{
    public class UnityProjectManagedAssemblyResolver : AssemblyResolverBase
    {
        private readonly Dictionary<string, string> _managedAssemblyNameToPaths = new Dictionary<string, string>();

        public UnityProjectManagedAssemblyResolver(BuildTarget target)
        {
            string[] dllGuids = AssetDatabase.FindAssets("t:DefaultAsset");
            var dllPaths = dllGuids.Select(guid => AssetDatabase.GUIDToAssetPath(guid))
                .Where(f => f.EndsWith(".dll"))
                .Where(dllPath =>
                {
                    PluginImporter importer = AssetImporter.GetAtPath(dllPath) as PluginImporter;
                    if (importer == null || importer.isNativePlugin)
                    {
                        return false;
                    }
                    if (!importer.GetCompatibleWithAnyPlatform() && !importer.GetCompatibleWithPlatform(target))
                    {
                        return false;
                    }
                    return true;
                }).ToArray();

            foreach (string dllPath in dllPaths)
            {
                Debug.Log($"UnityProjectManagedAssemblyResolver find managed dll:{dllPath}");
                string assName = Path.GetFileNameWithoutExtension(dllPath);
                if (_managedAssemblyNameToPaths.TryGetValue(assName, out var existAssPath))
                {
                    Debug.LogWarning($"UnityProjectManagedAssemblyResolver find duplicate assembly1:{existAssPath} assembly2:{dllPath}");
                }
                else
                {
                    _managedAssemblyNameToPaths.Add(Path.GetFileNameWithoutExtension(dllPath), dllPath);
                }
            }
        }

        public override string ResolveAssembly(string assemblyName)
        {
            if (_managedAssemblyNameToPaths.TryGetValue(assemblyName, out string assemblyPath))
            {
                return assemblyPath;
            }
            return null;
        }
    }
}
