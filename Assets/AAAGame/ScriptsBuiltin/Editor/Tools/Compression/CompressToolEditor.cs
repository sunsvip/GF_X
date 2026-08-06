using System;
using UnityEngine;

namespace UGF.EditorTools
{
    [EditorToolMenu("资源/压缩(优化)工具", null, 2)]
    public class CompressToolEditor : PersistentAssetSelectionToolEditorBase<CompressToolSubPanel>
    {
        public override string ToolName => "压缩(优化)工具";
        public override Vector2Int WinSize => new Vector2Int(600, 800);

        protected override string ClearListButtonText => "清除列表";
        protected override bool UseRootVerticalLayout => true;
        protected override bool WrapSelectionAreaInHelpBox => false;
        protected override bool WrapSettingsAreaInHelpBox => false;
        protected override bool ShowReadmeText => true;

        protected override CompressToolSubPanel CreateSubPanelInstance(Type panelType)
        {
            var panel = Activator.CreateInstance(panelType) as CompressToolSubPanel;
            panel?.Initialize(this);
            return panel;
        }
    }
}
