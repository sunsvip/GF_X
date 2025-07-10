using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Obfuz.Settings
{
    public class FieldEncryptionSettingsFacade
    {
        public int encryptionLevel;
        public List<string> ruleFiles;
    }

    [Serializable]
    public class FieldEncryptionSettings
    {
        [Tooltip("The encryption level for the obfuscation. Higher levels provide more security but may impact performance.")]
        [Range(1, 4)]
        public int encryptionLevel = 1;

        [Tooltip("rule config xml files")]
        public string[] ruleFiles;

        public FieldEncryptionSettingsFacade ToFacade()
        {
            return new FieldEncryptionSettingsFacade
            {
                ruleFiles = ruleFiles.ToList(),
                encryptionLevel = encryptionLevel,
            };
        }
    }
}
