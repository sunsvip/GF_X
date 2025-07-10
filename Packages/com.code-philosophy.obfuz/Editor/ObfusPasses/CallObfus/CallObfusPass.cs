using dnlib.DotNet;
using dnlib.DotNet.Emit;
using Obfuz.Utils;
using Obfuz.Emit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.Assertions;
using Obfuz.Settings;

namespace Obfuz.ObfusPasses.CallObfus
{
    public class CallObfusPass : BasicBlockObfuscationPassBase
    {
        private readonly CallObfuscationSettingsFacade _settings;
        private IObfuscator _dynamicProxyObfuscator;
        private IObfuscationPolicy _dynamicProxyPolicy;

        public override ObfuscationPassType Type => ObfuscationPassType.CallObfus;

        public CallObfusPass(CallObfuscationSettingsFacade settings)
        {
            _settings = settings;
        }

        public override void Stop()
        {
            _dynamicProxyObfuscator.Done();
        }

        public override void Start()
        {
            var ctx = ObfuscationPassContext.Current;
            _dynamicProxyObfuscator = new DefaultCallProxyObfuscator(ctx.encryptionScopeProvider, ctx.constFieldAllocator, ctx.moduleEntityManager, _settings);
            _dynamicProxyPolicy = new ConfigurableObfuscationPolicy(ctx.coreSettings.assembliesToObfuscate, _settings.ruleFiles);
        }

        protected override bool NeedObfuscateMethod(MethodDef method)
        {
            return _dynamicProxyPolicy.NeedObfuscateCallInMethod(method);
        }

        protected override bool TryObfuscateInstruction(MethodDef callerMethod, Instruction inst, BasicBlock block,
            int instructionIndex, IList<Instruction> globalInstructions, List<Instruction> outputInstructions, List<Instruction> totalFinalInstructions)
        {
            IMethod calledMethod = inst.Operand as IMethod;
            if (calledMethod == null || !calledMethod.IsMethod)
            {
                return false;
            }
            if (MetaUtil.ContainsContainsGenericParameter(calledMethod))
            {
                return false;
            }

            bool callVir;
            switch (inst.OpCode.Code)
            {
                case Code.Call:
                {
                    callVir = false;
                    break;
                }
                case Code.Callvirt:
                {
                    if (instructionIndex > 0 && globalInstructions[instructionIndex - 1].OpCode.Code == Code.Constrained)
                    {
                        return false;
                    }
                    callVir = true;
                    break;
                }
                default: return false;
            }

            if (!_dynamicProxyPolicy.NeedObfuscateCalledMethod(callerMethod, calledMethod, callVir, block.inLoop))
            {
                return false;
            }

            ObfuscationCachePolicy cachePolicy = _dynamicProxyPolicy.GetMethodObfuscationCachePolicy(callerMethod);
            bool cachedCallIndex = block.inLoop ? cachePolicy.cacheInLoop : cachePolicy.cacheNotInLoop;
            _dynamicProxyObfuscator.Obfuscate(callerMethod, calledMethod, callVir, cachedCallIndex, outputInstructions);
            return true;
        }
    }
}
