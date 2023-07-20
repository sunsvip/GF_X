using GameFramework;
using HybridCLR;
using HybridCLR.Editor;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace UGF.EditorTools
{
    public class StripLinkConfigTool
    {
        public const string LinkFile = "Assets/link.xml";
        public const string STRIP_GENERATE_TAG = "<!--GENERATE_TAG-->";
        private const string MatchPattern = "<assembly[\\s]+fullname[\\s]*=[\\s]*\"([^\"]+)\"";

        /// <summary>
        /// 获取项目全部dll
        /// </summary>
        /// <returns></returns>
        public static string[] GetProjectAssemblyDlls()
        {
            List<string> dlls = new List<string>();
            var dllDir = HybridCLR.Editor.SettingsUtil.GetAssembliesPostIl2CppStripDir(EditorUserBuildSettings.activeBuildTarget);
            if (!Directory.Exists(dllDir))
            {
                return dlls.ToArray();
            }
            var files = Directory.GetFiles(dllDir, "*.dll", SearchOption.AllDirectories);
            foreach (var file in files)
            {
                var fileName = Path.GetFileNameWithoutExtension(file);
                if (!dlls.Contains(fileName)) dlls.Add(fileName);
            }
            return dlls.ToArray();
        }
        /// <summary>
        /// 获取已经配置到link.xml里的dll
        /// </summary>
        /// <returns></returns>
        public static string[] GetSelectedAssemblyDlls()
        {
            List<string> dlls = new List<string>();
            if (!File.Exists(LinkFile))
            {
                return dlls.ToArray();
            }
            var lines = File.ReadAllLines(LinkFile);
            int generateBeginLine = lines.Length, generateEndLine = lines.Length;
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                if (generateBeginLine >= lines.Length && line.Trim().CompareTo(STRIP_GENERATE_TAG) == 0)
                {
                    generateBeginLine = i;
                }
                else if (generateEndLine >= lines.Length && line.Trim().CompareTo(STRIP_GENERATE_TAG) == 0)
                {
                    generateEndLine = i;
                }
                if (((i > generateBeginLine && generateEndLine >= lines.Length) || (i > generateBeginLine && i < generateEndLine)) && !string.IsNullOrWhiteSpace(line))
                {
                    var match = Regex.Match(line, MatchPattern);
                    if (match.Success)
                    {
                        var assemblyName = match.Result("$1");
                        if (!dlls.Contains(assemblyName)) dlls.Add(assemblyName);
                    }
                }

            }
            return dlls.ToArray();
        }

        internal static string[] GetSelectedAotDlls()
        {
            return HybridCLRSettings.Instance.patchAOTAssemblies;
        }
        public static bool Save2LinkFile(string[] stripList)
        {
            if (!File.Exists(LinkFile))
            {
                File.WriteAllText(LinkFile, $"<linker>{Environment.NewLine}{STRIP_GENERATE_TAG}{Environment.NewLine}{STRIP_GENERATE_TAG}</linker>");
            }
            var lines = File.ReadAllLines(LinkFile);
            FindGenerateLine(lines, out int beginLineIdx, out int endLineIdx);
            int headIdx = ArrayUtility.FindIndex(lines, line => line.Trim().CompareTo("<linker>") == 0);
            if (beginLineIdx >= lines.Length)
            {
                ArrayUtility.Insert(ref lines, headIdx + 1, STRIP_GENERATE_TAG);
            }
            if (endLineIdx >= lines.Length)
            {
                ArrayUtility.Insert(ref lines, headIdx + 1, STRIP_GENERATE_TAG);
            }
            FindGenerateLine(lines, out beginLineIdx, out endLineIdx);
            int insertIdx = beginLineIdx;
            for (int i = 0; i < stripList.Length; i++)
            {
                insertIdx = beginLineIdx + i + 1;
                if (insertIdx >= endLineIdx)
                {
                    ArrayUtility.Insert(ref lines, endLineIdx, FormatStripLine(stripList[i]));
                }
                else
                {
                    lines[insertIdx] = FormatStripLine(stripList[i]);
                }
            }
            while ((insertIdx + 1) < lines.Length && lines[insertIdx + 1].Trim().CompareTo(STRIP_GENERATE_TAG) != 0)
            {
                ArrayUtility.RemoveAt(ref lines, insertIdx + 1);
            }
            try
            {
                File.WriteAllLines(LinkFile, lines, System.Text.Encoding.UTF8);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogErrorFormat("Save2LinkFile Failed:{0}", e.Message);
                return false;
            }
        }

        internal static bool Save2AotDllList(string[] strings)
        {
            HybridCLRSettings.Instance.patchAOTAssemblies = strings;
            HybridCLRExtensionTool.CopyAotDllsToProject(EditorUserBuildSettings.activeBuildTarget);
            AssetDatabase.Refresh();
            return true;
        }
        private static string FormatStripLine(string assemblyName)
        {
            return $"\t<assembly fullname=\"{assemblyName}\" preserve=\"all\" />";
        }
        private static void FindGenerateLine(string[] lines, out int beginLineIdx, out int endLineIdx)
        {
            beginLineIdx = endLineIdx = lines.Length;
            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                if (beginLineIdx >= lines.Length && line.Trim().CompareTo(STRIP_GENERATE_TAG) == 0)
                {
                    beginLineIdx = i;
                }
                else if (endLineIdx >= lines.Length && line.Trim().CompareTo(STRIP_GENERATE_TAG) == 0)
                {
                    endLineIdx = i;
                }
            }
        }
    }

}
