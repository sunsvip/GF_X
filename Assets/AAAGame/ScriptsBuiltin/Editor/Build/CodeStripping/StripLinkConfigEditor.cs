using UnityEditor;
using UnityEngine;

namespace UGF.EditorTools
{
    internal enum ConfigEditorMode
    {
        StripLinkConfig,
        AotDllConfig,
        Netstandard2NetFrameworkConfig
    }
    [EditorToolMenu("打包/代码裁剪配置",null, 1)]
    public class StripLinkConfigEditor : EditorToolBase
    {
        public override string ToolName => "代码裁剪配置";
        public override Vector2Int WinSize => new Vector2Int(600, 800);

        private AssemblySelectionListView _assemblyListView;
        private ConfigEditorMode _mode;


        protected override void OnEnable()
        {
            base.OnEnable();
            _assemblyListView = new AssemblySelectionListView();
            InitEditorMode();
        }

        protected virtual void InitEditorMode()
        {
            SetEditorMode(ConfigEditorMode.StripLinkConfig);
        }

        internal void SetEditorMode(ConfigEditorMode mode)
        {
            _mode = mode;
            RefreshListData();
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginVertical();
            if (_assemblyListView.Count <= 0)
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
                EditorGUILayout.HelpBox(GetHelpMessage(), MessageType.Info);
            }

            _assemblyListView.Draw();
            EditorGUILayout.BeginHorizontal();
            var btWidth = GUILayout.Width(100);
            var btHeight = GUILayout.Height(30);
            if (GUILayout.Button("全选", btWidth, btHeight))
            {
                _assemblyListView.SetAll(true);
            }
            if (GUILayout.Button("全不选", btWidth, btHeight))
            {
                _assemblyListView.SetAll(false);
            }
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("刷新列表", btWidth, btHeight))
            {
                RefreshListData();
            }
            if (GUILayout.Button("保存", btWidth, btHeight))
            {
                if (TrySaveCurrentMode())
                {
                    EditorUtility.DisplayDialog(GetDialogTitle(), GetSuccessMessage(), "OK");
                }
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        private void RefreshListData()
        {
            string[] selectedDllList;
            switch (_mode)
            {
                case ConfigEditorMode.StripLinkConfig:
                    selectedDllList = StripLinkConfigRepository.GetSelectedAssemblyDlls();
                    break;
                case ConfigEditorMode.AotDllConfig:
                    selectedDllList = StripLinkConfigRepository.GetSelectedAotDlls();
                    break;
                case ConfigEditorMode.Netstandard2NetFrameworkConfig:
                    selectedDllList = StripLinkConfigRepository.GetSelectedNetframeworkDlls();
                    break;
                default:
                    selectedDllList = null;
                    break;
            }

            _assemblyListView.Reload(StripLinkConfigRepository.GetProjectAssemblyDlls(), selectedDllList);
        }

        private string GetHelpMessage()
        {
            switch (_mode)
            {
                case ConfigEditorMode.StripLinkConfig:
                    return "勾选需要添加到Link.xml的程序集,然后点击保存生效.";
                case ConfigEditorMode.AotDllConfig:
                    return "勾选需要添加到AOT元数据补充的dll,然后点击保存生效.";
                case ConfigEditorMode.Netstandard2NetFrameworkConfig:
                    return "勾选需要转换为NetFramework的dll";
                default:
                    return string.Empty;
            }
        }

        private bool TrySaveCurrentMode()
        {
            var selectedAssemblyNames = _assemblyListView.GetSelectedAssemblyNames();
            switch (_mode)
            {
                case ConfigEditorMode.StripLinkConfig:
                    return StripLinkConfigRepository.SaveLinkConfig(selectedAssemblyNames);
                case ConfigEditorMode.AotDllConfig:
                    return StripLinkConfigRepository.SaveAotDllList(selectedAssemblyNames);
                case ConfigEditorMode.Netstandard2NetFrameworkConfig:
                    return StripLinkConfigRepository.SaveNetstandard2NetFrameworkConfig(selectedAssemblyNames);
                default:
                    return false;
            }
        }

        private string GetDialogTitle()
        {
            switch (_mode)
            {
                case ConfigEditorMode.StripLinkConfig:
                    return "Strip LinkConfig Editor";
                case ConfigEditorMode.AotDllConfig:
                    return "AOT dlls Editor";
                case ConfigEditorMode.Netstandard2NetFrameworkConfig:
                    return "Netstandard to NetFramework Editor";
                default:
                    return "Strip Link Config";
            }
        }

        private string GetSuccessMessage()
        {
            switch (_mode)
            {
                case ConfigEditorMode.StripLinkConfig:
                    return "Update link.xml success!";
                case ConfigEditorMode.AotDllConfig:
                    return "Update AOT dll List success!";
                case ConfigEditorMode.Netstandard2NetFrameworkConfig:
                    return "Update config success!";
                default:
                    return "Update config success!";
            }
        }
    }
}
