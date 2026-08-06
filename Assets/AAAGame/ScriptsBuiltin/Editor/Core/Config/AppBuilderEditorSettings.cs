#if UNITY_EDITOR
namespace UGF.EditorTools
{
    [UGF.EditorTools.FilePath("ProjectSettings/AppBuilderEditorSettings.asset")]
    public class AppBuilderEditorSettings : UGF.EditorTools.EditorScriptableSingleton<AppBuilderEditorSettings>
    {
        public string UpdatePrefixUri;
        /// <summary>
        /// HybridCLR需要
        /// </summary>
        public bool Netstandard2NetFramework = true;
        public string ApplicableGameVersion;
        public bool ForceUpdateApp = false;
        public string AppUpdateUrl;
        public string AppUpdateDesc;
        public bool RevealFolder = false;
        public bool EnableResourceRuleEditor = false;

        //Android Build Settings
        public bool AndroidUseKeystore;
        public string AndroidKeystoreName;
        public string KeystorePass;
        public string AndroidKeyAliasName;
        public string KeyAliasPass;

        public string ResourceBuildDir = "AB";
        public string AppBuildDir = "BuildApp";
        public string[] Netstandard2NetFrameworkList;


        //dll混淆加密
        public bool EnableObfuz = false;
    }

}
#endif
