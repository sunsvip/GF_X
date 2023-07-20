#if UNITY_EDITOR
using GameFramework;
using HybridCLR.Editor;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace UGF.EditorTools
{
    public class HybridCLRExtensionTool
    {
        const string DISABLE_HYBRIDCLR = "DISABLE_HYBRIDCLR";
        [MenuItem("HybridCLR/CompileDll And Copy【生成热更dll】", false, 4)]
        public static void CompileTargetDll()
        {
            CompileTargetDll(false);
        }
        public static void CompileTargetDll(bool includeAotDll)
        {
            HybridCLR.Editor.Commands.CompileDllCommand.CompileDllActiveBuildTarget();
            var desDir = UtilityBuiltin.ResPath.GetCombinePath(Application.dataPath, ConstBuiltin.HOT_FIX_DLL_DIR);
            var dllFils = Directory.GetFiles(desDir, "*.dll.bytes");
            for (int i = dllFils.Length - 1; i >= 0; i--)
            {
                File.Delete(dllFils[i]);
            }
            string[] failList = CopyHotfixDllTo(EditorUserBuildSettings.activeBuildTarget, desDir, includeAotDll);
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

            foreach (var dll in HybridCLR.Editor.SettingsUtil.HotUpdateAssemblyFilesIncludePreserved)
            {
                string dllPath = UtilityBuiltin.ResPath.GetCombinePath(hotfixDllSrcDir, dll);
                if (File.Exists(dllPath))
                {
                    string dllBytesPath = UtilityBuiltin.ResPath.GetCombinePath(desDir, Utility.Text.Format("{0}.bytes", dll));
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
            var hotfixListFile = UtilityBuiltin.ResPath.GetCombinePath(Application.dataPath, ConstBuiltin.HOT_FIX_DLL_DIR, "HotfixFileList.txt");
            File.WriteAllText(hotfixListFile, UtilityBuiltin.Json.ToJson(HybridCLR.Editor.SettingsUtil.HotUpdateAssemblyFilesIncludePreserved), System.Text.Encoding.UTF8);
            AssetDatabase.Refresh();
            return failList.ToArray();
        }
        public static string[] CopyAotDllsToProject(BuildTarget target)
        {
            List<string> failList = new List<string>();
            string aotDllDir = HybridCLR.Editor.SettingsUtil.GetAssembliesPostIl2CppStripDir(target);
            string aotSaveDir = UtilityBuiltin.ResPath.GetCombinePath(Application.dataPath, "Resources", ConstBuiltin.AOT_DLL_DIR);
            if (Directory.Exists(aotSaveDir))
            {
                Directory.Delete(aotSaveDir, true);
            }
            Directory.CreateDirectory(aotSaveDir);
            foreach (var dll in HybridCLR.Editor.SettingsUtil.AOTAssemblyNames)
            {
                string dllPath = UtilityBuiltin.ResPath.GetCombinePath(aotDllDir, dll.EndsWith(".dll") ? dll : dll + ".dll");
                if (!File.Exists(dllPath))
                {
                    Debug.LogWarning($"ab中添加AOT补充元数据dll:{dllPath} 时发生错误,文件不存在。裁剪后的AOT dll在BuildPlayer时才能生成，因此需要你先构建一次游戏App后再打包。");
                    failList.Add(dllPath);
                    continue;
                }
                string dllBytesPath = UtilityBuiltin.ResPath.GetCombinePath(aotSaveDir, Utility.Text.Format("{0}.bytes", dll));
                File.Copy(dllPath, dllBytesPath, true);
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
            HybridCLRSettings.Instance.enable = true;
            HybridCLRSettings.Save();
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
            HybridCLRSettings.Instance.enable = false;
            HybridCLRSettings.Save();
            EditorUtility.DisplayDialog("HybridCLR", "切换到单机模式,已禁用HybridCLR热更! 记得在ResourceEditor中移除热更dll资源.", "知道了");
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

            var builtinFile = UtilityBuiltin.ResPath.GetCombinePath(assetParentDir, ConstEditor.BuiltinAssembly);
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
                Environment.SetEnvironmentVariable("UNITY_IL2CPP_PATH", string.Empty);
                Debug.Log("Remove UNITY_IL2CPP_PATH");
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
                if (Directory.Exists(HybridCLR.Editor.SettingsUtil.LocalIl2CppDir))
                {
                    Environment.SetEnvironmentVariable("UNITY_IL2CPP_PATH", HybridCLR.Editor.SettingsUtil.LocalIl2CppDir);
                    Debug.Log("Set UNITY_IL2CPP_PATH:" + HybridCLR.Editor.SettingsUtil.LocalIl2CppDir);
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
    }

}
#endif