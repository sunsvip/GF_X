using dnlib.DotNet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.Assertions;
using UnityEngine.UIElements;

namespace Obfuz.Utils
{

    public static class MetaUtil
    {
        public static string GetModuleNameWithoutExt(string moduleName)
        {
            return Path.GetFileNameWithoutExtension(moduleName);
        }

        public static (string, string) SplitNamespaceAndName(string fullName)
        {
            int index = fullName.LastIndexOf('/');
            if (index == -1)
            {
                int index2 = fullName.IndexOf('.');
                return index2 >= 0 ? (fullName.Substring(0, index2), fullName.Substring(index2 + 1)) : ("", fullName);
            }
            return ("", fullName.Substring(index + 1));
        }


        public static TypeDef GetBaseTypeDef(TypeDef type)
        {
            ITypeDefOrRef baseType = type.BaseType;
            if (baseType == null)
            {
                return null;
            }
            TypeDef baseTypeDef = baseType.ResolveTypeDef();
            if (baseTypeDef != null)
            {
                return baseTypeDef;
            }
            if (baseType is TypeSpec baseTypeSpec)
            {
                GenericInstSig genericIns = baseTypeSpec.TypeSig.ToGenericInstSig();
                return genericIns.GenericType.TypeDefOrRef.ResolveTypeDefThrow();
            }
            else
            {
                throw new Exception($"GetBaseTypeDef: {type} fail");
            }
        }

        public static TypeDef GetTypeDefOrGenericTypeBaseThrowException(ITypeDefOrRef type)
        {
            if (type.IsTypeDef)
            {
                return (TypeDef)type;
            }
            if (type.IsTypeRef)
            {
                return type.ResolveTypeDefThrow();
            }
            if (type.IsTypeSpec)
            {
                GenericInstSig gis = type.TryGetGenericInstSig();
                return gis.GenericType.ToTypeDefOrRef().ResolveTypeDefThrow();
            }
            throw new NotSupportedException($"{type}");
        }

        public static TypeDef GetTypeDefOrGenericTypeBaseOrNull(ITypeDefOrRef type)
        {
            if (type.IsTypeDef)
            {
                return (TypeDef)type;
            }
            if (type.IsTypeRef)
            {
                return type.ResolveTypeDefThrow();
            }
            if (type.IsTypeSpec)
            {
                GenericInstSig gis = type.TryGetGenericInstSig();
                if (gis == null)
                {
                    return null;
                }
                return gis.GenericType.ToTypeDefOrRef().ResolveTypeDefThrow();
            }
            return null;
        }

        public static TypeDef GetMemberRefTypeDefParentOrNull(IMemberRefParent parent)
        {
            if (parent is TypeDef typeDef)
            {
                return typeDef;
            }
            if (parent is TypeRef typeRef)
            {
                return typeRef.ResolveTypeDefThrow();
            }
            if (parent is TypeSpec typeSpec)
            {
                GenericInstSig gis = typeSpec.TryGetGenericInstSig();
                if (gis == null)
                {
                    return null;
                }
                return gis.GenericType.TypeDefOrRef.ResolveTypeDefThrow();
            }
            return null;
        }

        public static bool IsInheritFromUnityObject(TypeDef typeDef)
        {
            TypeDef cur = typeDef;
            while (true)
            {
                cur = GetBaseTypeDef(cur);
                if (cur == null)
                {
                    return false;
                }
                if (cur.Name == "Object" && cur.Namespace == "UnityEngine" && cur.Module.Name == "UnityEngine.CoreModule.dll")
                {
                    return true;
                }
            }
        }



        public static bool IsScriptOrSerializableType(TypeDef type)
        {
            if (type.ContainsGenericParameter)
            {
                return false;
            }
            if (type.IsSerializable)
            {
                return true;
            }

            for (TypeDef parentType = GetBaseTypeDef(type); parentType != null; parentType = GetBaseTypeDef(parentType))
            {
                if ((parentType.Name == "MonoBehaviour" || parentType.Name == "ScriptableObject")
                    && parentType.Namespace == "UnityEngine"
                    && parentType.Module.Assembly.Name == "UnityEngine.CoreModule")
                {
                    return true;
                }
            }

            return false;
        }

