using Obfuz.ObfusPasses;
using System;
using UnityEngine;

namespace Obfuz.Settings
{
    [Serializable]
    public class ObfuscationPassSettings
    {
        [Tooltip("enable obfuscation pass")]
        public ObfuscationPassType enabledPasses = ObfuscationPassType.All;

        [Tooltip("rule config xml files")]
        public string[] ruleFiles;
    }
}
