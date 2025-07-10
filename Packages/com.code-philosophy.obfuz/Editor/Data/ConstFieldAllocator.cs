using dnlib.DotNet;
using dnlib.DotNet.Emit;
using Obfuz.Editor;
using Obfuz.Emit;
using Obfuz.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Assertions;

namespace Obfuz.Data
{
    public class ModuleConstFieldAllocator : IGroupByModuleEntity
    {
        private ModuleDef _module;
        private readonly EncryptionScopeProvider _encryptionScopeProvider;
        private readonly RvaDataAllocator _rvaDataAllocator;
        private readonly GroupByModuleEntityManager _moduleEntityManager;
        private EncryptionScopeInfo _encryptionScope;
        private RandomCreator _randomCreator;
        private IEncryptor _encryptor;

        private TypeDef _holderTypeDef;

        class ConstFieldInfo
        {
            public FieldDef field;
            public object value;
        }

        class AnyComparer : IEqualityComparer<object>
        {
            public new bool Equals(object x, object y)
            {
                if (x is byte[] xBytes && y is byte[] yBytes)
                {
                    return StructuralComparisons.StructuralEqualityComparer.Equals(xBytes, yBytes);
                }
                return x.Equals(y);
            }

            public static int ComputeHashCode(object obj)
            {
                return HashUtil.ComputePrimitiveOrStringOrBytesHashCode(obj);
            }

            public int GetHashCode(object obj)
            {
                return ComputeHashCode(obj);
            }
        }

        private readonly Dictionary<object, ConstFieldInfo> _allocatedFields = new Dictionary<object, ConstFieldInfo>(new AnyComparer());
        private readonly Dictionary<FieldDef, ConstFieldInfo> _field2Fields = new Dictionary<FieldDef, ConstFieldInfo>();

        private readonly List<TypeDef> _holderTypeDefs = new List<TypeDef>();
        private bool _done;


        public ModuleConstFieldAllocator(EncryptionScopeProvider encryptionScopeProvider, RvaDataAllocator rvaDataAllocator, GroupByModuleEntityManager moduleEntityManager)
        {
            _encryptionScopeProvider = encryptionScopeProvider;
            _rvaDataAllocator = rvaDataAllocator;
            _moduleEntityManager = moduleEntityManager;
        }

        public void Init(ModuleDef mod)
        {
            _module = mod;
            _encryptionScope = _encryptionScopeProvider.GetScope(mod);
            _randomCreator = _encryptionScope.localRandomCreator;
            _encryptor = _encryptionScope.encryptor;
        }

        const int maxFieldCount = 1000;


        private TypeSig GetTypeSigOfValue(object value)
        {
            if (value is int)
                return _module.CorLibTypes.Int32;
            if (value is long)
                return _module.CorLibTypes.Int64;
            if (value is float)
                return _module.CorLibTypes.Single;
            if (value is double)
                return _module.CorLibTypes.Double;
            if (value is string)
                return _module.CorLibTypes.String;
            if (value is byte[])
                return new SZArraySig(_module.CorLibTypes.Byte);
            throw new NotSupportedException($"Unsupported type: {value.GetType()}");
        }

        private ConstFieldInfo CreateConstFieldInfo(object value)
        {
            if (_holderTypeDef == null || _holderTypeDef.Fields.Count >= maxFieldCount)
            {
                _module.EnableTypeDefFindCache = false;
                ITypeDefOrRef objectTypeRef = _module.Import(typeof(object));
                _holderTypeDef = new TypeDefUser($"{ConstValues.ObfuzInternalSymbolNamePrefix}ConstFieldHolder${_holderTypeDefs.Count}", objectTypeRef);
                _module.Types.Add(_holderTypeDef);
                _holderTypeDefs.Add(_holderTypeDef);
                _module.EnableTypeDefFindCache = true;
            }

            var field = new FieldDefUser($"{ConstValues.ObfuzInternalSymbolNamePrefix}RVA_Value{_holderTypeDef.Fields.Count}", new FieldSig(GetTypeSigOfValue(value)), FieldAttributes.Static | FieldAttributes.Public | FieldAttributes.InitOnly);
            field.DeclaringType = _holderTypeDef;
            return new ConstFieldInfo
            {
                field = field,
                value = value,
            };
        }

