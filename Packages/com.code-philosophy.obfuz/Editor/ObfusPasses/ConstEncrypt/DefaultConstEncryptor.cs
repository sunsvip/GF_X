using dnlib.DotNet;
using dnlib.DotNet.Emit;
using Obfuz.Emit;
using Obfuz.Data;
using Obfuz.Utils;
using System;
using System.Collections.Generic;
using NUnit.Framework;
using System.Text;
using Obfuz.Settings;

namespace Obfuz.ObfusPasses.ConstEncrypt
{
    public class DefaultConstEncryptor : IConstEncryptor
    {
        private readonly EncryptionScopeProvider _encryptionScopeProvider;
        private readonly RvaDataAllocator _rvaDataAllocator;
        private readonly ConstFieldAllocator _constFieldAllocator;
        private readonly GroupByModuleEntityManager _moduleEntityManager;
        private readonly ConstEncryptionSettingsFacade _settings;

        public DefaultConstEncryptor(EncryptionScopeProvider encryptionScopeProvider, RvaDataAllocator rvaDataAllocator, ConstFieldAllocator constFieldAllocator, GroupByModuleEntityManager moduleEntityManager, ConstEncryptionSettingsFacade settings)
        {
            _encryptionScopeProvider = encryptionScopeProvider;
            _rvaDataAllocator = rvaDataAllocator;
            _constFieldAllocator = constFieldAllocator;
            _moduleEntityManager = moduleEntityManager;
            _settings = settings;
        }

        private IRandom CreateRandomForValue(EncryptionScopeInfo encryptionScope, int value)
        {
            return encryptionScope.localRandomCreator(value);
        }

        private int GenerateEncryptionOperations(EncryptionScopeInfo encryptionScope, IRandom random)
        {
            return EncryptionUtil.GenerateEncryptionOpCodes(random, encryptionScope.encryptor, _settings.encryptionLevel);
        }

        public int GenerateSalt(IRandom random)
        {
            return random.NextInt();
        }

        private DefaultMetadataImporter GetModuleMetadataImporter(MethodDef method)
        {
            return _moduleEntityManager.GetDefaultModuleMetadataImporter(method.Module, _encryptionScopeProvider);
        }

        public void ObfuscateInt(MethodDef method, bool needCacheValue, int value, List<Instruction> obfuscatedInstructions)
        {
            if (needCacheValue)
            {
                FieldDef cacheField = _constFieldAllocator.Allocate(method.Module, value);
                obfuscatedInstructions.Add(Instruction.Create(OpCodes.Ldsfld, cacheField));
                return;
            }

            EncryptionScopeInfo encryptionScope = _encryptionScopeProvider.GetScope(method.Module);
            IRandom random = CreateRandomForValue(encryptionScope, value.GetHashCode());
            int ops = GenerateEncryptionOperations(encryptionScope, random);
            int salt = GenerateSalt(random);
            int encryptedValue = encryptionScope.encryptor.Encrypt(value, ops, salt);
            RvaData rvaData = _rvaDataAllocator.Allocate(method.Module, encryptedValue);

            DefaultMetadataImporter importer = GetModuleMetadataImporter(method);
            obfuscatedInstructions.Add(Instruction.Create(OpCodes.Ldsfld, rvaData.field));
            obfuscatedInstructions.Add(Instruction.CreateLdcI4(rvaData.offset));
            obfuscatedInstructions.Add(Instruction.CreateLdcI4(ops));
            obfuscatedInstructions.Add(Instruction.CreateLdcI4(salt));
            obfuscatedInstructions.Add(Instruction.Create(OpCodes.Call, importer.DecryptFromRvaInt));
        }

        public void ObfuscateLong(MethodDef method, bool needCacheValue, long value, List<Instruction> obfuscatedInstructions)
        {
            if (needCacheValue)
            {
                FieldDef cacheField = _constFieldAllocator.Allocate(method.Module, value);
                obfuscatedInstructions.Add(Instruction.Create(OpCodes.Ldsfld, cacheField));
                return;
            }

            EncryptionScopeInfo encryptionScope = _encryptionScopeProvider.GetScope(method.Module);
            IRandom random = CreateRandomForValue(encryptionScope, value.GetHashCode());
            int ops = GenerateEncryptionOperations(encryptionScope, random);
            int salt = GenerateSalt(random);
            long encryptedValue = encryptionScope.encryptor.Encrypt(value, ops, salt);
            RvaData rvaData = _rvaDataAllocator.Allocate(method.Module, encryptedValue);

            DefaultMetadataImporter importer = GetModuleMetadataImporter(method);
            obfuscatedInstructions.Add(Instruction.Create(OpCodes.Ldsfld, rvaData.field));
            obfuscatedInstructions.Add(Instruction.CreateLdcI4(rvaData.offset));
            obfuscatedInstructions.Add(Instruction.CreateLdcI4(ops));
            obfuscatedInstructions.Add(Instruction.CreateLdcI4(salt));
            obfuscatedInstructions.Add(Instruction.Create(OpCodes.Call, importer.DecryptFromRvaLong));
        }

