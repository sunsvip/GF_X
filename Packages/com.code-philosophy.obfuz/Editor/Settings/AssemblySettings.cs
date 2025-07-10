using NUnit.Framework;
using Obfuz.Editor;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Obfuz.Settings
{
    [Serializable]
    public class AssemblySettings
    {

        [Tooltip("name of assemblies to obfuscate, please don't add 'Obfuz.Runtime'")]
        public string[] assembliesToObfuscate;

        [Tooltip("name of assemblies not obfuscated but reference assemblies to obfuscated ")]
        public string[] nonObfuscatedButReferencingObfuscatedAssemblies;

        [Tooltip("additional assembly search paths")]
        public string[] additionalAssemblySearchPaths;

        [Tooltip("obfuscate Obfuz.Runtime")]
        public bool obfuscateObfuzRuntime = true;

        public List<string> GetAssembliesToObfuscate()
        {
            var asses = new List<string>(assembliesToObfuscate);
            if (obfuscateObfuzRuntime && !asses.Contains(ConstValues.ObfuzRuntimeAssemblyName))
            {
                asses.Add(ConstValues.ObfuzRuntimeAssemblyName);
            }
            return asses;
        }

        public List<string> GetObfuscationRelativeAssemblyNames()
        {
            var asses = GetAssembliesToObfuscate();
            asses.AddRange(nonObfuscatedButReferencingObfuscatedAssemblies);
            return asses;
        }
    }
}
