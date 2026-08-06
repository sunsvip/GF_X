namespace UGF.EditorTools
{
    internal sealed class AtlasVariantPanelState
    {
        internal readonly AtlasSettingsOverrideState Overrides = new AtlasSettingsOverrideState();
        internal bool EnableVariantScale;

        internal void Initialize()
        {
            Overrides.Initialize();
            EnableVariantScale = false;
        }

        internal void Release()
        {
            Overrides.Release();
        }
    }
}
