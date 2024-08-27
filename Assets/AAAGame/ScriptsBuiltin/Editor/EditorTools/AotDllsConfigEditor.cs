using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UGF.EditorTools
{
    [EditorToolMenu("热更/AOT泛型补充配置", null, 3)]
    public class AotDllsConfigEditor : StripLinkConfigEditor
    {
        public override string ToolName => "AOT泛型补充配置";
        protected override void InitEditorMode()
        {
            this.SetEditorMode(ConfigEditorMode.AotDllConfig);
        }
    }
}
