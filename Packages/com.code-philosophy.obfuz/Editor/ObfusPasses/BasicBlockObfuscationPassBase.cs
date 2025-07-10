using dnlib.DotNet.Emit;
using dnlib.DotNet;
using System.Collections.Generic;
using System.Linq;
using Obfuz.Emit;

namespace Obfuz.ObfusPasses
{
    public abstract class BasicBlockObfuscationPassBase : ObfuscationPassBase
    {
        protected abstract bool NeedObfuscateMethod(MethodDef method);

        public override void Process()
        {
            var ctx = ObfuscationPassContext.Current;
            ObfuscationMethodWhitelist whiteList = ctx.whiteList;
            ConfigurablePassPolicy passPolicy = ctx.passPolicy;
            foreach (ModuleDef mod in ctx.modulesToObfuscate)
            {
                if (whiteList.IsInWhiteList(mod) || !Support(passPolicy.GetAssemblyObfuscationPasses(mod)))
                {
                    continue;
                }
                // ToArray to avoid modify list exception
                foreach (TypeDef type in mod.GetTypes().ToArray())
                {
                    if (whiteList.IsInWhiteList(type) || !Support(passPolicy.GetTypeObfuscationPasses(type)))
                    {
                        continue;
                    }
                    // ToArray to avoid modify list exception
                    foreach (MethodDef method in type.Methods.ToArray())
                    {
                        if (!method.HasBody || ctx.whiteList.IsInWhiteList(method) || !Support(passPolicy.GetMethodObfuscationPasses(method)) || !NeedObfuscateMethod(method))
                        {
                            continue;
                        }
                        // TODO if isGeneratedBy Obfuscator, continue
                        ObfuscateData(method);
                    }
                }
            }
        }


        protected abstract bool TryObfuscateInstruction(MethodDef callingMethod, Instruction inst, BasicBlock block, int instructionIndex,
            IList<Instruction> globalInstructions, List<Instruction> outputInstructions, List<Instruction> totalFinalInstructions);

        private void ObfuscateData(MethodDef method)
        {
            BasicBlockCollection bbc = new BasicBlockCollection(method);

            IList<Instruction> instructions = method.Body.Instructions;

            var outputInstructions = new List<Instruction>();
            var totalFinalInstructions = new List<Instruction>();
            for (int i = 0; i < instructions.Count; i++)
            {
                Instruction inst = instructions[i];
                BasicBlock block = bbc.GetBasicBlockByInstruction(inst);
                outputInstructions.Clear();
                if (TryObfuscateInstruction(method, inst, block, i, instructions, outputInstructions, totalFinalInstructions))
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
