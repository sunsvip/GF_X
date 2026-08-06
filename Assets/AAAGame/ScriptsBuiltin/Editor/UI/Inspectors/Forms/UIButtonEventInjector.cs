using System;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace UGF.EditorTools
{
    internal static class UIButtonEventInjector
    {
        internal static bool BindString(Button button, MonoBehaviour target, string methodName, string argument)
        {
            if (button == null || target == null)
            {
                return false;
            }

            var callback = Delegate.CreateDelegate(typeof(UnityAction<string>), target, methodName, false) as UnityAction<string>;
            if (callback == null)
            {
                return false;
            }

            ResetPersistentListeners(button.onClick);
            UnityEventTools.AddStringPersistentListener(button.onClick, callback, argument);
            EditorUtility.SetDirty(button);
            return true;
        }

        internal static bool BindButton(Button button, MonoBehaviour target, string methodName)
        {
            if (button == null || target == null)
            {
                return false;
            }

            var callback = Delegate.CreateDelegate(typeof(UnityAction<Button>), target, methodName, false) as UnityAction<Button>;
            if (callback == null)
            {
                return false;
            }

            ResetPersistentListeners(button.onClick);
            UnityEventTools.AddObjectPersistentListener(button.onClick, callback, button);
            EditorUtility.SetDirty(button);
            return true;
        }

        internal static bool BindVoid(Button button, MonoBehaviour target, string methodName)
        {
            if (button == null || target == null)
            {
                return false;
            }

            var callback = Delegate.CreateDelegate(typeof(UnityAction), target, methodName, false) as UnityAction;
            if (callback == null)
            {
                return false;
            }

            ResetPersistentListeners(button.onClick);
            UnityEventTools.AddVoidPersistentListener(button.onClick, callback);
            EditorUtility.SetDirty(button);
            return true;
        }

        private static void ResetPersistentListeners(UnityEvent onClick)
        {
            for (var i = onClick.GetPersistentEventCount() - 1; i >= 0; i--)
            {
                UnityEventTools.RemovePersistentListener(onClick, i);
            }
        }
    }
}
