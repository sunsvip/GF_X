#if UNITY_EDITOR
using System;
using System.Linq;
using GameFramework;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace UGF.EditorTools
{
    internal static class UISerializeFieldBindingService
    {
        private static readonly string[] VarPrefixArray = { "private", "protected", "public" };

        internal static string GetVarPrefix(int index)
        {
            return VarPrefixArray[index];
        }

        internal static Type GetSampleType(string fullName)
        {
            return Utility.Assembly.GetType(fullName);
        }

        internal static void SerializeFieldProperties(SerializedObject serializedObject, SerializeFieldData[] fields, string refreshBindKey)
        {
            EditorPrefs.DeleteKey(refreshBindKey);
            if (serializedObject == null)
            {
                Debug.LogError("生成UI SerializedField失败, serializedObject为null");
                return;
            }

            foreach (var item in fields)
            {
                if (item == null || item.Targets == null)
                {
                    continue;
                }

                var varName = item.VarName;
                var varType = item.VarType;
                var isGameObject = string.Equals(varType, typeof(GameObject).FullName, StringComparison.Ordinal);
                var sampleType = isGameObject ? null : GetSampleType(varType);
                var property = serializedObject.FindProperty(varName);
                if (property == null)
                {
                    continue;
                }

                if (!isGameObject && sampleType == null)
                {
                    GFBuiltin.LogWarning(Utility.Text.Format("######检测到变量:{0}, 类型解析失败:{1}########", varName, varType));
                    continue;
                }

                if (item.Targets.Length == 1)
                {
                    var itemGo = item.Targets[0];
                    if (itemGo == null)
                    {
                        GFBuiltin.LogWarning(Utility.Text.Format("######检测到变量:{0}, GameObject引用丢失!########", varName));
                        continue;
                    }

                    UnityEngine.Object component = isGameObject ? itemGo : itemGo.GetComponent(sampleType);
                    if (component == null)
                    {
                        GFBuiltin.LogWarning(Utility.Text.Format("######检测到变量:{0}, 目标节点缺少组件:{1}########", varName, varType));
                        continue;
                    }

                    property.objectReferenceValue = component;
                }
                else if (property.isArray)
                {
                    property.ClearArray();
                    for (var i = 0; i < item.Targets.Length; i++)
                    {
                        if (i >= property.arraySize)
                        {
                            property.InsertArrayElementAtIndex(i);
                        }

                        var itemGo = item.Targets[i];
                        if (itemGo == null)
                        {
                            GFBuiltin.LogWarning(Utility.Text.Format("######检测到变量:{0},索引为{1}的GameObject引用丢失!########", varName, i));
                            continue;
                        }

                        UnityEngine.Object component = isGameObject ? itemGo : itemGo.GetComponent(sampleType);
                        if (component == null)
                        {
                            GFBuiltin.LogWarning(Utility.Text.Format("######检测到变量:{0},索引为{1}的节点缺少组件:{2}########", varName, i, varType));
                            continue;
                        }

                        property.GetArrayElementAtIndex(i).objectReferenceValue = component;
                    }
                }
            }

            serializedObject.ApplyModifiedProperties();
        }

        internal static void EnsureFieldsInitialized(SerializedObject serializedObject, ISerializeFieldTool serializeFieldTool, ref SerializedProperty fieldsProperty)
        {
            if (serializeFieldTool.SerializeFieldArr == null)
            {
                serializeFieldTool.SerializeFieldArr = Array.Empty<SerializeFieldData>();
                fieldsProperty = serializedObject.FindProperty("_fields");
            }
        }

        internal static void SyncReorderableListSize(SerializedProperty fieldsProperty, ref ReorderableList[] reorderableLists)
        {
            if (fieldsProperty.arraySize == reorderableLists.Length)
            {
                return;
            }

            if (fieldsProperty.arraySize > reorderableLists.Length)
            {
                for (var i = reorderableLists.Length; i < fieldsProperty.arraySize; i++)
                {
                    ArrayUtility.Insert(ref reorderableLists, reorderableLists.Length, null);
                }
            }
            else
            {
                while (reorderableLists.Length > fieldsProperty.arraySize)
                {
                    ArrayUtility.RemoveAt(ref reorderableLists, reorderableLists.Length - 1);
                }
            }
        }

        internal static string[] GetPopupContents(GameObject[] targets)
        {
            if (targets == null || targets.Length <= 0)
            {
                return Array.Empty<string>();
            }

            var typeNames = GetIntersectionComponents(targets);
            if (typeNames == null || typeNames.Length <= 0)
            {
                return Array.Empty<string>();
            }

            ArrayUtility.Insert(ref typeNames, 0, typeof(GameObject).FullName);
            return typeNames;
        }

        internal static string[] GetPopupContents(SerializedProperty targets)
        {
            var gameObjects = new GameObject[targets.arraySize];
            for (var i = 0; i < targets.arraySize; i++)
            {
                var property = targets.GetArrayElementAtIndex(i);
                gameObjects[i] = property != null && property.objectReferenceValue != null
                    ? property.objectReferenceValue as GameObject
                    : null;
            }

            return GetPopupContents(gameObjects);
        }

        private static string[] GetIntersectionComponents(GameObject[] targets)
        {
            GameObject firstTarget = null;
            for (var i = 0; i < targets.Length; i++)
            {
                if (targets[i] != null)
                {
                    firstTarget = targets[i];
                    break;
                }
            }

            if (firstTarget == null)
            {
                return Array.Empty<string>();
            }

            var components = firstTarget.GetComponents(typeof(Component)).Where(component => component != null).Distinct().ToArray();
            for (var i = components.Length - 1; i >= 1; i--)
            {
                var componentType = components[i].GetType().FullName;
                var allContains = true;
                for (var j = 0; j < targets.Length; j++)
                {
                    var target = targets[j];
                    if (target == null)
                    {
                        continue;
                    }

                    var targetComponents = target.GetComponents(typeof(Component));
                    var containsType = false;
                    for (var k = 0; k < targetComponents.Length; k++)
                    {
                        if (string.Equals(targetComponents[k].GetType().FullName, componentType, StringComparison.Ordinal))
                        {
                            containsType = true;
                            break;
                        }
                    }

                    allContains &= containsType;
                    if (!allContains)
                    {
                        break;
                    }
                }

                if (!allContains)
                {
                    ArrayUtility.RemoveAt(ref components, i);
                }
            }

            var typesArray = new string[components.Length];
            for (var i = 0; i < components.Length; i++)
            {
                typesArray[i] = components[i].GetType().FullName;
            }

            return typesArray;
        }
    }
}
#endif
