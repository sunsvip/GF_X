using dnlib.DotNet;
using dnlib.DotNet.Emit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Obfuz.ObfusPasses.CleanUp
{
    public class RemoveObfuzAttributesPass : ObfuscationPassBase
    {
        public override ObfuscationPassType Type => ObfuscationPassType.None;

        public override void Start()
        {
        }

        public override void Stop()
        {

        }


        private void RemoveObfuzAttributes(IHasCustomAttribute provider)
        {
            CustomAttributeCollection customAttributes = provider.CustomAttributes;
            if (customAttributes.Count == 0)
                return;
            var toRemove = new List<CustomAttribute>();
            customAttributes.RemoveAll("Obfuz.ObfuzIgnoreAttribute");
            customAttributes.RemoveAll("Obfuz.EncryptFieldAttribute");
        }

        public override void Process()
        {
            var ctx = ObfuscationPassContext.Current;
            foreach (ModuleDef mod in ctx.modulesToObfuscate)
            {
                RemoveObfuzAttributes(mod);
                foreach (TypeDef type in mod.GetTypes())
                {
                    RemoveObfuzAttributes(type);
                    foreach (FieldDef field in type.Fields)
                    {
                        RemoveObfuzAttributes(field);
                    }
                    foreach (MethodDef method in type.Methods)
                    {
                        RemoveObfuzAttributes(method);
                        foreach (Parameter param in method.Parameters)
                        {
                            if (param.ParamDef != null)
                            {
                                RemoveObfuzAttributes(param.ParamDef);
                            }
                        }
                    }
                    foreach (PropertyDef property in type.Properties)
                    {
                        RemoveObfuzAttributes(property);
                    }
                    foreach (EventDef eventDef in type.Events)
                    {
                        RemoveObfuzAttributes(eventDef);
                    }
                }
            }
        }
    }
}
