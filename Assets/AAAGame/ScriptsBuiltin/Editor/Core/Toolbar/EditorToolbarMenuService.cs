using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace UGF.EditorTools
{
    internal static class EditorToolbarMenuService
    {
        internal static void ShowSceneMenu()
        {
            var sceneEntries = SceneQuickOpenService.GetSceneEntries();
            var popMenu = new GenericMenu
            {
                allowDuplicateNames = true
            };

            for (var i = 0; i < sceneEntries.Count; i++)
            {
                var sceneEntry = sceneEntries[i];
                popMenu.AddItem(new GUIContent(sceneEntry.DisplayName), false, menuIndex =>
                {
                    var entry = sceneEntries[(int)menuIndex];
                    SceneQuickOpenService.TryOpenScene(entry.AssetPath);
                }, i);
            }

            popMenu.ShowAsContext();
        }

        internal static void ShowEditorToolMenu()
        {
            var editorToolList = EditorToolRegistry.GetOrderedEditorToolTypes();
            var popMenu = new GenericMenu();
            for (var i = 0; i < editorToolList.Count; i++)
            {
                var editorToolType = editorToolList[i];
                var toolAttribute = editorToolType.GetCustomAttribute<EditorToolMenuAttribute>();
                if (toolAttribute == null)
                {
                    continue;
                }

                var showAsUtility = toolAttribute.IsUtility;
                popMenu.AddItem(new GUIContent(toolAttribute.ToolMenuPath), false, () =>
                {
                    EditorToolbarCommandService.OpenEditorTool(editorToolType, showAsUtility);
                });
            }

            popMenu.ShowAsContext();
        }
    }
}
