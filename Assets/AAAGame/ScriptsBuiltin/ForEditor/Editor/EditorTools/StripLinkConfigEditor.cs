using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
namespace UGF.EditorTools
{
    internal enum ConfigEditorMode
    {
        StripLinkConfig,
        AotDllConfig
    }
    [EditorToolMenu("打包/代码裁剪配置",null, 1)]
    public class StripLinkConfigEditor : EditorToolBase
    {
        private class ItemData
        {
            public bool isOn;
            public string dllName;
            public ItemData(bool isOn, string dllName)
            {
                this.isOn = isOn;
                this.dllName = dllName;
            }
        }
        public override string ToolName => "代码裁剪配置";
        public override Vector2Int WinSize => new Vector2Int(600, 800);

        private Vector2 scrollPosition;
        private string[] selectedDllList;
        private List<ItemData> dataList;
        private GUIStyle normalStyle;
        private GUIStyle selectedStyle;

        ConfigEditorMode mode;


        private void OnEnable()
        {
            normalStyle = new GUIStyle();
            normalStyle.normal.textColor = Color.white;

            selectedStyle = new GUIStyle();
            selectedStyle.normal.textColor = Color.green;
            dataList = new List<ItemData>();

            InitEditorMode();
        }
        protected virtual void InitEditorMode()
        {
            SetEditorMode(ConfigEditorMode.StripLinkConfig);
        }
        internal void SetEditorMode(ConfigEditorMode mode)
        {
            this.mode = mode;
            RefreshListData();
        }
        private void OnGUI()
        {
            EditorGUILayout.BeginVertical();
            if (dataList.Count <= 0)
            {
                EditorGUILayout.HelpBox("未找到dll,请先Build项目以生成dll.", MessageType.Warning);
                if (GUILayout.Button("生成dll"))
                {
                    HybridCLR.Editor.Commands.StripAOTDllCommand.GenerateStripedAOTDlls();
                    RefreshListData();
                }
            }
            else
            {
                switch (mode)
                {
                    case ConfigEditorMode.StripLinkConfig:
                        EditorGUILayout.HelpBox("勾选需要添加到Link.xml的程序集,然后点击保存生效.", MessageType.Info);
                        break;
                    case ConfigEditorMode.AotDllConfig:
                        EditorGUILayout.HelpBox("勾选需要添加到AOT元数据补充的dll,然后点击保存生效.", MessageType.Info);
                        break;
                }
            }
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, false, true);
            for (int i = 0; i < dataList.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                var item = dataList[i];
                item.isOn = EditorGUILayout.ToggleLeft(item.dllName, item.isOn, item.isOn ? selectedStyle : normalStyle);
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();
            EditorGUILayout.BeginHorizontal();
            var btWidth = GUILayout.Width(100);
            var btHeight = GUILayout.Height(30);
            if (GUILayout.Button("全选", btWidth, btHeight))
            {
                SelectAll(true);
            }
            if (GUILayout.Button("全不选", btWidth, btHeight))
            {
                SelectAll(false);
            }
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("刷新列表", btWidth, btHeight))
            {
                RefreshListData();
            }
            if (GUILayout.Button("保存", btWidth, btHeight))
            {
                switch (mode)
                {
                    case ConfigEditorMode.StripLinkConfig:
                        if (StripLinkConfigTool.Save2LinkFile(GetCurrentSelectedList()))
                        {
                            EditorUtility.DisplayDialog("Strip LinkConfig Editor", "Update link.xml success!", "OK");
                        }
                        break;
                    case ConfigEditorMode.AotDllConfig:
                        if (StripLinkConfigTool.Save2AotDllList(GetCurrentSelectedList()))
                        {
                            EditorUtility.DisplayDialog("AOT dlls Editor", "Update AOT dll List success!", "OK");
                        }
                        break;
                }
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }
        private void SelectAll(bool isOn)
        {
            foreach (var item in dataList)
            {
                item.isOn = isOn;
            }
        }
        private string[] GetCurrentSelectedList()
        {
            List<string> result = new List<string>();
            foreach (var item in dataList)
            {
                if (item.isOn)
                {
                    result.Add(item.dllName);
                }
            }
            return result.ToArray();
        }
        private void RefreshListData()
        {
            dataList.Clear();

            switch (mode)
            {
                case ConfigEditorMode.StripLinkConfig:
                    selectedDllList = StripLinkConfigTool.GetSelectedAssemblyDlls();
                    break;
                case ConfigEditorMode.AotDllConfig:
                    selectedDllList = StripLinkConfigTool.GetSelectedAotDlls();
                    break;
            }
            foreach (var item in StripLinkConfigTool.GetProjectAssemblyDlls())
            {
                dataList.Add(new ItemData(IsInSelectedList(item), item));
            }
        }
        private bool IsInSelectedList(string dllName)
        {
            return ArrayUtility.Contains(selectedDllList, dllName);
        }
    }

}
