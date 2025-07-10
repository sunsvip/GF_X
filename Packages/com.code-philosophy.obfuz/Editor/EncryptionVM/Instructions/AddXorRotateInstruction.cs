using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Obfuz.EncryptionVM.Instructions
{
    public class AddXorRotateInstruction : EncryptionInstructionBase
    {
        // x = x + p1 + secretKey[index1];
        // x = x ^ p3 ^ salt;
        // x = Rotate(x, p2)

        private readonly int _addValue;
        private readonly int _index1;
        private readonly int _rotateBitNum;
        private readonly int _xorValue;

        public AddXorRotateInstruction(int addValue, int index1, int xorValue, int rotateBitNum)
        {
            _addValue = addValue;
            _index1 = index1;
            _rotateBitNum = rotateBitNum;
            _xorValue = xorValue;
        }

        public override int Encrypt(int value, int[] secretKey, int salt)
        {
            value += _addValue + secretKey[_index1];
            value ^= _xorValue ^ salt;
            uint part1 = (uint)value << _rotateBitNum;
            uint part2 = (uint)value >> (32 - _rotateBitNum);
            value = (int)(part1 | part2);
            return value;
        }

        public override int Decrypt(int value, int[] secretKey, int salt)
        {
            uint value2 = (uint)value >> _rotateBitNum;
            uint part1 = (uint)value << (32 - _rotateBitNum);
            value = (int)(value2 | part1);
            value ^= _xorValue ^ salt;
            value -= _addValue + secretKey[_index1];
            return value;
        }

        public override void GenerateEncryptCode(List<string> lines, string indent)
        {
            lines.Add(indent + $"value += {_addValue} + _secretKey[{_index1}];");
            lines.Add(indent + $"value ^= {_xorValue} ^ salt;");
            lines.Add(indent + $"uint part1 = (uint)value << {_rotateBitNum};");
            lines.Add(indent + $"uint part2 = (uint)value >> (32 - {_rotateBitNum});");
            lines.Add(indent + $"value = (int)(part1 | part2);");
        }

        public override void GenerateDecryptCode(List<string> lines, string indent)
        {
            lines.Add(indent + $"uint part1 = (uint)value >> {_rotateBitNum};");
            lines.Add(indent + $"uint part2 = (uint)value << (32 - {_rotateBitNum});");
            lines.Add(indent + $"value = (int)(part1 | part2);");
            lines.Add(indent + $"value ^= {_xorValue} ^ salt;");
            lines.Add(indent + $"value -= {_addValue} + _secretKey[{_index1}];");
        }
    }
}