        private FieldDef AllocateAny(object value)
        {
            if (_done)
            {
                throw new Exception("can't Allocate after done");
            }
            if (!_allocatedFields.TryGetValue(value, out var field))
            {
                field = CreateConstFieldInfo(value);
                _allocatedFields.Add(value, field);
                _field2Fields.Add(field.field, field);
            }
            return field.field;
        }

        public FieldDef Allocate(int value)
        {
            return AllocateAny(value);
        }

        public FieldDef Allocate(long value)
        {
            return AllocateAny(value);
        }

        public FieldDef Allocate(float value)
        {
            return AllocateAny(value);
        }

        public FieldDef Allocate(double value)
        {
            return AllocateAny(value);
        }

        public FieldDef Allocate(string value)
        {
            return AllocateAny(value);
        }

        public FieldDef Allocate(byte[] value)
        {
            return AllocateAny(value);
        }

        private DefaultMetadataImporter GetModuleMetadataImporter()
        {
            return _moduleEntityManager.GetDefaultModuleMetadataImporter(_module, _encryptionScopeProvider);
        }

        private void CreateCCtorOfRvaTypeDef(TypeDef type)
        {
            var cctor = new MethodDefUser(".cctor",
                MethodSig.CreateStatic(_module.CorLibTypes.Void),
                MethodImplAttributes.IL | MethodImplAttributes.Managed,
                MethodAttributes.Static | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName | MethodAttributes.Private);
            cctor.DeclaringType = type;
            //_rvaTypeDef.Methods.Add(cctor);
            var body = new CilBody();
            cctor.Body = body;
            var ins = body.Instructions;

            //IMethod method = _module.Import(typeof(System.Runtime.CompilerServices.RuntimeHelpers).GetMethod("InitializeArray", new[] { typeof(Array), typeof(RuntimeFieldHandle) }));
            //Assert.IsNotNull(method);


            DefaultMetadataImporter importer = GetModuleMetadataImporter();
            // TODO. obfuscate init codes
            foreach (var field in type.Fields)
            {
                ConstFieldInfo constInfo = _field2Fields[field];
                IRandom localRandom = _randomCreator(HashUtil.ComputePrimitiveOrStringOrBytesHashCode(constInfo.value));
                int ops = EncryptionUtil.GenerateEncryptionOpCodes(localRandom, _encryptor, 4);
                int salt = localRandom.NextInt();
                switch (constInfo.value)
                {
                    case int i:
                    {
                        int encryptedValue = _encryptor.Encrypt(i, ops, salt);
                        RvaData rvaData = _rvaDataAllocator.Allocate(_module, encryptedValue);
                        ins.Add(Instruction.Create(OpCodes.Ldsfld, rvaData.field));
                        ins.Add(Instruction.CreateLdcI4(rvaData.offset));
                        ins.Add(Instruction.CreateLdcI4(ops));
                        ins.Add(Instruction.CreateLdcI4(salt));
                        ins.Add(Instruction.Create(OpCodes.Call, importer.DecryptFromRvaInt));
                        break;
                    }
                    case long l:
                    {
                        long encryptedValue = _encryptor.Encrypt(l, ops, salt);
                        RvaData rvaData = _rvaDataAllocator.Allocate(_module, encryptedValue);
                        ins.Add(Instruction.Create(OpCodes.Ldsfld, rvaData.field));
                        ins.Add(Instruction.CreateLdcI4(rvaData.offset));
                        ins.Add(Instruction.CreateLdcI4(ops));
                        ins.Add(Instruction.CreateLdcI4(salt));
                        ins.Add(Instruction.Create(OpCodes.Call, importer.DecryptFromRvaLong));
                        break;
                    }
                    case float f:
                    {
                        float encryptedValue = _encryptor.Encrypt(f, ops, salt);
                        RvaData rvaData = _rvaDataAllocator.Allocate(_module, encryptedValue);
                        ins.Add(Instruction.Create(OpCodes.Ldsfld, rvaData.field));
                        ins.Add(Instruction.CreateLdcI4(rvaData.offset));
                        ins.Add(Instruction.CreateLdcI4(ops));
                        ins.Add(Instruction.CreateLdcI4(salt));
                        ins.Add(Instruction.Create(OpCodes.Call, importer.DecryptFromRvaFloat));
                        break;
                    }
                    case double d:
                    {
                        double encryptedValue = _encryptor.Encrypt(d, ops, salt);
                        RvaData rvaData = _rvaDataAllocator.Allocate(_module, encryptedValue);
                        ins.Add(Instruction.Create(OpCodes.Ldsfld, rvaData.field));
                        ins.Add(Instruction.CreateLdcI4(rvaData.offset));
                        ins.Add(Instruction.CreateLdcI4(ops));
                        ins.Add(Instruction.CreateLdcI4(salt));
                        ins.Add(Instruction.Create(OpCodes.Call, importer.DecryptFromRvaDouble));
                        break;
                    }
                    case string s:
                    {
                        byte[] encryptedValue = _encryptor.Encrypt(s, ops, salt);
                        RvaData rvaData = _rvaDataAllocator.Allocate(_module, encryptedValue);
                        ins.Add(Instruction.Create(OpCodes.Ldsfld, rvaData.field));
                        ins.Add(Instruction.CreateLdcI4(rvaData.offset));
                        Assert.AreEqual(encryptedValue.Length, rvaData.size);
                        ins.Add(Instruction.CreateLdcI4(encryptedValue.Length));
                        ins.Add(Instruction.CreateLdcI4(ops));
                        ins.Add(Instruction.CreateLdcI4(salt));
                        ins.Add(Instruction.Create(OpCodes.Call, importer.DecryptFromRvaString));
                        break;
                    }
                    case byte[] bs:
                    {
                        byte[] encryptedValue = _encryptor.Encrypt(bs, 0, bs.Length, ops, salt);
                        Assert.AreEqual(encryptedValue.Length, bs.Length);
                        RvaData rvaData = _rvaDataAllocator.Allocate(_module, encryptedValue);
                        ins.Add(Instruction.Create(OpCodes.Ldsfld, rvaData.field));
                        ins.Add(Instruction.CreateLdcI4(rvaData.offset));
                        ins.Add(Instruction.CreateLdcI4(bs.Length));
                        ins.Add(Instruction.CreateLdcI4(ops));
                        ins.Add(Instruction.CreateLdcI4(salt));
                        ins.Add(Instruction.Create(OpCodes.Call, importer.DecryptFromRvaBytes));
                        break;
                    }
                    default: throw new NotSupportedException($"Unsupported type: {constInfo.value.GetType()}");
                }
                ins.Add(Instruction.Create(OpCodes.Stsfld, field));
            }
            ins.Add(Instruction.Create(OpCodes.Ret));
        }

