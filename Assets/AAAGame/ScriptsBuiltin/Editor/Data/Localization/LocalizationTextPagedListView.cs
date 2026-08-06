using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace UGF.EditorTools
{
    internal sealed class LocalizationTextPagedListView
    {
        private const int DefaultPageSize = 100;

        private Vector2 _scrollViewPosition;
        private int _pageSize = DefaultPageSize;
        private int _currentPage;

        public void Reset()
        {
            _currentPage = 0;
            _scrollViewPosition = Vector2.zero;
            _pageSize = DefaultPageSize;
        }

        public void Draw(List<LocalizationText> localizationTexts)
        {
            EditorGUILayout.BeginVertical("box");
            int totalCount = localizationTexts.Count;
            int effectivePageSize = Mathf.Max(1, _pageSize);
            int totalPages = Mathf.Max(1, Mathf.CeilToInt(totalCount / (float)effectivePageSize));
            int clampedPage = Mathf.Clamp(_currentPage, 0, totalPages - 1);
            if (clampedPage != _currentPage)
            {
                _scrollViewPosition = Vector2.zero;
            }

            _currentPage = clampedPage;
            int startIndex = _currentPage * effectivePageSize;
            int endIndex = Mathf.Min(startIndex + effectivePageSize, totalCount);

            DrawPaginationBar(totalCount, totalPages);
            _scrollViewPosition = EditorGUILayout.BeginScrollView(_scrollViewPosition);
            for (int i = startIndex; i < endIndex; i++)
            {
                DrawItem(localizationTexts[i], i);
            }

            EditorGUILayout.EndScrollView();
            UpdateDynamicPageSize(totalCount);
            EditorGUILayout.EndVertical();
        }

        private void DrawPaginationBar(int totalCount, int totalPages)
        {
            EditorGUILayout.BeginHorizontal();
            GUI.enabled = _currentPage > 0;
            if (GUILayout.Button("上一页", GUILayout.Width(80)))
            {
                _currentPage--;
                _scrollViewPosition = Vector2.zero;
            }

            GUI.enabled = _currentPage < totalPages - 1;
            if (GUILayout.Button("下一页", GUILayout.Width(80)))
            {
                _currentPage++;
                _scrollViewPosition = Vector2.zero;
            }

            GUI.enabled = true;
            GUILayout.FlexibleSpace();
            if (totalCount > 0)
            {
                EditorGUILayout.LabelField($"第 {_currentPage + 1}/{totalPages} 页   共{totalCount}条", GUILayout.ExpandWidth(false));
            }
            else
            {
                EditorGUILayout.LabelField("暂无数据", GUILayout.ExpandWidth(false));
            }

            EditorGUILayout.EndHorizontal();
        }

        private static void DrawItem(LocalizationText localizationText, int index)
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            EditorGUILayout.LabelField(index.ToString(), GUILayout.Width(50));
            localizationText.Locked = EditorGUILayout.ToggleLeft(EditorGUIUtility.TrIconContent("LockIcon-On", "勾选锁住,将强制保留此行"), localizationText.Locked, GUILayout.Width(40));
            localizationText.Key = EditorGUILayout.TextField(localizationText.Key);
            GUILayout.Space(5);
            localizationText.Value = EditorGUILayout.TextField(localizationText.Value);
            EditorGUILayout.EndHorizontal();
        }

        private void UpdateDynamicPageSize(int totalCount)
        {
            if (Event.current.type != EventType.Repaint)
            {
                return;
            }

            Rect scrollRect = GUILayoutUtility.GetLastRect();
            float itemHeight = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing + 6f;
            if (itemHeight <= 0f || scrollRect.height <= 0f)
            {
                return;
            }

            int dynamicPageSize = Mathf.Max(1, Mathf.FloorToInt(scrollRect.height / itemHeight));
            if (dynamicPageSize == _pageSize)
            {
                return;
            }

            _pageSize = dynamicPageSize;
            int totalPages = Mathf.Max(1, Mathf.CeilToInt(totalCount / (float)_pageSize));
            _currentPage = Mathf.Clamp(_currentPage, 0, totalPages - 1);
            _scrollViewPosition = Vector2.zero;
        }
    }
}
