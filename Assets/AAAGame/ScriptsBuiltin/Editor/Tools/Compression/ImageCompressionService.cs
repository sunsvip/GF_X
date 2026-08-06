using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace UGF.EditorTools
{
    internal static class ImageCompressionService
    {
        private const int PngQuantExitTimeoutMs = 600000;

#if UNITY_EDITOR_WIN
        private const string PngQuantToolRelativePath = "Tools/CompressImageTools/pngquant_win/pngquant.exe";
#elif UNITY_EDITOR_OSX
        private const string PngQuantToolRelativePath = "Tools/CompressImageTools/pngquant_mac/pngquant";
#else
        private const string PngQuantToolRelativePath = null;
#endif

        internal static bool CompressOffline(string imageFileName, string outputFileName)
        {
            var fileExtension = Path.GetExtension(imageFileName);
            return string.Equals(fileExtension, ".png", StringComparison.OrdinalIgnoreCase)
                && CompressPngOffline(imageFileName, outputFileName);
        }

        private static bool CompressPngOffline(string imageFileName, string outputFileName)
        {
            if (string.IsNullOrEmpty(PngQuantToolRelativePath))
            {
                Debug.LogWarning("当前平台未配置 pngquant 离线压缩工具。");
                return false;
            }

            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            string pngQuantExecutable = Path.Combine(projectRoot, PngQuantToolRelativePath);
            if (!File.Exists(pngQuantExecutable))
            {
                Debug.LogWarning($"pngquant 离线压缩工具不存在: {pngQuantExecutable}");
                return false;
            }

            var arguments = new StringBuilder()
                .AppendFormat(" --force --quality {0}-{1}", (int)EditorToolSettings.Instance.CompressImgToolQualityMinLv, (int)EditorToolSettings.Instance.CompressImgToolQualityLv)
                .AppendFormat(" --speed {0}", EditorToolSettings.Instance.CompressImgToolFastLv)
                .AppendFormat(" --output \"{0}\"", outputFileName)
                .AppendFormat(" -- \"{0}\"", imageFileName)
                .ToString();

            var processStartInfo = new System.Diagnostics.ProcessStartInfo(pngQuantExecutable, arguments)
            {
                CreateNoWindow = true,
                UseShellExecute = false
            };

            try
            {
                using var process = System.Diagnostics.Process.Start(processStartInfo);
                if (process == null)
                {
                    Debug.LogWarning($"离线压缩图片失败，无法启动 pngquant: {imageFileName}");
                    return false;
                }

                if (!process.WaitForExit(PngQuantExitTimeoutMs))
                {
                    TryKillProcess(process);
                    Debug.LogWarning($"离线压缩图片超时: {imageFileName}");
                    return false;
                }

                bool success = process.ExitCode == 0;
                if (!success)
                {
                    Debug.LogWarningFormat("离线压缩图片:{0}失败,ExitCode:{1}", imageFileName, process.ExitCode);
                }

                return success;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"离线压缩图片失败: {imageFileName}, Error:{exception.Message}");
                return false;
            }
        }

        private static void TryKillProcess(System.Diagnostics.Process process)
        {
            try
            {
                if (process != null && !process.HasExited)
                {
                    process.Kill();
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"终止 pngquant 进程失败: {exception.Message}");
            }
        }
    }
}
