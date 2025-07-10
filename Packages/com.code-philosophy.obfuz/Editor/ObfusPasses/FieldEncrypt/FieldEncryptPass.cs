using dnlib.DotNet;
using dnlib.DotNet.Emit;
using Obfuz;
using Obfuz.Settings;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Obfuz.ObfusPasses.FieldEncrypt
{

    public class FieldEncryptPass : InstructionObfuscationPassBase
    {
        private FieldEncryptionSettingsFacade _settings;
        private IEncryptPolicy _encryptionPolicy;
        private IFieldEncryptor _memoryEncryptor;

        public override ObfuscationPassType Type => ObfuscationPassType.FieldEncrypt;

        public FieldEncryptPass(FieldEncryptionSettingsFacade settings)
        {
            _settings = settings;
        }

        protected override bool ForceProcessAllAssembliesAndIgnoreAllPolicy => true;

        public override void Start()
        {
            var ctx = ObfuscationPassContext.Current;
            _memoryEncryptor = new DefaultFieldEncryptor(ctx.encryptionScopeProvider, ctx.moduleEntityManager, _settings);
            _encryptionPolicy = new ConfigurableEncryptPolicy(ctx.coreSettings.assembliesToObfuscate, _settings.ruleFiles);
        }

        public override void Stop()
        {

        }

        protected override bool NeedObfuscateMethod(MethodDef method)
        {
            return true;
        }

        private bool IsSupportedFieldType(TypeSig type)
        {
            type = type.RemovePinnedAndModifiers();
            switch (type.ElementType)
            {
                case ElementType.I4:
                case ElementType.I8:
                case ElementType.U4:
                case ElementType.U8:
                case ElementType.R4:
                case ElementType.R8:
                return true;
                default: return false;
            }
        }

        protected override bool TryObfuscateInstruction(MethodDef callingMethod, Instruction inst, IList<Instruction> instructions, int instructionIndex, List<Instruction> outputInstructions, List<Instruction> totalFinalInstructions)
        {
            Code code = inst.OpCode.Code;
            if (!(inst.Operand is IField field) || !field.IsField)
            {
                return false;
            }
            FieldDef fieldDef = field.ResolveFieldDefThrow();
            if (!IsSupportedFieldType(fieldDef.FieldSig.Type) || !_encryptionPolicy.NeedEncrypt(fieldDef))
            {
                return false;
            }
            switch (code)
            {
                case Code.Ldfld:
                {
                    _memoryEncryptor.Decrypt(callingMethod, fieldDef, outputInstructions, inst);
                    break;
                }
                case Code.Stfld:
                {
                    _memoryEncryptor.Encrypt(callingMethod, fieldDef, outputInstructions, inst);
                    break;
                }
                case Code.Ldsfld:
                {
                    _memoryEncryptor.Decrypt(callingMethod, fieldDef, outputInstructions, inst);
                    break;
                }
                case Code.Stsfld:
                {
                    _memoryEncryptor.Encrypt(callingMethod, fieldDef, outputInstructions, inst);
                    break;
                }
                case Code.Ldflda:
                case Code.Ldsflda:
                {
                    throw new System.Exception($"You shouldn't get reference to memory encryption field: {field}");
                }
                default: return false;
            }
            //Debug.Log($"memory encrypt field: {field}");
            return true;
        }
    }
}
