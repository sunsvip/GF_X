#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityGameFramework.Runtime;

namespace UGF.EditorTools
{
    internal static class UIFormMenuCommands
    {
        private const string KeyButtonOnClick = "ClickUIButton";
        private const string KeyButtonOnClose = "OnClickClose";

        private static bool s_AddToFieldToggle;
        private static bool s_RemoveToFieldToggle;

        [MenuItem("GameObject/UIForm Tools/Add private", false, priority = 1002)]
        private static void AddPrivateVariableToUIForm()
        {
            UIFormHierarchyOverlay.QueueAddVariableSelection(0);
        }

        [MenuItem("GameObject/UIForm Tools/Add protected", false, priority = 1003)]
        private static void AddProtectedVariableToUIForm()
        {
            UIFormHierarchyOverlay.QueueAddVariableSelection(1);
        }

        [MenuItem("GameObject/UIForm Tools/Add split", false, priority = 1004)]
        private static void AddSplitVariableToUIForm()
        {
            UIFormHierarchyOverlay.QueueAddVariableSelection(2);
        }

        [MenuItem("GameObject/UIForm Tools/Remove", false, priority = 1005)]
        private static void RemoveUIFormVariable()
        {
            if (s_RemoveToFieldToggle || Selection.count <= 0)
            {
                return;
            }

            var selectedObjects = Selection.gameObjects;
            for (var i = 0; i < selectedObjects.Length; i++)
            {
                var item = selectedObjects[i];
                if (item == null)
                {
                    continue;
                }

                var serializeTool = UISerializeFieldEditorUtility.GetSerializeFieldTool(item);
                if (serializeTool == null)
                {
                    continue;
                }

                var fieldsProperties = serializeTool.SerializeFieldArr;
                if (fieldsProperties == null)
                {
                    continue;
                }

                var goLogic = serializeTool as MonoBehaviour;
                Undo.RecordObject(goLogic, goLogic.name);
                for (var fieldIndex = fieldsProperties.Length - 1; fieldIndex >= 0; fieldIndex--)
                {
                    var field = fieldsProperties[fieldIndex];
                    if (field == null || field.Targets == null || field.Targets.Length <= 0)
                    {
                        continue;
                    }

                    for (var targetIndex = field.Targets.Length - 1; targetIndex >= 0; targetIndex--)
                    {
                        if (field.Targets[targetIndex] != item)
                        {
                            continue;
                        }

                        if (field.Targets.Length <= 1)
                        {
                            ArrayUtility.RemoveAt(ref fieldsProperties, fieldIndex);
                        }
                        else
                        {
                            ArrayUtility.RemoveAt(ref field.Targets, targetIndex);
                        }
                    }
                }

                serializeTool.SerializeFieldArr = fieldsProperties;
                EditorUtility.SetDirty(goLogic);
            }

            s_RemoveToFieldToggle = true;
            s_AddToFieldToggle = false;
        }

        [MenuItem("GameObject/UIForm Tools/Add Button OnClick(string)", false, priority = 1101)]
        private static void AddClickButtonEventString()
        {
            AddClickButtonEvent<string>();
        }

        [MenuItem("GameObject/UIForm Tools/Add Button OnClick(Button)", false, priority = 1102)]
        private static void AddClickButtonEventButton()
        {
            AddClickButtonEvent<Button>();
        }

