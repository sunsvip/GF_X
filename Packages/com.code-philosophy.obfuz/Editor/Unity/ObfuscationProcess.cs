using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor.Build;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEditor.Compilation;
using Obfuz.Utils;
using FileUtil = Obfuz.Utils.FileUtil;
using Obfuz.Settings;
using dnlib.DotNet;

namespace Obfuz.Unity
{

#if UNITY_2019_1_OR_NEWER
    public class ObfuscationProcess : IPostBuildPlayerScriptDLLs
    {
        public int callbackOrder => 10000;

        public static event Action<ObfuscationBeginEventArgs> OnObfuscationBegin;

        public static event Action<ObfuscationEndEventArgs> OnObfuscationEnd;

        public void OnPostBuildPlayerScriptDLLs(BuildReport report)
        {
#if !UNITY_2022_1_OR_NEWER
            RunObfuscate(report.files);
#else
            RunObfuscate(report.GetFiles());
#endif
        }

        private static void BackupOriginalDlls(string srcDir, string dstDir, HashSet<string> dllNames)
        {
            FileUtil.RecreateDir(dstDir);
            foreach (string dllName in dllNames)
            {
                string srcFile = Path.Combine(srcDir, dllName + ".dll");
                string dstFile = Path.Combine(dstDir, dllName + ".dll");
                if (File.Exists(srcFile))
                {
                    File.Copy(srcFile, dstFile, true);
                    Debug.Log($"BackupOriginalDll {srcFile} -> {dstFile}");
                }
            }
        }

        public static void ValidateReferences(string stagingAreaTempManagedDllDir, HashSet<string> assembliesToObfuscated, HashSet<string> obfuscationRelativeAssemblyNames)
        {
            var modCtx = ModuleDef.CreateModuleContext();
            var asmResolver = (AssemblyResolver)modCtx.AssemblyResolver;

            foreach (string assFile in Directory.GetFiles(stagingAreaTempManagedDllDir, "*.dll", SearchOption.AllDirectories))
            {
                ModuleDefMD mod = ModuleDefMD.Load(File.ReadAllBytes(assFile), modCtx);
                string modName = mod.Assembly.Name;
                foreach (AssemblyRef assRef in mod.GetAssemblyRefs())
                {
                    string refAssName = assRef.Name;
                    if (assembliesToObfuscated.Contains(refAssName) && !obfuscationRelativeAssemblyNames.Contains(modName))
                    {
                        throw new BuildFailedException($"assembly:{modName} references to obfuscated assembly:{refAssName}, but it's not been added to ObfuzSettings.AssemblySettings.NonObfuscatedButReferencingObfuscatedAssemblies.");
                    }
                }
                mod.Dispose();
            }
        }

        private static void RunObfuscate(BuildFile[] files)
        {
            ObfuzSettings settings = ObfuzSettings.Instance;
            if (!settings.enable)
            {
                Debug.Log("Obfuscation is disabled.");
                return;
            }

            Debug.Log("Obfuscation begin...");
            var buildTarget = EditorUserBuildSettings.activeBuildTarget;

            var obfuscationRelativeAssemblyNames = new HashSet<string>(settings.assemblySettings.GetObfuscationRelativeAssemblyNames());
            string stagingAreaTempManagedDllDir = Path.GetDirectoryName(files.First(file => file.path.EndsWith(".dll")).path);
            string backupPlayerScriptAssembliesPath = settings.GetOriginalAssemblyBackupDir(buildTarget);
            BackupOriginalDlls(stagingAreaTempManagedDllDir, backupPlayerScriptAssembliesPath, obfuscationRelativeAssemblyNames);

            string applicationContentsPath = EditorApplication.applicationContentsPath;

            var obfuscatorBuilder = ObfuscatorBuilder.FromObfuzSettings(settings, buildTarget, false);

            var assemblySearchDirs = new List<string>
                {
                   stagingAreaTempManagedDllDir,
                };
            obfuscatorBuilder.InsertTopPriorityAssemblySearchPaths(assemblySearchDirs);

            ValidateReferences(stagingAreaTempManagedDllDir, new HashSet<string>(obfuscatorBuilder.CoreSettingsFacade.assembliesToObfuscate), obfuscationRelativeAssemblyNames);


            OnObfuscationBegin?.Invoke(new ObfuscationBeginEventArgs
            {
                scriptAssembliesPath = stagingAreaTempManagedDllDir,
                obfuscatedScriptAssembliesPath = obfuscatorBuilder.CoreSettingsFacade.obfuscatedAssemblyOutputPath,
            });
            bool succ = false;

            try
            {
                Obfuscator obfuz = obfuscatorBuilder.Build();
                obfuz.Run();

                foreach (var dllName in obfuscationRelativeAssemblyNames)
                {
                    string src = $"{obfuscatorBuilder.CoreSettingsFacade.obfuscatedAssemblyOutputPath}/{dllName}.dll";
                    string dst = $"{stagingAreaTempManagedDllDir}/{dllName}.dll";

                    if (!File.Exists(src))
                    {
                        Debug.LogWarning($"obfuscation assembly not found! skip copy. path:{src}");
                        continue;
                    }
                    File.Copy(src, dst, true);
                    Debug.Log($"obfuscate dll:{dst}");
                }
                succ = true;
            }
            catch (Exception e)
            {
                succ = false;
                Debug.LogException(e);
                Debug.LogError($"Obfuscation failed.");
            }
            OnObfuscationEnd?.Invoke(new ObfuscationEndEventArgs
            {
                success = succ,
                originalScriptAssembliesPath = backupPlayerScriptAssembliesPath,
                obfuscatedScriptAssembliesPath = stagingAreaTempManagedDllDir,
            });

            Debug.Log("Obfuscation end.");
        }
    }
#endif
        }
