using dnlib.DotNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Obfuz.ObfusPasses.SymbolObfus.Policies
{
    internal class SupportPassPolicy : ObfuscationPolicyBase
    {
        private readonly ConfigurablePassPolicy _policy;


        private bool Support(ObfuscationPassType passType)
        {
            return passType.HasFlag(ObfuscationPassType.SymbolObfus);
        }

        public SupportPassPolicy(ConfigurablePassPolicy policy)
        {
            _policy = policy;
        }

        public override bool NeedRename(TypeDef typeDef)
        {
            return Support(_policy.GetTypeObfuscationPasses(typeDef));
        }

        public override bool NeedRename(MethodDef methodDef)
        {
            return Support(_policy.GetMethodObfuscationPasses(methodDef));
        }

        public override bool NeedRename(FieldDef fieldDef)
        {
            return Support(_policy.GetFieldObfuscationPasses(fieldDef));
        }

        public override bool NeedRename(PropertyDef propertyDef)
        {
            return Support(_policy.GetPropertyObfuscationPasses(propertyDef));
        }

        public override bool NeedRename(EventDef eventDef)
        {
            return Support(_policy.GetEventObfuscationPasses(eventDef));
        }
    }
}
