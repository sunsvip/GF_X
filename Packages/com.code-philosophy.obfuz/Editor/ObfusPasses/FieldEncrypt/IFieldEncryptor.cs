using dnlib.DotNet;
using dnlib.DotNet.Emit;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Obfuz.ObfusPasses.FieldEncrypt
{
    public class MemoryEncryptionContext
    {
        public ModuleDef module;

        public Instruction currentInstruction;
    }

    public interface IFieldEncryptor
    {
        void Encrypt(MethodDef method, FieldDef field, List<Instruction> outputInstructions, Instruction currentInstruction);

        void Decrypt(MethodDef method, FieldDef field, List<Instruction> outputInstructions, Instruction currentInstruction);
    }

    public abstract class FieldEncryptorBase : IFieldEncryptor
    {
        public abstract void Encrypt(MethodDef method, FieldDef field, List<Instruction> outputInstructions, Instruction currentInstruction);
        public abstract void Decrypt(MethodDef method, FieldDef field, List<Instruction> outputInstructions, Instruction currentInstruction);
    }
}
