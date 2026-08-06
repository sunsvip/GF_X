namespace UGF.EditorTools
{
    internal sealed class AtlasCreationPanelState
    {
        internal readonly AtlasSettingsOverrideState Overrides = new AtlasSettingsOverrideState();
        internal bool GenerateAtlasVariant;
        internal bool IncludeChildrenFolders = true;
        internal int AtlasSpriteSizeLimit = 512;

        internal void Initialize()
        {
            Overrides.Initialize();
            GenerateAtlasVariant = false;
            IncludeChildrenFolders = true;
            AtlasSpriteSizeLimit = 512;
        }

        internal void Release()
        {
            Overrides.Release();
        }
    }
}
