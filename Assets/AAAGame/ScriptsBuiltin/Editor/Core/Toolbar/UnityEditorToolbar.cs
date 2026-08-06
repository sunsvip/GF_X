#if !UNITY_6000_3_OR_NEWER
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace UGF.EditorTools
{
    internal static class ToolbarCallback
    {
        private static ScriptableObject s_CurrentToolbar;

        public static Action OnToolbarGUILeft;
        public static Action OnToolbarGUIRight;

        static ToolbarCallback()
        {
            EditorApplication.update -= OnUpdate;
            EditorApplication.update += OnUpdate;
        }

        private static void OnUpdate()
        {
            if (ToolbarInternalApiBridge.ToolbarType == null)
            {
                return;
            }

            if (s_CurrentToolbar == null)
            {
                var toolbars = Resources.FindObjectsOfTypeAll(ToolbarInternalApiBridge.ToolbarType);
                s_CurrentToolbar = toolbars.Length > 0 ? toolbars[0] as ScriptableObject : null;
                if (s_CurrentToolbar == null)
                {
                    return;
                }

                var root = ToolbarInternalApiBridge.GetToolbarRoot(s_CurrentToolbar);
                if (root == null)
                {
                    return;
                }

                RegisterCallback(root, "ToolbarZoneLeftAlign", OnToolbarGUILeft);
                RegisterCallback(root, "ToolbarZoneRightAlign", OnToolbarGUIRight);
            }
        }

        private static void RegisterCallback(VisualElement root, string zoneName, Action callback)
        {
            var toolbarZone = root.Q(zoneName);
            if (toolbarZone == null)
            {
                return;
            }

            var parent = new VisualElement
            {
                style =
                {
                    flexGrow = 1,
                    flexDirection = FlexDirection.Row,
                }
            };

            var container = new IMGUIContainer();
            container.style.flexGrow = 1;
            container.onGUIHandler += () => callback?.Invoke();
            parent.Add(container);
            toolbarZone.Add(parent);
        }
    }

    [InitializeOnLoad]
    public static class UnityEditorToolbar
    {
        public static readonly List<Action> LeftToolbarGUI = new List<Action>();
        public static readonly List<Action> RightToolbarGUI = new List<Action>();

        static UnityEditorToolbar()
        {
            ToolbarCallback.OnToolbarGUILeft = GUILeft;
            ToolbarCallback.OnToolbarGUIRight = GUIRight;
        }

        public static void GUILeft()
        {
            GUILayout.BeginHorizontal();
            foreach (var handler in LeftToolbarGUI)
            {
                handler();
            }
            GUILayout.EndHorizontal();
        }

        public static void GUIRight()
        {
            GUILayout.BeginHorizontal();
            foreach (var handler in RightToolbarGUI)
            {
                handler();
            }
            GUILayout.EndHorizontal();
        }
    }
}
#endif