        public static bool IsSerializableTypeSig(TypeSig typeSig)
        {
            typeSig = typeSig.RemovePinnedAndModifiers();
            switch (typeSig.ElementType)
            {
                case ElementType.Boolean:
                case ElementType.Char:
                case ElementType.I1:
                case ElementType.U1:
                case ElementType.I2:
                case ElementType.U2:
                case ElementType.I4:
                case ElementType.U4:
                case ElementType.I8:
                case ElementType.U8:
                case ElementType.R4:
                case ElementType.R8:
                case ElementType.String:
                return true;
                case ElementType.Class:
                return IsScriptOrSerializableType(typeSig.ToTypeDefOrRef().ResolveTypeDefThrow());
                case ElementType.ValueType:
                {
                    TypeDef typeDef = typeSig.ToTypeDefOrRef().ResolveTypeDefThrow();
                    if (typeDef.IsEnum)
                    {
                        return true;
                    }
                    return typeDef.IsSerializable;
                }
                case ElementType.GenericInst:
                {
                    GenericInstSig genericIns = typeSig.ToGenericInstSig();
                    TypeDef typeDef = genericIns.GenericType.ToTypeDefOrRef().ResolveTypeDefThrow();
                    return typeDef.FullName == "System.Collections.Generic.List`1" && IsSerializableTypeSig(genericIns.GenericArguments[0]);
                }
                case ElementType.SZArray:
                {
                    return IsSerializableTypeSig(typeSig.RemovePinnedAndModifiers().Next);
                }
                default:
                return false;
            }
        }

        public static bool IsSerializableField(FieldDef field)
        {
            if (field.IsStatic)
            {
                return false;
            }
            var fieldSig = field.FieldSig.Type;
            if (field.IsPublic)
            {
                return IsSerializableTypeSig(fieldSig);
            }
            if (field.CustomAttributes.Any(c => c.TypeFullName == "UnityEngine.SerializeField"))
            {
                //UnityEngine.Debug.Assert(IsSerializableTypeSig(fieldSig));
                return true;
            }
            return false;
        }

        public static bool MayRenameCustomDataType(ElementType type)
        {
            return type == ElementType.Class || type == ElementType.ValueType || type == ElementType.Object || type == ElementType.SZArray;
        }

