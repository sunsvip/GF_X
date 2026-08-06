using UnityEditor;

namespace UGF.EditorTools
{
    public static class HybridCLRExtensionTool
    {
        public const string ENABLE_HYBRIDCLR = "ENABLE_HYBRIDCLR";
        public const string ENABLE_OBFUZ = "ENABLE_OBFUZ";

        [MenuItem("HybridCLR/CompileDll And Copy[生成热更dll]", false, 4)]
        public static void CompileTargetDll()
        {
            CompileTargetDll(false);
        }

        [MenuItem("HybridCLR/Copy AotDll To Project[AOT dlls到工程]", false, 5)]
        public static void CopyAotDll2ResourcePath()
        {
            HybridClrArtifactsCopier.CopyAotDllsToProject(EditorUserBuildSettings.activeBuildTarget);
        }

        [MenuItem("HybridCLR/ObfuzExtension/Obfuz GenerateLinkXml[混淆后代码裁剪配置]", false)]
        public static void GenerateLinkXml()
        {
            Obfuz.Unity.LinkXmlProcess.GenerateAdditionalLinkXmlFile(EditorUserBuildSettings.activeBuildTarget);
        }

        public static void CompileTargetDll(bool copyAotDlls)
        {
            CompileTargetDll(copyAotDlls, EditorUserBuildSettings.activeBuildTarget);
        }

        public static void CompileTargetDll(bool copyAotDlls, BuildTarget activeTarget)
        {
            HybridClrArtifactsCopier.CompileTargetDll(copyAotDlls, activeTarget);
        }

        public static string[] CopyHotfixDllTo(BuildTarget target, string desDir, bool copyAotMeta = true)
        {
            return HybridClrArtifactsCopier.CopyHotfixDllTo(target, desDir, copyAotMeta);
        }

        public static string[] CopyAotDllsToProject(BuildTarget target)
        {
            return HybridClrArtifactsCopier.CopyAotDllsToProject(target);
        }

        public static string[] CopyNetFrameworkDllToProject(string[] aotDlls)
        {
            return HybridClrArtifactsCopier.CopyNetFrameworkDllToProject(aotDlls);
        }

        public static void EnableHybridCLR()
        {
            HybridClrProjectConfigurator.EnableHybridCLR();
        }

        public static void DisableHybridCLR()
        {
            HybridClrProjectConfigurator.DisableHybridCLR();
        }

        public static void DisableObfuz()
        {
            HybridClrProjectConfigurator.DisableObfuz();
        }

        public static void EnableObfuz()
        {
            HybridClrProjectConfigurator.EnableObfuz();
        }

        public static string GetStripAssembliesDir(BuildTarget target)
        {
            return HybridClrArtifactsCopier.GetStripAssembliesDir(target);
        }
    }
}
