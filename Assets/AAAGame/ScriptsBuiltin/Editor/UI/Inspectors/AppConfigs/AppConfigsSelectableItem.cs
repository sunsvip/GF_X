namespace UGF.EditorTools
{
    internal sealed class AppConfigsSelectableItem
    {
        internal bool IsOn;
        internal string Name { get; }

        internal AppConfigsSelectableItem(bool isOn, string name)
        {
            IsOn = isOn;
            Name = name;
        }
    }
}
