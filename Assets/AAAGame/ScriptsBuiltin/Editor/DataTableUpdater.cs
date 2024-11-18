#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;
using System;
using System.Collections.Generic;

namespace UGF.EditorTools
{
    public static partial class DataTableUpdater
    {
        static IList<string> tableFileChangedList;
        static IList<string> configFileChangedList;
        static IList<string> languageFileChangedList;

        static bool isInitialized = false;
        static AppConfigs appConfigs = null;
        [InitializeOnLoadMethod]
        private static void Init()
        {
            if (isInitialized) return;
            tableFileChangedList = new List<string>();
            configFileChangedList = new List<string>();
            languageFileChangedList = new List<string>();
            EditorApplication.update -= OnUpdate;
            EditorApplication.update += OnUpdate;
            var tbWatcher = new FileSystemWatcher(ConstEditor.DataTableExcelPath, "*.xlsx");
            tbWatcher.IncludeSubdirectories = true;

            tbWatcher.NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite | NotifyFilters.FileName;
            tbWatcher.EnableRaisingEvents = true;
            var fileChangedCb = new FileSystemEventHandler(OnDataTableChanged);
            var fileRenameCb = new RenamedEventHandler(OnDataTableChanged);
            tbWatcher.Changed -= fileChangedCb;
            tbWatcher.Changed += fileChangedCb;
            tbWatcher.Deleted -= fileChangedCb;
            tbWatcher.Deleted += fileChangedCb;
            tbWatcher.Renamed -= fileRenameCb;
            tbWatcher.Renamed += fileRenameCb;

            var cfgWatcher = new FileSystemWatcher(ConstEditor.ConfigExcelPath, "*.xlsx");
            cfgWatcher.IncludeSubdirectories = true;
            cfgWatcher.NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite | NotifyFilters.FileName;
            cfgWatcher.EnableRaisingEvents = true;
            var cfgFileChangedCb = new FileSystemEventHandler(OnConfigChanged);
            var cfgFileRenameCb = new RenamedEventHandler(OnConfigChanged);
            cfgWatcher.Changed -= cfgFileChangedCb;
            cfgWatcher.Changed += cfgFileChangedCb;
            cfgWatcher.Deleted -= cfgFileChangedCb;
            cfgWatcher.Deleted += cfgFileChangedCb;
            cfgWatcher.Renamed -= cfgFileRenameCb;
            cfgWatcher.Renamed += cfgFileRenameCb;

            var langWatcher = new FileSystemWatcher(ConstEditor.LanguageExcelPath, "*.xlsx");
            langWatcher.IncludeSubdirectories = true;
            langWatcher.NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite;
            langWatcher.EnableRaisingEvents = true;
            var langFileChangedCb = new FileSystemEventHandler(OnLanguageChanged);
            langWatcher.Changed -= langFileChangedCb;
            langWatcher.Changed += langFileChangedCb;
            langWatcher.Deleted -= langFileChangedCb;
            langWatcher.Deleted += langFileChangedCb;
            appConfigs = AppConfigs.GetInstanceEditor();
            isInitialized = true;
        }


        private static void OnUpdate()
        {
            if (!isInitialized) return;

            if (tableFileChangedList.Count > 0)
            {
                var changedFiles = GetMainExcelFiles(GameDataType.DataTable, appConfigs.DataTables, tableFileChangedList);
                GameDataGenerator.RefreshAllDataTable(changedFiles);
                if (changedFiles.Contains(ConstEditor.UITableExcelFullPath))
                {
                    GameDataGenerator.GenerateUIViewScript();
                }
                if (changedFiles.Contains(ConstEditor.EntityGroupTableExcelFullPath) ||
                        changedFiles.Contains(ConstEditor.SoundGroupTableExcelFullPath) ||
                        changedFiles.Contains(ConstEditor.UIGroupTableExcelFullPath) ||
                        changedFiles.Contains(ConstEditor.EntityGroupTableExcelFullPath))
                {
                    GameDataGenerator.GenerateGroupEnumScript();
                }
                foreach (var item in changedFiles)
                {
                    GFBuiltin.LogInfo($"-----------------自动刷新DataTable:{item}-----------------");
                }
                tableFileChangedList.Clear();
            }
            if (configFileChangedList.Count > 0)
            {
                var changedFiles = GetMainExcelFiles(GameDataType.Config, appConfigs.Configs, configFileChangedList);
                GameDataGenerator.RefreshAllConfig(changedFiles);
                foreach (var item in changedFiles)
                {
                    GFBuiltin.LogInfo($"-----------------自动刷新Config:{item}-----------------");
                }
                configFileChangedList.Clear();
            }
            if (languageFileChangedList.Count > 0)
            {
                var changedFiles = GetMainExcelFiles(GameDataType.Language, appConfigs.Languages, languageFileChangedList);
                GameDataGenerator.RefreshAllLanguage(changedFiles);
                foreach (var item in changedFiles)
                {
                    GFBuiltin.LogInfo($"-----------------自动刷新Language:{item}-----------------");
                }
                languageFileChangedList.Clear();
            }
        }
        /// <summary>
        /// 根据改变的Excel列表获取所有对应的主文件列表
        /// </summary>
        /// <param name="tp"></param>
        /// <param name="relativeMainFiles"></param>
        /// <param name="changedFiles"></param>
        /// <returns></returns>
        private static IList<string> GetMainExcelFiles(GameDataType tp, IList<string> relativeMainFiles, IList<string> changedFiles)
        {
            IList<string> result = new List<string>();
            foreach (var changedFile in changedFiles)
            {
                var relativePathNoExt = GameDataGenerator.GetGameDataExcelRelativePath(tp, changedFile);
                foreach (var mainName in relativeMainFiles)
                {
                    if (relativePathNoExt.CompareTo(mainName) == 0 || relativePathNoExt.StartsWith(mainName + ConstBuiltin.AB_TEST_TAG))
                    {
                        var mainExcelFullPath = GameDataGenerator.GameDataExcelRelative2FullPath(tp, mainName);
                        if (!result.Contains(mainExcelFullPath))
                        {
                            result.Add(mainExcelFullPath);
                        }
                    }
                }
            }
            return result;
        }

        private static void OnConfigChanged(object sender, FileSystemEventArgs e)
        {
            var fName = Path.GetFileNameWithoutExtension(e.Name);
            if (!fName.StartsWith("~$"))
            {
                configFileChangedList.Add(e.FullPath);
            }
        }
        private static void OnDataTableChanged(object sender, FileSystemEventArgs e)
        {
            var fName = Path.GetFileNameWithoutExtension(e.Name);
            if (!fName.StartsWith("~$"))
            {
                tableFileChangedList.Add(e.FullPath);
            }
        }

        private static void OnLanguageChanged(object sender, FileSystemEventArgs e)
        {
            var fName = Path.GetFileNameWithoutExtension(e.Name);
            if (!fName.StartsWith("~$"))
            {
                languageFileChangedList.Add(e.FullPath);
            }
        }
    }

}
#endif
