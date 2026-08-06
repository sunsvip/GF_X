using GameFramework;
using GameFramework.Resource;
using UnityEditor;
using UnityEngine;
using UnityGameFramework.Editor.ResourceTools;

namespace UGF.EditorTools
{
    public partial class AppBuilderEditor
    {
        private void DrawPlatform(Platform platform, string platformName)
        {
            _controller.SelectPlatform(platform, EditorGUILayout.ToggleLeft(platformName, _controller.IsPlatformSelected(platform)));
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
            _controller.OutputDirectory = System.IO.Path.GetFullPath(AppBuilderEditorSettings.Instance.ResourceBuildDir, ConstEditor.ProjectRootPath);
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

            Repaint();
            return false;
        }

        private bool OnProcessingBinary(string binaryName, float progress)
        {
            if (EditorUtility.DisplayCancelableProgressBar("Processing Binary", Utility.Text.Format("Processing '{0}'...", binaryName), progress))
            {
                EditorUtility.ClearProgressBar();
                return true;
            }

            Repaint();
            return false;
        }

        private void OnProcessResourceComplete(Platform platform)
        {
            EditorUtility.ClearProgressBar();
            Debug.Log(Utility.Text.Format("Build resources for '{0}' complete.", platform.ToString()));
            if (AppBuilderEditorSettings.Instance.RevealFolder && AppSettings.Instance.ResourceMode != ResourceMode.Package)
            {
                EditorUtility.RevealInFinder(UtilityBuiltin.AssetsPath.GetCombinePath(GetResourceOutputPathByMode(AppSettings.Instance.ResourceMode), platform.ToString()));
            }
        }

        private void OnBuildResourceError(string errorMessage)
        {
            EditorUtility.ClearProgressBar();
            Debug.LogWarning(Utility.Text.Format("Build resources error with error message '{0}'.", errorMessage));
        }

        public void JenkinsBuildResource(JenkinsBuildResourceConfig configJson)
        {
            AppBuilderExecutionService.ExecuteJenkinsResourceBuild(configJson);
        }

        public void JenkinsBuildApp(JenkinsBuildAppConfig configJson)
        {
            AppBuilderExecutionService.ExecuteJenkinsAppBuild(configJson);
        }
    }
}
