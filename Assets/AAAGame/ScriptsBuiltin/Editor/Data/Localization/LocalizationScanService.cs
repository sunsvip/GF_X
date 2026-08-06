using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace UGF.EditorTools
{
    internal static class LocalizationScanService
    {
#if UNITY_EDITOR_WIN
        private const string ScannerTool = "Tools/LocalizationStringScanner/LocalizationCodeScanner.exe";
#else
        private const string ScannerTool = "Tools/LocalizationStringScanner/LocalizationCodeScanner";
#endif
        private const int ScannerExitTimeoutMs = 600000;

        internal static List<string> ScanAllLocalizationText(string[] localizationFuncNames, Action<string, int, int> onScanProgress = null)
        {
            var textsFromPrefab = ScanLocalizationTextFromPrefab(onScanProgress);
            var textsFromDataTable = ScanLocalizationTextFromDataTables(onScanProgress);

            var tmpOutputTxtFile = UtilityBuiltin.AssetsPath.GetCombinePath(ConstEditor.ToolsPath, "LocalizationTextsScannerOutput.txt");
            List<string> textsFromCode;
            try
            {
                textsFromCode = ScanLocalizationTextFromCode(Path.GetDirectoryName(ConstEditor.HotfixAssembly), localizationFuncNames, tmpOutputTxtFile, onScanProgress, true);
            }
            catch (Exception exception)
            {
                UnityEngine.Debug.LogError($"扫描代码本地化文本失败，已保留Prefab和DataTable结果。Error:{exception.Message}");
                textsFromCode = new List<string>();
            }

            List<string> result = new List<string>();
            AppendUnique(textsFromPrefab, result);
            AppendUnique(textsFromDataTable, result);
            AppendUnique(textsFromCode, result);
            return result;
        }

        internal static List<string> ScanLocalizationTextFromCode(string csFileDir, string[] funcNames, string outputFile, Action<string, int, int> onProgressUpdate = null, bool scanByDir = false)
        {
            List<string> result = new List<string>();
            if (!Directory.Exists(csFileDir))
            {
                return result;
            }

            if (File.Exists(outputFile))
            {
                File.Delete(outputFile);
            }

            if (!scanByDir)
            {
                var csFiles = Directory.GetFiles(csFileDir, "*.cs", SearchOption.AllDirectories);
                int totalCount = csFiles.Length;
                for (int i = 0; i < totalCount; i++)
                {
                    var csFile = csFiles[i];
                    onProgressUpdate?.Invoke(csFile, totalCount, i);
                    ScanLocalizationTextFromScript(csFile, funcNames, outputFile);
                }
            }
            else
            {
                onProgressUpdate?.Invoke(csFileDir, 1, 1);
                ScanLocalizationTextFromScript(csFileDir, funcNames, outputFile);
            }

            if (!File.Exists(outputFile))
            {
                return result;
            }

            var allLines = File.ReadAllLines(outputFile);
            AppendUnique(allLines, result);
            return result;
        }

        internal static List<string> ScanLocalizationTextFromPrefab(Action<string, int, int> onProgressUpdate = null)
        {
            var assetGuids = AssetDatabase.FindAssets("t:Prefab", new[] { ConstEditor.PrefabsPath });
            List<string> keyList = new List<string>();
            int totalCount = assetGuids.Length;
            for (int i = 0; i < totalCount; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(assetGuids[i]);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                onProgressUpdate?.Invoke(path, totalCount, i);
                if (prefab == null)
                {
                    continue;
                }

                var keyArr = prefab.GetComponentsInChildren<UnityGameFramework.Runtime.UIStringKey>(true);
                foreach (var newKey in keyArr)
                {
                    if (string.IsNullOrWhiteSpace(newKey.Key) || keyList.Contains(newKey.Key))
                    {
                        continue;
                    }

                    keyList.Add(newKey.Key);
                }
            }

            return keyList;
        }

        internal static List<string> ScanLocalizationTextFromDataTables(Action<string, int, int> onProgressUpdate = null)
        {
            List<string> keyList = new List<string>();
            var appConfig = AppConfigs.GetInstanceEditor();
            var mainTbFullFiles = GameDataGenerator.GameDataExcelRelative2FullPath(GameDataType.DataTable, appConfig.DataTables);
            var tbFullFiles = GameDataGenerator.GetGameDataExcelWithABFiles(GameDataType.DataTable, mainTbFullFiles);
            for (int i = 0; i < tbFullFiles.Count; i++)
            {
                var excelFile = tbFullFiles[i];
                var fileInfo = new FileInfo(excelFile);
                if (!fileInfo.Exists)
                {
                    continue;
                }

                onProgressUpdate?.Invoke(excelFile, tbFullFiles.Count, i);
                try
                {
                    using FileStream excelStream = new FileStream(excelFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    using var excelPackage = new ExcelPackage(excelStream);
                    var excelSheet = excelPackage.Workbook.Worksheets.FirstOrDefault();
                    if (excelSheet?.Dimension == null || excelSheet.Dimension.End.Row < 1)
                    {
                        continue;
                    }

                    for (int colIndex = excelSheet.Dimension.Start.Column; colIndex <= excelSheet.Dimension.End.Column; colIndex++)
                    {
                        if (!string.Equals(excelSheet.GetValue<string>(1, colIndex), LocalizationTextScanner.EXCEL_I18N_TAG, StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        for (int rowIndex = 5; rowIndex <= excelSheet.Dimension.End.Row; rowIndex++)
                        {
                            string langKey = excelSheet.GetValue<string>(rowIndex, colIndex);
                            if (string.IsNullOrWhiteSpace(langKey) || keyList.Contains(langKey))
                            {
                                continue;
                            }

                            keyList.Add(langKey);
                        }
                    }
                }
                catch (Exception exception)
                {
                    UnityEngine.Debug.LogError($"扫描数据表本地化文本失败!文件:{excelFile}, Error:{exception.Message}");
                }
            }

            return keyList;
        }

        private static bool ScanLocalizationTextFromScript(string srcPath, string[] functionNames, string outputFile)
        {
            string scannerToolFile = UtilityBuiltin.AssetsPath.GetCombinePath(Directory.GetParent(Application.dataPath).FullName, ScannerTool);
            StringBuilder strBuilder = new StringBuilder();
            strBuilder.Append($" \"{srcPath}\"");
            strBuilder.Append($" \"{outputFile}\"");
            foreach (var func in functionNames)
            {
                strBuilder.Append($" \"{func}\"");
            }

            var processInfo = new ProcessStartInfo(scannerToolFile, strBuilder.ToString())
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                WorkingDirectory = Directory.GetParent(Application.dataPath).FullName
            };

            try
            {
                using var process = Process.Start(processInfo);
                if (process == null)
                {
                    UnityEngine.Debug.LogError($"扫描代码本地化文本失败! 启动扫描进程失败。srcPath:{srcPath}, functions:{UtilityBuiltin.Json.ToJson(functionNames)}, outputFile:{outputFile}");
                    return false;
                }

                if (!process.WaitForExit(ScannerExitTimeoutMs))
                {
                    TryKillProcess(process);
                    UnityEngine.Debug.LogError($"扫描代码本地化文本失败! 扫描进程超时。srcPath:{srcPath}, outputFile:{outputFile}");
                    return false;
                }

                bool success = process.ExitCode == 0;
                if (!success)
                {
                    UnityEngine.Debug.LogError($"扫描代码本地化文本失败! srcPath:{srcPath}, functions:{UtilityBuiltin.Json.ToJson(functionNames)}, outputFile:{outputFile}");
                }

                return success;
            }
            catch (Exception exception)
            {
                UnityEngine.Debug.LogError($"扫描代码本地化文本失败! srcPath:{srcPath}, functions:{UtilityBuiltin.Json.ToJson(functionNames)}, outputFile:{outputFile}, Error:{exception.Message}");
                return false;
            }
        }

        private static void TryKillProcess(Process process)
        {
            try
            {
                if (process != null && !process.HasExited)
                {
                    process.Kill();
                }
            }
            catch (Exception exception)
            {
                UnityEngine.Debug.LogWarning($"终止本地化扫描进程失败:{exception.Message}");
            }
        }

        private static void AppendUnique(IEnumerable<string> source, List<string> result)
        {
            foreach (var item in source)
            {
                if (string.IsNullOrWhiteSpace(item) || result.Contains(item))
                {
                    continue;
                }

                result.Add(item);
            }
        }
    }
}
