using dnlib.DotNet;
using dnlib.DotNet.Emit;
using Obfuz.Emit;
using Obfuz.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.Assertions;

namespace Obfuz.Data
{
    public struct RvaData
    {
        public readonly FieldDef field;
        public readonly int offset;
        public readonly int size;

        public RvaData(FieldDef field, int offset, int size)
        {
            this.field = field;
            this.offset = offset;
            this.size = size;
        }
    }

    public class ModuleRvaDataAllocator : GroupByModuleEntityBase
    {
        // randomized
        const int maxRvaDataSize = 0x1000;

        private ModuleDef _module;
        private readonly EncryptionScopeProvider _encryptionScopeProvider;
        private readonly GroupByModuleEntityManager _moduleEntityManager;

        private EncryptionScopeInfo _encryptionScope;
        private IRandom _random;

        class RvaField
        {
            public FieldDef holderDataField;
            public FieldDef runtimeValueField;
            public int encryptionOps;
            public uint size;
            public List<byte> bytes;
            public int salt;

            public void FillPaddingToSize(int newSize)
            {
                for (int i = bytes.Count; i < newSize; i++)
                {
                    bytes.Add(0xAB);
                }
            }

            public void FillPaddingToEnd()
            {
                // fill with random value
                for (int i = bytes.Count; i < size; i++)
                {
                    bytes.Add(0xAB);
                }
            }
        }

        private readonly List<RvaField> _rvaFields = new List<RvaField>();
        private RvaField _currentField;


        private TypeDef _rvaTypeDef;

        private readonly Dictionary<int, TypeDef> _dataHolderTypeBySizes = new Dictionary<int, TypeDef>();
        private bool _done;

        public ModuleRvaDataAllocator(EncryptionScopeProvider encryptionScopeProvider, GroupByModuleEntityManager moduleEntityManager)
        {
            _encryptionScopeProvider = encryptionScopeProvider;
            _moduleEntityManager = moduleEntityManager;
        }

        public override void Init(ModuleDef mod)
        {
            _module = mod;
            _encryptionScope = _encryptionScopeProvider.GetScope(mod);
            _random = _encryptionScope.localRandomCreator(HashUtil.ComputeHash(mod.Name));
        }

        private (FieldDef, FieldDef) CreateDataHolderRvaField(TypeDef dataHolderType)
        {
            if (_rvaTypeDef == null)
            {
                _module.EnableTypeDefFindCache = false;
                //_rvaTypeDef = _module.Find("$ObfuzRVA$", true);
                //if (_rvaTypeDef != null)
                //{
                //    throw new Exception($"can't obfuscate a obfuscated assembly");
                //}
                ITypeDefOrRef objectTypeRef = _module.Import(typeof(object));
                _rvaTypeDef = new TypeDefUser("$Obfuz$RVA$", objectTypeRef);
                _module.Types.Add(_rvaTypeDef);
                _module.EnableTypeDefFindCache = true;
            }


            var holderField = new FieldDefUser($"$RVA_Data{_rvaFields.Count}", new FieldSig(dataHolderType.ToTypeSig()), FieldAttributes.InitOnly | FieldAttributes.Static | FieldAttributes.HasFieldRVA);
            holderField.DeclaringType = _rvaTypeDef;

            var runtimeValueField = new FieldDefUser($"$RVA_Value{_rvaFields.Count}", new FieldSig(new SZArraySig(_module.CorLibTypes.Byte)), FieldAttributes.Static | FieldAttributes.Public);
            runtimeValueField.DeclaringType = _rvaTypeDef;
            return (holderField, runtimeValueField);
        }

        private TypeDef GetDataHolderType(int size)
        {
            size = (size + 15) & ~15; // align to 6 bytes
            if (_dataHolderTypeBySizes.TryGetValue(size, out var type))
                return type;
            var dataHolderType = new TypeDefUser($"$ObfuzRVA$DataHolder{size}", _module.Import(typeof(ValueType)));
            dataHolderType.Attributes = TypeAttributes.Public | TypeAttributes.Sealed;
            dataHolderType.Layout = TypeAttributes.ExplicitLayout;
            dataHolderType.PackingSize = 1;
            dataHolderType.ClassSize = (uint)size;
            _dataHolderTypeBySizes.Add(size, dataHolderType);
            _module.Types.Add(dataHolderType);
            return dataHolderType;
        }

        private static int AlignTo(int size, int alignment)
        {
            return (size + alignment - 1) & ~(alignment - 1);
        }

