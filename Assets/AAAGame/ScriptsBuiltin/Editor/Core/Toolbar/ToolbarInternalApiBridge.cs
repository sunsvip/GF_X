using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace UGF.EditorTools
{
    internal static class ToolbarInternalApiBridge
    {
        private static readonly Assembly s_EditorAssembly = typeof(UnityEditor.Editor).Assembly;
        private static readonly Type s_ToolbarType = s_EditorAssembly.GetType("UnityEditor.Toolbar");
        private static readonly Type s_ConnectionUiHelperType = s_EditorAssembly.GetType("UnityEditor.Networking.PlayerConnection.ConnectionUIHelper");
        private static readonly MethodInfo s_GetIconMethod = s_ConnectionUiHelperType?.GetMethod("GetIcon", BindingFlags.Static | BindingFlags.Public);

        public static Type ToolbarType => s_ToolbarType;

        public static Texture GetBuildTargetIcon(BuildTarget buildTarget)
        {
            var platformIcon = s_GetIconMethod?.Invoke(null, new object[] { buildTarget.ToString() }) as GUIContent;
            return platformIcon?.image ?? EditorGUIUtility.FindTexture("BuildSettings.Editor.Small");
        }

        public static VisualElement GetToolbarRoot(ScriptableObject toolbar)
        {
            if (toolbar == null)
            {
                return null;
            }

            var rootField = toolbar.GetType().GetField("m_Root", BindingFlags.NonPublic | BindingFlags.Instance);
            return rootField?.GetValue(toolbar) as VisualElement;
        }
    }
}
