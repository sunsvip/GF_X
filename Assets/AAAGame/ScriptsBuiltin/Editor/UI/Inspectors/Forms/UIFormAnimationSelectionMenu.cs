#if UNITY_EDITOR
using UnityEditor;
using UnityGameFramework.Runtime;

namespace UGF.EditorTools
{
    internal static class UIFormAnimationSelectionMenu
    {
        internal static void ShowAnimationNames(SerializedObject serializedObject, UIFormBase uiForm, SerializedProperty property)
        {
            var clips = AnimationUtility.GetAnimationClips(uiForm.gameObject);
            if (clips == null || clips.Length == 0)
            {
                return;
            }

            var dropdownMenu = new GenericMenu();
            for (var i = 0; i < clips.Length; i++)
            {
                var clip = clips[i];
                if (clip == null)
                {
                    continue;
                }

                var clipName = clip.name;
                dropdownMenu.AddItem(new UnityEngine.GUIContent(clipName), property.stringValue == clipName, () =>
                {
                    serializedObject.Update();
                    property.stringValue = clipName;
                    serializedObject.ApplyModifiedProperties();
                });
            }

            dropdownMenu.AddItem(new UnityEngine.GUIContent("NULL"), property.stringValue == string.Empty, () =>
            {
                serializedObject.Update();
                property.stringValue = string.Empty;
                serializedObject.ApplyModifiedProperties();
            });
            dropdownMenu.ShowAsContext();
        }

        internal static void ShowUIAnimation(SerializedObject serializedObject, SerializedProperty property)
        {
            if (property.objectReferenceValue == null)
            {
                return;
            }

            var currentSequence = property.objectReferenceValue as DOTweenSequence;
            var animations = currentSequence.GetComponents<DOTweenSequence>();
            var dropdownMenu = new GenericMenu();
            for (var i = 0; i < animations.Length; i++)
            {
                var item = animations[i];
                dropdownMenu.AddItem(new UnityEngine.GUIContent(GameFramework.Utility.Text.Format("Index {0}", i)), currentSequence == item, () =>
                {
                    serializedObject.Update();
                    property.objectReferenceValue = item;
                    serializedObject.ApplyModifiedProperties();
                });
            }

            dropdownMenu.ShowAsContext();
        }
    }
}
#endif
