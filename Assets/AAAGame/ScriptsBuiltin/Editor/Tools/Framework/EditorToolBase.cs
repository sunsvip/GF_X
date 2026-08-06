using UnityEditor;
using UnityEngine;

namespace UGF.EditorTools
{
    public abstract class EditorToolBase : EditorWindow
    {
        public abstract string ToolName { get; }
        public abstract Vector2Int WinSize { get; }

        protected virtual void OnEnable()
        {
            SetWindowTitle(ToolName);
            ApplyWindowSettings();
        }

        protected void ApplyWindowSettings()
        {
            var windowSize = new Vector2(WinSize.x, WinSize.y);
            minSize = windowSize;
        }

        protected void SetWindowTitle(string title)
        {
            if (titleContent == null)
            {
                titleContent = new GUIContent(title);
                return;
            }

            titleContent.text = title;
        }
    }
}
