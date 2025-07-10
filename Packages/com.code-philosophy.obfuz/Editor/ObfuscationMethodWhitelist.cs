using dnlib.DotNet;
using Obfuz.Editor;
using Obfuz.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Obfuz
{
    public class ObfuscationMethodWhitelist
    {

        public bool IsInWhiteList(ModuleDef module)
        {
            string modName = module.Assembly.Name;
            if (modName == ConstValues.ObfuzRuntimeAssemblyName)
            {
                return true;
            }
            //if (MetaUtil.HasObfuzIgnoreScope(module))
            //{
            //    return true;
            //}
            return false;
        }

        private bool DoesMethodContainsRuntimeInitializeOnLoadMethodAttributeAndLoadTypeGreaterEqualAfterAssembliesLoaded(MethodDef method)
        {
            CustomAttribute ca = method.CustomAttributes.Find("UnityEngine.RuntimeInitializeOnLoadMethodAttribute");
            if (ca != null && ca.ConstructorArguments.Count > 0)
            {
                RuntimeInitializeLoadType loadType = (RuntimeInitializeLoadType)ca.ConstructorArguments[0].Value;
                if (loadType >= RuntimeInitializeLoadType.AfterAssembliesLoaded)
                {
                    return true;
                }
            }
            return false;
        }

        public bool IsInWhiteList(MethodDef method)
        {
            TypeDef typeDef = method.DeclaringType;
            if (IsInWhiteList(typeDef))
            {
                return true;
            }
            if (method.Name.StartsWith(ConstValues.ObfuzInternalSymbolNamePrefix))
            {
                return true;
            }
            if (MetaUtil.HasSelfOrInheritObfuzIgnoreScope(method, typeDef, ObfuzScope.MethodBody))
            {
                return true;
            }
            CustomAttribute ca = method.CustomAttributes.Find("UnityEngine.RuntimeInitializeOnLoadMethodAttribute");
            if (DoesMethodContainsRuntimeInitializeOnLoadMethodAttributeAndLoadTypeGreaterEqualAfterAssembliesLoaded(method))
            {
                return true;
            }

            // don't obfuscate cctor when it has RuntimeInitializeOnLoadMethodAttribute with load type AfterAssembliesLoaded
            if (method.IsStatic && method.Name == ".cctor" && typeDef.Methods.Any(m => DoesMethodContainsRuntimeInitializeOnLoadMethodAttributeAndLoadTypeGreaterEqualAfterAssembliesLoaded(m)))
            {
                return true;
            }
            return false;
        }

        public bool IsInWhiteList(TypeDef type)
        {
            if (type.Name.StartsWith(ConstValues.ObfuzInternalSymbolNamePrefix))
            {
                return true;
            }
            if (IsInWhiteList(type.Module))
            {
                return true;
            }
            if (MetaUtil.HasSelfOrInheritObfuzIgnoreScope(type, type.DeclaringType, ObfuzScope.TypeName))
            {
                return true;
            }
            //if (type.DeclaringType != null && IsInWhiteList(type.DeclaringType))
            //{
            //    return true;
            //}
            if (type.FullName == "Obfuz.EncryptionVM.GeneratedEncryptionVirtualMachine")
            {
                return true;
            }
            return false;
        }
    }
}
