using UnityEditor;

namespace UGF.EditorTools
{
    public class JenkinsBuildAppConfig
    {
        public string ResourceOutputDir;
        public BuildTarget Platform;
        public bool FullBuild;
        public bool DebugMode;
        public bool DevelopmentBuild;
        public bool BuildAppBundle;
        public string Version;
        public int VersionCode;
    }
}
