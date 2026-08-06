using GameFramework;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UGF.EditorTools.Data.DataTable;
using UnityEditor;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace UGF.EditorTools
{
    internal static class GameDataExportService
    {
        private const int DataTableTypeRow = 3;
        private const int DataTableContentStartRow = 5;

        internal static bool ExportLanguageExcel(string excelFile, string outJsonFile, bool useBytes)
        {
            List<LocalizationText> textList = new List<LocalizationText>();
            try
            {
                LocalizationTextScanner.LoadLanguageExcelTexts(excelFile, ref textList);
                SortedDictionary<string, string> languageDic = CreateLanguageDictionary(textList, excelFile);
                if (languageDic == null)
                {
                    GameDataGenerator.DeleteGeneratedFile(outJsonFile);
                    return false;
                }

                    GameDataGenerator.EnsureFileDirectory(outJsonFile);
                if (useBytes)
                {
                    GameDataGenerator.WriteStringPairsBytesFile(outJsonFile, languageDic);
                    GameDataGenerator.DeleteGeneratedFile(Path.ChangeExtension(outJsonFile, ".json"));
                }
                else
                {
                    GameDataGenerator.WriteTextFile(outJsonFile, UtilityBuiltin.Json.ToJson(languageDic), GameDataGenerator.Utf8NoBom);
                    GameDataGenerator.DeleteGeneratedFile(Path.ChangeExtension(outJsonFile, ".bytes"));
                }

                return true;
            }
            catch (Exception e)
            {
                GameDataGenerator.DeleteGeneratedFile(outJsonFile);
                Debug.LogError($"多语言Excel导出json失败:{e.Message}");
                return false;
            }
        }

        internal static bool Excel2TxtFile(string excelFileName, string outTxtFile, bool normalizeCustomJsonColumns = true)
        {
            if (!Excel2TextLines(excelFileName, out string[] lines, normalizeCustomJsonColumns))
            {
                return false;
            }

            return WriteTextLinesFile(outTxtFile, lines);
        }

        internal static void RefreshAllLanguage(IList<string> files = null)
        {
            AppConfigs appConfig = AppConfigs.GetInstanceEditor();
            IList<string> excelFiles = files == null
                ? GameDataPathService.GetGameDataExcelWithABFiles(GameDataType.Language, GameDataPathService.GetConfiguredMainFiles(GameDataType.Language, appConfig.Languages))
                : GameDataPathService.GetGameDataExcelWithABFiles(GameDataType.Language, files);

            int totalExcelCount = excelFiles.Count;
            for (int i = 0; i < totalExcelCount; i++)
            {
                string excelFileName = excelFiles[i];
                string outputFileName = GameDataPathService.GetGameDataRuntimeOutputFile(GameDataType.Language, excelFileName, appConfig.LoadFromBytes);
                EditorUtility.DisplayProgressBar($"导出Language:({i}/{totalExcelCount})", $"{excelFileName} -> {outputFileName}", i / (float)totalExcelCount);
                if (ExportLanguageExcel(excelFileName, outputFileName, appConfig.LoadFromBytes))
                {
                    GF.Log($"Language导出成功:{outputFileName}");
                }
            }

            EditorUtility.ClearProgressBar();
        }

        internal static void RefreshAllConfig(IList<string> files = null)
        {
            AppConfigs appConfig = AppConfigs.GetInstanceEditor();
            IList<string> excelFiles = files == null
                ? GameDataPathService.GetGameDataExcelWithABFiles(GameDataType.Config, GameDataPathService.GetConfiguredMainFiles(GameDataType.Config, appConfig.Configs))
                : GameDataPathService.GetGameDataExcelWithABFiles(GameDataType.Config, files);

            int totalExcelCount = excelFiles.Count;
            for (int i = 0; i < totalExcelCount; i++)
            {
                string excelFileName = excelFiles[i];
                string outputFileName = GameDataPathService.GetGameDataRuntimeOutputFile(GameDataType.Config, excelFileName, appConfig.LoadFromBytes);
                EditorUtility.DisplayProgressBar($"导出Config:({i}/{totalExcelCount})", $"{excelFileName} -> {outputFileName}", i / (float)totalExcelCount);
                if (ExportConfigExcel(excelFileName, outputFileName, appConfig.LoadFromBytes))
                {
                    GFBuiltin.Log(Utility.Text.Format("导出Config文件成功: '{0}'.", outputFileName));
                }
                else
                {
                    GameDataGenerator.DeleteGeneratedFile(outputFileName);
                }
            }

            EditorUtility.ClearProgressBar();
        }

        internal static void RefreshAllDataTable(IList<string> fullPathFiles = null)
        {
            AppConfigs appConfig = AppConfigs.GetInstanceEditor();
            IList<string> excelFiles = fullPathFiles == null
                ? GameDataPathService.GetGameDataExcelWithABFiles(GameDataType.DataTable, GameDataPathService.GetConfiguredDataTableMainFiles(appConfig))
                : GameDataPathService.GetGameDataExcelWithABFiles(GameDataType.DataTable, fullPathFiles);

            int totalExcelCount = excelFiles.Count;
            for (int i = 0; i < totalExcelCount; i++)
            {
                string excelFileName = excelFiles[i];
                string outputPath = GameDataPathService.GetGameDataRuntimeOutputFile(GameDataType.DataTable, excelFileName, appConfig.LoadFromBytes);
                EditorUtility.DisplayProgressBar($"导出DataTable:({i}/{totalExcelCount})", $"{excelFileName} -> {outputPath}", i / (float)totalExcelCount);
                try
                {
                    if (Excel2TextLines(excelFileName, out string[] lines, normalizeCustomJsonColumns: true))
                    {
                        GF.Log($"导出DataTable成功:{excelFileName} -> {outputPath}");
                        DataTableProcessor dataTableProcessor = DataTableGenerator.CreateDataTableProcessor(excelFileName, lines);
                        if (!DataTableGenerator.CheckRawData(dataTableProcessor, excelFileName))
                        {
                            Debug.LogError(Utility.Text.Format("Check raw data failure. DataTable file='{0}'", excelFileName));
                            GameDataGenerator.DeleteGeneratedFile(outputPath);
                            EditorUtility.ClearProgressBar();
                            break;
                        }

                        if (appConfig.LoadFromBytes)
                        {
                            if (!DataTableGenerator.GenerateDataFile(dataTableProcessor, excelFileName, outputPath))
                            {
                                GameDataGenerator.DeleteGeneratedFile(outputPath);
                                EditorUtility.ClearProgressBar();
                                break;
                            }

                            GameDataGenerator.DeleteGeneratedFile(Path.ChangeExtension(outputPath, ".txt"));
                        }
                        else
                        {
                            if (!WriteTextLinesFile(outputPath, lines))
                            {
                                GameDataGenerator.DeleteGeneratedFile(outputPath);
                                EditorUtility.ClearProgressBar();
                                break;
                            }

                            GameDataGenerator.DeleteGeneratedFile(Path.ChangeExtension(outputPath, ".bytes"));
                        }

                        if (!GameDataPathService.IsABTestFile(excelFileName))
                        {
                            DataTableGenerator.GenerateCodeFileByTableName(dataTableProcessor, GameDataPathService.GetGameDataExcelRelativePath(GameDataType.DataTable, excelFileName));
                        }
                    }
                    else
                    {
                        GameDataGenerator.DeleteGeneratedFile(outputPath);
                    }
                }
                catch (Exception e)
                {
                    GameDataGenerator.DeleteGeneratedFile(outputPath);
                    Debug.LogErrorFormat("Excel -> DataTable:{0}", e.Message);
                    EditorUtility.ClearProgressBar();
                    break;
                }
            }

            EditorUtility.ClearProgressBar();
        }

        private static SortedDictionary<string, string> CreateLanguageDictionary(List<LocalizationText> textList, string excelFile)
        {
            SortedDictionary<string, string> languageDic = new SortedDictionary<string, string>();
            foreach (LocalizationText item in textList)
            {
                if (item == null || string.IsNullOrWhiteSpace(item.Key))
                {
                    continue;
                }

                if (languageDic.ContainsKey(item.Key))
                {
                    Debug.LogError($"多语言Excel存在重复Key, 文件:{excelFile}, Key:{item.Key}");
                    return null;
                }

                languageDic.Add(item.Key, item.Value ?? string.Empty);
            }

            return languageDic;
        }

        private static string[] ExcelSheetToTextLines(ExcelWorksheet excelSheet, bool normalizeCustomJsonColumns)
        {
            // 空工作表 Dimension 为 null, 提前返回空行, 避免 Dimension.End.Row 触发 NRE.
            if (excelSheet.Dimension == null)
            {
                return Array.Empty<string>();
            }

            Dictionary<int, string> customJsonColumns = normalizeCustomJsonColumns ? GetCustomJsonColumns(excelSheet) : null;
            HashSet<int> dateTimeColumns = GetDateTimeColumns(excelSheet);
            StringBuilder lineTxt = new StringBuilder();
            List<string> lines = new List<string>(excelSheet.Dimension.End.Row - excelSheet.Dimension.Start.Row + 1);
            for (int rowIndex = excelSheet.Dimension.Start.Row; rowIndex <= excelSheet.Dimension.End.Row; rowIndex++)
            {
                bool isDataRow = rowIndex >= DataTableContentStartRow && !IsCommentRow(excelSheet, rowIndex);
                lineTxt.Clear();
                for (int colIndex = excelSheet.Dimension.Start.Column; colIndex <= excelSheet.Dimension.End.Column; colIndex++)
                {
                    string cellContent;
                    if (isDataRow && dateTimeColumns != null && dateTimeColumns.Contains(colIndex))
                    {
                        cellContent = GetDateTimeCellText(excelSheet, rowIndex, colIndex);
                    }
                    else
                    {
                        cellContent = excelSheet.GetValue<string>(rowIndex, colIndex);
                        if (isDataRow && customJsonColumns != null && customJsonColumns.TryGetValue(colIndex, out string customJsonType))
                        {
                            cellContent = DataTableProcessor.NormalizeCustomJsonValue(customJsonType, cellContent);
                        }
                        else
                        {
                            cellContent = NormalizeCellTextForTxt(cellContent);
                        }
                    }

                    lineTxt.Append(cellContent);
                    if (colIndex < excelSheet.Dimension.End.Column)
                    {
                        lineTxt.Append('\t');
                    }
                }

                string lineStr = lineTxt.ToString();
                if (!string.IsNullOrWhiteSpace(lineStr))
                {
                    lines.Add(lineStr);
                }
            }

            return lines.ToArray();
        }

        private static bool WriteTextLinesFile(string outTxtFile, string[] lines)
        {
            try
            {
                GameDataGenerator.WriteTextFile(outTxtFile, string.Join(Environment.NewLine, lines), GameDataGenerator.Utf8NoBom);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"excel导出:{outTxtFile}失败:{e.Message}");
                return false;
            }
        }

        private static string NormalizeCellTextForTxt(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }

            bool hasSpecial = false;
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                if (c == '\r' || c == '\n' || c == '\t')
                {
                    hasSpecial = true;
                    break;
                }
            }

            if (!hasSpecial)
            {
                return value;
            }

            StringBuilder builder = new StringBuilder(value.Length);
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                if (c == '\r' || c == '\n')
                {
                    continue;
                }

                if (c == '\t')
                {
                    builder.Append(' ');
                    continue;
                }

                builder.Append(c);
            }

            return builder.ToString();
        }

        private static bool Excel2TextLines(string excelFileName, out string[] lines, bool normalizeCustomJsonColumns = true)
        {
            lines = null;
            try
            {
                using FileStream excelStream = new FileStream(excelFileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var excelPackage = new ExcelPackage(excelStream);
                lines = ExcelSheetToTextLines(excelPackage.Workbook.Worksheets[0], normalizeCustomJsonColumns);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"excel导出txt失败:{e.Message}");
                return false;
            }
        }

        private static Dictionary<int, string> GetCustomJsonColumns(ExcelWorksheet excelSheet)
        {
            Dictionary<int, string> result = null;
            if (excelSheet.Dimension.End.Row < DataTableTypeRow)
            {
                return result;
            }

            for (int colIndex = excelSheet.Dimension.Start.Column; colIndex <= excelSheet.Dimension.End.Column; colIndex++)
            {
                string typeName = excelSheet.GetValue<string>(DataTableTypeRow, colIndex);
                if (!DataTableProcessor.IsCustomJsonType(typeName))
                {
                    continue;
                }

                result ??= new Dictionary<int, string>();
                result[colIndex] = typeName;
            }

            return result;
        }

        private static HashSet<int> GetDateTimeColumns(ExcelWorksheet excelSheet)
        {
            if (excelSheet.Dimension.End.Row < DataTableTypeRow)
            {
                return null;
            }

            HashSet<int> result = null;
            for (int colIndex = excelSheet.Dimension.Start.Column; colIndex <= excelSheet.Dimension.End.Column; colIndex++)
            {
                string typeName = excelSheet.GetValue<string>(DataTableTypeRow, colIndex);
                if (!string.Equals(typeName, "datetime", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(typeName, "system.datetime", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                result ??= new HashSet<int>();
                result.Add(colIndex);
            }

            return result;
        }

        private static string GetDateTimeCellText(ExcelWorksheet excelSheet, int rowIndex, int colIndex)
        {
            object cellValue = excelSheet.Cells[rowIndex, colIndex].Value;
            if (cellValue == null)
            {
                return string.Empty;
            }

            DateTime dateTime = cellValue switch
            {
                DateTime value => value,
                double value => DateTime.FromOADate(value),
                float value => DateTime.FromOADate(value),
                decimal value => DateTime.FromOADate((double)value),
                int value => DateTime.FromOADate(value),
                long value => DateTime.FromOADate(value),
                _ => throw new InvalidDataException($"DateTime数据表单元格必须为Excel日期值. Row='{rowIndex}' Column='{colIndex}' Value='{cellValue}'")
            };

            return dateTime.ToString(DataTableExtension.DateTimeFormat, CultureInfo.InvariantCulture);
        }

        private static bool IsCommentRow(ExcelWorksheet excelSheet, int rowIndex)
        {
            string firstCellValue = excelSheet.GetValue<string>(rowIndex, excelSheet.Dimension.Start.Column);
            return !string.IsNullOrEmpty(firstCellValue) && firstCellValue.StartsWith(DataTableProcessor.CommentLineSeparator, StringComparison.Ordinal);
        }

        private static bool ExportConfigExcel(string excelFileName, string outputFileName, bool useBytes)
        {
            if (!Excel2TextLines(excelFileName, out string[] lines))
            {
                return false;
            }

            if (useBytes)
            {
                List<KeyValuePair<string, string>> configEntries = CreateConfigEntries(lines, excelFileName);
                if (configEntries == null)
                {
                    GameDataGenerator.DeleteGeneratedFile(outputFileName);
                    return false;
                }

                GameDataGenerator.WriteStringPairsBytesFile(outputFileName, configEntries);
                GameDataGenerator.DeleteGeneratedFile(Path.ChangeExtension(outputFileName, ".txt"));
                return true;
            }

            if (!WriteTextLinesFile(outputFileName, lines))
            {
                return false;
            }

            GameDataGenerator.DeleteGeneratedFile(Path.ChangeExtension(outputFileName, ".bytes"));
            return true;
        }

        private static List<KeyValuePair<string, string>> CreateConfigEntries(string[] lines, string sourceName)
        {
            List<KeyValuePair<string, string>> configEntries = new List<KeyValuePair<string, string>>();
            HashSet<string> configNames = new HashSet<string>();
            foreach (string line in lines)
            {
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith(DataTableProcessor.CommentLineSeparator, StringComparison.Ordinal))
                {
                    continue;
                }

                string[] keyValues = line.Split(DataTableProcessor.DataSplitSeparators, StringSplitOptions.None);
                if (keyValues.Length != 4)
                {
                    Debug.LogError(Utility.Text.Format("Can not parse config line string '{0}' in '{1}' which column count is invalid.", line, sourceName));
                    return null;
                }

                string configName = keyValues[1];
                string configValue = keyValues[3];
                if (string.IsNullOrWhiteSpace(configName))
                {
                    Debug.LogError(Utility.Text.Format("Can not parse config line string '{0}' in '{1}' which config name is invalid.", line, sourceName));
                    return null;
                }

                if (!configNames.Add(configName))
                {
                    Debug.LogError(Utility.Text.Format("Can not parse config line string '{0}' in '{1}' which config name is duplicate.", line, sourceName));
                    return null;
                }

                configEntries.Add(KeyValuePair.Create(configName, configValue));
            }

            return configEntries;
        }
    }
}
