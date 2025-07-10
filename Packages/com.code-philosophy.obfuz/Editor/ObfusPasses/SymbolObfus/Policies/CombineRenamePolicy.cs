using dnlib.DotNet;
using System.Linq;

namespace Obfuz.ObfusPasses.SymbolObfus.Policies
{
    public class CombineRenamePolicy : IObfuscationPolicy
    {
        private readonly IObfuscationPolicy[] _policies;

        public CombineRenamePolicy(params IObfuscationPolicy[] policies)
        {
            _policies = policies;
        }

        public bool NeedRename(TypeDef typeDef)
        {
            return _policies.All(policy => policy.NeedRename(typeDef));
        }

        public bool NeedRename(MethodDef methodDef)
        {
            return _policies.All(policy => policy.NeedRename(methodDef));
        }

        public bool NeedRename(FieldDef fieldDef)
        {
            return _policies.All(policy => policy.NeedRename(fieldDef));
        }

        public bool NeedRename(PropertyDef propertyDef)
        {
            return _policies.All(policy => policy.NeedRename(propertyDef));
        }

        public bool NeedRename(EventDef eventDef)
        {
            return _policies.All(policy => policy.NeedRename(eventDef));
        }
    }
}
