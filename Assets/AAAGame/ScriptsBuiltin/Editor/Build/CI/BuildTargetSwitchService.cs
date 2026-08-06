using UnityEditor;
using UnityEngine;

namespace UGF.EditorTools
{
    internal static class BuildTargetSwitchService
    {
        public static bool CheckAndSwitchPlatform(BuildTarget platform)
        {
            if (EditorUserBuildSettings.activeBuildTarget == platform)
            {
                return true;
            }

            var buildTargetGroup = BuildPipeline.GetBuildTargetGroup(platform);
            Debug.Log($"#########Switch Active BuildTarget,TargetGroup:{buildTargetGroup}, BuildTarget:{platform}##########");
            return EditorUserBuildSettings.SwitchActiveBuildTarget(buildTargetGroup, platform);
        }
    }
}
