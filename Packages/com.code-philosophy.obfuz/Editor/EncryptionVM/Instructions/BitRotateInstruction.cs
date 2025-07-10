using System.Collections.Generic;

namespace Obfuz.EncryptionVM.Instructions
{
    public class BitRotateInstruction : EncryptionInstructionBase
    {
        private readonly int _rotateBitNum;
        private readonly int _opKeyIndex;

        public BitRotateInstruction(int rotateBitNum, int opKeyIndex)
        {
            _rotateBitNum = rotateBitNum;
            _opKeyIndex = opKeyIndex;
        }

        public override int Encrypt(int value, int[] secretKey, int salt)
        {
            uint part1 = (uint)value << _rotateBitNum;
            uint part2 = (uint)value >> (32 - _rotateBitNum);
            return ((int)(part1 | part2) ^ secretKey[_opKeyIndex]) + salt;
        }

        public override int Decrypt(int value, int[] secretKey, int salt)
        {
            uint value2 = (uint)((value - salt) ^ secretKey[_opKeyIndex]);
            uint part1 = value2 >> _rotateBitNum;
            uint part2 = value2 << (32 - _rotateBitNum);
            return (int)(part1 | part2);
        }

        public override void GenerateEncryptCode(List<string> lines, string indent)
        {
            lines.Add(indent + $"uint part1 = (uint)value << {_rotateBitNum};");
            lines.Add(indent + $"uint part2 = (uint)value >> (32 - {_rotateBitNum});");
            lines.Add(indent + $"value = ((int)(part1 | part2) ^ _secretKey[{_opKeyIndex}]) + salt;");
        }

        public override void GenerateDecryptCode(List<string> lines, string indent)
        {
            lines.Add(indent + $"uint value2 = (uint)((value - salt) ^ _secretKey[{_opKeyIndex}]);");
            lines.Add(indent + $"uint part1 = value2 >> {_rotateBitNum};");
            lines.Add(indent + $"uint part2 = value2 << (32 - {_rotateBitNum});");
            lines.Add(indent + $"value = (int)(part1 | part2);");
        }
    }
}
