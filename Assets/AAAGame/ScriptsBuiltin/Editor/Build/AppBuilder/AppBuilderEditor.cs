using GameFramework;
using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using GameFramework.Resource;
using HybridCLR.Editor.Commands;
using System.Text;
using static UnityEditor.BuildPlayerWindow;
using System.Collections;
using UnityEditor.Build.Reporting;
using UnityGameFramework.Editor.ResourceTools;
using UnityEditor.Build;
using System.Xml;
using UGF.EditorTools.Build.ResourceRules;
using Obfuz.Unity;

namespace UGF.EditorTools
{
    /// <summary>
    /// 资源生成器。
    /// </summary>
    public partial class AppBuilderEditor : EditorWindow
    {
        private readonly string[] _keystoreExtNames = { ".keystore", ".jks", ".ks" };
        private ResourceBuilderController _controller = null;
        private bool _queueBuildResources = false;
        private int _compressionHelperTypeNameIndex = 0;
        private int _buildEventHandlerTypeNameIndex = 0;
        private GUIContent _hotfixUrlContent;
        private GUIContent _applicableVersionContent;
        private GUIContent _forceUpdateAppContent;
        private GUIContent _appUpdateUrlContent;
        private GUIContent _appUpdateDescriptionContent;
        private GUIContent _revealFolderContent;
        private GUIContent _buildResourcesButtonContent;
        private GUIContent _buildAppButtonContent;
        private GUIContent _saveButtonContent;
        private GUIContent _playerSettingsButtonContent;
        private GUIContent _appSettingsButtonContent;
        private GUIContent _hybridClrSettingsButtonContent;
        private GUIContent _netstandardToFrameworkContent;
        private Vector2 _scrollPosition;
        private GUIStyle _dropDownButtonStyle;

        public static void Open()
        {
            AppBuilderEditor window = GetWindow<AppBuilderEditor>("App Builder", true);
            window.minSize = new Vector2(800f, 800f);
        }

        private void OnEnable()
        {
            _hotfixUrlContent = new GUIContent("Update Prefix Uri", "热更新资源服务器地址");
            _netstandardToFrameworkContent = new GUIContent("Netstandard dll -> NetFramework dll", "将Netstandard转换为NetFramework库, 以解决生成桥接函数失败.");
            _applicableVersionContent = new GUIContent("Applicable Version", "资源适用的客户端版本号,多版本用'|'分割");
            _forceUpdateAppContent = new GUIContent("Force Update", "是否强制更新App");
            _appUpdateUrlContent = new GUIContent("App Update Url", "App更新下载地址");
            _appUpdateDescriptionContent = new GUIContent("App Update Description:", "App更新公告,用于显示在对话框(支持TextMeshPro富文本)");
            _revealFolderContent = new GUIContent("Reveal Folder", "打包完成后打开资源输出目录");
            _buildResourcesButtonContent = EditorGUIUtility.TrTextContentWithIcon("Build Resources", "打AB包/热更", "CloudConnect@2x");
            _buildAppButtonContent = EditorGUIUtility.TrTextContentWithIcon("Build App", "打新包,首次打热更包请使用Full Build", "UnityLogo");

            _playerSettingsButtonContent = EditorGUIUtility.TrTextContentWithIcon("Player Settings", "打开Player Settings界面", "Settings");
            _appSettingsButtonContent = EditorGUIUtility.TrTextContentWithIcon("App Settings", "打开App Settings界面", "Settings");
            _hybridClrSettingsButtonContent = EditorGUIUtility.TrTextContentWithIcon("Hotfix Settings", "打开HybridCLR Settings界面", "Settings");
            _saveButtonContent = EditorGUIUtility.TrTextContentWithIcon("Save", "保存设置", "SaveAs@2x");

            var dropDownToggleButton = EditorGUIUtility.GetBuiltinSkin(EditorSkin.Inspector).FindStyle("DropDownToggleButton");
            _dropDownButtonStyle = new GUIStyle(dropDownToggleButton);
            _dropDownButtonStyle.normal.textColor = Color.white;
            _dropDownButtonStyle.alignment = TextAnchor.MiddleCenter;
            _dropDownButtonStyle.hover.textColor = Color.white;
            _dropDownButtonStyle.active.textColor = Color.white;

            if (AppSettings.Instance == null)
            {
                if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                {
                    AssetDatabase.CreateFolder("Assets", "Resources");
                }

                AssetDatabase.CreateAsset(CreateInstance<AppSettings>(), "Assets/Resources/AppSettings.asset");
                AssetDatabase.SaveAssets();
            }

            _controller = new ResourceBuilderController();
            _controller.OnLoadingResource += OnLoadingResource;
            _controller.OnLoadingAsset += OnLoadingAsset;
            _controller.OnLoadCompleted += OnLoadCompleted;
            _controller.OnAnalyzingAsset += OnAnalyzingAsset;
            _controller.OnAnalyzeCompleted += OnAnalyzeCompleted;
            _controller.ProcessingAssetBundle += OnProcessingAssetBundle;
            _controller.ProcessingBinary += OnProcessingBinary;
            _controller.ProcessResourceComplete += OnProcessResourceComplete;
            _controller.BuildResourceError += OnBuildResourceError;
            _queueBuildResources = false;

            if (_controller.Load())
            {
                Debug.Log("Load configuration success.");

                _compressionHelperTypeNameIndex = 0;
                string[] compressionHelperTypeNames = _controller.GetCompressionHelperTypeNames();
                for (int i = 0; i < compressionHelperTypeNames.Length; i++)
                {
                    if (_controller.CompressionHelperTypeName == compressionHelperTypeNames[i])
                    {
                        _compressionHelperTypeNameIndex = i;
                        break;
                    }
                }

                _controller.RefreshCompressionHelper();

                _buildEventHandlerTypeNameIndex = 0;
                string[] buildEventHandlerTypeNames = _controller.GetBuildEventHandlerTypeNames();
                for (int i = 0; i < buildEventHandlerTypeNames.Length; i++)
                {
                    if (_controller.BuildEventHandlerTypeName == buildEventHandlerTypeNames[i])
                    {
                        _buildEventHandlerTypeNameIndex = i;
                        break;
                    }
                }

                _controller.RefreshBuildEventHandler();
            }
            else
            {
                Debug.LogWarning("Load configuration failure.");
            }

            if (string.IsNullOrWhiteSpace(_controller.OutputDirectory) || !Directory.Exists(_controller.OutputDirectory))
            {
                _controller.OutputDirectory = ConstEditor.AssetBundleOutputPath;
            }
            if (AppSettings.Instance != null && AppSettings.Instance.ResourceMode != ResourceMode.Unspecified)
            {
                SetResourceMode(AppSettings.Instance.ResourceMode);
            }

            RefreshHybridCLREnable();
            RefreshObfuzEnable();
        }

