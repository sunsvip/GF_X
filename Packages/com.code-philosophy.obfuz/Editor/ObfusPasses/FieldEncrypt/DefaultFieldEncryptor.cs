using dnlib.DotNet;
using dnlib.DotNet.Emit;
using Obfuz.Emit;
using Obfuz.Settings;
using Obfuz.Utils;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using UnityEngine.Assertions;

namespace Obfuz.ObfusPasses.FieldEncrypt
{
    public class DefaultFieldEncryptor : FieldEncryptorBase
    {
        private readonly EncryptionScopeProvider _encryptionScopeProvider;
        private readonly GroupByModuleEntityManager _moduleEntityManager;
        private readonly FieldEncryptionSettingsFacade _settings;

        public DefaultFieldEncryptor(EncryptionScopeProvider encryptionScopeProvider, GroupByModuleEntityManager moduleEntityManager, FieldEncryptionSettingsFacade settings)
        {
            _encryptionScopeProvider = encryptionScopeProvider;
            _moduleEntityManager = moduleEntityManager;
            _settings = settings;
        }

        private DefaultMetadataImporter GetMetadataImporter(MethodDef method)
        {
            return _moduleEntityManager.GetDefaultModuleMetadataImporter(method.Module, _encryptionScopeProvider);
        }

         class FieldEncryptInfo
        {
            public int encryptOps;
            public int salt;
            public ElementType fieldType;
            public long xorValueForZero;
        }

        private readonly Dictionary<FieldDef, FieldEncryptInfo> _fieldEncryptInfoCache = new Dictionary<FieldDef, FieldEncryptInfo>();


        private long CalcXorValueForZero(IEncryptor encryptor, ElementType type, int encryptOps, int salt)
        {
            switch (type)
            {
                case ElementType.I4:
                case ElementType.U4:
                case ElementType.R4:
                    return encryptor.Encrypt(0, encryptOps, salt);
                case ElementType.I8:
                case ElementType.U8:
                case ElementType.R8:
                return encryptor.Encrypt(0L, encryptOps, salt);
                default:
                throw new NotSupportedException($"Unsupported field type: {type} for encryption");
            }
        }


        private IRandom CreateRandomForField(RandomCreator randomCreator, FieldDef field)
        {
            return randomCreator(FieldEqualityComparer.CompareDeclaringTypes.GetHashCode(field));
        }

        private int GenerateEncryptionOperations(IRandom random, IEncryptor encryptor)
        {
            return EncryptionUtil.GenerateEncryptionOpCodes(random, encryptor, _settings.encryptionLevel);
        }

        public int GenerateSalt(IRandom random)
        {
            return random.NextInt();
        }

        private FieldEncryptInfo GetFieldEncryptInfo(FieldDef field)
        {
            if (_fieldEncryptInfoCache.TryGetValue(field, out var info))
            {
                return info;
            }
            EncryptionScopeInfo encryptionScope = _encryptionScopeProvider.GetScope(field.Module);

            IRandom random = CreateRandomForField(encryptionScope.localRandomCreator, field);
            IEncryptor encryptor = encryptionScope.encryptor;
            int encryptOps = GenerateEncryptionOperations(random, encryptor);
            int salt = GenerateSalt(random);
            ElementType fieldType = field.FieldSig.Type.RemovePinnedAndModifiers().ElementType;
            long xorValueForZero = CalcXorValueForZero(encryptor, fieldType, encryptOps, salt);

            info = new FieldEncryptInfo
            {
                encryptOps = encryptOps,
                salt = salt,
                fieldType = fieldType,
                xorValueForZero = xorValueForZero,
            };
            _fieldEncryptInfoCache[field] = info;
            return info;
        }

