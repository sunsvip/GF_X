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
        private const double ExportDebounceSeconds = 0.8d;

        private static readonly ChangedExcelCollector TableFileChanges = new ChangedExcelCollector();
        private static readonly ChangedExcelCollector ConfigFileChanges = new ChangedExcelCollector();
        private static readonly ChangedExcelCollector LanguageFileChanges = new ChangedExcelCollector();
        private static readonly GameDataRefreshScheduler RefreshScheduler = new GameDataRefreshScheduler(ExportDebounceSeconds);

        private static FileSystemWatcher tableFileWatcher;
        private static FileSystemWatcher configFileWatcher;
        private static FileSystemWatcher languageFileWatcher;

        private static bool isInitialized = false;
        private static AppConfigs appConfigs = null;

        // 由 FileSystemWatcher 后台线程置位, 主线程 OnUpdate 消费. EditorApplication.timeSinceStartup 只能在主线程读取, 故防抖计时不能在 watcher 线程里触发.
        private static volatile bool s_pendingNotify;

        [InitializeOnLoadMethod]
        private static void Init()
        {
            if (isInitialized) return;
            EditorApplication.update -= OnUpdate;
            EditorApplication.update += OnUpdate;
            tableFileWatcher = GameDataWatchService.CreateExcelWatcher(ConstEditor.DataTableExcelPath, OnDataTableChanged);
            configFileWatcher = GameDataWatchService.CreateExcelWatcher(ConstEditor.ConfigExcelPath, OnConfigChanged);
            languageFileWatcher = GameDataWatchService.CreateExcelWatcher(ConstEditor.LanguageExcelPath, OnLanguageChanged);
            appConfigs = AppConfigs.GetInstanceEditor();
            AssemblyReloadEvents.beforeAssemblyReload -= DisposeWatchers;
            AssemblyReloadEvents.beforeAssemblyReload += DisposeWatchers;
            EditorApplication.quitting -= DisposeWatchers;
            EditorApplication.quitting += DisposeWatchers;
            isInitialized = true;
        }

        private static void DisposeWatchers()
        {
            GameDataWatchService.DisposeExcelWatcher(ref tableFileWatcher, OnDataTableChanged);
            GameDataWatchService.DisposeExcelWatcher(ref configFileWatcher, OnConfigChanged);
            GameDataWatchService.DisposeExcelWatcher(ref languageFileWatcher, OnLanguageChanged);
            EditorApplication.update -= OnUpdate;
            AssemblyReloadEvents.beforeAssemblyReload -= DisposeWatchers;
            EditorApplication.quitting -= DisposeWatchers;
            isInitialized = false;
        }

        private static void OnUpdate()
        {
            if (!isInitialized) return;
            if (s_pendingNotify)
            {
                s_pendingNotify = false;
                RefreshScheduler.NotifyChange(EditorApplication.timeSinceStartup);
            }
            var hasPendingChanges = TableFileChanges.HasPending() || ConfigFileChanges.HasPending() || LanguageFileChanges.HasPending();
            if (RefreshScheduler.ShouldDefer(hasPendingChanges, EditorApplication.timeSinceStartup))
            {
                return;
            }

            appConfigs = AppConfigs.GetInstanceEditor();
            if (TableFileChanges.TryConsume(out IList<string> changedTableFiles))
            {
                GameDataGenerator.RunAssetBatch(() =>
                {
                    GameDataGenerator.CleanGeneratedGameDataForMissingExcels(GameDataType.DataTable, changedTableFiles);
                    var changedFiles = GetMainExcelFiles(GameDataType.DataTable, GetWatchedDataTableNames(), changedTableFiles);
                    RemoveMissingFiles(changedFiles);
                    if (changedFiles.Count > 0)
                    {
                        GameDataGenerator.RefreshAllDataTable(changedFiles);
                        if (changedFiles.Contains(ConstEditor.UITableExcelFullPath))
                        {
                            GameDataGenerator.GenerateUIFormNamesScript();
                        }
                        if (changedFiles.Contains(ConstEditor.EntityGroupTableExcelFullPath) ||
                            changedFiles.Contains(ConstEditor.SoundGroupTableExcelFullPath) ||
                            changedFiles.Contains(ConstEditor.UIGroupTableExcelFullPath))
                        {
                            GameDataGenerator.GenerateGroupEnumScript();
                        }
                        foreach (var item in changedFiles)
                        {
                            GFBuiltin.Log($"-----------------自动刷新DataTable:{item}-----------------");
                        }
                    }
                });
            }
            if (ConfigFileChanges.TryConsume(out IList<string> changedConfigFiles))
            {
                GameDataGenerator.RunAssetBatch(() =>
                {
                    GameDataGenerator.CleanGeneratedGameDataForMissingExcels(GameDataType.Config, changedConfigFiles);
                    var changedFiles = GetMainExcelFiles(GameDataType.Config, appConfigs.Configs, changedConfigFiles);
                    RemoveMissingFiles(changedFiles);
                    if (changedFiles.Count > 0)
                    {
                        GameDataGenerator.RefreshAllConfig(changedFiles);
                        foreach (var item in changedFiles)
                        {
                            GFBuiltin.Log($"-----------------自动刷新Config:{item}-----------------");
                        }
                    }
                });
            }
            if (LanguageFileChanges.TryConsume(out IList<string> changedLanguageFiles))
            {
                GameDataGenerator.RunAssetBatch(() =>
                {
                    GameDataGenerator.CleanGeneratedGameDataForMissingExcels(GameDataType.Language, changedLanguageFiles);
                    var changedFiles = GetMainExcelFiles(GameDataType.Language, appConfigs.Languages, changedLanguageFiles);
                    RemoveMissingFiles(changedFiles);
                    if (changedFiles.Count > 0)
                    {
                        GameDataGenerator.RefreshAllLanguage(changedFiles);
                        foreach (var item in changedFiles)
                        {
                            GFBuiltin.Log($"-----------------自动刷新Language{item}-----------------");
                        }
                    }
                });
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
            if (relativeMainFiles == null)
            {
                return result;
            }

            foreach (var changedFile in changedFiles)
            {
                var relativePathNoExt = GameDataGenerator.GetGameDataExcelRelativePath(tp, changedFile);
                foreach (var mainName in relativeMainFiles)
                {
                    var mainExcelFullPath = GameDataGenerator.GameDataExcelRelative2FullPath(tp, mainName);
                    if (string.Equals(relativePathNoExt, mainName, StringComparison.Ordinal) || GameDataGenerator.IsABTestFile(changedFile, mainExcelFullPath))
                    {
                        if (!result.Contains(mainExcelFullPath))
                        {
                            result.Add(mainExcelFullPath);
                        }
                    }
                }
            }
            return result;
        }

        private static void RemoveMissingFiles(IList<string> files)
        {
            for (int i = files.Count - 1; i >= 0; i--)
            {
                if (!File.Exists(files[i]))
                {
                    files.RemoveAt(i);
                }
            }
        }

        private static void OnConfigChanged(object sender, FileSystemEventArgs e)
        {
            AddChangedFile(ConfigFileChanges, e);
        }
        private static void OnDataTableChanged(object sender, FileSystemEventArgs e)
        {
            AddChangedFile(TableFileChanges, e);
        }

        private static void OnLanguageChanged(object sender, FileSystemEventArgs e)
        {
            AddChangedFile(LanguageFileChanges, e);
        }

        private static string[] GetWatchedDataTableNames()
        {
            List<string> tableNames = new List<string>(ConstEditor.FrameworkRequiredDataTables.Length + (appConfigs.DataTables?.Length ?? 0));
            AddTableNames(tableNames, ConstEditor.FrameworkRequiredDataTables);
            AddTableNames(tableNames, appConfigs.DataTables);
            return tableNames.ToArray();
        }

        private static void AddTableNames(List<string> tableNames, IEnumerable<string> source)
        {
            if (source == null)
            {
                return;
            }

            foreach (string tableName in source)
            {
                if (!tableNames.Contains(tableName))
                {
                    tableNames.Add(tableName);
                }
            }
        }

        private static void AddChangedFile(ChangedExcelCollector changedFiles, FileSystemEventArgs e)
        {
            var fName = Path.GetFileNameWithoutExtension(e.Name);
            if (!fName.StartsWith("~$", StringComparison.Ordinal))
            {
                changedFiles.AddUnique(e.FullPath);
                // watcher 运行在后台线程, 不能读取 EditorApplication.timeSinceStartup, 仅置标志由主线程 OnUpdate 触发防抖.
                s_pendingNotify = true;
            }
        }

        internal static void ReloadAppConfigs()
        {
            appConfigs = AppConfigs.GetInstanceEditor();
        }
    }

}
#endif
