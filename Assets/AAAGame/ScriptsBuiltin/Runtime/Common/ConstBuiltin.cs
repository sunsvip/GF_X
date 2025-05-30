/// <summary>
/// 内置Const(非热更)
/// </summary
public static class ConstBuiltin
{
    public readonly static string HOT_FIX_DLL_DIR = "AAAGame/HotfixDlls";
    public readonly static string AOT_DLL_DIR = "AotDlls";//相对于Resources目录
    public readonly static string CheckVersionUrl = "http://127.0.0.1/1_0_0_1/";//热更新检测地址
    public readonly static string VersionFile = "version.json";
    public readonly static bool NoNetworkAllow = true;//热更模式时没网络是否允许进入游戏
    public readonly static string DES_KEY = "VaBwUXzd";//网络数据DES加密
    public readonly static string AOT_DLLS_KEY = "password";//AOT dll加密解密key

    /// <summary>
    /// DataTable,Config,Language都支持AB测试,文件分为主文件和AB测试文件, AB测试文件名以'#'+ AB测试组名字结尾
    /// </summary>
    public const char AB_TEST_TAG = '#';
    /// <summary>
    /// 用户设置Key
    /// </summary>
    [Obfuz.ObfuzIgnore]
    public static class Setting
    {
        /// <summary>
        /// 语言国际化
        /// </summary>
        public readonly static string Language = "Setting.Language";
        /// <summary>
        /// 退出App时间
        /// </summary>
        public readonly static string QuitAppTime = "Setting.QuitAppTime";
        /// <summary>
        /// A/B测试组
        /// </summary>
        public readonly static string ABTestGroup = "Setting.ABTestGroup";
    }
}
