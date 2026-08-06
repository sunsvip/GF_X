using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace UGF.EditorTools.Build.ResourceRules
{
    /// <summary>
    /// Resource 规则编辑器，支持按规则配置自动生成 ResourceCollection.xml
    /// </summary>
    public partial class ResourceRuleEditor : EditorWindow
    {
        private ResourceRuleEditorData _configuration;
        private ReorderableList _ruleList;
        private Vector2 _scrollPosition = Vector2.zero;

        private void OnGUI()
        {
            if (_configuration == null)
            {
                Load();
            }

            if (_ruleList == null)
            {
                InitRuleListDrawer();
            }

            GUILayout.BeginHorizontal(EditorStyles.toolbar);
            if (GUILayout.Button("Add", EditorStyles.toolbarButton))
            {
                Add();
            }

            if (GUILayout.Button("Save", EditorStyles.toolbarButton))
            {
                Save();
            }

            if (GUILayout.Button("Refresh ResourceCollection.xml", EditorStyles.toolbarButton))
            {
                RefreshResourceCollection();
            }

            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Space(10);
            OnListElementLabelGUI();
            GUILayout.EndHorizontal();

            GUILayout.BeginVertical();
            GUILayout.Space(30);
            _scrollPosition = GUILayout.BeginScrollView(_scrollPosition);
            _ruleList.DoLayoutList();
            GUILayout.EndScrollView();
            GUILayout.EndVertical();

            if (GUI.changed)
            {
                EditorUtility.SetDirty(_configuration);
            }
        }
    }
}