        [MenuItem("GameObject/UIForm Tools/Add Localization Key", false, priority = 1103)]
        private static void AddLocalizationKey()
        {
            if (Selection.count == 0)
            {
                return;
            }

            foreach (var item in Selection.gameObjects)
            {
                if (item == null)
                {
                    continue;
                }

                if (item.GetComponent<TMPro.TextMeshProUGUI>() == null &&
                    item.GetComponent<Text>() == null &&
                    item.GetComponent<TMPro.TextMeshPro>() == null)
                {
                    continue;
                }

                var serializeTool = UISerializeFieldEditorUtility.GetSerializeFieldTool(item);
                if (serializeTool == null)
                {
                    continue;
                }

                var goLogic = serializeTool as MonoBehaviour;
                var stringKey = item.GetComponent<UIStringKey>();
                if (stringKey == null)
                {
                    stringKey = Undo.AddComponent<UIStringKey>(item);
                }
                else
                {
                    Undo.RecordObject(stringKey, stringKey.name);
                }

                stringKey.Key = GameFramework.Utility.Text.Format("{0}.{1}", goLogic.name, item.name);
                EditorUtility.SetDirty(stringKey);
            }
        }

        [MenuItem("GameObject/UIForm Tools/Raycast Target/Disable", false, priority = 1104)]
        private static void DisableRaycastTarget(MenuCommand command)
        {
            var gameObjects = Selection.gameObjects;
            if (gameObjects.Length == 0 || gameObjects.Length > 1 && command.context != gameObjects[0])
            {
                return;
            }

            bool includeChildren = EditorUtility.DisplayDialog("提示", "是否遍历设置子节点?", "是", "否");
            SetRaycastTarget(gameObjects, false, includeChildren);
        }

        [MenuItem("GameObject/UIForm Tools/Raycast Target/Enable", false, priority = 1105)]
        private static void EnableRaycastTarget(MenuCommand command)
        {
            var gameObjects = Selection.gameObjects;
            if (gameObjects.Length == 0 || gameObjects.Length > 1 && command.context != gameObjects[0])
            {
                return;
            }

            bool includeChildren = EditorUtility.DisplayDialog("提示", "是否遍历设置子节点?", "是", "否");
            SetRaycastTarget(gameObjects, true, includeChildren);
        }

        [MenuItem("GameObject/UIForm Tools/Add Close Button Event", false, priority = 1102)]
        private static void AddCloseButtonEvent()
        {
            if (Selection.count <= 0)
            {
                return;
            }

            foreach (var item in Selection.gameObjects)
            {
                if (item == null || !item.TryGetComponent<Button>(out var buttonComponent))
                {
                    continue;
                }

                var serializeTool = UISerializeFieldEditorUtility.GetSerializeFieldTool(item);
                if (serializeTool == null)
                {
                    continue;
                }

                var goLogic = serializeTool as MonoBehaviour;
                Undo.RecordObject(buttonComponent, buttonComponent.name);
                UIButtonEventInjector.BindVoid(buttonComponent, goLogic, KeyButtonOnClose);
            }
        }

        internal static void ResetSelectionState()
        {
            s_AddToFieldToggle = false;
            s_RemoveToFieldToggle = false;
        }