        public void Done()
        {
            if (_done)
            {
                throw new Exception("Already done");
            }
            _done = true;
            foreach (var typeDef in _holderTypeDefs)
            {
                CreateCCtorOfRvaTypeDef(typeDef);
            }
        }
    }

    public class ConstFieldAllocator
    {
        private readonly EncryptionScopeProvider _encryptionScopeProvider;
        private readonly RvaDataAllocator _rvaDataAllocator;
        private readonly GroupByModuleEntityManager _moduleEntityManager;

        public ConstFieldAllocator(EncryptionScopeProvider encryptionScopeProvider, RvaDataAllocator rvaDataAllocator, GroupByModuleEntityManager moduleEntityManager)
        {
            _encryptionScopeProvider = encryptionScopeProvider;
            _rvaDataAllocator = rvaDataAllocator;
            _moduleEntityManager = moduleEntityManager;
        }

        private ModuleConstFieldAllocator GetModuleAllocator(ModuleDef mod)
        {
            return _moduleEntityManager.GetEntity<ModuleConstFieldAllocator>(mod, () => new ModuleConstFieldAllocator(_encryptionScopeProvider, _rvaDataAllocator, _moduleEntityManager));
        }

        public FieldDef Allocate(ModuleDef mod, int value)
        {
            return GetModuleAllocator(mod).Allocate(value);
        }

        public FieldDef Allocate(ModuleDef mod, long value)
        {
            return GetModuleAllocator(mod).Allocate(value);
        }

        public FieldDef Allocate(ModuleDef mod, float value)
        {
            return GetModuleAllocator(mod).Allocate(value);
        }

        public FieldDef Allocate(ModuleDef mod, double value)
        {
            return GetModuleAllocator(mod).Allocate(value);
        }

        public FieldDef Allocate(ModuleDef mod, byte[] value)
        {
            return GetModuleAllocator(mod).Allocate(value);
        }

        public FieldDef Allocate(ModuleDef mod, string value)
        {
            return GetModuleAllocator(mod).Allocate(value);
        }

        public void Done()
        {
            foreach (var moduleAllocator in _moduleEntityManager.GetEntities<ModuleConstFieldAllocator>())
            {
                moduleAllocator.Done();
            }
        }
    }
}