        public void ObfuscateFloat(MethodDef method, bool needCacheValue, float value, List<Instruction> obfuscatedInstructions)
        {
            if (needCacheValue)
            {
                FieldDef cacheField = _constFieldAllocator.Allocate(method.Module, value);
                obfuscatedInstructions.Add(Instruction.Create(OpCodes.Ldsfld, cacheField));
                return;
            }

            EncryptionScopeInfo encryptionScope = _encryptionScopeProvider.GetScope(method.Module);
            IRandom random = CreateRandomForValue(encryptionScope, value.GetHashCode());
            int ops = GenerateEncryptionOperations(encryptionScope, random);
            int salt = GenerateSalt(random);
            float encryptedValue = encryptionScope.encryptor.Encrypt(value, ops, salt);
            RvaData rvaData = _rvaDataAllocator.Allocate(method.Module, encryptedValue);

            DefaultMetadataImporter importer = GetModuleMetadataImporter(method);
            obfuscatedInstructions.Add(Instruction.Create(OpCodes.Ldsfld, rvaData.field));
            obfuscatedInstructions.Add(Instruction.CreateLdcI4(rvaData.offset));
            obfuscatedInstructions.Add(Instruction.CreateLdcI4(ops));
            obfuscatedInstructions.Add(Instruction.CreateLdcI4(salt));
            obfuscatedInstructions.Add(Instruction.Create(OpCodes.Call, importer.DecryptFromRvaFloat));
        }

        public void ObfuscateDouble(MethodDef method, bool needCacheValue, double value, List<Instruction> obfuscatedInstructions)
        {
            if (needCacheValue)
            {
                FieldDef cacheField = _constFieldAllocator.Allocate(method.Module, value);
                obfuscatedInstructions.Add(Instruction.Create(OpCodes.Ldsfld, cacheField));
                return;
            }

            EncryptionScopeInfo encryptionScope = _encryptionScopeProvider.GetScope(method.Module);
            IRandom random = CreateRandomForValue(encryptionScope, value.GetHashCode());
            int ops = GenerateEncryptionOperations(encryptionScope, random);
            int salt = GenerateSalt(random);
            double encryptedValue = encryptionScope.encryptor.Encrypt(value, ops, salt);
            RvaData rvaData = _rvaDataAllocator.Allocate(method.Module, encryptedValue);

            DefaultMetadataImporter importer = GetModuleMetadataImporter(method);
            obfuscatedInstructions.Add(Instruction.Create(OpCodes.Ldsfld, rvaData.field));
            obfuscatedInstructions.Add(Instruction.CreateLdcI4(rvaData.offset));
            obfuscatedInstructions.Add(Instruction.CreateLdcI4(ops));
            obfuscatedInstructions.Add(Instruction.CreateLdcI4(salt));
            obfuscatedInstructions.Add(Instruction.Create(OpCodes.Call, importer.DecryptFromRvaDouble));
        }

        public void ObfuscateBytes(MethodDef method, bool needCacheValue, byte[] value, List<Instruction> obfuscatedInstructions)
        {
            throw new NotSupportedException("ObfuscateBytes is not supported yet.");
            //if (needCacheValue)
            //{
            //    FieldDef cacheField = _constFieldAllocator.Allocate(method.Module, value);
            //    obfuscatedInstructions.Add(Instruction.Create(OpCodes.Ldsfld, cacheField));
            //    return;
            //}

            //int ops = GenerateEncryptionOperations();
            //int salt = GenerateSalt();
            //byte[] encryptedValue = _encryptor.Encrypt(value, 0, value.Length, ops, salt);
            //Assert.IsTrue(encryptedValue.Length % 4 == 0);
            //RvaData rvaData = _rvaDataAllocator.Allocate(method.Module, encryptedValue);

            //DefaultMetadataImporter importer = GetModuleMetadataImporter(method);
            //obfuscatedInstructions.Add(Instruction.Create(OpCodes.Ldsfld, rvaData.field));
            //obfuscatedInstructions.Add(Instruction.CreateLdcI4(rvaData.offset));
            //// should use value.Length, can't use rvaData.size, because rvaData.size is align to 4, it's not the actual length.
            //obfuscatedInstructions.Add(Instruction.CreateLdcI4(value.Length));
            //obfuscatedInstructions.Add(Instruction.CreateLdcI4(ops));
            //obfuscatedInstructions.Add(Instruction.CreateLdcI4(salt));
            //obfuscatedInstructions.Add(Instruction.Create(OpCodes.Call, importer.DecryptFromRvaBytes));
        }

        public void ObfuscateString(MethodDef method, bool needCacheValue, string value, List<Instruction> obfuscatedInstructions)
        {
            if (needCacheValue)
            {
                FieldDef cacheField = _constFieldAllocator.Allocate(method.Module, value);
                obfuscatedInstructions.Add(Instruction.Create(OpCodes.Ldsfld, cacheField));
                return;
            }

            EncryptionScopeInfo encryptionScope = _encryptionScopeProvider.GetScope(method.Module);
            IRandom random = CreateRandomForValue(encryptionScope, value.GetHashCode());
            int ops = GenerateEncryptionOperations(encryptionScope, random);
            int salt = GenerateSalt(random);
            int stringByteLength = Encoding.UTF8.GetByteCount(value);
            byte[] encryptedValue = encryptionScope.encryptor.Encrypt(value, ops, salt);
            Assert.AreEqual(stringByteLength, encryptedValue.Length);
            RvaData rvaData = _rvaDataAllocator.Allocate(method.Module, encryptedValue);

            DefaultMetadataImporter importer = GetModuleMetadataImporter(method);
            obfuscatedInstructions.Add(Instruction.Create(OpCodes.Ldsfld, rvaData.field));
            obfuscatedInstructions.Add(Instruction.CreateLdcI4(rvaData.offset));
            // should use stringByteLength, can't use rvaData.size, because rvaData.size is align to 4, it's not the actual length.
            obfuscatedInstructions.Add(Instruction.CreateLdcI4(stringByteLength));
            obfuscatedInstructions.Add(Instruction.CreateLdcI4(ops));
            obfuscatedInstructions.Add(Instruction.CreateLdcI4(salt));
            obfuscatedInstructions.Add(Instruction.Create(OpCodes.Call, importer.DecryptFromRvaString));
        }

        public void Done()
        {
        }
    }
}
