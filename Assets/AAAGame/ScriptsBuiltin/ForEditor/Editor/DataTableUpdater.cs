#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;
using System;

namespace UGF.EditorTools
{
    public static partial class DataTableUpdater
    {
        static string[] tableFileChangedList;
        static string[] configFileChangedList;
        static string[] languageFileChangedList;

        static bool isInitialized = false;
        static AppConfigs appConfigs = null;
        [InitializeOnLoadMethod]
        private static void Init()
        {
            if (isInitialized) return;
            tableFileChangedList = new string[0];
            configFileChangedList = new string[0];
            languageFileChangedList = new string[0];
            EditorApplication.update += OnUpdate;
            var tbWatcher = new FileSystemWatcher(ConstEditor.DataTableExcelPath, "*.xlsx");

            tbWatcher.NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite | NotifyFilters.FileName;
            tbWatcher.EnableRaisingEvents = true;
            var fileChangedCb = new FileSystemEventHandler(OnDataTableChanged);
            var fileRenameCb = new RenamedEventHandler(OnDataTableChanged);
            tbWatcher.Changed += fileChangedCb;
            tbWatcher.Deleted += fileChangedCb;
            tbWatcher.Renamed += fileRenameCb;

            var cfgWatcher = new FileSystemWatcher(ConstEditor.ConfigExcelPath, "*.xlsx");
            cfgWatcher.NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite | NotifyFilters.FileName;
            cfgWatcher.EnableRaisingEvents = true;
            var cfgFileChangedCb = new FileSystemEventHandler(OnConfigChanged);
            var cfgFileRenameCb = new RenamedEventHandler(OnConfigChanged);
            cfgWatcher.Changed += cfgFileChangedCb;
            cfgWatcher.Deleted += cfgFileChangedCb;
            cfgWatcher.Renamed += cfgFileRenameCb;

            var langWatcher = new FileSystemWatcher(ConstEditor.LanguageExcelPath, "*.xlsx");
            langWatcher.NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite;
            langWatcher.EnableRaisingEvents = true;
            var langFileChangedCb = new FileSystemEventHandler(OnLanguageChanged);
            langWatcher.Changed += langFileChangedCb;
            langWatcher.Deleted += langFileChangedCb;
            appConfigs = AppConfigs.GetInstanceEditor();
            isInitialized = true;
        }


        private static void OnUpdate()
        {
            if (!isInitialized) return;

            if (tableFileChangedList.Length > 0)
            {
                var changedFiles = GetMainExcelFiles(GameDataType.DataTable, appConfigs.DataTables, tableFileChangedList);
                GameDataGenerator.RefreshAllDataTable(changedFiles);
                if (ArrayUtility.Contains(changedFiles, ConstEditor.UITableExcelFullPath))
                {
                    GameDataGenerator.GenerateUIViewScript();
                }
                if (ArrayUtility.Contains(changedFiles, ConstEditor.EntityGroupTableExcelFullPath) ||
                        ArrayUtility.Contains(changedFiles, ConstEditor.SoundGroupTableExcelFullPath) ||
                        ArrayUtility.Contains(changedFiles, ConstEditor.UIGroupTableExcelFullPath) ||
                        ArrayUtility.Contains(changedFiles, ConstEditor.EntityGroupTableExcelFullPath))
                {
                    GameDataGenerator.GenerateGroupEnumScript();
                }
                foreach (var item in changedFiles)
                {
                    GFBuiltin.LogInfo($"-----------------自动刷新DataTable:{item}-----------------");
                }
                ArrayUtility.Clear(ref tableFileChangedList);
            }
            if (configFileChangedList.Length > 0)
            {
                var changedFiles = GetMainExcelFiles(GameDataType.Config, appConfigs.Configs, configFileChangedList);
                GameDataGenerator.RefreshAllConfig(changedFiles);
                foreach (var item in changedFiles)
                {
                    GFBuiltin.LogInfo($"-----------------自动刷新Config:{item}-----------------");
                }
                ArrayUtility.Clear(ref configFileChangedList);
            }
            if (languageFileChangedList.Length > 0)
            {
                var changedFiles = GetMainExcelFiles(GameDataType.Language, appConfigs.Languages, languageFileChangedList);
                GameDataGenerator.RefreshAllLanguage(changedFiles);
                foreach (var item in changedFiles)
                {
                    GFBuiltin.LogInfo($"-----------------自动刷新Language:{item}-----------------");
                }
                ArrayUtility.Clear(ref languageFileChangedList);
            }
        }
        /// <summary>
        /// 根据改变的Excel列表获取所有对应的主文件列表
        /// </summary>
        /// <param name="tp"></param>
        /// <param name="relativeMainFiles"></param>
        /// <param name="changedFiles"></param>
        /// <returns></returns>
        private static string[] GetMainExcelFiles(GameDataType tp, string[] relativeMainFiles, string[] changedFiles)
        {
            string[] result = new string[0];
            foreach (var changedFile in changedFiles)
            {
                var relativePathNoExt = GameDataGenerator.GetGameDataExcelRelativePath(tp, changedFile);
                foreach (var mainName in relativeMainFiles)
                {
                    if (relativePathNoExt.CompareTo(mainName) == 0 || relativePathNoExt.StartsWith(mainName + ConstBuiltin.AB_TEST_TAG))
                    {
                        var mainExcelFullPath = GameDataGenerator.GameDataExcelRelative2FullPath(tp, mainName);
                        if (!ArrayUtility.Contains(result, mainExcelFullPath))
                        {
                            ArrayUtility.Add(ref result, mainExcelFullPath);
                        }
                    }
                }
            }
            return result;
        }

        private static void OnConfigChanged(object sender, FileSystemEventArgs e)
        {
            var fName = Path.GetFileNameWithoutExtension(e.Name);
            if (!fName.StartsWith('~'))
            {
                ArrayUtility.Add(ref configFileChangedList, e.FullPath);
            }
        }
        private static void OnDataTableChanged(object sender, FileSystemEventArgs e)
        {
            var fName = Path.GetFileNameWithoutExtension(e.Name);
            if (!fName.StartsWith('~'))
            {
                ArrayUtility.Add(ref tableFileChangedList, e.FullPath);
            }
        }

        private static void OnLanguageChanged(object sender, FileSystemEventArgs e)
        {
            var fName = Path.GetFileNameWithoutExtension(e.Name);
            if (!fName.StartsWith('~'))
            {
                ArrayUtility.Add(ref languageFileChangedList, e.FullPath);
            }
        }
    }

}
#endif