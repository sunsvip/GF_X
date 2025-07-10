using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Obfuz.Settings
{
    public class ConstEncryptionSettingsFacade
    {
        public int encryptionLevel;
        public List<string> ruleFiles;
    }

    [Serializable]
    public class ConstEncryptionSettings
    {
        [Tooltip("The encryption level for the obfuscation. Higher levels provide more security but may impact performance.")]
        [Range(1, 4)]
        public int encryptionLevel = 1;

        [Tooltip("config xml files")]
        public string[] ruleFiles;

        public ConstEncryptionSettingsFacade ToFacade()
        {
            return new ConstEncryptionSettingsFacade
            {
                ruleFiles = ruleFiles.ToList(),
                encryptionLevel = encryptionLevel,
            };
        }
    }
}
