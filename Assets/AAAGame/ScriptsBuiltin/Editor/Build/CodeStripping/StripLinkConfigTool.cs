using HybridCLR;
using HybridCLR.Editor;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using UnityEditor;
using UnityEngine;

namespace UGF.EditorTools
{
    public class StripLinkConfigTool
    {
        public const string LinkFile = "Assets/link.xml";
        public const string STRIP_GENERATE_TAG = "<!--GENERATE_TAG-->";
        private const string GenerateTagValue = "GENERATE_TAG";

        /// <summary>
        /// 获取项目全部dll
        /// </summary>
        public static string[] GetProjectAssemblyDlls()
        {
            var dlls = new List<string>();
#if ENABLE_HYBRIDCLR
            var dllDir = HybridCLR.Editor.SettingsUtil.GetAssembliesPostIl2CppStripDir(EditorUserBuildSettings.activeBuildTarget);
#else
            var dllDir = HybridCLRExtensionTool.GetStripAssembliesDir(EditorUserBuildSettings.activeBuildTarget);
#endif
            if (!Directory.Exists(dllDir))
            {
                return dlls.ToArray();
            }

            var files = Directory.GetFiles(dllDir, "*.dll", SearchOption.AllDirectories);
            foreach (var file in files)
            {
                var fileName = Path.GetFileNameWithoutExtension(file);
                if (!dlls.Contains(fileName))
                {
                    dlls.Add(fileName);
                }
            }

            return dlls.ToArray();
        }

        /// <summary>
        /// 获取已经配置到link.xml里且位于自动生成区间内的dll
        /// </summary>
        public static string[] GetSelectedAssemblyDlls()
        {
            if (!TryLoadLinkDocument(createIfMissing: false, out var xmlDocument, out var linkerElement))
            {
                return Array.Empty<string>();
            }

            EnsureManagedRegion(xmlDocument, linkerElement, out var beginTag, out var endTag);
            var dlls = new List<string>();
            for (var node = beginTag.NextSibling; node != null && node != endTag; node = node.NextSibling)
            {
                if (node is not XmlElement assemblyElement || !string.Equals(assemblyElement.Name, "assembly", StringComparison.Ordinal))
                {
                    continue;
                }

                var assemblyName = assemblyElement.GetAttribute("fullname");
                if (!string.IsNullOrWhiteSpace(assemblyName) && !dlls.Contains(assemblyName))
                {
                    dlls.Add(assemblyName);
                }
            }

            return dlls.ToArray();
        }

        internal static string[] GetSelectedNetframeworkDlls()
        {
            return AppBuilderEditorSettings.Instance.Netstandard2NetFrameworkList;
        }

        internal static string[] GetSelectedAotDlls()
        {
            return HybridCLR.Editor.Settings.HybridCLRSettings.Instance.patchAOTAssemblies;
        }

        public static bool Save2LinkFile(string[] stripList)
        {
            if (!TryLoadLinkDocument(createIfMissing: true, out var xmlDocument, out var linkerElement))
            {
                return false;
            }

            EnsureManagedRegion(xmlDocument, linkerElement, out var beginTag, out var endTag);
            ClearManagedRegion(beginTag, endTag, linkerElement);

            var uniqueAssemblyNames = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < stripList.Length; i++)
            {
                var assemblyName = stripList[i];
                if (string.IsNullOrWhiteSpace(assemblyName) || !uniqueAssemblyNames.Add(assemblyName))
                {
                    continue;
                }

                InsertManagedAssembly(xmlDocument, linkerElement, endTag, assemblyName);
            }

            try
            {
                SaveLinkDocument(xmlDocument);
                AssetDatabase.Refresh();
                return true;
            }
            catch (Exception e)
            {
                Debug.LogErrorFormat("Save2LinkFile Failed:{0}", e.Message);
                return false;
            }
        }

        internal static bool SaveNetstandard2NetFrameworkConfig(string[] strings)
        {
            AppBuilderEditorSettings.Instance.Netstandard2NetFrameworkList = strings;
            AppBuilderEditorSettings.Save();

            HybridCLRExtensionTool.CopyNetFrameworkDllToProject(strings);
            AssetDatabase.Refresh();
            return true;
        }

        internal static bool Save2AotDllList(string[] strings)
        {
            HybridCLR.Editor.Settings.HybridCLRSettings.Instance.patchAOTAssemblies = strings;
            HybridCLR.Editor.Settings.HybridCLRSettings.Save();
            HybridCLRExtensionTool.CopyAotDllsToProject(EditorUserBuildSettings.activeBuildTarget);
            AssetDatabase.Refresh();
            return true;
        }

