using GameFramework.Localization;
using System;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace UGF.EditorTools
{
    internal sealed class LocalizationSupportLanguageListView
    {
        private const string Title = "多语言列表:";

        private readonly Language[] _languageAllOptions;
        private readonly Texture _mainLanguageIcon;
        private readonly ReorderableList _list;

        public LocalizationSupportLanguageListView()
        {
            _languageAllOptions = Enum.GetValues(typeof(Language)) as Language[];
            ArrayUtility.RemoveAt(ref _languageAllOptions, 0);
            _mainLanguageIcon = EditorGUIUtility.TrIconContent("Favorite@2x").image;

            _list = new ReorderableList(EditorToolSettings.Instance.LanguagesSupport, typeof(int), true, true, true, true)
            {
                multiSelect = true
            };
            _list.drawHeaderCallback = DrawHeader;
            _list.onAddCallback = OnAdd;
            _list.onRemoveCallback = OnRemove;
            _list.drawElementCallback = DrawElement;
            _list.onChangedCallback = OnListChanged;
            _list.onReorderCallback = OnListReordered;
        }

        public void Draw(Vector2 scrollPosition, out Vector2 nextScrollPosition)
        {
            nextScrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.MaxHeight(200));
            _list.DoLayoutList();
            EditorGUILayout.EndScrollView();
        }

        private void DrawHeader(Rect rect)
        {
            EditorGUI.LabelField(rect, Title);
        }

        private void DrawElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            if (index == 0)
            {
                var mainLanguageRect = rect;
                mainLanguageRect.x += 5;
                mainLanguageRect.width = EditorGUIUtility.singleLineHeight;
                mainLanguageRect.height = EditorGUIUtility.singleLineHeight;
                GUI.DrawTexture(mainLanguageRect, _mainLanguageIcon);
            }

            float mainLanguageIconWidth = EditorGUIUtility.singleLineHeight + 10;
            rect.x += mainLanguageIconWidth;
            rect.width -= mainLanguageIconWidth;
            var item = EditorToolSettings.Instance.LanguagesSupport[index];
            EditorGUI.LabelField(rect, ((Language)item).ToString());
        }

        private void OnAdd(ReorderableList list)
        {
            var unselectedLanguages = _languageAllOptions.Where(language => !EditorToolSettings.Instance.LanguagesSupport.Contains((int)language));
            var popupLanguages = new GenericMenu();
            foreach (var language in unselectedLanguages)
            {
                int currentLanguage = (int)language;
                popupLanguages.AddItem(new GUIContent(language.ToString()), false, () =>
                {
                    if (!EditorToolSettings.Instance.LanguagesSupport.Contains(currentLanguage))
                    {
                        EditorToolSettings.Instance.LanguagesSupport.Add(currentLanguage);
                        SaveSettings();
                    }
                });
            }

            popupLanguages.ShowAsContext();
        }

        private void OnRemove(ReorderableList list)
        {
            for (int i = list.selectedIndices.Count - 1; i >= 0; i--)
            {
                int index = list.selectedIndices[i];
                if (index >= 0 && index < EditorToolSettings.Instance.LanguagesSupport.Count)
                {
                    EditorToolSettings.Instance.LanguagesSupport.RemoveAt(index);
                }
            }

            SaveSettings();
        }

        private static void OnListChanged(ReorderableList list)
        {
            SaveSettings();
        }

        private static void OnListReordered(ReorderableList list)
        {
            SaveSettings();
        }

        private static void SaveSettings()
        {
            EditorToolSettings.Save();
        }
    }
}
