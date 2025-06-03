#if UNITY_EDITOR
using GameFramework;
using HybridCLR.Editor;
using HybridCLR.Editor.Commands;
using Newtonsoft.Json.Linq;
using Obfuz.Settings;
using Obfuz;
using Obfuz.Unity;
using Obfuz4HybridCLR;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using System.Text;

namespace UGF.EditorTools
{
    public class HybridCLRExtensionTool
    {
        public const string DISABLE_HYBRIDCLR = "DISABLE_HYBRIDCLR";
        public const string ENABLE_OBFUZ = "ENABLE_OBFUZ";
        [MenuItem("HybridCLR/CompileDll And Copy[生成热更dll]", false, 4)]
        public static void CompileTargetDll()
        {
            CompileTargetDll(false);
        }
        [MenuItem("HybridCLR/Copy AotDll To Project[AOT dlls到工程]", false, 5)]
        public static void CopyAotDll2ResourcePath()
        {
            CopyAotDllsToProject(EditorUserBuildSettings.activeBuildTarget);
        }
        [MenuItem("HybridCLR/ObfuzExtension/Obfuz GenerateLinkXml[混淆后代码裁剪配置]", false)]
        public static void GenerateLinkXml()
        {
            Obfuz.Unity.LinkXmlProcess.GenerateAdditionalLinkXmlFile(EditorUserBuildSettings.activeBuildTarget);
        }
        public static void CompileTargetDll(bool includeAotDll)
        {
            var activeTarget = EditorUserBuildSettings.activeBuildTarget;
            HybridCLR.Editor.Commands.CompileDllCommand.CompileDllActiveBuildTarget();
            if (Obfuz.Settings.ObfuzSettings.Instance.enable)
            {
                ObfuscateUtil.ObfuscateHotUpdateAssemblies(activeTarget, GetObfuzDllsDir(activeTarget));
            }
            var desDir = UtilityBuiltin.AssetsPath.GetCombinePath(Application.dataPath, ConstBuiltin.HOT_FIX_DLL_DIR);
            var dllFils = Directory.GetFiles(desDir, "*.dll.bytes");
            for (int i = dllFils.Length - 1; i >= 0; i--)
            {
                File.Delete(dllFils[i]);
            }
            string[] failList = CopyHotfixDllTo(activeTarget, desDir, includeAotDll);
            string content = $"Compile dlls and copy to '{ConstBuiltin.HOT_FIX_DLL_DIR}' success.";
            if (failList.Length > 0)
            {
                content = "Error! Missing file:" + Environment.NewLine;
                foreach (var item in failList)
                {
                    content += item + Environment.NewLine;
                }
                EditorUtility.DisplayDialog("CompileDll And Copy", content, "OK");
                return;
            }
        }
        static string GetObfuzDllsDir(BuildTarget activeTarget)
        {
            return SettingsUtil.GetHotUpdateDllsOutputDirByTarget(activeTarget) + "Obfuz";
        }
        /// <summary>
        /// 把热更新dll拷贝到指定目录
        /// </summary>
        /// <param name="target">平台</param>
        /// <param name="desDir">拷贝到目标目录</param>
        /// <param name="copyAotMeta">是否同时拷贝AOT元数据补充dll</param>
        /// <returns></returns>
        public static string[] CopyHotfixDllTo(BuildTarget target, string desDir, bool copyAotMeta = true)
        {
            List<string> failList = new List<string>();
            string hotfixDllSrcDir = HybridCLR.Editor.SettingsUtil.GetHotUpdateDllsOutputDirByTarget(target);
            string obfuzDllSrcDir = GetObfuzDllsDir(target);
            var obfuzDllList = Obfuz.Settings.ObfuzSettings.Instance.assemblySettings.GetObfuscationRelativeAssemblyNames();
            foreach (var dll in HybridCLR.Editor.SettingsUtil.HotUpdateAssemblyNamesIncludePreserved)
            {
                bool isObfuzDll = Obfuz.Settings.ObfuzSettings.Instance.enable && obfuzDllList.Contains(dll);
                string dllPath = UtilityBuiltin.AssetsPath.GetCombinePath(isObfuzDll ? obfuzDllSrcDir : hotfixDllSrcDir, dll + ".dll");
                if (File.Exists(dllPath))
                {
                    string dllBytesPath = UtilityBuiltin.AssetsPath.GetCombinePath(desDir, Utility.Text.Format("{0}.bytes", dll));
                    File.Copy(dllPath, dllBytesPath, true);
                }
                else
                {
                    failList.Add(dllPath);
                }
            }

            if (copyAotMeta)
            {
                var failNames = CopyAotDllsToProject(target);
                failList.AddRange(failNames);
            }
            var hotfixListFile = UtilityBuiltin.AssetsPath.GetCombinePath(Application.dataPath, ConstBuiltin.HOT_FIX_DLL_DIR, "HotfixFileList.txt");
            File.WriteAllText(hotfixListFile, UtilityBuiltin.Json.ToJson(HybridCLR.Editor.SettingsUtil.HotUpdateAssemblyNamesIncludePreserved), System.Text.Encoding.UTF8);
            AssetDatabase.Refresh();
            return failList.ToArray();
        }
        public static string[] CopyAotDllsToProject(BuildTarget target)
        {
            List<string> failList = new List<string>();
            string aotDllDir = HybridCLR.Editor.SettingsUtil.GetAssembliesPostIl2CppStripDir(target);
            string aotSaveDir = UtilityBuiltin.AssetsPath.GetCombinePath(Application.dataPath, "Resources", ConstBuiltin.AOT_DLL_DIR);
            if (Directory.Exists(aotSaveDir))
            {
                Directory.Delete(aotSaveDir, true);
            }
            Directory.CreateDirectory(aotSaveDir);
            var aotDllEncryptCode = UTF8Encoding.UTF8.GetBytes(ConstBuiltin.AOT_DLLS_KEY);
            foreach (var dll in HybridCLR.Editor.SettingsUtil.AOTAssemblyNames)
            {
                string dllPath = UtilityBuiltin.AssetsPath.GetCombinePath(aotDllDir, dll + ".dll");
                if (!File.Exists(dllPath))
                {
                    Debug.LogWarning($"ab中添加AOT补充元数据dll:{dllPath} 时发生错误,文件不存在。裁剪后的AOT dll在BuildPlayer时才能生成，因此需要你先构建一次游戏App后再打包。");
                    failList.Add(dllPath);
                    continue;
                }
                string dllBytesPath = UtilityBuiltin.AssetsPath.GetCombinePath(aotSaveDir, Utility.Text.Format("{0}.bytes", dll));

                var dllBytes = File.ReadAllBytes(dllPath);
                if (AppSettings.Instance.EncryptAOTDlls != null && AppSettings.Instance.EncryptAOTDlls.Contains(dll))
                {
                    Utility.Encryption.GetQuickSelfXorBytes(dllBytes, aotDllEncryptCode);
                }
                File.WriteAllBytes(dllBytesPath, dllBytes);
            }

            return failList.ToArray();
        }
        public static void EnableHybridCLR()
        {
#if UNITY_2021_1_OR_NEWER
            var bTarget = GetCurrentNamedBuildTarget();
            PlayerSettings.GetScriptingDefineSymbols(bTarget, out string[] defines);
#else
        var bTarget = GetCurrentBuildTarget();
        PlayerSettings.GetScriptingDefineSymbolsForGroup(bTarget, out string[] defines);
#endif
            if (ArrayUtility.Contains(defines, DISABLE_HYBRIDCLR))
            {
                ArrayUtility.Remove<string>(ref defines, DISABLE_HYBRIDCLR);
#if UNITY_2021_1_OR_NEWER
                PlayerSettings.SetScriptingDefineSymbols(bTarget, defines);
#else
            PlayerSettings.SetScriptingDefineSymbolsForGroup(bTarget, defines);
#endif
            }
            RefreshPlayerSettings();
            RefreshAssemblyDefinition(false);
            HybridCLR.Editor.Settings.HybridCLRSettings.Instance.enable = true;
            HybridCLR.Editor.Settings.HybridCLRSettings.Save();
            EditorUtility.DisplayDialog("HybridCLR", "切换到热更模式,已启用HybridCLR热更! 记得在ResourceEditor中添加热更dll资源.", "知道了");
        }
        public static void DisableHybridCLR()
        {
#if UNITY_2021_1_OR_NEWER
            var bTarget = GetCurrentNamedBuildTarget();
            PlayerSettings.GetScriptingDefineSymbols(bTarget, out string[] defines);
#else
        var bTarget = GetCurrentBuildTarget();
        PlayerSettings.GetScriptingDefineSymbolsForGroup(bTarget, out string[] defines);
#endif
            if (!ArrayUtility.Contains(defines, DISABLE_HYBRIDCLR))
            {
                ArrayUtility.Add<string>(ref defines, DISABLE_HYBRIDCLR);
#if UNITY_2021_1_OR_NEWER
                PlayerSettings.SetScriptingDefineSymbols(bTarget, defines);
#else
            PlayerSettings.SetScriptingDefineSymbolsForGroup(bTarget, defines);
#endif
            }
            RefreshPlayerSettings();
            RefreshAssemblyDefinition(true);
            HybridCLR.Editor.Settings.HybridCLRSettings.Instance.enable = false;
            HybridCLR.Editor.Settings.HybridCLRSettings.Save();
            EditorUtility.DisplayDialog("HybridCLR", "切换到单机模式,已禁用HybridCLR热更! 记得在ResourceEditor中移除热更dll资源.", "知道了");
        }
        public static void DisableObfuz()
        {
#if UNITY_2021_1_OR_NEWER
            var bTarget = GetCurrentNamedBuildTarget();
            PlayerSettings.GetScriptingDefineSymbols(bTarget, out string[] defines);
#else
        var bTarget = GetCurrentBuildTarget();
        PlayerSettings.GetScriptingDefineSymbolsForGroup(bTarget, out string[] defines);
#endif
            if (ArrayUtility.Contains(defines, ENABLE_OBFUZ))
            {
                ArrayUtility.Remove<string>(ref defines, ENABLE_OBFUZ);
#if UNITY_2021_1_OR_NEWER
                PlayerSettings.SetScriptingDefineSymbols(bTarget, defines);
#else
            PlayerSettings.SetScriptingDefineSymbolsForGroup(bTarget, defines);
#endif
            }
        }
        public static void EnableObfuz()
        {
            ObfuzMenu.GenerateEncryptionVM();
            ObfuzMenu.SaveSecretFile();
#if UNITY_2021_1_OR_NEWER
            var bTarget = GetCurrentNamedBuildTarget();
            PlayerSettings.GetScriptingDefineSymbols(bTarget, out string[] defines);
#else
        var bTarget = GetCurrentBuildTarget();
        PlayerSettings.GetScriptingDefineSymbolsForGroup(bTarget, out string[] defines);
#endif
            if (!ArrayUtility.Contains(defines, ENABLE_OBFUZ))
            {
                ArrayUtility.Add<string>(ref defines, ENABLE_OBFUZ);
#if UNITY_2021_1_OR_NEWER
                PlayerSettings.SetScriptingDefineSymbols(bTarget, defines);
#else
            PlayerSettings.SetScriptingDefineSymbolsForGroup(bTarget, defines);
#endif
            }
        }
        private static void RefreshPlayerSettings()
        {
#if DISABLE_HYBRIDCLR
            PlayerSettings.gcIncremental = true;
#else
            PlayerSettings.gcIncremental = false;
            PlayerSettings.SetScriptingBackend(EditorUserBuildSettings.selectedBuildTargetGroup, ScriptingImplementation.IL2CPP);
#if UNITY_2021_1_OR_NEWER
            PlayerSettings.SetApiCompatibilityLevel(EditorUserBuildSettings.selectedBuildTargetGroup, ApiCompatibilityLevel.NET_Unity_4_8);
#else
        PlayerSettings.SetApiCompatibilityLevel(EditorUserBuildSettings.selectedBuildTargetGroup, ApiCompatibilityLevel.NET_4_6);
#endif
#endif
        }
        private static void RefreshAssemblyDefinition(bool disableHybridCLR)
        {
            var assetParentDir = Directory.GetParent(Application.dataPath).FullName;

            var builtinFile = UtilityBuiltin.AssetsPath.GetCombinePath(assetParentDir, ConstEditor.BuiltinAssembly);
            var textData = File.ReadAllText(builtinFile);
            var jsonData = UtilityBuiltin.Json.ToObject<Newtonsoft.Json.Linq.JObject>(textData);
            var refAsmbs = jsonData["references"] as Newtonsoft.Json.Linq.JArray;

            if (refAsmbs.Count() > 0)
            {
                if (refAsmbs[0].Value<string>().StartsWith("GUID:"))
                {
                    EditorUtility.DisplayDialog("Error", Utility.Text.Format("解析Assembly Definition文件{0}失败: 请将其Use GUIDs设置为false后重试!", ConstEditor.BuiltinAssembly), "OK");
                    return;
                }
            }
            Environment.SetEnvironmentVariable("UNITY_IL2CPP_PATH", disableHybridCLR ? "" : HybridCLR.Editor.SettingsUtil.LocalIl2CppDir);
            if (disableHybridCLR)
            {
                bool changed = false;
                for (int i = refAsmbs.Count() - 1; i >= 0; i--)
                {
                    if (refAsmbs[i].Value<string>().CompareTo("HybridCLR.Runtime") == 0)
                    {
                        refAsmbs.RemoveAt(i);
                        changed = true;
                        break;
                    }
                }
                if (changed)
                {
                    File.WriteAllText(builtinFile, jsonData.ToString(Newtonsoft.Json.Formatting.Indented));
                    AssetDatabase.Refresh();
                }
            }
            else
            {
                var hasValue = false;
                for (int i = refAsmbs.Count() - 1; i >= 0; i--)
                {
                    if (refAsmbs[i].Value<string>().CompareTo("HybridCLR.Runtime") == 0)
                    {
                        hasValue = true;
                        break;
                    }
                }
                if (!hasValue)
                {
                    refAsmbs.Add("HybridCLR.Runtime");
                    File.WriteAllText(builtinFile, jsonData.ToString(Newtonsoft.Json.Formatting.Indented));
                    AssetDatabase.Refresh();
                }
            }
        }
        private static UnityEditor.BuildTargetGroup GetCurrentBuildTarget()
        {
#if UNITY_ANDROID
            return UnityEditor.BuildTargetGroup.Android;
#elif UNITY_IOS
        return UnityEditor.BuildTargetGroup.iOS;
#elif UNITY_STANDALONE
            return UnityEditor.BuildTargetGroup.Standalone;
#elif UNITY_WEBGL
        return UnityEditor.BuildTargetGroup.WebGL;
#else
        return UnityEditor.BuildTargetGroup.Unknown;
#endif
        }
#if UNITY_2021_1_OR_NEWER
        private static UnityEditor.Build.NamedBuildTarget GetCurrentNamedBuildTarget()
        {
#if UNITY_ANDROID
            return UnityEditor.Build.NamedBuildTarget.Android;
#elif UNITY_IOS
        return UnityEditor.Build.NamedBuildTarget.iOS;
#elif UNITY_STANDALONE
            return UnityEditor.Build.NamedBuildTarget.Standalone;
#elif UNITY_WEBGL
        return UnityEditor.Build.NamedBuildTarget.WebGL;
#else
        return UnityEditor.Build.NamedBuildTarget.Unknown;
#endif
        }
#endif
        public static string GetStripAssembliesDir(BuildTarget target)
        {
            string projectDir = Directory.GetParent(Application.dataPath).ToString();
            switch (target)
            {
                case BuildTarget.StandaloneWindows:
                case BuildTarget.StandaloneWindows64:
                    return $"{projectDir}/Library/Bee/artifacts/WinPlayerBuildProgram/ManagedStripped";
                case BuildTarget.StandaloneLinux64:
                    return $"{projectDir}/Library/Bee/artifacts/LinuxPlayerBuildProgram/ManagedStripped";
                case BuildTarget.WSAPlayer:
                    return $"{projectDir}/Library/Bee/artifacts/UWPPlayerBuildProgram/ManagedStripped";
                case BuildTarget.Android:
                    return $"{projectDir}/Library/Bee/artifacts/Android/ManagedStripped";
#if TUANJIE_2022_3_OR_NEWER
                case BuildTarget.HMIAndroid:
                    return $"{projectDir}/Library/Bee/artifacts/HMIAndroid/ManagedStripped";
#endif
                case BuildTarget.iOS:
#if UNITY_TVOS
                case BuildTarget.tvOS:
#endif
                    return $"{projectDir}/Library/Bee/artifacts/iOS/ManagedStripped";
#if UNITY_VISIONOS
                case BuildTarget.VisionOS:
#if UNITY_6000_0_OR_NEWER
                return $"{projectDir}/Library/Bee/artifacts/VisionOS/ManagedStripped";
#else
                return $"{projectDir}/Library/Bee/artifacts/iOS/ManagedStripped";
#endif
#endif
                case BuildTarget.WebGL:
                    return $"{projectDir}/Library/Bee/artifacts/WebGL/ManagedStripped";
                case BuildTarget.StandaloneOSX:
                    return $"{projectDir}/Library/Bee/artifacts/MacStandalonePlayerBuildProgram/ManagedStripped";
                case BuildTarget.PS4:
                    return $"{projectDir}/Library/Bee/artifacts/PS4PlayerBuildProgram/ManagedStripped";
                case BuildTarget.PS5:
                    return $"{projectDir}/Library/Bee/artifacts/PS5PlayerBuildProgram/ManagedStripped";
#if UNITY_WEIXINMINIGAME
                case BuildTarget.WeixinMiniGame:
                    return $"{projectDir}/Library/Bee/artifacts/WeixinMiniGame/ManagedStripped";
#endif
#if UNITY_OPENHARMONY
                case BuildTarget.OpenHarmony:
                    return $"{projectDir}/Library/Bee/artifacts/OpenHarmonyPlayerBuildProgram/ManagedStripped";
#endif
                default: return "";
            }
        }
    }

}
#endif