        internal static void AddToFields(int varPrefix, string varType)
        {
            if (s_AddToFieldToggle || Selection.count <= 0)
            {
                return;
            }

            var targets = UISerializeFieldEditorUtility.GetTargetsFromSelectedNodes(Selection.gameObjects);
            if (varPrefix != 2)
            {
                var groupedTargets = new Dictionary<ISerializeFieldTool, List<GameObject>>();
                for (var i = 0; i < targets.Length; i++)
                {
                    var item = targets[i];
                    var serializeTool = UISerializeFieldEditorUtility.GetSerializeFieldTool(item);
                    if (serializeTool == null)
                    {
                        continue;
                    }

                    var fieldsProperties = serializeTool.SerializeFieldArr;
                    if (fieldsProperties == null)
                    {
                        continue;
                    }

                    if (!groupedTargets.TryGetValue(serializeTool, out var group))
                    {
                        group = new List<GameObject>();
                        groupedTargets.Add(serializeTool, group);
                    }

                    group.Add(item);
                }

                foreach (var item in groupedTargets)
                {
                    var serializeTool = item.Key;
                    var goLogic = serializeTool as MonoBehaviour;
                    Undo.RecordObject(goLogic, goLogic.name);
                    var fieldsProperties = serializeTool.SerializeFieldArr;
                    var gameObjects = item.Value.ToArray();
                    var field = new SerializeFieldData(UISerializeFieldEditorUtility.GenerateFieldName(fieldsProperties, gameObjects), gameObjects)
                    {
                        VarPrefix = varPrefix,
                        VarType = varType
                    };
                    ArrayUtility.Add(ref fieldsProperties, field);
                    serializeTool.SerializeFieldArr = fieldsProperties;
                    EditorUtility.SetDirty(goLogic);
                }
            }
            else
            {
                var recordedObjects = new HashSet<Object>();
                for (var i = 0; i < targets.Length; i++)
                {
                    var item = targets[i];
                    var serializeTool = UISerializeFieldEditorUtility.GetSerializeFieldTool(item);
                    if (serializeTool == null)
                    {
                        continue;
                    }

                    var fieldsProperties = serializeTool.SerializeFieldArr;
                    if (fieldsProperties == null)
                    {
                        continue;
                    }

                    var goLogic = serializeTool as MonoBehaviour;
                    if (recordedObjects.Add(goLogic))
                    {
                        Undo.RecordObject(goLogic, goLogic.name);
                    }

                    var elements = new[] { item };
                    var field = new SerializeFieldData(UISerializeFieldEditorUtility.GenerateFieldName(fieldsProperties, elements), elements)
                    {
                        VarPrefix = 1,
                        VarType = varType
                    };
                    ArrayUtility.Add(ref fieldsProperties, field);
                    serializeTool.SerializeFieldArr = fieldsProperties;
                    EditorUtility.SetDirty(goLogic);
                }
            }

            s_AddToFieldToggle = true;
            s_RemoveToFieldToggle = false;
        }

        private static void SetRaycastTarget(GameObject[] gameObjects, bool enable, bool recursively)
        {
            if (recursively)
            {
                foreach (var go in gameObjects)
                {
                    if (go == null)
                    {
                        continue;
                    }

                    var graphics = go.GetComponentsInChildren<Graphic>(true);
                    for (var i = 0; i < graphics.Length; i++)
                    {
                        var graphic = graphics[i];
                        if (graphic.raycastTarget == enable)
                        {
                            continue;
                        }

                        Undo.RecordObject(graphic, graphic.name);
                        graphic.raycastTarget = enable;
                        EditorUtility.SetDirty(graphic);
                    }
                }
            }
            else
            {
                foreach (var go in gameObjects)
                {
                    if (go == null || !go.TryGetComponent<Graphic>(out var graphic) || graphic.raycastTarget == enable)
                    {
                        continue;
                    }

                    Undo.RecordObject(graphic, graphic.name);
                    graphic.raycastTarget = enable;
                    EditorUtility.SetDirty(graphic);
                }
            }
        }

        private static void AddClickButtonEvent<T>()
        {
            if (Selection.count == 0)
            {
                return;
            }

            var selectedObjects = Selection.gameObjects;
            for (var i = 0; i < selectedObjects.Length; i++)
            {
                var item = selectedObjects[i];
                if (item == null || !item.TryGetComponent<Button>(out var buttonComponent))
                {
                    continue;
                }

                var serializeTool = UISerializeFieldEditorUtility.GetSerializeFieldTool(item);
                if (serializeTool == null)
                {
                    continue;
                }

                var goLogic = serializeTool as MonoBehaviour;
                Undo.RecordObject(buttonComponent, buttonComponent.name);
                if (typeof(T) == typeof(string))
                {
                    UIButtonEventInjector.BindString(buttonComponent, goLogic, KeyButtonOnClick, buttonComponent.name);
                }
                else if (typeof(T) == typeof(Button))
                {
                    UIButtonEventInjector.BindButton(buttonComponent, goLogic, KeyButtonOnClick);
                }
            }
        }
    }
}
#endif
