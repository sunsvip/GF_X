using GameFramework;
using UnityGameFramework.Runtime;

public static class LocalizationExtension
{
    public static void LoadLanguage(this LocalizationComponent com, string name, string abTestGroup, object userData)
    {
        string assetName = name;
        if (!string.IsNullOrWhiteSpace(abTestGroup))
        {
            var abTestAssetName = Utility.Text.Format("{0}{1}{2}", name, ConstBuiltin.AB_TEST_TAG, abTestGroup);
            if (GF.Resource.HasAsset(UtilityBuiltin.ResPath.GetLanguagePath(abTestAssetName)) != GameFramework.Resource.HasAssetResult.NotExist)
            {
                assetName = abTestAssetName;
            }
        }
        com.ReadData(UtilityBuiltin.ResPath.GetLanguagePath(assetName), userData);
    }
    public static void LoadLanguage(this LocalizationComponent com, string name, object userData)
    {
        string abTestGroup = GFBuiltin.Setting.GetABTestGroup();
        com.LoadLanguage(name, abTestGroup, userData);
    }
}
