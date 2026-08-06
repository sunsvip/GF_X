using GameFramework.Localization;
using OfficeOpenXml;
using OfficeOpenXml.Drawing.Controls;
using OfficeOpenXml.Style;
using System;
using System.Collections.Generic;
using System.IO;

namespace UGF.EditorTools
{
    internal static class LocalizationLanguageExcelRepository
    {
        private const string LockToggleName = "锁定";
        private const string LockTips = "勾选锁定此行,锁定后将强制保留";
        private const int LockColumn = 1;
        private const int KeyColumn = 2;
        private const int ValueColumn = 3;
        private const string CellKeyTips = "多语言Key";
        private const string CellValueTips = "多语言Value, 当值为空时, [一键翻译]会自动填充Value值";

        internal static void SaveLanguage(Language language, List<LocalizationText> texts)
        {
            var excelName = GetLanguageExcelFileName(language);
            try
            {
                var excelFileInfo = new FileInfo(excelName);
                using var excel = new ExcelPackage(excelFileInfo);
                ExcelWorksheet sheet = excel.Workbook.Worksheets.Count < 1
                    ? excel.Workbook.Worksheets.Add("Sheet1")
                    : excel.Workbook.Worksheets[0];

                sheet.Cells.Clear();
                sheet.Drawings.Clear();
                for (var i = 0; i < texts.Count; i++)
                {
                    var rowIndex = i + 1;
                    var lanText = texts[i];
                    var lockCell = sheet.Cells[rowIndex, LockColumn];
                    var checkBox = sheet.Drawings.AddCheckBoxControl(lockCell.Address);
                    checkBox.Checked = lanText.Locked ? eCheckState.Checked : eCheckState.Unchecked;
                    lockCell.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    lockCell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    checkBox.Text = LockToggleName;
                    checkBox.LockedText = true;
                    checkBox.SetSize(30, 20);
                    checkBox.SetPosition(i, 0, 0, 0);
                    checkBox.AdjustPositionAndSize();

                    var keyCell = sheet.Cells[rowIndex, KeyColumn];
                    keyCell.Value = lanText.Key;
                    var valueCell = sheet.Cells[rowIndex, ValueColumn];
                    valueCell.Value = lanText.Value;
                    if (i == 0)
                    {
                        (lockCell.Comment ?? lockCell.AddComment("")).Text = LockTips;
                        (keyCell.Comment ?? keyCell.AddComment("")).Text = CellKeyTips;
                        (valueCell.Comment ?? valueCell.AddComment("")).Text = CellValueTips;
                    }
                }

                try
                {
                    sheet.Column(LockColumn).AutoFit();
                    sheet.Column(KeyColumn).AutoFit(20, 50);
                    sheet.Column(ValueColumn).AutoFit(20, 50);
                }
                catch (Exception autoFitException)
                {
                    UnityEngine.Debug.LogWarning($"AutoFit列宽失败, 已回退为固定列宽(可能为无头/无GDI+环境): {autoFitException.Message}");
                    sheet.Column(LockColumn).Width = 10;
                    sheet.Column(KeyColumn).Width = 50;
                    sheet.Column(ValueColumn).Width = 50;
                }

                excel.Save();
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException(
                    $"保存语言Excel失败: {excelName}。请确认文件未被其它程序占用。详细信息: {exception.Message}",
                    exception);
            }
        }

        internal static void LoadLanguageExcelTexts(Language language, ref List<LocalizationText> localizationTexts)
        {
            if (language == Language.Unspecified)
            {
                return;
            }

            LoadLanguageExcelTexts(GetLanguageExcelFileName(language), ref localizationTexts);
        }

        internal static void LoadLanguageExcelTexts(string languageExcelFile, ref List<LocalizationText> localizationTexts)
        {
            localizationTexts.Clear();
            if (!File.Exists(languageExcelFile))
            {
                return;
            }

            try
            {
                using FileStream excelStream = new FileStream(languageExcelFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var excelPackage = new ExcelPackage(excelStream);
                if (excelPackage.Workbook.Worksheets.Count <= 0)
                {
                    return;
                }

                var excelSheet = excelPackage.Workbook.Worksheets[0];
                if (excelSheet.Dimension == null)
                {
                    return;
                }

                for (var rowIndex = excelSheet.Dimension.Start.Row; rowIndex <= excelSheet.Dimension.End.Row; rowIndex++)
                {
                    var key = excelSheet.GetValue<string>(rowIndex, KeyColumn);
                    var value = excelSheet.GetValue<string>(rowIndex, ValueColumn);
                    if (string.IsNullOrWhiteSpace(key))
                    {
                        continue;
                    }

                    var langText = new LocalizationText
                    {
                        Key = key,
                        Value = value
                    };
                    var cell = excelSheet.Cells[rowIndex, LockColumn];
                    var checkBox = excelSheet.Drawings[cell.Address] as ExcelControlCheckBox;
                    langText.Locked = checkBox != null && checkBox.Checked == eCheckState.Checked;
                    localizationTexts.Add(langText);
                }
            }
            catch (Exception exception)
            {
                UnityEngine.Debug.LogError($"读取语言本地化文本列表失败:{exception.Message}");
            }
        }

        internal static string GetLanguageExcelFileName(Language language)
        {
            return UtilityBuiltin.AssetsPath.GetCombinePath(ConstEditor.LanguageExcelPath, GameFramework.Utility.Text.Format("{0}.xlsx", language.ToString()));
        }
    }
}
