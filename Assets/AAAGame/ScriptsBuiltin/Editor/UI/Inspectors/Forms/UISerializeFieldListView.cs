#if UNITY_EDITOR
using GameFramework;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace UGF.EditorTools
{
    internal sealed class UISerializeFieldListView
    {
        private static readonly string[] VarPrefixArray = { "private", "protected", "public" };
        private const float FieldPrefixWidth = 80f;
        private const float FieldTypeWidth = 220f;

        private readonly GUIContent _prefixContent = new GUIContent();
        private readonly GUIContent _typeContent = new GUIContent();
        private ReorderableList[] _reorderableLists = System.Array.Empty<ReorderableList>();
        private SerializedProperty _fieldsProperty;
        private int _currentFieldIndex;
        private int _currentFoldoutItemIndex = -1;

        internal void Initialize(SerializedProperty fieldsProperty)
        {
            _fieldsProperty = fieldsProperty;
            if (fieldsProperty == null)
            {
                _reorderableLists = System.Array.Empty<ReorderableList>();
                _currentFoldoutItemIndex = -1;
                _currentFieldIndex = 0;
                return;
            }

            UISerializeFieldBindingService.SyncReorderableListSize(fieldsProperty, ref _reorderableLists);
        }

        internal void Draw(SerializedObject serializedObject, SerializedProperty fieldsProperty, Object undoTarget)
        {
            Initialize(fieldsProperty);
            for (int i = 0; i < fieldsProperty.arraySize; i++)
            {
                DrawFieldItem(serializedObject, fieldsProperty, undoTarget, i);
            }
        }

        private void DrawFieldItem(SerializedObject serializedObject, SerializedProperty fieldsProperty, Object undoTarget, int index)
        {
            var item = fieldsProperty.GetArrayElementAtIndex(index);
            var varNameProperty = item.FindPropertyRelative("VarName");
            var varTypeProperty = item.FindPropertyRelative("VarType");
            var targetsProperty = item.FindPropertyRelative("Targets");
            var varPrefixProperty = item.FindPropertyRelative("VarPrefix");

            int targetsCount = targetsProperty != null ? targetsProperty.arraySize : 0;
            bool foldoutItem = index == _currentFoldoutItemIndex;
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(Utility.Text.Format(foldoutItem ? "▼{0} [{1}]" : "▶{0} [{1}]", index, targetsCount), EditorStyles.label, GUILayout.Width(50)))
            {
                _currentFoldoutItemIndex = _currentFoldoutItemIndex == index ? -1 : index;
            }

            _prefixContent.text = UISerializeFieldBindingService.GetVarPrefix(varPrefixProperty.intValue);
            if (EditorGUILayout.DropdownButton(_prefixContent, FocusType.Passive, GUILayout.Width(FieldPrefixWidth)))
            {
                GenericMenu popupMenu = new GenericMenu();
                for (int prefixIndex = 0; prefixIndex < VarPrefixArray.Length; prefixIndex++)
                {
                    string varPrefix = VarPrefixArray[prefixIndex];
                    popupMenu.AddItem(new GUIContent(varPrefix), prefixIndex == varPrefixProperty.intValue, selectedIndex =>
                    {
                        serializedObject.Update();
                        varPrefixProperty.intValue = (int)selectedIndex;
                        serializedObject.ApplyModifiedProperties();
                    }, prefixIndex);
                }

                popupMenu.ShowAsContext();
            }

            _typeContent.text = varTypeProperty.stringValue;
            if (EditorGUILayout.DropdownButton(_typeContent, FocusType.Passive, GUILayout.MaxWidth(FieldTypeWidth)))
            {
                GenericMenu popupMenu = new GenericMenu();
                var popupContents = UISerializeFieldBindingService.GetPopupContents(targetsProperty);
                for (int contentIndex = 0; contentIndex < popupContents.Length; contentIndex++)
                {
                    var typeName = popupContents[contentIndex];
                    popupMenu.AddItem(new GUIContent(typeName), typeName.CompareTo(varTypeProperty.stringValue) == 0, selectedType =>
                    {
                        serializedObject.Update();
                        varTypeProperty.stringValue = selectedType.ToString();
                        serializedObject.ApplyModifiedProperties();
                    }, typeName);
                }

                popupMenu.ShowAsContext();
            }

            varNameProperty.stringValue = GUILayout.TextField(varNameProperty.stringValue, GUILayout.ExpandWidth(true));
            if (GUILayout.Button("+", GUILayout.Width(EditorGUIUtility.singleLineHeight)))
            {
                InsertField(fieldsProperty, undoTarget, index + 1);
            }

            if (GUILayout.Button("-", GUILayout.Width(EditorGUIUtility.singleLineHeight)))
            {
                RemoveField(fieldsProperty, undoTarget, index);
            }

            EditorGUILayout.EndHorizontal();
            if (foldoutItem && index < fieldsProperty.arraySize)
            {
                DrawTargetsList(serializedObject, fieldsProperty, index);
            }
        }

        private void DrawTargetsList(SerializedObject serializedObject, SerializedProperty fieldsProperty, int index)
        {
            var item = fieldsProperty.GetArrayElementAtIndex(index);
            var targetsProperty = item.FindPropertyRelative("Targets");
            _currentFieldIndex = index;
            ReorderableList reorderableList = _reorderableLists[index];
            if (reorderableList == null)
            {
                reorderableList = new ReorderableList(serializedObject, targetsProperty, true, false, true, true);
                reorderableList.drawElementCallback = DrawVariableTargets;
                _reorderableLists[index] = reorderableList;
            }
            else
            {
                reorderableList.serializedProperty = targetsProperty;
            }

            reorderableList.DoLayoutList();
        }

        private void InsertField(SerializedProperty fieldsProperty, Object undoTarget, int index)
        {
            Undo.RecordObject(undoTarget, undoTarget.name);
            fieldsProperty.InsertArrayElementAtIndex(index);
            ArrayUtility.Insert(ref _reorderableLists, index, null);
            var insertedField = fieldsProperty.GetArrayElementAtIndex(index);
            if (insertedField == null)
            {
                return;
            }

            var varNameProperty = insertedField.FindPropertyRelative("VarName");
            if (!string.IsNullOrEmpty(varNameProperty.stringValue))
            {
                varNameProperty.stringValue += index.ToString();
            }
        }

        private void RemoveField(SerializedProperty fieldsProperty, Object undoTarget, int index)
        {
            Undo.RecordObject(undoTarget, undoTarget.name);
            fieldsProperty.DeleteArrayElementAtIndex(index);
            ArrayUtility.RemoveAt(ref _reorderableLists, index);
            if (_currentFoldoutItemIndex >= fieldsProperty.arraySize)
            {
                _currentFoldoutItemIndex = fieldsProperty.arraySize - 1;
            }
        }

        private void DrawVariableTargets(Rect rect, int index, bool isActive, bool isFocused)
        {
            EditorGUI.BeginDisabledGroup(EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlaying);
            var field = _fieldsProperty != null && _currentFieldIndex < _fieldsProperty.arraySize
                ? _fieldsProperty.GetArrayElementAtIndex(_currentFieldIndex)
                : null;
            if (field != null)
            {
                var targetsProperty = field.FindPropertyRelative("Targets");
                var targetProperty = targetsProperty.GetArrayElementAtIndex(index);
                EditorGUI.LabelField(rect, index.ToString());
                rect.xMin += 50;
                EditorGUI.ObjectField(rect, targetProperty, GUIContent.none);
            }

            EditorGUI.EndDisabledGroup();
        }
    }
}
#endif
