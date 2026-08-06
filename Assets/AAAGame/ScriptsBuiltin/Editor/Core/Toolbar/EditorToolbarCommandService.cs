using System;
using Unity.CodeEditor;
using UnityEditor;

namespace UGF.EditorTools
{
    internal static class EditorToolbarCommandService
    {
        internal static void OpenAppBuilder()
        {
            AppBuilderEditor.Open();
        }

        internal static void SelectAppConfigs()
        {
            Selection.activeObject = AppConfigs.GetInstanceEditor();
        }

        internal static void OpenCSharpProject()
        {
            AssetDatabase.Refresh();
            CodeEditor.Editor.CurrentCodeEditor.SyncAll();
            CodeEditor.Editor.CurrentCodeEditor.OpenProject();
        }

        internal static void OpenEditorTool(Type editorToolType, bool showAsUtility)
        {
            var window = EditorWindow.GetWindow(editorToolType);
            if (showAsUtility)
            {
                window.ShowUtility();
                return;
            }

            window.Show();
        }
    }
}