        private static bool TryLoadLinkDocument(bool createIfMissing, out XmlDocument xmlDocument, out XmlElement linkerElement)
        {
            xmlDocument = new XmlDocument();
            linkerElement = null;
            if (!File.Exists(LinkFile))
            {
                if (!createIfMissing)
                {
                    return false;
                }

                linkerElement = xmlDocument.CreateElement("linker");
                xmlDocument.AppendChild(linkerElement);
                return true;
            }

            try
            {
                xmlDocument.Load(LinkFile);
            }
            catch (Exception e)
            {
                Debug.LogErrorFormat("Load link.xml Failed:{0}", e.Message);
                return false;
            }

            if (xmlDocument.DocumentElement == null)
            {
                if (!createIfMissing)
                {
                    return false;
                }

                linkerElement = xmlDocument.CreateElement("linker");
                xmlDocument.AppendChild(linkerElement);
                return true;
            }

            if (!string.Equals(xmlDocument.DocumentElement.Name, "linker", StringComparison.Ordinal))
            {
                Debug.LogError("Load link.xml Failed: root node is not <linker>.");
                return false;
            }

            linkerElement = xmlDocument.DocumentElement;
            return true;
        }

        private static void EnsureManagedRegion(XmlDocument xmlDocument, XmlElement linkerElement, out XmlComment beginTag, out XmlComment endTag)
        {
            beginTag = null;
            endTag = null;
            for (var node = linkerElement.FirstChild; node != null; node = node.NextSibling)
            {
                if (node is not XmlComment comment || !string.Equals(comment.Value, GenerateTagValue, StringComparison.Ordinal))
                {
                    continue;
                }

                if (beginTag == null)
                {
                    beginTag = comment;
                }
                else
                {
                    endTag = comment;
                    break;
                }
            }

            if (beginTag == null)
            {
                beginTag = xmlDocument.CreateComment(GenerateTagValue);
                if (linkerElement.FirstChild != null)
                {
                    linkerElement.InsertBefore(beginTag, linkerElement.FirstChild);
                }
                else
                {
                    linkerElement.AppendChild(beginTag);
                }
            }

            if (endTag == null)
            {
                endTag = xmlDocument.CreateComment(GenerateTagValue);
                if (beginTag.NextSibling != null)
                {
                    linkerElement.InsertBefore(endTag, beginTag.NextSibling);
                }
                else
                {
                    linkerElement.AppendChild(endTag);
                }
            }
        }

        private static void ClearManagedRegion(XmlComment beginTag, XmlComment endTag, XmlElement linkerElement)
        {
            var node = beginTag.NextSibling;
            while (node != null && node != endTag)
            {
                var nextNode = node.NextSibling;
                linkerElement.RemoveChild(node);
                node = nextNode;
            }
        }

        private static void InsertManagedAssembly(XmlDocument xmlDocument, XmlElement linkerElement, XmlNode endTag, string assemblyName)
        {
            var indentText = xmlDocument.CreateWhitespace(Environment.NewLine + "\t");
            var assemblyElement = xmlDocument.CreateElement("assembly");
            assemblyElement.SetAttribute("fullname", assemblyName);
            assemblyElement.SetAttribute("preserve", "all");

            linkerElement.InsertBefore(indentText, endTag);
            linkerElement.InsertBefore(assemblyElement, endTag);
        }

        private static void SaveLinkDocument(XmlDocument xmlDocument)
        {
            var directory = Path.GetDirectoryName(LinkFile);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            NormalizeManagedRegionWhitespace(xmlDocument);
            var settings = new XmlWriterSettings
            {
                Encoding = new UTF8Encoding(false),
                Indent = true,
                OmitXmlDeclaration = true,
                NewLineChars = Environment.NewLine,
                NewLineHandling = NewLineHandling.Replace
            };

            using var writer = XmlWriter.Create(LinkFile, settings);
            xmlDocument.Save(writer);
        }

        private static void NormalizeManagedRegionWhitespace(XmlDocument xmlDocument)
        {
            if (xmlDocument.DocumentElement == null)
            {
                return;
            }

            var linkerElement = xmlDocument.DocumentElement;
            for (var node = linkerElement.FirstChild; node != null; node = node.NextSibling)
            {
                if (node is XmlWhitespace || node is XmlSignificantWhitespace)
                {
                    node.Value = node.Value.Replace("\r\n", "\n").Replace("\n", Environment.NewLine);
                }
            }
        }
    }
}