        public static TypeSig RetargetTypeRefInTypeSig(TypeSig type)
        {
            TypeSig next = type.Next;
            TypeSig newNext = next != null ? RetargetTypeRefInTypeSig(next) : null;
            if (type.IsModifier || type.IsPinned)
            {
                if (next == newNext)
                {
                    return type;
                }
                if (type is CModReqdSig cmrs)
                {
                    return new CModReqdSig(cmrs.Modifier, newNext);
                }
                if (type is CModOptSig cmos)
                {
                    return new CModOptSig(cmos.Modifier, newNext);
                }
                if (type is PinnedSig ps)
                {
                    return new PinnedSig(newNext);
                }
                throw new System.NotSupportedException(type.ToString());
            }
            switch (type.ElementType)
            {
                case ElementType.Ptr:
                {
                    if (next == newNext)
                    {
                        return type;
                    }
                    return new PtrSig(newNext);
                }
                case ElementType.ValueType:
                case ElementType.Class:
                {
                    var vts = type as ClassOrValueTypeSig;
                    if (vts.TypeDefOrRef is TypeDef typeDef)
                    {
                        return type;
                    }
                    TypeRef typeRef = (TypeRef)vts.TypeDefOrRef;
                    if (typeRef.DefinitionAssembly.IsCorLib())
                    {
                        return type;
                    }
                    typeDef = typeRef.ResolveTypeDefThrow();
                    return type.IsClassSig ? (TypeSig)new ClassSig(typeDef) : new ValueTypeSig(typeDef);
                }
                case ElementType.Array:
                {
                    if (next == newNext)
                    {
                        return type;
                    }
                    return new ArraySig(newNext);
                }
                case ElementType.SZArray:
                {
                    if (next == newNext)
                    {
                        return type;
                    }
                    return new SZArraySig(newNext);
                }
                case ElementType.GenericInst:
                {
                    var gis = type as GenericInstSig;
                    ClassOrValueTypeSig genericType = gis.GenericType;
                    ClassOrValueTypeSig newGenericType = (ClassOrValueTypeSig)RetargetTypeRefInTypeSig(genericType);
                    bool anyChange = genericType != newGenericType;
                    var genericArgs = new List<TypeSig>();
                    foreach (var arg in gis.GenericArguments)
                    {
                        TypeSig newArg = RetargetTypeRefInTypeSig(arg);
                        anyChange |= newArg != arg;
                        genericArgs.Add(newArg);
                    }
                    if (!anyChange)
                    {
                        return type;
                    }
                    return new GenericInstSig(newGenericType, genericArgs);
                }
                case ElementType.FnPtr:
                {
                    var fp = type as FnPtrSig;
                    MethodSig methodSig = fp.MethodSig;
                    TypeSig newReturnType = RetargetTypeRefInTypeSig(methodSig.RetType);
                    bool anyChange = newReturnType != methodSig.RetType;
                    var newArgs = new List<TypeSig>();
                    foreach (TypeSig arg in methodSig.Params)
                    {
                        TypeSig newArg = RetargetTypeRefInTypeSig(arg);
                        anyChange |= newArg != newReturnType;
                    }
                    if (!anyChange)
                    {
                        return type;
                    }
                    var newParamsAfterSentinel = new List<TypeSig>();
                    foreach (TypeSig arg in methodSig.ParamsAfterSentinel)
                    {
                        TypeSig newArg = RetargetTypeRefInTypeSig(arg);
                        anyChange |= newArg != arg;
                        newParamsAfterSentinel.Add(newArg);
                    }

                    var newMethodSig = new MethodSig(methodSig.CallingConvention, methodSig.GenParamCount, newReturnType, newArgs, newParamsAfterSentinel);
                    return new FnPtrSig(newMethodSig);
                }
                case ElementType.ByRef:
                {
                    if (next == newNext)
                    {
                        return type;
                    }
                    return new ByRefSig(newNext);
                }
                default:
                {
                    return type;
                }
            }
        }


        public static object RetargetTypeRefInTypeSigOfValue(object oldValue)
        {
            if (oldValue == null)
            {
                return null;
            }
            string typeName = oldValue.GetType().FullName;
            if (oldValue.GetType().IsPrimitive)
            {
                return oldValue;
            }
            if (oldValue is string || oldValue is UTF8String)
            {
                return oldValue;
            }
            if (oldValue is TypeSig typeSig)
            {
                return RetargetTypeRefInTypeSig(typeSig);
            }
            if (oldValue is CAArgument caValue)
            {
                TypeSig newType = RetargetTypeRefInTypeSig(caValue.Type);
                object newValue = RetargetTypeRefInTypeSigOfValue(caValue.Value);
                if (newType != caValue.Type || newValue != caValue.Value)
                {
                    return new CAArgument(newType, newValue);
                }
                return oldValue;
            }
            if (oldValue is List<CAArgument> oldArr)
            {
                bool anyChange = false;
                var newArr = new List<CAArgument>();
                foreach (CAArgument oldArg in oldArr)
                {
                    if (TryRetargetTypeRefInArgument(oldArg, out var newArg))
                    {
                        anyChange = true;
                        newArr.Add(newArg);
                    }
                    else
                    {
                        newArr.Add(oldArg);
                    }
                }
                return anyChange ? newArr : oldArr;
            }
            throw new NotSupportedException($"type:{oldValue.GetType()} value:{oldValue}");
        }



        public static bool TryRetargetTypeRefInArgument(CAArgument oldArg, out CAArgument newArg)
        {
            TypeSig newType = RetargetTypeRefInTypeSig(oldArg.Type);
            object newValue = RetargetTypeRefInTypeSigOfValue(oldArg.Value);
            if (newType != oldArg.Type || oldArg.Value != newValue)
            {
                newArg = new CAArgument(newType, newValue);
                return true;
            }
            newArg = default;
            return false;
        }

