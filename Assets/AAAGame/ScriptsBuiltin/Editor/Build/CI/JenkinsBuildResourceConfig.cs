using UnityEditor;

namespace UGF.EditorTools
{
    public class JenkinsBuildResourceConfig
    {
        public string ResourceOutputDir;
        public BuildTarget Platform;
        public bool ForceRebuild;
        public int ResourceVersion;
        public string UpdatePrefixUrl;
        public string ApplicableVersions;
        public bool ForceUpdate;
        public string AppUpdateUrl;
        public string AppUpdateDescription;
    }
}