        private RvaField CreateRvaField(int size)
        {
            TypeDef dataHolderType = GetDataHolderType(size);
            var (holderDataField, runtimeValueField) = CreateDataHolderRvaField(dataHolderType);
            var newRvaField = new RvaField
            {
                holderDataField = holderDataField,
                runtimeValueField = runtimeValueField,
                size = dataHolderType.ClassSize,
                bytes = new List<byte>((int)dataHolderType.ClassSize),
                encryptionOps = _random.NextInt(),
                salt = _random.NextInt(),
            };
            _rvaFields.Add(newRvaField);
            return newRvaField;
        }

        private RvaField GetRvaField(int preservedSize, int alignment)
        {
            if (_done)
            {
                throw new Exception("can't GetRvaField after done");
            }
            Assert.IsTrue(preservedSize % alignment == 0);
            // for big size, create a new field
            if (preservedSize >= maxRvaDataSize)
            {
                return CreateRvaField(preservedSize);
            }

            if (_currentField != null)
            {
                int offset = AlignTo(_currentField.bytes.Count, alignment);

                int expectedSize = offset + preservedSize;
                if (expectedSize <= _currentField.size)
                {
                    _currentField.FillPaddingToSize(offset);
                    return _currentField;
                }

                _currentField.FillPaddingToEnd();
            }
            _currentField = CreateRvaField(maxRvaDataSize);
            return _currentField;
        }

        public RvaData Allocate(int value)
        {
            RvaField field = GetRvaField(4, 4);
            int offset = field.bytes.Count;
            Assert.IsTrue(offset % 4 == 0);
            field.bytes.AddRange(BitConverter.GetBytes(value));
            return new RvaData(field.runtimeValueField, offset, 4);
        }

        public RvaData Allocate(long value)
        {
            RvaField field = GetRvaField(8, 8);
            int offset = field.bytes.Count;
            Assert.IsTrue(offset % 8 == 0);
            field.bytes.AddRange(BitConverter.GetBytes(value));
            return new RvaData(field.runtimeValueField, offset, 8);
        }

        public RvaData Allocate(float value)
        {
            RvaField field = GetRvaField(4, 4);
            int offset = field.bytes.Count;
            Assert.IsTrue(offset % 4 == 0);
            field.bytes.AddRange(BitConverter.GetBytes(value));
            return new RvaData(field.runtimeValueField, offset, 4);
        }

        public RvaData Allocate(double value)
        {
            RvaField field = GetRvaField(8, 8);
            int offset = field.bytes.Count;
            Assert.IsTrue(offset % 8 == 0);
            field.bytes.AddRange(BitConverter.GetBytes(value));
            return new RvaData(field.runtimeValueField, offset, 8);
        }

        public RvaData Allocate(string value)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(value);
            return Allocate(bytes);
        }

        public RvaData Allocate(byte[] value)
        {
            RvaField field = GetRvaField(value.Length, 1);
            int offset = field.bytes.Count;
            field.bytes.AddRange(value);
            return new RvaData(field.runtimeValueField, offset, value.Length);
        }


        private void AddVerifyCodes(IList<Instruction> insts, DefaultMetadataImporter importer)
        {
            int verifyIntValue = 0x12345678;
            IRandom verifyRandom = _encryptionScope.localRandomCreator(verifyIntValue);
            int verifyOps = EncryptionUtil.GenerateEncryptionOpCodes(verifyRandom, _encryptionScope.encryptor, 4);
            int verifySalt = verifyRandom.NextInt();
            int encryptedVerifyIntValue = _encryptionScope.encryptor.Encrypt(verifyIntValue, verifyOps, verifySalt);

            insts.Add(Instruction.Create(OpCodes.Ldc_I4, verifyIntValue));
            insts.Add(Instruction.CreateLdcI4(encryptedVerifyIntValue));
            insts.Add(Instruction.CreateLdcI4(verifyOps));
            insts.Add(Instruction.CreateLdcI4(verifySalt));
            insts.Add(Instruction.Create(OpCodes.Call, importer.DecryptInt));
            insts.Add(Instruction.Create(OpCodes.Call, importer.VerifySecretKey));

        }

