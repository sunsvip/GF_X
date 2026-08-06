using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UGF.EditorTools
{
    [EditorToolMenu("热更/Netstandard to NetFramework", null, 4)]
    public class NS2NFConfigEditor : StripLinkConfigEditor
    {
        public override string ToolName => "Netstandard to NetFramework dll";
        protected override void InitEditorMode()
        {
            SetEditorMode(ConfigEditorMode.Netstandard2NetFrameworkConfig);
        }
    }
}
