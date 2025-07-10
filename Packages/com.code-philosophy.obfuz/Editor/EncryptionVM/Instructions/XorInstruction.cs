using System.Collections.Generic;

namespace Obfuz.EncryptionVM.Instructions
{
    public class  XorInstruction : EncryptionInstructionBase
    {
        private readonly int _xorValue;
        private readonly int _opKeyIndex;

        public XorInstruction(int xorValue, int opKeyIndex)
        {
            _xorValue = xorValue;
            _opKeyIndex = opKeyIndex;
        }

        public override int Encrypt(int value, int[] secretKey, int salt)
        {
            return ((value ^ secretKey[_opKeyIndex]) + salt) ^ _xorValue;
        }

        public override int Decrypt(int value, int[] secretKey, int salt)
        {
            return ((value ^ _xorValue) - salt) ^ secretKey[_opKeyIndex];
        }

        public override void GenerateEncryptCode(List<string> lines, string indent)
        {
            lines.Add(indent + $"value = ((value ^ _secretKey[{_opKeyIndex}]) + salt) ^ {_xorValue};");
        }

        public override void GenerateDecryptCode(List<string> lines, string indent)
        {
            lines.Add(indent + $"value = ((value ^ {_xorValue}) - salt) ^ _secretKey[{_opKeyIndex}];");
        }
    }
}
