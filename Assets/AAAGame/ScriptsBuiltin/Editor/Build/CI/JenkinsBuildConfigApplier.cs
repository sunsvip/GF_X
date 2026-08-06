using UnityEditor;
using UnityGameFramework.Editor.ResourceTools;

namespace UGF.EditorTools
{
    internal static class JenkinsBuildConfigApplier
    {
        public static void ApplyResourceBuildConfig(ResourceBuilderController controller, JenkinsBuildResourceConfig config, Platform platform)
        {
            controller.OutputDirectory = config.ResourceOutputDir;
            controller.Platforms = platform;
            controller.ForceRebuildAssetBundleSelected = config.ForceRebuild;
            controller.InternalResourceVersion = config.ResourceVersion;

            AppBuilderEditorSettings.Instance.UpdatePrefixUri = config.UpdatePrefixUrl;
            AppBuilderEditorSettings.Instance.ApplicableGameVersion = config.ApplicableVersions;
            AppBuilderEditorSettings.Instance.ForceUpdateApp = config.ForceUpdate;
            AppBuilderEditorSettings.Instance.AppUpdateUrl = config.AppUpdateUrl;
            AppBuilderEditorSettings.Instance.AppUpdateDesc = config.AppUpdateDescription;
        }

        public static void ApplyAppBuildConfig(JenkinsBuildAppConfig config)
        {
#if UNITY_ANDROID
            PlayerSettings.Android.useCustomKeystore = AppBuilderEditorSettings.Instance.AndroidUseKeystore;
            PlayerSettings.Android.keystoreName = AppBuilderEditorSettings.Instance.AndroidKeystoreName;
            PlayerSettings.Android.keystorePass = AppBuilderEditorSettings.Instance.KeystorePass;
            PlayerSettings.Android.keyaliasName = AppBuilderEditorSettings.Instance.AndroidKeyAliasName;
            PlayerSettings.Android.keyaliasPass = AppBuilderEditorSettings.Instance.KeyAliasPass;

            EditorUserBuildSettings.buildAppBundle = config.BuildAppBundle;
            PlayerSettings.Android.bundleVersionCode = config.VersionCode;
#endif
            EditorUserBuildSettings.development = config.DevelopmentBuild;
            AppSettings.Instance.DebugMode = config.DebugMode;
            PlayerSettings.bundleVersion = config.Version;
        }
    }
}
