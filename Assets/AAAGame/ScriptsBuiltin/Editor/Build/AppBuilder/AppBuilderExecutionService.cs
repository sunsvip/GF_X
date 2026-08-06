using GameFramework;
using GameFramework.Resource;
using HybridCLR.Editor.AOT;
using HybridCLR.Editor.Commands;
using HybridCLR.Editor.Installer;
using Obfuz.Unity;
using Obfuz4HybridCLR;
using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityGameFramework.Editor.ResourceTools;

namespace UGF.EditorTools
{
    internal static class AppBuilderExecutionService
    {
        private static readonly string[] KeystoreExtensions = { ".keystore", ".jks", ".ks" };

        public static void ExecuteJenkinsResourceBuild(JenkinsBuildResourceConfig config)
        {
            Debug.Log("###########Build Resources: Init configs##########");
            Debug.Log(UtilityBuiltin.Json.ToJson(config));
            Debug.Log("#########################################");
            if (!GameFrameworkBuildPlatformUtility.TryGetPlatform(config.Platform, out var platform))
            {
                Debug.LogError($"#############Build Resources failed, Unsupport platform:{config.Platform}############");
                return;
            }

            var controller = CreateResourceBuilderController();
            JenkinsBuildConfigApplier.ApplyResourceBuildConfig(controller, config, platform);
            DuplicateAssetResolver.RefreshResourceRule();
            controller.OutputPackedSelected = AppSettings.Instance.ResourceMode != ResourceMode.Package && HasPackedResource();
            DuplicateAssetResolver.AutoResolveAbDuplicateAssets(false, controller.OutputPackedSelected);
            BuildResources(controller, saveConfiguration: true);
        }

        public static void ExecuteJenkinsAppBuild(JenkinsBuildAppConfig config)
        {
            Debug.Log("###########Build App: Init configs##########");
            Debug.Log(UtilityBuiltin.Json.ToJson(config));
            Debug.Log("#########################################");
            if (!GameFrameworkBuildPlatformUtility.TryGetPlatform(config.Platform, out var platform))
            {
                Debug.LogError($"#############Build App failed, Unsupport platform:{config.Platform}############");
                return;
            }

            JenkinsBuildConfigApplier.ApplyAppBuildConfig(config);
            Debug.Log("#############AppBuildSettings############");
            Debug.Log(UtilityBuiltin.Json.ToJson(AppBuilderEditorSettings.Instance));
            Debug.Log("#########################################");

            var controller = CreateResourceBuilderController();
            controller.Platforms = platform;
            if (BuildApp(controller, config.FullBuild))
            {
                BuildPlayerLaunchService.ExecutePendingBuild(HandlePostprocessBuild);
            }
        }

        private static ResourceBuilderController CreateResourceBuilderController()
        {
            var controller = new ResourceBuilderController();
            if (controller.Load())
            {
                controller.RefreshCompressionHelper();
                controller.RefreshBuildEventHandler();
            }
            else
            {
                Debug.LogWarning("Load resource builder configuration failure.");
            }

            controller.OutputDirectory = Path.GetFullPath(AppBuilderEditorSettings.Instance.ResourceBuildDir, ConstEditor.ProjectRootPath);
            if (string.IsNullOrWhiteSpace(controller.OutputDirectory) || !Directory.Exists(controller.OutputDirectory))
            {
                controller.OutputDirectory = ConstEditor.AssetBundleOutputPath;
            }

            if (AppSettings.Instance.ResourceMode != ResourceMode.Unspecified)
            {
                ApplyResourceMode(controller, AppSettings.Instance.ResourceMode);
            }

            return controller;
        }

        private static void ApplyResourceMode(ResourceBuilderController controller, ResourceMode mode)
        {
            controller.OutputPackageSelected = false;
            controller.OutputFullSelected = false;
            controller.OutputPackedSelected = false;
            switch (mode)
            {
                case ResourceMode.Package:
                    controller.OutputPackageSelected = true;
                    break;

                case ResourceMode.Updatable:
                case ResourceMode.UpdatableWhilePlaying:
                    controller.OutputFullSelected = true;
                    break;
            }
        }

        internal static void PrepareHotfixBuild(ResourceBuilderController controller)
        {
            DuplicateAssetResolver.RefreshResourceRule();
            controller.OutputPackedSelected = AppSettings.Instance.ResourceMode != ResourceMode.Package && HasPackedResource();
            DuplicateAssetResolver.AutoResolveAbDuplicateAssets(false, controller.OutputPackedSelected);
        }

        internal static void BuildResources(ResourceBuilderController controller, bool saveConfiguration)
        {
            if (controller.BuildResources())
            {
                Debug.Log("Build resources success.");
                if (saveConfiguration)
                {
                    SaveConfiguration(controller);
                }
            }
            else
            {
                Debug.LogWarning("Build resources failure.");
            }
        }