        public static bool TryRetargetTypeRefInNamedArgument(CANamedArgument arg)
        {
            bool anyChange = false;
            TypeSig newType = RetargetTypeRefInTypeSig(arg.Type);
            if (newType != arg.Type)
            {
                anyChange = true;
                arg.Type = newType;
            }
            if (TryRetargetTypeRefInArgument(arg.Argument, out var newArg))
            {
                arg.Argument = newArg;
                anyChange = true;
            }
            return anyChange;
        }

        //public static bool ContainsContainsGenericParameter1(MethodDef method)
        //{
        //    Assert.IsTrue(!(method.DeclaringType.ContainsGenericParameter || method.MethodSig.ContainsGenericParameter));
        //    return false;
        //}

        public static bool ContainsContainsGenericParameter1(MethodSpec methodSpec)
        {
            if (methodSpec.GenericInstMethodSig.ContainsGenericParameter)
            {
                return true;
            }
            IMethodDefOrRef method = methodSpec.Method;
            if (method.IsMethodDef)
            {
                return false;// ContainsContainsGenericParameter1((MethodDef)method);
            }
            if (method.IsMemberRef)
            {
                return ContainsContainsGenericParameter1((MemberRef)method);
            }
            throw new Exception($"unknown method: {method}");
        }

        public static bool ContainsContainsGenericParameter1(MemberRef memberRef)
        {
            IMemberRefParent parent = memberRef.Class;
            if (parent is TypeSpec typeSpec)
            {
                return typeSpec.ContainsGenericParameter;
            }
            return false;
        }

        public static bool ContainsContainsGenericParameter(IMethod method)
        {
            Assert.IsTrue(method.IsMethod);
            if (method is MethodDef methodDef)
            {
                return false;
            }

            if (method is MethodSpec methodSpec)
            {
                return ContainsContainsGenericParameter1(methodSpec);
            }
            if (method is MemberRef memberRef)
            {
                return ContainsContainsGenericParameter1(memberRef);
            }
            throw new Exception($"unknown method: {method}");
        }



        public static TypeSig Inflate(TypeSig sig, GenericArgumentContext ctx)
        {
            if (!sig.ContainsGenericParameter)
            {
                return sig;
            }
            return ctx.Resolve(sig);
        }


        public static MethodSig InflateMethodSig(MethodSig methodSig, GenericArgumentContext genericArgumentContext)
        {
            var newReturnType = Inflate(methodSig.RetType, genericArgumentContext);
            var newParams = new List<TypeSig>();
            foreach (var param in methodSig.Params)
            {
                newParams.Add(Inflate(param, genericArgumentContext));
            }
            var newParamsAfterSentinel = new List<TypeSig>();
            if (methodSig.ParamsAfterSentinel != null)
            {
                throw new NotSupportedException($"methodSig.ParamsAfterSentinel is not supported: {methodSig}");
                //foreach (var param in methodSig.ParamsAfterSentinel)
                //{
                //    newParamsAfterSentinel.Add(Inflate(param, genericArgumentContext));
                //}
            }
            return new MethodSig(methodSig.CallingConvention, methodSig.GenParamCount, newReturnType, newParams, null);
        }

        public static IList<TypeSig> GetGenericArguments(IMemberRefParent type)
        {
            if (type is TypeDef typeDef)
            {
                return null;
            }
            if (type is TypeRef typeRef)
            {
                return null;
            }
            if (type is TypeSpec typeSpec)
            {
                GenericInstSig genericInstSig = typeSpec.TypeSig.ToGenericInstSig();
                return genericInstSig?.GenericArguments;
            }
            throw new NotSupportedException($"type:{type}");
        }

        public static MethodSig GetInflatedMethodSig(IMethod method)
        {
            if (method is MethodDef methodDef)
            {
                return methodDef.MethodSig;
            }
            if (method is MemberRef memberRef)
            {
                return InflateMethodSig(memberRef.MethodSig, new GenericArgumentContext(GetGenericArguments(memberRef.Class), null));
            }
            if (method is MethodSpec methodSpec)
            {
                var genericInstMethodSig = methodSpec.GenericInstMethodSig;
                if (methodSpec.Method is MethodDef methodDef2)
                {
                    return InflateMethodSig(methodDef2.MethodSig, new GenericArgumentContext(null, genericInstMethodSig.GenericArguments));
                }
                if (methodSpec.Method is MemberRef memberRef2)
                {
                    return InflateMethodSig(memberRef2.MethodSig, new GenericArgumentContext(GetGenericArguments(memberRef2.Class), genericInstMethodSig.GenericArguments));
                }

            }
            throw new NotSupportedException($" method: {method}");
        }

