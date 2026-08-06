#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace UGF.EditorTools
{
    public partial class GFEditorCommandTools
    {
        [MenuItem("Game Framework/GameTools/Clear All Prefabs Missing Scripts【清除工程所有Prefab丢失脚本】")]
        public static void ClearMissingScripts()
        {
            var pfbArr = AssetDatabase.FindAssets("t:Prefab");
            foreach (var item in pfbArr)
            {
                var pfbFileName = AssetDatabase.GUIDToAssetPath(item);
                ProjectPanelMenuCommands.ClearPrefabMissingComponents(pfbFileName);
            }
        }
        [MenuItem("GameObject/GF Tools/Copy Transform Path", priority = 1001)]
        static void CopyNodePath()
        {
            if (null == Selection.activeTransform) return;
            var stage = PrefabStageUtility.GetCurrentPrefabStage();
            string path;
            if (null != stage && null != stage.prefabContentsRoot)
            {
                path = GetNodePath(Selection.activeTransform, stage.prefabContentsRoot.transform);
            }
            else
            {
                path = GetNodePath(Selection.activeTransform);
            }
            EditorGUIUtility.systemCopyBuffer = path;
        }
        #region 通用方法

        public static string GetNodePath(Transform node, Transform root = null)
        {
            if (node == null)
            {
                return string.Empty;
            }
            Transform curNode = node;
            string path = curNode.name;
            while (curNode.parent != null && curNode.parent != root)
            {
                curNode = curNode.parent;
                path = string.Format("{0}/{1}", curNode.name, path);
            }
            return path;
        }
        #endregion
    }
}
#endif
