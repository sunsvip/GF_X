#if UNITY_EDITOR
using GameFramework;
using UnityEditor;
using UnityEngine;

namespace UGF.EditorTools
{
    [InitializeOnLoad]
    internal static class UIFormHierarchyOverlay
    {
        private static GUIStyle s_VariableLabelStyle;
        private static int s_PendingVarPrefixIndex = -1;

        static UIFormHierarchyOverlay()
        {
            Selection.selectionChanged -= OnSelectionChanged;
            Selection.selectionChanged += OnSelectionChanged;
            EditorApplication.hierarchyWindowItemOnGUI -= OnHierarchyItemOnGUI;
            EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyItemOnGUI;
        }

        internal static void QueueAddVariableSelection(int varPrefixIndex)
        {
            s_PendingVarPrefixIndex = varPrefixIndex;
        }

        private static void OnSelectionChanged()
        {
            UIFormMenuCommands.ResetSelectionState();
        }

        private static void OnHierarchyItemOnGUI(int instanceId, Rect rect)
        {
            OpenSelectComponentMenu(rect);
            EnsureLabelStyle();
#if UNITY_6000_3_OR_NEWER
            var currentNode = EditorUtility.EntityIdToObject(instanceId) as GameObject;
#else
            var currentNode = EditorUtility.InstanceIDToObject(instanceId) as GameObject;
#endif
            if (currentNode == null)
            {
                return;
            }

            var uiFormTool = UISerializeFieldEditorUtility.GetSerializeFieldTool(currentNode);
            if (uiFormTool == null)
            {
                return;
            }

            var fields = uiFormTool.SerializeFieldArr;
            if (fields == null)
            {
                return;
            }

            for (var i = 0; i < fields.Length; i++)
            {
                var item = fields[i];
                if (item == null || item.Targets == null)
                {
                    continue;
                }

                if (!ArrayUtility.Contains(item.Targets, currentNode))
                {
                    continue;
                }

                var displayContent = EditorGUIUtility.TrTextContent(Utility.Text.Format(
                    "{0} {1} {2}",
                    UISerializeFieldBindingService.GetVarPrefix(item.VarPrefix),
                    UISerializeFieldEditorUtility.GetDisplayVarTypeName(item.VarType),
                    item.VarName));
                Vector2 itemLabelSize = s_VariableLabelStyle.CalcSize(displayContent);
                var itemLabelRect = new Rect(rect.x, rect.y, itemLabelSize.x, itemLabelSize.y);
                itemLabelRect.y = rect.y;
                itemLabelRect.width = Mathf.Min(rect.width * 0.4f, itemLabelRect.width);
                itemLabelRect.x = rect.xMax - itemLabelRect.width;
                if (itemLabelRect.width > 100f)
                {
                    GUI.Label(itemLabelRect, displayContent, s_VariableLabelStyle);
                }

                break;
            }
        }

        private static void OpenSelectComponentMenu(Rect rect)
        {
            if (s_PendingVarPrefixIndex < 0)
            {
                return;
            }

            var targets = UISerializeFieldEditorUtility.GetTargetsFromSelectedNodes(Selection.gameObjects);
            var popupContents = UISerializeFieldBindingService.GetPopupContents(targets);
            if (popupContents.Length == 0)
            {
                s_PendingVarPrefixIndex = -1;
                return;
            }

            var menuContents = new GUIContent[popupContents.Length];
            for (var i = 0; i < popupContents.Length; i++)
            {
                menuContents[i] = new GUIContent(popupContents[i]);
            }

            var selectedPrefix = s_PendingVarPrefixIndex;
            rect.width = 200f;
            rect.height = Mathf.Max(100f, menuContents.Length * rect.height);
            EditorUtility.DisplayCustomMenu(rect, menuContents, -1, (_, contents, selected) =>
            {
                UIFormMenuCommands.AddToFields(selectedPrefix, contents[selected]);
            }, null);
            s_PendingVarPrefixIndex = -1;
        }

        private static void EnsureLabelStyle()
        {
            if (s_VariableLabelStyle != null)
            {
                return;
            }

            s_VariableLabelStyle = new GUIStyle(EditorStyles.helpBox)
            {
                stretchWidth = false,
                stretchHeight = false,
                fontStyle = FontStyle.Bold
            };
            s_VariableLabelStyle.normal.textColor = Color.white * 0.88f;
            s_VariableLabelStyle.hover.textColor = Color.cyan;
        }
    }
}
#endif
