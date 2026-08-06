using GameFramework;
using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace UGF.EditorTools
{
    internal static class BuildPlayerLaunchService
    {
        private const string PendingBuildTaskKey = "BUILD_TASK_TAG";

        public static bool HasPendingBuild()
        {
            return EditorPrefs.GetBool(PendingBuildTaskKey, false);
        }

        public static void SchedulePendingBuild()
        {
            EditorPrefs.SetBool(PendingBuildTaskKey, true);
        }

        public static void ExecutePendingBuild(Action<BuildReport> buildCompletionHandler)
        {
            EditorPrefs.SetBool(PendingBuildTaskKey, false);

            var buildCompletionHandlerField = typeof(BuildPlayerWindow).GetField("buildCompletionHandler", BindingFlags.Static | BindingFlags.NonPublic);
            var getBuildPlayerOptions = typeof(BuildPlayerWindow.DefaultBuildMethods).GetMethod("GetBuildPlayerOptionsInternal", BindingFlags.NonPublic | BindingFlags.Static);
            var editorUserBuildSettingsUtilsType = Utility.Assembly.GetType("UnityEditor.EditorUserBuildSettingsUtils");
            var calculateSelectedBuildTargetMethod = editorUserBuildSettingsUtilsType?.GetMethod("CalculateSelectedBuildTarget", BindingFlags.Public | BindingFlags.Static);
            var getSelectedSubtargetMethod = typeof(EditorUserBuildSettings).GetMethod("GetSelectedSubtargetFor", BindingFlags.Static | BindingFlags.NonPublic);
            if (buildCompletionHandlerField == null || getBuildPlayerOptions == null || calculateSelectedBuildTargetMethod == null || getSelectedSubtargetMethod == null)
            {
                Debug.LogWarning("Call build methods failed: required Unity build reflection entry was not found.");
                return;
            }

            buildCompletionHandlerField.SetValue(null, buildCompletionHandler);
            var buildOptions = new BuildPlayerOptions
            {
                options = BuildOptions.ShowBuiltPlayer,
                targetGroup = EditorUserBuildSettings.selectedBuildTargetGroup,
                target = (BuildTarget)calculateSelectedBuildTargetMethod.Invoke(null, null)
            };
            buildOptions.subtarget = (int)getSelectedSubtargetMethod.Invoke(null, new object[] { buildOptions.target });

            var errBuildDir = string.IsNullOrWhiteSpace(AppBuilderEditorSettings.Instance.AppBuildDir);
            var locationPathName = GetBuildLocation(buildOptions.targetGroup, buildOptions.target, buildOptions.subtarget, buildOptions.options);
            if (string.IsNullOrWhiteSpace(locationPathName))
            {
                Debug.LogWarning("Call build methods failed: build output location is invalid.");
                return;
            }

            var locationDir = Path.GetDirectoryName(locationPathName);
            if (string.IsNullOrWhiteSpace(locationDir))
            {
                Debug.LogWarning("Call build methods failed: build output directory is invalid.");
                return;
            }

            if (!Directory.Exists(locationDir))
            {
                Directory.CreateDirectory(locationDir);
            }

            EditorUserBuildSettings.SetBuildLocation(buildOptions.target, locationPathName);
            buildOptions = (BuildPlayerOptions)getBuildPlayerOptions.Invoke(null, new object[] { errBuildDir, buildOptions });
            buildOptions.locationPathName = locationPathName;

            BuildPlayerWindow.DefaultBuildMethods.BuildPlayer(buildOptions);
        }

        private static string GetBuildLocation(BuildTargetGroup targetGroup, BuildTarget target, int subtarget, BuildOptions options)
        {
            var defaultFolder = UtilityBuiltin.AssetsPath.GetCombinePath(ConstEditor.ProjectRootPath, AppBuilderEditorSettings.Instance.AppBuildDir, target.ToString());
            var defaultName = Application.productName;
            string extension = null;
            var postprocessBuildPlayerType = Utility.Assembly.GetType("UnityEditor.PostprocessBuildPlayer");
#if UNITY_6000_0_OR_NEWER
            var getExtensionMethod = postprocessBuildPlayerType?.GetMethod("GetExtensionForBuildTarget", new Type[] { typeof(BuildTarget), typeof(int), typeof(BuildOptions) });
            if (getExtensionMethod != null)
            {
                extension = getExtensionMethod.Invoke(null, new object[] { target, subtarget, options }) as string;
            }
#else
            var getExtensionMethod = postprocessBuildPlayerType?.GetMethod("GetExtensionForBuildTarget", new Type[] { typeof(BuildTargetGroup), typeof(BuildTarget), typeof(int), typeof(BuildOptions) });
            if (getExtensionMethod != null)
            {
                extension = getExtensionMethod.Invoke(null, new object[] { targetGroup, target, subtarget, options }) as string;
            }
#endif
            var buildPath = defaultFolder;
            if (!string.IsNullOrEmpty(extension))
            {
                var appFileName = Utility.Text.Format("{0}.{1}", defaultName, extension);
                buildPath = UtilityBuiltin.AssetsPath.GetCombinePath(defaultFolder, appFileName);
            }

            return buildPath;
        }
    }
}
