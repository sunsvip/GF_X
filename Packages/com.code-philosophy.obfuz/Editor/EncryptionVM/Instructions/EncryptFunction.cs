using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Obfuz.EncryptionVM.Instructions
{

    public class EncryptFunction : EncryptionInstructionBase
    {
        private readonly IEncryptionInstruction[] _instructions;

        public EncryptFunction(IEncryptionInstruction[] instructions)
        {
            _instructions = instructions;
        }

        public override int Encrypt(int value, int[] secretKey, int salt)
        {
            foreach (var instruction in _instructions)
            {
                value = instruction.Encrypt(value, secretKey, salt);
            }
            return value;
        }

        public override int Decrypt(int value, int[] secretKey, int salt)
        {
            for (int i = _instructions.Length - 1; i >= 0; i--)
            {
                value = _instructions[i].Decrypt(value, secretKey, salt);
            }
            return value;
        }

        public override void GenerateEncryptCode(List<string> lines, string indent)
        {
            throw new NotImplementedException();
        }

        public override void GenerateDecryptCode(List<string> lines, string indent)
        {
            throw new NotImplementedException();
        }
    }
}
