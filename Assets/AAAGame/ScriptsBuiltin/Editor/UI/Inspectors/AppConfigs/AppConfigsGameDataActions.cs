#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace UGF.EditorTools
{
    internal static class AppConfigsGameDataActions
    {
        internal static void ValidatePaths()
        {
            GameDataGenerator.ValidateGameDataPaths();
        }

        internal static void ExportAll()
        {
            GameDataGenerator.RefreshAllGameData();
        }

        internal static void CleanGeneratedData()
        {
            GameDataGenerator.CleanGeneratedGameData();
        }

        internal static void ExportSelection(GameDataType gameDataType, string[] selectedItems)
        {
            GameDataGenerator.RunAssetBatch(() =>
            {
                switch (gameDataType)
                {
                    case GameDataType.DataTable:
                        GameDataGenerator.RefreshAllDataTable(GameDataGenerator.GameDataExcelRelative2FullPath(gameDataType, selectedItems));
                        GameDataGenerator.GenerateUIFormNamesScript();
                        GameDataGenerator.GenerateGroupEnumScript();
                        break;

                    case GameDataType.Config:
                        GameDataGenerator.RefreshAllConfig(GameDataGenerator.GameDataExcelRelative2FullPath(gameDataType, selectedItems));
                        break;

                    case GameDataType.Language:
                        GameDataGenerator.RefreshAllLanguage(GameDataGenerator.GameDataExcelRelative2FullPath(gameDataType, selectedItems));
                        break;
                }
            });
        }

        internal static void ExportFrameworkRequiredTable(string tableName)
        {
            var excelPath = GameDataGenerator.GameDataExcelRelative2FullPath(GameDataType.DataTable, tableName);
            GameDataGenerator.RunAssetBatch(() =>
            {
                GameDataGenerator.RefreshAllDataTable(new[] { excelPath });
                if (tableName == ConstEditor.UITableName)
                {
                    GameDataGenerator.GenerateUIFormNamesScript();
                }

                if (tableName == ConstEditor.EntityGroupTableName
                    || tableName == ConstEditor.SoundGroupTableName
                    || tableName == ConstEditor.UIGroupTableName)
                {
                    GameDataGenerator.GenerateGroupEnumScript();
                }
            });
        }

        internal static bool TryCreateExcel(GameDataType gameDataType, string excelDirectory, string excelName, out string excelPath)
        {
            excelPath = null;
            if (string.IsNullOrWhiteSpace(excelName))
            {
                return false;
            }

            excelPath = UtilityBuiltin.AssetsPath.GetCombinePath(excelDirectory, excelName + ".xlsx");
            if (File.Exists(excelPath))
            {
                Debug.LogWarning($"创建{gameDataType}失败, 文件已存在:{excelPath}");
                return false;
            }

            bool created = gameDataType switch
            {
                GameDataType.DataTable => GameDataGenerator.CreateDataTableExcel(excelPath),
                GameDataType.Config => GameDataGenerator.CreateGameConfigExcel(excelPath),
                _ => false
            };
            return created;
        }
    }
}
#endif
