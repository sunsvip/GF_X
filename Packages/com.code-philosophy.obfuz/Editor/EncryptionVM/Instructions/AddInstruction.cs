using System.Collections.Generic;

namespace Obfuz.EncryptionVM.Instructions
{
    public class AddInstruction : EncryptionInstructionBase
    {
        private readonly int _addValue;
        private readonly int _opKeyIndex;

        public AddInstruction(int addValue, int opKeyIndex)
        {
            _addValue = addValue;
            _opKeyIndex = opKeyIndex;
        }
        public override int Encrypt(int value, int[] secretKey, int salt)
        {
            return ((value + secretKey[_opKeyIndex]) ^ salt) + _addValue;
        }

        public override int Decrypt(int value, int[] secretKey, int salt)
        {
            return ((value - _addValue) ^ salt) - secretKey[_opKeyIndex];
        }

        public override void GenerateEncryptCode(List<string> lines, string indent)
        {
            lines.Add(indent + $"value = ((value + _secretKey[{_opKeyIndex}]) ^ salt) + {_addValue};");
        }

        public override void GenerateDecryptCode(List<string> lines, string indent)
        {
            lines.Add(indent + $"value = ((value  - {_addValue}) ^ salt) - _secretKey[{_opKeyIndex}];");
        }
    }
}
