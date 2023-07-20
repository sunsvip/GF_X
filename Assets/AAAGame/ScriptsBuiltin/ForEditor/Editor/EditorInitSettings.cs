using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace UGF.EditorTools
{
    public static class EditorInitSettings
    {
        [InitializeOnLoadMethod]
        private static void InitEditorLayers()
        {
            AddLayer(ConstEditor.DefaultLayers);
        }

        private static bool HasLayer(SerializedObject tagObject, string layerName)
        {
            var tagManager = tagObject ?? new SerializedObject(AssetDatabase.LoadAssetAtPath<Object>("ProjectSettings/TagManager.asset"));
            if (tagManager == null) return false;

            var layers = tagManager.FindProperty("layers");
            for (int i = 0; i < layers.arraySize; i++)
            {
                var name = layers.GetArrayElementAtIndex(i).stringValue;
                if (layerName.CompareTo(name) == 0)
                {
                    return true;
                }
            }
            return false;
        }
        private static void AddLayer(string[] layerNames)
        {
            var tagManager = new SerializedObject(AssetDatabase.LoadAssetAtPath<Object>("ProjectSettings/TagManager.asset"));
            if (tagManager == null) return;

            foreach (var layerName in layerNames)
            {
                if (HasLayer(tagManager, layerName))
                {
                    continue;
                }

                var layers = tagManager.FindProperty("layers");
                for (int i = 0; i < layers.arraySize; i++)
                {
                    var layerInfo = layers.GetArrayElementAtIndex(i);
                    if (string.IsNullOrWhiteSpace(layerInfo.stringValue))
                    {
                        layerInfo.stringValue = layerName;
                        tagManager.ApplyModifiedProperties();
                        break;
                    }
                }
            }
        }
    }

}
