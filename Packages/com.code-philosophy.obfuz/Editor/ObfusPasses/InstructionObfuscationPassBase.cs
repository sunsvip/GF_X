using dnlib.DotNet.Emit;
using dnlib.DotNet;
using System.Collections.Generic;
using System.Linq;

namespace Obfuz.ObfusPasses
{
    public abstract class InstructionObfuscationPassBase : ObfuscationPassBase
    {
        protected virtual bool ForceProcessAllAssembliesAndIgnoreAllPolicy => false;

        protected abstract bool NeedObfuscateMethod(MethodDef method);

        public override void Process()
        {
            var ctx = ObfuscationPassContext.Current;
            var modules = ForceProcessAllAssembliesAndIgnoreAllPolicy ? ctx.allObfuscationRelativeModules : ctx.modulesToObfuscate;
            ObfuscationMethodWhitelist whiteList = ctx.whiteList;
            ConfigurablePassPolicy passPolicy = ctx.passPolicy;
            foreach (ModuleDef mod in modules)
            {
                if (!ForceProcessAllAssembliesAndIgnoreAllPolicy && (whiteList.IsInWhiteList(mod) || !Support(passPolicy.GetAssemblyObfuscationPasses(mod))))
                {
                    continue;
                }
                // ToArray to avoid modify list exception
                foreach (TypeDef type in mod.GetTypes().ToArray())
                {
                    if (!ForceProcessAllAssembliesAndIgnoreAllPolicy && (whiteList.IsInWhiteList(type) || !Support(passPolicy.GetTypeObfuscationPasses(type))))
                    {
                        continue;
                    }
                    // ToArray to avoid modify list exception
                    foreach (MethodDef method in type.Methods.ToArray())
                    {
                        if (!method.HasBody || (!ForceProcessAllAssembliesAndIgnoreAllPolicy && (ctx.whiteList.IsInWhiteList(method) || !Support(passPolicy.GetMethodObfuscationPasses(method)) || !NeedObfuscateMethod(method))))
                        {
                            continue;
                        }
                        // TODO if isGeneratedBy Obfuscator, continue
                        ObfuscateData(method);
                    }
                }
            }
        }


        protected abstract bool TryObfuscateInstruction(MethodDef callingMethod, Instruction inst, IList<Instruction> instructions, int instructionIndex,
            List<Instruction> outputInstructions, List<Instruction> totalFinalInstructions);

        private void ObfuscateData(MethodDef method)
        {
            IList<Instruction> instructions = method.Body.Instructions;
            var outputInstructions = new List<Instruction>();
            var totalFinalInstructions = new List<Instruction>();
            for (int i = 0; i < instructions.Count; i++)
            {
                Instruction inst = instructions[i];
                outputInstructions.Clear();
                if (TryObfuscateInstruction(method, inst, instructions, i, outputInstructions, totalFinalInstructions))
                {
                    // current instruction may be the target of control flow instruction, so we can't remove it directly.
                    // we replace it with nop now, then remove it in CleanUpInstructionPass
                    inst.OpCode = outputInstructions[0].OpCode;
                    inst.Operand = outputInstructions[0].Operand;
                    totalFinalInstructions.Add(inst);
                    for (int k = 1; k < outputInstructions.Count; k++)
                    {
                        totalFinalInstructions.Add(outputInstructions[k]);
                    }
                }
                else
                {
                    totalFinalInstructions.Add(inst);
                }
            }

            instructions.Clear();
            foreach (var obInst in totalFinalInstructions)
            {
                instructions.Add(obInst);
            }
        }
    }
}
