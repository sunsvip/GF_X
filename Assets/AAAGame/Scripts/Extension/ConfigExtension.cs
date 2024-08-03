using UnityEngine;
using GameFramework;
using UnityGameFramework.Runtime;
public static class ConfigExtension
{
    public static void LoadConfig(this ConfigComponent cfg, string name, string abTestGroup, object userData)
    {
        string cfgName = name;
        if (!string.IsNullOrWhiteSpace(abTestGroup))
        {
            var abTestCfgName = Utility.Text.Format("{0}{1}{2}", name, ConstBuiltin.AB_TEST_TAG, abTestGroup);
            if (GF.Resource.HasAsset(UtilityBuiltin.AssetsPath.GetConfigPath(abTestCfgName)) != GameFramework.Resource.HasAssetResult.NotExist)
            {
                cfgName = abTestCfgName;
            }
        }
        cfg.ReadData(UtilityBuiltin.AssetsPath.GetConfigPath(cfgName), userData);
    }
    public static void LoadConfig(this ConfigComponent cfg, string name, object userData)
    {
        string abTestGroup = GFBuiltin.Setting.GetABTestGroup();
        cfg.LoadConfig(name, abTestGroup, userData);
    }
    public static Vector2Int GetVector2Int(this ConfigComponent cfg, string key)
    {
        return cfg.GetVector2Int(key, Vector2Int.zero);
    }
    public static Vector2Int GetVector2Int(this ConfigComponent cfg, string key, Vector2Int defaultValue = default)
    {
        if (!cfg.HasConfig(key)) return defaultValue;

        return DataTableExtension.ParseVector2Int(cfg.GetString(key));
    }
    public static Vector2 GetVector2(this ConfigComponent cfg, string key)
    {
        return cfg.GetVector2(key, Vector2.zero);
    }
    public static Vector2 GetVector2(this ConfigComponent cfg, string key, Vector2 defaultValue = default)
    {
        if (!cfg.HasConfig(key)) return defaultValue;

        return DataTableExtension.ParseVector2(cfg.GetString(key));
    }

    public static Vector3 GetVector3(this ConfigComponent cfg, string key)
    {
        return cfg.GetVector3(key, Vector3.zero);
    }
    public static Vector3 GetVector3(this ConfigComponent cfg, string key, Vector3 defaultValue = default)
    {
        if (!cfg.HasConfig(key)) return defaultValue;

        return DataTableExtension.ParseVector3(cfg.GetString(key));
    }
    public static T[] GetArray<T>(this ConfigComponent cfg, string key, T[] defaultValue = null)
    {
        if (!cfg.HasConfig(key)) return defaultValue;

        return DataTableExtension.ParseArray<T>(cfg.GetString(key));
    }
}
