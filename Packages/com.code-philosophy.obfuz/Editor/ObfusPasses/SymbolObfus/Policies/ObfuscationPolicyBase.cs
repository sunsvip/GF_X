using dnlib.DotNet;

namespace Obfuz.ObfusPasses.SymbolObfus.Policies
{
    public abstract class ObfuscationPolicyBase : IObfuscationPolicy
    {

        public virtual bool NeedRename(TypeDef typeDef)
        {
            return true;
        }

        public virtual bool NeedRename(MethodDef methodDef)
        {
            return true;
        }

        public virtual bool NeedRename(FieldDef fieldDef)
        {
            return true;
        }

        public virtual bool NeedRename(PropertyDef propertyDef)
        {
            return true;
        }

        public virtual bool NeedRename(EventDef eventDef)
        {
            return true;
        }
    }
}
