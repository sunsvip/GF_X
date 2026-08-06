using GameFramework;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace UGF.EditorTools
{
    internal static class GameDataPathService
    {
        internal static string GetGameDataRelativeName(string fileName, string relativePath)
        {
            string path = Path.GetRelativePath(relativePath, fileName);
            return UtilityBuiltin.AssetsPath.GetCombinePath(Path.GetDirectoryName(path), Path.GetFileNameWithoutExtension(path));
        }

        internal static IList<string> GetConfiguredDataTableMainFiles(AppConfigs appConfig)
        {
            List<string> result = new List<string>(ConstEditor.FrameworkRequiredDataTables.Length + (appConfig.DataTables?.Length ?? 0));
            AddDataTableMainFiles(result, ConstEditor.FrameworkRequiredDataTables);
            AddDataTableMainFiles(result, appConfig.DataTables);
            return result;
        }

        internal static IList<string> GetConfiguredMainFiles(GameDataType type, IEnumerable<string> relativeNames)
        {
            List<string> result = new List<string>();
            if (relativeNames == null)
            {
                return result;
            }

            foreach (string relativeName in relativeNames)
            {
                if (string.IsNullOrWhiteSpace(relativeName))
                {
                    continue;
                }

                string excelFullPath = GameDataExcelRelative2FullPath(type, relativeName);
                if (!result.Contains(excelFullPath))
                {
                    result.Add(excelFullPath);
                }
            }

            return result;
        }

        internal static IList<string> GetGameDataExcelWithABFiles(GameDataType tp, IList<string> mainFiles)
        {
            List<string> result = new List<string>();
            if (mainFiles == null)
            {
                return result;
            }

            foreach (string mainFile in mainFiles)
            {
                result.AddRange(GetGameDataExcelWithABFiles(tp, mainFile));
            }

            return result;
        }

        internal static string GetGameDataExcelRelativePath(GameDataType tp, string excelFile)
        {
            string excelRelativePath = Path.GetRelativePath(GetGameDataExcelDir(tp), excelFile);
            return UtilityBuiltin.AssetsPath.GetCombinePath(Path.GetDirectoryName(excelRelativePath), Path.GetFileNameWithoutExtension(excelRelativePath));
        }

        internal static string[] GameDataExcelRelative2FullPath(GameDataType tp, string[] relativeExcelPathArr)
        {
            string[] result = new string[relativeExcelPathArr.Length];
            for (int i = 0; i < relativeExcelPathArr.Length; i++)
            {
                result[i] = GameDataExcelRelative2FullPath(tp, relativeExcelPathArr[i]);
            }

            return result;
        }

        internal static string GameDataExcelRelative2FullPath(GameDataType tp, string relativeExcelPath)
        {
            return UtilityBuiltin.AssetsPath.GetCombinePath(GetGameDataExcelDir(tp), relativeExcelPath + ".xlsx");
        }

        internal static string GetGameDataRuntimeOutputFile(GameDataType tp, string excelFile, bool useBytes)
        {
            string excelRelativePath = GetGameDataExcelRelativePath(tp, excelFile);
            return UtilityBuiltin.AssetsPath.GetCombinePath(GetGameDataExcelOutputDir(tp), excelRelativePath + GetGameDataRuntimeOutputFileExtension(tp, useBytes));
        }

        internal static string GetGameDataExcelOutputDir(GameDataType tp)
        {
            return tp switch
            {
                GameDataType.DataTable => ConstEditor.DataTablePath,
                GameDataType.Config => ConstEditor.GameConfigPath,
                GameDataType.Language => ConstEditor.LanguagePath,
                _ => string.Empty,
            };
        }

        internal static string GetGameDataExcelDir(GameDataType tp)
        {
            return tp switch
            {
                GameDataType.DataTable => ConstEditor.DataTableExcelPath,
                GameDataType.Config => ConstEditor.ConfigExcelPath,
                GameDataType.Language => ConstEditor.LanguageExcelPath,
                _ => string.Empty,
            };
        }

        internal static IList<string> GetAllGameDataExcels(GameDataType dtTp, GameDataExcelFileType tps, string mainExcelName = null)
        {
            List<string> result = new List<string>();

            if (dtTp.HasFlag(GameDataType.DataTable))
            {
                result.AddRange(GetGameDataExcelAtDir(GetGameDataExcelDir(GameDataType.DataTable), tps, mainExcelName));
            }

            if (dtTp.HasFlag(GameDataType.Language))
            {
                result.AddRange(GetGameDataExcelAtDir(GetGameDataExcelDir(GameDataType.Language), tps, mainExcelName));
            }

            if (dtTp.HasFlag(GameDataType.Config))
            {
                result.AddRange(GetGameDataExcelAtDir(GetGameDataExcelDir(GameDataType.Config), tps, mainExcelName));
            }

            return result;
        }

        internal static bool IsABTestFile(string excelFile)
        {
            string fileName = Path.GetFileNameWithoutExtension(excelFile);
            int tagIndex = fileName.IndexOf(ConstBuiltin.AB_TEST_TAG, StringComparison.Ordinal);
            return tagIndex > 0 && tagIndex < fileName.Length - 1;
        }

        internal static bool IsABTestFile(string excelFile, string mainExcelFile)
        {
            string excelDirectory = Utility.Path.GetRegularPath(Path.GetDirectoryName(excelFile) ?? string.Empty);
            string mainDirectory = Utility.Path.GetRegularPath(Path.GetDirectoryName(mainExcelFile) ?? string.Empty);
            if (!string.Equals(excelDirectory, mainDirectory, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            string mainFileName = Path.GetFileNameWithoutExtension(mainExcelFile);
            string abFileName = Path.GetFileNameWithoutExtension(excelFile);
            string prefix = mainFileName + ConstBuiltin.AB_TEST_TAG;
            return abFileName.Length > prefix.Length && abFileName.StartsWith(prefix, StringComparison.Ordinal);
        }

        private static void AddDataTableMainFiles(List<string> result, IEnumerable<string> tableNames)
        {
            if (tableNames == null)
            {
                return;
            }

            foreach (string tableName in tableNames)
            {
                if (string.IsNullOrWhiteSpace(tableName))
                {
                    continue;
                }

                string tableExcelFullPath = GameDataExcelRelative2FullPath(GameDataType.DataTable, tableName);
                if (!result.Contains(tableExcelFullPath))
                {
                    result.Add(tableExcelFullPath);
                }
            }
        }

        private static IList<string> GetGameDataExcelWithABFiles(GameDataType tp, string mainExcelFile)
        {
            List<string> result = new List<string> { mainExcelFile };
            string excelName = Path.GetFileNameWithoutExtension(mainExcelFile);
            IList<string> allAbFiles = GetAllGameDataExcels(tp, GameDataExcelFileType.ABTestFile, excelName);
            foreach (string item in allAbFiles)
            {
                if (IsABTestFile(item, mainExcelFile))
                {
                    result.Add(item);
                }
            }

            return result;
        }

        private static string GetGameDataRuntimeOutputFileExtension(GameDataType tp, bool useBytes)
        {
            return useBytes ? ".bytes" : GetGameDataExcelOutputFileExtension(tp);
        }

        private static string GetGameDataExcelOutputFileExtension(GameDataType tp)
        {
            return tp switch
            {
                GameDataType.DataTable => ".txt",
                GameDataType.Config => ".txt",
                GameDataType.Language => ".json",
                _ => string.Empty,
            };
        }

        private static IList<string> GetGameDataExcelAtDir(string excelDir, GameDataExcelFileType tps, string mainExcelName)
        {
            List<string> result = new List<string>();
            if (string.IsNullOrWhiteSpace(excelDir) || !Directory.Exists(excelDir))
            {
                Debug.LogWarning($"获取GameData Excel失败, 给定路径为空或不存在:{excelDir}");
                return result;
            }

            IList<string> excelFiles = GetFiles(excelDir, "*.xlsx", SearchOption.AllDirectories, mainExcelName);
            foreach (string item in excelFiles)
            {
                bool isABFile = IsABTestFile(item);
                if (tps.HasFlag(GameDataExcelFileType.MainFile) && !isABFile)
                {
                    result.Add(item);
                }

                if (tps.HasFlag(GameDataExcelFileType.ABTestFile) && isABFile)
                {
                    result.Add(item);
                }
            }

            return result;
        }

        private static IList<string> GetFiles(string path, string searchPattern, SearchOption option, string mainExcelName)
        {
            string[] excels = Directory.GetFiles(path, searchPattern, option);
            Array.Sort(excels, StringComparer.OrdinalIgnoreCase);
            List<string> result = new List<string>();
            if (!string.IsNullOrEmpty(mainExcelName))
            {
                string abTestPrefixName = mainExcelName + ConstBuiltin.AB_TEST_TAG;
                foreach (string item in excels)
                {
                    string nameNoExt = Path.GetFileNameWithoutExtension(item);
                    if (nameNoExt.StartsWith("~$", StringComparison.Ordinal))
                    {
                        continue;
                    }

                    if (nameNoExt.StartsWith(abTestPrefixName, StringComparison.Ordinal))
                    {
                        result.Add(item);
                    }
                }
            }
            else
            {
                foreach (string item in excels)
                {
                    if (Path.GetFileNameWithoutExtension(item).StartsWith("~$", StringComparison.Ordinal))
                    {
                        continue;
                    }

                    result.Add(item);
                }
            }

            return result;
        }
    }
}
