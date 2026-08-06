#if UNITY_EDITOR
using System.IO;
using UnityEngine;
namespace UGF.EditorTools
{

    /// <summary>
    /// 默认编辑器配置项
    /// </summary>
    public static class ConstEditor
    {
        public const bool AutoScriptUTF8 = true;//新建脚本时自动修改脚本编码方式为utf-8以支持中文
        public const bool ResolveDuplicateAssets = true;
        public const string GameDataRoot = "AAAGameData";
        public const string DataTableExcelRoot = GameDataRoot + "/DataTables";
        public const string ConfigExcelRoot = GameDataRoot + "/Configs";
        public const string LanguageExcelRoot = GameDataRoot + "/Languages";
        public const string RuntimeDataTableRoot = "Assets/AAAGame/DataTable";
        public const string RuntimeConfigRoot = "Assets/AAAGame/Config";
        public const string RuntimeLanguageRoot = "Assets/AAAGame/Language";
        public const string UIViewScriptFile = "Assets/AAAGame/Scripts/UI/Core/UIViews.cs";
        public const string UISerializeFieldDir = "Assets/AAAGame/Scripts/UI/UIVariables";//生成UI变量代码目录
        public const string UIItemSerializeFiledDir = "Assets/AAAGame/Scripts/UI/UIItemVariables";//生成UI变量代码目录
        public const string UITableName = "Core/UITable";
        public const string EntityGroupTableName = "Core/EntityGroupTable";
        public const string SoundGroupTableName = "Core/SoundGroupTable";
        public const string UIGroupTableName = "Core/UIGroupTable";
        public const string UITableExcel = UITableName + ".xlsx";
        public static string UITableExcelFullPath => GetDataTableExcelPath(UITableName);

        public const string EntityGroupTableExcel = EntityGroupTableName + ".xlsx";
        public static string EntityGroupTableExcelFullPath => GetDataTableExcelPath(EntityGroupTableName);

        public const string SoundGroupTableExcel = SoundGroupTableName + ".xlsx";
        public static string SoundGroupTableExcelFullPath => GetDataTableExcelPath(SoundGroupTableName);

        public const string UIGroupTableExcel = UIGroupTableName + ".xlsx";
        public static string UIGroupTableExcelFullPath => GetDataTableExcelPath(UIGroupTableName);
        public static readonly string[] FrameworkRequiredDataTables =
        {
            EntityGroupTableName,
            SoundGroupTableName,
            UIGroupTableName,
            UITableName,
        };

        public const string ConstGroupScriptFileFullName = "Assets/AAAGame/Scripts/Common/Core/Const.Groups.cs";

        public static readonly string PrefabsPath = "Assets/AAAGame/Prefabs";
        public static readonly string ScenePath = "Assets/AAAGame/Scene";

        public const string DataTableCodeTemplate = "Assets/AAAGame/ScriptsBuiltin/Editor/Data/DataTable/Templates/DataTableCodeTemplate/DataTableCodeTemplate.txt"; //生成配置表代码的模板文件
        public const string BuiltinAssembly = "Assets/AAAGame/ScriptsBuiltin/Runtime/Builtin.Runtime.asmdef";
        public const string HotfixAssembly = "Assets/AAAGame/Scripts/Hotfix.asmdef";


        public const string SharedAssetBundleName = "SharedAssets";//AssetBundle分包共用资源
        internal static readonly string KeystorePass = "topgames";
        internal static readonly string KeyAliasPass = "topgames";
        internal static string KeystoreName => UtilityBuiltin.AssetsPath.GetCombinePath(ProjectRootPath, "user.keystore");
        internal static readonly string KeyAliasName = "release";
        internal static string AssetBundleOutputPath => UtilityBuiltin.AssetsPath.GetCombinePath(ProjectRootPath, "AB");
        public static readonly string UpdatePrefixUri = "http://127.0.0.1/1_0_0_1/";//默认资源下载地址
        internal static readonly string AppUpdateUrl = "https://play.google.com/store/apps/details?id=";

        /// <summary>
        /// 数据表Excel目录
        /// </summary>
        public static string DataTableExcelPath => UtilityBuiltin.AssetsPath.GetCombinePath(ProjectRootPath, DataTableExcelRoot);
        /// <summary>
        /// 配置表Excel目录
        /// </summary>
        public static string ConfigExcelPath => UtilityBuiltin.AssetsPath.GetCombinePath(ProjectRootPath, ConfigExcelRoot);
        /// <summary>
        /// 语言国际化Excel目录
        /// </summary>
        public static string LanguageExcelPath => UtilityBuiltin.AssetsPath.GetCombinePath(ProjectRootPath, LanguageExcelRoot);

        public static string ToolsPath => UtilityBuiltin.AssetsPath.GetCombinePath(ProjectRootPath, "Tools");
        public const string DataTablePath = RuntimeDataTableRoot;
        public const string GameConfigPath = RuntimeConfigRoot;
        public const string LanguagePath = RuntimeLanguageRoot;
        public const string DataTableCodePath = "Assets/AAAGame/Scripts/DataTable";
        public const string UIScriptsPath = "Assets/AAAGame/Scripts/UI";
        public const string UIItemScriptsPath = "Assets/AAAGame/Scripts/UI/Item";
        public const string UIFormTemplate = "Assets/AAAGame/ScriptsBuiltin/Editor/UI/Templates/UIFormTemplate.prefab";
        public const string UIDialogTemplate = "Assets/AAAGame/ScriptsBuiltin/Editor/UI/Templates/UIDialogTemplate.prefab";
        public const string UIItemTemplate = "Assets/AAAGame/ScriptsBuiltin/Editor/UI/Templates/UIItemTemplate.prefab";
        public const string UIScriptFileTemplate = "Assets/AAAGame/ScriptsBuiltin/Editor/UI/Templates/UIScriptFileTemplate.txt";
        public const string UIItemScriptFileTemplate = "Assets/AAAGame/ScriptsBuiltin/Editor/UI/Templates/UIItemScriptFileTemplate.txt";
        private static readonly string s_ProjectRootPath = Directory.GetParent(Application.dataPath)?.FullName ?? Application.dataPath;

        public static string ProjectRootPath => s_ProjectRootPath;

        public static string GetDataTableExcelPath(string tableName)
        {
            return UtilityBuiltin.AssetsPath.GetCombinePath(DataTableExcelPath, tableName + ".xlsx");
        }

        public static string GetConfigExcelPath(string configName)
        {
            return UtilityBuiltin.AssetsPath.GetCombinePath(ConfigExcelPath, configName + ".xlsx");
        }

        public static string GetLanguageExcelPath(string languageName)
        {
            return UtilityBuiltin.AssetsPath.GetCombinePath(LanguageExcelPath, languageName + ".xlsx");
        }

        public static string GetDataTableOutputPath(string tableName, bool useBytes)
        {
            return UtilityBuiltin.AssetsPath.GetCombinePath(RuntimeDataTableRoot, tableName + (useBytes ? ".bytes" : ".txt"));
        }

        public static string GetConfigOutputPath(string configName, bool useBytes)
        {
            return UtilityBuiltin.AssetsPath.GetCombinePath(RuntimeConfigRoot, configName + (useBytes ? ".bytes" : ".txt"));
        }

        public static string GetLanguageOutputPath(string languageName, bool useBytes)
        {
            return UtilityBuiltin.AssetsPath.GetCombinePath(RuntimeLanguageRoot, languageName + (useBytes ? ".bytes" : ".json"));
        }

    }
}
#endif
