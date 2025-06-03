using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using System.Text;
using GameFramework;
public partial class HierarchyRightClickMenus
{
    [MenuItem("GameObject/GF Tools/Data Table/Copy Colors", false, priority = 2001)]
    static void CopyValueColors()
    {
        var objs = Selection.gameObjects;
        StringBuilder strBuilder = new StringBuilder();
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
    [MenuItem("GameObject/GF Tools/Data Table/Copy Colors Array", false, priority = 2002)]
    static void CopyValueColorsArray()
    {
        var objs = Selection.gameObjects;
        StringBuilder strBuilder = new StringBuilder();
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
