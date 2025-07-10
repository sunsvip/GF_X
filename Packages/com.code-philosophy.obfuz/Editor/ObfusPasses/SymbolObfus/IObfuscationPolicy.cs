using dnlib.DotNet;

namespace Obfuz.ObfusPasses.SymbolObfus
{
    public interface IObfuscationPolicy
    {
        bool NeedRename(TypeDef typeDef);

        bool NeedRename(MethodDef methodDef);

        bool NeedRename(FieldDef fieldDef);

        bool NeedRename(PropertyDef propertyDef);

        bool NeedRename(EventDef eventDef);
    }
}
