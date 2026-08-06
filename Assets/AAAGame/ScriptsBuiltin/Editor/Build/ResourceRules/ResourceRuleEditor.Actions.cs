using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace UGF.EditorTools.Build.ResourceRules
{
    public partial class ResourceRuleEditor : EditorWindow
    {
        [MenuItem("Game Framework/Resource Tools/Resource Rule Editor", false, 50)]
        public static void Open()
        {
            ResourceRuleEditor window = GetWindow<ResourceRuleEditor>(true, "Resource Rule Editor", true);
            window.minSize = new Vector2(1260f, 420f);
        }

        [OnOpenAsset]
        public static bool OnOpenAsset(int instanceID, int line)
        {
#if UNITY_6000_3_OR_NEWER
            var config = EditorUtility.EntityIdToObject(instanceID) as ResourceRuleEditorData;
#else
            var config = EditorUtility.InstanceIDToObject(instanceID) as ResourceRuleEditorData;
#endif
            if (config == null)
            {
                return false;
            }

            ResourceRuleEditor window = GetWindow<ResourceRuleEditor>(true, "Resource Rule Editor", true);
            window.minSize = new Vector2(1260f, 420f);
            window._currentConfigPath = AssetDatabase.GetAssetPath(config);
            window.Load();
            return true;
        }

        private void OnSelectionChange()
        {
            var config = Selection.activeObject as ResourceRuleEditorData;
            if (config != null && config != _configuration)
            {
                _currentConfigPath = AssetDatabase.GetAssetPath(config);
                Load();
                GetWindow<ResourceRuleEditor>().Focus();
            }
        }

        public void RefreshResourceCollection()
        {
            if (_configuration == null)
            {
                Load();
            }

            if (ResourceRuleCompiler.RefreshResourceCollection(_configuration))
            {
                Debug.Log("Refresh ResourceCollection.xml success");
            }
            else
            {
                Debug.Log("Refresh ResourceCollection.xml fail");
            }
        }

        public void RefreshResourceCollection(string configPath)
        {
            if (_configuration == null || !_currentConfigPath.Equals(configPath))
            {
                _currentConfigPath = configPath;
                Load();
            }

            if (ResourceRuleCompiler.RefreshResourceCollection(_configuration))
            {
                Debug.Log("Refresh ResourceCollection.xml success");
            }
            else
            {
                Debug.Log("Refresh ResourceCollection.xml fail");
            }
        }
    }
}

