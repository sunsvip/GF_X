using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace UGF.EditorTools
{
    public abstract class EditorToolBase : EditorWindow
    {
        public abstract string ToolName { get; }
        public abstract Vector2Int WinSize { get; }
        private void Awake()
        {
            this.titleContent = new GUIContent(ToolName);
            this.position.Set(this.position.x, this.position.y, this.WinSize.x, this.WinSize.y);
        }
    }

}
