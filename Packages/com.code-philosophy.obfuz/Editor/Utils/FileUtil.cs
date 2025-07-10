using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Obfuz.Utils
{
    public static class FileUtil
    {
        public static void CreateParentDir(string path)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
        }

        public static void RemoveDir(string dir, bool log = false)
        {
            if (log)
            {
                UnityEngine.Debug.Log($"removeDir dir:{dir}");
            }

            int maxTryCount = 5;
            for (int i = 0; i < maxTryCount; ++i)
            {
                try
                {
                    if (!Directory.Exists(dir))
                    {
                        return;
                    }
                    foreach (var file in Directory.GetFiles(dir))
                    {
                        File.SetAttributes(file, FileAttributes.Normal);
                        File.Delete(file);
                    }
                    foreach (var subDir in Directory.GetDirectories(dir))
                    {
                        RemoveDir(subDir);
                    }
                    Directory.Delete(dir, true);
                    break;
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogError($"removeDir:{dir} with exception:{e}. try count:{i}");
                    Thread.Sleep(100);
                }
            }
        }

        public static void RecreateDir(string dir)
        {
            if (Directory.Exists(dir))
            {
                RemoveDir(dir, true);
            }
            Directory.CreateDirectory(dir);
        }

        private static void CopyWithCheckLongFile(string srcFile, string dstFile)
        {
            var maxPathLength = 255;
#if UNITY_EDITOR_OSX
            maxPathLength = 1024;
#endif
            if (srcFile.Length > maxPathLength)
            {
                UnityEngine.Debug.LogError($"srcFile:{srcFile} path is too long. skip copy!");
                return;
            }
            if (dstFile.Length > maxPathLength)
            {
                UnityEngine.Debug.LogError($"dstFile:{dstFile} path is too long. skip copy!");
                return;
            }
            File.Copy(srcFile, dstFile);
        }

        public static void CopyDir(string src, string dst, bool log = false)
        {
            if (log)
            {
                UnityEngine.Debug.Log($"copyDir {src} => {dst}");
            }
            RemoveDir(dst);
            Directory.CreateDirectory(dst);
            foreach (var file in Directory.GetFiles(src))
            {
                CopyWithCheckLongFile(file, $"{dst}/{Path.GetFileName(file)}");
            }
            foreach (var subDir in Directory.GetDirectories(src))
            {
                CopyDir(subDir, $"{dst}/{Path.GetFileName(subDir)}");
            }
        }
    }
}
