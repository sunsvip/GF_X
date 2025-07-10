﻿using dnlib.DotNet;
using Obfuz.Data;
using Obfuz.Emit;
using Obfuz.EncryptionVM;
using Obfuz.ObfusPasses;
using Obfuz.ObfusPasses.CleanUp;
using Obfuz.ObfusPasses.SymbolObfus;
using Obfuz.Unity;
using Obfuz.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using IAssemblyResolver = Obfuz.Utils.IAssemblyResolver;

namespace Obfuz
{

    public class Obfuscator
    {
        private readonly CoreSettingsFacade _coreSettings;
        private readonly List<string> _allObfuscationRelativeAssemblyNames;
        private readonly HashSet<string> _assembliesUsingDynamicSecretKeys;
        private readonly CombinedAssemblyResolver _assemblyResolver;

        private readonly ConfigurablePassPolicy _passPolicy;

        private readonly Pipeline _pipeline1 = new Pipeline();
        private readonly Pipeline _pipeline2 = new Pipeline();

        private ObfuscationPassContext _ctx;

        public Obfuscator(ObfuscatorBuilder builder)
        {
            CheckSettings(builder.CoreSettingsFacade);
            _coreSettings = builder.CoreSettingsFacade;

            _allObfuscationRelativeAssemblyNames = _coreSettings.assembliesToObfuscate
                .Concat(_coreSettings.nonObfuscatedButReferencingObfuscatedAssemblies)
                .ToList();
            _assembliesUsingDynamicSecretKeys = new HashSet<string>(_coreSettings.assembliesUsingDynamicSecretKeys);

            _assemblyResolver = new CombinedAssemblyResolver(new PathAssemblyResolver(_coreSettings.assemblySearchPaths.ToArray()), new UnityProjectManagedAssemblyResolver(_coreSettings.buildTarget));
            _passPolicy = new ConfigurablePassPolicy(_coreSettings.assembliesToObfuscate, _coreSettings.enabledObfuscationPasses, _coreSettings.obfuscationPassRuleConfigFiles);

            foreach (var pass in _coreSettings.obfuscationPasses)
            {
                if (pass is SymbolObfusPass symbolObfusPass)
                {
                    _pipeline2.AddPass(pass);
                }
                else
                {
                    _pipeline1.AddPass(pass);
                }
            }
            _pipeline1.AddPass(new CleanUpInstructionPass());
            _pipeline2.AddPass(new RemoveObfuzAttributesPass());
        }

        private void CheckSettings(CoreSettingsFacade settings)
        {
            var totalAssemblies = new HashSet<string>();
            foreach (var assName in settings.assembliesToObfuscate)
            {
                if (string.IsNullOrWhiteSpace(assName))
                {
                    throw new Exception($"the name of some assembly in assembliesToObfuscate is empty! Please check your settings.");
                }
                if (!totalAssemblies.Add(assName))
                {
                    throw new Exception($"the name of assembly `{assName}` in assembliesToObfuscate is duplicated! Please check your settings.");
                }
            }
            foreach (var assName in settings.nonObfuscatedButReferencingObfuscatedAssemblies)
            {
                if (string.IsNullOrWhiteSpace(assName))
                {
                    throw new Exception($"the name of some assembly in nonObfuscatedButReferencingObfuscatedAssemblies is empty! Please check your settings.");
                }
                if (!totalAssemblies.Add(assName))
                {
                    throw new Exception($"the name of assembly `{assName}` in nonObfuscatedButReferencingObfuscatedAssemblies is duplicated! Please check your settings.");
                }
            }
        }

        public void Run()
        {
            Debug.Log($"Obfuscator Run. begin");
            FileUtil.RecreateDir(_coreSettings.obfuscatedAssemblyOutputPath);
            FileUtil.RecreateDir(_coreSettings.obfuscatedAssemblyTempOutputPath);
            RunPipeline(_pipeline1);
            _assemblyResolver.InsertFirst(new PathAssemblyResolver(_coreSettings.obfuscatedAssemblyTempOutputPath));
            RunPipeline(_pipeline2);
            FileUtil.CopyDir(_coreSettings.obfuscatedAssemblyTempOutputPath, _coreSettings.obfuscatedAssemblyOutputPath, true);
            Debug.Log($"Obfuscator Run. end");
        }

