using GameFramework;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace UGF.EditorTools
{
    [Flags]
    public enum GameDataExcelFileType
    {
        MainFile = 1,
        ABTestFile = 2
    }

    public class GameDataGenerator
    {
        internal static readonly UTF8Encoding Utf8NoBom = new UTF8Encoding(false);

        [InitializeOnLoadMethod]
        private static void InitEPPlusLicense()
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        }

        [MenuItem("Game Framework/GameTools/Refresh All Excels【刷新所有数据表】", false, 1001)]
        public static void GenerateDataTables()
        {
            RefreshAllGameData();
        }

        public static void RefreshAllGameData()
        {
            if (!ValidateGameDataPaths())
            {
                UnityEngine.Debug.LogError("GameData export stopped because path validation failed.");
                return;
            }

            RunAssetBatch(() =>
            {
                RefreshAllDataTable();
                RefreshAllConfig();
                RefreshAllLanguage();
                GenerateUIFormNamesScript();
                GenerateGroupEnumScript();
                CleanGeneratedGameData();
            });
        }

        public static bool CreateGameConfigExcel(string excelPath)
        {
            return GameDataExcelTemplateService.CreateGameConfigExcel(excelPath);
        }

        public static bool CreateDataTableExcel(string excelPath)
        {
            return GameDataExcelTemplateService.CreateDataTableExcel(excelPath);
        }

        public static void GenerateGroupEnumScript()
        {
            RunAssetBatch(GameDataScriptGenerationService.GenerateGroupEnumScript);
        }

        public static void GenerateUIFormNamesScript()
        {
            RunAssetBatch(GameDataScriptGenerationService.GenerateUIFormNamesScript);
        }

        public static bool ExportLanguageExcel(string excelFile, string outJsonFile, bool useBytes)
        {
            return GameDataExportService.ExportLanguageExcel(excelFile, outJsonFile, useBytes);
        }

        public static bool Excel2TxtFile(string excelFileName, string outTxtFile, bool normalizeCustomJsonColumns = true)
        {
            return GameDataExportService.Excel2TxtFile(excelFileName, outTxtFile, normalizeCustomJsonColumns);
        }

        public static void RefreshAllLanguage(IList<string> files = null)
        {
            RunAssetBatch(() => GameDataExportService.RefreshAllLanguage(files));
        }

        public static void RefreshAllConfig(IList<string> files = null)
        {
            RunAssetBatch(() => GameDataExportService.RefreshAllConfig(files));
        }

        public static void RefreshAllDataTable(IList<string> fullPathFiles = null)
        {
            RunAssetBatch(() => GameDataExportService.RefreshAllDataTable(fullPathFiles));
        }

        public static bool ValidateGameDataPaths()
        {
            bool valid = true;
            valid &= ValidateDirectory(ConstEditor.DataTableExcelPath, "DataTable Excel Root");
            valid &= ValidateDirectory(ConstEditor.ConfigExcelPath, "Config Excel Root");
            valid &= ValidateDirectory(ConstEditor.LanguageExcelPath, "Language Excel Root");

            foreach (string frameworkTable in ConstEditor.FrameworkRequiredDataTables)
            {
                string excelFile = GameDataPathService.GameDataExcelRelative2FullPath(GameDataType.DataTable, frameworkTable);
                if (!File.Exists(excelFile))
                {
                    Debug.LogError(Utility.Text.Format("Framework required data table is missing: {0}", excelFile));
                    valid = false;
                }
            }

            EnsureFileDirectory(UtilityBuiltin.AssetsPath.GetCombinePath(ConstEditor.DataTablePath, ".keep"));
            EnsureFileDirectory(UtilityBuiltin.AssetsPath.GetCombinePath(ConstEditor.GameConfigPath, ".keep"));
            EnsureFileDirectory(UtilityBuiltin.AssetsPath.GetCombinePath(ConstEditor.LanguagePath, ".keep"));
            return valid;
        }

        public static void CleanGeneratedGameData()
        {
            AppConfigs appConfig = AppConfigs.GetInstanceEditor();
            bool useBytes = appConfig.LoadFromBytes;

            CleanOppositeOutputs(GameDataType.DataTable, GameDataPathService.GetGameDataExcelWithABFiles(GameDataType.DataTable, GameDataPathService.GetConfiguredDataTableMainFiles(appConfig)), useBytes);
            CleanOppositeOutputs(GameDataType.Config, GameDataPathService.GetGameDataExcelWithABFiles(GameDataType.Config, GameDataPathService.GetConfiguredMainFiles(GameDataType.Config, appConfig.Configs)), useBytes);
            CleanOppositeOutputs(GameDataType.Language, GameDataPathService.GetGameDataExcelWithABFiles(GameDataType.Language, GameDataPathService.GetConfiguredMainFiles(GameDataType.Language, appConfig.Languages)), useBytes);
        }

        public static void CleanGeneratedGameDataForMissingExcels(GameDataType type, IEnumerable<string> excelFiles)
        {
            if (excelFiles == null)
            {
                return;
            }

            foreach (string excelFile in excelFiles)
            {
                if (string.IsNullOrWhiteSpace(excelFile) || File.Exists(excelFile))
                {
                    continue;
                }

                CleanGeneratedOutputsForExcel(type, excelFile);
            }
        }

        internal static void RunAssetBatch(Action action)
        {
            if (s_AssetEditing)
            {
                action();
                return;
            }

            var assetEditingStarted = false;
            s_AssetEditing = true;
            try
            {
                AssetDatabase.StartAssetEditing();
                assetEditingStarted = true;
                action();
            }
            finally
            {
                try
                {
                    if (assetEditingStarted)
                    {
                        AssetDatabase.StopAssetEditing();
                    }
                }
                finally
                {
                    s_AssetEditing = false;
                    AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                }
            }
        }

        private static bool s_AssetEditing;

        internal static void EnsureFileDirectory(string fileName)
        {
            string directoryName = Path.GetDirectoryName(fileName);
            if (!string.IsNullOrWhiteSpace(directoryName) && !Directory.Exists(directoryName))
            {
                Directory.CreateDirectory(directoryName);
            }
        }

        internal static void WriteTextFile(string fileName, string content, Encoding encoding)
        {
            EnsureFileDirectory(fileName);
            File.WriteAllText(fileName, content, encoding);
        }

        internal static void DeleteGeneratedFile(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return;
            }

            string regularPath = UtilityBuiltin.AssetsPath.GetCombinePath(fileName);
            if (regularPath.StartsWith("Assets/", StringComparison.Ordinal))
            {
                if (File.Exists(regularPath))
                {
                    AssetDatabase.DeleteAsset(regularPath);
                }
                else
                {
                    string assetMetaFile = regularPath + ".meta";
                    if (File.Exists(assetMetaFile))
                    {
                        File.Delete(assetMetaFile);
                    }
                }

                return;
            }

            if (File.Exists(regularPath))
            {
                File.Delete(regularPath);
            }

            string metaFile = regularPath + ".meta";
            if (File.Exists(metaFile))
            {
                File.Delete(metaFile);
            }
        }

        internal static void WriteStringPairsBytesFile(string bytesFileName, IEnumerable<KeyValuePair<string, string>> pairs)
        {
            EnsureFileDirectory(bytesFileName);
            using (var fileStream = new FileStream(bytesFileName, FileMode.Create, FileAccess.Write))
            using (var binaryWriter = new BinaryWriter(fileStream, Utf8NoBom))
            {
                foreach (var item in pairs)
                {
                    binaryWriter.Write(item.Key);
                    binaryWriter.Write(item.Value ?? string.Empty);
                }
            }
        }

        private static bool ValidateDirectory(string directoryName, string displayName)
        {
            if (Directory.Exists(directoryName))
            {
                return true;
            }

            Debug.LogError(Utility.Text.Format("{0} does not exist: {1}", displayName, directoryName));
            return false;
        }

        private static void CleanGeneratedOutputsForExcel(GameDataType type, string excelFile)
        {
            DeleteGeneratedFile(GameDataPathService.GetGameDataRuntimeOutputFile(type, excelFile, true));
            DeleteGeneratedFile(GameDataPathService.GetGameDataRuntimeOutputFile(type, excelFile, false));

            if (type == GameDataType.DataTable && !GameDataPathService.IsABTestFile(excelFile))
            {
                string dataTableName = GameDataPathService.GetGameDataExcelRelativePath(GameDataType.DataTable, excelFile);
                DeleteGeneratedFile(Utility.Path.GetRegularPath(Path.Combine(ConstEditor.DataTableCodePath, dataTableName + ".cs")));
                CleanABOutputsForMissingMain(type, excelFile);
            }
            else if (!GameDataPathService.IsABTestFile(excelFile))
            {
                CleanABOutputsForMissingMain(type, excelFile);
            }
        }

        private static void CleanABOutputsForMissingMain(GameDataType type, string mainExcelFile)
        {
            string mainRelativeName = GameDataPathService.GetGameDataExcelRelativePath(type, mainExcelFile);
            string outputRoot = GameDataPathService.GetGameDataExcelOutputDir(type);
            string filePrefix = Path.GetFileName(mainRelativeName) + ConstBuiltin.AB_TEST_TAG;
            string relativeDirectory = Path.GetDirectoryName(mainRelativeName);

            CleanGeneratedFilesWithPrefix(CombineDirectory(outputRoot, relativeDirectory), filePrefix);
            if (type == GameDataType.DataTable)
            {
                CleanGeneratedFilesWithPrefix(CombineDirectory(ConstEditor.DataTableCodePath, relativeDirectory), filePrefix);
            }
        }

        private static string CombineDirectory(string root, string relativeDirectory)
        {
            return string.IsNullOrWhiteSpace(relativeDirectory) ? root : UtilityBuiltin.AssetsPath.GetCombinePath(root, relativeDirectory);
        }

        private static void CleanGeneratedFilesWithPrefix(string directoryName, string filePrefix)
        {
            if (string.IsNullOrWhiteSpace(directoryName) || !Directory.Exists(directoryName))
            {
                return;
            }

            string[] files = Directory.GetFiles(directoryName, filePrefix + ".*", SearchOption.TopDirectoryOnly);
            foreach (string file in files)
            {
                if (file.EndsWith(".meta", StringComparison.Ordinal))
                {
                    continue;
                }

                DeleteGeneratedFile(file);
            }
        }

        private static void CleanOppositeOutputs(GameDataType type, IList<string> excelFiles, bool useBytes)
        {
            foreach (string excelFile in excelFiles)
            {
                DeleteGeneratedFile(GameDataPathService.GetGameDataRuntimeOutputFile(type, excelFile, !useBytes));
            }
        }

        internal static string GetGameDataRelativeName(string fileName, string relativePath)
        {
            return GameDataPathService.GetGameDataRelativeName(fileName, relativePath);
        }

        public static IList<string> GetGameDataExcelWithABFiles(GameDataType tp, IList<string> mainFiles)
        {
            return GameDataPathService.GetGameDataExcelWithABFiles(tp, mainFiles);
        }

        public static string GetGameDataExcelRelativePath(GameDataType tp, string excelFile)
        {
            return GameDataPathService.GetGameDataExcelRelativePath(tp, excelFile);
        }

        public static string[] GameDataExcelRelative2FullPath(GameDataType tp, string[] relativeExcelPathArr)
        {
            return GameDataPathService.GameDataExcelRelative2FullPath(tp, relativeExcelPathArr);
        }

        public static string GameDataExcelRelative2FullPath(GameDataType tp, string relativeExcelPath)
        {
            return GameDataPathService.GameDataExcelRelative2FullPath(tp, relativeExcelPath);
        }

        public static string GetGameDataRuntimeOutputFile(GameDataType tp, string excelFile, bool useBytes)
        {
            return GameDataPathService.GetGameDataRuntimeOutputFile(tp, excelFile, useBytes);
        }

        public static string GetGameDataExcelOutputDir(GameDataType tp)
        {
            return GameDataPathService.GetGameDataExcelOutputDir(tp);
        }

        public static string GetGameDataExcelDir(GameDataType tp)
        {
            return GameDataPathService.GetGameDataExcelDir(tp);
        }

        public static IList<string> GetAllGameDataExcels(GameDataType dtTp, GameDataExcelFileType tps, string mainExcelName = null)
        {
            return GameDataPathService.GetAllGameDataExcels(dtTp, tps, mainExcelName);
        }

        public static bool IsABTestFile(string excelFile)
        {
            return GameDataPathService.IsABTestFile(excelFile);
        }

        public static bool IsABTestFile(string excelFile, string mainExcelFile)
        {
            return GameDataPathService.IsABTestFile(excelFile, mainExcelFile);
        }
    }
}
