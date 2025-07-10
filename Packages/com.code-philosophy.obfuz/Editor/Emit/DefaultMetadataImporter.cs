using dnlib.DotNet;
using System;
using UnityEngine.Assertions;

namespace Obfuz.Emit
{
    public class EncryptionServiceMetadataImporter
    {
        private readonly ModuleDef _module;
        private readonly Type _encryptionServiceType;

        private IMethod _encryptBlock;
        private IMethod _decryptBlock;
        private IMethod _encryptInt;
        private IMethod _decryptInt;
        private IMethod _encryptLong;
        private IMethod _decryptLong;
        private IMethod _encryptFloat;
        private IMethod _decryptFloat;
        private IMethod _encryptDouble;
        private IMethod _decryptDouble;
        private IMethod _encryptString;
        private IMethod _decryptString;
        private IMethod _encryptBytes;
        private IMethod _decryptBytes;

        private IMethod _decryptFromRvaInt;
        private IMethod _decryptFromRvaLong;
        private IMethod _decryptFromRvaFloat;
        private IMethod _decryptFromRvaDouble;
        private IMethod _decryptFromRvaString;
        private IMethod _decryptFromRvaBytes;

        public IMethod EncryptBlock => _encryptBlock;
        public IMethod DecryptBlock => _decryptBlock;

        public IMethod EncryptInt => _encryptInt;
        public IMethod DecryptInt => _decryptInt;
        public IMethod EncryptLong => _encryptLong;
        public IMethod DecryptLong => _decryptLong;
        public IMethod EncryptFloat => _encryptFloat;
        public IMethod DecryptFloat => _decryptFloat;
        public IMethod EncryptDouble => _encryptDouble;
        public IMethod DecryptDouble => _decryptDouble;
        public IMethod EncryptString => _encryptString;
        public IMethod DecryptString => _decryptString;
        public IMethod EncryptBytes => _encryptBytes;
        public IMethod DecryptBytes => _decryptBytes;

        public IMethod DecryptFromRvaInt => _decryptFromRvaInt;
        public IMethod DecryptFromRvaLong => _decryptFromRvaLong;
        public IMethod DecryptFromRvaFloat => _decryptFromRvaFloat;
        public IMethod DecryptFromRvaDouble => _decryptFromRvaDouble;
        public IMethod DecryptFromRvaBytes => _decryptFromRvaBytes;
        public IMethod DecryptFromRvaString => _decryptFromRvaString;

