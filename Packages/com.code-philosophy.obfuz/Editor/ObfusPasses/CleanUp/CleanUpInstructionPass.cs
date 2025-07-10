using dnlib.DotNet;
using dnlib.DotNet.Emit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Obfuz.ObfusPasses.CleanUp
{
    public class CleanUpInstructionPass : ObfuscationPassBase
    {
        public override ObfuscationPassType Type => ObfuscationPassType.None;

        public override void Start()
        {
        }

        public override void Stop()
        {

        }

        public override void Process()
        {
            var ctx = ObfuscationPassContext.Current;
            foreach (ModuleDef mod in ctx.modulesToObfuscate)
            {
                foreach (TypeDef type in mod.GetTypes())
                {
                    foreach (MethodDef method in type.Methods)
                    {
                        if (method.HasBody)
                        {
                            CilBody body = method.Body;
                            body.SimplifyBranches();
                            body.OptimizeMacros();
                            body.OptimizeBranches();
                            // TODO remove dup
                        }
                    }
                }
            }
        }
    }
}
