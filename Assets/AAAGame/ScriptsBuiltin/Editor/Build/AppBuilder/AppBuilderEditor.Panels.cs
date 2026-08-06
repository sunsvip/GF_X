using GameFramework;
using GameFramework.Resource;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace UGF.EditorTools
{
    public partial class AppBuilderEditor
    {
        private void DrawBuildAppButton()
        {
            Rect buildRect = GUILayoutUtility.GetRect(_buildAppButtonContent, _dropDownButtonStyle, GUILayout.Height(35));
            Rect buildRectPopupButton = buildRect;
            buildRectPopupButton.x += buildRect.width - 35;
            buildRectPopupButton.width = 35;
            if (AppSettings.Instance.ResourceMode == ResourceMode.Package)
            {
                if (GUI.Button(buildRect, _buildAppButtonContent) && EditorUtility.DisplayDialog("Build App", Utility.Text.Format("App Version: {0}", Application.version), "Build", "Cancel"))
                {
                    BuildApp(false);
                    GUIUtility.ExitGUI();
                }

                return;
            }

            if (EditorGUI.DropdownButton(buildRectPopupButton, GUIContent.none, FocusType.Passive, GUIStyle.none))
            {
                GenericMenu menu = new GenericMenu();
                menu.AddItem(new GUIContent("Full Build(Generate AOT Dlls)", "Build时生成AOT Dlls"), false, () => BuildApp(true));
                menu.DropDown(buildRect);
            }
            else if (GUI.Button(buildRect, _buildAppButtonContent, _dropDownButtonStyle) && EditorUtility.DisplayDialog("Build App", Utility.Text.Format("App Version: {0}", Application.version), "Build", "Cancel"))
            {
                BuildApp(false);
                GUIUtility.ExitGUI();
            }
        }

        private void DrawAppBuildEditorSettingsPanel()
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
                if (GUILayout.Button(_appSettingsButtonContent))
                {
                    Selection.activeObject = AppSettings.Instance;
                }

                if (GUILayout.Button(_playerSettingsButtonContent))
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

                EditorGUILayout.BeginHorizontal();
                {
                    EditorGUILayout.LabelField("App Build Path", GUILayout.Width(160f));
                    AppBuilderEditorSettings.Instance.AppBuildDir = EditorGUILayout.TextField(AppBuilderEditorSettings.Instance.AppBuildDir);
                    if (GUILayout.Button("Select Path", GUILayout.Width(160f)))
                    {
                        string path = EditorDialogUtility.OpenRelativeFolderPanel("Select App Build Path", AppBuilderEditorSettings.Instance.AppBuildDir);
                        if (!string.IsNullOrWhiteSpace(path))
                        {
                            AppBuilderEditorSettings.Instance.AppBuildDir = path;
                        }

                        GUIUtility.ExitGUI();
                    }

                    EditorGUILayout.EndHorizontal();
                }
#if UNITY_ANDROID
                EditorGUILayout.BeginHorizontal();
                {
                    EditorGUILayout.LabelField("Version Code", GUILayout.Width(160f));
                    PlayerSettings.Android.bundleVersionCode = EditorGUILayout.IntField(PlayerSettings.Android.bundleVersionCode);
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                {
                    PlayerSettings.Android.useCustomKeystore = EditorGUILayout.ToggleLeft("Use Custom Keystore", PlayerSettings.Android.useCustomKeystore, GUILayout.Width(160f));
                    EditorGUI.BeginDisabledGroup(!PlayerSettings.Android.useCustomKeystore);
                    {
                        AppBuilderEditorSettings.Instance.AndroidKeystoreName = PlayerSettings.Android.keystoreName = EditorGUILayout.TextField(AppBuilderEditorSettings.Instance.AndroidKeystoreName);
                        if (GUILayout.Button("Select Keystore", GUILayout.Width(160f)))
                        {
                            string path = EditorDialogUtility.OpenRelativeFilePanel("Select Keystore", AppBuilderEditorSettings.Instance.AndroidKeystoreName, "keystore,jks,ks");
                            if (!string.IsNullOrWhiteSpace(path))
                            {
                                AppBuilderEditorSettings.Instance.AndroidKeystoreName = PlayerSettings.Android.keystoreName = path;
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
                        AppBuilderEditorSettings.Instance.KeystorePass = PlayerSettings.Android.keystorePass = EditorGUILayout.PasswordField(AppBuilderEditorSettings.Instance.KeystorePass);
                    }
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.BeginHorizontal();
                    {
                        EditorGUILayout.LabelField("KeyAliasName", GUILayout.Width(160f));
                        AppBuilderEditorSettings.Instance.AndroidKeyAliasName = PlayerSettings.Android.keyaliasName = EditorGUILayout.TextField(AppBuilderEditorSettings.Instance.AndroidKeyAliasName);
                    }
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.BeginHorizontal();
                    {
                        EditorGUILayout.LabelField("Alias Password", GUILayout.Width(160f));
                        AppBuilderEditorSettings.Instance.KeyAliasPass = PlayerSettings.Android.keyaliasPass = EditorGUILayout.PasswordField(AppBuilderEditorSettings.Instance.KeyAliasPass);
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
                if (GUILayout.Button(_hybridClrSettingsButtonContent, GUILayout.Width(160f)))
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
                    EditorGUILayout.LabelField(_hotfixUrlContent, GUILayout.Width(160f));
                    AppBuilderEditorSettings.Instance.UpdatePrefixUri = EditorGUILayout.TextField(AppBuilderEditorSettings.Instance.UpdatePrefixUri);
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                {
                    EditorGUILayout.LabelField(_applicableVersionContent, GUILayout.Width(160f));
                    AppBuilderEditorSettings.Instance.ApplicableGameVersion = EditorGUILayout.TextField(AppBuilderEditorSettings.Instance.ApplicableGameVersion);
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                {
                    EditorGUILayout.LabelField(_appUpdateUrlContent, GUILayout.Width(160f));
                    AppBuilderEditorSettings.Instance.AppUpdateUrl = EditorGUILayout.TextField(AppBuilderEditorSettings.Instance.AppUpdateUrl);
                    AppBuilderEditorSettings.Instance.ForceUpdateApp = EditorGUILayout.ToggleLeft(_forceUpdateAppContent, AppBuilderEditorSettings.Instance.ForceUpdateApp, GUILayout.Width(100f));
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                {
                    AppBuilderEditorSettings.Instance.Netstandard2NetFramework = EditorGUILayout.ToggleLeft(_netstandardToFrameworkContent, AppBuilderEditorSettings.Instance.Netstandard2NetFramework);
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("Config", GUILayout.Width(160f)))
                    {
                        EditorWindow.GetWindow<NS2NFConfigEditor>().Show();
                    }
                }
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField(_appUpdateDescriptionContent, GUILayout.Width(160f));
                AppBuilderEditorSettings.Instance.AppUpdateDesc = EditorGUILayout.TextArea(AppBuilderEditorSettings.Instance.AppUpdateDesc, GUILayout.Height(50));
            }
            EditorGUILayout.EndVertical();
        }
    }
}