        private void RunPipeline(Pipeline pipeline)
        {
            if (pipeline.Empty)
            {
                return;
            }
            OnPreObfuscation(pipeline);
            DoObfuscation(pipeline);
            OnPostObfuscation(pipeline);
        }

        private IEncryptor CreateEncryptionVirtualMachine(byte[] secretKey)
        {
            var vmCreator = new VirtualMachineCreator(_coreSettings.encryptionVmGenerationSecretKey);
            var vm = vmCreator.CreateVirtualMachine(_coreSettings.encryptionVmOpCodeCount);
            var vmGenerator = new VirtualMachineCodeGenerator(vm);

            string encryptionVmCodeFile = _coreSettings.encryptionVmCodeFile;
            if (!File.Exists(encryptionVmCodeFile))
            {
                throw new Exception($"EncryptionVm CodeFile:`{encryptionVmCodeFile}` not exists! Please run `Obfuz/GenerateVm` to generate it!");
            }
            if (!vmGenerator.ValidateMatch(encryptionVmCodeFile))
            {
                throw new Exception($"EncryptionVm CodeFile:`{encryptionVmCodeFile}` not match with encryptionVM settings! Please run `Obfuz/GenerateVm` to update it!");
            }
            var vms = new VirtualMachineSimulator(vm, secretKey);

            var generatedVmTypes = AppDomain.CurrentDomain.GetAssemblies()
                .Select(assembly => assembly.GetType("Obfuz.EncryptionVM.GeneratedEncryptionVirtualMachine"))
                .Where(type => type != null)
                .ToList();
            if (generatedVmTypes.Count == 0)
            {
                throw new Exception($"class Obfuz.EncryptionVM.GeneratedEncryptionVirtualMachine not found in any assembly! Please run `Obfuz/GenerateVm` to generate it!");
            }
            if (generatedVmTypes.Count > 1)
            {
                throw new Exception($"class Obfuz.EncryptionVM.GeneratedEncryptionVirtualMachine found in multiple assemblies! Please retain only one!");
            }

            var gvmInstance = (IEncryptor)Activator.CreateInstance(generatedVmTypes[0], new object[] { secretKey } );

            VerifyVm(vm, vms, gvmInstance);

            return vms;
        }

