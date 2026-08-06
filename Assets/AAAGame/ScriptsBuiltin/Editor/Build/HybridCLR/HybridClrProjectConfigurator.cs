using GameFramework;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.Build;

namespace UGF.EditorTools
{
    internal static class HybridClrProjectConfigurator
    {
        private static readonly NamedBuildTarget[] BuildTargets =
        {
            NamedBuildTarget.Android,
            NamedBuildTarget.iOS,
            NamedBuildTarget.Standalone,
            NamedBuildTarget.WebGL,
            NamedBuildTarget.PS4,
            NamedBuildTarget.NintendoSwitch,
            NamedBuildTarget.XboxOne
        };

        internal static void EnableHybridCLR()
        {
            foreach (NamedBuildTarget buildTarget in BuildTargets)
            {
                PlayerSettings.GetScriptingDefineSymbols(buildTarget, out string[] defines);
                if (!ArrayUtility.Contains(defines, HybridCLRExtensionTool.ENABLE_HYBRIDCLR))
                {
                    ArrayUtility.Add(ref defines, HybridCLRExtensionTool.ENABLE_HYBRIDCLR);
                    PlayerSettings.SetScriptingDefineSymbols(buildTarget, defines);
                }
            }

            RefreshPlayerSettings();
            RefreshAssemblyDefinition(false);
            HybridCLR.Editor.Settings.HybridCLRSettings.Instance.enable = true;
            HybridCLR.Editor.Settings.HybridCLRSettings.Save();
        }

        internal static void DisableHybridCLR()
        {
            foreach (NamedBuildTarget buildTarget in BuildTargets)
            {
                PlayerSettings.GetScriptingDefineSymbols(buildTarget, out string[] defines);
                if (ArrayUtility.Contains(defines, HybridCLRExtensionTool.ENABLE_HYBRIDCLR))
                {
                    ArrayUtility.Remove(ref defines, HybridCLRExtensionTool.ENABLE_HYBRIDCLR);
                    PlayerSettings.SetScriptingDefineSymbols(buildTarget, defines);
                }
            }

            RefreshPlayerSettings();
            RefreshAssemblyDefinition(true);
            HybridCLR.Editor.Settings.HybridCLRSettings.Instance.enable = false;
            HybridCLR.Editor.Settings.HybridCLRSettings.Save();
        }

        internal static void DisableObfuz()
        {
            NamedBuildTarget buildTarget = GetCurrentNamedBuildTarget();
            PlayerSettings.GetScriptingDefineSymbols(buildTarget, out string[] defines);
            if (ArrayUtility.Contains(defines, HybridCLRExtensionTool.ENABLE_OBFUZ))
            {
                ArrayUtility.Remove(ref defines, HybridCLRExtensionTool.ENABLE_OBFUZ);
                PlayerSettings.SetScriptingDefineSymbols(buildTarget, defines);
            }
        }

        internal static void EnableObfuz()
        {
            NamedBuildTarget buildTarget = GetCurrentNamedBuildTarget();
            PlayerSettings.GetScriptingDefineSymbols(buildTarget, out string[] defines);
            if (!ArrayUtility.Contains(defines, HybridCLRExtensionTool.ENABLE_OBFUZ))
            {
                ArrayUtility.Add(ref defines, HybridCLRExtensionTool.ENABLE_OBFUZ);
                PlayerSettings.SetScriptingDefineSymbols(buildTarget, defines);
            }
        }

        private static void RefreshPlayerSettings()
        {
#if ENABLE_HYBRIDCLR
            NamedBuildTarget target = NamedBuildTarget.FromBuildTargetGroup(EditorUserBuildSettings.selectedBuildTargetGroup);
            PlayerSettings.SetScriptingBackend(target, ScriptingImplementation.IL2CPP);
            PlayerSettings.SetApiCompatibilityLevel(target, ApiCompatibilityLevel.NET_Unity_4_8);
#endif
        }

        private static void RefreshAssemblyDefinition(bool disableHybridCLR)
        {
            string builtinFile = UtilityBuiltin.AssetsPath.GetCombinePath(ConstEditor.ProjectRootPath, ConstEditor.BuiltinAssembly);
            try
            {
                string textData = File.ReadAllText(builtinFile, Encoding.UTF8);
                JObject jsonData = UtilityBuiltin.Json.ToObject<JObject>(textData);
                JArray refAssemblies = jsonData["references"] as JArray;

                if (refAssemblies == null || refAssemblies.Count == 0)
                {
                    Environment.SetEnvironmentVariable("UNITY_IL2CPP_PATH", disableHybridCLR ? string.Empty : HybridCLR.Editor.SettingsUtil.LocalIl2CppDir);
                    return;
                }

                string firstReference = refAssemblies[0].Value<string>();
                if (!string.IsNullOrEmpty(firstReference) && firstReference.StartsWith("GUID:", StringComparison.Ordinal))
                {
                    EditorUtility.DisplayDialog("Error", Utility.Text.Format("解析Assembly Definition文件{0}失败: 请将其Use GUIDs设置为false后重试!", ConstEditor.BuiltinAssembly), "OK");
                    return;
                }

                Environment.SetEnvironmentVariable("UNITY_IL2CPP_PATH", disableHybridCLR ? string.Empty : HybridCLR.Editor.SettingsUtil.LocalIl2CppDir);
                if (disableHybridCLR)
                {
                    bool changed = false;
                    for (int i = refAssemblies.Count - 1; i >= 0; i--)
                    {
                        if (string.Equals(refAssemblies[i].Value<string>(), "HybridCLR.Runtime", StringComparison.Ordinal))
                        {
                            refAssemblies.RemoveAt(i);
                            changed = true;
                            break;
                        }
                    }

                    if (changed)
                    {
                        File.WriteAllText(builtinFile, jsonData.ToString(Formatting.Indented), new UTF8Encoding(false));
                        AssetDatabase.Refresh();
                    }

                    return;
                }

                bool hasValue = false;
                for (int i = refAssemblies.Count - 1; i >= 0; i--)
                {
                    if (string.Equals(refAssemblies[i].Value<string>(), "HybridCLR.Runtime", StringComparison.Ordinal))
                    {
                        hasValue = true;
                        break;
                    }
                }

                if (!hasValue)
                {
                    refAssemblies.Add("HybridCLR.Runtime");
                    File.WriteAllText(builtinFile, jsonData.ToString(Formatting.Indented), new UTF8Encoding(false));
                    AssetDatabase.Refresh();
                }
            }
            catch (Exception exception)
            {
                throw new BuildFailedException($"刷新程序集定义失败: {builtinFile}, Error: {exception.Message}");
            }
        }

        private static NamedBuildTarget GetCurrentNamedBuildTarget()
        {
#if UNITY_ANDROID
            return NamedBuildTarget.Android;
#elif UNITY_IOS
            return NamedBuildTarget.iOS;
#elif UNITY_STANDALONE
            return NamedBuildTarget.Standalone;
#elif UNITY_WEBGL
            return NamedBuildTarget.WebGL;
#else
            return NamedBuildTarget.Unknown;
#endif
        }
    }
}
