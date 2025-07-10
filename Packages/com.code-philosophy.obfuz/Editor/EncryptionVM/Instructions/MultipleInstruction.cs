using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace Obfuz.EncryptionVM.Instructions
{
    public class MultipleInstruction : EncryptionInstructionBase
    {
        private readonly int _multiValue;
        private readonly int _revertMultiValue;
        private readonly int _opKeyIndex;

        public MultipleInstruction(int addValue, int opKeyIndex)
        {
            _multiValue = addValue;
            _opKeyIndex = opKeyIndex;
            _revertMultiValue = (int)ModInverseOdd((uint)addValue);
            Verify();
        }

        private void Verify()
        {
            int a = 1122334;
            Assert.AreEqual(a, a * _multiValue * _revertMultiValue);
        }

        public static uint ModInverseOdd(uint a)
        {
            if (a % 2 == 0)
                throw new ArgumentException("Input must be an odd number.", nameof(a));

            uint x = 1; // 初始解：x₀ = 1 (mod 2)
            for (int i = 0; i < 5; i++) // 迭代5次（2^1 → 2^32）
            {
                int shift = 2 << i;        // 当前模数为 2^(2^(i+1))
                ulong mod = 1UL << shift; // 使用 ulong 避免溢出
                ulong ax = (ulong)a * x;  // 计算 a*x（64位避免截断）
                ulong term = (2 - ax) % mod;
                x = (uint)((x * term) % mod); // 更新 x，结果截断为 uint
            }
            return x; // 最终解为 x₅ mod 2^32
        }

        public override int Encrypt(int value, int[] secretKey, int salt)
        {
            return value * _multiValue + secretKey[_opKeyIndex] + salt;
        }

        public override int Decrypt(int value, int[] secretKey, int salt)
        {
            return (value - secretKey[_opKeyIndex] - salt) * _revertMultiValue;
        }

        public override void GenerateEncryptCode(List<string> lines, string indent)
        {
            lines.Add(indent + $"value = value *  {_multiValue} + _secretKey[{_opKeyIndex}] + salt;");
        }

        public override void GenerateDecryptCode(List<string> lines, string indent)
        {
            lines.Add(indent + $"value = (value - _secretKey[{_opKeyIndex}] - salt) * {_revertMultiValue};");
        }
    }
}
