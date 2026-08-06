using System.IO;

namespace UGF.EditorTools
{
    internal static class GameDataWatchService
    {
        public static FileSystemWatcher CreateExcelWatcher(string path, FileSystemEventHandler changedHandler)
        {
            if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
            {
                return null;
            }

            var watcher = new FileSystemWatcher(path, "*.xlsx")
            {
                IncludeSubdirectories = true,
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName,
                EnableRaisingEvents = true
            };
            watcher.Changed += changedHandler;
            watcher.Created += changedHandler;
            watcher.Deleted += changedHandler;
            return watcher;
        }

        public static void DisposeExcelWatcher(ref FileSystemWatcher watcher, FileSystemEventHandler changedHandler)
        {
            if (watcher == null)
            {
                return;
            }

            watcher.EnableRaisingEvents = false;
            watcher.Changed -= changedHandler;
            watcher.Created -= changedHandler;
            watcher.Deleted -= changedHandler;
            watcher.Dispose();
            watcher = null;
        }
    }
}