        public static ThisArgType GetThisArgType(IMethod method)
        {
            if (!method.MethodSig.HasThis)
            {
                return ThisArgType.None;
            }
            if (method is MethodDef methodDef)
            {
                return methodDef.DeclaringType.IsValueType ? ThisArgType.ValueType : ThisArgType.Class;
            }
            if (method is MemberRef memberRef)
            {
                TypeDef typeDef = MetaUtil.GetMemberRefTypeDefParentOrNull(memberRef.Class);
                if (typeDef == null)
                {
                    return ThisArgType.Class;
                }
                return typeDef.IsValueType ? ThisArgType.ValueType : ThisArgType.Class;
            }
            if (method is MethodSpec methodSpec)
            {
                return GetThisArgType(methodSpec.Method);
            }
            throw new NotSupportedException($" method: {method}");
        }

        public static MethodSig ToSharedMethodSig(ICorLibTypes corTypes, MethodSig methodSig)
        {
            var newReturnType = methodSig.RetType;
            var newParams = new List<TypeSig>();
            foreach (var param in methodSig.Params)
            {
                newParams.Add(ToShareTypeSig(corTypes, param));
            }
            if (methodSig.ParamsAfterSentinel != null)
            {
                //foreach (var param in methodSig.ParamsAfterSentinel)
                //{
                //    newParamsAfterSentinel.Add(ToShareTypeSig(corTypes, param));
                //}
                throw new NotSupportedException($"methodSig.ParamsAfterSentinel is not supported: {methodSig}");
            }
            return new MethodSig(methodSig.CallingConvention, methodSig.GenParamCount, newReturnType, newParams, null);
        }

        public static TypeSig ToShareTypeSig(ICorLibTypes corTypes, TypeSig typeSig)
        {
            var a = typeSig.RemovePinnedAndModifiers();
            switch (a.ElementType)
            {
                case ElementType.Void: return corTypes.Void;
                case ElementType.Boolean: return corTypes.Byte;
                case ElementType.Char: return corTypes.UInt16;
                case ElementType.I1: return corTypes.SByte;
                case ElementType.U1: return corTypes.Byte;
                case ElementType.I2: return corTypes.Int16;
                case ElementType.U2: return corTypes.UInt16;
                case ElementType.I4: return corTypes.Int32;
                case ElementType.U4: return corTypes.UInt32;
                case ElementType.I8: return corTypes.Int64;
                case ElementType.U8: return corTypes.UInt64;
                case ElementType.R4: return corTypes.Single;
                case ElementType.R8: return corTypes.Double;
                case ElementType.String: return corTypes.Object;
                case ElementType.TypedByRef: return corTypes.TypedReference;
                case ElementType.I: return corTypes.IntPtr;
                case ElementType.U: return corTypes.UIntPtr;
                case ElementType.Object: return corTypes.Object;
                case ElementType.Sentinel: return typeSig;
                case ElementType.Ptr: return corTypes.UIntPtr;
                case ElementType.ByRef: return corTypes.UIntPtr;
                case ElementType.SZArray: return typeSig;
                case ElementType.Array: return typeSig;
                case ElementType.ValueType:
                {
                    TypeDef typeDef = a.ToTypeDefOrRef().ResolveTypeDef();
                    if (typeDef == null)
                    {
                        throw new Exception($"type:{a} definition could not be found");
                    }
                    if (typeDef.IsEnum)
                    {
                        return ToShareTypeSig(corTypes, typeDef.GetEnumUnderlyingType());
                    }
                    return typeSig;
                }
                case ElementType.Var:
                case ElementType.MVar:
                case ElementType.Class: return corTypes.Object;
                case ElementType.GenericInst:
                {
                    var gia = (GenericInstSig)a;
                    TypeDef typeDef = gia.GenericType.ToTypeDefOrRef().ResolveTypeDef();
                    if (typeDef == null)
                    {
                        throw new Exception($"type:{a} definition could not be found");
                    }
                    if (typeDef.IsEnum)
                    {
                        return ToShareTypeSig(corTypes, typeDef.GetEnumUnderlyingType());
                    }
                    if (!typeDef.IsValueType)
                    {
                        return corTypes.Object;
                    }
                    // il2cpp will raise error when try to share generic value type
                    return typeSig;
                    //return new GenericInstSig(gia.GenericType, gia.GenericArguments.Select(ga => ToShareTypeSig(corTypes, ga)).ToList());
                }
                case ElementType.FnPtr: return corTypes.UIntPtr;
                case ElementType.ValueArray: return typeSig;
                case ElementType.Module: return typeSig;
                default:
                throw new NotSupportedException(typeSig.ToString());
            }
        }


