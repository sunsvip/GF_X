using GameFramework;
using System;
using System.IO;
using UnityEditor;
using UnityGameFramework.Editor.ResourceTools;

namespace UGF.EditorTools
{
    public partial class AppBuilderEditor
    {
        private void BuildHotfix()
        {
            AppBuilderExecutionService.PrepareHotfixBuild(_controller);
            _queueBuildResources = true;
        }

        private bool BuildApp(bool generateAot)
        {
#if UNITY_ANDROID
            if (AppBuilderEditorSettings.Instance.AndroidUseKeystore && !AppBuilderExecutionService.IsKeystoreAvailable(AppBuilderEditorSettings.Instance.AndroidKeystoreName))
            {
                EditorUtility.DisplayDialog("Build Error!", Utility.Text.Format("Keystore文件不存在或格式错误:{0}", AppBuilderEditorSettings.Instance.AndroidKeystoreName), "GOT IT");
                return false;
            }
#endif
            return AppBuilderExecutionService.BuildApp(_controller, generateAot);
        }

        private void BrowseOutputDirectory()
        {
            string directory = EditorDialogUtility.OpenRelativeFolderPanel("Select Output Directory", AppBuilderEditorSettings.Instance.ResourceBuildDir);
            if (!string.IsNullOrEmpty(directory))
            {
                AppBuilderEditorSettings.Instance.ResourceBuildDir = directory;
                _controller.OutputDirectory = Path.GetFullPath(AppBuilderEditorSettings.Instance.ResourceBuildDir, ConstEditor.ProjectRootPath);
            }
        }

        private void GetBuildMessage(out string message, out MessageType messageType)
        {
            message = string.Empty;
            messageType = MessageType.Error;
            if (_controller.Platforms == Platform.Undefined)
            {
                if (!string.IsNullOrEmpty(message))
                {
                    message += Environment.NewLine;
                }

                message += "Platform is invalid.";
            }

            if (string.IsNullOrEmpty(_controller.CompressionHelperTypeName))
            {
                if (!string.IsNullOrEmpty(message))
                {
                    message += Environment.NewLine;
                }

                message += "Compression helper is invalid.";
            }

            if (!_controller.IsValidOutputDirectory)
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
            if (Directory.Exists(_controller.OutputPackagePath))
            {
                message += Utility.Text.Format("{0} will be overwritten.", _controller.OutputPackagePath);
                messageType = MessageType.Warning;
            }

            if (Directory.Exists(_controller.OutputFullPath))
            {
                if (message.Length > 0)
                {
                    message += " ";
                }

                message += Utility.Text.Format("{0} will be overwritten.", _controller.OutputFullPath);
                messageType = MessageType.Warning;
            }

            if (Directory.Exists(_controller.OutputPackedPath))
            {
                if (message.Length > 0)
                {
                    message += " ";
                }

                message += Utility.Text.Format("{0} will be overwritten.", _controller.OutputPackedPath);
                messageType = MessageType.Warning;
            }

            if (messageType != MessageType.Warning)
            {
                message = "Ready to build.";
            }
        }

        private void BuildResources()
        {
            AppBuilderExecutionService.BuildResources(_controller, saveConfiguration: true);
        }
    }
}
