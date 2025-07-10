using dnlib.DotNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Obfuz.Utils
{
    public static class TypeSigUtil
    {
        public static string ComputeTypeDefSignature(TypeDef type)
        {
            return type.FullName;
        }

        public static string ComputeMethodDefSignature(MethodDef method)
        {
            var result = new StringBuilder();
            ComputeTypeSigName(method.MethodSig.RetType, result);
            result.Append(" ");
            result.Append(method.DeclaringType.FullName);
            result.Append("::");
            result.Append(method.Name);
            if (method.HasGenericParameters)
            {
                result.Append($"`{method.GenericParameters.Count}");
            }
            result.Append("(");
            for (int i = 0; i < method.Parameters.Count; i++)
            {
                if (i > 0)
                {
                    result.Append(", ");
                }
                ComputeTypeSigName(method.Parameters[i].Type, result);
            }
            result.Append(")");
            return result.ToString();
        }

        public static string ComputeFieldDefSignature(FieldDef field)
        {
            var result = new StringBuilder();
            ComputeTypeSigName(field.FieldSig.Type, result);
            result.Append(" ");
            result.Append(field.Name);
            return result.ToString();
        }

        public static string ComputePropertyDefSignature(PropertyDef property)
        {
            var result = new StringBuilder();

            PropertySig propertySig = property.PropertySig;
            ComputeTypeSigName(propertySig.RetType, result);
            result.Append(" ");
            result.Append(property.Name);

            IList<TypeSig> parameters = propertySig.Params;
            if (parameters.Count > 0)
            {
                result.Append("(");

                for (int i = 0; i < parameters.Count; i++)
                {
                    if (i > 0)
                    {
                        result.Append(", ");
                    }
                    ComputeTypeSigName(parameters[i], result);
                }
                result.Append(")");
            }
            
            return result.ToString();
        }

        public static string ComputeEventDefSignature(EventDef eventDef)
        {
            var result = new StringBuilder();
            ComputeTypeSigName(eventDef.EventType.ToTypeSig(), result);
            result.Append(" ");
            result.Append(eventDef.Name);
            return result.ToString();
        }

        public static string ComputeMethodSpecSignature(TypeSig type)
        {
            var sb = new StringBuilder();
            ComputeTypeSigName(type, sb);
            return sb.ToString();
        }

        public static void ComputeTypeSigName(TypeSig type, StringBuilder result)
        {
            type = type.RemovePinnedAndModifiers();
            switch (type.ElementType)
            {
                case ElementType.Void: result.Append("void"); break;
                case ElementType.Boolean: result.Append("bool"); break;
                case ElementType.Char: result.Append("char"); break;
                case ElementType.I1: result.Append("sbyte"); break;
                case ElementType.U1: result.Append("byte"); break;
                case ElementType.I2: result.Append("short"); break;
                case ElementType.U2: result.Append("ushort"); break;
                case ElementType.I4: result.Append("int"); break;
                case ElementType.U4: result.Append("uint"); break;
                case ElementType.I8: result.Append("long"); break;
                case ElementType.U8: result.Append("ulong"); break;
                case ElementType.R4: result.Append("float"); break;
                case ElementType.R8: result.Append("double"); break;
                case ElementType.String: result.Append("string"); break;
                case ElementType.Ptr:
                ComputeTypeSigName(((PtrSig)type).Next, result);
                result.Append("*");
                break;
                case ElementType.ByRef:
                ComputeTypeSigName(((ByRefSig)type).Next, result);
                result.Append("&");
                break;
                case ElementType.ValueType:
                case ElementType.Class:
                {
                    var valueOrClassType = type.ToClassOrValueTypeSig();
                    var typeDef = valueOrClassType.ToTypeDefOrRef().ResolveTypeDefThrow();
                    if (typeDef.Module.IsCoreLibraryModule != true)
                    {
                        result.Append($"[{typeDef.Module.Assembly.Name}]");
                    }
                    result.Append(typeDef.FullName);
                    break;
                }
                case ElementType.GenericInst:
                {
                    var genInst = (GenericInstSig)type;
                    ComputeTypeSigName(genInst.GenericType, result);
                    result.Append("<");
                    for (int i = 0; i < genInst.GenericArguments.Count; i++)
                    {
                        if (i > 0)
                        {
                            result.Append(",");
                        }
                        ComputeTypeSigName(genInst.GenericArguments[i], result);
                    }
                    result.Append(">");
                    break;
                }
                case ElementType.SZArray:
                ComputeTypeSigName(((SZArraySig)type).Next, result);
                result.Append("[]");
                break;
                case ElementType.Array:
                {
                    var arraySig = (ArraySig)type;
                    ComputeTypeSigName(arraySig.Next, result);
                    result.Append("[");
                    for (int i = 0; i < arraySig.Rank; i++)
                    {
                        if (i > 0)
                        {
                            result.Append(",");
                        }
                        //result.Append(arraySig.Sizes[i]);
                    }
                    result.Append("]");
                    break;
                }
                case ElementType.FnPtr:
                {
                    var fnPtr = (FnPtrSig)type;
                    result.Append("(");
                    MethodSig ms = fnPtr.MethodSig;
                    ComputeTypeSigName(ms.RetType, result);
                    result.Append("(");
                    for (int i = 0; i < ms.Params.Count; i++)
                    {
                        if (i > 0)
                        {
                            result.Append(",");
                        }
                        ComputeTypeSigName(ms.Params[i], result);
                    }
                    result.Append(")*");
                    break;
                }
                case ElementType.TypedByRef:
                result.Append("typedref");
                break;
                case ElementType.I:
                result.Append("nint");
                break;
                case ElementType.U:
                result.Append("nuint");
                break;
                case ElementType.Object:
                result.Append("object");
                break;
                case ElementType.Var:
                {
                    var var = (GenericVar)type;
                    result.Append($"!{var.Number}");
                    break;
                }
                case ElementType.MVar:
                {
                    var mvar = (GenericMVar)type;
                    result.Append($"!!{mvar.Number}");
                    break;
                }
                default: throw new NotSupportedException($"[ComputeTypeSigName] not support :{type}");

            }
        }
    }
}