        private void VerifyVm(VirtualMachine vm, VirtualMachineSimulator vms, IEncryptor gvm)
        {
            int testInt = 11223344;
            long testLong = 1122334455667788L;
            float testFloat = 1234f;
            double testDouble = 1122334455.0;
            string testString = "hello,world";
            for (int i = 0; i < vm.opCodes.Length; i++)
            {
                int ops = i * vm.opCodes.Length + i;
                //int salt = i;
                //int ops = -1135538782;
                int salt = -879409147;
                {
                    int encryptedIntOfVms = vms.Encrypt(testInt, ops, salt);
                    int decryptedIntOfVms = vms.Decrypt(encryptedIntOfVms, ops, salt);
                    if (decryptedIntOfVms != testInt)
                    {
                        throw new Exception($"VirtualMachineSimulator decrypt failed! opCode:{i}, originalValue:{testInt} decryptedValue:{decryptedIntOfVms}");
                    }
                    int encryptedValueOfGvm = gvm.Encrypt(testInt, ops, salt);
                    int decryptedValueOfGvm = gvm.Decrypt(encryptedValueOfGvm, ops, salt);
                    if (encryptedValueOfGvm != encryptedIntOfVms)
                    {
                        throw new Exception($"encryptedValue not match! opCode:{i}, originalValue:{testInt} encryptedValue VirtualMachineSimulator:{encryptedIntOfVms} GeneratedEncryptionVirtualMachine:{encryptedValueOfGvm}");
                    }
                    if (decryptedValueOfGvm != testInt)
                    {
                        throw new Exception($"GeneratedEncryptionVirtualMachine decrypt failed! opCode:{i}, originalValue:{testInt} decryptedValue:{decryptedValueOfGvm}");
                    }
                }
                {
                    long encryptedLongOfVms = vms.Encrypt(testLong, ops, salt);
                    long decryptedLongOfVms = vms.Decrypt(encryptedLongOfVms, ops, salt);
                    if (decryptedLongOfVms != testLong)
                    {
                        throw new Exception($"VirtualMachineSimulator decrypt long failed! opCode:{i}, originalValue:{testLong} decryptedValue:{decryptedLongOfVms}");
                    }
                    long encryptedValueOfGvm = gvm.Encrypt(testLong, ops, salt);
                    long decryptedValueOfGvm = gvm.Decrypt(encryptedValueOfGvm, ops, salt);
                    if (encryptedValueOfGvm != encryptedLongOfVms)
                    {
                        throw new Exception($"encryptedValue not match! opCode:{i}, originalValue:{testLong} encryptedValue VirtualMachineSimulator:{encryptedLongOfVms} GeneratedEncryptionVirtualMachine:{encryptedValueOfGvm}");
                    }
                    if (decryptedValueOfGvm != testLong)
                    {
                        throw new Exception($"GeneratedEncryptionVirtualMachine decrypt long failed! opCode:{i}, originalValue:{testLong} decryptedValue:{decryptedValueOfGvm}");
                    }
                }
                {
                    float encryptedFloatOfVms = vms.Encrypt(testFloat, ops, salt);
                    float decryptedFloatOfVms = vms.Decrypt(encryptedFloatOfVms, ops, salt);
                    if (decryptedFloatOfVms != testFloat)
                    {
                        throw new Exception("encryptedFloat not match");
                    }
                    float encryptedValueOfGvm = gvm.Encrypt(testFloat, ops, salt);
                    float decryptedValueOfGvm = gvm.Decrypt(encryptedFloatOfVms, ops, salt);
                    if (encryptedFloatOfVms != encryptedValueOfGvm)
                    {
                        throw new Exception($"encryptedValue not match! opCode:{i}, originalValue:{testFloat} encryptedValue");
                    }
                    if (decryptedValueOfGvm != testFloat)
                    {
                        throw new Exception($"GeneratedEncryptionVirtualMachine decrypt float failed! opCode:{i}, originalValue:{testFloat}");
                    }
                }
                {
                    double encryptedFloatOfVms = vms.Encrypt(testDouble, ops, salt);
                    double decryptedFloatOfVms = vms.Decrypt(encryptedFloatOfVms, ops, salt);
                    if (decryptedFloatOfVms != testDouble)
                    {
                        throw new Exception("encryptedFloat not match");
                    }
                    double encryptedValueOfGvm = gvm.Encrypt(testDouble, ops, salt);
                    double decryptedValueOfGvm = gvm.Decrypt(encryptedFloatOfVms, ops, salt);
                    if (encryptedFloatOfVms != encryptedValueOfGvm)
                    {
                        throw new Exception($"encryptedValue not match! opCode:{i}, originalValue:{testDouble} encryptedValue");
                    }
                    if (decryptedValueOfGvm != testDouble)
                    {
                        throw new Exception($"GeneratedEncryptionVirtualMachine decrypt float failed! opCode:{i}, originalValue:{testDouble}");
                    }
                }

                {
                    byte[] encryptedStrOfVms = vms.Encrypt(testString, ops, salt);
                    string decryptedStrOfVms = vms.DecryptString(encryptedStrOfVms, 0, encryptedStrOfVms.Length, ops, salt);
                    if (decryptedStrOfVms != testString)
                    {
                        throw new Exception($"VirtualMachineSimulator decrypt string failed! opCode:{i}, originalValue:{testString} decryptedValue:{decryptedStrOfVms}");
                    }
                    byte[] encryptedStrOfGvm = gvm.Encrypt(testString, ops, salt);
                    string decryptedStrOfGvm = gvm.DecryptString(encryptedStrOfGvm, 0, encryptedStrOfGvm.Length, ops, salt);
                    if (!encryptedStrOfGvm.SequenceEqual(encryptedStrOfVms))
                    {
                        throw new Exception($"encryptedValue not match! opCode:{i}, originalValue:{testString} encryptedValue VirtualMachineSimulator:{encryptedStrOfVms} GeneratedEncryptionVirtualMachine:{encryptedStrOfGvm}");
                    }
                    if (decryptedStrOfGvm != testString)
                    {
                        throw new Exception($"GeneratedEncryptionVirtualMachine decrypt string failed! opCode:{i}, originalValue:{testString} decryptedValue:{decryptedStrOfGvm}");
                    }
                }
            }
        }