        public static void AppendIl2CppStackTraceNameOfTypeSig(StringBuilder sb, TypeSig typeSig)
        {
            typeSig = typeSig.RemovePinnedAndModifiers();
            
            switch (typeSig.ElementType)
            {
                case ElementType.Void: sb.Append("Void"); break;
                case ElementType.Boolean: sb.Append("Boolean"); break;
                case ElementType.Char: sb.Append("Char"); break;
                case ElementType.I1: sb.Append("SByte"); break;
                case ElementType.U1: sb.Append("Byte"); break;
                case ElementType.I2: sb.Append("Int16"); break;
                case ElementType.U2: sb.Append("UInt16"); break;
                case ElementType.I4: sb.Append("Int32"); break;
                case ElementType.U4: sb.Append("UInt32"); break;
                case ElementType.I8: sb.Append("Int64"); break;
                case ElementType.U8: sb.Append("UInt64"); break;
                case ElementType.R4: sb.Append("Single"); break;
                case ElementType.R8: sb.Append("Double"); break;
                case ElementType.String: sb.Append("String"); break;
                case ElementType.Ptr: AppendIl2CppStackTraceNameOfTypeSig(sb, typeSig.Next); sb.Append("*"); break;
                case ElementType.ByRef: AppendIl2CppStackTraceNameOfTypeSig(sb, typeSig.Next); sb.Append("&"); break;
                case ElementType.ValueType:
                case ElementType.Class:
                {
                    var classOrValueTypeSig = (ClassOrValueTypeSig)typeSig;
                    TypeDef typeDef = classOrValueTypeSig.TypeDefOrRef.ResolveTypeDef();
                    if (typeDef == null)
                    {
                        throw new Exception($"type:{classOrValueTypeSig} definition could not be found");
                    }
                    sb.Append(typeDef.Name);
                    break;
                }
                case ElementType.GenericInst:
                {
                    var genericInstSig = (GenericInstSig)typeSig;
                    AppendIl2CppStackTraceNameOfTypeSig(sb, genericInstSig.GenericType);
                    break;
                }
                case ElementType.Var:
                case ElementType.MVar:
                {
                    var varSig = (GenericSig)typeSig;
                    sb.Append(varSig.GenericParam.Name);
                    break;
                }
                case ElementType.I: sb.Append("IntPtr"); break;
                case ElementType.U: sb.Append("UIntPtr"); break;
                case ElementType.FnPtr: sb.Append("IntPtr"); break;
                case ElementType.Object: sb.Append("Object"); break;
                case ElementType.SZArray:
                {
                    var szArraySig = (SZArraySig)typeSig;
                    AppendIl2CppStackTraceNameOfTypeSig(sb, szArraySig.Next);
                    sb.Append("[]");
                    break;
                }
                case ElementType.Array:
                {
                    var arraySig = (ArraySig)typeSig;
                    AppendIl2CppStackTraceNameOfTypeSig(sb, arraySig.Next);
                    sb.Append("[");
                    for (int i = 0; i < arraySig.Rank - 1; i++)
                    {
                        sb.Append(",");
                    }
                    sb.Append("]");
                    break;
                }
                case ElementType.TypedByRef: sb.Append("TypedReference"); break;
                default:
                throw new NotSupportedException(typeSig.ToString());
            }
        }

