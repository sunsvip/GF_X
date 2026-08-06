namespace UGF.EditorTools
{
    internal sealed class StripLinkConfigSelectionItem
    {
        public StripLinkConfigSelectionItem(bool isSelected, string assemblyName)
        {
            IsSelected = isSelected;
            AssemblyName = assemblyName;
        }

        public bool IsSelected;
        public string AssemblyName { get; }
    }
}