        private void CreateCCtorOfRvaTypeDef()
        {
            if (_rvaTypeDef == null)
            {
                return;
            }
            ModuleDef mod = _rvaTypeDef.Module;
            var cctorMethod = new MethodDefUser(".cctor",
                MethodSig.CreateStatic(_module.CorLibTypes.Void),
                MethodImplAttributes.IL | MethodImplAttributes.Managed,
                MethodAttributes.Static | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName | MethodAttributes.Private);
            cctorMethod.DeclaringType = _rvaTypeDef;
            //_rvaTypeDef.Methods.Add(cctor);
            var body = new CilBody();
            cctorMethod.Body = body;
            var ins = body.Instructions;

            DefaultMetadataImporter importer = _moduleEntityManager.GetDefaultModuleMetadataImporter(mod, _encryptionScopeProvider);
            AddVerifyCodes(ins, importer);
            foreach (var field in _rvaFields)
            {
                // ldc
                // newarr
                // dup
                // stsfld
                // ldtoken
                // RuntimeHelpers.InitializeArray(array, fieldHandle);
                ins.Add(Instruction.Create(OpCodes.Ldc_I4, (int)field.size));
                ins.Add(Instruction.Create(OpCodes.Newarr, field.runtimeValueField.FieldType.Next.ToTypeDefOrRef()));
                ins.Add(Instruction.Create(OpCodes.Dup));
                ins.Add(Instruction.Create(OpCodes.Dup));
                ins.Add(Instruction.Create(OpCodes.Stsfld, field.runtimeValueField));
                ins.Add(Instruction.Create(OpCodes.Ldtoken, field.holderDataField));
                ins.Add(Instruction.Create(OpCodes.Call, importer.InitializedArray));

                // EncryptionService.DecryptBlock(array, field.encryptionOps, field.salt);
                ins.Add(Instruction.CreateLdcI4(field.encryptionOps));
                ins.Add(Instruction.Create(OpCodes.Ldc_I4, field.salt));
                ins.Add(Instruction.Create(OpCodes.Call, importer.DecryptBlock));

            }
            ins.Add(Instruction.Create(OpCodes.Ret));
        }

        private void SetFieldsRVA()
        {
            foreach (var field in _rvaFields)
            {
                Assert.IsTrue(field.bytes.Count <= field.size);
                if (field.bytes.Count < field.size)
                {
                    field.FillPaddingToEnd();
                }
                byte[] data = field.bytes.ToArray();
                _encryptionScope.encryptor.EncryptBlock(data, field.encryptionOps, field.salt);
                field.holderDataField.InitialValue = data;
            }
        }

        public void Done()
        {
            if (_done)
            {
                throw new Exception("can't call Done twice");
            }
            _done = true;
            SetFieldsRVA();
            CreateCCtorOfRvaTypeDef();
        }
    }

    public class RvaDataAllocator
    {
        private readonly EncryptionScopeProvider _encryptionScopeProvider;
        private readonly GroupByModuleEntityManager _moduleEntityManager;

        public RvaDataAllocator(EncryptionScopeProvider encryptionScopeProvider, GroupByModuleEntityManager moduleEntityManager)
        {
            _encryptionScopeProvider = encryptionScopeProvider;
            _moduleEntityManager = moduleEntityManager;
        }

        private ModuleRvaDataAllocator GetModuleRvaDataAllocator(ModuleDef mod)
        {
            return _moduleEntityManager.GetEntity<ModuleRvaDataAllocator>(mod, () => new ModuleRvaDataAllocator(_encryptionScopeProvider, _moduleEntityManager));
        }

        public RvaData Allocate(ModuleDef mod, int value)
        {
            return GetModuleRvaDataAllocator(mod).Allocate(value);
        }

        public RvaData Allocate(ModuleDef mod, long value)
        {
            return GetModuleRvaDataAllocator(mod).Allocate(value);
        }

        public RvaData Allocate(ModuleDef mod, float value)
        {
            return GetModuleRvaDataAllocator(mod).Allocate(value);
        }

        public RvaData Allocate(ModuleDef mod, double value)
        {
            return GetModuleRvaDataAllocator(mod).Allocate(value);
        }

        public RvaData Allocate(ModuleDef mod, string value)
        {
            return GetModuleRvaDataAllocator(mod).Allocate(value);
        }

        public RvaData Allocate(ModuleDef mod, byte[] value)
        {
            return GetModuleRvaDataAllocator(mod).Allocate(value);
        }

        public void Done()
        {
            foreach (var allocator in _moduleEntityManager.GetEntities<ModuleRvaDataAllocator>())
            {
                allocator.Done();
            }
        }
    }
}
