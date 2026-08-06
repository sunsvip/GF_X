using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityGameFramework.Editor.ResourceTools;

namespace UGF.EditorTools
{
    internal static class VersionListOutputWriter
    {
        public static void WriteVersionFile(Platform platform, string versionListPath, int versionListLength, int versionListHashCode, int versionListCompressedLength, int versionListCompressedHashCode)
        {
            if (string.IsNullOrWhiteSpace(versionListPath))
            {
                throw new ArgumentException("Version list path is invalid.", nameof(versionListPath));
            }

            var dir = Path.GetDirectoryName(versionListPath);
            if (string.IsNullOrWhiteSpace(dir))
            {
                throw new InvalidOperationException($"Unable to resolve version list directory from path: {versionListPath}");
            }

            var parentDirectory = Directory.GetParent(dir);
            if (parentDirectory == null)
            {
                throw new InvalidOperationException($"Unable to resolve resource version directory from path: {versionListPath}");
            }

            var resourceVersionStr = parentDirectory.Name.Split('_').LastOrDefault();
            if (string.IsNullOrWhiteSpace(resourceVersionStr)
                || !int.TryParse(resourceVersionStr, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var resourceVersion))
            {
                throw new InvalidOperationException($"Unable to parse resource version from directory: {parentDirectory.FullName}");
            }

            var outputVersionFile = UtilityBuiltin.AssetsPath.GetCombinePath(dir, ConstBuiltin.VersionFile);

            var outputVersionInfo = new VersionInfo
            {
                ApplicableGameVersion = AppBuilderEditorSettings.Instance.ApplicableGameVersion,
                ForceUpdateApp = AppBuilderEditorSettings.Instance.ForceUpdateApp,
                AppUpdateDesc = AppBuilderEditorSettings.Instance.AppUpdateDesc,
                AppUpdateUrl = AppBuilderEditorSettings.Instance.AppUpdateUrl,
                UpdatePrefixUri = UtilityBuiltin.AssetsPath.GetCombinePath(AppBuilderEditorSettings.Instance.UpdatePrefixUri, platform.ToString()),
                VersionListHashCode = versionListHashCode,
                VersionListLength = versionListLength,
                VersionListCompressedHashCode = versionListCompressedHashCode,
                VersionListCompressedLength = versionListCompressedLength,
                InternalResourceVersion = resourceVersion,
                LastAppVersion = Application.version
            };

            File.WriteAllText(outputVersionFile, UtilityBuiltin.Json.ToJson(outputVersionInfo), new UTF8Encoding(false));
        }
    }
}
