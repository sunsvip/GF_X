using NUnit.Framework;
using Obfuz.EncryptionVM.Instructions;
using Obfuz.Utils;
using System;
using System.Collections.Generic;
using UnityEngine.Assertions;
using UnityEngine.UIElements;

namespace Obfuz.EncryptionVM
{
    public class VirtualMachineCreator
    {
        private readonly string _vmGenerationSecretKey;
        private readonly IRandom _random;

        public const int CodeGenerationSecretKeyLength = 1024;

        public const int VirtualMachineVersion = 1;

        public VirtualMachineCreator(string vmGenerationSecretKey)
        {
            _vmGenerationSecretKey = vmGenerationSecretKey;
            byte[] byteGenerationSecretKey = KeyGenerator.GenerateKey(vmGenerationSecretKey, CodeGenerationSecretKeyLength);
            int[] intGenerationSecretKey = KeyGenerator.ConvertToIntKey(byteGenerationSecretKey);
            _random = new RandomWithKey(intGenerationSecretKey, 0);
        }

        private readonly List<Func<IRandom, int, EncryptionInstructionBase>> _instructionCreators = new List<Func<IRandom, int, EncryptionInstructionBase>>
        {
            (r, len) => new AddInstruction(r.NextInt(), r.NextInt(len)),
            (r, len) => new XorInstruction(r.NextInt(), r.NextInt(len)),
            (r, len) => new BitRotateInstruction(r.NextInt(32), r.NextInt(len)),
            (r, len) => new MultipleInstruction(r.NextInt() | 0x1, r.NextInt(len)),
            (r, len) => new AddRotateXorInstruction(r.NextInt(), r.NextInt(len), r.NextInt(32), r.NextInt()),
            (r, len) => new AddXorRotateInstruction(r.NextInt(), r.NextInt(len), r.NextInt(), r.NextInt(32)),
            (r, len) => new XorAddRotateInstruction(r.NextInt(), r.NextInt(), r.NextInt(len), r.NextInt(32)),
            (r, len) => new MultipleRotateXorInstruction(r.NextInt() | 0x1, r.NextInt(len), r.NextInt(32), r.NextInt()),
            (r, len) => new MultipleXorRotateInstruction(r.NextInt() | 0x1,  r.NextInt(len), r.NextInt(), r.NextInt(32)),
            (r, len) => new XorMultipleRotateInstruction(r.NextInt(), r.NextInt() | 0x1, r.NextInt(len), r.NextInt(32)),
        };

        private IEncryptionInstruction CreateRandomInstruction(int intSecretKeyLength)
        {
            return _instructionCreators[_random.NextInt(_instructionCreators.Count)](_random, intSecretKeyLength);
        }

        private EncryptionInstructionWithOpCode CreateEncryptOpCode(ushort code)
        {
            IEncryptionInstruction inst = CreateRandomInstruction(VirtualMachine.SecretKeyLength / sizeof(int));
            return new EncryptionInstructionWithOpCode(code, inst);
        }

        public VirtualMachine CreateVirtualMachine(int opCodeCount)
        {
            if (opCodeCount < 64)
            {
                throw new System.Exception("OpCode count should be >= 64");
            }
            if ((opCodeCount & (opCodeCount - 1)) != 0)
            {
                throw new System.Exception("OpCode count should be power of 2");
            }
            var opCodes = new EncryptionInstructionWithOpCode[opCodeCount];
            for (int i = 0; i < opCodes.Length; i++)
            {
                opCodes[i] = CreateEncryptOpCode((ushort)i);
            }
            return new VirtualMachine(VirtualMachineVersion, _vmGenerationSecretKey, opCodes);
        }
    }
}
