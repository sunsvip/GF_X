using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Obfuz.Settings
{
    public class SymbolObfuscationSettingsFacade
    {
        public bool debug;
        public string obfuscatedNamePrefix;
        public bool useConsistentNamespaceObfuscation;
        public string symbolMappingFile;
        public List<string> ruleFiles;
    }

    [Serializable]
    public class SymbolObfuscationSettings
    {
        public bool debug;

        [Tooltip("prefix for obfuscated name to avoid name confliction with original name")]
        public string obfuscatedNamePrefix = "$";

        [Tooltip("obfuscate same namespace to one name")]
        public bool useConsistentNamespaceObfuscation = true;

        [Tooltip("symbol mapping file path")]
        public string symbolMappingFile = "Assets/Obfuz/SymbolObfus/symbol-mapping.xml";

        [Tooltip("rule files")]
        public string[] ruleFiles;

        public SymbolObfuscationSettingsFacade ToFacade()
        {
            return new SymbolObfuscationSettingsFacade
            {
                debug = debug,
                obfuscatedNamePrefix = obfuscatedNamePrefix,
                useConsistentNamespaceObfuscation = useConsistentNamespaceObfuscation,
                symbolMappingFile = symbolMappingFile,
                ruleFiles = ruleFiles.ToList(),
            };
        }
    }
}
