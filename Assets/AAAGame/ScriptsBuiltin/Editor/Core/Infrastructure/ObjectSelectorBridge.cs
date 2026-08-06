using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityObject = UnityEngine.Object;

namespace UGF.EditorTools
{
    internal static class ObjectSelectorBridge
    {
        private const string ObjectSelectorClosedCommand = "ObjectSelectorClosed";
        private static readonly Dictionary<int, Action<UnityObject>> SelectorClosedCallbacks = new Dictionary<int, Action<UnityObject>>(4);

        internal static bool Open(Type assetType, string searchFilter = null, Action<UnityObject> onObjectSelectorClosed = null, int objectSelectorId = 0)
        {
            SelectorClosedCallbacks[objectSelectorId] = onObjectSelectorClosed;
            EditorGUIUtility.ShowObjectPicker<UnityObject>(null, false, searchFilter ?? string.Empty, objectSelectorId);
            return true;
        }

        internal static void HandleObjectSelectorEvent(Event currentEvent)
        {
            if (currentEvent == null ||
                currentEvent.type != EventType.ExecuteCommand ||
                !string.Equals(currentEvent.commandName, ObjectSelectorClosedCommand, StringComparison.Ordinal))
            {
                return;
            }

            var objectSelectorId = EditorGUIUtility.GetObjectPickerControlID();
            if (!SelectorClosedCallbacks.TryGetValue(objectSelectorId, out var onClosed))
            {
                return;
            }

            SelectorClosedCallbacks.Remove(objectSelectorId);
            onClosed?.Invoke(EditorGUIUtility.GetObjectPickerObject());
            currentEvent.Use();
        }
    }
}
