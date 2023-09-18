using GameFramework;
using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using GameFramework.Resource;
using Unity.VisualScripting;
using HybridCLR.Editor.Commands;
using System.Text;
using System.Linq;
using static UnityEditor.BuildPlayerWindow;
using System.Collections;
using UnityEditor.Build.Reporting;
using UnityGameFramework.Editor.ResourceTools;
using Cysharp.Threading.Tasks;
using UnityEngine.TestTools;
using UnityEngine.Assertions;
using System.Drawing.Drawing2D;

namespace UGF.EditorTools
{
    /// <summary>
    /// 资源生成器。
    /// </summary>
    public class AppBuildEidtor : EditorWindow
    {
        const string BUILD_TASK_TAG = "BUILD_TASK_TAG";
        private readonly string[] keystoreExtNames = { ".keystore", ".jks", ".ks" };
        private ResourceBuilderController m_Controller = null;
        private bool m_OrderBuildResources = false;
        private int m_CompressionHelperTypeNameIndex = 0;
        private int m_BuildEventHandlerTypeNameIndex = 0;
        private GUIContent hotfixUrlContent;
        private GUIContent applicableVerContent;
        private GUIContent forceUpdateAppContent;
        private GUIContent appUpdateUrlContent;
        private GUIContent appUpdateDescContent;
        private GUIContent revealFolderContent;
        private GUIContent buildResBtContent;
        private GUIContent buildAppBtContent;
        private GUIContent saveBtContent;
        private GUIContent playerSettingBtContent;
        private GUIContent hybridclrSettingBtContent;
        private Vector2 scrollPosition;
        private GUIStyle dropDownBtStyle;

        public static void Open()
        {
            AppBuildEidtor window = GetWindow<AppBuildEidtor>("App Builder", true);
#if UNITY_2019_3_OR_NEWER
            window.minSize = new Vector2(800f, 800f);
#else
            window.minSize = new Vector2(800f, 750f);
#endif
        }

        private void OnEnable()
        {
            if (Utility.Assembly.GetType("UnityEditor.BuildPlayerWindow").GetField("getBuildPlayerOptionsHandler", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null) == null)
            {
                RegisterGetBuildPlayerOptionsHandler(CustomBuildOptions);
            }
            hotfixUrlContent = new GUIContent("Update Prefix Uri", "热更新资源服务器地址");
            applicableVerContent = new GUIContent("Applicable Version", "资源适用的客户端版本号,多版本用'|'分割");
            forceUpdateAppContent = new GUIContent("Force Update", "是否强制更新App");
            appUpdateUrlContent = new GUIContent("App Update Url", "App更新下载地址");
            appUpdateDescContent = new GUIContent("App Update Description:", "App更新公告,用于显示在对话框(支持TextMeshPro富文本)");
            revealFolderContent = new GUIContent("Reveal Folder", "打包完成后打开资源输出目录");
            buildResBtContent = EditorGUIUtility.TrTextContentWithIcon("Build Resources", "打AB包/热更", "CloudConnect@2x");
            buildAppBtContent = EditorGUIUtility.TrTextContentWithIcon("Build App", "打新包,首次打热更包请使用Full Build", "UnityLogo");

            playerSettingBtContent = EditorGUIUtility.TrTextContentWithIcon("Player Settings", "打开Player Settings界面", "Settings");
            hybridclrSettingBtContent = EditorGUIUtility.TrTextContentWithIcon("Hotfix Settings", "打开HybridCLR Settings界面", "Settings");
            saveBtContent = EditorGUIUtility.TrTextContentWithIcon("Save", "保存设置", "SaveAs@2x");

            var dropDownToggleButton = EditorGUIUtility.GetBuiltinSkin(EditorSkin.Inspector).FindStyle("DropDownToggleButton");
            dropDownBtStyle = new GUIStyle(dropDownToggleButton);
            dropDownBtStyle.normal.textColor = Color.white;
            dropDownBtStyle.alignment = TextAnchor.MiddleCenter;
            dropDownBtStyle.hover.textColor = Color.white;
            dropDownBtStyle.active.textColor = Color.white;

            if (AppSettings.Instance == null)
            {
                AssetDatabase.CreateAsset(CreateInstance<AppSettings>(), "Assets/Resources/AppSettings.asset");
            }
            RefreshHybridCLREnable();

            m_Controller = new ResourceBuilderController();
            m_Controller.OnLoadingResource += OnLoadingResource;
            m_Controller.OnLoadingAsset += OnLoadingAsset;
            m_Controller.OnLoadCompleted += OnLoadCompleted;
            m_Controller.OnAnalyzingAsset += OnAnalyzingAsset;
            m_Controller.OnAnalyzeCompleted += OnAnalyzeCompleted;
            m_Controller.ProcessingAssetBundle += OnProcessingAssetBundle;
            m_Controller.ProcessingBinary += OnProcessingBinary;
            m_Controller.ProcessResourceComplete += OnProcessResourceComplete;
            m_Controller.BuildResourceError += OnBuildResourceError;
            m_OrderBuildResources = false;

            if (m_Controller.Load())
            {
                Debug.Log("Load configuration success.");

                m_CompressionHelperTypeNameIndex = 0;
                string[] compressionHelperTypeNames = m_Controller.GetCompressionHelperTypeNames();
                for (int i = 0; i < compressionHelperTypeNames.Length; i++)
                {
                    if (m_Controller.CompressionHelperTypeName == compressionHelperTypeNames[i])
                    {
                        m_CompressionHelperTypeNameIndex = i;
                        break;
                    }
                }

                m_Controller.RefreshCompressionHelper();

                m_BuildEventHandlerTypeNameIndex = 0;
                string[] buildEventHandlerTypeNames = m_Controller.GetBuildEventHandlerTypeNames();
                for (int i = 0; i < buildEventHandlerTypeNames.Length; i++)
                {
                    if (m_Controller.BuildEventHandlerTypeName == buildEventHandlerTypeNames[i])
                    {
                        m_BuildEventHandlerTypeNameIndex = i;
                        break;
                    }
                }

                m_Controller.RefreshBuildEventHandler();
            }
            else
            {
                Debug.LogWarning("Load configuration failure.");
            }

            if (string.IsNullOrWhiteSpace(m_Controller.OutputDirectory) || !Directory.Exists(m_Controller.OutputDirectory))
            {
                m_Controller.OutputDirectory = ConstEditor.AssetBundleOutputPath;
            }
            if (AppSettings.Instance.ResourceMode != ResourceMode.Unspecified)
            {
                SetResourceMode(AppSettings.Instance.ResourceMode);
            }
        }

