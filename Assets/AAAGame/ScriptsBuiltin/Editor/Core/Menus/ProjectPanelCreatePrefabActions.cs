using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace UGF.EditorTools
{
    internal sealed class DoCreatePrefab : UnityEditor.ProjectWindowCallback.EndNameEditAction
    {
        public override void Action(int instanceId, string pathName, string resourceFile)
        {
            try
            {
                if (AssetDatabase.CopyAsset(resourceFile, pathName))
                {
                    var newPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(pathName);
                    ProjectWindowUtil.ShowCreatedAsset(newPrefab);
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
    }

    internal sealed class DoCreateUIPrefabAndScriptFile : UnityEditor.ProjectWindowCallback.EndNameEditAction
    {
        public override void Action(int instanceId, string pathName, string resourceFile)
        {
            try
            {
                if (AssetDatabase.CopyAsset(resourceFile, pathName))
                {
                    var newPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(pathName);
                    ProjectWindowUtil.ShowCreatedAsset(newPrefab);

                    var uiPrefabName = Path.GetFileNameWithoutExtension(pathName);
                    var uiScriptFile = UtilityBuiltin.AssetsPath.GetCombinePath(ConstEditor.UIScriptsPath, uiPrefabName + ".cs");
                    if (!ProjectPanelUiCreationService.TryCreateUIScriptFile(uiScriptFile, ConstEditor.UIScriptFileTemplate, uiPrefabName))
                    {
                        return;
                    }

                    EditorDeferredTaskQueue.EnqueueComponentAttachTask(pathName, uiScriptFile);
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
    }

    internal sealed class DoCreateUIItemAndScriptFile : UnityEditor.ProjectWindowCallback.EndNameEditAction
    {
        public override void Action(int instanceId, string pathName, string resourceFile)
        {
            try
            {
                if (AssetDatabase.CopyAsset(resourceFile, pathName))
                {
                    var newPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(pathName);
                    ProjectWindowUtil.ShowCreatedAsset(newPrefab);

                    var uiPrefabName = Path.GetFileNameWithoutExtension(pathName);
                    var uiScriptFile = UtilityBuiltin.AssetsPath.GetCombinePath(ConstEditor.UIItemScriptsPath, uiPrefabName + ".cs");
                    if (!ProjectPanelUiCreationService.TryCreateUIScriptFile(uiScriptFile, ConstEditor.UIItemScriptFileTemplate, uiPrefabName))
                    {
                        return;
                    }

                    EditorDeferredTaskQueue.EnqueueComponentAttachTask(pathName, uiScriptFile);
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
    }
}
