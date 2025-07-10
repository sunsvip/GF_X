using System;
using System.IO;
using UnityEngine;

namespace Obfuz.Settings
{
    [Serializable]
    public class SecretSettings
    {

        [Tooltip("default static secret key")]
        public string defaultStaticSecretKey = "Code Philosophy-Static";

        [Tooltip("default dynamic secret key")]
        public string defaultDynamicSecretKey = "Code Philosophy-Dynamic";

        [Tooltip("default static secret key output path")]
        public string staticSecretKeyOutputPath = $"Assets/Resources/Obfuz/defaultStaticSecretKey.bytes";

        [Tooltip("default dynamic secret key output path")]
        public string dynamicSecretKeyOutputPath = $"Assets/Resources/Obfuz/defaultDynamicSecretKey.bytes";

        [Tooltip("random seed")]
        public int randomSeed = 0;

        [Tooltip("name of assemblies those use dynamic secret key")]
        public string[] assembliesUsingDynamicSecretKeys;
    }
}