        internal static bool IsKeystoreAvailable(string keystore)
        {
            if (string.IsNullOrWhiteSpace(keystore))
            {
                return false;
            }

            var ext = Path.GetExtension(keystore);
            return File.Exists(keystore) && Array.Exists(KeystoreExtensions, item => string.Equals(item, ext, StringComparison.OrdinalIgnoreCase));
        }

        internal static bool BuildApp(ResourceBuilderController controller, bool generateAot)
        {
            DuplicateAssetResolver.RefreshResourceRule();
            controller.OutputPackedSelected = AppSettings.Instance.ResourceMode != ResourceMode.Package && HasPackedResource();
            if (controller.OutputPackageSelected)
            {
                DuplicateAssetResolver.AutoResolveAbDuplicateAssets();
                if (!controller.BuildResources())
                {
                    return false;
                }

                Debug.Log("########## Build Resources Success ###########");
                DeleteAotDlls();
                PrepareBuildApp(generateAot);
                return true;
            }

            StreamingAssetsBundleCleaner.RemoveStreamingAssetsBundles();
            var buildAppReady = !controller.OutputPackedSelected;
            if (controller.OutputPackedSelected)
            {
#if ENABLE_HYBRIDCLR
                HybridCLRExtensionTool.CompileTargetDll(false);
#endif
                DuplicateAssetResolver.AutoResolveAbDuplicateAssets(false, true);
                buildAppReady = controller.BuildResources();
            }

            if (!buildAppReady)
            {
                return false;
            }

            PrepareBuildApp(generateAot);
            return true;
        }

        private static void DeleteAotDlls()
        {
            string aotSaveDir = UtilityBuiltin.AssetsPath.GetCombinePath("Assets", "Resources", ConstBuiltin.AOT_DLL_DIR);
            if (Directory.Exists(aotSaveDir))
            {
                AssetDatabase.DeleteAsset(aotSaveDir);
            }
        }

        private static void PrepareBuildApp(bool generateAotDll)
        {
#if ENABLE_HYBRIDCLR
            GenerateHotfixCodeStripConfig(false);
            HybridClrGenerateAll(generateAotDll);
#else
            GenerateHotfixCodeStripConfig(true);
#endif
            AssetDatabase.Refresh();
            BuildPlayerLaunchService.SchedulePendingBuild();
        }

        private static void GenerateHotfixCodeStripConfig(bool includeHotfixAssemblies)
        {
            var linkDir = Path.GetDirectoryName(ConstEditor.HotfixAssembly);
            var linkFile = UtilityBuiltin.AssetsPath.GetCombinePath(linkDir, "link.xml");
            if (includeHotfixAssemblies)
            {
                var builder = new StringBuilder();
                builder.AppendLine("<linker>");
                foreach (var dllName in HybridCLR.Editor.SettingsUtil.HotUpdateAssemblyNamesIncludePreserved)
                {
                    builder.AppendLine(Utility.Text.Format("\t<assembly fullname=\"{0}\" preserve=\"all\" />", dllName));
                }
                builder.AppendLine("</linker>");
                File.WriteAllText(linkFile, builder.ToString(), new UTF8Encoding(false));
                return;
            }

            if (File.Exists(linkFile))
            {
                File.Delete(linkFile);
            }
        }

        private static void HybridClrGenerateAll(bool generateAotDll)
        {
            var installer = new InstallerController();
            if (!installer.HasInstalledHybridCLR())
            {
                throw new UnityEditor.Build.BuildFailedException("You have not initialized HybridCLR, please install it via menu 'HybridCLR/Installer'");
            }

            if (AppBuilderEditorSettings.Instance.EnableObfuz)
            {
                ObfuzMenu.GenerateEncryptionVM();
                ObfuzMenu.SaveSecretFile();
            }

            var target = EditorUserBuildSettings.activeBuildTarget;
#if ENABLE_OBFUZ
            CompileDllCommand.CompileDll(target);
            Il2CppDefGeneratorCommand.GenerateIl2CppDef();
            LinkGeneratorCommand.GenerateLinkXml(target);
            if (generateAotDll)
            {
                StripAOTDllCommand.GenerateStripedAOTDlls(target);
                AOTReferenceGeneratorCommand.GenerateAOTGenericReference(target);
            }

            string obfuscatedHotUpdateDllPath = Obfuz4HybridCLR.PrebuildCommandExt.GetObfuscatedHotUpdateAssemblyOutputPath(target);
            Obfuz4HybridCLR.ObfuscateUtil.ObfuscateHotUpdateAssemblies(target, obfuscatedHotUpdateDllPath);
            Obfuz4HybridCLR.PrebuildCommandExt.GenerateMethodBridgeAndReversePInvokeWrapper(target, obfuscatedHotUpdateDllPath);
            HybridCLRExtensionTool.CopyAotDllsToProject(target);
#else
            HybridCLRExtensionTool.CompileTargetDll(false);
            Il2CppDefGeneratorCommand.GenerateIl2CppDef();
            LinkGeneratorCommand.GenerateLinkXml(target);
            if (generateAotDll)
            {
                StripAOTDllCommand.GenerateStripedAOTDlls(target);
                AOTReferenceGeneratorCommand.GenerateAOTGenericReference(target);
            }

            MethodBridgeGeneratorCommand.GenerateMethodBridgeAndReversePInvokeWrapper(target);
            HybridCLRExtensionTool.CopyAotDllsToProject(target);
#endif
        }

