using dnlib.DotNet;
using Obfuz.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Obfuz.ObfusPasses.SymbolObfus
{

    public class VirtualMethodGroup
    {
        public List<MethodDef> methods;
    }

    public class VirtualMethodGroupCalculator
    {

        private class TypeFlatMethods
        {
            public HashSet<MethodDef> flatMethods = new HashSet<MethodDef>();


            public bool TryFindMatchVirtualMethod(MethodDef method, out MethodDef matchMethodDef)
            {
                foreach (var parentOrInterfaceMethod in flatMethods)
                {
                    if (parentOrInterfaceMethod.Name == method.Name && parentOrInterfaceMethod.GetParamCount() == method.GetParamCount())
                    {
                        matchMethodDef = parentOrInterfaceMethod;
                        return true;
                    }
                }
                matchMethodDef = null;
                return false;
            }
        }


        private readonly Dictionary<MethodDef, VirtualMethodGroup> _methodGroups = new Dictionary<MethodDef, VirtualMethodGroup>();
        private readonly Dictionary<TypeDef, TypeFlatMethods> _visitedTypes = new Dictionary<TypeDef, TypeFlatMethods>();



        public VirtualMethodGroup GetMethodGroup(MethodDef methodDef)
        {
            if (_methodGroups.TryGetValue(methodDef, out var group))
            {
                return group;
            }
            return null;
        }

        public void CalculateType(TypeDef typeDef)
        {
            if (_visitedTypes.ContainsKey(typeDef))
            {
                return;
            }

            var typeMethods = new TypeFlatMethods();

            var interfaceMethods = new List<MethodDef>();
            if (typeDef.BaseType != null)
            {
                TypeDef baseTypeDef = MetaUtil.GetTypeDefOrGenericTypeBaseThrowException(typeDef.BaseType);
                CalculateType(baseTypeDef);
                typeMethods.flatMethods.AddRange(_visitedTypes[baseTypeDef].flatMethods);
                foreach (var intfType in typeDef.Interfaces)
                {
                    TypeDef intfTypeDef = MetaUtil.GetTypeDefOrGenericTypeBaseThrowException(intfType.Interface);
                    CalculateType(intfTypeDef);
                    //typeMethods.flatMethods.AddRange(_visitedTypes[intfTypeDef].flatMethods);
                    interfaceMethods.AddRange(_visitedTypes[intfTypeDef].flatMethods);
                }
            }
            foreach (MethodDef method in interfaceMethods)
            {
                if (typeMethods.TryFindMatchVirtualMethod(method, out var matchMethodDef))
                {
                    // merge group
                    var group = _methodGroups[matchMethodDef];
                    var matchGroup = _methodGroups[method];
                    if (group != matchGroup)
                    {
                        foreach (var m in matchGroup.methods)
                        {
                            group.methods.Add(m);
                            _methodGroups[m] = group;
                        }
                    }
                }
            }

            typeMethods.flatMethods.AddRange(interfaceMethods);
            foreach (MethodDef method in typeDef.Methods)
            {
                if (!method.IsVirtual)
                {
                    continue;
                }
                if (typeMethods.TryFindMatchVirtualMethod(method, out var matchMethodDef))
                {
                    var group = _methodGroups[matchMethodDef];
                    group.methods.Add(method);
                    _methodGroups.Add(method, group);
                }
                else
                {
                    _methodGroups.Add(method, new VirtualMethodGroup() { methods = new List<MethodDef> { method } });
                }
                if (method.IsNewSlot)
                {
                    typeMethods.flatMethods.Add(method);
                }
            }
            _visitedTypes.Add(typeDef, typeMethods);
        }
    }
}
