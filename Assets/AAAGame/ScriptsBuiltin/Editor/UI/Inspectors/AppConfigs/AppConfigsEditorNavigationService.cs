#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace UGF.EditorTools
{
    internal static class AppConfigsEditorNavigationService
    {
        internal static void OpenConstEditorScript()
        {
            var scriptGuids = AssetDatabase.FindAssets("ConstEditor t:Script", new[] { "Assets/AAAGame/ScriptsBuiltin/Editor" });
            if (scriptGuids == null || scriptGuids.Length == 0)
            {
                Debug.LogWarning("Open ConstEditor failed: script asset was not found.");
                return;
            }

            var scriptPath = AssetDatabase.GUIDToAssetPath(scriptGuids[0]);
            if (string.IsNullOrEmpty(scriptPath))
            {
                Debug.LogWarning("Open ConstEditor failed: resolved asset path is empty.");
                return;
            }

            InternalEditorUtility.OpenFileAtLineExternal(scriptPath, 0);
        }
    }
}
#endif