        public EncryptionServiceMetadataImporter(ModuleDef mod, Type encryptionServiceType)
        {
            _module = mod;
            _encryptionServiceType = encryptionServiceType;
            _encryptBlock = mod.Import(encryptionServiceType.GetMethod("EncryptBlock", new[] { typeof(byte[]), typeof(int), typeof(int) }));
            Assert.IsNotNull(_encryptBlock);
            _decryptBlock = mod.Import(encryptionServiceType.GetMethod("DecryptBlock", new[] { typeof(byte[]), typeof(int), typeof(int) }));
            Assert.IsNotNull(_decryptBlock);
            _encryptInt = mod.Import(encryptionServiceType.GetMethod("Encrypt", new[] { typeof(int), typeof(int), typeof(int) }));
            Assert.IsNotNull(_encryptInt);
            _decryptInt = mod.Import(encryptionServiceType.GetMethod("Decrypt", new[] { typeof(int), typeof(int), typeof(int) }));
            Assert.IsNotNull(_decryptInt);
            _encryptLong = mod.Import(encryptionServiceType.GetMethod("Encrypt", new[] { typeof(long), typeof(int), typeof(int) }));
            Assert.IsNotNull(_encryptLong);
            _decryptLong = mod.Import(encryptionServiceType.GetMethod("Decrypt", new[] { typeof(long), typeof(int), typeof(int) }));
            Assert.IsNotNull(_decryptLong);
            _encryptFloat = mod.Import(encryptionServiceType.GetMethod("Encrypt", new[] { typeof(float), typeof(int), typeof(int) }));
            Assert.IsNotNull(_encryptFloat);
            _decryptFloat = mod.Import(encryptionServiceType.GetMethod("Decrypt", new[] { typeof(float), typeof(int), typeof(int) }));
            Assert.IsNotNull(_decryptFloat);
            _encryptDouble = mod.Import(encryptionServiceType.GetMethod("Encrypt", new[] { typeof(double), typeof(int), typeof(int) }));
            Assert.IsNotNull(_encryptDouble);
            _decryptDouble = mod.Import(encryptionServiceType.GetMethod("Decrypt", new[] { typeof(double), typeof(int), typeof(int) }));
            Assert.IsNotNull(_decryptDouble);
            _encryptString = mod.Import(encryptionServiceType.GetMethod("Encrypt", new[] { typeof(string), typeof(int), typeof(int) }));
            Assert.IsNotNull(_encryptString);
            _decryptString = mod.Import(encryptionServiceType.GetMethod("DecryptString", new[] { typeof(byte[]), typeof(int), typeof(int), typeof(int), typeof(int) }));
            Assert.IsNotNull(_decryptString);
            _encryptBytes = mod.Import(encryptionServiceType.GetMethod("Encrypt", new[] { typeof(byte[]), typeof(int), typeof(int), typeof(int), typeof(int) }));
            Assert.IsNotNull(_encryptBytes);
            _decryptBytes = mod.Import(encryptionServiceType.GetMethod("Decrypt", new[] { typeof(byte[]), typeof(int), typeof(int), typeof(int), typeof(int) }));
            Assert.IsNotNull(_decryptBytes);

            _decryptFromRvaInt = mod.Import(encryptionServiceType.GetMethod("DecryptFromRvaInt", new[] { typeof(byte[]), typeof(int), typeof(int), typeof(int) }));
            Assert.IsNotNull(_decryptFromRvaInt);
            _decryptFromRvaLong = mod.Import(encryptionServiceType.GetMethod("DecryptFromRvaLong", new[] { typeof(byte[]), typeof(int), typeof(int), typeof(int) }));
            Assert.IsNotNull(_decryptFromRvaLong);
            _decryptFromRvaFloat = mod.Import(encryptionServiceType.GetMethod("DecryptFromRvaFloat", new[] { typeof(byte[]), typeof(int), typeof(int), typeof(int) }));
            Assert.IsNotNull(_decryptFromRvaFloat);
            _decryptFromRvaDouble = mod.Import(encryptionServiceType.GetMethod("DecryptFromRvaDouble", new[] { typeof(byte[]), typeof(int), typeof(int), typeof(int) }));
            Assert.IsNotNull(_decryptFromRvaDouble);
            _decryptFromRvaBytes = mod.Import(encryptionServiceType.GetMethod("DecryptFromRvaBytes", new[] { typeof(byte[]), typeof(int), typeof(int), typeof(int), typeof(int) }));
            Assert.IsNotNull(_decryptFromRvaBytes);
            _decryptFromRvaString = mod.Import(encryptionServiceType.GetMethod("DecryptFromRvaString", new[] { typeof(byte[]), typeof(int), typeof(int), typeof(int), typeof(int) }));
            Assert.IsNotNull(_decryptFromRvaString);
        }
    }

    public class DefaultMetadataImporter : GroupByModuleEntityBase
    {
        private readonly EncryptionScopeProvider _encryptionScopeProvider;
        private EncryptionScopeInfo _encryptionScope;
        private EncryptionServiceMetadataImporter _defaultEncryptionServiceMetadataImporter;


        private EncryptionServiceMetadataImporter _staticDefaultEncryptionServiceMetadataImporter;
        private EncryptionServiceMetadataImporter _dynamicDefaultEncryptionServiceMetadataImporter;

        public DefaultMetadataImporter(EncryptionScopeProvider encryptionScopeProvider)
        {
            _encryptionScopeProvider = encryptionScopeProvider;
        }

