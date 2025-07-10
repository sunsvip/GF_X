namespace Obfuz.ObfusPasses.SymbolObfus.NameMakers
{
    public interface INameScope
    {
        bool AddPreservedName(string name);

        bool IsNamePreserved(string name);

        string GetNewName(string originalName, bool reuse);
    }
}
