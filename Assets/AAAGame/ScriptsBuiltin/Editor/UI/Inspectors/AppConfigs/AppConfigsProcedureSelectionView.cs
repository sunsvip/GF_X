using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace UGF.EditorTools
{
    internal sealed class AppConfigsProcedureSelectionView
    {
        private readonly GUIStyle _normalStyle;
        private readonly GUIStyle _selectedStyle;
        private readonly GUIContent _titleContent;
        private AppConfigsSelectableItem[] _procedures = System.Array.Empty<AppConfigsSelectableItem>();

        internal bool Foldout = true;
        internal Vector2 ScrollPos;

        internal AppConfigsProcedureSelectionView(GUIStyle normalStyle, GUIStyle selectedStyle)
        {
            _normalStyle = normalStyle;
            _selectedStyle = selectedStyle;
            _titleContent = new GUIContent("流程(Procedures)", "勾选的流程在有限状态机中有效");
        }

        internal void Reload(AppConfigs config)
        {
            _procedures = AppConfigsProcedureSelectionService.LoadProcedures(config);
        }

        internal bool DrawPanel(GUILayoutOption perItemWidth)
        {
            Foldout = EditorGUILayout.Foldout(Foldout, _titleContent);
            if (!Foldout)
            {
                return false;
            }

            var changed = false;
            EditorGUILayout.BeginVertical();
            {
                ScrollPos = EditorGUILayout.BeginScrollView(ScrollPos, "box", GUILayout.Height(200));
                {
                    EditorGUI.BeginChangeCheck();
                    for (var i = 0; i < _procedures.Length; i++)
                    {
                        if (i % AppConfigsInspector.ONE_LINE_SHOW_COUNT == 0)
                        {
                            EditorGUILayout.BeginHorizontal();
                        }

                        var item = _procedures[i];
                        item.IsOn = EditorGUILayout.ToggleLeft(item.Name, item.IsOn, item.IsOn ? _selectedStyle : _normalStyle, perItemWidth);
                        if (i % AppConfigsInspector.ONE_LINE_SHOW_COUNT == AppConfigsInspector.ONE_LINE_SHOW_COUNT - 1)
                        {
                            EditorGUILayout.EndHorizontal();
                        }
                    }

                    changed = EditorGUI.EndChangeCheck();
                    if (_procedures.Length % AppConfigsInspector.ONE_LINE_SHOW_COUNT != 0)
                    {
                        EditorGUILayout.EndHorizontal();
                    }
                }
                EditorGUILayout.EndScrollView();
            }
            EditorGUILayout.EndVertical();
            return changed;
        }

        internal string[] GetSelectedItems()
        {
            var selectedProcedures = new List<string>(_procedures.Length);
            for (var i = 0; i < _procedures.Length; i++)
            {
                if (_procedures[i].IsOn)
                {
                    selectedProcedures.Add(_procedures[i].Name);
                }
            }

            return selectedProcedures.ToArray();
        }
    }
}
