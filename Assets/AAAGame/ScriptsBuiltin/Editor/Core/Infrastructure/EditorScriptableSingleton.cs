#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditorInternal;
using UnityEngine;

namespace UGF.EditorTools
{
    public class EditorScriptableSingleton<T> : ScriptableObject where T : ScriptableObject
    {
        private static T s_Instance;
        public static T Instance
        {
            get
            {
                if (!s_Instance)
                {
                    LoadOrCreate();
                }
                return s_Instance;
            }
        }
        public static T LoadOrCreate()
        {
            string filePath = GetFilePath();
            if (!string.IsNullOrEmpty(filePath))
            {
                try
                {
                    var arr = InternalEditorUtility.LoadSerializedFileAndForget(filePath);
                    var loadedInstance = arr.Length > 0 ? arr[0] as T : null;
                    s_Instance = loadedInstance ?? s_Instance ?? CreateInstance<T>();
                }
                catch (Exception exception)
                {
                    Debug.LogError($"{nameof(EditorScriptableSingleton<T>)} load failed: {filePath}, Error: {exception.Message}");
                    s_Instance = s_Instance ? s_Instance : CreateInstance<T>();
                }
            }
            else
            {
                Debug.LogError($"{nameof(EditorScriptableSingleton<T>)}: 请设置持久化存档路径！ ");
            }
            return s_Instance;
        }

        public static void Save(bool saveAsText = true)
        {
            if (!s_Instance)
            {
                Debug.LogWarning($"{nameof(EditorScriptableSingleton<T>)} save skipped: instance is null.");
                return;
            }

            string filePath = GetFilePath();
            if (!string.IsNullOrEmpty(filePath))
            {
                try
                {
                    string directoryName = Path.GetDirectoryName(filePath);
                    if (!string.IsNullOrWhiteSpace(directoryName) && !Directory.Exists(directoryName))
                    {
                        Directory.CreateDirectory(directoryName);
                    }

                    UnityEngine.Object[] obj = new T[1] { s_Instance };
                    InternalEditorUtility.SaveToSerializedFileAndForget(obj, filePath, saveAsText);
                }
                catch (Exception exception)
                {
                    Debug.LogError($"{nameof(EditorScriptableSingleton<T>)} save failed: {filePath}, Error: {exception.Message}");
                }
            }
        }
        protected static string GetFilePath()
        {
            return typeof(T).GetCustomAttributes(inherit: true)
                  .OfType<FilePathAttribute>()
                  .FirstOrDefault(v => v != null)
                  ?.filepath;
        }
    }
    [AttributeUsage(AttributeTargets.Class)]
    public class FilePathAttribute : Attribute
    {
        internal string filepath;
        /// <summary>
        /// 单例存放路径
        /// </summary>
        /// <param name="path">相对 Project 路径</param>
        public FilePathAttribute(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentException("Invalid relative path (it is empty)");
            }
            path = path.Replace('\\', '/');
            while (path.Length > 0 && (path[0] == '/' || path[0] == '\\'))
            {
                path = path.Substring(1);
            }
            filepath = path;
        }
    }
}
#endif
