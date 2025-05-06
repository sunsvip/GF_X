using GameFramework;
using GameFramework.DataTable;
using GameFramework.Editor.DataTableTools;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityGameFramework.Runtime;
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
        /// <summary>
        /// Excel下拉列表总限制255个字符
        /// </summary>
        const int MAX_CHAR_LENGTH = 255;
        static IList<KeyValuePair<int, string>> m_DataTableVarTypes = null;
        [InitializeOnLoadMethod]
        static void InitEPPlusLicense()
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        }
        [MenuItem("Game Framework/GameTools/Refresh All Excels【刷新所有数据表】", false, 1001)]
        public static void GenerateDataTables()
        {
            RefreshAllDataTable();
            RefreshAllConfig();
            RefreshAllLanguage();
            GenerateUIFormNamesScript();
            GenerateGroupEnumScript();
            AssetDatabase.Refresh();
        }
        public static bool CreateGameConfigExcel(string excelPath)
        {
            if (File.Exists(excelPath))
            {
                Debug.LogWarning($"创建配置表失败! 文件已存在:{excelPath}");
                return false;
            }
            try
            {
                var excelDir = Path.GetDirectoryName(excelPath);
                if (!Directory.Exists(excelDir))
                {
                    Directory.CreateDirectory(excelDir);
                }
                using (var excel = new ExcelPackage(excelPath))
                {
                    var sheet = excel.Workbook.Worksheets.Add("Sheet 1");
                    sheet.SetValue(1, 1, "#");
                    sheet.SetValue(1, 2, Path.GetFileNameWithoutExtension(excelPath));
                    sheet.SetValue(2, 1, "#");
                    sheet.SetValue(2, 2, "Key");
                    sheet.SetValue(2, 3, "备注");
                    sheet.SetValue(2, 4, "Value");
                    excel.Save();
                }
                return true;
            }
            catch (Exception emsg)
            {
                Debug.LogError($"创建Excel:{excelPath}失败! Error:{emsg}");
                return false;
            }

        }
        public static bool CreateDataTableExcel(string excelPath)
        {
            if (File.Exists(excelPath))
            {
                Debug.LogWarning($"创建数据表失败! 文件已存在:{excelPath}");
                return false;
            }
            try
            {
                var excelDir = Path.GetDirectoryName(excelPath);
                if (!Directory.Exists(excelDir))
                {
                    Directory.CreateDirectory(excelDir);
                }
                using (var excel = new ExcelPackage(excelPath))
                {
                    var sheet = excel.Workbook.Worksheets.Add("Sheet 1");
                    sheet.SetValue(1, 1, "#");
                    sheet.SetValue(1, 2, Path.GetFileNameWithoutExtension(excelPath));
                    sheet.SetValue(2, 1, "#");
                    sheet.SetValue(2, 2, "ID");
                    sheet.SetValue(3, 1, "#");
                    sheet.SetValue(3, 2, "int");
                    sheet.SetValue(4, 1, "#");
                    sheet.SetValue(4, 3, "备注");
                    sheet.SetValue(4, 4, "请添加字段, 字段名首字母大写");
                    if (m_DataTableVarTypes == null)
                    {
                        m_DataTableVarTypes = ScanVariableTypes();
                    }
                    if (m_DataTableVarTypes != null)
                    {
                        var listValidation = sheet.DataValidations.AddListValidation("D3:Z3");
                        listValidation.AllowBlank = false;
                        listValidation.Formula.Values.Clear();
                        //listValidation.ShowErrorMessage = true;
                        //listValidation.ShowInputMessage = true;
                        foreach (var typeName in m_DataTableVarTypes)
                        {
                            listValidation.Formula.Values.Add(typeName.Value);
                        }
                    }
                    var i18nValidation = sheet.DataValidations.AddListValidation("D1:Z1");
                    i18nValidation.AllowBlank = true;
                    i18nValidation.Formula.Values.Clear();
                    i18nValidation.Formula.Values.Add(LocalizationTextScanner.EXCEL_I18N_TAG);
                    excel.Save();
                }
                return true;
            }
            catch (Exception emsg)
            {
                Debug.LogError($"创建Excel:{excelPath}失败! Error:{emsg}");
                return false;
            }

        }
        private static List<KeyValuePair<int, string>> ScanVariableTypes()
        {
            List<KeyValuePair<int, string>> types = new List<KeyValuePair<int, string>>();
            var nestedTypes = typeof(GameFramework.Editor.DataTableTools.DataTableProcessor).GetNestedTypes(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            foreach (var item in nestedTypes)
            {
                if (item.IsClass && item.BaseType.IsGenericType && item.BaseType.Name.CompareTo("GenericDataProcessor`1") == 0)
                {
                    var itemObj = Activator.CreateInstance(item);
                    var itemType = itemObj.GetType();
                    string typeName = itemType.GetProperty("LanguageKeyword").GetValue(itemObj) as string;
                    int priority = (int)itemType.GetProperty("ShowOrder").GetValue(itemObj);
                    types.Add(KeyValuePair.Create<int, string>(priority, typeName));
                }
            }
            types.Sort((itmA, itmB) => itmA.Key.CompareTo(itmB.Key));

            int totalLength = 0;
            int cutIndex = -1;
            for (int i = 0; i < types.Count; i++)
            {
                var item = types[i].Value;
                totalLength += item.Length;
                if (totalLength + i + 1 >= MAX_CHAR_LENGTH) break;
                cutIndex = i;
            }
            if (cutIndex < 0) return null;
            for (int i = types.Count - 1; i > cutIndex; i--) types.RemoveAt(i);
            return types;
        }
        /// <summary>
        /// 生成Entity,Sound,UI枚举脚本
        /// </summary>
        public static void GenerateGroupEnumScript()
        {
            var excelDir = ConstEditor.DataTableExcelPath;
            if (!Directory.Exists(excelDir))
            {
                Debug.LogErrorFormat("Excel DataTable directory is not exists:{0}", excelDir);
                return;
            }
            string[] groupExcels = { ConstEditor.EntityGroupTableExcel, ConstEditor.UIGroupTableExcel, ConstEditor.SoundGroupTableExcel };
            StringBuilder sBuilder = new StringBuilder();
            sBuilder.AppendLine("//此代码由工具自动生成, 请勿手动修改");
            sBuilder.AppendLine("public static partial class Const");
            sBuilder.AppendLine("{");
            foreach (var excel in groupExcels)
            {
                var excelFileName = UtilityBuiltin.AssetsPath.GetCombinePath(excelDir, excel);
                if (!File.Exists(excelFileName))
                {
                    Debug.LogErrorFormat("Excel is not exists:{0}", excelFileName);
                    return;
                }
                var excelPackage = new ExcelPackage(excelFileName);
                var excelSheet = excelPackage.Workbook.Worksheets[0];
                List<string> groupList = new List<string>();
                for (int rowIndex = excelSheet.Dimension.Start.Row; rowIndex <= excelSheet.Dimension.End.Row; rowIndex++)
                {
                    var rowStr = excelSheet.GetValue(rowIndex, 1);
                    if (rowStr != null && rowStr.ToString().StartsWith("#"))
                    {
                        continue;
                    }
                    var groupName = excelSheet.GetValue(rowIndex, 4).ToString();
                    if (!groupList.Contains(groupName)) groupList.Add(groupName);
                }
                excelSheet.Dispose();
                excelPackage.Dispose();

                string className = Path.GetFileNameWithoutExtension(excelFileName);
                string endWithStr = "Table";
                if (className.EndsWith(endWithStr))
                {
                    className = className.Substring(0, className.Length - endWithStr.Length);
                }
                sBuilder.AppendLine(Utility.Text.Format("\tpublic enum {0}", className));
                sBuilder.AppendLine("\t{");
                for (int i = 0; i < groupList.Count; i++)
                {
                    if (i < groupList.Count - 1)
                    {
                        sBuilder.AppendLine(Utility.Text.Format("\t\t{0},", groupList[i]));
                    }
                    else
                    {
                        sBuilder.AppendLine(Utility.Text.Format("\t\t{0}", groupList[i]));
                    }
                }
                sBuilder.AppendLine("\t}");
            }
            sBuilder.AppendLine("}");

            var outFileName = ConstEditor.ConstGroupScriptFileFullName;
            try
            {
                File.WriteAllText(outFileName, sBuilder.ToString());
                Debug.LogFormat("------------------成功生成Group文件:{0}---------------", outFileName);
            }
            catch (Exception e)
            {
                Debug.LogErrorFormat("Group文件生成失败:{0}", e.Message);
                throw;
            }
        }
        /// <summary>
        /// 生成UI界面枚举类型
        /// </summary>
        public static void GenerateUIFormNamesScript()
        {
            var excelDir = ConstEditor.DataTableExcelPath;
            if (!Directory.Exists(excelDir))
            {
                Debug.LogError($"生成UIView代码失败! 不存在文件夹:{excelDir}");
                return;
            }
            var excelFileName = UtilityBuiltin.AssetsPath.GetCombinePath(excelDir, ConstEditor.UITableExcel);
            if (!File.Exists(excelFileName))
            {
                Debug.LogError($"{excelFileName} 文件不存在!");
                return;
            }
            var excelPackage = new ExcelPackage(excelFileName);
            var excelSheet = excelPackage.Workbook.Worksheets[0];
            Dictionary<int, string> uiViewDic = new Dictionary<int, string>();
            for (int rowIndex = excelSheet.Dimension.Start.Row; rowIndex <= excelSheet.Dimension.End.Row; rowIndex++)
            {
                var rowStr = excelSheet.GetValue(rowIndex, 1);
                if (rowStr != null && rowStr.ToString().StartsWith("#"))
                {
                    continue;
                }
                uiViewDic.Add(int.Parse(excelSheet.GetValue(rowIndex, 2).ToString()), excelSheet.GetValue(rowIndex, 5).ToString());
            }
            excelSheet.Dispose();
            excelPackage.Dispose();
            var className = Path.GetFileNameWithoutExtension(ConstEditor.UIViewScriptFile);
            StringBuilder sBuilder = new StringBuilder();
            sBuilder.AppendLine("/**此代码由工具自动生成,请勿手动修改!**/");
            sBuilder.AppendLine(Utility.Text.Format("public enum {0} : int", className));
            sBuilder.AppendLine("{");
            int curIndex = 0;
            foreach (KeyValuePair<int, string> uiItem in uiViewDic)
            {
                string uiViewName = Path.GetFileName(uiItem.Value);
                bool isLast = curIndex == uiViewDic.Count - 1;
                sBuilder.AppendLine(Utility.Text.Format("\t{0} = {1}{2}", uiViewName, uiItem.Key, isLast ? "" : ","));
                curIndex++;
            }
            sBuilder.AppendLine("}");
            File.WriteAllText(ConstEditor.UIViewScriptFile, sBuilder.ToString());
            Debug.LogFormat("-------------------成功生成:{0}-----------------", ConstEditor.UIViewScriptFile);
        }
        /// <summary>
        /// 多语言Excel导出json
        /// </summary>
        /// <param name="excelFile"></param>
        /// <param name="outJsonFile"></param>
        /// <returns></returns>
        public static bool ExportLanguageExcel(string excelFile, string outJsonFile, bool useBytes)
        {
            List<LocalizationText> textList = new List<LocalizationText>();
            try
            {
                LocalizationTextScanner.LoadLanguageExcelTexts(excelFile, ref textList);
                SortedDictionary<string, string> languageDic = new SortedDictionary<string, string>();
                foreach (var item in textList)
                {
                    if (!languageDic.ContainsKey(item.Key))
                    {
                        languageDic.Add(item.Key, item.Value);
                    }
                }
                File.WriteAllText(outJsonFile, UtilityBuiltin.Json.ToJson(languageDic), Encoding.UTF8);
                if (useBytes)
                {
                    var bytesFileName = Path.ChangeExtension(outJsonFile, ".bytes");
                    using var fileStream = new FileStream(bytesFileName, FileMode.Create, FileAccess.Write);
                    using var binaryWriter = new BinaryWriter(fileStream, Encoding.UTF8);
                    foreach (var item in languageDic)
                    {
                        binaryWriter.Write(item.Key);
                        binaryWriter.Write(item.Value);
                    }
                }
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"多语言Excel导出json失败:{e.Message}");
                return false;
            }
        }
        static bool ExcelSheet2TxtFile(ExcelWorksheet excelSheet, string outTxtFile)
        {
            StringBuilder excelTxt = new StringBuilder();
            StringBuilder lineTxt = new StringBuilder();
            for (int rowIndex = excelSheet.Dimension.Start.Row; rowIndex <= excelSheet.Dimension.End.Row; rowIndex++)
            {
                lineTxt.Clear();
                string rowTxt = string.Empty;
                for (int colIndex = excelSheet.Dimension.Start.Column; colIndex <= excelSheet.Dimension.End.Column; colIndex++)
                {
                    string cellContent = excelSheet.GetValue<string>(rowIndex, colIndex);
                    if (!string.IsNullOrEmpty(cellContent))
                    {
                        cellContent = Regex.Replace(cellContent, "\n", @"\n");
                    }
                    lineTxt.Append(cellContent);
                    if (colIndex < excelSheet.Dimension.End.Column)
                    {
                        lineTxt.Append('\t');
                    }
                }
                string lineStr = lineTxt.ToString();
                if (string.IsNullOrWhiteSpace(lineStr))
                {
                    continue;
                }
                excelTxt.Append(lineStr);
                if (rowIndex < excelSheet.Dimension.End.Row)
                {
                    excelTxt.AppendLine();
                }
            }
            try
            {
                var outTxtDir = Path.GetDirectoryName(outTxtFile);
                if (!Directory.Exists(outTxtDir))
                {
                    Directory.CreateDirectory(outTxtDir);
                }
                File.WriteAllText(outTxtFile, excelTxt.ToString(), Encoding.UTF8);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"excel导出:{outTxtFile}失败:{e.Message}");
                return false;
            }
        }
        /// <summary>
        /// Excel转换为Txt
        /// </summary>
        public static bool Excel2TxtFile(string excelFileName, string outTxtFile)
        {
            bool result = true;
            var fileInfo = new FileInfo(excelFileName);
            string tmpExcelFile = UtilityBuiltin.AssetsPath.GetCombinePath(fileInfo.Directory.FullName, Utility.Text.Format("{0}.temp", fileInfo.Name));
            //Debug.Log($">>>>>>>>Excel2Txt: excel:{excelFileName}, outTxtFile:{outTxtFile}");
            try
            {
                File.Copy(excelFileName, tmpExcelFile, true);
                using (var excelPackage = new ExcelPackage(tmpExcelFile))
                {
                    result = ExcelSheet2TxtFile(excelPackage.Workbook.Worksheets[0], outTxtFile);
                    //int sheetCount = excelPackage.Workbook.Worksheets.Count;
                    //if (sheetCount == 1)
                    //{
                    //    result = ExcelSheet2TxtFile(excelPackage.Workbook.Worksheets[0], outTxtFile);
                    //}
                    //else
                    //{
                    //    var outputDir = Path.GetDirectoryName(outTxtFile);
                    //    var outputFileName = Path.GetFileNameWithoutExtension(outTxtFile);
                    //    var outputFileExtension = Path.GetExtension(outTxtFile);

                    //    for (int i = 0; i < sheetCount; i++)
                    //    {
                    //        var excelSheet = excelPackage.Workbook.Worksheets[i];
                    //        string sheetName = string.IsNullOrWhiteSpace(excelSheet.Name) ? i.ToString() : excelSheet.Name;
                    //        var fileName = UtilityBuiltin.AssetsPath.GetCombinePath(outputDir, Utility.Text.Format("{0}_{1}{2}", outputFileName, sheetName, outputFileExtension));
                    //        result &= ExcelSheet2TxtFile(excelSheet, fileName);
                    //    }
                    //}
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"excel导出txt失败:{e.Message}");
                result = false;
            }

            if (File.Exists(tmpExcelFile))
            {
                File.Delete(tmpExcelFile);
            }
            return result;
        }
        /// <summary>
        /// 从多语言Excel文件导出数据到工程
        /// </summary>
        /// <param name="files"></param>
        public static void RefreshAllLanguage(IList<string> files = null)
        {
            IList<string> excelFiles;
            if (files == null)
            {
                excelFiles = GetAllGameDataExcels(GameDataType.Language, GameDataExcelFileType.MainFile | GameDataExcelFileType.ABTestFile);
            }
            else
            {
                excelFiles = GetGameDataExcelWithABFiles(GameDataType.Language, files);
            }
            var appConfig = AppConfigs.GetInstanceEditor();
            int totalExcelCount = excelFiles.Count;
            for (int i = 0; i < totalExcelCount; i++)
            {
                var excelFileName = excelFiles[i];
                string outputFileName = GetGameDataExcelOutputFile(GameDataType.Language, excelFileName);
                EditorUtility.DisplayProgressBar($"导出Language:({i}/{totalExcelCount})", $"{excelFileName} -> {outputFileName}", i / (float)totalExcelCount);
                if (ExportLanguageExcel(excelFileName, outputFileName, appConfig.LoadFromBytes))
                {
                    GF.LogInfo($"Language导出成功:{outputFileName}");
                }
            }
            EditorUtility.ClearProgressBar();
            AssetDatabase.Refresh();
        }
        //[MenuItem("Game Framework/GameTools/Refresh All GameConfigs")]
        public static void RefreshAllConfig(IList<string> files = null)
        {
            IList<string> excelFiles;
            if (files == null)
            {
                excelFiles = GetAllGameDataExcels(GameDataType.Config, GameDataExcelFileType.MainFile | GameDataExcelFileType.ABTestFile);
            }
            else
            {
                excelFiles = GetGameDataExcelWithABFiles(GameDataType.Config, files);
            }
            var appConfig = AppConfigs.GetInstanceEditor();
            int totalExcelCount = excelFiles.Count;
            for (int i = 0; i < totalExcelCount; i++)
            {
                var excelFileName = excelFiles[i];
                string outputFileName = GetGameDataExcelOutputFile(GameDataType.Config, excelFileName);
                EditorUtility.DisplayProgressBar($"导出Config:({i}/{totalExcelCount})", $"{excelFileName} -> {outputFileName}", i / (float)totalExcelCount);
                if (Excel2TxtFile(excelFileName, outputFileName))
                {
                    GFBuiltin.LogInfo(Utility.Text.Format("导出Config文件成功: '{0}'.", outputFileName));
                }
                if (appConfig.LoadFromBytes)
                {
                    if (ExportConfig2BytesFile(outputFileName))
                    {
                        GFBuiltin.LogInfo(Utility.Text.Format("导出Config二进制文件成功: '{0}'.", outputFileName));
                    }
                }
            }
            EditorUtility.ClearProgressBar();
            AssetDatabase.Refresh();
        }

        public static void RefreshAllDataTable(IList<string> fullPathFiles = null)
        {
            var appConfig = AppConfigs.GetInstanceEditor();
            IList<string> excelFiles;
            if (fullPathFiles == null)
            {
                excelFiles = GetAllGameDataExcels(GameDataType.DataTable, GameDataExcelFileType.MainFile | GameDataExcelFileType.ABTestFile);
            }
            else
            {
                excelFiles = GetGameDataExcelWithABFiles(GameDataType.DataTable, fullPathFiles);
            }
            int totalExcelCount = excelFiles.Count;
            for (int i = 0; i < totalExcelCount; i++)
            {
                var excelFileName = excelFiles[i];
                string outputPath = GetGameDataExcelOutputFile(GameDataType.DataTable, excelFileName);
                EditorUtility.DisplayProgressBar($"导出DataTable:({i}/{totalExcelCount})", $"{excelFileName} -> {outputPath}", i / (float)totalExcelCount);
                try
                {
                    if (Excel2TxtFile(excelFileName, outputPath))
                    {
                        GF.LogInfo($"导出DataTable成功:{excelFileName} -> {outputPath}");
                        if (appConfig.LoadFromBytes)
                        {
                            DataTableProcessor dataTableProcessor = DataTableGenerator.CreateDataTableProcessor(outputPath);
                            if (!DataTableGenerator.CheckRawData(dataTableProcessor, outputPath))
                            {
                                Debug.LogError(Utility.Text.Format("Check raw data failure. DataTable file='{0}'", outputPath));
                                EditorUtility.ClearProgressBar();
                                break;
                            }
                            DataTableGenerator.GenerateDataFile(dataTableProcessor, outputPath);
                        }
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogErrorFormat("Excel -> DataTable:{0}", e.Message);
                    EditorUtility.ClearProgressBar();
                    break;
                }
            }
            AssetDatabase.Refresh();
            EditorUtility.ClearProgressBar();
            //生成数据表代码
            int dataTbCount = appConfig.DataTables.Length;

            string outputDir = GetGameDataExcelOutputDir(GameDataType.DataTable);
            string outputExtension = GetGameDataExcelOutputFileExtension(GameDataType.DataTable);
            for (int i = 0; i < dataTbCount; i++)
            {
                var dataTableName = appConfig.DataTables[i];
                string tbTxtFile = UtilityBuiltin.AssetsPath.GetCombinePath(outputDir, dataTableName + outputExtension);
                EditorUtility.DisplayProgressBar($"进度:({i}/{dataTbCount})", $"生成DataTable代码:{dataTableName}", i / (float)dataTbCount);
                if (!File.Exists(tbTxtFile))
                {
                    Debug.LogWarning($"生成DataTable代码失败! {dataTableName}文件不存在:{tbTxtFile}");
                    continue;
                }
                DataTableProcessor dataTableProcessor = DataTableGenerator.CreateDataTableProcessor(tbTxtFile);
                if (!DataTableGenerator.CheckRawData(dataTableProcessor, tbTxtFile))
                {
                    Debug.LogError(Utility.Text.Format("Check raw data failure. DataTableName='{0}'", dataTableName));
                    break;
                }

                DataTableGenerator.GenerateCodeFile(dataTableProcessor, tbTxtFile);
            }
            EditorUtility.ClearProgressBar();
            AssetDatabase.Refresh();
        }

        private static bool ExportConfig2BytesFile(string configFile)
        {
            if (!File.Exists(configFile)) return false;
            string bytesFileName = Path.ChangeExtension(configFile, ".bytes");

            try
            {
                
                using (StreamReader reader = new StreamReader(configFile))
                {
                    using var fileStream = new FileStream(bytesFileName, FileMode.Create, FileAccess.Write);
                    using var binaryWriter = new BinaryWriter(fileStream, Encoding.UTF8);
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        if (string.IsNullOrWhiteSpace(line) || line.StartsWith(DataTableProcessor.CommentLineSeparator)) continue;
                        var keyValues = line.Split(DataTableProcessor.DataSplitSeparators, StringSplitOptions.None);
                        if (keyValues.Length != 4)
                        {
                            Log.Error("Can not parse config line string '{0}' which column count is invalid.", line);
                            continue;
                        }
                        string configName = keyValues[1];
                        string configValue = keyValues[3];
                        binaryWriter.Write(configName);
                        binaryWriter.Write(configValue);
                    }
                }
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogError(Utility.Text.Format("Parse config file '{0}' failure, exception is '{1}'.", configFile, exception.ToString()));
                return false;
            }
        }
        internal static string GetGameDataRelativeName(string fileName, string relativePath)
        {
            var path = Path.GetRelativePath(relativePath, fileName);
            return UtilityBuiltin.AssetsPath.GetCombinePath(Path.GetDirectoryName(path), Path.GetFileNameWithoutExtension(path));
        }
        /// <summary>
        /// 给定主文件列表, 返回所有主文件及其AB测试文件
        /// </summary>
        /// <param name="tp"></param>
        /// <param name="mainFiles"></param>
        /// <returns></returns>
        public static IList<string> GetGameDataExcelWithABFiles(GameDataType tp, IList<string> mainFiles)
        {
            List<string> result = new List<string>();
            foreach (var mainFile in mainFiles)
            {
                var files = GetGameDataExcelWithABFiles(tp, mainFile);
                result.AddRange(files);
            }
            return result;
        }
        /// <summary>
        /// 给定主文件,返回主文件及其AB测试文件
        /// </summary>
        /// <param name="tp"></param>
        /// <param name="mainFile"></param>
        /// <returns></returns>
        private static IList<string> GetGameDataExcelWithABFiles(GameDataType tp, string mainExcelFile)
        {
            List<string> result = new List<string> { mainExcelFile };
            var excelName = Path.GetFileNameWithoutExtension(mainExcelFile);
            var allAbFiles = GetAllGameDataExcels(tp, GameDataExcelFileType.ABTestFile, excelName);
            foreach (var item in allAbFiles)
            {
                if (IsABTestFile(item, mainExcelFile))
                {
                    result.Add(item);
                }
            }
            return result;
        }
        /// <summary>
        /// 返回Excel的相对目录(无扩展名)
        /// </summary>
        /// <param name="tp"></param>
        /// <param name="excelFile"></param>
        /// <returns></returns>
        public static string GetGameDataExcelRelativePath(GameDataType tp, string excelFile)
        {
            var excelRelativePath = Path.GetRelativePath(GameDataGenerator.GetGameDataExcelDir(tp), excelFile);
            excelRelativePath = UtilityBuiltin.AssetsPath.GetCombinePath(Path.GetDirectoryName(excelRelativePath), Path.GetFileNameWithoutExtension(excelRelativePath)); // 获取表的相对路径并去掉扩展名
            return excelRelativePath;
        }
        public static string[] GameDataExcelRelative2FullPath(GameDataType tp, string[] relativeExcelPathArr)
        {
            string[] result = new string[relativeExcelPathArr.Length];
            for (int i = 0; i < relativeExcelPathArr.Length; i++)
            {
                result[i] = GameDataExcelRelative2FullPath(tp, relativeExcelPathArr[i]);
            }
            return result;
        }
        public static string GameDataExcelRelative2FullPath(GameDataType tp, string relativeExcelPath)
        {
            var excelDir = GetGameDataExcelDir(tp);
            return UtilityBuiltin.AssetsPath.GetCombinePath(excelDir, relativeExcelPath + ".xlsx");
        }
        public static string GetGameDataExcelOutputFile(GameDataType tp, string excelFile)
        {
            var excelRelativePath = GetGameDataExcelRelativePath(tp, excelFile);

            string extensionName = GetGameDataExcelOutputFileExtension(tp);
            return UtilityBuiltin.AssetsPath.GetCombinePath(GetGameDataExcelOutputDir(tp), excelRelativePath + extensionName);
        }

        private static string GetGameDataExcelOutputFileExtension(GameDataType tp)
        {
            string extensionName = "";
            switch (tp)
            {
                case GameDataType.DataTable:
                case GameDataType.Config:
                    extensionName = ".txt";
                    break;
                case GameDataType.Language:
                    extensionName = ".json";
                    break;
            }
            return extensionName;
        }

        /// <summary>
        /// 获取游戏数据表Excel的输出路径
        /// </summary>
        /// <param name="tp"></param>
        /// <returns></returns>
        public static string GetGameDataExcelOutputDir(GameDataType tp)
        {
            string excelDir = "";
            switch (tp)
            {
                case GameDataType.DataTable:
                    excelDir = ConstEditor.DataTablePath;
                    break;
                case GameDataType.Config:
                    excelDir = ConstEditor.GameConfigPath;
                    break;
                case GameDataType.Language:
                    excelDir = ConstEditor.LanguagePath;
                    break;
            }
            return excelDir;
        }
        /// <summary>
        /// 获取各种游戏数据表Excel的所在路径
        /// </summary>
        /// <param name="tp"></param>
        /// <returns></returns>
        public static string GetGameDataExcelDir(GameDataType tp)
        {
            string excelDir = "";
            switch (tp)
            {
                case GameDataType.DataTable:
                    excelDir = ConstEditor.DataTableExcelPath;
                    break;
                case GameDataType.Config:
                    excelDir = ConstEditor.ConfigExcelPath;
                    break;
                case GameDataType.Language:
                    excelDir = ConstEditor.LanguageExcelPath;
                    break;
            }
            return excelDir;
        }

        public static IList<string> GetAllGameDataExcels(GameDataType dtTp, GameDataExcelFileType tps, string mainExcelName = null)
        {
            List<string> result = new List<string>();

            if (dtTp.HasFlag(GameDataType.DataTable))
            {
                var files = GetGameDataExcelAtDir(GetGameDataExcelDir(GameDataType.DataTable), tps, mainExcelName);
                result.AddRange(files);
            }
            if (dtTp.HasFlag(GameDataType.Language))
            {
                var files = GetGameDataExcelAtDir(GetGameDataExcelDir(GameDataType.Language), tps, mainExcelName);
                result.AddRange(files);
            }
            if (dtTp.HasFlag(GameDataType.Config))
            {
                var files = GetGameDataExcelAtDir(GetGameDataExcelDir(GameDataType.Config), tps, mainExcelName);
                result.AddRange(files);
            }
            return result;
        }
        /// <summary>
        /// 获取给定目录下Excel文件, 可以按文件类型筛选结果
        /// </summary>
        /// <param name="excelDir"></param>
        /// <param name="tps"></param>
        /// <returns></returns>
        private static IList<string> GetGameDataExcelAtDir(string excelDir, GameDataExcelFileType tps, string mainExcelName)
        {
            List<string> result = new List<string>();
            if (string.IsNullOrWhiteSpace(excelDir) || !Directory.Exists(excelDir))
            {
                Debug.LogWarning($"获取GameData Excel失败, 给定路径为空或不存在:{excelDir}");
                return result;
            }
            IList<string> excelFiles = GetFiles(excelDir, "*.xlsx", SearchOption.AllDirectories, mainExcelName);
            foreach (var item in excelFiles)
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
        /// <summary>
        /// 获取给定路径下所有文件(不包含临时文件)
        /// </summary>
        /// <param name="path"></param>
        /// <param name="searchPattern"></param>
        /// <param name="option"></param>
        /// <returns></returns>
        private static IList<string> GetFiles(string path, string searchPattern, SearchOption option, string mainExcelName)
        {
            var excels = Directory.GetFiles(path, searchPattern, option);
            List<string> result = new List<string>();
            if (!string.IsNullOrEmpty(mainExcelName))
            {
                var abTestPrefixName = mainExcelName + ConstBuiltin.AB_TEST_TAG;
                foreach (var item in excels)
                {
                    var nameNoExt = Path.GetFileNameWithoutExtension(item);
                    if (nameNoExt.StartsWith("~$")) continue;

                    if (nameNoExt.StartsWith(abTestPrefixName))
                    {
                        result.Add(item);
                    }
                }
            }
            else
            {
                foreach (var item in excels)
                {
                    if (Path.GetFileNameWithoutExtension(item).StartsWith("~$")) continue;
                    result.Add(item);
                }
            }
            return result;
        }
        /// <summary>
        /// 判断是否为AB测试表
        /// </summary>
        /// <param name="excelFile"></param>
        /// <returns></returns>
        public static bool IsABTestFile(string excelFile)
        {
            var fileName = Path.GetFileNameWithoutExtension(excelFile);
            return Regex.IsMatch(fileName, Utility.Text.Format("{0}\\p{{L}}$", ConstBuiltin.AB_TEST_TAG));
        }
        /// <summary>
        /// 判断excel文件是否是给定主文件的AB测试文件, AB测试文件命名规则: [主文件名] + [#] + [测试组名]
        /// </summary>
        /// <param name="excelFile"></param>
        /// <param name="mainExcelFileNameNoExt"></param>
        /// <returns></returns>
        public static bool IsABTestFile(string excelFile, string mainExcelFile)
        {
            var mainFileName = Path.GetFileNameWithoutExtension(mainExcelFile);
            var abFileName = Path.GetFileNameWithoutExtension(excelFile);
            return abFileName.StartsWith(mainFileName + ConstBuiltin.AB_TEST_TAG);
        }
    }

}
