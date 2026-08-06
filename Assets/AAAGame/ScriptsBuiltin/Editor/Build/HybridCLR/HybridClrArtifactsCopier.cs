using GameFramework;
using HybridCLR.Editor.AOT;
using HybridCLR.Editor.Commands;
using Obfuz4HybridCLR;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace UGF.EditorTools
{
    internal static class HybridClrArtifactsCopier
    {
        internal static void CompileTargetDll(bool copyAotDlls, BuildTarget activeTarget)
        {
            CompileDllCommand.CompileDll(activeTarget);
            if (AppBuilderEditorSettings.Instance.EnableObfuz)
            {
                ObfuscateUtil.ObfuscateHotUpdateAssemblies(activeTarget, GetObfuzDllsDir(activeTarget));
            }

            string desDir = UtilityBuiltin.AssetsPath.GetCombinePath(Application.dataPath, ConstBuiltin.HOT_FIX_DLL_DIR);
            Directory.CreateDirectory(desDir);
            string[] failList = CopyHotfixDllTo(activeTarget, desDir, copyAotDlls);
            if (failList.Length <= 0)
            {
                return;
            }

            StringBuilder content = new StringBuilder();
            content.AppendLine("Error! Missing file:");
            foreach (string item in failList)
            {
                content.AppendLine(item);
            }

            string errorMessage = content.ToString();
            if (!Application.isBatchMode)
            {
                EditorUtility.DisplayDialog("CompileDll And Copy", errorMessage, "OK");
            }

            throw new BuildFailedException(errorMessage);
        }

        internal static string[] CopyHotfixDllTo(BuildTarget target, string desDir, bool copyAotMeta = true)
        {
            try
            {
                List<string> failList = new List<string>();
                Directory.CreateDirectory(desDir);
                string hotfixDllSrcDir = HybridCLR.Editor.SettingsUtil.GetHotUpdateDllsOutputDirByTarget(target);
                string obfuzDllSrcDir = GetObfuzDllsDir(target);
                var obfuzDllList = Obfuz.Settings.ObfuzSettings.Instance.assemblySettings.GetObfuscationRelativeAssemblyNames();
                foreach (string dll in HybridCLR.Editor.SettingsUtil.HotUpdateAssemblyNamesIncludePreserved)
                {
                    bool isObfuzDll = AppBuilderEditorSettings.Instance.EnableObfuz && obfuzDllList.Contains(dll);
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
                    failList.AddRange(CopyAotDllsToProject(target));
                }

                string hotfixListFile = UtilityBuiltin.AssetsPath.GetCombinePath(Application.dataPath, ConstBuiltin.HOT_FIX_DLL_DIR, "HotfixFileList.txt");
                File.WriteAllText(hotfixListFile, UtilityBuiltin.Json.ToJson(HybridCLR.Editor.SettingsUtil.HotUpdateAssemblyNamesIncludePreserved), new UTF8Encoding(false));
                AssetDatabase.Refresh();
                return failList.ToArray();
            }
            catch (Exception exception)
            {
                if (Directory.Exists(desDir))
                {
                    Directory.Delete(desDir, true);
                }

                throw new BuildFailedException($"拷贝热更 DLL 失败: {exception.Message}");
            }
        }

        internal static string[] CopyAotDllsToProject(BuildTarget target)
        {
            string aotSaveDir = UtilityBuiltin.AssetsPath.GetCombinePath(Application.dataPath, "Resources", ConstBuiltin.AOT_DLL_DIR);
            try
            {
                List<string> failList = new List<string>();
                string aotDllDir = HybridCLR.Editor.SettingsUtil.GetAssembliesPostIl2CppStripDir(target);
                if (Directory.Exists(aotSaveDir))
                {
                    Directory.Delete(aotSaveDir, true);
                }

                Directory.CreateDirectory(aotSaveDir);
                byte[] aotDllEncryptCode = Encoding.UTF8.GetBytes(ConstBuiltin.AOT_DLLS_KEY);
                foreach (string dll in HybridCLR.Editor.SettingsUtil.AOTAssemblyNames)
                {
                    string dllPath = UtilityBuiltin.AssetsPath.GetCombinePath(aotDllDir, dll + ".dll");
                    if (!File.Exists(dllPath))
                    {
                        Debug.LogWarning($"拷贝AOT元数据补充dll:{dllPath} 时发生错误,文件不存在。裁剪后的AOT dll在BuildPlayer时才能生成，因此需要你先构建一次游戏App后再打包。");
                        failList.Add(dllPath);
                        continue;
                    }

                    string dllBytesPath = UtilityBuiltin.AssetsPath.GetCombinePath(aotSaveDir, Utility.Text.Format("{0}.bytes", dll));
                    byte[] dllBytes = File.ReadAllBytes(dllPath);
                    dllBytes = AOTAssemblyMetadataStripper.Strip(dllBytes);
                    if (AppSettings.Instance.EncryptAOTDlls != null && Array.Exists(AppSettings.Instance.EncryptAOTDlls, item => string.Equals(item, dll, StringComparison.Ordinal)))
                    {
                        Utility.Encryption.GetQuickSelfXorBytes(dllBytes, aotDllEncryptCode);
                    }

                    File.WriteAllBytes(dllBytesPath, dllBytes);
                }

                AssetDatabase.Refresh();
                return failList.ToArray();
            }
            catch (Exception exception)
            {
                if (Directory.Exists(aotSaveDir))
                {
                    Directory.Delete(aotSaveDir, true);
                }

                throw new BuildFailedException($"拷贝 AOT DLL 失败: {exception.Message}");
            }
        }

        internal static string[] CopyNetFrameworkDllToProject(string[] aotDlls)
        {
            string aotSaveDir = UtilityBuiltin.AssetsPath.GetCombinePath(Application.dataPath, "Netstandard2NetFramework");
            try
            {
                List<string> failList = new List<string>();
                string aotDllDir = HybridCLR.Editor.SettingsUtil.GetAssembliesPostIl2CppStripDir(EditorUserBuildSettings.activeBuildTarget);
                if (Directory.Exists(aotSaveDir))
                {
                    Directory.Delete(aotSaveDir, true);
                }

                Directory.CreateDirectory(aotSaveDir);
                foreach (string dll in aotDlls)
                {
                    string dllFileName = dll.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) ? dll : dll + ".dll";
                    string dllPath = UtilityBuiltin.AssetsPath.GetCombinePath(aotDllDir, dllFileName);
                    if (!File.Exists(dllPath))
                    {
                        Debug.LogWarning($"dll文件不存在:{dllPath},需要先打包生成AOT dll");
                        failList.Add(dllPath);
                        continue;
                    }

                    string dllBytesPath = UtilityBuiltin.AssetsPath.GetCombinePath(aotSaveDir, dllFileName);
                    File.Copy(dllPath, dllBytesPath, true);
                }

                return failList.ToArray();
            }
            catch (Exception exception)
            {
                if (Directory.Exists(aotSaveDir))
                {
                    Directory.Delete(aotSaveDir, true);
                }

                throw new BuildFailedException($"拷贝 NetFramework DLL 失败: {exception.Message}");
            }
        }

        internal static string GetStripAssembliesDir(BuildTarget target)
        {
            string projectDir = ConstEditor.ProjectRootPath;
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
                default:
                    return string.Empty;
            }
        }

        private static string GetObfuzDllsDir(BuildTarget activeTarget)
        {
            return PrebuildCommandExt.GetObfuscatedHotUpdateAssemblyOutputPath(activeTarget);
        }
    }
}
