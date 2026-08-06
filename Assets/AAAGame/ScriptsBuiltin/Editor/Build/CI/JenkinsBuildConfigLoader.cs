using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace UGF.EditorTools
{
    internal static class JenkinsBuildConfigLoader
    {
        public static bool TryLoadConfig<T>(string relativeConfigFile, string taskName, out T config) where T : class
        {
            var configFile = UtilityBuiltin.AssetsPath.GetCombinePath(ConstEditor.ProjectRootPath, relativeConfigFile);
            if (!File.Exists(configFile))
            {
                Debug.LogError($"#####################{taskName} failed! 构建配置文件不存在:{configFile}#####################");
                config = null;
                return false;
            }

            try
            {
                var jsonStr = File.ReadAllText(configFile, Encoding.UTF8);
                config = UtilityBuiltin.Json.ToObject<T>(jsonStr);
                Debug.Log($"##############{taskName} configs:{jsonStr}################");
            }
            catch (Exception err)
            {
                Debug.LogError($"#####################{taskName} failed! Parse build config file failed:{configFile}, Error:{err.Message}#####################");
                config = null;
                return false;
            }

            if (config == null)
            {
                Debug.LogError($"#####################{taskName} failed! Deserialize config file failed:{configFile}#####################");
                return false;
            }

            return true;
        }
    }
}
