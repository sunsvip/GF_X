using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Obfuz.Settings
{
    public class CallObfuscationSettingsFacade
    {
        public List<string> ruleFiles;
        public int obfuscationLevel;
        public int maxProxyMethodCountPerDispatchMethod;
    }

    [Serializable]
    public class CallObfuscationSettings
    {
        [Tooltip("The obfuscation level for the obfuscation. Higher levels provide more security but may impact performance.")]
        [Range(1, 4)]
        public int obfuscationLevel = 1;

        [Tooltip("The maximum number of proxy methods that can be generated per dispatch method. This helps to limit the complexity of the generated code and improve performance.")]
        public int maxProxyMethodCountPerDispatchMethod = 100;

        [Tooltip("rule config xml files")]
        public string[] ruleFiles;

        public CallObfuscationSettingsFacade ToFacade()
        {
            return new CallObfuscationSettingsFacade
            {
                ruleFiles = ruleFiles.ToList(),
                obfuscationLevel = obfuscationLevel,
                maxProxyMethodCountPerDispatchMethod = maxProxyMethodCountPerDispatchMethod,
            };
        }
    }
}
