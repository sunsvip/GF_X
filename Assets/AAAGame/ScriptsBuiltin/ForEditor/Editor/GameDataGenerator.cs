using GameFramework;
using GameFramework.Editor.DataTableTools;
using OfficeOpenXml;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using static Unity.VisualScripting.Dependencies.Sqlite.SQLite3;

namespace UGF.EditorTools
{
    [Flags]
    public enum GameDataExcelFileType
    {
        MainFile = 1 << 0,
        ABTestFile = 1 << 1
    }
    public class GameDataGenerator
    {
        [MenuItem("Game Framework/GameTools/Clear Missing Scripts【清除Prefab丢失脚本】")]
        public static void ClearMissingScripts()
        {
            var pfbArr = AssetDatabase.FindAssets("t:Prefab");
            foreach (var item in pfbArr)
            {
                var pfbFileName = AssetDatabase.GUIDToAssetPath(item);
                var pfb = AssetDatabase.LoadAssetAtPath<GameObject>(pfbFileName);
                GameObjectUtility.RemoveMonoBehavioursWithMissingScript(pfb);
            }
        }
        [MenuItem("Game Framework/GameTools/Refresh All Excels【刷新所有数据表】", false, 1001)]
        public static void GenerateDataTables()
        {
            RefreshAllDataTable();
            RefreshAllConfig();
            RefreshAllLanguage();
            try
            {
                GenerateUIViewScript();
            }
            catch (System.Exception e)
            {
                Debug.LogErrorFormat("生成UIView.cs失败:{0}", e.Message);
                throw;
            }
            GenerateGroupEnumScript();
            AssetDatabase.Refresh();
        }
        public static bool CreateGameConfigExcel(string excelPath)
        {
            try
            {
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
            try
            {
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
                var excelFileName = UtilityBuiltin.ResPath.GetCombinePath(excelDir, excel);
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
        public static void GenerateUIViewScript()
        {
            var excelDir = ConstEditor.DataTableExcelPath;
            if (!Directory.Exists(excelDir))
            {
                Debug.LogError($"生成UIView代码失败! 不存在文件夹:{excelDir}");
                return;
            }
            var excelFileName = UtilityBuiltin.ResPath.GetCombinePath(excelDir, ConstEditor.UITableExcel);
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
            StringBuilder sBuilder = new StringBuilder();
            sBuilder.AppendLine("public enum UIViews : int");
            sBuilder.AppendLine("{");
            int curIndex = 0;
            foreach (KeyValuePair<int, string> uiItem in uiViewDic)
            {
                if (curIndex < uiViewDic.Count - 1)
                {
                    sBuilder.AppendLine(Utility.Text.Format("\t{0} = {1},", uiItem.Value, uiItem.Key));
                }
                else
                {
                    sBuilder.AppendLine(Utility.Text.Format("\t{0} = {1}", uiItem.Value, uiItem.Key));
                }
                curIndex++;
            }
            sBuilder.AppendLine("}");
            File.WriteAllText(ConstEditor.UIViewScriptFile, sBuilder.ToString());
            Debug.LogFormat("-------------------成功生成UIViews.cs-----------------");
        }
        /// <summary>
        /// 多语言Excel导出json
        /// </summary>
        /// <param name="excelFile"></param>
        /// <param name="outJsonFile"></param>
        /// <returns></returns>
        public static bool LanguageExcel2Json(string excelFile, string outJsonFile)
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
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"多语言Excel导出json失败:{e.Message}");
                return false;
            }
        }
        /// <summary>
        /// Excel转换为Txt
        /// </summary>
        public static bool Excel2TxtFile(string excelFileName, string outTxtFile)
        {
            bool result = false;
            var fileInfo = new FileInfo(excelFileName);
            string tmpExcelFile = UtilityBuiltin.ResPath.GetCombinePath(fileInfo.Directory.FullName, Utility.Text.Format("{0}.temp", fileInfo.Name));
            //Debug.Log($">>>>>>>>Excel2Txt: excel:{excelFileName}, outTxtFile:{outTxtFile}");
            try
            {
                File.Copy(excelFileName, tmpExcelFile, true);
                using (var excelPackage = new ExcelPackage(tmpExcelFile))
                {
                    var excelSheet = excelPackage.Workbook.Worksheets[0];
                    string excelTxt = string.Empty;
                    for (int rowIndex = excelSheet.Dimension.Start.Row; rowIndex <= excelSheet.Dimension.End.Row; rowIndex++)
                    {
                        string rowTxt = string.Empty;
                        for (int colIndex = excelSheet.Dimension.Start.Column; colIndex <= excelSheet.Dimension.End.Column; colIndex++)
                        {
                            rowTxt = Utility.Text.Format("{0}{1}\t", rowTxt, excelSheet.GetValue(rowIndex, colIndex));
                        }
                        rowTxt = rowTxt.Substring(0, rowTxt.Length - 1);
                        excelTxt = Utility.Text.Format("{0}{1}\n", excelTxt, rowTxt);
                    }
                    excelTxt = excelTxt.TrimEnd('\n');
                    File.WriteAllText(outTxtFile, excelTxt, Encoding.UTF8);
                    result = true;
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
        public static void RefreshAllLanguage(string[] files = null)
        {
            string[] excelFiles;
            if (files == null)
            {
                excelFiles = GetAllGameDataExcels(GameDataType.Language, GameDataExcelFileType.MainFile | GameDataExcelFileType.ABTestFile);
            }
            else
            {
                excelFiles = GetGameDataExcelWithABFiles(GameDataType.Language, files);
            }
            int totalExcelCount = excelFiles.Length;
            for (int i = 0; i < totalExcelCount; i++)
            {
                var excelFileName = excelFiles[i];
                string outputFileName = GetGameDataExcelOutputFile(GameDataType.Language, excelFileName);
                EditorUtility.DisplayProgressBar($"导出Language:({i}/{totalExcelCount})", $"{excelFileName} -> {outputFileName}", i / (float)totalExcelCount);
                if (LanguageExcel2Json(excelFileName, outputFileName))
                {
                    GF.LogInfo($"Language导出成功:{outputFileName}");
                }
            }
            EditorUtility.ClearProgressBar();
            AssetDatabase.Refresh();
        }
        //[MenuItem("Game Framework/GameTools/Refresh All GameConfigs")]
        public static void RefreshAllConfig(string[] files = null)
        {
            string[] excelFiles;
            if (files == null)
            {
                excelFiles = GetAllGameDataExcels(GameDataType.Config, GameDataExcelFileType.MainFile | GameDataExcelFileType.ABTestFile);
            }
            else
            {
                excelFiles = GetGameDataExcelWithABFiles(GameDataType.Config, files);
            }
            int totalExcelCount = excelFiles.Length;
            for (int i = 0; i < totalExcelCount; i++)
            {
                var excelFileName = excelFiles[i];
                string outputFileName = GetGameDataExcelOutputFile(GameDataType.Config, excelFileName);
                EditorUtility.DisplayProgressBar($"导出Config:({i}/{totalExcelCount})", $"{excelFileName} -> {outputFileName}", i / (float)totalExcelCount);
                if (Excel2TxtFile(excelFileName, outputFileName))
                {
                    GF.LogInfo($"导出Config成功:{outputFileName}");
                }
            }
            EditorUtility.ClearProgressBar();
            AssetDatabase.Refresh();
        }
        public static void RefreshAllDataTable(string[] fullPathFiles = null)
        {
            string[] excelFiles;
            if (fullPathFiles == null)
            {
                excelFiles = GetAllGameDataExcels(GameDataType.DataTable, GameDataExcelFileType.MainFile | GameDataExcelFileType.ABTestFile);
            }
            else
            {
                excelFiles = GetGameDataExcelWithABFiles(GameDataType.DataTable, fullPathFiles);
            }
            int totalExcelCount = excelFiles.Length;
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
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogErrorFormat("Excel -> DataTable:{0}", e.Message);
                }
            }
            AssetDatabase.Refresh();
            EditorUtility.ClearProgressBar();
            //生成数据表代码
            var appConfig = AppConfigs.GetInstanceEditor(); ;
            int dataTbCount = appConfig.DataTables.Length;

            string outputDir = GetGameDataExcelOutputDir(GameDataType.DataTable);
            string outputExtension = GetGameDataExcelOutputFileExtension(GameDataType.DataTable);
            for (int i = 0; i < dataTbCount; i++)
            {
                var dataTableName = Path.GetFileNameWithoutExtension(appConfig.DataTables[i]);
                string tbTxtFile = UtilityBuiltin.ResPath.GetCombinePath(outputDir, appConfig.DataTables[i] + outputExtension);
                EditorUtility.DisplayProgressBar($"进度:({i}/{dataTbCount})", $"生成DataTable代码:{dataTableName}", i / (float)dataTbCount);
                if (!File.Exists(tbTxtFile))
                {
                    Debug.LogWarning($"生成DataTable代码失败! {dataTableName}文件不存在:{tbTxtFile}");
                    continue;
                }
                DataTableProcessor dataTableProcessor = DataTableGenerator.CreateDataTableProcessor(dataTableName);
                if (!DataTableGenerator.CheckRawData(dataTableProcessor, dataTableName))
                {
                    Debug.LogError(Utility.Text.Format("Check raw data failure. DataTableName='{0}'", dataTableName));
                    break;
                }

                DataTableGenerator.GenerateCodeFile(dataTableProcessor, dataTableName);
            }
            EditorUtility.ClearProgressBar();
            AssetDatabase.Refresh();
        }

        /// <summary>
        /// 给定主文件列表, 返回所有主文件及其AB测试文件
        /// </summary>
        /// <param name="tp"></param>
        /// <param name="files"></param>
        /// <returns></returns>
        public static string[] GetGameDataExcelWithABFiles(GameDataType tp, string[] mainFiles)
        {
            string[] result = new string[0];
            foreach (var mainFile in mainFiles)
            {
                var files = GetGameDataExcelWithABFiles(tp, mainFile);
                ArrayUtility.AddRange(ref result, files);
            }
            return result;
        }
        /// <summary>
        /// 给定主文件,返回主文件及其AB测试文件
        /// </summary>
        /// <param name="tp"></param>
        /// <param name="mainFile"></param>
        /// <returns></returns>
        private static string[] GetGameDataExcelWithABFiles(GameDataType tp, string mainExcelFile)
        {
            string[] result = new string[] { mainExcelFile };
            var allAbFiles = GetAllGameDataExcels(tp, GameDataExcelFileType.ABTestFile);
            foreach (var item in allAbFiles)
            {
                if (IsABTestFile(item, mainExcelFile))
                {
                    ArrayUtility.Add(ref result, item);
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
            excelRelativePath = UtilityBuiltin.ResPath.GetCombinePath(Path.GetDirectoryName(excelRelativePath), Path.GetFileNameWithoutExtension(excelRelativePath)); // 获取表的相对路径并去掉扩展名
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

            var fileName = Path.GetFileName(relativeExcelPath);
            fileName = fileName.EndsWith(".xlsx") ? fileName : fileName + ".xlsx";
            var fullDir = Utility.Path.GetRegularPath(Path.GetDirectoryName(relativeExcelPath));
            if (fullDir.StartsWith(excelDir))
            {
                return UtilityBuiltin.ResPath.GetCombinePath(fullDir, fileName);
            }
            return UtilityBuiltin.ResPath.GetCombinePath(excelDir, fileName);
        }
        public static string GetGameDataExcelOutputFile(GameDataType tp, string excelFile)
        {
            var excelRelativePath = GetGameDataExcelRelativePath(tp, excelFile);

            string extensionName = GetGameDataExcelOutputFileExtension(tp);
            return UtilityBuiltin.ResPath.GetCombinePath(GetGameDataExcelOutputDir(tp), excelRelativePath + extensionName);
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

        public static string[] GetAllGameDataExcels(GameDataType dtTp, GameDataExcelFileType tps)
        {
            string[] result = new string[0];

            if (dtTp.HasFlag(GameDataType.DataTable))
            {
                var files = GetGameDataExcelAtDir(GetGameDataExcelDir(GameDataType.DataTable), tps);
                if (files != null && files.Length > 0) ArrayUtility.AddRange(ref result, files);
            }
            if (dtTp.HasFlag(GameDataType.Language))
            {
                var files = GetGameDataExcelAtDir(GetGameDataExcelDir(GameDataType.Language), tps);
                if (files != null && files.Length > 0) ArrayUtility.AddRange(ref result, files);
            }
            if (dtTp.HasFlag(GameDataType.Config))
            {
                var files = GetGameDataExcelAtDir(GetGameDataExcelDir(GameDataType.Config), tps);
                if (files != null && files.Length > 0) ArrayUtility.AddRange(ref result, files);
            }
            return result;
        }
        /// <summary>
        /// 获取给定目录下Excel文件, 可以按文件类型筛选结果
        /// </summary>
        /// <param name="excelDir"></param>
        /// <param name="tps"></param>
        /// <returns></returns>
        private static string[] GetGameDataExcelAtDir(string excelDir, GameDataExcelFileType tps)
        {
            string[] result = new string[0];
            if (string.IsNullOrWhiteSpace(excelDir) || !Directory.Exists(excelDir))
            {
                Debug.LogWarning($"获取GameData Excel失败, 给定路径为空或不存在:{excelDir}");
                return result;
            }
            string[] excelFiles = GetFiles(excelDir, "*.xlsx", SearchOption.AllDirectories);

            foreach (var item in excelFiles)
            {
                bool isABFile = IsABTestFile(item);
                if (tps.HasFlag(GameDataExcelFileType.MainFile) && !isABFile)
                {
                    ArrayUtility.Add(ref result, item);
                }
                if (tps.HasFlag(GameDataExcelFileType.ABTestFile) && isABFile)
                {
                    ArrayUtility.Add(ref result, item);
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
        private static string[] GetFiles(string path, string searchPattern, SearchOption option)
        {
            return Directory.GetFiles(path, searchPattern, option).Where(fName => !fName.StartsWith('~')).ToArray();
        }
        /// <summary>
        /// 判断是否为AB测试表
        /// </summary>
        /// <param name="excelFile"></param>
        /// <returns></returns>
        public static bool IsABTestFile(string excelFile)
        {
            var fileName = Path.GetFileNameWithoutExtension(excelFile);
            for (int i = fileName.Length - 1; i >= 0; i--)
            {
                if (fileName[i] == ConstBuiltin.AB_TEST_TAG) return true;
            }
            return false;
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
