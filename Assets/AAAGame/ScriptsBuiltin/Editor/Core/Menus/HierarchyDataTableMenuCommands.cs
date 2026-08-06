using System.Text;
using GameFramework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace UGF.EditorTools
{
    public static class HierarchyDataTableMenuCommands
    {
        [MenuItem("GameObject/GF Tools/Data Table/Copy Colors", priority = 2001)]
        private static void CopyValueColors()
        {
            var objs = Selection.gameObjects;
            var strBuilder = new StringBuilder();
            foreach (var obj in objs)
            {
                if (obj == null) continue;

                if (obj.TryGetComponent<MaskableGraphic>(out var renderer))
                {
                    var color = renderer.color;
                    strBuilder.AppendLine(Utility.Text.Format("{0},{1},{2},{3}", color.r, color.g, color.b, color.a));
                }
            }

            EditorGUIUtility.systemCopyBuffer = strBuilder.ToString();
        }

        [MenuItem("GameObject/GF Tools/Data Table/Copy Colors Array", priority = 2002)]
        private static void CopyValueColorsArray()
        {
            var objs = Selection.gameObjects;
            var strBuilder = new StringBuilder();
            foreach (var obj in objs)
            {
                if (obj == null) continue;

                if (obj.TryGetComponent<MaskableGraphic>(out var renderer))
                {
                    var color = renderer.color;
                    strBuilder.Append(Utility.Text.Format("[{0},{1},{2},{3}],", color.r, color.g, color.b, color.a));
                }
            }

            if (strBuilder.Length == 0) return;
            EditorGUIUtility.systemCopyBuffer = strBuilder.ToString(0, strBuilder.Length - 1);
        }
    }
}
