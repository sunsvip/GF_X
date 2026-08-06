using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.IO;
using UGF.EditorTools.Data.DataTable;
using UnityEngine;

namespace UGF.EditorTools
{
    internal static class GameDataExcelTemplateService
    {
        private const int MaxCharLength = 255;

        private static IList<KeyValuePair<int, string>> s_DataTableVarTypes;

        internal static bool CreateGameConfigExcel(string excelPath)
        {
            if (File.Exists(excelPath))
            {
                Debug.LogWarning($"创建配置表失败! 文件已存在:{excelPath}");
                return false;
            }

            try
            {
                GameDataGenerator.EnsureFileDirectory(excelPath);
                using var excel = new ExcelPackage(excelPath);
                var sheet = excel.Workbook.Worksheets.Add("Sheet 1");
                sheet.SetValue(1, 1, "#");
                sheet.SetValue(1, 2, Path.GetFileNameWithoutExtension(excelPath));
                sheet.SetValue(2, 1, "#");
                sheet.SetValue(2, 2, "Key");
                sheet.SetValue(2, 3, "备注");
                sheet.SetValue(2, 4, "Value");
                excel.Save();
                return true;
            }
            catch (Exception emsg)
            {
                Debug.LogError($"创建Excel:{excelPath}失败! Error:{emsg}");
                return false;
            }
        }

        internal static bool CreateDataTableExcel(string excelPath)
        {
            if (File.Exists(excelPath))
            {
                Debug.LogWarning($"创建数据表失败! 文件已存在:{excelPath}");
                return false;
            }

            try
            {
                GameDataGenerator.EnsureFileDirectory(excelPath);
                using var excel = new ExcelPackage(excelPath);
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

                s_DataTableVarTypes ??= ScanVariableTypes();
                if (s_DataTableVarTypes != null)
                {
                    var listValidation = sheet.DataValidations.AddListValidation("D3:Z3");
                    listValidation.AllowBlank = false;
                    listValidation.Formula.Values.Clear();
                    foreach (var typeName in s_DataTableVarTypes)
                    {
                        listValidation.Formula.Values.Add(typeName.Value);
                    }
                }

                var i18nValidation = sheet.DataValidations.AddListValidation("D1:Z1");
                i18nValidation.AllowBlank = true;
                i18nValidation.Formula.Values.Clear();
                i18nValidation.Formula.Values.Add(LocalizationTextScanner.EXCEL_I18N_TAG);
                excel.Save();
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
            var types = new List<KeyValuePair<int, string>>(DataTableProcessor.GetDropdownTypes());
            int totalLength = 0;
            int cutIndex = -1;
            for (int i = 0; i < types.Count; i++)
            {
                string item = types[i].Value;
                totalLength += item.Length;
                if (totalLength + i + 1 >= MaxCharLength)
                {
                    break;
                }

                cutIndex = i;
            }

            if (cutIndex < 0)
            {
                return null;
            }

            for (int i = types.Count - 1; i > cutIndex; i--)
            {
                types.RemoveAt(i);
            }

            return types;
        }
    }
}

