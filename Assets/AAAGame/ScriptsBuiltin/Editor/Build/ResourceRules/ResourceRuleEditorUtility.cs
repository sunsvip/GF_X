using UnityEngine;

namespace UGF.EditorTools.Build.ResourceRules
{
    public static class ResourceRuleEditorUtility
    {
        public static void RefreshResourceCollection()
        {
            ResourceRuleCompiler.RefreshResourceCollection();
        }

        public static void RefreshResourceCollection(string configPath)
        {
            ResourceRuleCompiler.RefreshResourceCollection(configPath);
        }
    }
}

