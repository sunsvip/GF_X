#if UNITY_EDITOR
using System;
using System.Linq;
using System.Text.RegularExpressions;
using GameFramework;
using UnityEngine;

namespace UGF.EditorTools
{
    internal static class UISerializeFieldEditorUtility
    {
        private const string ArrayFieldSuffix = "Arr";

        internal static ISerializeFieldTool GetSerializeFieldTool(GameObject go)
        {
            if (go == null)
            {
                return null;
            }

            var parent = go.transform.parent;
            while (parent != null)
            {
                var mono = parent.GetComponents<MonoBehaviour>().FirstOrDefault(item => item is ISerializeFieldTool);
                if (mono != null)
                {
                    return mono as ISerializeFieldTool;
                }

                parent = parent.parent;
            }

            return go.GetComponents<MonoBehaviour>().FirstOrDefault(item => item is ISerializeFieldTool) as ISerializeFieldTool;
        }

        internal static GameObject[] GetTargetsFromSelectedNodes(GameObject[] selectedList)
        {
            if (selectedList == null || selectedList.Length == 0)
            {
                return Array.Empty<GameObject>();
            }

            var targets = new GameObject[selectedList.Length];
            Array.Copy(selectedList, targets, selectedList.Length);
            return targets.OrderBy(go => go.transform.GetSiblingIndex()).ToArray();
        }

        internal static string GetDisplayVarTypeName(string varFullTypeName)
        {
            if (string.IsNullOrWhiteSpace(varFullTypeName))
            {
                return string.Empty;
            }

            int dotIndex = varFullTypeName.LastIndexOf('.');
            return dotIndex >= 0 ? varFullTypeName[(dotIndex + 1)..] : varFullTypeName;
        }

        internal static string GenerateFieldName(SerializeFieldData[] fields, GameObject[] targets)
        {
            var go = targets[0];
            string varName = Regex.Replace(go.name, "[^\\w]", string.Empty);
            if (targets.Length > 1)
            {
                varName += ArrayFieldSuffix;
            }

            if (fields == null || fields.Length == 0)
            {
                return GetFieldVarName(varName);
            }

            bool contains = false;
            for (var i = 0; i < fields.Length; i++)
            {
                var item = fields[i];
                if (item != null && item.VarName.CompareTo(varName) == 0)
                {
                    contains = true;
                    break;
                }
            }

            if (contains)
            {
                varName = Utility.Text.Format("{0}_{1}", varName, go.transform.GetSiblingIndex());
            }

            return GetFieldVarName(varName);
        }

        private static string GetFieldVarName(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            return Utility.Text.Format("var{0}{1}", char.ToUpperInvariant(value[0]), value[1..]);
        }
    }
}
#endif