        public override void Init(ModuleDef mod)
        {
            _module = mod;
            _encryptionScope = _encryptionScopeProvider.GetScope(mod);
            var constUtilityType = typeof(ConstUtility);

            _castIntAsFloat = mod.Import(constUtilityType.GetMethod("CastIntAsFloat"));
            Assert.IsNotNull(_castIntAsFloat, "CastIntAsFloat not found");
            _castLongAsDouble = mod.Import(constUtilityType.GetMethod("CastLongAsDouble"));
            Assert.IsNotNull(_castLongAsDouble, "CastLongAsDouble not found");
            _castFloatAsInt = mod.Import(constUtilityType.GetMethod("CastFloatAsInt"));
            Assert.IsNotNull(_castFloatAsInt, "CastFloatAsInt not found");
            _castDoubleAsLong = mod.Import(constUtilityType.GetMethod("CastDoubleAsLong"));
            Assert.IsNotNull(_castDoubleAsLong, "CastDoubleAsLong not found");

            _initializeArray = mod.Import(typeof(System.Runtime.CompilerServices.RuntimeHelpers).GetMethod("InitializeArray", new[] { typeof(Array), typeof(RuntimeFieldHandle) }));
            Assert.IsNotNull(_initializeArray);
            _verifySecretKey = mod.Import(typeof(AssetUtility).GetMethod("VerifySecretKey", new[] { typeof(int), typeof(int) }));
            Assert.IsNotNull(_verifySecretKey, "VerifySecretKey not found");

            _staticDefaultEncryptionServiceMetadataImporter = new EncryptionServiceMetadataImporter(mod, typeof(EncryptionService<DefaultStaticEncryptionScope>));
            _dynamicDefaultEncryptionServiceMetadataImporter = new EncryptionServiceMetadataImporter(mod, typeof(EncryptionService<DefaultDynamicEncryptionScope>));
            if (_encryptionScopeProvider.IsDynamicSecretAssembly(mod))
            {
                _defaultEncryptionServiceMetadataImporter = _dynamicDefaultEncryptionServiceMetadataImporter;
            }
            else
            {
                _defaultEncryptionServiceMetadataImporter = _staticDefaultEncryptionServiceMetadataImporter;
            }
        }

        public EncryptionServiceMetadataImporter GetEncryptionServiceMetadataImporterOfModule(ModuleDef mod)
        {
            return _encryptionScopeProvider.IsDynamicSecretAssembly(mod) ? _dynamicDefaultEncryptionServiceMetadataImporter : _staticDefaultEncryptionServiceMetadataImporter;
        }

        private ModuleDef _module;
        private IMethod _castIntAsFloat;
        private IMethod _castLongAsDouble;
        private IMethod _castFloatAsInt;
        private IMethod _castDoubleAsLong;
        private IMethod _initializeArray;
        private IMethod _verifySecretKey;

        public IMethod CastIntAsFloat => _castIntAsFloat;
        public IMethod CastLongAsDouble => _castLongAsDouble;
        public IMethod CastFloatAsInt => _castFloatAsInt;
        public IMethod CastDoubleAsLong => _castDoubleAsLong;

        public IMethod InitializedArray => _initializeArray;

        public IMethod VerifySecretKey => _verifySecretKey;

        public IMethod EncryptBlock => _defaultEncryptionServiceMetadataImporter.EncryptBlock;
        public IMethod DecryptBlock => _defaultEncryptionServiceMetadataImporter.DecryptBlock;

        public IMethod EncryptInt => _defaultEncryptionServiceMetadataImporter.EncryptInt;
        public IMethod DecryptInt => _defaultEncryptionServiceMetadataImporter.DecryptInt;
        public IMethod EncryptLong => _defaultEncryptionServiceMetadataImporter.EncryptLong;
        public IMethod DecryptLong => _defaultEncryptionServiceMetadataImporter.DecryptLong;
        public IMethod EncryptFloat => _defaultEncryptionServiceMetadataImporter.EncryptFloat;
        public IMethod DecryptFloat => _defaultEncryptionServiceMetadataImporter.DecryptFloat;
        public IMethod EncryptDouble => _defaultEncryptionServiceMetadataImporter.EncryptDouble;
        public IMethod DecryptDouble => _defaultEncryptionServiceMetadataImporter.DecryptDouble;
        public IMethod EncryptString => _defaultEncryptionServiceMetadataImporter.EncryptString;
        public IMethod DecryptString => _defaultEncryptionServiceMetadataImporter.DecryptString;
        public IMethod EncryptBytes => _defaultEncryptionServiceMetadataImporter.EncryptBytes;
        public IMethod DecryptBytes => _defaultEncryptionServiceMetadataImporter.DecryptBytes;

        public IMethod DecryptFromRvaInt => _defaultEncryptionServiceMetadataImporter.DecryptFromRvaInt;
        public IMethod DecryptFromRvaLong => _defaultEncryptionServiceMetadataImporter.DecryptFromRvaLong;
        public IMethod DecryptFromRvaFloat => _defaultEncryptionServiceMetadataImporter.DecryptFromRvaFloat;
        public IMethod DecryptFromRvaDouble => _defaultEncryptionServiceMetadataImporter.DecryptFromRvaDouble;
        public IMethod DecryptFromRvaBytes => _defaultEncryptionServiceMetadataImporter.DecryptFromRvaBytes;
        public IMethod DecryptFromRvaString => _defaultEncryptionServiceMetadataImporter.DecryptFromRvaString;

    }
}
