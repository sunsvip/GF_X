using dnlib.DotNet;
using Obfuz.Utils;

namespace Obfuz.ObfusPasses.SymbolObfus.Policies
{
    public class SystemRenamePolicy : ObfuscationPolicyBase
    {
        public override bool NeedRename(TypeDef typeDef)
        {
            string name = typeDef.Name;
            if (name == "<Module>" || name == "ObfuzIgnoreAttribute")
            {
                return false;
            }
            if (MetaUtil.HasSelfOrInheritObfuzIgnoreScope(typeDef, typeDef.DeclaringType, ObfuzScope.TypeName))
            {
                return false;
            }
            return true;
        }

        public override bool NeedRename(MethodDef methodDef)
        {
            if (methodDef.DeclaringType.IsDelegate)
            {
                return false;
            }
            if (methodDef.Name == ".ctor" || methodDef.Name == ".cctor")
            {
                return false;
            }

            if (MetaUtil.HasSelfOrInheritObfuzIgnoreScope(methodDef, methodDef.DeclaringType, ObfuzScope.MethodName))
            {
                return false;
            }
            return true;
        }

        public override bool NeedRename(FieldDef fieldDef)
        {
            if (fieldDef.DeclaringType.IsDelegate)
            {
                return false;
            }
            if (MetaUtil.HasSelfOrInheritObfuzIgnoreScope(fieldDef, fieldDef.DeclaringType, ObfuzScope.Field))
            {
                return false;
            }
            if (fieldDef.DeclaringType.IsEnum && fieldDef.Name == "value__")
            {
                return false;
            }
            return true;
        }

        public override bool NeedRename(PropertyDef propertyDef)
        {
            if (propertyDef.DeclaringType.IsDelegate)
            {
                return false;
            }
            if (MetaUtil.HasSelfOrInheritObfuzIgnoreScope(propertyDef, propertyDef.DeclaringType, ObfuzScope.PropertyName))
            {
                return false;
            }
            return true;
        }

        public override bool NeedRename(EventDef eventDef)
        {
            if (eventDef.DeclaringType.IsDelegate)
            {
                return false;
            }
            if (MetaUtil.HasSelfOrInheritObfuzIgnoreScope(eventDef, eventDef.DeclaringType, ObfuzScope.EventName))
            {
                return false;
            }
            return true;
        }
    }
}
