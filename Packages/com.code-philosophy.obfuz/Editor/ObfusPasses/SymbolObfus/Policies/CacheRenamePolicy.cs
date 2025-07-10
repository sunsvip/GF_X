using dnlib.DotNet;
using System.Collections.Generic;
using System.Linq;

namespace Obfuz.ObfusPasses.SymbolObfus.Policies
{
    public class CacheRenamePolicy : ObfuscationPolicyBase
    {
        private readonly IObfuscationPolicy _underlyingPolicy;

        private readonly Dictionary<object, bool> _computeCache = new Dictionary<object, bool>();

        public CacheRenamePolicy(IObfuscationPolicy underlyingPolicy)
        {
            _underlyingPolicy = underlyingPolicy;
        }

        public override bool NeedRename(TypeDef typeDef)
        {
            if (!_computeCache.TryGetValue(typeDef, out var value))
            {
                value = _underlyingPolicy.NeedRename(typeDef);
                _computeCache[typeDef] = value;
            }
            return value;
        }

        public override bool NeedRename(MethodDef methodDef)
        {
            if (!_computeCache.TryGetValue(methodDef, out var value))
            {
                value = _underlyingPolicy.NeedRename(methodDef);
                _computeCache[methodDef] = value;
            }
            return value;
        }

        public override bool NeedRename(FieldDef fieldDef)
        {
            if (!_computeCache.TryGetValue(fieldDef, out var value))
            {
                value = _underlyingPolicy.NeedRename(fieldDef);
                _computeCache[fieldDef] = value;
            }
            return value;
        }

        public override bool NeedRename(PropertyDef propertyDef)
        {
            if (!_computeCache.TryGetValue(propertyDef, out var value))
            {
                value = _underlyingPolicy.NeedRename(propertyDef);
                _computeCache[propertyDef] = value;
            }
            return value;
        }

        public override bool NeedRename(EventDef eventDef)
        {
            if (!_computeCache.TryGetValue(eventDef, out var value))
            {
                value = _underlyingPolicy.NeedRename(eventDef);
                _computeCache[eventDef] = value;
            }
            return value;
        }
    }
}