        private EncryptionScopeInfo CreateEncryptionScope(byte[] byteSecret)
        {
            int[] intSecretKey = KeyGenerator.ConvertToIntKey(byteSecret);
            IEncryptor encryption = CreateEncryptionVirtualMachine(byteSecret);
            RandomCreator localRandomCreator = (seed) => new RandomWithKey(intSecretKey, _coreSettings.randomSeed ^ seed);
            return new EncryptionScopeInfo(encryption, localRandomCreator);
        }

        private EncryptionScopeProvider CreateEncryptionScopeProvider()
        {
            var defaultStaticScope = CreateEncryptionScope(_coreSettings.defaultStaticSecretKey);
            var defaultDynamicScope = CreateEncryptionScope(_coreSettings.defaultDynamicSecretKey);
            foreach (string dynamicAssName in _assembliesUsingDynamicSecretKeys)
            {
                if (!_coreSettings.assembliesToObfuscate.Contains(dynamicAssName))
                {
                    throw new Exception($"Dynamic secret assembly `{dynamicAssName}` should be in the assembliesToObfuscate list!");
                }
            }
            return new EncryptionScopeProvider(defaultStaticScope, defaultDynamicScope, _assembliesUsingDynamicSecretKeys);
        }

        private void OnPreObfuscation(Pipeline pipeline)
        {
            AssemblyCache assemblyCache = new AssemblyCache(_assemblyResolver);
            List<ModuleDef> modulesToObfuscate = new List<ModuleDef>();
            List<ModuleDef> allObfuscationRelativeModules = new List<ModuleDef>();
            LoadAssemblies(assemblyCache, modulesToObfuscate, allObfuscationRelativeModules);

            EncryptionScopeProvider encryptionScopeProvider = CreateEncryptionScopeProvider();
            var moduleEntityManager = new GroupByModuleEntityManager();
            var rvaDataAllocator = new RvaDataAllocator(encryptionScopeProvider, moduleEntityManager);
            var constFieldAllocator = new ConstFieldAllocator(encryptionScopeProvider, rvaDataAllocator, moduleEntityManager);
            _ctx = new ObfuscationPassContext
            {
                coreSettings = _coreSettings,
                assemblyCache = assemblyCache,
                modulesToObfuscate = modulesToObfuscate,
                allObfuscationRelativeModules = allObfuscationRelativeModules,
                moduleEntityManager = moduleEntityManager,

                encryptionScopeProvider = encryptionScopeProvider,

                rvaDataAllocator = rvaDataAllocator,
                constFieldAllocator = constFieldAllocator,
                whiteList = new ObfuscationMethodWhitelist(),
                passPolicy = _passPolicy,
            };
            ObfuscationPassContext.Current = _ctx;
            pipeline.Start();
        }

        private void LoadAssemblies(AssemblyCache assemblyCache, List<ModuleDef> modulesToObfuscate, List<ModuleDef> allObfuscationRelativeModules)
        {
            foreach (string assName in _allObfuscationRelativeAssemblyNames)
            {
                ModuleDefMD mod = assemblyCache.TryLoadModule(assName);
                if (mod == null)
                {
                    Debug.Log($"assembly: {assName} not found! ignore.");
                    continue;
                }
                if (_coreSettings.assembliesToObfuscate.Contains(assName))
                {
                    modulesToObfuscate.Add(mod);
                }
                allObfuscationRelativeModules.Add(mod);
            }
        }

        private void WriteAssemblies()
        {
            foreach (ModuleDef mod in _ctx.allObfuscationRelativeModules)
            {
                string assNameWithExt = mod.Name;
                string outputFile = $"{_coreSettings.obfuscatedAssemblyTempOutputPath}/{assNameWithExt}";
                mod.Write(outputFile);
                Debug.Log($"save module. name:{mod.Assembly.Name} output:{outputFile}");
            }
        }

        private void DoObfuscation(Pipeline pipeline)
        {
            pipeline.Run();
        }

        private void OnPostObfuscation(Pipeline pipeline)
        {
            pipeline.Stop();

            _ctx.constFieldAllocator.Done();
            _ctx.rvaDataAllocator.Done();
            WriteAssemblies();
        }
    }
}