        private void Update()
        {
            if (m_OrderBuildResources)
            {
                m_OrderBuildResources = false;
                BuildResources();
            }

            if (!EditorApplication.isCompiling && !EditorApplication.isUpdating && EditorPrefs.GetBool(BUILD_TASK_TAG, false))
            {
                EditorPrefs.SetBool(BUILD_TASK_TAG, false);
                CallBuildMethods();
            }
        }
        private void OnGUI()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(position.width), GUILayout.Height(position.height));
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            {
                GUILayout.Space(5f);
                EditorGUILayout.LabelField("Environment Information", EditorStyles.boldLabel);
                EditorGUILayout.BeginVertical("box");
                {
                    EditorGUILayout.BeginHorizontal();
                    {
                        EditorGUILayout.LabelField("Product Name", GUILayout.Width(160f));
                        EditorGUILayout.LabelField(m_Controller.ProductName);
                    }
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginHorizontal();
                    {
                        EditorGUILayout.LabelField("Company Name", GUILayout.Width(160f));
                        EditorGUILayout.LabelField(m_Controller.CompanyName);
                    }
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginHorizontal();
                    {
                        EditorGUILayout.LabelField("Game Identifier", GUILayout.Width(160f));
                        EditorGUILayout.LabelField(m_Controller.GameIdentifier);
                    }
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginHorizontal();
                    {
                        EditorGUILayout.LabelField("Game Framework Version", GUILayout.Width(160f));
                        EditorGUILayout.LabelField(m_Controller.GameFrameworkVersion);
                    }
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginHorizontal();
                    {
                        EditorGUILayout.LabelField("Unity Version", GUILayout.Width(160f));
                        EditorGUILayout.LabelField(m_Controller.UnityVersion);
                    }
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginHorizontal();
                    {
                        EditorGUILayout.LabelField("Applicable Game Version", GUILayout.Width(160f));
                        EditorGUILayout.LabelField(m_Controller.ApplicableGameVersion);
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
                        m_Controller.AssetBundleCompression = (AssetBundleCompressionType)EditorGUILayout.EnumPopup(m_Controller.AssetBundleCompression);
                    }
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginHorizontal();
                    {
                        EditorGUILayout.LabelField("Compression Helper", GUILayout.Width(160f));
                        string[] names = m_Controller.GetCompressionHelperTypeNames();
                        int selectedIndex = EditorGUILayout.Popup(m_CompressionHelperTypeNameIndex, names);
                        if (selectedIndex != m_CompressionHelperTypeNameIndex)
                        {
                            m_CompressionHelperTypeNameIndex = selectedIndex;
                            m_Controller.CompressionHelperTypeName = selectedIndex <= 0 ? string.Empty : names[selectedIndex];
                            if (m_Controller.RefreshCompressionHelper())
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
                        m_Controller.AdditionalCompressionSelected = EditorGUILayout.ToggleLeft("Additional Compression for Output Full Resources with Compression Helper", m_Controller.AdditionalCompressionSelected);
                    }
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUILayout.EndVertical();
                GUILayout.Space(5f);
                EditorGUILayout.BeginHorizontal();
                {
                    EditorGUILayout.LabelField("Build Resources Settings", EditorStyles.boldLabel);
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
                        m_Controller.ForceRebuildAssetBundleSelected = EditorGUILayout.Toggle(m_Controller.ForceRebuildAssetBundleSelected);
                    }
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginHorizontal();
                    {
                        EditorGUILayout.LabelField("Build Event Handler", GUILayout.Width(160f));
                        string[] names = m_Controller.GetBuildEventHandlerTypeNames();
                        int selectedIndex = EditorGUILayout.Popup(m_BuildEventHandlerTypeNameIndex, names);
                        if (selectedIndex != m_BuildEventHandlerTypeNameIndex)
                        {
                            m_BuildEventHandlerTypeNameIndex = selectedIndex;
                            m_Controller.BuildEventHandlerTypeName = selectedIndex <= 0 ? string.Empty : names[selectedIndex];
                            if (m_Controller.RefreshBuildEventHandler())
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
                        m_Controller.InternalResourceVersion = EditorGUILayout.IntField(m_Controller.InternalResourceVersion);
                    }
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginHorizontal();
                    {
                        EditorGUILayout.LabelField("Resource Version", GUILayout.Width(160f));
                        GUILayout.Label(Utility.Text.Format("{0} ({1})", m_Controller.ApplicableGameVersion, m_Controller.InternalResourceVersion.ToString()));
                    }
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginHorizontal();
                    {
                        EditorGUILayout.LabelField("Output Directory", GUILayout.Width(160f));
                        m_Controller.OutputDirectory = EditorGUILayout.TextField(m_Controller.OutputDirectory);
                        if (GUILayout.Button("Browse...", GUILayout.Width(80f)))
                        {
                            BrowseOutputDirectory();
                        }
                    }
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.BeginHorizontal();
                    {
                        EditorGUILayout.LabelField("Output Resources Path", GUILayout.Width(160f));
                        GUILayout.Label(GetResourceOupoutPathByMode(AppSettings.Instance.ResourceMode));
                        EditorGUILayout.LabelField("Resource Mode:", GUILayout.Width(100f));
                        EditorGUI.BeginChangeCheck();
                        {
                            AppSettings.Instance.ResourceMode = (ResourceMode)EditorGUILayout.EnumPopup(AppSettings.Instance.ResourceMode, GUILayout.Width(160f));
                        }
                        if (EditorGUI.EndChangeCheck())
                        {
                            RefreshHybridCLREnable();
                        }
                        if (AppSettings.Instance.ResourceMode != ResourceMode.Unspecified)
                        {
                            SetResourceMode(AppSettings.Instance.ResourceMode);
                        }
                        AppBuildSettings.Instance.RevealFolder = EditorGUILayout.ToggleLeft(revealFolderContent, AppBuildSettings.Instance.RevealFolder, GUILayout.Width(105f));
                    }
                    EditorGUILayout.EndHorizontal();
                    if (AppSettings.Instance.ResourceMode == ResourceMode.Unspecified)
                    {
                        EditorGUILayout.HelpBox("ResourceMode is invalid.", MessageType.Error);
                    }
                    EditorGUILayout.BeginHorizontal();
                    {
                        EditorGUILayout.LabelField("Working Path", GUILayout.Width(160f));
                        GUILayout.Label(m_Controller.WorkingPath);
                    }
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginHorizontal();
                    {
                        EditorGUILayout.LabelField("Build Report Path", GUILayout.Width(160f));
                        GUILayout.Label(m_Controller.BuildReportPath);
                    }
                    EditorGUILayout.EndHorizontal();
                }

                EditorGUILayout.EndVertical();
                string buildMessage = string.Empty;
                MessageType buildMessageType = MessageType.None;
                GetBuildMessage(out buildMessage, out buildMessageType);
                EditorGUILayout.HelpBox(buildMessage, buildMessageType);
                if (m_Controller.OutputFullSelected || m_Controller.OutputPackedSelected)
                {
                    DrawHotfixConfigPanel();
                }
                DrawAppBuildSettingsPanel();
                EditorGUILayout.EndScrollView();

                EditorGUILayout.BeginHorizontal("box");
                {
                    EditorGUI.BeginDisabledGroup(m_Controller.Platforms == Platform.Undefined || string.IsNullOrEmpty(m_Controller.CompressionHelperTypeName) || !m_Controller.IsValidOutputDirectory || AppSettings.Instance.ResourceMode == ResourceMode.Unspecified);
                    {
                        if (GUILayout.Button(buildResBtContent, GUILayout.Height(35)))
                        {
                            if (EditorUtility.DisplayDialog("Build Resources", Utility.Text.Format("Resources Version: {0}", m_Controller.InternalResourceVersion), "Build", "Cancel"))
                            {
                                BuildHotfix();
                            }
                        }
                        DrawBuildAppButton();
                    }
                    EditorGUI.EndDisabledGroup();
                    if (GUILayout.Button(saveBtContent, GUILayout.Height(35)))
                    {
                        SaveConfiguration();
                    }
                }
                EditorGUILayout.EndHorizontal();
                GUILayout.Space(2f);
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawBuildAppButton()
        {
            Rect buildRect = GUILayoutUtility.GetRect(buildAppBtContent, dropDownBtStyle,
                        GUILayout.Height(35));
            Rect buildRectPopupButton = buildRect;
            buildRectPopupButton.x += buildRect.width - 35;
            buildRectPopupButton.width = 35;
            if (AppSettings.Instance.ResourceMode == ResourceMode.Package)
            {
                if (GUI.Button(buildRect, buildAppBtContent))
                {
                    if (EditorUtility.DisplayDialog("Build App", Utility.Text.Format("App Version: {0}", Application.version), "Build", "Cancel"))
                    {
                        BuildApp(false);
                        GUIUtility.ExitGUI();
                    }
                }
                return;
            }

            if (EditorGUI.DropdownButton(buildRectPopupButton, GUIContent.none, FocusType.Passive,
                GUIStyle.none))
            {
                GenericMenu menu = new GenericMenu();
                menu.AddItem(new GUIContent("Full Build(Generate AOT Dlls)", "Build时生成AOT Dlls"), false,
                    () =>
                    {
                        BuildApp(true);
                    });
                menu.DropDown(buildRect);
            }
            else if (GUI.Button(buildRect, buildAppBtContent, dropDownBtStyle))
            {
                if (EditorUtility.DisplayDialog("Build App", Utility.Text.Format("App Version: {0}", Application.version), "Build", "Cancel"))
                {
                    BuildApp(false);
                    GUIUtility.ExitGUI();
                }
            }
        }

        private void RefreshHybridCLREnable()
        {
            if (AppSettings.Instance.ResourceMode != ResourceMode.Unspecified)
            {
                if (AppSettings.Instance.ResourceMode == ResourceMode.Package)
                {
#if !DISABLE_HYBRIDCLR
                    HybridCLRExtensionTool.DisableHybridCLR();
#endif
                }
                else
                {
#if DISABLE_HYBRIDCLR
                    HybridCLRExtensionTool.EnableHybridCLR();
#endif
                }
            }
        }
        private string GetResourceOupoutPathByMode(ResourceMode mode)
        {
            string result = null;
            switch (mode)
            {
                case ResourceMode.Package:
                    result = m_Controller.OutputPackagePath;
                    break;
                case ResourceMode.Updatable:
                    result = m_Controller.OutputFullPath;
                    break;
                case ResourceMode.UpdatableWhilePlaying:
                    result = m_Controller.OutputPackedPath;
                    break;
            }
            return result;
        }
        private void SetResourceMode(ResourceMode mode)
        {
            m_Controller.OutputPackageSelected = false;
            m_Controller.OutputFullSelected = false;
            m_Controller.OutputPackedSelected = false;
            switch (mode)
            {
                case ResourceMode.Package:
                    m_Controller.OutputPackageSelected = true;
                    break;

                case ResourceMode.Updatable:
                case ResourceMode.UpdatableWhilePlaying:
                    m_Controller.OutputFullSelected = true;
                    break;
            }
        }
        private async UniTask<bool> HasPackedResource()
        {
            ResourceEditorController resEditor = new ResourceEditorController();
            bool loaded = false;
            bool result = false;
            resEditor.OnLoadCompleted += () =>
            {
                var bundles = resEditor.GetResources();
                for (int i = 0; i < bundles.Length; i++)
                {
                    var ab = bundles[i];
                    if (ab.Packed) result = true;
                }
                loaded = true;
            };
            if (resEditor.Load())
            {
                await UniTask.WaitUntil(() => true == loaded);
            }
            else
            {
                loaded = true;
            }
            return result;
        }
        private void OpenResourcesEditor()
        {
            var resEditorClass = Utility.Assembly.GetType("UnityGameFramework.Editor.ResourceTools.ResourceEditor");
            resEditorClass?.GetMethod("Open", BindingFlags.Static | BindingFlags.NonPublic)?.Invoke(null, null);
        }
        private void DrawAppBuildSettingsPanel()
        {
            GUILayout.Space(5f);
            EditorGUILayout.BeginHorizontal();
            {
                EditorGUILayout.LabelField("Build App Settings:", EditorStyles.boldLabel, GUILayout.Width(160));
#if UNITY_ANDROID
                EditorUserBuildSettings.buildAppBundle = EditorGUILayout.ToggleLeft("Build App Bundle(GP)", EditorUserBuildSettings.buildAppBundle);
#endif
                EditorUserBuildSettings.development = EditorGUILayout.ToggleLeft("Development Build", EditorUserBuildSettings.development);
                AppSettings.Instance.DebugMode = EditorGUILayout.ToggleLeft("Debug Mode", AppSettings.Instance.DebugMode);
                if (GUILayout.Button(playerSettingBtContent))
                {
                    SettingsService.OpenProjectSettings("Project/Player");
                    GUIUtility.ExitGUI();
                }
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginVertical("box");
            {
                EditorGUILayout.BeginHorizontal();
                {
                    EditorGUILayout.LabelField("Version", GUILayout.Width(160f));
                    PlayerSettings.bundleVersion = EditorGUILayout.TextField(PlayerSettings.bundleVersion);
                }
                EditorGUILayout.EndHorizontal();
#if UNITY_ANDROID
                EditorGUILayout.BeginHorizontal();
                {
                    EditorGUILayout.LabelField("Version Code", GUILayout.Width(160f));
                    PlayerSettings.Android.bundleVersionCode = EditorGUILayout.IntField(PlayerSettings.Android.bundleVersionCode);
                }
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.BeginHorizontal();
                {
                    EditorGUILayout.LabelField("App Build Path", GUILayout.Width(160f));
                    AppBuildSettings.Instance.AppBuildDir = EditorGUILayout.TextField(AppBuildSettings.Instance.AppBuildDir);
                    if (GUILayout.Button("Select Path", GUILayout.Width(160f)))
                    {
                        string path = EditorUtilityExtension.OpenRelativeFolderPanel("Select App Build Path", AppBuildSettings.Instance.AppBuildDir);
                        //string projectRoot = Directory.GetParent(Application.dataPath).FullName;
                        //var appBuildDir = string.IsNullOrWhiteSpace(AppBuildSettings.Instance.AppBuildDir) ? projectRoot : AppBuildSettings.Instance.AppBuildDir;
                        //var openPath = Directory.Exists(appBuildDir) ? appBuildDir : projectRoot;
                        //string path = EditorUtility.OpenFolderPanel("Select App Build Path", openPath, "");
                        if (!string.IsNullOrWhiteSpace(path))
                        {
                            AppBuildSettings.Instance.AppBuildDir = path;// Path.GetRelativePath(projectRoot, path);
                        }
                        GUIUtility.ExitGUI();
                    }
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUILayout.BeginHorizontal();
                {
                    PlayerSettings.Android.useCustomKeystore = EditorGUILayout.ToggleLeft("Use Custom Keystore", PlayerSettings.Android.useCustomKeystore, GUILayout.Width(160f));
                    EditorGUI.BeginDisabledGroup(!PlayerSettings.Android.useCustomKeystore);
                    {
                        AppBuildSettings.Instance.AndroidKeystoreName = PlayerSettings.Android.keystoreName = EditorGUILayout.TextField(AppBuildSettings.Instance.AndroidKeystoreName);
                        if (GUILayout.Button("Select Keystore", GUILayout.Width(160f)))
                        {

                            //string projectRoot = Directory.GetParent(Application.dataPath).FullName;
                            //var keystoreDir = string.IsNullOrWhiteSpace(AppBuildSettings.Instance.AndroidKeystoreName) ? projectRoot : Path.GetDirectoryName(AppBuildSettings.Instance.AndroidKeyAliasName);
                            //var openPath = Directory.Exists(keystoreDir) ? keystoreDir : projectRoot;
                            //string path = EditorUtility.OpenFilePanel("Select Keystore", openPath, "keystore,jks,ks");
                            string path = EditorUtilityExtension.OpenRelativeFilePanel("Select Keystore", AppBuildSettings.Instance.AndroidKeystoreName, "keystore,jks,ks");
                            if (!string.IsNullOrWhiteSpace(path))
                            {
                                AppBuildSettings.Instance.AndroidKeystoreName = PlayerSettings.Android.keystoreName = path;// Path.GetRelativePath(projectRoot, path);
                            }

                            GUIUtility.ExitGUI();
                        }
                    }
                    EditorGUI.EndDisabledGroup();
                }
                EditorGUILayout.EndHorizontal();
                if (PlayerSettings.Android.useCustomKeystore)
                {
                    EditorGUILayout.BeginHorizontal();
                    {
                        EditorGUILayout.LabelField("Keystore Password", GUILayout.Width(160f));
                        AppBuildSettings.Instance.KeystorePass = PlayerSettings.keystorePass = EditorGUILayout.PasswordField(AppBuildSettings.Instance.KeystorePass);
                    }
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginHorizontal();
                    {
                        EditorGUILayout.LabelField("KeyAliasName", GUILayout.Width(160f));
                        AppBuildSettings.Instance.AndroidKeyAliasName = PlayerSettings.Android.keyaliasName = EditorGUILayout.TextField(AppBuildSettings.Instance.AndroidKeyAliasName);
                    }
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginHorizontal();
                    {
                        EditorGUILayout.LabelField("Alias Password", GUILayout.Width(160f));
                        AppBuildSettings.Instance.KeyAliasPass = PlayerSettings.keyaliasPass = EditorGUILayout.PasswordField(AppBuildSettings.Instance.KeyAliasPass);
                    }
                    EditorGUILayout.EndHorizontal();
                }

#elif UNITY_IOS
                EditorGUILayout.BeginHorizontal();
                {
                    EditorGUILayout.LabelField("Build Number", GUILayout.Width(160f));
                    PlayerSettings.iOS.buildNumber = EditorGUILayout.TextField(PlayerSettings.iOS.buildNumber);
                }
                EditorGUILayout.EndHorizontal();
#endif
            }
            EditorGUILayout.EndVertical();
        }
        private void DrawHotfixConfigPanel()
        {
            GUILayout.Space(5f);
            EditorGUILayout.BeginHorizontal();
            {
                EditorGUILayout.LabelField("Hotfix Settings:", EditorStyles.boldLabel);
                if (GUILayout.Button(hybridclrSettingBtContent, GUILayout.Width(160f)))
                {
                    SettingsService.OpenProjectSettings("Project/HybridCLR Settings");
                    GUIUtility.ExitGUI();
                }
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginVertical("box");
            {
                EditorGUILayout.BeginHorizontal();
                {
                    EditorGUILayout.LabelField(hotfixUrlContent, GUILayout.Width(160f));
                    AppBuildSettings.Instance.UpdatePrefixUri = EditorGUILayout.TextField(AppBuildSettings.Instance.UpdatePrefixUri);
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                {
                    EditorGUILayout.LabelField(applicableVerContent, GUILayout.Width(160f));
                    AppBuildSettings.Instance.ApplicableGameVersion = EditorGUILayout.TextField(AppBuildSettings.Instance.ApplicableGameVersion);
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                {
                    EditorGUILayout.LabelField(appUpdateUrlContent, GUILayout.Width(160f));
                    AppBuildSettings.Instance.AppUpdateUrl = EditorGUILayout.TextField(AppBuildSettings.Instance.AppUpdateUrl);
                    AppBuildSettings.Instance.ForceUpdateApp = EditorGUILayout.ToggleLeft(forceUpdateAppContent, AppBuildSettings.Instance.ForceUpdateApp, GUILayout.Width(100f));
                }
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField(appUpdateDescContent, GUILayout.Width(160f));
                AppBuildSettings.Instance.AppUpdateDesc = EditorGUILayout.TextArea(AppBuildSettings.Instance.AppUpdateDesc, GUILayout.Height(50));
            }
            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// 打包资源
        /// </summary>
        private async void BuildHotfix()
        {
            m_Controller.OutputPackedSelected = (AppSettings.Instance.ResourceMode != ResourceMode.Package) && await HasPackedResource();
            await AssetBuildHandler.AutoResolveAbDuplicateAssets();
#if !DISABLE_HYBRIDCLR
            HybridCLRExtensionTool.CompileTargetDll();
#endif
            m_OrderBuildResources = true;
        }
        /// <summary>
        /// 打包App
        /// </summary>
        /// <param name="generateAot"></param>
        private async void BuildApp(bool generateAot)
        {
#if UNITY_ANDROID
            if (AppBuildSettings.Instance.AndroidUseKeystore && !CheckKeystoreAvailable(AppBuildSettings.Instance.AndroidKeystoreName))
            {
                EditorUtility.DisplayDialog("Build Error!", Utility.Text.Format("Keystore文件不存在或格式错误:{0}", AppBuildSettings.Instance.AndroidKeystoreName), "GOT IT");
                return;
            }
#endif
            m_Controller.OutputPackedSelected = (AppSettings.Instance.ResourceMode != ResourceMode.Package) && await HasPackedResource();
            if (m_Controller.OutputPackageSelected)
            {
                await AssetBuildHandler.AutoResolveAbDuplicateAssets();
                if (m_Controller.BuildResources())
                {
                    DeleteAotDlls();//单机模式删除Resource下的AOT dlls
                    //AssetDatabase.Refresh();
                    PrepareBuildApp(generateAot);
                }
            }
            else
            {
                AssetBuildHandler.RemoveStreamingAssetsBundles();
                bool buildAppReady = !m_Controller.OutputPackedSelected;
                if (m_Controller.OutputPackedSelected)
                {
#if !DISABLE_HYBRIDCLR
                    HybridCLRExtensionTool.CompileTargetDll(false);
#endif
                    await AssetBuildHandler.AutoResolveAbDuplicateAssets();
                    buildAppReady = m_Controller.BuildResources();
                }
                if (buildAppReady)
                {
                    PrepareBuildApp(generateAot);
                }
            }
        }


        private bool CheckKeystoreAvailable(string keystore)
        {
            if (string.IsNullOrWhiteSpace(keystore)) return false;
            var ext = Path.GetExtension(keystore).ToLower();
            if (File.Exists(keystore) && keystoreExtNames.Contains(ext))
            {
                return true;
            }
            return false;
        }
        private void DeleteAotDlls()
        {
            string aotSaveDir = UtilityBuiltin.ResPath.GetCombinePath(Application.dataPath, "Resources", ConstBuiltin.AOT_DLL_DIR);
            if (Directory.Exists(aotSaveDir))
            {
                Directory.Delete(aotSaveDir, true);
            }
        }

        private void PrepareBuildApp(bool generateAotDll = false)
        {
#if !DISABLE_HYBRIDCLR
            GenerateHotfixCodeStripConfig(false);
            HybridCLRGenerateAll(generateAotDll);
#else
            GenerateHotfixCodeStripConfig(true);
#endif
            AssetDatabase.Refresh();
            EditorPrefs.SetBool(BUILD_TASK_TAG, true);
        }
        private void CallBuildMethods()
        {
            var buildWin = Utility.Assembly.GetType("UnityEditor.BuildPlayerWindow");
            if (buildWin != null)
            {
                buildWin.GetField("buildCompletionHandler", BindingFlags.Static | BindingFlags.NonPublic).SetValue(buildWin, new Action<BuildReport>(OnPostprocessBuild));
                var buildFunc = buildWin.GetMethod("CallBuildMethods", System.Reflection.BindingFlags.Static | BindingFlags.NonPublic);
                buildFunc.Invoke(null, new object[] { false, BuildOptions.ShowBuiltPlayer });
            }
        }

        private void OnPostprocessBuild(BuildReport report)
        {
            if (report.summary.result != BuildResult.Succeeded)
            {
                Debug.LogError("Build App Failed:" + report.summary.result.ToString());
                return;
            }

            if (File.Exists(report.summary.outputPath))
            {
                var desFile = GetRenamedApp(report.summary.outputPath);
                if (File.Exists(desFile))
                {
                    File.Delete(desFile);
                }
                File.Move(report.summary.outputPath, desFile);
            }
        }

        /// <summary>
        /// 生成或删除热更dlls防裁剪的link.xml
        /// 单机模式时需把热更dlls打到包里
        /// </summary>
        /// <param name="v">true生成; false删除</param>
        private void GenerateHotfixCodeStripConfig(bool v)
        {
            var linkDir = Path.GetDirectoryName(ConstEditor.HotfixAssembly);
            var linkFile = UtilityBuiltin.ResPath.GetCombinePath(linkDir, "link.xml");
            if (v)
            {
                var strBuilder = new StringBuilder();
                strBuilder.AppendLine("<linker>");
                foreach (var dllName in HybridCLR.Editor.SettingsUtil.HotUpdateAssemblyNamesIncludePreserved)
                {
                    strBuilder.AppendLineFormat("\t<assembly fullname=\"{0}\" preserve=\"all\" />", dllName);
                }
                strBuilder.AppendLine("</linker>");
                File.WriteAllText(linkFile, strBuilder.ToString());
            }
            else
            {
                if (File.Exists(linkFile)) File.Delete(linkFile);//热更包不需要添加防裁剪
            }
        }
        /// <summary>
        /// 生成HybridCLR热更相关
        /// </summary>
        /// <param name="generateAotDll"></param>
        private void HybridCLRGenerateAll(bool generateAotDll)
        {
            BuildTarget target = EditorUserBuildSettings.activeBuildTarget;
            HybridCLRExtensionTool.CompileTargetDll(false);
            Il2CppDefGeneratorCommand.GenerateIl2CppDef();

            // 这几个生成依赖HotUpdateDlls
            LinkGeneratorCommand.GenerateLinkXml(target);
            if (generateAotDll)
            {
                // 生成裁剪后的aot dll
                StripAOTDllCommand.GenerateStripedAOTDlls(target);
                HybridCLRExtensionTool.CopyAotDllsToProject(target);
            }

            // 桥接函数生成依赖于AOT dll，必须保证已经build过，生成AOT dll
            MethodBridgeGeneratorCommand.GenerateMethodBridge(target);
            ReversePInvokeWrapperGeneratorCommand.GenerateReversePInvokeWrapper(target);
            AOTReferenceGeneratorCommand.GenerateAOTGenericReference(target);
        }
        private void BrowseOutputDirectory()
        {
            var projRoot = Directory.GetParent(Application.dataPath).FullName;
            string directory = EditorUtilityExtension.OpenRelativeFolderPanel("Select Output Directory", AppBuildSettings.Instance.ResourceBuildDir);// EditorUtility.OpenFolderPanel("Select Output Directory", m_Controller.OutputDirectory, string.Empty);
            if (!string.IsNullOrEmpty(directory))
            {
                AppBuildSettings.Instance.ResourceBuildDir = directory;
                m_Controller.OutputDirectory = Path.GetFullPath(AppBuildSettings.Instance.ResourceBuildDir, projRoot);
            }
        }

        private void GetBuildMessage(out string message, out MessageType messageType)
        {
            message = string.Empty;
            messageType = MessageType.Error;
            if (m_Controller.Platforms == Platform.Undefined)
            {
                if (!string.IsNullOrEmpty(message))
                {
                    message += Environment.NewLine;
                }

                message += "Platform is invalid.";
            }

            if (string.IsNullOrEmpty(m_Controller.CompressionHelperTypeName))
            {
                if (!string.IsNullOrEmpty(message))
                {
                    message += Environment.NewLine;
                }

                message += "Compression helper is invalid.";
            }

            if (!m_Controller.IsValidOutputDirectory)
            {
                if (!string.IsNullOrEmpty(message))
                {
                    message += Environment.NewLine;
                }

                message += "Output directory is invalid.";
            }

            if (!string.IsNullOrEmpty(message))
            {
                return;
            }

            messageType = MessageType.Info;
            if (Directory.Exists(m_Controller.OutputPackagePath))
            {
                message += Utility.Text.Format("{0} will be overwritten.", m_Controller.OutputPackagePath);
                messageType = MessageType.Warning;
            }

            if (Directory.Exists(m_Controller.OutputFullPath))
            {
                if (message.Length > 0)
                {
                    message += " ";
                }

                message += Utility.Text.Format("{0} will be overwritten.", m_Controller.OutputFullPath);
                messageType = MessageType.Warning;
            }

            if (Directory.Exists(m_Controller.OutputPackedPath))
            {
                if (message.Length > 0)
                {
                    message += " ";
                }

                message += Utility.Text.Format("{0} will be overwritten.", m_Controller.OutputPackedPath);
                messageType = MessageType.Warning;
            }

            if (messageType == MessageType.Warning)
            {
                return;
            }

            message = "Ready to build.";
        }

        private void BuildResources()
        {
            if (m_Controller.BuildResources())
            {
                Debug.Log("Build resources success.");
                SaveConfiguration();
            }
            else
            {
                Debug.LogWarning("Build resources failure.");
            }
        }

        private void SaveConfiguration()
        {
            EditorUtility.SetDirty(AppSettings.Instance);
            AppBuildSettings.Save();
            if (m_Controller.Save())
            {
                Debug.Log("Save configuration success.");
            }
            else
            {
                Debug.LogWarning("Save configuration failure.");
            }
        }

        private void DrawPlatform(Platform platform, string platformName)
        {
            m_Controller.SelectPlatform(platform, EditorGUILayout.ToggleLeft(platformName, m_Controller.IsPlatformSelected(platform)));
        }

        private void OnLoadingResource(int index, int count)
        {
            EditorUtility.DisplayProgressBar("Loading Resources", Utility.Text.Format("Loading resources, {0}/{1} loaded.", index.ToString(), count.ToString()), (float)index / count);
        }

        private void OnLoadingAsset(int index, int count)
        {
            EditorUtility.DisplayProgressBar("Loading Assets", Utility.Text.Format("Loading assets, {0}/{1} loaded.", index.ToString(), count.ToString()), (float)index / count);
        }

        private void OnLoadCompleted()
        {
            var projRoot = Directory.GetParent(Application.dataPath).FullName;
            m_Controller.OutputDirectory = Path.GetFullPath(AppBuildSettings.Instance.ResourceBuildDir, projRoot);
            EditorUtility.ClearProgressBar();
        }

        private void OnAnalyzingAsset(int index, int count)
        {
            EditorUtility.DisplayProgressBar("Analyzing Assets", Utility.Text.Format("Analyzing assets, {0}/{1} analyzed.", index.ToString(), count.ToString()), (float)index / count);
        }

        private void OnAnalyzeCompleted()
        {
            EditorUtility.ClearProgressBar();
        }

        private bool OnProcessingAssetBundle(string assetBundleName, float progress)
        {
            if (EditorUtility.DisplayCancelableProgressBar("Processing AssetBundle", Utility.Text.Format("Processing '{0}'...", assetBundleName), progress))
            {
                EditorUtility.ClearProgressBar();
                return true;
            }
            else
            {
                Repaint();
                return false;
            }
        }

        private bool OnProcessingBinary(string binaryName, float progress)
        {
            if (EditorUtility.DisplayCancelableProgressBar("Processing Binary", Utility.Text.Format("Processing '{0}'...", binaryName), progress))
            {
                EditorUtility.ClearProgressBar();
                return true;
            }
            else
            {
                Repaint();
                return false;
            }
        }

        private void OnProcessResourceComplete(Platform platform)
        {
            EditorUtility.ClearProgressBar();
            Debug.Log(Utility.Text.Format("Build resources for '{0}' complete.", platform.ToString()));

            if (AppBuildSettings.Instance.RevealFolder && AppSettings.Instance.ResourceMode != ResourceMode.Package)
            {
                EditorUtility.RevealInFinder(UtilityBuiltin.ResPath.GetCombinePath(GetResourceOupoutPathByMode(AppSettings.Instance.ResourceMode), platform.ToString()));
            }
        }

        private void OnBuildResourceError(string errorMessage)
        {
            EditorUtility.ClearProgressBar();
            Debug.LogWarning(Utility.Text.Format("Build resources error with error message '{0}'.", errorMessage));
        }
        private BuildTarget GetSelectedBuildTarget()
        {
            var buildTarget = (BuildTarget)Utility.Assembly.GetType("UnityEditor.EditorUserBuildSettingsUtils").GetMethod("CalculateSelectedBuildTarget", BindingFlags.Static | BindingFlags.Public).Invoke(null, null);
            return buildTarget;
        }
        private BuildPlayerOptions CustomBuildOptions(BuildPlayerOptions options)
        {
            var buildTarget = GetSelectedBuildTarget();
            BuildTargetGroup buildTargetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
            int subtarget = (int)Utility.Assembly.GetType("UnityEditor.EditorUserBuildSettings").GetMethod("GetSelectedSubtargetFor", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { buildTarget });

            string buildLocation = GetBuildLocation(buildTargetGroup, buildTarget, subtarget, options.options);

            bool isDir = !Path.HasExtension(buildLocation);
            if (string.IsNullOrWhiteSpace(buildLocation) || (isDir && !Directory.Exists(buildLocation)))
                throw new BuildMethodException("Build location for buildTarget " + buildTarget + " is not valid:" + buildLocation);

            //Check if Lz4 is supported for the current buildtargetgroup and enable it if need be
            if ((bool)Utility.Assembly.GetType("UnityEditor.PostprocessBuildPlayer").GetMethod("SupportsLz4Compression", BindingFlags.Static | BindingFlags.Public).Invoke(null, new object[] { buildTargetGroup, buildTarget }))
            {
                //internal enum Compression
                //{
                //    None = 0,
                //    Lz4 = 2,
                //    Lz4HC = 3,
                //}
                var compression = (int)Utility.Assembly.GetType("UnityEditor.EditorUserBuildSettings").GetMethod("GetCompressionType", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { buildTargetGroup });
                if (compression < 0)
                    compression = (int)Utility.Assembly.GetType("UnityEditor.PostprocessBuildPlayer").GetMethod("GetDefaultCompression", BindingFlags.Static | BindingFlags.Public).Invoke(null, new object[] { buildTargetGroup, buildTarget });
                if (compression == 2)//Lz4
                    options.options |= BuildOptions.CompressWithLz4;
                else if (compression == 3)//Lz4HC
                    options.options |= BuildOptions.CompressWithLz4HC;
            }

            bool developmentBuild = EditorUserBuildSettings.development;
            if (developmentBuild)
                options.options |= BuildOptions.Development;
            if (EditorUserBuildSettings.allowDebugging && developmentBuild)
                options.options |= BuildOptions.AllowDebugging;
            if (EditorUserBuildSettings.symlinkSources)
                options.options |= BuildOptions.SymlinkSources;
            if (EditorUserBuildSettings.connectProfiler && (developmentBuild || buildTarget == BuildTarget.WSAPlayer))
                options.options |= BuildOptions.ConnectWithProfiler;
            if (EditorUserBuildSettings.buildWithDeepProfilingSupport && developmentBuild)
                options.options |= BuildOptions.EnableDeepProfilingSupport;
            if (EditorUserBuildSettings.buildScriptsOnly)
                options.options |= BuildOptions.BuildScriptsOnly;

            string connectID = Utility.Assembly.GetType("UnityEditor.Profiling.ProfilerUserSettings").GetProperty("customConnectionID", BindingFlags.Static | BindingFlags.Public).GetValue(null, null) as string;
            if (!string.IsNullOrEmpty(connectID) && developmentBuild)
                options.options |= BuildOptions.CustomConnectionID;

            var checkFunc = typeof(UnityEditor.BuildPlayerWindow.DefaultBuildMethods).GetMethod("IsInstallInBuildFolderOption", BindingFlags.Static | BindingFlags.NonPublic);

            if ((bool)checkFunc.Invoke(null, null))
            {
                options.options |= BuildOptions.InstallInBuildFolder;
            }

            options.target = buildTarget;
            options.subtarget = subtarget;
            options.targetGroup = buildTargetGroup;
            options.locationPathName = buildLocation;
            options.assetBundleManifestPath = Utility.Assembly.GetType("UnityEditor.PostprocessBuildPlayer").GetMethod("GetStreamingAssetsBundleManifestPath", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, null) as string;

            // Build a list of scenes that are enabled
            ArrayList scenesList = new ArrayList();
            EditorBuildSettingsScene[] editorScenes = EditorBuildSettings.scenes;
            foreach (EditorBuildSettingsScene scene in editorScenes)
            {
                if (scene.enabled && !string.IsNullOrEmpty(scene.path))
                {
                    scenesList.Add(scene.path);
                    break;// GF框架只需要把启动场景打进包里,其它场景动态加载
                }
            }

            options.scenes = scenesList.ToArray(typeof(string)) as string[];
            return options;
        }
        private string GetRenamedApp(string appFile)
        {
            var dir = Path.GetDirectoryName(appFile);
            var name = Path.GetFileNameWithoutExtension(appFile);
            var ext = Path.GetExtension(appFile);

            var finalName = Utility.Text.Format("{0}_{1}{2}_v{3}{4}", name, AppSettings.Instance.DebugMode ? "debug" : "release", EditorUserBuildSettings.development ? "Dev" : string.Empty, Application.version, ext);
            return Path.Combine(dir, finalName);
        }
        private static string GetBuildLocation(BuildTargetGroup targetGroup, BuildTarget target, int subtarget, BuildOptions options)
        {
            string defaultFolder = UtilityBuiltin.ResPath.GetCombinePath(Directory.GetParent(Application.dataPath).FullName, AppBuildSettings.Instance.AppBuildDir, target.ToString());
            string defaultName = Application.productName;
            //打出包后给包重命名

            string extension = Utility.Assembly.GetType("UnityEditor.PostprocessBuildPlayer").GetMethod("GetExtensionForBuildTarget", new Type[] { typeof(BuildTargetGroup), typeof(BuildTarget), typeof(int), typeof(BuildOptions) }).Invoke(null, new object[] { targetGroup, target, subtarget, options }) as string;
            string buildPath = defaultFolder;
            if (!string.IsNullOrEmpty(extension))
            {
                string appFileName = Utility.Text.Format("{0}.{1}", defaultName, extension);
                buildPath = UtilityBuiltin.ResPath.GetCombinePath(defaultFolder, appFileName);
            }
            return buildPath;
        }
        #region Jenkins打包
        /// <summary>
        /// Jenkins打包资源调用
        /// </summary>
        public void JenkinsBuildResource(JenkinsBuildResourceConfig configJson)
        {
            Debug.Log($"###########打包资源前初始化配置##########");
            Debug.Log($"{UtilityBuiltin.Json.ToJson(configJson)}");
            Debug.Log($"#########################################");
            var tPlatform = GetGFPlatform(configJson.Platform);
            if (tPlatform == Platform.Undefined)
            {
                Debug.LogError($"#############打包失败, 不支持的平台:{configJson.Platform}############");
                return;
            }
            m_Controller.OutputDirectory = configJson.ResourceOutputDir;
            m_Controller.Platforms = tPlatform;
            m_Controller.ForceRebuildAssetBundleSelected = configJson.ForceRebuild;
            m_Controller.InternalResourceVersion = configJson.ResourceVersion;
            AppBuildSettings.Instance.UpdatePrefixUri = configJson.UpdatePrefixUrl;
            AppBuildSettings.Instance.ApplicableGameVersion = configJson.ApplicableVersions;
            AppBuildSettings.Instance.AppUpdateUrl = configJson.AppUpdateUrl;
            AppBuildSettings.Instance.AppUpdateDesc = configJson.AppUpdateDescription;
            this.BuildHotfix();
            this.BuildResources();
        }
        /// <summary>
        /// Jenkins打包App调用
        /// </summary>
        /// <param name="createAot"></param>
        public void JenkinsBuildApp(JenkinsBuildAppConfig configJson)
        {
            Debug.Log($"###########打包App前初始化配置##########");
            Debug.Log($"{UtilityBuiltin.Json.ToJson(configJson)}");
            Debug.Log($"#########################################");
            var tPlatform = GetGFPlatform(configJson.Platform);
            if (tPlatform == Platform.Undefined)
            {
                Debug.LogError($"#############打包失败, 不支持的平台:{configJson.Platform}############");
                return;
            }


#if UNITY_ANDROID
            PlayerSettings.Android.useCustomKeystore = AppBuildSettings.Instance.AndroidUseKeystore;
            PlayerSettings.Android.keystoreName = AppBuildSettings.Instance.AndroidKeystoreName;
            PlayerSettings.keystorePass = AppBuildSettings.Instance.KeystorePass;
            PlayerSettings.Android.keyaliasName = AppBuildSettings.Instance.AndroidKeyAliasName;
            PlayerSettings.keystorePass = AppBuildSettings.Instance.KeystorePass;

            EditorUserBuildSettings.buildAppBundle = configJson.BuildAppBundle;
            PlayerSettings.Android.bundleVersionCode = configJson.VersionCode;
#endif
            EditorUserBuildSettings.development = configJson.DevelopmentBuild;
            AppSettings.Instance.DebugMode = configJson.DebugMode;
            PlayerSettings.bundleVersion = configJson.Version;

            Debug.Log($"#############AppBuildSettings############");
            Debug.Log($"{UtilityBuiltin.Json.ToJson(AppBuildSettings.Instance)}");
            Debug.Log($"#########################################");
            this.BuildApp(configJson.FullBuild);
            EditorPrefs.SetBool(BUILD_TASK_TAG, false);
            this.CallBuildMethods();
        }
        private Platform GetGFPlatform(BuildTarget buildTarget)
        {
            switch (buildTarget)
            {
                case BuildTarget.StandaloneOSX:
                    return Platform.MacOS;
                case BuildTarget.StandaloneWindows:
                    return Platform.Windows;
                case BuildTarget.iOS:
                    return Platform.IOS;
                case BuildTarget.Android:
                    return Platform.Android;
                case BuildTarget.StandaloneWindows64:
                    return Platform.Windows64;
                case BuildTarget.WebGL:
                    return Platform.WebGL;
                case BuildTarget.StandaloneLinux64:
                    return Platform.Linux;
                default:
                    return Platform.Undefined;
            }
        }
        #endregion
    }
}
