using GameFramework;
using UnityEditor;
using UnityEngine;

namespace UGF.EditorTools
{
    internal static class EditorDeferredTaskQueue
    {
        private const string AddScriptTaskSessionKey = "UGF.EditorTools.ProjectPanelMenuCommands.AddScriptTask";
        private const char TaskSeparator = '\n';
        private const char FieldSeparator = '\t';

        public static void EnqueueComponentAttachTask(string prefabAssetPath, string scriptAssetPath)
        {
            var currentQueue = SessionState.GetString(AddScriptTaskSessionKey, string.Empty);
            var newTask = $"{prefabAssetPath}{FieldSeparator}{scriptAssetPath}";
            SessionState.SetString(AddScriptTaskSessionKey, string.IsNullOrWhiteSpace(currentQueue) ? newTask : $"{currentQueue}{TaskSeparator}{newTask}");
        }

        [InitializeOnLoadMethod]
        private static void ProcessPendingTasks()
        {
            while (TryDequeueComponentAttachTask(out var prefabAssetPath, out var scriptAssetPath))
            {
                var targetPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabAssetPath);
                var monoScript = AssetDatabase.LoadAssetAtPath<MonoScript>(scriptAssetPath);
                if (targetPrefab == null || monoScript == null)
                {
                    continue;
                }

                var monoType = monoScript.GetClass();
                if (monoType == null)
                {
                    continue;
                }

                targetPrefab.GetOrAddComponent(monoType);
                // LoadAssetAtPath 返回的即 prefab 资产根, 挂载组件后需显式保存, 否则组件不会持久化到磁盘.
                PrefabUtility.SavePrefabAsset(targetPrefab);
            }
        }

        private static bool TryDequeueComponentAttachTask(out string prefabAssetPath, out string scriptAssetPath)
        {
            prefabAssetPath = null;
            scriptAssetPath = null;

            var taskInfo = SessionState.GetString(AddScriptTaskSessionKey, string.Empty);
            if (string.IsNullOrWhiteSpace(taskInfo))
            {
                return false;
            }

            var taskLines = taskInfo.Split(new[] { TaskSeparator }, System.StringSplitOptions.RemoveEmptyEntries);
            if (taskLines.Length <= 0)
            {
                SessionState.EraseString(AddScriptTaskSessionKey);
                return false;
            }

            var infos = taskLines[0].Split(FieldSeparator);

            // 从会话状态中移除首行(写回其余行, 否则清空), 保证畸形行不会卡住整个队列.
            if (taskLines.Length > 1)
            {
                SessionState.SetString(AddScriptTaskSessionKey, string.Join(TaskSeparator.ToString(), taskLines, 1, taskLines.Length - 1));
            }
            else
            {
                SessionState.EraseString(AddScriptTaskSessionKey);
            }

            if (infos.Length != 2)
            {
                // 畸形任务行: 已从队列移除, 跳过它继续处理后续任务, 而非阻塞整个队列.
                return TryDequeueComponentAttachTask(out prefabAssetPath, out scriptAssetPath);
            }

            prefabAssetPath = infos[0];
            scriptAssetPath = infos[1];
            return true;
        }
    }
}