        public static TypeDef GetRootDeclaringType(TypeDef type)
        {
            TypeDef cur = type;
            while (true)
            {
                TypeDef declaringType = cur.DeclaringType;
                if (declaringType == null)
                {
                    return cur;
                }
                cur = declaringType;
            }
        }

        public static string CreateMethodDefIl2CppStackTraceSignature(MethodDef method)
        {
            var result = new StringBuilder();
            TypeDef declaringType = method.DeclaringType;

            string namespaze = GetRootDeclaringType(declaringType).Namespace;
            if (!string.IsNullOrEmpty(namespaze))
            {
                result.Append(namespaze);
                result.Append(".");
            }
            result.Append(declaringType.Name);
            result.Append(":");
            result.Append(method.Name);
            result.Append("(");

            int index = 0;
            foreach (TypeSig p in method.GetParams())
            {
                if (index > 0)
                {
                    result.Append(", ");
                }
                AppendIl2CppStackTraceNameOfTypeSig(result, p);
                ++index;
            }
            result.Append(")");
            return result.ToString();
        }

        public static ObfuzScope? GetObfuzIgnoreScope(IHasCustomAttribute obj)
        {
            var ca = obj.CustomAttributes.FirstOrDefault(c => c.AttributeType.FullName == "Obfuz.ObfuzIgnoreAttribute");
            if (ca == null)
            {
                return null;
            }
            var scope = (ObfuzScope)ca.ConstructorArguments[0].Value;
            return scope;
        }

        public static ObfuzScope? GetSelfOrInheritObfuzIgnoreScope(TypeDef typeDef)
        {
            TypeDef cur = typeDef;
            while (cur != null)
            {
                var ca = cur.CustomAttributes?.FirstOrDefault(c => c.AttributeType.FullName == "Obfuz.ObfuzIgnoreAttribute");
                if (ca != null)
                {
                    var scope = (ObfuzScope)ca.ConstructorArguments[0].Value;
                    CANamedArgument inheritByNestedTypesArg = ca.GetNamedArgument("ApplyToMembers", false);
                    bool inheritByNestedTypes = inheritByNestedTypesArg == null || (bool)inheritByNestedTypesArg.Value;
                    return cur == typeDef || inheritByNestedTypes ? (ObfuzScope?) scope : null;
                }
                cur = cur.DeclaringType;
            }
            return null;
        }


        public static bool HasObfuzIgnoreScope(IHasCustomAttribute obj, ObfuzScope targetScope)
        {
            ObfuzScope? objScope = GetObfuzIgnoreScope(obj);
            if (objScope == null)
            {
                return false;
            }
            return objScope != null && (objScope & targetScope) != 0;
        }

        public static bool HasSelfOrInheritObfuzIgnoreScope(IHasCustomAttribute obj, TypeDef declaringType, ObfuzScope targetScope)
        {
            ObfuzScope? objScope = GetObfuzIgnoreScope(obj);
            if (objScope != null)
            {
                return (objScope & targetScope) != 0;
            }

            ObfuzScope? parentScope = GetSelfOrInheritObfuzIgnoreScope(declaringType);
            return parentScope != null && (parentScope & targetScope) != 0;
        }

        public static bool HasCompilerGeneratedAttribute(IHasCustomAttribute obj)
        {
            return obj.CustomAttributes.Find("System.Runtime.CompilerServices.CompilerGeneratedAttribute") != null;
        }

        public static bool HasEncryptFieldAttribute(IHasCustomAttribute obj)
        {
            return obj.CustomAttributes.Find("Obfuz.EncryptFieldAttribute") != null;
        }

        public static bool HasRuntimeInitializeOnLoadMethodAttribute(MethodDef method)
        {
            return method.CustomAttributes.Find("UnityEngine.RuntimeInitializeOnLoadMethodAttribute") != null;
        }

        public static bool HasBlackboardEnumAttribute(TypeDef typeDef)
        {
            return typeDef.CustomAttributes.Find("Unity.Behavior.BlackboardEnumAttribute") != null;
        }
    }
}
