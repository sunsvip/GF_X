using GameFramework;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

namespace UGF.EditorTools
{
    internal static class GameDataScriptGenerationService
    {
        internal static void GenerateGroupEnumScript()
        {
            string excelDir = ConstEditor.DataTableExcelPath;
            if (!Directory.Exists(excelDir))
            {
                Debug.LogErrorFormat("Excel DataTable directory is not exists:{0}", excelDir);
                return;
            }

            string[] groupExcels = { ConstEditor.EntityGroupTableExcel, ConstEditor.UIGroupTableExcel, ConstEditor.SoundGroupTableExcel };
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("//此代码由工具自动生成, 请勿手动修改");
            builder.AppendLine("public static partial class Const");
            builder.AppendLine("{");
            foreach (string excel in groupExcels)
            {
                string excelFileName = UtilityBuiltin.AssetsPath.GetCombinePath(excelDir, excel);
                if (!File.Exists(excelFileName))
                {
                    Debug.LogErrorFormat("Excel is not exists:{0}", excelFileName);
                    return;
                }

                List<string> groupList = new List<string>();
                try
                {
                    // 与导出路径一致的共享只读打开方式(FileShare.ReadWrite), 避免 Excel 正被打开时构造抛 IOException 冒泡出刷新流程.
                    using var excelStream = new FileStream(excelFileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    using var excelPackage = new ExcelPackage(excelStream);
                    var excelSheet = excelPackage.Workbook.Worksheets[0];
                    if (excelSheet.Dimension == null)
                    {
                        Debug.LogWarningFormat("Excel is empty:{0}", excelFileName);
                        continue;
                    }

                    for (int rowIndex = excelSheet.Dimension.Start.Row; rowIndex <= excelSheet.Dimension.End.Row; rowIndex++)
                    {
                        object rowStr = excelSheet.GetValue(rowIndex, 1);
                        if (rowStr != null && rowStr.ToString().StartsWith("#", StringComparison.Ordinal))
                        {
                            continue;
                        }

                        string groupName = excelSheet.GetValue<string>(rowIndex, 4);
                        if (string.IsNullOrWhiteSpace(groupName) || groupList.Contains(groupName))
                        {
                            continue;
                        }

                        if (!groupList.Contains(groupName))
                        {
                            groupList.Add(groupName);
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.LogErrorFormat("读取Excel失败:{0}, Error:{1}", excelFileName, e.Message);
                    return;
                }

                string className = Path.GetFileNameWithoutExtension(excelFileName);
                const string endWithStr = "Table";
                if (className.EndsWith(endWithStr, StringComparison.Ordinal))
                {
                    className = className[..^endWithStr.Length];
                }

                builder.AppendLine("#if " + HybridCLRExtensionTool.ENABLE_OBFUZ);
                builder.AppendLine("\t[Obfuz.ObfuzIgnore]");
                builder.AppendLine("#endif");
                builder.AppendLine(Utility.Text.Format("\tpublic enum {0}", className));
                builder.AppendLine("\t{");
                for (int i = 0; i < groupList.Count; i++)
                {
                    builder.AppendLine(i < groupList.Count - 1
                        ? Utility.Text.Format("\t\t{0},", groupList[i])
                        : Utility.Text.Format("\t\t{0}", groupList[i]));
                }

                builder.AppendLine("\t}");
            }

            builder.AppendLine("}");

            string outFileName = ConstEditor.ConstGroupScriptFileFullName;
            try
            {
                WriteAllTextAtomically(outFileName, builder.ToString(), GameDataGenerator.Utf8NoBom);
                Debug.LogFormat("------------------成功生成Group文件:{0}---------------", outFileName);
            }
            catch (Exception e)
            {
                Debug.LogErrorFormat("Group文件生成失败:{0}", e.Message);
                throw;
            }
        }

        internal static void GenerateUIFormNamesScript()
        {
            string excelDir = ConstEditor.DataTableExcelPath;
            if (!Directory.Exists(excelDir))
            {
                Debug.LogError($"生成UIView代码失败! 不存在文件夹:{excelDir}");
                return;
            }

            string excelFileName = UtilityBuiltin.AssetsPath.GetCombinePath(excelDir, ConstEditor.UITableExcel);
            if (!File.Exists(excelFileName))
            {
                Debug.LogError($"{excelFileName} 文件不存在!");
                return;
            }

            Dictionary<int, string> uiViewDic = new Dictionary<int, string>();
            try
            {
                // 与导出路径一致的共享只读打开方式(FileShare.ReadWrite), 避免 Excel 正被打开时构造抛 IOException 冒泡出刷新流程.
                using var excelStream = new FileStream(excelFileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var excelPackage = new ExcelPackage(excelStream);
                var excelSheet = excelPackage.Workbook.Worksheets[0];
                if (excelSheet.Dimension == null)
                {
                    Debug.LogError($"生成UIView代码失败! Excel为空:{excelFileName}");
                    return;
                }

                for (int rowIndex = excelSheet.Dimension.Start.Row; rowIndex <= excelSheet.Dimension.End.Row; rowIndex++)
                {
                    object rowStr = excelSheet.GetValue(rowIndex, 1);
                    if (rowStr != null && rowStr.ToString().StartsWith("#", StringComparison.Ordinal))
                    {
                        continue;
                    }

                    string idText = excelSheet.GetValue<string>(rowIndex, 2);
                    string uiViewPath = excelSheet.GetValue<string>(rowIndex, 5);
                    if (string.IsNullOrWhiteSpace(idText) || string.IsNullOrWhiteSpace(uiViewPath))
                    {
                        continue;
                    }

                    if (!int.TryParse(idText, NumberStyles.Integer, CultureInfo.InvariantCulture, out int id))
                    {
                        Debug.LogWarning($"生成UIView代码时跳过非法ID行: {excelFileName} row={rowIndex}, id={idText}");
                        continue;
                    }

                    if (uiViewDic.ContainsKey(id))
                    {
                        Debug.LogWarning($"生成UIView代码时跳过重复ID行: {excelFileName} row={rowIndex}, id={id}");
                        continue;
                    }

                    uiViewDic.Add(id, uiViewPath);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"生成UIView代码失败! 读取Excel出错:{excelFileName}, Error:{e.Message}");
                return;
            }

            string className = Path.GetFileNameWithoutExtension(ConstEditor.UIViewScriptFile);
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("/**此代码由工具自动生成,请勿手动修改!**/");
            builder.AppendLine("#if " + HybridCLRExtensionTool.ENABLE_OBFUZ);
            builder.AppendLine("[Obfuz.ObfuzIgnore]");
            builder.AppendLine("#endif");
            builder.AppendLine(Utility.Text.Format("public enum {0} : int", className));
            builder.AppendLine("{");
            int curIndex = 0;
            foreach (KeyValuePair<int, string> uiItem in uiViewDic)
            {
                string uiViewName = Path.GetFileName(uiItem.Value);
                bool isLast = curIndex == uiViewDic.Count - 1;
                builder.AppendLine(Utility.Text.Format("\t{0} = {1}{2}", uiViewName, uiItem.Key, isLast ? string.Empty : ","));
                curIndex++;
            }

            builder.AppendLine("}");
            try
            {
                WriteAllTextAtomically(ConstEditor.UIViewScriptFile, builder.ToString(), GameDataGenerator.Utf8NoBom);
                Debug.LogFormat("-------------------成功生成:{0}-----------------", ConstEditor.UIViewScriptFile);
            }
            catch (Exception exception)
            {
                Debug.LogErrorFormat("UIView代码生成失败:{0}", exception.Message);
                throw;
            }
        }

        private static void WriteAllTextAtomically(string path, string content, Encoding encoding)
        {
            string tempPath = path + ".tmp";
            File.WriteAllText(tempPath, content, encoding);
            try
            {
                if (File.Exists(path))
                {
                    File.Replace(tempPath, path, null);
                }
                else
                {
                    File.Move(tempPath, path);
                }
            }
            catch
            {
                if (File.Exists(tempPath))
                {
                    File.Delete(tempPath);
                }

                throw;
            }
        }
    }
}
