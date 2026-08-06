using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace UGF.EditorTools.Build.ResourceRules
{
    internal static class ResourceRuleEditorConfigRepository
    {
        internal static List<string> LoadAllConfigPaths()
        {
            return AssetDatabase.FindAssets("t:ResourceRuleEditorData")
                .Select(AssetDatabase.GUIDToAssetPath)
                .ToList();
        }

        internal static string[] CreateConfigNames(IReadOnlyList<string> configPaths)
        {
            var result = new string[configPaths.Count];
            for (int i = 0; i < configPaths.Count; i++)
            {
                result[i] = Path.GetFileNameWithoutExtension(configPaths[i]);
            }

            return result;
        }

        internal static ResourceRuleEditorData LoadConfig(string configPath)
        {
            return AssetDatabase.LoadAssetAtPath<ResourceRuleEditorData>(configPath);
        }

        internal static ResourceRuleEditorData CreateDefaultConfig()
        {
            return ScriptableObject.CreateInstance<ResourceRuleEditorData>();
        }

        internal static void Save(ResourceRuleEditorData configuration, string configPath)
        {
            if (LoadConfig(configPath) == null)
            {
                AssetDatabase.CreateAsset(configuration, configPath);
            }
            else
            {
                EditorUtility.SetDirty(configuration);
            }

            AssetDatabase.SaveAssets();
        }
    }
}

