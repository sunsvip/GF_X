using System;
using UnityEngine;

namespace UGF.EditorTools
{
    /// <summary>
    /// 批处理操作工具
    /// </summary>
    public abstract class UtilityToolEditorBase : PersistentAssetSelectionToolEditorBase<UtilitySubToolBase>
    {
        public override Vector2Int WinSize => new Vector2Int(600, 800);

        protected override UtilitySubToolBase CreateSubPanelInstance(Type panelType)
        {
            var panel = Activator.CreateInstance(panelType) as UtilitySubToolBase;
            panel?.Initialize(this);
            return panel;
        }
    }
}
