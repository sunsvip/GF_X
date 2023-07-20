using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace UGF.EditorTools
{
    public class JenkinsBuilder
    {
        const string BuildResourceConfigFile = "Tools/Jenkins/BuildResourceConfig.json";
        const string BuildAppConfigFile = "Tools/Jenkins/BuildAppConfig.json";
        public static void BuildResource()
        {
            Debug.Log("------------------------------Start BuildResource------------------------------");
            var configFile = UtilityBuiltin.ResPath.GetCombinePath(Directory.GetParent(Application.dataPath).FullName, BuildResourceConfigFile);
            if (!File.Exists(configFile))
            {
                Debug.LogError($"构建失败! 构建配置文件不存在:{configFile}");
                return;
            }
            JenkinsBuildResourceConfig configJson = null;
            try
            {
                var jsonStr = File.ReadAllText(configFile);
                configJson = UtilityBuiltin.Json.ToObject<JenkinsBuildResourceConfig>(jsonStr);

            }
            catch (Exception err)
            {
                Debug.LogError($"构建失败! 构建配置文件解析失败:{configFile}, Error:{err.Message}");
                return;
            }
            if (configJson == null)
            {
                Debug.LogError($"构建失败! 反序列构建配置参数失败:{configFile}");
                return;
            }
            
            if (!CheckAndSwitchPlatform(configJson.Platform))
            {
                Debug.LogError($"构建失败! 切换平台({configJson.Platform})失败.");
                return;
            }

            var appBuilder = EditorWindow.GetWindow<AppBuildEidtor>();
            appBuilder.Show();
            appBuilder.JenkinsBuildResource(configJson);
        }
        public static void BuildApp()
        {
            Debug.Log("------------------------------Start BuildApp------------------------------");
            var configFile = UtilityBuiltin.ResPath.GetCombinePath(Directory.GetParent(Application.dataPath).FullName, BuildAppConfigFile);
            if (!File.Exists(configFile))
            {
                Debug.LogError($"构建失败! 构建配置文件不存在:{configFile}");
                return;
            }
            JenkinsBuildAppConfig configJson = null;
            try
            {
                var jsonStr = File.ReadAllText(configFile);
                configJson = UtilityBuiltin.Json.ToObject<JenkinsBuildAppConfig>(jsonStr);

            }
            catch (Exception err)
            {
                Debug.LogError($"构建失败! 构建配置文件解析失败:{configFile}, Error:{err.Message}");
                return;
            }
            if (configJson == null)
            {
                Debug.LogError($"构建失败! 反序列换构建配置失败:{configFile}");
                return;
            }
            if (!CheckAndSwitchPlatform(configJson.Platform))
            {
                Debug.LogError($"构建失败! 切换平台{configJson.Platform}失败.");
                return;
            }
            var appBuilder = EditorWindow.GetWindow<AppBuildEidtor>();
            appBuilder.Show();
            appBuilder.JenkinsBuildApp(configJson);
        }
        /// <summary>
        /// 切换到目标平台
        /// </summary>
        /// <param name="platform"></param>
        /// <returns></returns>
        private static bool CheckAndSwitchPlatform(BuildTarget platform)
        {
            if (EditorUserBuildSettings.activeBuildTarget != platform)
            {
                BuildTargetGroup buildTargetGroup = BuildPipeline.GetBuildTargetGroup(platform);
                Debug.Log($"#########切换平台,TargetGroup:{buildTargetGroup}, BuildTarget:{platform}#######");
                return EditorUserBuildSettings.SwitchActiveBuildTarget(buildTargetGroup, platform);
            }
            return true;
        }
    }

    public class JenkinsBuildResourceConfig
    {
        public string ResourceOutputDir; //构建资源输出目录
        public BuildTarget Platform; //构建平台
        public bool ForceRebuild; //强制重新构建全部资源
        public int ResourceVersion; //资源版本号

        public string UpdatePrefixUrl; //热更资源服务器地址
        public string ApplicableVersions; //资源适用的App版本号
        public bool ForceUpdate; //是否强制更新App
        public string AppUpdateUrl; //App更新地址
        public string AppUpdateDescription; //App更新说明
    }
    public class JenkinsBuildAppConfig
    {
        public string ResourceOutputDir; //构建资源输出目录(只有非全热更需要)
        public BuildTarget Platform; //构建平台
        public bool FullBuild; //打包前先为热更生成AOT dll
        public bool DebugMode; //显示debug窗口
        public bool DevelopmentBuild; //调试模式
        public bool BuildAppBundle; //打Google Play aab包
        public string Version; //App版本号
        public int VersionCode; //App版本编号
    }
}

