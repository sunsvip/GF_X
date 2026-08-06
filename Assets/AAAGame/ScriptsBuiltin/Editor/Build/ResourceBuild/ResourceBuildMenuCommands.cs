using UnityEditor;

namespace UGF.EditorTools
{
    internal static class ResourceBuildMenuCommands
    {
        [MenuItem("Game Framework/Resource Tools/Resolve Duplicate Assets【解决AB资源重复依赖冗余】", false, 100)]
        private static void RefreshSharedAssets()
        {
            DuplicateAssetResolver.AutoResolveAbDuplicateAssets(true);
        }
    }
}
