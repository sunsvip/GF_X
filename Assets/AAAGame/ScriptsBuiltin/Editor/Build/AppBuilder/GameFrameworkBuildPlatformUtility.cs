using UnityEditor;
using UnityGameFramework.Editor.ResourceTools;

namespace UGF.EditorTools
{
    internal static class GameFrameworkBuildPlatformUtility
    {
        public static bool TryGetPlatform(BuildTarget buildTarget, out Platform platform)
        {
            switch (buildTarget)
            {
                case BuildTarget.StandaloneOSX:
                    platform = Platform.MacOS;
                    return true;
                case BuildTarget.StandaloneWindows:
                    platform = Platform.Windows;
                    return true;
                case BuildTarget.iOS:
                    platform = Platform.IOS;
                    return true;
                case BuildTarget.Android:
                    platform = Platform.Android;
                    return true;
                case BuildTarget.StandaloneWindows64:
                    platform = Platform.Windows64;
                    return true;
                case BuildTarget.WebGL:
                    platform = Platform.WebGL;
                    return true;
                case BuildTarget.StandaloneLinux64:
                    platform = Platform.Linux;
                    return true;
                default:
                    platform = Platform.Undefined;
                    return false;
            }
        }
    }
}