        public override void Encrypt(MethodDef method, FieldDef field, List<Instruction> outputInstructions, Instruction currentInstruction)
        {
            DefaultMetadataImporter importer = GetMetadataImporter(method);
            EncryptionServiceMetadataImporter encryptionServiceMetadataImporter = importer.GetEncryptionServiceMetadataImporterOfModule(field.Module);
            FieldEncryptInfo fei = GetFieldEncryptInfo(field);
            if (fei.fieldType == ElementType.I4 || fei.fieldType == ElementType.U4 || fei.fieldType == ElementType.R4)
            {
                // value has been put on stack

                if (fei.fieldType == ElementType.R4)
                {
                    outputInstructions.Add(Instruction.Create(OpCodes.Call, importer.CastFloatAsInt));
                }
                // encrypt
                outputInstructions.Add(Instruction.CreateLdcI4(fei.encryptOps));
                outputInstructions.Add(Instruction.CreateLdcI4(fei.salt));
                outputInstructions.Add(Instruction.Create(OpCodes.Call, encryptionServiceMetadataImporter.EncryptInt));
                // xor
                outputInstructions.Add(Instruction.CreateLdcI4((int)fei.xorValueForZero));
                outputInstructions.Add(Instruction.Create(OpCodes.Xor));

                if (fei.fieldType == ElementType.R4)
                {
                    outputInstructions.Add(Instruction.Create(OpCodes.Call, importer.CastIntAsFloat));
                }
            }
            else if (fei.fieldType == ElementType.I8 || fei.fieldType == ElementType.U8 || fei.fieldType == ElementType.R8)
            {
                // value has been put on stack
                if (fei.fieldType == ElementType.R8)
                {
                    outputInstructions.Add(Instruction.Create(OpCodes.Call, importer.CastDoubleAsLong));
                }

                // encrypt
                outputInstructions.Add(Instruction.CreateLdcI4(fei.encryptOps));
                outputInstructions.Add(Instruction.CreateLdcI4(fei.salt));
                outputInstructions.Add(Instruction.Create(OpCodes.Call, encryptionServiceMetadataImporter.EncryptLong));
                // xor
                outputInstructions.Add(Instruction.Create(OpCodes.Ldc_I8, fei.xorValueForZero));
                outputInstructions.Add(Instruction.Create(OpCodes.Xor));
                if (fei.fieldType == ElementType.R8)
                {
                    outputInstructions.Add(Instruction.Create(OpCodes.Call, importer.CastLongAsDouble));
                }
            }
            else
            {
                Assert.IsTrue(false, $"Unsupported field type: {fei.fieldType} for encryption");
            }

            outputInstructions.Add(currentInstruction.Clone());
        }

        public override void Decrypt(MethodDef method, FieldDef field, List<Instruction> outputInstructions, Instruction currentInstruction)
        {
            outputInstructions.Add(currentInstruction.Clone());
            DefaultMetadataImporter importer = GetMetadataImporter(method);
            EncryptionServiceMetadataImporter encryptionServiceMetadataImporter = importer.GetEncryptionServiceMetadataImporterOfModule(field.Module);
            FieldEncryptInfo fei = GetFieldEncryptInfo(field);
            if (fei.fieldType == ElementType.I4 || fei.fieldType == ElementType.U4 || fei.fieldType == ElementType.R4)
            {
                // value has been put on stack
                // xor
                if (fei.fieldType == ElementType.R4)
                {
                    outputInstructions.Add(Instruction.Create(OpCodes.Call, importer.CastFloatAsInt));
                }
                outputInstructions.Add(Instruction.CreateLdcI4((int)fei.xorValueForZero));
                outputInstructions.Add(Instruction.Create(OpCodes.Xor));

                // decrypt
                outputInstructions.Add(Instruction.CreateLdcI4(fei.encryptOps));
                outputInstructions.Add(Instruction.CreateLdcI4(fei.salt));
                outputInstructions.Add(Instruction.Create(OpCodes.Call, encryptionServiceMetadataImporter.DecryptInt));

                if (fei.fieldType == ElementType.R4)
                {
                    outputInstructions.Add(Instruction.Create(OpCodes.Call, importer.CastIntAsFloat));
                }
            }
            else if (fei.fieldType == ElementType.I8 || fei.fieldType == ElementType.U8 || fei.fieldType == ElementType.R8)
            {
                // value has been put on stack
                // xor
                if (fei.fieldType == ElementType.R8)
                {
                    outputInstructions.Add(Instruction.Create(OpCodes.Call, importer.CastDoubleAsLong));
                }
                outputInstructions.Add(Instruction.Create(OpCodes.Ldc_I8, fei.xorValueForZero));
                outputInstructions.Add(Instruction.Create(OpCodes.Xor));

                // decrypt
                outputInstructions.Add(Instruction.CreateLdcI4(fei.encryptOps));
                outputInstructions.Add(Instruction.CreateLdcI4(fei.salt));
                outputInstructions.Add(Instruction.Create(OpCodes.Call, encryptionServiceMetadataImporter.DecryptLong));

                if (fei.fieldType == ElementType.R8)
                {
                    outputInstructions.Add(Instruction.Create(OpCodes.Call, importer.CastLongAsDouble));
                }
            }
            else
            {
                Assert.IsTrue(false, $"Unsupported field type: {fei.fieldType} for decryption");
            }
        }
    }
}
