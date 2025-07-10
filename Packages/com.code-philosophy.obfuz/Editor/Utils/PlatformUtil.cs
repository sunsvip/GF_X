using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;

namespace Obfuz.Utils
{
    public static class PlatformUtil
    {
        public static bool IsMonoBackend()
        {
            return PlayerSettings.GetScriptingBackend(EditorUserBuildSettings.selectedBuildTargetGroup)
                == ScriptingImplementation.Mono2x;
        }
    }
}