        private void Update()
        {
            if (_queueBuildResources)
            {
                _queueBuildResources = false;
                BuildResources();
            }

            if (!EditorApplication.isCompiling && !EditorApplication.isUpdating && BuildPlayerLaunchService.HasPendingBuild())
            {
                BuildPlayerLaunchService.ExecutePendingBuild(AppBuilderExecutionService.HandlePostprocessBuild);
            }
        }
        private void OnGUI()
        {
            EditorGUI.BeginDisabledGroup(EditorApplication.isCompiling);
            EditorGUILayout.BeginVertical(GUILayout.Width(position.width), GUILayout.Height(position.height));
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            {
                GUILayout.Space(5f);
                EditorGUILayout.LabelField("Environment Information", EditorStyles.boldLabel);
                EditorGUILayout.BeginVertical("box");
                {
                    EditorGUILayout.BeginHorizontal();
                    {
                        EditorGUILayout.LabelField("Product Name", GUILayout.Width(160f));
                        EditorGUILayout.LabelField(_controller.ProductName);
                    }
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginHorizontal();
                    {
                        EditorGUILayout.LabelField("Company Name", GUILayout.Width(160f));
                        EditorGUILayout.LabelField(_controller.CompanyName);
                    }
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginHorizontal();
                    {
                        EditorGUILayout.LabelField("Game Identifier", GUILayout.Width(160f));
                        EditorGUILayout.LabelField(_controller.GameIdentifier);
                    }
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginHorizontal();
                    {
                        EditorGUILayout.LabelField("Game Framework Version", GUILayout.Width(160f));
                        EditorGUILayout.LabelField(_controller.GameFrameworkVersion);
                    }
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginHorizontal();
                    {
                        EditorGUILayout.LabelField("Unity Version", GUILayout.Width(160f));
                        EditorGUILayout.LabelField(_controller.UnityVersion);
                    }
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginHorizontal();
                    {
                        EditorGUILayout.LabelField("Applicable Game Version", GUILayout.Width(160f));
                        EditorGUILayout.LabelField(_controller.ApplicableGameVersion);
                    }
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUILayout.EndVertical();
                GUILayout.Space(5f);
                EditorGUILayout.BeginHorizontal();
                {
                    EditorGUILayout.BeginVertical();
                    {
                        EditorGUILayout.LabelField("Platforms", EditorStyles.boldLabel);
                        EditorGUILayout.BeginHorizontal("box");
                        {
                            EditorGUILayout.BeginVertical();
                            {
                                DrawPlatform(Platform.Windows, "Windows");
                                DrawPlatform(Platform.Windows64, "Windows x64");
                                DrawPlatform(Platform.MacOS, "macOS");
                            }
                            EditorGUILayout.EndVertical();
                            EditorGUILayout.BeginVertical();
                            {
                                DrawPlatform(Platform.Linux, "Linux");
                                DrawPlatform(Platform.IOS, "iOS");
                                DrawPlatform(Platform.Android, "Android");
                            }
                            EditorGUILayout.EndVertical();
                            EditorGUILayout.BeginVertical();
                            {
                                DrawPlatform(Platform.WindowsStore, "Windows Store");
                                DrawPlatform(Platform.WebGL, "WebGL");
                            }
                            EditorGUILayout.EndVertical();
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                    EditorGUILayout.EndVertical();
                }
                EditorGUILayout.EndHorizontal();
                GUILayout.Space(5f);
                EditorGUILayout.LabelField("Compression", EditorStyles.boldLabel);
                EditorGUILayout.BeginVertical("box");
                {
                    EditorGUILayout.BeginHorizontal();
                    {
                        EditorGUILayout.LabelField("AssetBundle Compression", GUILayout.Width(160f));
                        _controller.AssetBundleCompression = (AssetBundleCompressionType)EditorGUILayout.EnumPopup(_controller.AssetBundleCompression);
                    }
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginHorizontal();
                    {
                        EditorGUILayout.LabelField("Compression Helper", GUILayout.Width(160f));
                        string[] names = _controller.GetCompressionHelperTypeNames();
                        int selectedIndex = EditorGUILayout.Popup(_compressionHelperTypeNameIndex, names);
                        if (selectedIndex != _compressionHelperTypeNameIndex)
                        {
                            _compressionHelperTypeNameIndex = selectedIndex;
                            _controller.CompressionHelperTypeName = selectedIndex <= 0 ? string.Empty : names[selectedIndex];
                            if (_controller.RefreshCompressionHelper())
                            {
                                Debug.Log("Set compression helper success.");
                            }
                            else
                            {
                                Debug.LogWarning("Set compression helper failure.");
                            }
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginHorizontal();
                    {
                        EditorGUILayout.LabelField("Additional Compression", GUILayout.Width(160f));
                        _controller.AdditionalCompressionSelected = EditorGUILayout.ToggleLeft("Additional Compression for Output Full Resources with Compression Helper", _controller.AdditionalCompressionSelected);
                    }
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUILayout.EndVertical();
                GUILayout.Space(5f);
                EditorGUILayout.BeginHorizontal();
                {
                    EditorGUILayout.LabelField("Build Resources Settings", EditorStyles.boldLabel);
                    AppBuilderEditorSettings.Instance.EnableResourceRuleEditor = EditorGUILayout.ToggleLeft("Enable [Rule Editor]", AppBuilderEditorSettings.Instance.EnableResourceRuleEditor, GUILayout.Width(160));
                    if (AppBuilderEditorSettings.Instance.EnableResourceRuleEditor && GUILayout.Button("Rule Editor", GUILayout.Width(160f)))
                    {
                        ResourceRuleEditor.Open();
                        GUIUtility.ExitGUI();
                    }
                    if (GUILayout.Button("Resources Editor", GUILayout.Width(160f)))
                    {
                        OpenResourcesEditor();
                        GUIUtility.ExitGUI();
                    }
                }
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.BeginVertical("box");
                {
                    EditorGUILayout.BeginHorizontal();
                    {
                        EditorGUILayout.LabelField("Force Rebuild AssetBundle", GUILayout.Width(160f));
                        _controller.ForceRebuildAssetBundleSelected = EditorGUILayout.Toggle(_controller.ForceRebuildAssetBundleSelected);
                    }
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginHorizontal();
                    {
                        EditorGUILayout.LabelField("Enable Obfuz", GUILayout.Width(160f));
                        EditorGUI.BeginChangeCheck();
                        AppBuilderEditorSettings.Instance.EnableObfuz = EditorGUILayout.Toggle(AppBuilderEditorSettings.Instance.EnableObfuz);
                        if (EditorGUI.EndChangeCheck())
                        {
                            RefreshObfuzEnable();
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                    EditorGUILayout.BeginHorizontal();
                    {
                        EditorGUILayout.LabelField("Resource Mode", GUILayout.Width(160f));
                        EditorGUI.BeginChangeCheck();
                        {
                            AppSettings.Instance.ResourceMode = (ResourceMode)EditorGUILayout.EnumPopup(AppSettings.Instance.ResourceMode);
                        }
                        if (EditorGUI.EndChangeCheck())
                        {
                            RefreshHybridCLREnable();
                        }
                        if (AppSettings.Instance.ResourceMode != ResourceMode.Unspecified)
                        {
                            SetResourceMode(AppSettings.Instance.ResourceMode);
                        }
                        AppBuilderEditorSettings.Instance.RevealFolder = EditorGUILayout.ToggleLeft(_revealFolderContent, AppBuilderEditorSettings.Instance.RevealFolder, GUILayout.Width(105f));
                        EditorGUILayout.EndHorizontal();
                        if (AppSettings.Instance.ResourceMode == ResourceMode.Unspecified)
                        {
                            EditorGUILayout.HelpBox("ResourceMode is invalid.", MessageType.Error);
                        }
                    }
                    EditorGUILayout.BeginHorizontal();
                    {
                        EditorGUILayout.LabelField("Build Event Handler", GUILayout.Width(160f));
                        string[] names = _controller.GetBuildEventHandlerTypeNames();
                        int selectedIndex = EditorGUILayout.Popup(_buildEventHandlerTypeNameIndex, names);
                        if (selectedIndex != _buildEventHandlerTypeNameIndex)
                        {
                            _buildEventHandlerTypeNameIndex = selectedIndex;
                            _controller.BuildEventHandlerTypeName = selectedIndex <= 0 ? string.Empty : names[selectedIndex];
                            if (_controller.RefreshBuildEventHandler())
                            {
                                Debug.Log("Set build event handler success.");
                            }
                            else
                            {
                                Debug.LogWarning("Set build event handler failure.");
                            }
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginHorizontal();
                    {
                        EditorGUILayout.LabelField("Internal Resource Version", GUILayout.Width(160f));
                        _controller.InternalResourceVersion = EditorGUILayout.IntField(_controller.InternalResourceVersion);
                    }
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginHorizontal();
                    {
                        EditorGUILayout.LabelField("Resource Version", GUILayout.Width(160f));
                        GUILayout.Label(Utility.Text.Format("{0} ({1})", _controller.ApplicableGameVersion, _controller.InternalResourceVersion.ToString()));
                    }
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginHorizontal();
                    {
                        EditorGUILayout.LabelField("Output Directory", GUILayout.Width(160f));
                        _controller.OutputDirectory = EditorGUILayout.TextField(_controller.OutputDirectory);
                        if (GUILayout.Button("Browse...", GUILayout.Width(80f)))
                        {
                            BrowseOutputDirectory();
                        }
                    }
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.BeginHorizontal();
                    {
                        EditorGUILayout.LabelField("Output Resources Path", GUILayout.Width(160f));
                        GUILayout.Label(GetResourceOutputPathByMode(AppSettings.Instance.ResourceMode));
                    }
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.BeginHorizontal();
                    {
                        EditorGUILayout.LabelField("Working Path", GUILayout.Width(160f));
                        GUILayout.Label(_controller.WorkingPath);
                    }
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginHorizontal();
                    {
                        EditorGUILayout.LabelField("Build Report Path", GUILayout.Width(160f));
                        GUILayout.Label(_controller.BuildReportPath);
                    }
                    EditorGUILayout.EndHorizontal();
                }

                EditorGUILayout.EndVertical();
                string buildMessage = string.Empty;
                MessageType buildMessageType = MessageType.None;
                GetBuildMessage(out buildMessage, out buildMessageType);
                EditorGUILayout.HelpBox(buildMessage, buildMessageType);
                if (_controller.OutputFullSelected || _controller.OutputPackedSelected)
                {
                    DrawHotfixConfigPanel();
                }
                DrawAppBuildEditorSettingsPanel();
                EditorGUILayout.EndScrollView();

                EditorGUILayout.BeginHorizontal("box");
                {
                    EditorGUI.BeginDisabledGroup(_controller.Platforms == Platform.Undefined || string.IsNullOrEmpty(_controller.CompressionHelperTypeName) || !_controller.IsValidOutputDirectory || AppSettings.Instance.ResourceMode == ResourceMode.Unspecified);
                    {
                        if (GUILayout.Button(_buildResourcesButtonContent, GUILayout.Height(35)))
                        {
                            if (EditorUtility.DisplayDialog("Build Resources", Utility.Text.Format("Resources Version: {0}", _controller.InternalResourceVersion), "Build", "Cancel"))
                            {
                                BuildHotfix();
                            }
                        }
                        DrawBuildAppButton();
                    }
                    EditorGUI.EndDisabledGroup();
                    if (GUILayout.Button(_saveButtonContent, GUILayout.Height(35)))
                    {
                        AppBuilderExecutionService.SaveConfiguration(_controller);
                    }
                }
                EditorGUILayout.EndHorizontal();
                GUILayout.Space(2f);
            }
            EditorGUILayout.EndVertical();
            EditorGUI.EndDisabledGroup();
        }

    }
}

