using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Obfuz.Settings
{
    [Serializable]
    public class EncryptionVMSettings
    {
        [Tooltip("secret key for generating encryption virtual machine source code")]
        public string codeGenerationSecretKey = "Obfuz";

        [Tooltip("encryption OpCode count, should be power of 2 and >= 64")]
        public int encryptionOpCodeCount = 256;

        [Tooltip("encryption virtual machine source code output path")]
        public string codeOutputPath = "Assets/Obfuz/GeneratedEncryptionVirtualMachine.cs";
    }
}
