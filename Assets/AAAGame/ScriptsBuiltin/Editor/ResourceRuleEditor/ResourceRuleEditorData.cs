using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityGameFramework.Editor.ResourceTools;

namespace UGF.EditorTools.ResourceTools
{
    public class ResourceRuleEditorData : ScriptableObject
    {
        public List<ResourceRule> rules = new List<ResourceRule>();
    }

    [System.Serializable]
    public class ResourceRule
    {
        public bool valid = true;
        public string name = string.Empty;
        public string variant = null;
        public string fileSystem = string.Empty;
        public string groups = string.Empty;
        public string assetsDirectoryPath = string.Empty;
        public LoadType loadType = LoadType.LoadFromMemoryAndQuickDecrypt;
        public bool packed = true;
        public ResourceFilterType filterType = ResourceFilterType.Root;
        public string searchPatterns = "*.*";
        public string excludeSearchPattern = string.Empty;
    }

    public enum ResourceFilterType
    {
        Root,
        RootTopDirectoryOnly,
        Children,
        ChildrenFoldersOnly,
        ChildrenFilesOnly,
    }
}