        internal static void SaveConfiguration(ResourceBuilderController controller)
        {
            EditorUtility.SetDirty(AppSettings.Instance);
            AppBuilderEditorSettings.Save();
            Obfuz.Settings.ObfuzSettings.Save();
            AssetDatabase.SaveAssets();
            if (controller.Save())
            {
                Debug.Log("Save configuration success.");
            }
            else
            {
                Debug.LogWarning("Save configuration failure.");
            }
        }

        internal static void HandlePostprocessBuild(BuildReport report)
        {
            if (report.summary.result != BuildResult.Succeeded)
            {
                Debug.LogError("Build App Failed:" + report.summary.result);
                return;
            }

            RenameApp(report);
        }

        private static void RenameApp(BuildReport report)
        {
            if (report.summary.result != BuildResult.Succeeded)
            {
                return;
            }

            if (report.summary.platform != BuildTarget.Android)
            {
                return;
            }

            var appFile = report.summary.outputPath;
            if (!File.Exists(appFile))
            {
                return;
            }

            var dir = Path.GetDirectoryName(appFile);
            if (string.IsNullOrWhiteSpace(dir))
            {
                Debug.LogError($"Rename build app failed: invalid output directory. Path: {appFile}");
                return;
            }

            var name = Path.GetFileNameWithoutExtension(appFile);
            var ext = Path.GetExtension(appFile);
            var finalName = Utility.Text.Format(
                "{0}_{1}{2}_v{3}{4}",
                name,
                AppSettings.Instance.DebugMode ? "debug" : "release",
                EditorUserBuildSettings.development ? "Dev" : string.Empty,
                Application.version,
                ext);
            finalName = Path.Combine(dir, finalName);

            try
            {
                if (File.Exists(finalName))
                {
                    File.Delete(finalName);
                }

                File.Move(report.summary.outputPath, finalName);
            }
            catch (Exception exception)
            {
                Debug.LogError($"Rename build app failed: {exception.Message}, Source: {report.summary.outputPath}, Target: {finalName}");
            }
        }

        private static bool HasPackedResource()
        {
            var configPath = Utility.Path.GetRegularPath(Path.Combine(Application.dataPath, "GameFramework/Configs/ResourceCollection.xml"));
            var ugfEditorType = Utility.Assembly.GetType("UnityGameFramework.Editor.Type");
            var getConfigPathMethod = ugfEditorType?.GetMethod("GetConfigurationPath", BindingFlags.Static | BindingFlags.NonPublic);
            if (getConfigPathMethod != null)
            {
                var genericGetConfigPathMethod = getConfigPathMethod.MakeGenericMethod(typeof(ResourceCollectionConfigPathAttribute));
                configPath = genericGetConfigPathMethod.Invoke(null, null) as string ?? configPath;
            }

            if (!File.Exists(configPath))
            {
                return false;
            }

            try
            {
                var xmlDocument = new XmlDocument();
                xmlDocument.Load(configPath);
                XmlNode xmlRoot = xmlDocument.SelectSingleNode("UnityGameFramework");
                if (xmlRoot == null)
                {
                    return false;
                }

                XmlNode xmlCollection = xmlRoot.SelectSingleNode("ResourceCollection");
                if (xmlCollection == null)
                {
                    return false;
                }

                XmlNode xmlResources = xmlCollection.SelectSingleNode("Resources");
                if (xmlResources == null)
                {
                    return false;
                }

                var xmlNodeList = xmlResources.ChildNodes;
                foreach (XmlNode childNode in xmlNodeList)
                {
                    if (!string.Equals(childNode.Name, "Resource", StringComparison.Ordinal))
                    {
                        continue;
                    }

                    var packedAttr = childNode.Attributes?.GetNamedItem("Packed");
                    if (packedAttr != null && bool.TryParse(packedAttr.Value, out var packed) && packed)
                    {
                        return true;
                    }
                }
            }
            catch (Exception exception)
            {
                Debug.LogError($"Check packed resource failed: {exception.Message}");
            }

            return false;
        }
    }
}
