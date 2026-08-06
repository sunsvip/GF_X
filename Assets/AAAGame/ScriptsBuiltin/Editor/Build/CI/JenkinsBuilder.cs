using UnityEngine;

namespace UGF.EditorTools
{
    public class JenkinsBuilder
    {
        const string BuildResourceConfigFile = "Tools/Jenkins/BuildResourceConfig.json";
        const string BuildAppConfigFile = "Tools/Jenkins/BuildAppConfig.json";

        public static void BuildResource()
        {
            Debug.Log("##########################Start BuildResource############################");
            if (!JenkinsBuildConfigLoader.TryLoadConfig(BuildResourceConfigFile, "Build Resources", out JenkinsBuildResourceConfig configJson))
            {
                return;
            }

            if (!BuildTargetSwitchService.CheckAndSwitchPlatform(configJson.Platform))
            {
                Debug.LogError($"#####################Build Resources failed! Switch platform [{configJson.Platform}] failed.#####################");
                return;
            }

            JenkinsBuildExecutionService.ExecuteResourceBuild(configJson);
        }
        public static void BuildApp()
        {
            Debug.Log("#########################Start BuildApp################################");
            if (!JenkinsBuildConfigLoader.TryLoadConfig(BuildAppConfigFile, "Build App", out JenkinsBuildAppConfig configJson))
            {
                return;
            }
            if (!BuildTargetSwitchService.CheckAndSwitchPlatform(configJson.Platform))
            {
                Debug.LogError($"############Build App failed! Switch platform [{configJson.Platform}] failed.###########");
                return;
            }
            JenkinsBuildExecutionService.ExecuteAppBuild(configJson);
        }
    }
}

