using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace UGF.EditorTools
{
    internal sealed class AssemblySelectionListView
    {
        private readonly List<StripLinkConfigSelectionItem> _items = new List<StripLinkConfigSelectionItem>();
        private readonly GUIStyle _normalStyle;
        private readonly GUIStyle _selectedStyle;
        private Vector2 _scrollPosition;

        public AssemblySelectionListView()
        {
            _normalStyle = new GUIStyle
            {
                normal = { textColor = Color.white }
            };

            _selectedStyle = new GUIStyle
            {
                normal = { textColor = Color.green }
            };
        }

        public int Count => _items.Count;

        public void Reload(string[] allAssemblyNames, string[] selectedAssemblyNames)
        {
            _items.Clear();
            if (allAssemblyNames == null || allAssemblyNames.Length == 0)
            {
                return;
            }

            for (var i = 0; i < allAssemblyNames.Length; i++)
            {
                var assemblyName = allAssemblyNames[i];
                _items.Add(new StripLinkConfigSelectionItem(ArrayUtility.Contains(selectedAssemblyNames, assemblyName), assemblyName));
            }
        }

        public void Draw()
        {
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, false, true);
            for (var i = 0; i < _items.Count; i++)
            {
                var item = _items[i];
                EditorGUILayout.BeginHorizontal();
                item.IsSelected = EditorGUILayout.ToggleLeft(item.AssemblyName, item.IsSelected, item.IsSelected ? _selectedStyle : _normalStyle);
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();
        }

        public void SetAll(bool isSelected)
        {
            for (var i = 0; i < _items.Count; i++)
            {
                _items[i].IsSelected = isSelected;
            }
        }

        public string[] GetSelectedAssemblyNames()
        {
            var result = new List<string>(_items.Count);
            for (var i = 0; i < _items.Count; i++)
            {
                var item = _items[i];
                if (item.IsSelected)
                {
                    result.Add(item.AssemblyName);
                }
            }

            return result.ToArray();
        }
    }
}
