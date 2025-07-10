using dnlib.DotNet;

namespace Obfuz.ObfusPasses.SymbolObfus
{
    public interface INameMaker
    {
        void AddPreservedName(TypeDef typeDef, string name);

        void AddPreservedNamespace(TypeDef typeDef, string name);

        void AddPreservedName(MethodDef methodDef, string name);

        void AddPreservedName(FieldDef fieldDef, string name);

        void AddPreservedName(PropertyDef propertyDef, string name);

        void AddPreservedName(EventDef eventDef, string name);

        bool IsNamePreserved(VirtualMethodGroup virtualMethodGroup, string name);

        string GetNewName(TypeDef typeDef, string originalName);

        string GetNewNamespace(TypeDef typeDef, string originalNamespace, bool reuse);

        string GetNewName(MethodDef methodDef, string originalName);

        string GetNewName(VirtualMethodGroup virtualMethodGroup, string originalName);

        string GetNewName(ParamDef param, string originalName);

        string GetNewName(FieldDef fieldDef, string originalName);

        string GetNewName(PropertyDef propertyDef, string originalName);

        string GetNewName(EventDef eventDef, string originalName);
    }
}
